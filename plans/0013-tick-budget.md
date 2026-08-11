# 0013 — The Tick budget ledger

**What does a Tick cost?** One table, one row per consumer, priced against **four candidate Tick
budgets** — and, in the column that is the point of the document, **whether each row's multiplicand
was measured or guessed.**

---

## What this is, and what it must never become

**A view, exactly as [`0000`](0000-board.md) is a view.** Every number here is owned somewhere else:
by [`spike-results`](../docs/spike-results.md), by a slice plan, or by
[`0002`](0002-open-questions.md). This file restates them and **cites the owner in every row**. When
it disagrees with an owner, the owner is right and this file is stale.

**It is not a ledger of open questions.** `0002` owns those, and a debt in two ledgers is the defect
[`0012`](0012-corpus-audit.md) exists to diagnose. What this file adds is the one thing no owner can
hold, because it is a property of the *set*: **the sum, how it moves with the target speed, and how
much of it is resting on numbers nobody has measured.**

**It exists because the corpus kept producing alarming figures and nothing added them up.** Each sat
in its own record, in its own currency, with its own caveat, and `0002`'s routing-share row had been
waiting on exactly this — *"cannot be ratified until the Tick's other consumers are priced."*

---

## The four budgets

A Tick budget is the reference rate divided by a **speed multiplier**, and the multiplier is a product
decision that has never been argued anywhere in the corpus. So this file prices every row against all
four rather than picking one:

| Speed | Ticks/s | Budget | What it means |
|---|---|---|---|
| **8×** | 128 | **7.8 ms** | Fast-forward beyond anything `01` describes |
| **4×** | 64 | **15.6 ms** | `CLAUDE.md`'s stated budget, and the only one any document uses |
| **2×** | 32 | **31.25 ms** | |
| **1×** | 16 | **62.5 ms** | The reference rate: a Day is 8m32s |

**Nothing argues that 4× at 1M is the requirement.** The reference rate is `CLAUDE.md`'s constants
table and 4× is where 15.6 ms comes from; that this is the *target* is asserted nowhere. `0002`
separately records that the budget **names no machine**, alongside `adr/0037`'s 8–15 ms band. Typed
**arguable** under `adr/0043` — a session can close it and no measurement can.

Every capture below is **`powersave`** on one Intel i5-10400, single-threaded. Absolutes are upper
bounds; ratios are unaffected.

---

## Unit costs — what is actually measured

Separated from the ledger because a unit cost survives its multiplicand being wrong, and these are
the durable half of the document.

| Unit | Cost | Flat in? | Owner |
|---|---|---|---|
| One Rule **evaluation** (`02 §4.3`'s bakery terms) | **82.84 ns** | yes — 1.8% over two decades | [`0011`](0011-rule-engine-bins-and-rules.md) task 9 |
| One **chain rung**, marginal | **53.6 ns** | — | as above |
| One **whole engine Tick** per due Rule, no term work | **121.6 → 198.3 ns** | **no — the sort** | `RuleTickBenchmarks` |
| ⚠️ **One whole engine Tick per due Rule, *in situ*** | **~552 ns** | — | [`0011`](0011-rule-engine-bins-and-rules.md) **finding 42** |
| ⚠️ **One Rule evaluation, *in situ*** | **~329 ns** | — | as above |
| One **Zone Rule trigger**, 16-Lot sample | **488 → 740 ns** | **no — but the variable is the working set, not the Zone**: 1.56× over a 1,000× Zone against a control that moved 989× | [`0014`](0014-zone-rules-and-the-sweep-family.md) task 9 |
| **Pollution diffusion**, one Cell dirty | **31.6 µs** | — | `MapLayerBenchmarks` |
| **Pollution diffusion**, whole map | **1.01 ms** | — | as above |
| One **State Hash** at 1M | **32.47 ms** | — | [S0a](../docs/spike-results.md) |
| **One routing worst Tick** at 16 Trip starts | **~9.4–10.5 ms** (published as 10.37) | — | [S2 R5](../docs/spike-results.md), five pinned captures |

**The two marked rows are the same quantities measured in a running city rather than in a fixture,
and they are 2.8× and 4.0× the synthetic ones.** They arrived after slice 7 task 10a put a Ruleset in
a 1M world for the first time, and they are the reason the *measured / guessed* column below is no
longer the only thing in this document a reader should be suspicious of. **This table's left-hand
column was never audited the way the multiplicand column was**: a fixture is best case on every axis
nobody was thinking about, and the Rule engine's was best case on three at once — no terms, every
Rule due in one bucket walked in slot order, and no Citizen or Household table competing for cache.
A unit cost is a hypothesis until a real world has produced one. **Routing's 10.37 ms came off a
synthetic harness too and has never met a world.**

> **S2 R7 briefly recorded 10.37 ms as having no artefact at all; that was retracted the same day**,
> when the re-run it prompted found the figure sitting in a table as `10370.13 µs`. The sweep's
> matcher had compared rendered strings, and this document renders in milliseconds where the harness
> prints microseconds. **The unit is measured.**
>
> **What the re-run did establish is that the row's precision was never real.** Seven captures now
> stand behind the cell and the five correctly pinned ones read **9.37, 9.45, 9.51, 10.37 and
> 10.51 ms**. The figure is a **maximum over 256 Ticks**, and a maximum quoted to two decimals in a
> budget row claims a precision a maximum does not have — so the row above now reads **~9.4–10.5 ms**.
> At 4× that is **60–67%** of the budget rather than a point estimate of 66%, which does not move the
> ledger's verdict and does change what may be said about it.
>
> **The row's weakness was always the multiplicand, and that has not improved.** R6.3 found it counts
> **the wrong event entirely** — under static Habit a Trip start is a *lookup*, and the expensive
> event is a **diversion**. A row whose multiplicand is wrong in kind cannot be rescued by a sound
> unit, so this document's dependence on the row stays qualitative. **What the episode adds is about
> this table's own auditing**: the *measured / guessed* column audits the multiplicand, `0011`
> finding 42 showed the unit column was never audited, and an attempt to audit it produced a false
> accusation before it produced a correction. **Three passes over one row, two of which found
> something and one of which invented something.**

**The gap was then attributed rather than left as a warning** (`0011` finding 43). Added to the
fixture one at a time at a fixed due count: **terms ×1.84**, **scatter ×1.49**, **population ×1.14** —
a product of **3.13×** against an observed **3.70×**, with the residual most likely the rest of the
term axis, since the instrument's balanced Rule cannot apply anything. **Terms are the largest axis
and were expected to be the smallest.** Two of the three are properties of a real city and cannot be
optimised away; the term axis is code, and it is `World.FindBin` searching a Building's intrusive Bin
list once per term per evaluation.

**The lesson has a mirror image, and session M walked straight into it three days later.** This
document's warning is *a measured multiplicand against a guessed one*, and the paragraph above adds
*a unit cost is a hypothesis*. Session M then produced **872% of the routing budget** for a proposed
Habit refresh — a **measured unit** (112 route computations per Tick, R7's re-capture) times a
**guessed multiplicand** (one Habit per Citizen at 1M, from nothing), and treated it as a reason to
discard a mechanism. It is the same defect as the Bin Rule row wearing the opposite face, and the
repair is not *stop estimating*: it is **type the two halves of an estimate separately before acting
on it.**

> A **shape** claim — *a rotation costs `store size ÷ period`, so it couples the learning rate to the
> population* — is arithmetic about the mechanism. It holds at every store size, needs no measurement,
> and is `arguable` under `adr/0043`. **That is what a design decision may rest on.**
>
> A **magnitude** claim — *872%* — is the shape multiplied by numbers, and it inherits the softest one.
> It is `measurable`, and until the multiplicand exists it may be used to *decide what to measure* and
> never to decide what to build.

Session M's outcome survived because the shape claim was sufficient on its own: the mechanism chosen
is bounded by a quantity already in the budget *whatever the multiplicand turns out to be*, which is a
property, not a number. **Prefer the option that is robust to your own estimate being wrong** — that
is the one line of this worth carrying to the next row in the ledger.

**The one non-flat unit is the intent sort, and it was found by looking for it.** Phase 3 sorts its
intents into the settle order, which is `O(n log n)` where everything else in the engine is linear.
Measured across two decades of due count the per-Rule cost rises **121.6 → 146.3 → 198.3 ns**, a
**1.63×** rise where `log₂` predicts **1.7×**. So the term is real, it is identified, and it is not a
blow-up: it costs about 63% more per Rule at 100,000 due than at 1,000.

---

## The ledger

At **1,000,000 Citizens**. Shares are of each budget; **a row's share is only as good as its
multiplicand**, which is why that column sits next to it rather than in a footnote.

| Consumer | Phase | Cost/Tick | Multiplicand | 8× | 4× | 2× | 1× |
|---|---|---|---|---|---|---|---|
| **Skeleton, staggered invariants, Layer schedule** | all | 0.112 ms | 1M rows — **measured** | 1.4% | 0.7% | 0.4% | 0.2% |
| ~~**Bin Rule engine**, whole Tick, before term work~~ | ~~1–3~~ | ~~10.42 ms~~ | ~~56,250 due — **guessed**~~ | ~~134%~~ | ~~67%~~ | ~~33%~~ | ~~17%~~ |
| **Bin Rule engine**, whole Tick, **in situ** | 1–3 | **6.4 ms** | 11,586 due — **measured, on a toy Ruleset** | 82% | **41%** | 20% | 10% |
| **Routing** | 4 Move | **~9.4–10.5 ms** — unit **measured**, a *maximum* | 16 Trip starts — **guessed, and the wrong event** | **120–135%** | **60–67%** | **30–34%** | **15–17%** |
| **Microscopic Lane model** | 4 Move | **27.4–29.3 ns a Vehicle** — unit **measured** (S5 L5), a `powersave` **lower bound** | **the Microscopic Cap — unset, and 5b's** | — | — | — | — |
| **Map Layer diffusion**, on the Tick it lands | 5 Layers | 0.03–1.01 ms | dirty region — **measured range** | 0.4–13% | 0.2–6.5% | 0.1–3.2% | 0.05–1.6% |
| **Zone Rules**, worst aligned Tick | 6 Growth | **0.012 ms** | 16 Rules triggering together — **guessed**; unit **measured** | 0.15% | **0.08%** | 0.04% | 0.02% |
| **Event Wheel, general** | 1 Wake | **unbuilt** — slice 9 | — | — | — | — | — |
| **Commit** | 7 | **unbuilt** | — | — | — | — | — |
| | | | | **≥229%** | **≥114%** | **≥57%** | **≥29%** |

**Read the last row across, not down.** The headline is not *we are over budget*; it is that **the
simulation as priced fits at 2× and does not fit at 4×** — and the difference between those is a
product decision nobody has made, not an engineering problem anybody has to solve.

**⚠ The row carrying most of that sum is the weakest one in the table.** At 4×, routing is **60–67
points of the ≥114** — more than every other priced consumer put together, and without it the ledger
reads **42–48%**, which fits at 4× with room. So the headline *fits at 2×, does not fit at 4×* **is a
statement about routing and almost nothing else.** Its unit is measured, is a **maximum** rather than
a mean, and spans 9.37–10.51 ms across five pinned captures; its **multiplicand counts the wrong
event** — R6.3 found that under static Habit a Trip start is a lookup and the expensive event is a
**diversion**, priced at 861.87% of the budget on its own. **So the one correction with a known
direction points sharply up.** This document has priced everything except the row that decides the
answer.

**The sum fell from ≥140% to ≥114% at 4×, and that is not good news.** It moved because the Bin Rule
row stopped being a guess, and the correction happened to point down; the *unit* underneath it moved
**up** by 2.8× at the same time. What the fall actually measures is how much slack there was in a
figure everybody quoted. **At 114% the conclusion is now marginal rather than comfortable** — a single
unbuilt phase, or a real Ruleset instead of a toy one, decides it either way.

**The sum is now short by a *named* hole rather than by an absent one, and that is the change S5
made.** Until 2026-08-11 this table had **no row for the Microscopic tier at all** — not even
*unbuilt*, which the Event Wheel and Commit both get — so the movement subsystem was priced in halves:
routing carried 60–67 points at 4× and the Lane model carried nothing, and nothing in the document
said so. The row above contributes **no share**, because its multiplicand is the Microscopic Cap and
the Cap is unset. **That is a gap and not a debt** (`adr/0052`'s distinction): nothing accretes on a
value that does not exist. What it now does is make the absence visible to anyone reading the sum.

> **⚠ The unit above moved 1.64× on 2026-08-11, and not because the kernel changed.** S5's L5 found
> `IntegerMath.FloorDiv` evaluating its modulo unconditionally, so **every `Fixed.Div` in this project
> was two 64-bit divisions**. Correcting it is bit-identical — 1,060 tests green, no golden baseline
> moved — and took the Lane model from 47.3–48.0 ns to **27.4–29.3 ns**.
>
> **The other rows were then checked rather than assumed, and they are essentially unmoved.** An
> earlier revision of this note said the Bin Rule engine's **6.4 ms** and S0b's **8.72 ms** were
> *"upper bounds by an unknown amount"*. **The amount is now known and it is ~1%.** Measured by the
> two-point slope S0b itself uses — 200,000 Citizens under `rulesets/minimal.toml`, 2,000 against
> 8,000 Ticks, best of three, both trees built Release and pinned to the same core pair — the per-Tick
> cost reads **0.9483 ms before and 0.9383 ms after: 1.011×**.
>
> **The blast radius is narrow for a legible reason: the defect's cost is proportional to how
> division-dense the consumer is.** The Lane kernel does three divisions per Vehicle against ~41 ns of
> other work and gained **1.50×**; the Rule engine does about two per due Rule against ~552 ns and
> gained **1.1%**. **Map Layers cannot have been affected at all** — diffusion and decay go through
> `RoundDiv`, which has no modulo in it. And **routing's hot site was fixed before it was ever
> published**: see the note in `spike-results` → *S5, L5*. **No published figure in this document
> needs withdrawing.**

**What the unit buys, stated as a sensitivity rather than as a forecast.** At 29.3 ns a Vehicle — the
slower of L5's two readings — one core:

| Vehicles held Microscopic | Cost/Tick | 8× | 4× | 2× | 1× |
|---:|---:|---:|---:|---:|---:|
| 25,000 | 0.73 ms | 9.4% | **4.7%** | 2.3% | 1.2% |
| 50,000 | 1.47 ms | 19% | **9.4%** | 4.7% | 2.3% |
| 100,000 | 2.93 ms | 38% | **19%** | 9.4% | 4.7% |
| 186,624 — S2 R2's fixture, **not a stressed count** | 5.47 ms | 70% | **35%** | 18% | 8.7% |
| **532,750** | **15.6 ms** | 200% | **100%** | 50% | 25% |

**Read the last row as the ceiling and none of the others as a prediction.** It rose from 324,945 to
532,750 on the `FloorDiv` correction, which is worth noticing for what it says about ceilings quoted
from unaudited substrates rather than for the number. The Cap is a ratio
`adr/0062` settled the units of — it counts **Vehicles** — and S5 supplies only the affordable half.
How many Vehicles a real city stresses at once is milestone **5b**'s and does not exist, which is why
no row in this table claims a share for it. **A number becoming a decision by being the only number in
the room is a habit this corpus has already recorded**, and this table is where it would happen.

### Notes each row needs

- **The Microscopic row is the first in this table whose unit was measured *before* anything was built
  with it, and it inverts the document's own standing lesson.** `0013`'s organising column is *measured
  multiplicand against guessed*, and its general form is **a unit cost is a hypothesis until a real
  world has produced one**. Here the unit is measured on a kernel and the *multiplicand* is the thing
  that does not exist — the mirror image of the routing row, which has a measured unit and a
  multiplicand that counts the wrong event. **Both are half-priced rows and they are half-priced in
  opposite halves**, so neither can be repaired by the other's method: routing needs 5b's Trip
  generation to fix a **multiplicand**, and the Lane model needs 5b's stress counts to acquire one.
- ~~**The Lane model's unit is a `powersave` lower bound and its tripwire fired on that basis.**~~
  **The prediction in this note was right and its reasoning was wrong, which is worth keeping.** It
  said the unit *"may improve and cannot get worse"* and that the 1.23× T1 needed was within reach —
  attributing the slack to the **governor**. The slack was real and it was in `IntegerMath.FloorDiv`:
  L5 found 1.50× in a redundant modulo, T1 is withdrawn, and the figures above are the corrected ones.
  The `performance` capture is still owed and **no verdict turns on it**.
- **The Bin Rule row is now measured end to end in a running city, and the row it replaced was right
  by cancellation.** The struck row multiplied a synthetic unit cost that was **2.8× too low** by a
  multiplicand that was **~5× too high**, and the product landed within 40% of the truth. That is a
  worse failure than being wrong: any change that moves one factor without the other would have gone
  unnoticed, and both factors were about to move — the unit when a real Ruleset arrived, the
  multiplicand when a real one is chosen.
- **The new row's multiplicand is measured and is still not representative.** 11,586 due per Tick
  comes from `rulesets/minimal.toml`, which carries 2 Rule Instances per Building and says in its own
  header that it models no city; `0002`'s guess is 450 per 1,000 Citizens, which is 3.75 per Building.
  So the **41% share is not a forecast** — it is what this Ruleset costs. **The durable half is the
  unit cost**, and even that is a floor, because these Rules carry one term and `02 §4.3`'s bakery
  carries four.
- **The Bin Rule row supersedes an earlier Phase-2-only estimate of ~60% and a whole-Tick estimate of
  67%.** The first multiplied a measured Phase 2 by an *inferred* Phase 3 re-check; the second
  measured all eight phases but in a fixture. Neither should be quoted.
- **The first row already contains the staggered invariant tier.** S0a's empty Tick is *"the phase
  skeleton, the staggered invariant tier and the Layer schedule, and nothing else"*, and slice 5's
  own extrapolation of the tier to 1M (~91 µs) is 81% of that 0.112 ms — two measurements
  corroborating. Adding them separately would double-count, and an earlier draft of this table did.
- **The Zone Rule row is the first whose multiplicand is guessed and whose share is negligible
  anyway, and that is the finding rather than the number.** Slice 10's tripwire measured that the cost
  of a trigger does not scale with the number of Lots it could look at — 1.56× over three orders of
  magnitude, against a control rung that moved 989× on the same data — so **the multiplicand cannot be
  *Lots***, which was the fear. What is left is *how many Zone Rules a Ruleset declares*, and sixteen
  of them triggering on the same Tick, which is the worst alignment a file can author, costs 0.08% of
  a 15.6 ms budget. The row is here so that a later Ruleset with a hundred Zone Rules has something to
  be checked against, not because 0.012 ms competes with anything.
- **The Layer row is the only one whose multiplicand is not a guess, and that is a property of the
  map rather than good luck.** The pollution kernel has a radius of 8 Cells, so an interior emitter
  makes 289 Cells resident and a 128×128 map **saturates at about 256 scattered sources** — three
  orders of magnitude below the 120,001 Buildings a 1M city holds. No plausible industrial share can
  put a city on the sloped part of that curve, so residency is not a lever the city has. The range
  quoted is the dirty region instead, from one Cell to the whole map, and it is paid **once every 64
  Ticks** — amortised, 0.5–15.8 µs per Tick, which is nothing on any budget.

### Out of the Tick, and they belong here anyway

| | Cost at 1M | 8× | 4× | 2× | 1× | Why it is not a row above |
|---|---|---|---|---|---|---|
| **One State Hash** | 32.47 ms | 416% | 208% | 104% | 52% | Sampled on a cadence, never per-Tick. What it bounds is *how often a hash may be taken*, which every golden-baseline and bisection workflow is downstream of. `05 §9` does not mention it — [`0000`](0000-board.md) → *Owed* |
| **The Decide guard** | 76.4 ms | 979% | 490% | 244% | 122% | A correctness check, not a shipping consumer. On by default, `--no-decide-guard` for long runs. It is here because it was `O(world)` **with no switch at all** until S0a, and being a guard is not what made it affordable |
| **End-of-run invariants** | 4.84 ms at 100k, once | — | — | — | — | `adr/0033`'s *unaffordable per Tick and trivial at the end of a run*, which is the tiering working |

---

## What the sum says

**Three of the five priced rows rest on guessed multiplicands, and the two large ones are among
them.** It
would be wrong to read ≥140% at 4× as *the simulation is 40% over budget*: it is two unit costs
multiplied by two guesses, plus two rows that are genuinely small. It would be equally wrong to
dismiss it, because **the unit costs are real and the guesses are the corpus's own** — 450 Rule
Instances per 1,000 Citizens is `0002`'s number and rate 8 is `02 §4.3`'s bakery.

**The honest statements are the inverted ones**, per the rule S2 R3 established after drafting its own
budget row the wrong way round:

> ~~Bin Rule evaluation fits while fewer than **~188,000 evaluations** occur per Tick at 4×.~~
> **The Bin Rule engine fits while fewer than ~28,000 Rule Instances come due per Tick at 4×**
> (~56,000 at 2×) — measured in situ, whole Tick, one term per Rule.
> Routing fits while fewer than **85 Trips** start per Tick — *mean-derived*, with a measured worst
> Tick already at 66% from an arrival rate 5× lower.
> Pollution diffusion fits on any budget, at any city size, with the whole map dirty.
> Zone Rules fit while fewer than **~21,000 triggers** land on one Tick at 4× — measured at the
> largest Zone, and **independent of how many Lots those Zones contain**.

Each survives its multiplicand being settled elsewhere. None is a verdict.

**The Rule wire moved by 3.3× and it moved onto the corpus's own worked example.** Against `0002`'s
450 Rule Instances per 1,000 Citizens — 450,000 at 1M — the ~28,000 due per Tick at 4× requires a
**mean Rule rate above ~15.9 Ticks**, where the retired evaluation-based wire required only **4.8**.
`02 §4.3`'s bakery runs at **rate 8**: comfortable under the old wire, over budget at 4× under this
one, and exactly on the line at 2×. **That is a statement about the wire, not about the bakery** —
the multiplicand is still a guess and one worked example is not a Ruleset — but it is the first time
the two have been close enough to touch, and it is what the multiplicand being settled will decide.

---

## Levers that are designed in and have never been pulled

Named so the ≥140% is not read as a standing indictment of the architecture, and so nobody pulls one
*before* the multiplicands are real — which would be optimising against a guess.

1. **The target speed.** The four columns above are the lever. 2× fits today with two unbuilt phases
   still to come; 1× fits with room. It costs a sitting rather than a slice, and it is the largest
   single lever in the document.
2. **Everything measured is single-threaded, and Phase 2 is parallel by construction.** `adr/0037`
   makes Decide read-only; `02 §8` rule 3 makes randomness counter-based, so results are independent
   of evaluation order and Phase 2 needs no coordination at all. **Lint 4 — thread-count equivalence —
   is declared and not yet live**, and `05 §6` never decided which thread runs `step()`.
3. **The sort is `O(n log n)` and the shuffle's remit is small.** `adr/0049` narrowed it to *who goes
   short when there is not enough, never how much anyone takes when there is* — so a cheaper
   construction that preserves that property is available in principle. **Not a defect and not
   scheduled**: it is 63% per Rule across two decades, not a wall.
4. **Precedent that these collapse when examined.** The Decide guard was 95% of a run and turned out
   to be a switch. `adr/0037`'s full-world double buffer was *"8–15 ms at 1M — 50–100% of the
   budget"* and deleting it cost nothing and added no bookkeeping.

---

## How this file gets less embarrassing

**Stop guessing multiplicands.** Every *guessed* cell names something that does not exist yet rather
than something nobody bothered to measure:

| The guess | What it is | What would replace it |
|---|---|---|
| 450 Rule Instances per 1,000 Citizens | A sizing ratio invented for the tables | A **real Ruleset** — slice 7 task 10 — then **S0b** counting what a city actually arms |
| Mean Rule rate of 8 Ticks | One worked example's bakery, generalised to every Rule | The same authored Ruleset, the first artefact with more than one rate in it |
| Trip arrival rate | Nothing generates Trips | Trip generation, `06` milestone 5b. Until then S2's O-D family is **invented**, and no figure derived from it may be quoted without naming the rung |
| 15.6 ms at 1M | A product decision nobody has argued | A session. **Arguable, not measurable** |

**Price the unpriced.** ~~Phase 3, and Map Layer diffusion~~ — **both done**, and doing them changed
two things: the engine row grew from an inferred 60% to a measured 67%, and the Layer row turned out
to be the only one in the table with no guess in it at all.

What remains unpriced is **unbuilt**: the general Event Wheel drain (slice 9), Growth and Commit.

---

## When to stop building and start fixing

Recorded as a condition rather than a feeling, so that *are we ignoring this* is checkable.

**Keep building while every over-budget row has a guess in it.** Both do. `0000` already corrected
itself once away from letting an unbuilt concern set the order — *"the design was generating
design"* — and the same failure is available here in performance clothing. S0b exists precisely
because a Tick with nothing in it cannot be priced, which is what S0a proved by finding that every
Tick figure in the corpus had been taken over an empty world.

**Stop and do an architecture pass when this table sums past 100% at the chosen speed with *measured*
multiplicands.** At that point the guesses are gone, the levers above are the remaining options, and
choosing between them is a design decision rather than a benchmark's. Note that *the chosen speed* is
itself one of the levers, so this condition cannot be evaluated until it has been argued.

---

## What building this ledger found

**1. A benchmark that rebuilds its world per iteration measures the rebuild.** The first attempt at
Phase 3 used `IterationSetup` to rebuild a 100,000-Building world, because Phase 3 writes and cannot
be run twice over one arrangement. The measured `Apply` was then the first code to touch freshly
allocated arrays, so it paid that world's page faults and its collection: **error bars wider than the
means**, a 1,000-Building row reading 2.223 ms mean against a 1.517 ms median, and medians scaling
1.5× then 15.5× across rungs a decade apart. Forcing collection into the setup narrowed the bars and
**did not fix the shape**. *A cost that tracks world size rather than work is not a measurement of the
work*, and the figures were discarded rather than published with a caveat.

The way out was to make the work repeatable instead of the setup cheap: **a Rule with no terms leaves
the world bit-identical**, so a whole Tick can be stepped thousands of times over one warm
arrangement. It measures less — no term walking — but it measures it *honestly*, and it is what
identified the sort.

**2. The first Layer sweep swept the wrong axis and produced a falling curve.** Whole-map recompute
read 982 µs at 100 emitters, 1,024 µs at 1,000 and **662 µs at 10,000** — cost falling as sources
rise, which is S2's *an artefact that varies with the swept axis is not distinguishable from a
result*. Two causes, both in the fixture: the stride formula degenerated to 1 at the top rung and laid
every source in a contiguous run, and **residency saturates at ~256 emitters**, so the axis had
stopped moving anything two rungs earlier. Re-swept over the dirty region, the column is monotone and
the whole-map row is flat — **and the flat row is the control that confirms the instrument**, since a
full recompute must not vary with a dirty rectangle it ignores. The 128-side incremental (975 µs)
converging on the full recompute (1,011 µs) is the second corroboration.

**3. A saturation claim was asserted before it was measured, and it was wrong by two rungs.** The
first version of `MapLayerFixtureTests` asserted that 16 scattered emitters would leave most of the
map resident. It leaves **26%**. The ladder now prints every rung and the assertion is made where the
measurement put it. Small, and exactly the shape `adr/0043` is about — the claim was cheap to check
and was nearly shipped as an argument.
