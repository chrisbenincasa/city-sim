# 0038 — Session U: the Pool or the seller

**⚠ OPENED 2026-08-22. NOTHING BELOW IS SETTLED.** This document is the brief, not the record. It
states what is being grilled, what has already been conceded on each side, and what would have to be
true for the sitting to close. **No ADR has been written and no code has moved.**

**The document under stress is
[`adr/0013`](../docs/adr/0013-goods-are-pooled-within-a-district-and-shipped-between.md)** — *Goods are
pooled within a District and shipped between* — and with it `04 §4`'s price paragraph and
[`adr/0050`](../docs/adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)'s
counterparty rule.

---

## Why it opened, and it was not scheduled

**It opened out of [`0037`](0037-goods-between-buildings-the-district-pool.md) decision 10**, which
asked *what holds the Pool's money between a Provider's deposit and a consumer's draw* and was sized as
a question to take at the head of task 7. **It did not stay that size.** Answering it required saying
who the two parties to a Pool trade are, and the answer — **the Pool is one of them** — is what
prompted the question that actually matters:

> ***Why doesn't the bakery buy directly from the mill?***

**That is a question about `adr/0013`, and `0037` cannot hold it.** A slice plan may settle a task's
mechanism; it may not amend the ADR its milestone is built on. Filed to
[`0002`](0002-open-questions.md) §A the same day under
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)
— *route the finding to the code that owns it, on the day*.

---

## What the sitting must settle

**One question, typed *arguable*.** No measurement settles it: both designs run, both are affordable
on the evidence below, and what separates them is what the city should *be*.

> **Does a District Pool hold stock, or is stock held by sellers and the Pool reduced to a venue?**

Three named candidates. **None is the front-runner yet.**

| | Candidate | What it means |
|---|---|---|
| **1** | **Pool as inventory** — status quo | `adr/0013` unchanged. A Provider deposits into a Pool Bin; a consumer draws from it. Decision 10 must then be answered, and consignment is the only answer that survives (below) |
| **2** | **Direct bilateral trade** | A consumer buys from a *named* seller's Bin. The Pool ceases to exist as a store. Prices become per-seller |
| **3** | **Pool as venue** — the middle | The `(District, Good)` row survives as **price anchor, wake target and connectivity test**; stock and money live with sellers. **Written down because it exists, not because it is preferred** |

---

## What has been conceded, and by whom

**Recorded because a sitting that reopens a decision must show its own work.** These are the moves
already made in the exchange that opened it, on both sides.

### Conceded against the Pool

1. 🔴 **`adr/0013` never asks who the bakery buys from.** Its *Rejected* section names exactly two
   alternatives — ***ship everything, including within a District***, and ***pool everything,
   city-wide*** — and **both are about movement**. Its case is transport granularity: *"a routing query
   per unit moved"*, GlassBox embodying freight. **Direct bilateral trade inside a District is
   compatible with everything it actually decided**, provided delivery stays instant and free.
   *"The Pool is just a Bin per Good per District"* appears only in **Consequences**, as a note that
   nothing new had to be invented.

2. 🔴 **The corpus has already ruled on this exact reading, one day earlier and on a different clause.**
   `04 §4`, amended by
   [`adr/0133`](../docs/adr/0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md):
   *"`adr/0013` is not reopened: its case was a **simulation-budget argument about query volume**, and
   it never claimed carriage was worth nothing — **free-inside was the default reading of a budget
   decision, not a finding.**"* ***The Pool as counterparty is the same shape of reading.*** One budget
   decision has now been found carrying two unexamined riders in two days.

3. 🔴 **`adr/0013` has never been examined and the corpus says so in writing.**
   [`0018`](0018-session-n-the-bin-the-pool-and-the-economy.md) — session N, the sitting that took this
   cluster — records that `adr/0010`–`0022` sat in **one blanket 🟢 coverage row covering thirteen
   ADRs**, *"which `adr/0043` calls a defect in its own right — so a 🟢 mark here is not evidence any
   sentence in `adr/0013`, `0017` or `0022` was ever examined."* **Session N flagged it and did not
   take it.**

4. ⚠ **The *"it kills the District price"* objection is circular, and it was raised and withdrawn in
   the exchange.** `04 §4`: *"**Local.** Prices are per-District, **because** Goods are pooled
   per-District."* The per-District price is **derived from** the pooling, not an independent
   requirement it must satisfy. Removing the pooling removes the price's premise; it does not break a
   constraint. **What replaces it is per-seller dispersion**, which is a design rather than a
   regression.

5. ⚠ **The cost objection was borrowed from a mechanism that does different work.**
   [`0013`](0013-tick-budget.md)'s job-search analysis is the nearest measured analogue — a Building
   choosing a specific counterparty — and its own finding is ***"where it goes is not the search"***.
   The bill is the **spatial box**: the Commute Budget makes it a 14-Cell radius, **841 Cells**, so
   `CountIn` plus three `NthIn` is **~3,400 `int` reads before any routing**, and the box walk and the
   routing are the same order. **A seller lookup needs no box** — *the mills in my District* is a list.
   `DistrictResidency` is O(1) Cell→District and a per-`(District, Good)` seller list is
   **the shape `DistrictPoolTable` already has**, shipped at task 5. ⚠ **This does not make the cost
   claim settled** — it makes the *analogy* invalid. The real figure is **unmeasured**, and under
   [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
   nothing may cite it as decided.

6. ⚠ **Decision 10 does not get answered under candidates 2 or 3 — it stops being asked.** It exists
   only because the Pool has **no owner**: `MoneyLedger` resolves `Treasury / Household / Business` and
   nothing else. A seller's money Bin is a Business balance that already exists. ***A question that
   dissolves under one candidate and needs a fifth `BinOwnerKind` under another is evidence about the
   candidates.***

7. ⚠ **The corpus already matches labour bilaterally, and nobody has argued for the asymmetry.**
   [`adr/0081`](../docs/adr/0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md)
   has a Citizen pick a **specific Workplace** by satisficing over `candidates`. Goods are pooled.
   **The two mechanisms were designed four milestones apart and no document compares them.**

8. ⚠ **`adr/0133`'s haulage charge needs a payee and direct trade supplies one.** `04 §4` states the
   blocker in its own words: *"a cost with no counterparty destroys money, which `adr/0024` forbids."*
   Under candidate 1 the payee is the thing decision 10 could not find.

### Conceded for the Pool

1. 🔴 **Atomicity, and this is the sharpest objection standing.** `02 §4.1`: ***"A Rule applies in its
   entirety or not at all."*** `RuleEngine.Bin` returns **one** `int` slot. A bakery wanting 6 flour
   from a District holding 4 at mill A and 2 at mill B **cannot split the order** — the engine has no
   way to express a term spanning two Bins. With a Pool, 6 units is 6 units.
   ⚠ **The consequence is a market behaviour, not a bug: a small seller cannot supply a large buyer.**
   **✅ ACCEPTED IN THE EXCHANGE as an economic answer rather than a limitation** — a small mill serves
   small bakeries, or grows. ***But "or grows" names a Building upgrade that does not exist***, so the
   sitting must decide whether the design is complete without it or merely tolerable. Classify under
   [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) before leaning on
   it either way.

2. 🔴 **The purchase gets harder, not easier.** Task 7's central difficulty is that **term resolution is
   1:1 and a purchase is 1:2** — good in, money out, and the money leg has no Bin to subscribe to
   because no term names one. **Direct trade makes it 1:3**: seller's good Bin, buyer's money Bin,
   seller's money Bin. **The engine work grows and the sitting must price it.**

3. ⚠ **The wait list.**
   [`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)
   queues a blocked Rule on **one** Bin, so a bakery short of flour *district-wide* has nothing to
   subscribe to when every individual mill is empty. **This may dissolve** — the `(District, Good)` row
   can itself be the wake target, which is candidate 3 — **but it has not been worked through** and it
   is the objection most likely to decide between 2 and 3.

4. ⚠ **Rework, and it is committed work.** Milestone 12 **tasks 5 and 6 shipped 2026-08-22** on
   Pool-as-inventory: `DistrictPoolTable` with a `Bin` handle, and `Price`/`Rate`/`Consumed` columns
   with a damped reprice. ⚠ **[`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
   forbids citing hash movement as a reason to defer, narrow or split this** — the cost to weigh is the
   *rework*, and the sitting must scope it rather than assume it. **Unscoped as of opening.** The
   working guess, and it is a guess: the row survives, `Bin` becomes a seller list.

---

## What the Pool cannot do, and it is decided rather than arguable

**Under candidate 1, consignment is forced.** This is not a preference and the sitting should not
re-litigate it.

[`adr/0003`](../docs/adr/0003-deterministic-integer-simulation.md): *"The design genuinely never holds
negative money — a destitute Household departs rather than borrowing, a bankrupt Business is a distinct
diagnosis from a starved one, the treasury empties and its Rules wait, and borrowing is an explicit
player action that adds money."* **So a Pool cannot finance inventory.** Paying the Provider at deposit
deadlocks at Tick 0: the Provider waits on money only a draw supplies, and the draw waits on stock only
a sale supplies. ***The first unit must enter unpaid.***

⚠ **And `0037` decision 10 states consignment's cost wrongly.** It says *"the Pool must remember how
much it owes per Good"*. **A debt needs a creditor.** What is actually needed is **units consigned per
`(Business, Good)`** — a **share**, not a debt — which is a larger structure than the column the
decision priced.

---

## What would close it

**A closure is an ADR against `adr/0013`** — amending it if the pooling survives, superseding it if it
does not — plus, in the same sitting:

1. **The atomicity consequence classified** under `adr/0070`: is *a small seller cannot supply a large
   buyer* **refused** (accepted design), or is it **unbuilt** pending a Building upgrade? Only *refused*
   is evidence.
2. **The wait-list target named**, because it is what separates candidates 2 and 3.
3. **The rework scoped against tasks 5 and 6** — which columns survive, which move.
4. **The cost claim typed and routed**, not argued. It is *measurable* and currently unmeasured.
5. **`0037` decision 10 struck**, whichever way it goes: answered under 1, void as posed under 2 and 3.
6. **`04 §4`'s price paragraph rewritten or confirmed**, since its *Local* bullet reads as a consequence
   of whatever this decides.

⚠ **The sitting must not close the cost question by argument.** `adr/0043` binds it: if the per-firing
seller lookup is the deciding term, **it goes to a machine and the sitting stops there.**

---

## Findings

**U1 — `0037` cites `RuleEngine.cs:801` and the symbol is at line 854.** Task 7's entry names a line
number for `RuleEngine.Bin`. ⚠ **The claim is still true** — it returns one `int` — **and the citation
has drifted**, which is exactly what
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
writing half forbids: ***name a symbol, never a time***. Corrected in `0037` on the day.
