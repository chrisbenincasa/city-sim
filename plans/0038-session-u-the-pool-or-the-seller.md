# 0038 — Session U: the Pool or the seller

✅ **CLOSED 2026-08-22 into [`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md)** — ***a District Pool is a market and not a store, so stock stays with the seller.*** [`adr/0013`](../docs/adr/0013-goods-are-pooled-within-a-district-and-shipped-between.md) is **amended, not superseded**. ⚠ **Read the ADR for the decision and this document for how it was reached** — the two open mechanisms named in *Where it stands* below were both answered after it was written, and *The close* at the foot records how.

~~**⚠ OPENED 2026-08-22. NOTHING BELOW IS SETTLED.**~~ This document is the brief, not the record. It
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

---

---

# The sitting — ran 2026-08-22

**⚠ IT DID NOT CLOSE, and the reason is in *What is left* below.** No ADR is written. `adr/0013`
stands. Tasks 5 and 6 stand as shipped. **What the sitting produced is evidence, one surviving
objection, one cost nobody had named, and a defect it was not looking for.**

**Three mechanical reads, none of them argument**: the wait-list machinery, the term-resolution
machinery, and an inventory of what tasks 5 and 6 built. ***Every finding below is read off a symbol
rather than off a sentence about one*** — which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
used as a method instead of quoted as a rule, and **four of the eight objections in the brief above did
not survive contact with the code.**

---

## What did not survive

**U2 — the *1:1 versus 1:2* problem is not a problem, and *1:3* was never on the table.**
🔴 ***A term does not name a Bin.*** `BinRef` is `(Scope Scope, ResourceId Resource)` — two fields,
`Ruleset.cs:108` — and which Bin that becomes is decided at **evaluation time** by
`RuleEngine.Bin(world, building, reference, rule)`. `Scope.Local` walks the Building's own Bin list;
`Scope.Global` walks the treasury's; `Scope.Pool` throws. **So the counterparty is already a resolution
decision and not a term decision**, and a seller lookup would sit exactly where the other two searches
already sit. The money leg was never a term under either candidate — `Scope`'s own remark: ***"No
payment is ever authored in a Rule."*** ⚠ **This was the brief's second objection for the Pool and it
is withdrawn entirely.**

**U3 — atomicity binds on ONE APPLICATION, not on the buyer's appetite, and the brief overstated it by
reading the word rather than the Rules.** Shipped production Rules declare `{ min = 1, max = 4 }` —
greedy. `02 §4.1`: a Rule *"applies as many times as its inputs allow within that band, and **fails if
it cannot reach `min`**"*, against `min × delta`. **The fixed form `{ min = max }` occurs once in the
whole corpus**, on `taxed.toml`'s tax circuit, which is the *"actor owes a quantum"* case the engine's
own doc names. ⚠ **And because the seller is chosen in resolution, resolution can choose one that holds
a batch** — so a Rule fails only when **no seller in the District** has one, which is a genuine
district-wide shortage and the correct failure. ***The small-seller consequence is real and it is much
smaller than the brief said.***

**U4 — *or it grows* is UNDESIGNED and the argument does not need it.** No Building-upgrade mechanism
exists anywhere in [`06`](../docs/06-roadmap.md), placed or unplaced. Under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) it may not carry
weight — **and after U3 it carries none**, because *a seller holds at least one batch* is a complete
market behaviour on its own. ⚠ **Recorded because the brief accepted it and should not have.**

**U5 — the rework is bounded, and most of tasks 5 and 6 is price rather than custody.** `MarketRuleset`
survives **verbatim, signature included**: `Reprice(Money price, Money ceiling, long level, long rate)`
takes the level as a **plain `long`**, so distributing stock changes *who computes argument three* and
nothing else. The whole `[market]` and `[[hinterland]]` loader path survives; `Price`, `Rate`,
`Consumed` and `District` survive; **20 of 20 `MarketRulesetLoadTests` cases and 22 of 26
`PoolPriceTests` cases survive.** What goes is the `Bin` column and what reaches through it — one
arithmetic line (`Bins.LevelAt`), one identity line (`Bins.Resource`, which wants a real `Resource`
column on the row instead), `CreateDistrictPoolBin`'s return type, `RetirePool`'s stock transfer,
`Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool`, `BinOwnerKind.District` and its **three** uses, and
**one test that genuinely needs a new fixture** — `A_glut_walks_the_price_to_nothing`, which deposits
10,000,000 units into a single Bin. ⚠ **`DistrictPoolTable` has no `Resource` column on purpose** — it
reads the Good off the Bin — so a stockless row needs one added back.

**U6 — the code's own named hole says the Pool is a market.** `RuleEngine.cs:872`, the `Scope.Pool`
throw, written when slice 6 shipped: ***"the Pool is a MARKET, not a wider Bin lookup. A pool term
crosses an ownership boundary, so the Good moves one way and money the other at the prevailing price,
settled atomically with the Rule. Implementing this as a Bin lookup ships an unconserved economy, and
no refusal can catch that."*** ⚠ **Read it for what it is**: a warning about the **money leg**, and it
binds task 7 under *every* candidate. **It is not evidence between them.** What it is evidence of is
that *the Pool is a market rather than a container* is the corpus's fourth independent statement of the
same thing, in a fourth place, by a fourth author.

---

## What survived, and it is one thing

**U7 — 🔴 THE WAIT LIST IS A REAL COST AND IT DID NOT DISSOLVE.** It is structural rather than
incidental, and three facts fix it:

1. **`World.Subscribe(Handle<RuleInstance>, Handle<Bin>, Blocking)`** is the only overload, and
   `RuleInstanceTable.WaitingOn` is a `HandleColumn<Bin>`. **A waiter names exactly one Bin row.**
2. **`RuleInstanceTable.QueueNext` is a single link column shared with the Event Wheel**, so a Rule
   Instance is on **exactly one** list — armed or waiting, never both, never two. ***Subscribing to all
   N sellers is not expressible.***
3. **`World.Drain(binSlot, blocking, tick)` reads `remaining` from `Bins.LevelAt(binSlot)`** — the
   Bin's *own* level — head-only, spend-down, stop-at-first-uncovered. And
   `RuleEngine.Requirement(world, instance, binSlot, blocking)` nets only those terms **whose resolved
   Bin equals `binSlot`**.

**So under stock-with-sellers a blocked buyer must pick one seller to sleep on, and a deposit by any
other seller will not wake it.** ⚠ **Making the `(District, Good)` row the wake target is possible and
is not free**: `Drain` would have to take its `remaining` from somewhere other than the venue Bin's own
level, and `Requirement` would have to match a term that resolved to a **different** Bin. ***Both are
changes to the wait list, which is the single piece of machinery this project has already narrowed
twice this month*** — [`0003`](0003-build-plan.md) queue items **14** and **16**. **Bounded, named, and
not to be waved through.**

**U8 — 🔴 A COST NOBODY HAD NAMED: per-seller price formation is new design.** `04 §4` says the price
is **emergent** and neither the player's nor the Ruleset's. Task 6 makes it emerge from **Pool level
against recent consumption**, one row per `(District, Good)`. **Move stock to sellers and every seller
needs its own price**, which multiplies the price rows from `District × Good` to `Seller × Good` and
makes `Rate`/`Consumed` per-seller and **noisier at exactly the smoothing this corpus has already been
bitten by** (`0037` **F32**, the flooring dead zone). ⚠ **The arithmetic ports unchanged** — `Reprice`
does not care whose level it is given — **so this is a design cost and not an engineering one.**
⚠ **And it lands one milestone early**: [`06`](../docs/06-roadmap.md) milestone **13 is the price
surface**, whose named risk is *"that growth is paced by a sample rather than cleared by a market."*
***Whether 12 should be deciding how a price forms at all is a scoping question this sitting opened and
did not answer.***

---

## The finding it was not looking for

**U9 — 🔴 `World.RetirePool` bypasses the drain, and the encapsulation that prevents it only covers the
outside.** `Deposit` and `Withdraw` are the only doors that move a level, and each calls `Drain`
immediately after; `BinTable._level` is private and `BinTable.Move` is `internal` **precisely so this
cannot be bypassed**. `RetirePool` is inside `World`: it does `Bins.Move(bin, -held); Bins.Move(into,
held);`, calls `WakeAll` on the **dying** Bin, and does **nothing on the heir** — so a waiter on the
heir sleeps through the stock it just inherited. ***The guard was built against outside callers and the
violation came from inside the house.***
**Filed as [`0003`](0003-build-plan.md) queue item 17 under
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md),
on the day, unfixed.** ⚠ **It is unreachable today** — `Scope.Pool` throws, so no Rule can name a Pool
Bin and a Pool Bin's wait lists can never hold a waiter — **and it becomes reachable on the day task 7
lands**, which is the same task that would make it fire. ⚠ **It is not yet writable as a test**, which
is what makes it the kind that ships.

---

## Where it stands

**The scoreboard, stated plainly.** Of the brief's four objections for the Pool: **U2 withdrawn, U3
substantially weakened, U5 measured and small, U7 SURVIVES.** Of its eight against: none was refuted,
and **U6 removes one from the ledger by showing it argues for neither side.**

**What the sitting believes, and it is a belief rather than a decision.** `adr/0013` decided a
**transport** question — Goods move freely inside a District, and are shipped between. ***That is a
claim about REACH, not about STORAGE.*** A bakery reaching any mill in its District at no carriage cost
is the whole of what bounds the freight budget, and it does not require one shared inventory to deliver
it. **On that reading the pooling survives and the Pool-as-container does not**, `adr/0013` is
**amended rather than superseded**, and candidates 2 and 3 collapse into one shape: **stock with
sellers, and the `(District, Good)` row as the market.**

**⚠ WHAT STOPS IT CLOSING IS NOT DOUBT ABOUT THAT READING.** It is that **U7 and U8 are two open
mechanisms and the sitting may not choose them by argument**: one is a change to the wait list, and one
is a price model that milestone 13 may own. ***A sitting that settles the reading and then designs the
replacement in the same breath is how the wait list got narrowed twice.***

**What closing needs, and each item is small:**

1. **The wake target designed** — the `(District, Good)` row as the venue, with `Drain`'s `remaining`
   and `Requirement`'s Bin match both stated. **U7 is the whole of it.**
2. **The price's owner decided** — per-seller at 12, or the `(District, Good)` row keeps a reference
   price and per-seller dispersion waits for 13. **U8, and it is a scoping call.**
3. **Then the ADR**, against `adr/0013`, carrying U2–U9 as its record.

⚠ **The cost claim stays *measurable and unmeasured* through all of it**
([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)).
**Nothing above measured a seller lookup.** What U2 established is that the lookup lives in resolution
beside two searches that already run there — ***which says where it goes and not what it costs.***


---

## The close

**The two open mechanisms were answered the same day, and neither needed new design.**

**U7 — the wait list. ✅ A market row is an event bus, and the buyer parks on the bus.** A seller's
deposit rings it; `World.Drain` walks the list with **the depositing seller's level** as the budget, and
the first waiter that fits wakes. ⚠ **One thing gets SIMPLER rather than harder, which is not what the
brief expected.** `RuleEngine.Requirement` today resolves every term to a slot and compares slots; a
waiter parked on the market compares **`BinRef`** — `(Scope, ResourceId)`, two fields, **no resolution
at all**. ***That is the set arithmetic `RulesetLoader.RefuseUnrelievedChains` already performs at
load***, so the pattern is in the build rather than invented here. ⚠ **A woken waiter still reserves
nothing**, so several may wake against one seller's stock and re-fail — **inherited from `adr/0063`,
not created here**, and the drain's guarantee was always about an instant.

**U8 — the price. ✅ The stand-in already exists and costs nothing.** `Ruleset.ImportCeiling(resource)`
is authored per-Good, is already the value a Pool opens at, and is already the price every trade clears
at on the ten shipped files stating no `[market]`. **A seller opens there.** ⚠ **So `plans/0002` §D
gains NO row** — no new number, no new ratifier, `adr/0052` untouched. ⚠ **A Ruleset-authored per-kind
price was considered and refused**: `04 §4` says the price is emergent and *"neither the player's nor
the Ruleset's"*, and ***a ceiling is a bound rather than a price***, so clearing at it contradicts
nothing. **Per-kind matters only where two kinds sell one Good, and no shipped file does.**
***The `Price` field moves to the seller now so that dispersion is expressible from the first day***,
which is the whole reason to decide it at 12 rather than at 13.

⚠ **What did NOT close, and it is deliberate.** The per-firing seller-lookup cost is **measurable and
unmeasured**, and `adr/0139` says so in its own *Consequences* rather than leaving it to a reader.
***The sitting established where the lookup goes and never what it costs.***
