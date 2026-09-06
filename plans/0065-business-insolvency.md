# 0065 — Business insolvency, and the failure the city cannot see

**Scoping document. Written 2026-09-06, against `business-insolvency` at `d1ef17e`.**
**[`0045`](0045-amnesty.md) queue — a new row, and the first one found by asking whether a loop
CLOSES rather than by asking what has no reader.**

> ***A Business can take in less than it pays out, at every payday, for the life of the world, and
> nothing in the city notices.***

⚠ **This document exists because the work was started at the keyboard and should not have been.**
A saved hashed column, a new Ruleset key, a chosen unit and a changed tenancy rule went into the tree
in about fifteen minutes, and **six unsettled design questions were written down afterwards** — which
is the tell. The code is kept as a **probe** (see *What the probe found*), because running it produced
a finding no document would have; the design is scoped here before any more of it is written.

---

## Status

✅ **BUILT 2026-09-06, tasks 1, 2, 3, 5, 6, 7 and 8 — the same day it was scoped.** Task 4 is struck.
**All six decisions closed**: 1 and 3 by the user on the scoping call, 2 void, 4, 5 and 6 as
recommended. `scripts/test.sh` green at **2,880**.

⚠ **The scoping pass changed the mechanism before it changed a line of code**, which is the argument
for having written this document at all — see *Two failures, not one*. ***It also cost the document
one of its own three findings***, which is recorded rather than deleted.

⚠ **Runs beside [`0045`](0045-amnesty.md) row 19** — *the Day is a comb* — which is live in another
session. **The two do not share a file**: row 19 owns `CommuteRoster.ShiftStartOf` and
`ServiceEngine.Attend`; this owns `WageEngine`, `BusinessTable` and the tenancy path. ***They do share
three golden baselines***, which is decision 6.

---

## The gap, stated

**The money loop closed over the last two weeks and nobody joined the ends.**

| Half | Where it is | State |
|---|---|---|
| Money **in** | `ShoppingEngine.cs:372`–`373` — a Household withdraws from its purse, the seller's till is credited | ✅ landed in PR #5, 2026-09-05 |
| Money **out** | `WageEngine.Pay` — a Business pays its workers from that till, in worker-id order, until the money runs out | ✅ landed 2026-08-27, `0045` row 6 |
| Failure **detected** | `PayrollReading.Underpaying`, and `Pay` returns `owed` | ✅ written every payday |
| Failure **remembered** | — | 🔴 **nothing** |
| Failure **consequence** | — | 🔴 **nothing** |

**`Underpaying` has zero readers outside the engine that writes it**, and `owed` is computed fresh
every payday and discarded. ***So an insolvency is an instant in this build and never a condition.***

🔴 **And that is the sentence [`CLAUDE.md`](../CLAUDE.md) already carries about the whole city** —
*no shipped world can express **balance → unbalance → balance***. It attributes that to the premises
Rule chain being always-succeeds or never-succeeds, and notes ***a tenant now has a middle***. **A
Business is a tenant and it does not have one**: it has an income, an outgoing, and no state between
solvent and solvent.

---

## Two failures, not one — **the correction, 2026-09-06, by the user on the scoping call**

🔴 **The first draft of this document collapsed two different events into one verb, and the collapse
was invisible until somebody asked what a folded shop does next.**

| | The failure | The road | State |
|---|---|---|---|
| **A** | **A trade loses its FRONT.** Its premises were razed, condemned or emptied. ***It is solvent.*** It waits in the Unpremised Pool for somewhere to trade from, and if none comes within `[placement] gives_up_after_days` it goes bankrupt | `World.Unpremise` → pool → `World.Depart` | ✅ **built, and complete** |
| **B** | **A trade goes BANKRUPT.** It could not pay its staff. ***It is not between premises — it is finished.*** Everybody is dismissed and it is wound up | 🔴 **nothing** | 🔴 **the gap** |

⚠ **The first draft routed B into A's road, and the result does not survive one question:
*if a shop failed, how can it still do business?*** It cannot. `UnfitBusiness` sends **its Rules and
its Bins with the premises**, so an unpremised trade holds no stock and runs no Rule — ***the pool is
for a trade that can still trade and simply has nowhere to do it.*** A bankrupt one is not looking
for a front.

✅ **And the two roads share an END rather than a route**, which is what makes the corrected shape
cheap. **Bankruptcy is the terminal state and there are two ways to reach it**: *could not pay*
(**B**, unbuilt) and *could not find premises in time* (**A**, built, and `Depart` is already that
bankruptcy). ***So this row builds a second road to a destination that already exists.***

⚠ **Which voids one of this document's own findings** — see *What scoping found* **3**. **B does not
enter the Pool**, so it opens no door into it, so nothing about `gives_up_after_days`' obligation
widens. ***The finding was true of the mechanism as first drafted and the mechanism was wrong.***

---

## What the build already holds — surveyed 2026-09-06

**Read this before proposing a mechanism.** Four of the five things insolvency needs already exist,
and the survey is why the task list below is short.

### ✅ What exists and works

- **`World.Unpremise(Handle<Business>, Ticks)`** — ⚠ **which is road A's primitive and NOT this
  row's**, and the first draft's misreading of that is the whole of *Two failures, not one*.
  It takes the trade off the Building's list, calls `UnfitBusiness` so **its Rules and its Bins go
  with the premises and its BALANCE does not** (`adr/0166`, `adr/0144`), severs `Businesses.Building`,
  takes every worker off the commute roster, and joins the Unpremised Pool. ⚠ **The jobs SURVIVE** —
  its own remark: *"This is not a dismissal — the staff keep their employer and lose only the journey,
  which is what an employer between premises means."* ***That sentence is true of a trade between
  premises and false of a bankrupt one***, which is how one verb came to carry two failures.
- **`World.DestroyBusiness(Handle<Business>)`** — **road B's primitive.** Off the Building's list,
  **every worker popped off the employer's list** with `Citizens.Workplace` left severable to answer
  ***my employer is gone***, every owned Bin freed, the row freed. ⚠ **It holds no economics by
  design**, so the money-supply adjustment is the caller's — `Depart` does exactly that one line
  before calling it.
- **`World.Depart(Handle<Business>)`** — retires a Business **already in the pool**, adjusting
  `MoneySupply.Issued` before `DestroyBusiness` frees the row and every Bin it owned. ⚠ **It refuses a
  PREMISED Business** and reports `ABusinessIsPremisedOrItIsInThePool`, so it is not the fold verb.
- **The pool's sink** — `[placement] gives_up_after_days`, which
  [`adr/0144`](../docs/adr/0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md)
  already extends to Businesses: *a shop's search takes as long as a family's, to start.*
  ***So the sink this mechanism needs was built before it and no second bound is owed.***
- **The arrears cap** — `WageEngine.cs:161` and `World.cs:6776`. Per-worker entitlement accrues from
  `LastPaidDay`, is **capped at one pay period**, and the remainder is **forfeit rather than carried**.
  ⚠ **This is `adr/0006`'s sink and it is correct; nothing here should touch it.** Its own measurement:
  before the cap existed, one underpaying shop ran a shortfall of 8,384 on Day 14 and **499,712 on Day
  56**, climbing linearly with no ceiling.

### 🔴 What does not exist

- **No distress state.** `BusinessTable` declares `Building`, `Kind`, `Origin`, `BinHead`, `BinTail`,
  `Balance`, `BuildingNext`, `PoolSlot`, `WorkerHead`, `WorkerTail` — **and nothing that says this
  trade is in trouble.**
- **No fail path.** `GiveUp`, `Fold`, `Dissolve`, `Bankrupt`, `Insolven*` are **zero hits across
  `src/`.**
- **No payroll readout.** `PayrollReading` is reachable only through `simulation.LastPayroll` in
  tests. ⚠ **`Borough.Headless` prints no wage figure in any mode**, which is why the probe's run had
  to be read off `--business` and said nothing. ***A mechanism whose only instrument is a unit test
  cannot satisfy this project's Definition of done.***

---

## What the probe found

**One finding, and it is the argument for having run the code at all.**

🔴 **P1 — A trade may declare a wage, hold no till, load clean, and silently never pay anybody.**

`WageEngine.Pay` opens by resolving `Businesses.Balance`, and on failure returns `(0, 0, 0)` with the
remark *"A world whose Ruleset names no money."* ⚠ **But that is not the only way to reach it.** A
Business's Bins come from its **premises** (`UnfitBusiness`'s own rule), so whether a trade has a till
depends on **which building kind it ends up tenanting** — and nothing checks that the pairing makes
sense.

**Measured**: `rulesets/insolvent.toml` — `waged.toml` with a wage on the `shop` trade — folded
**nothing over 40,960 Ticks**, and the unpremised count never left **0**. The cause is that only
`shopfront` declares `{ resource = "money", owner = "business" }`; `shop` tenants `dwelling`, which
declares **none**. ***So the demonstration world could not demonstrate, and the reason was a defect
one layer below the mechanism being demonstrated.***

⚠ **It is probably NOT refusable at the parse site**, which is what makes it interesting: a trade is
not statically bound to a building kind, so the loader cannot know. **That makes it an invariant or a
readout, and `adr/0048`'s *validate where you parse* does not reach it.** Decision 5 owns where it
goes.

---

## Open decisions

### 1. ✅ SETTLED 2026-09-06 — **insolvency is BANKRUPTCY, and bankruptcy dismisses everybody**

**Decided by the user on the scoping call, and it corrected the question rather than answering it** —
see *Two failures, not one*. A trade that cannot pay its staff **is not an employer between premises**
and does not go looking for a front. **It is wound up**: the staff are dismissed, the balance leaves
the money supply, and the row is freed.

✅ **The verb already exists and it is `DestroyBusiness`.** It takes the trade off the Building's list,
**pops every worker off the employer's list** and leaves `Citizens.Workplace` — declared
`Reference.Severable` for exactly this — answering ***my employer is gone*** rather than *I never had
one*. It frees every Bin the trade owned. ⚠ **What it does NOT do is the money supply**, which
`Depart` does immediately before calling it, ***because the table operation is deliberately kept free
of economics.*** **So road B is: adjust `MoneySupply.Issued`, then `DestroyBusiness`** — and it is
**not** `Depart`, which asserts the trade is already in the Pool.

⚠ **What the FOUNDER loses stays open and is deliberately not answered here.**
[`adr/0146`](../docs/adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md)
spends a Citizen and the Household's money to found a trade, so a founded shop has a person with a
stake — but ***the capital is already spent, and whether failure claws anything further back is a
question about a world nobody has watched.*** **Goes to [`0045`](0045-amnesty.md)'s *Owed when the
freeze lifts* table**, since standing order 2 bars [`0002`](0002-open-questions.md) §A.

### 2. ⚫ VOID — **the question was a property of the wrong mechanism**

~~What drains the pool of folded trades?~~ **Nothing folds into the Pool.** Decision 1 sends a
bankrupt trade to `DestroyBusiness`, so **road B never touches the Unpremised Pool** and the Pool
gains no door.

🔴 **THE FINDING THIS DOCUMENT DREW FROM IT IS VOID WITH IT, AND THAT IS WORTH KEEPING RATHER THAN
DELETING.** The first draft concluded that insolvency was *the third door into the Pool* and that
`ReadGivesUpAfterDays`' obligation — keyed on **gate kinds**, which
[`0046`](0046-life-stages-and-a-self-generating-population.md) stage 3 had already found too narrow —
should widen to cover it. ***That was a true observation about a mechanism that was wrong***, and it
would have shipped a loader refusal enforcing a rule about a door that does not exist.
**`0046` stage 3's complaint stands on its own two cases and this row is not a third.**

⚠ **One residue survives and is filed rather than asserted.** [`CLAUDE.md`](../CLAUDE.md)'s constants
table calls `gives_up_after_days` *"Required of any Ruleset declaring a gate kind and refused
elsewhere"*, and the reader appears to refuse only an **absent-and-needed** key rather than a stated
one — so the *refused elsewhere* half looks false. **Unconfirmed. Belongs to
[`0012`](0012-corpus-audit.md) if it holds**, and nothing in this row rests on it.

### 3. ✅ SETTLED 2026-09-06 — **option b, and the world turned out not to have a margin to tune**

**`rulesets/insolvent.toml`, and it is `shopping.toml` rather than `waged.toml`** — because `shopping`
already has the one trade in the corpus that both **earns** and **pays**: a `grocer` holds a money
Bin, sells to Households who walk in, and pays staff out of that till. ***The base file was chosen by
asking which trade could be caught between the two***, and only one can.

🔴 **F1 — THE WAGE IS NOT THE BINDING CONSTRAINT, AND THE SWEEP IS THE FINDING.** `wage_per_day` was
swept **512 / 1024 / 2048 / 4096** — a fourfold range — and trades wound up moved **44 / 44 / 46 /
46**. **The shortfall scales with the wage and the deaths do not.**

***So these failures are STRUCTURAL rather than marginal***: a fixed subset of grocers has essentially
no custom whatever its payroll, and the rest cover even the highest wage tried. **The city decides
which shops fail, by where they are** — a spatial cause, and it was *predicted* on the scoping call
and is now *measured* rather than assumed.

⚠ **So *thin margin* is not what this file shows, and its header says so rather than letting the
numbers imply it.** **512** is chosen because it is where the city's payroll obligation is roughly met
in aggregate — **2.9M paid against 3.6M owed**, against **10.8M against 37.8M** at 4096 — which is an
honest aggregate and **not a knob that tunes who dies**. ***A world where a trade can earn a little
and not enough does not exist yet***, and building one is what would make a decline threshold
ratifiable.

### 3a. *The original entry, kept for what it framed.* 🔴 What is the demonstration world?

⚠ **`CLAUDE.md`: a demonstration Ruleset is a test fixture, so this decides what the suite covers.**

| Option | What it shows | What it costs |
|---|---|---|
| **a.** `dwelling` gains a money Bin, `shop` gains a wage | Every `shop` in the city fails. **Unbalance only** | Smallest. ⚠ *A trade that cannot sell is not a trade that sold badly* — a fixture, not an economy |
| **b.** `shop` also gains a selling Rule sized so **some** trades survive | **balance → unbalance → balance**, with the grocer as a control in the same world | A selling Rule and a margin, and the margin is a number chosen by taste |
| **c.** Build on `founded.toml` instead | Failure lands on a **founder**, which is decision 1's subject | Largest, and it needs decision 1 answered first |

**Recommendation: b.** ***A world in which failure is certain measures a stopwatch and not a design***
— which is `CLAUDE.md`'s own sentence about why no `0002` §D1 decline number can be ratified, and
option **a** commits exactly that error one level down.

### 4. ✅ Occasions, not duration — **recommended, settleable on the page**

**Consecutive paydays a trade fails to meet in full**, reset to zero by a payroll met in full.
⚠ **Deliberately NOT a duration, where the Household's `gives_up_after_days` is one**: waiting is all
an unplaced Household does, whereas ***a payday is the only moment a Business's solvency is ever
tested***. The same threshold therefore means a week or seven weeks depending on `pay_period_days`,
**and that is correct rather than a unit error** — what is counted is chances to pay.

⚠ **Recovery is the sink and it is not optional.** A count that only rose would retire every Business
in the city on a long enough run — `adr/0006` read backwards: the collection would not grow, but the
population would drain with elapsed time.

**PROVISIONAL under [`0045`](0045-amnesty.md) standing order 4** — chosen by taste, no `0002` §D row,
no ratifier, because `adr/0052` is suspended. ***Ratification needs a city that can fail and the city
needed this to fail at all.***

### 5. 🔴 Where does P1 go — refusal, invariant, or nothing?

A wage-declaring trade with no till. **Not refusable at parse** (a trade is not bound to a building
kind), so the candidates are an **invariant** at end of run, a **readout** row, or **filed and left**.

**Recommendation: a readout row, and file the rest.** It is the same shape as `--business`'s
*Businesses with nobody*, which exists precisely because a silent zero reads as health. An invariant
would fire on hand-built test Rulesets that legitimately declare a trade with no premises at all.

### 6. 🔴 Golden re-records, shared with the row-19 session — **coordination, not design**

**A new saved column on `BusinessTable` moves every world's State Hash immediately, before any
behaviour changes.** That re-records `world-hash.txt`, `session-trace.txt` and
`driving-session-trace.txt`. **Row 19 moves the same three.**

⚠ [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
prices a hash move at **nothing** and forbids citing one as a reason to defer, narrow or split work —
***so this is a merge-order question and never a scope question.*** **What survives is attribution**:
each hash move gets a commit whose subject explains it, and ***two unrelated mechanisms moving one
baseline in one commit is the hazard [`0003`](0003-build-plan.md) names — a re-record is a command; a
mis-attributed hash move is a bug hunt.***

**Recommendation: whoever lands second re-records, in a commit that does nothing else.**

---

## Tasks

**Sequenced. 1 and 2 are gated on decisions 1 and 3.**

| | Task | Gate |
|---|---|---|
| **1** | **The distress state.** A saved, hashed, **saturating** count of consecutive short paydays on `BusinessTable`, written by `WageEngine` and reset by a payroll met in full. ⚠ **Saved and not derived**: nothing else records that last payday was short, so a reload that recomputed it would hand every insolvent trade a clean slate on every load | decision 4 ✅ |
| **2** | **The consequence — BANKRUPTCY.** The threshold crossing adjusts `MoneySupply.Issued` by the till's level and calls `World.DestroyBusiness`. ⚠ **The premises are LEFT STANDING and empty** — `adr/0141`, and milestone 25's *condemnation ends a tenancy and leaves the premises standing*; what ends here is the **trade**, not the building. ⚠ **Not `Depart`**, which asserts the trade is already in the Pool | decision 1 ✅ |
| **3** | **The Ruleset key.** `[[business]] goes_bankrupt_after_short_paydays`, absent means never, refused on a trade that states no wage. ⚠ **Absent-means-never is reached by omitting the key rather than by defaulting one** — `gives_up_after_days`' idiom. ⚠ **The first draft called it `folds_after_short_paydays`; *fold* was the word that let two failures share one verb** | decision 4 ✅ |
| **4** | ⚫ **STRUCK.** ~~The give-up obligation widened~~ — decision 2 is void: road B opens no door into the Pool | — |
| **5** | **The readout.** A payroll section in `Borough.Headless` — paid, workers reached, shortfall, trades short, trades folded, and P1's till-less-wage row. ***Without this the mechanism cannot be watched and is not done*** | decision 5 |
| **6** | **The world.** The demonstration Ruleset, with its own header saying what it exists to show and what it must not be read as | **decision 3** |
| **7** | **Watch it, and record what surprised you.** | 1–6 |
| **8** | **The re-record**, in a commit that does nothing else | decision 6, and row 19 |

---

## What this must not do

- **Must not touch the arrears cap.** `WageEngine`'s one-period forfeit is `adr/0006`'s sink with a
  measurement behind it. ***An insolvency mechanism that made debt accumulate would be reintroducing
  the exact unbounded magnitude the cap was built to kill.***
- **Must not make the count a duration**, and must not let it grow without a reset — decision 4.
- ⚫ ~~**Must not destroy a Business at the threshold.**~~ 🔴 **INVERTED 2026-09-06 by decision 1, and
  kept rather than deleted because it is the exact sentence the correction overturned.** Destroying it
  **is** the mechanism; what must not happen is a bankrupt trade entering the Unpremised Pool, ***which
  is where the first draft sent it.***
- **Must not route bankruptcy through `Depart`.** That verb asserts the trade is already pooled and
  reports `ABusinessIsPremisedOrItIsInThePool` otherwise. **Road B is the money-supply adjustment plus
  `DestroyBusiness`.**
- **Must not take the premises down with the trade.** A bankruptcy empties a Building; it does not
  demolish one. ***The premises stand, and somebody else may take them.***
- **Must not open a §D row or name a ratifier** — standing order 4 suspends `adr/0052`.
- **Must not write an ADR** — standing order 1.
- **Must not cite the hash move as a reason to narrow anything** — `adr/0100`.

---

## Definition of done

Cumulative on [`CLAUDE.md`](../CLAUDE.md)'s list, refined here.

- `dotnet build` clean with no GPU and no Godot; `scripts/test.sh` green.
- **A world that runs balance → unbalance → balance in ONE run**, with a control trade that does not
  fold, if decision 3 takes option **b**.
- **No collection and no magnitude trending upward at steady state** over a long run — specifically
  the Unpremised Pool, which this row puts a third door into.
- 🔴 ***Watched, with something surprising in it*** ([`0045`](0045-amnesty.md)'s amendment). ⚠ **A
  column of hexadecimal does not discharge this**, and neither does a passing test.
- **A `src/` change lands** — standing order 5. ***This document is not the deliverable.***

---

## What scoping found

**Three things, before a line of the real build was written.**

1. **P1** — the till-less wage, above. **Found by running the probe and not by reading anything**, and
   it is the whole argument for the probe having been written.
2. **The sink already existed.** `adr/0144` shares the Household's give-up bound with Businesses, so
   what looked like the hardest open question — *what drains a pool of folded shops* — was settled in
   August and needed a survey rather than a decision.
3. ⚫ ~~**The give-up obligation is keyed on gate kinds and should be keyed on doors**, and this row is
   the third door.~~ 🔴 **WITHDRAWN THE SAME DAY** — see decision 2. ***It was a sound observation
   about a mechanism that turned out to be the wrong mechanism***, and it would have shipped a loader
   refusal policing a door that does not exist.

---

---

## What building it found

🔴 **F2 — `DestroyBusiness` LEFT A PREMISED TRADE'S RULE INSTANCES BEHIND, AND NOTHING HAD EVER
REACHED THAT CASE.** The first run of the mechanism died on
`StaleHandleException` out of `World.FindLocalBin`, on the next `RuleEngine.Evaluate` after the first
bankruptcy. `DestroyBusiness` never called `UnfitBusiness`, so the Building's Rule list kept Instances
naming a freed row.

⚠ **Both prior callers hid it, and neither was wrong.** `Depart` asserts the row is **already in the
pool**, so `Unpremise` has unfitted it on the way in; `DestroyBuilding` is taking the premises down
anyway. ***A bankruptcy is the first case in the build where a premised trade dies and its Building
survives***, and the fix is one hoisted call in `World.DestroyBusiness` — a no-op for both existing
callers, which is what made it safe to fix in the primitive rather than special-case at the new site.
🔴 **It is the failure milestone 27 task 9 died of, arriving from a third side**, and `Unpremise`'s own
remark names two of them.

**F3 — the shortfall dwarfed the wages, and the ratio is the reading.** At the inherited
`wage_per_day = 4096`, 120 Days moved **10,830,365** to Households against **37,795,756** owed and not
covered. ⚠ **A payroll figure alone reads as health**; it is the pair that says the city cannot pay
itself, and neither number means anything without the other.

**F4 — an employer with no staff can never go bankrupt, and that is correct.** `Pay` returns
`owed = 0` when the worker list is empty, which resets the count. ***A trade with nobody to pay has
not failed to pay anybody*** — recorded because it looks like a hole and is not one.

---

🔴 **AND THE ONE THAT MATTERED WAS NOT FOUND BY SCOPING AT ALL.** The three above are mechanical — a
guard, a sink, a key — and the document that held them still had **two different failures wearing one
verb**. ***What separated them was a question about people***: *if a shop failed, how can it still do
business?* Neither the survey, the probe, nor the corpus asked it.

⚠ **The tell was in the vocabulary and nobody read it.** The first draft called the mechanism
**fold**, which is a word that means *close down* and *give up a hand* and *stop trading* all at once
— ***so a name that could describe either failure let the design hold both without noticing.***
`CONTEXT.md`'s rule that every term has exactly one meaning is the standing defence against precisely
this, and it does not reach a word that was never added to it.
