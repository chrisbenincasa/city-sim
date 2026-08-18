# 0031 — Conserved Money and the treasury

`06` milestone **10**. The brief.

---

## Status

⚠ **SCOPED 2026-08-18, ungated, not started.** Picked because milestone **7** is running in a
concurrent session and milestone **9** — the next number in the spine — turned out not to be the
independent root [`06`](../docs/06-roadmap.md)'s dependency graph says it is. See *What scoping
found* → **F1**.

**Five decisions are open and each is named against the task it blocks.** Two of them are
hash-bearing numbers that the corpus declares owed in prose and that sit in **no ledger**, so they
have no ratifier — [`0002`](0002-open-questions.md) §D has never held a row for either.

---

## Why this milestone exists, in one paragraph

**There is no money.** `Money` is a real quantity type with a signed 64-bit representation and a
`TryDebit`, `ResourceFamily.Money` is a live enum member, the loader refuses a capacity on a money
Bin, and `HouseholdTable` declares `Money` and `Savings` as saved, hashed, cold columns that go into
the save file — and **every writer in the repository is a test, a fixture or the golden builder**.
The build states this about itself in the one place it costs something:
`Evidence/CitizenEvidence.cs:54-62` omits household finances from a Citizen's Evidence and says why —
*"every writer in the repository is a test, so the field would report a legitimate-looking zero for
every Household in every world. It is omitted rather than returned"* — against an
[`02 §9`](../docs/02-simulation-model.md) that names *"household finances"* among the things Evidence
must answer of a Citizen. This milestone gives the columns a producer, and the city a treasury to
conserve them against.

---

## The named risk

**That the economy is not conserved and the city has no balance sheet**
([`adr/0024`](../docs/adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md),
[`adr/0031`](../docs/adr/0031-one-resource-abstraction-and-depth-not-count.md), `CONTEXT.md` → Money).

The risk is not that money is missing — it is that money arrives **unconserved** and nothing notices.
`adr/0024` is explicit that this is the failure it exists to prevent: money from nothing *"makes an
entire class of economic bug, runaway income, invisible to the conservation checks that catch it
everywhere else."* And [`adr/0003`](../docs/adr/0003-deterministic-integer-simulation.md) puts the
detector in the same place — *"conservation invariants are the money detector"* — having already
established that the width check is not one, because *"overflowing i64 would require the money supply
to grow a billionfold."*

⚠ **The invariant the corpus promises does not exist, and two doc-comments in the build say so in
their own words.** `adr/0031` promises *"conservation becomes one invariant. Nothing is created or
destroyed except at the gate is now a single check across all nine Resources."* What is built is
`WorldInvariants.MoneyIsRepresentable`, which `Invariant.cs:142-144` calls *"`adr/0003`'s overflow
detector"* and `WorldInvariants.cs:612-613` calls *"the half of money conserved that can be checked
before there is a treasury to conserve it against."* Both sentences are accurate and neither is the
invariant. ***An obligation specified in three documents and built in none*** is how
`HouseholdHomeExists` came to be reported by nothing, which
[`0014`](0014-zone-rules-and-the-sweep-family.md) task 8 found by audit rather than by failure.

---

## What the build already holds — surveyed 2026-08-18

**More than `06`'s row implies, and the gap is narrower and better-shaped than *no money*.**

| Built and reached | Where |
|---|---|
| `Money`, a signed 64-bit `readonly record struct`, with `TryDebit`, `IsNegative` and comparison | `Quantities/Money.cs:20`. Its remark argues **both** choices — 64-bit because *"income flow at the target population is on the order of 10⁹ per period against an `int` maximum of 2.1×10⁹"*, signed because going unsigned would *"arm `balance - cost`"* |
| `ResourceFamily.Money`, and `[[resource]] family` as a **required** key with no default | `Rules/Ruleset.cs:223`; `RulesetLoader.cs:497-524`, which refuses an unknown family — *"the family decides transport and whether the Bin has a ceiling, so there is no default"* |
| A money Bin is `BinCapacity.Unbounded`, and an authored capacity is **refused** | `RulesetLoader.cs:1207-1222` — *"a finite one would mean an actor too full of money to be paid -- a sale failing on space because the seller is rich"* |
| **Refusal 4, unbalanced money**, per Rule and in both directions | `RulesetLoader.cs:194`, `:1378-1414` — *"a cost paid to nobody is a leak, not a cost"* |
| A money Bin's level is a `long`, and so is every quantity on the path that writes it | [`adr/0065`](../docs/adr/0065-a-bin-holds-a-long-and-unbounded-names-a-ceiling-whose-approach-is-a-defect-rather-than-a-refusal.md); `Readouts.Read` returns `long`, so `ApplyCount.Percent` — *"the door money walks through"* — does not narrow |
| Eviction preserves both columns, deliberately | `World.EvictToPool` (`World.cs:680-688`), `World.DestroyBuilding` (`:1930`), on [`adr/0054`](../docs/adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md) — *"demolition from becoming a hole in `adr/0024`'s conserved Money"* |
| A hot-reload family change is refused | `World.cs:443-447` — *"a Good becoming Money would make every unit already banked either created or destroyed by an edit"* |

| Declared and produced by nothing | Where |
|---|---|
| `HouseholdTable.Money`, `.Savings` | `HouseholdTable.cs:44-45`. Read by one invariant (`WorldInvariants.cs:633-634`); **no production writer** |
| `Money.TryDebit`, `Money.IsNegative` | `Money.cs:38`, `:26`. **Test-only callers.** `src/` cites `TryDebit` in a doc-comment and never calls it |
| `Ruleset.IsConserved` — *"Money and nothing else (`adr/0024`)"* | `Ruleset.cs:1424`. **One test caller**, no production call site |
| `CommandKind.Govern` | `Command.cs:56`. ⚠ `Command.cs:17-18`: *"two of the four are declared and not yet applied… Govern needs Policy, [which does] not exist"* |

| Absent entirely, and named | Where the hole is stated |
|---|---|
| **The treasury** | `RuleEngine.cs:813-817` throws on `Scope.Global` — *"there are no global Bins. A city-wide Bin is one no Building owns, and where it would live is an entity decision — the treasury is the only content named for it"* |
| Any money Resource in a shipped Ruleset | All five files declare `family = "good"` on every `[[resource]]`. The **only** `family = "money"` declaration in the tree is in a loader test |
| A Business balance, a price, a wage, a tax, Upkeep, a Policy | See *Open decisions* 1 and 4 |
| A money magnitude in the Census | No `Metric` member is a money one, so [`01 §6`](../docs/01-player-experience.md)'s **money supply** trajectory indicator is produced by nothing |

---

## What this milestone is, and what closes it

⚠ **This is a *closed* money system, and the closure is the point rather than a shortfall.**
`adr/0024` makes the Outside Connection money's **only** source and sink, and the Outside Connection
is milestone **14**. So no money enters or leaves the city in this milestone, and the money supply is
fixed at the founding balance for the whole of it.

That is the strongest possible test of the thing the milestone exists to build. **A closed system
makes the conservation invariant exact rather than statistical**: the sum over every balance and the
treasury is a constant, checkable to the unit on every Tick of a long run, with no gate flow to
subtract first. The balance of payments — which is `adr/0024`'s endgame and `01 §5.1`'s *"a different
bill — the money supply, not the treasury"* — is milestone 14's, and it is built **on top of** an
invariant that was exact before there was a gate to relax it.

**What must therefore be true at the end**: money moves, in both directions, through the treasury;
the sum never changes; the Household balance sheet has a production writer; and Evidence answers the
question `02 §9` asks and `CitizenEvidence` currently declines.

---

## Open decisions this milestone owes, before the task that needs them

### 1. Where a Business's balance lives — and two ADRs disagree in their own words

⚠ **Blocks task 5.** `adr/0024`'s consequence list says *"**Businesses hold money.** Closes the open
question of whether they hold only Bins. They hold a balance"*, and
[`02 §4.3`](../docs/02-simulation-model.md) generalises it — *"money is nonetheless **held** at
`local` scope by every actor that has any… **no actor's balance is ever `global`**."*

[`adr/0025`](../docs/adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md)`:88` says the
opposite, by name: a Building *"may hold Bins, one Access Point, one Parking Shed. It may **never**
hold a Need, **money**, a Provider List, or a Trip."* There is no `BusinessTable`; a Business in this
build **is** a Building.

⚠ **The clause bans money and permits the container money lives in.** `CONTEXT.md` → Bin says a Bin
holds one Resource and that money is one, and [`04 §2`](../docs/04-economy-and-goods.md) says *"Money
is a Resource too, and its Bin is unbounded."* So a money Bin at `local` scope on a Building is
simultaneously permitted by the clause's first sentence and forbidden by its second.

**Its stated reason does not reach the case it forbids.** `adr/0025`'s test is *"a Building field that
would have to be averaged across its Occupants is a Cohort forming"* — and a Business's balance is
**not** an average over its Occupants; it is the Business's own, and it is exactly what
[`adr/0050`](../docs/adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)
means by *"it buys inputs at Pool prices, sells outputs at Pool prices, and the difference is a
margin nobody had to invent a mechanism for."* ***A prohibition can name a thing its own reason does
not cover***, and the corpus has met the mirror of this — `plans/0026`'s *a doc-comment forbidding one
shape is not a decision permitting the others*. Settle it with an amendment banner on whichever ADR
loses, never by reading one of them charitably at the write site.

### 2. Money's unit — declared owed, and it is in no ledger

⚠ **Blocks task 2.** `adr/0050` states it: *"**Money's unit must be fine relative to prices.** Prices
are integers, so the smallest expressible price is 1 and a coarse unit gives the economy no
resolution. This is a sizing decision owed before the first priced Ruleset."*
[`adr/0065`](../docs/adr/0065-a-bin-holds-a-long-and-unbounded-names-a-ceiling-whose-approach-is-a-defect-rather-than-a-refusal.md) adds that
*"money's unit is a Ruleset choice"*, so it is authored rather than compiled.

⚠ **It has no row in [`0002`](0002-open-questions.md) §D and therefore no ratifier.** The only owner
either ADR names is a **time** — *before the first priced Ruleset* — and under
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
as amended a ratifier names **a machine, a world and a quantity**, of which a date is none. Open the
row **before** choosing the number, or this milestone ships the failure that ADR exists to prevent.

### 3. The founding balance — the same shape, and the corpus already wrote the ratifier down

⚠ **Blocks task 9.** [`01 §3`](../docs/01-player-experience.md) settles the *mechanism* — *"the city
is founded with money and the player did not ask for it. Money's only door is the Outside Connection,
so a city that exports nothing earns nothing and a founding balance of zero is a game that cannot
start"* — and leaves open whether it is *"a **gift** or a **loan to be serviced**."* It also states
its own standing: *"it is world-creation state that enters the treasury Bin, so it is hash-bearing and
needs a named ratifier under `adr/0052`; the ratifier is the first real play session."*

⚠ **That sentence is a §D row that was never written.** It names the ratifier and the refuting
observations, in the document, and no ledger carries it — which is `plans/0012` **Cause 1** in its
one-copy form: *a fact with no copy at all*. File it, then choose.

### 4. Does Upkeep land in this milestone, when its counterparty is unbuilt?

⚠ **Blocks task 6, and it is a scope question rather than a design one.** `06`'s row says this
milestone *"carries the Household balance sheet, Upkeep (`adr/0035`) and Policy's spend (`adr/0033`)."*
[`adr/0035`](../docs/adr/0035-infrastructure-is-priced-by-what-it-consumes.md) specifies Upkeep fully —
*"`construction cost ÷ effective life`"*, as a **Sweep Rule**, with wear stored as *"an absolute count
— never as a fraction of design life."*

**But it also says the money is a transfer rather than a sink**: *"a § spent on a road is not
destroyed — it becomes somebody's income… bought from local Processing it becomes local wages;
imported, it leaves through the gate."* There is no Materials chain, no Business balance and no gate,
so an Upkeep draw in **this** milestone has **no counterparty to pay** — and a draw with no
counterparty is precisely the leak refusal 4 exists to refuse.

Under [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) the absent
Materials chain is **unbuilt**, not refused, so it is evidence of nothing and the question is not
*should Upkeep compensate*. It is: **defer Upkeep to the milestone that supplies its counterparty, or
give it one here.** ⚠ And `adr/0035` supplies an independent reason to expect the former — *"design
life values are the balance surface of the whole Bill axis and are not authored by hand. They are
derived from the share of a mature city's budget Upkeep should occupy"* — so **its numbers are an
output of a budget that does not exist yet**, which makes them unauthorable in this milestone by
`adr/0052` rather than merely awkward.

### 5. Does a `Govern` command fit the Input Log's four fields?

⚠ **Blocks task 5.** `Command.cs:24-28` records that *"Service and Govern have not been examined
against that test"* — the test being whether their payload fits the log's four fields. A Policy is a
Rule with, per `01 §2`, *"a named payer and named beneficiaries"*, and per `CONTEXT.md` → Policy it
*"moves conserved Money between named parties."* That is more structure than a tax rate. Examine it
**before** writing the command, because the Input Log's shape is replay's shape.

---

## Tasks

Ordered so that nothing is built before the decision it rests on. Tasks 1–4 need only decision 2.

### Task 1 — the treasury is a global Bin, and `Scope.Global` stops throwing

`02 §4.3` gives the shape exactly, and it is narrower than *a Bin like any other*: *"`global` names
the **treasury**, and it appears only as the far end of an explicit **transfer** — `local` money out,
`global` money in, balancing within the one atomic Rule. That is the shape the loader accepts and the
only spelling available for a counterparty that is not a market… So the treasury is a destination,
never a balance-holder in the sense actors are. **No transfer executes today, since `global` throws.**"*

⚠ **The reason no actor's balance is `global` is a performance argument with a measurement behind it**,
and it is [`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md)'s: *"left as-is,
every money-consuming Rule in the city would subscribe to one Bin and every tax collection would wake
ten thousand of them."* That ADR also names the escape hatch if it bites — *"the answer is to give
Districts their own budget Bins, not to reintroduce polling"* — so a hot treasury wait list has a
decided response and does not reopen the design.

### Task 2 — a money Resource reaches a shipped Ruleset

**No shipped Ruleset declares one.** Follow the precedent the last three demonstration files set —
*the same file with one thing changed*, with the header carrying why — rather than editing
`minimal.toml`, whose own header says it models no city. This is where decision 2's unit is spent.

### Task 3 — refusal 4 is relaxed to exactly the extent `global` widens it

⚠ **The loader currently refuses the transfer this milestone is built to write, and says so.**
`RulesetLoader.cs:1372-1375`: *"**It refuses more than it needs to today, deliberately.** A wage… and
an import payment both have real counterparties that no scope can currently name, so both would be
refused."* Once `global` names one, the over-refusal is no longer deliberate — it is a stale guard.

⚠ **Relaxing it moves the refusal count of record, which three documents carry and which has drifted
once already**: `adr/0048` names it, `adr/0015` names it, and `adr/0048` records that the number went
out of step before. Update all three in the same commit, or the next reader re-derives the wrong one.

### Task 4 — the conservation invariant `adr/0031` promises

*Nothing is created or destroyed except at the gate*, as a whole-world end-of-run check, with
`MoneyIsRepresentable`'s overflow half kept rather than replaced — they catch different failures and
`02 §10` sorts invariants by frequency, so both sit in the end-of-run tier at no per-Tick cost.

**In this milestone there is no gate**, so the check is an equality against a constant rather than a
sum with a flow term. Write it that way and let milestone 14 add the term; ***an assertion that is
correct and temporarily strict*** is 5b's distinction and the good side of it.

### Task 5 — the Household balance sheet gets a production writer

The circuit that needs neither a price nor a wage: **a Policy sweeps a tax from Households into the
treasury, and a transfer pays it back out.** Both ends are `local`↔`global` transfers, both are Sweep
Rules, and `02 §4.2` supplies the two properties that make it correct — *"a transfer is not a
behaviour, it is an **entitlement** — paying a random subset of the eligible would be a bug"*, and a
Policy paying out of a treasury that runs dry *"pays whom it reaches and reports where it stopped."*
⚠ **The rotation that makes exhaustion fair is specific to this case and does not generalise** —
`02 §4.2` narrows it to *because a treasury is exhausted*, and
[`adr/0055`](../docs/adr/0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md) refuses it for Zone Rules.

Needs decisions 1 and 5.

### Task 6 — Upkeep, or the written record that it was deferred and why

Whichever decision 4 returns. ⚠ **If it is deferred, the deferral is written into `06` and
`adr/0035`, not left in this file** — a milestone quietly shedding a row its roadmap assigns it is how
milestone 10 came to be two milestones wearing one number.

### Task 7 — something to look at

The circular flow, printed: where money is, and what moved. **A balance sheet is a level and a flow at
once**, and `01 §5.1` requires the two money aggregates be reported **separately** — *"a different
bill — the money supply, not the treasury"* — so a picture showing one is a picture that hides the
one the endgame turns on. The Census can carry it: `Series.cs:17` widened a sample to 64 bits
*"because a magnitude is a `Money` or a `Fixed` rather than a count"*, and no `Metric` member has ever
been one.

### Task 8 — Evidence answers the question it currently declines

`CitizenEvidence.cs:54-62` omits household finances and names the exact condition for putting them
back. This task is that condition being met, and it is milestone **6**'s assembler earning its
scoping claim — that Evidence is an assembler rather than a store, so a new fact becomes readable
without a new accumulator.

### Task 9 — the long acceptance run

100,000+ Ticks. **The money sum is invariant to the unit**, so it is asserted as an exact equality and
not a band — the only exact conservation assertion this project will ever have, and it stops being
available the moment milestone 14 opens the gate. ⚠ **Take it while it is exact.** Needs decision 3.

---

## What this milestone must not do

- **Not author a price anywhere.** `04 §4` — prices are *"not set by the player and not authored in
  the Ruleset"* — and `adr/0050` forbids the syntax that would let one in. The price surface is 13.
- **Not build a wage.** `adr/0024` makes wages mandatory *eventually* and calls it *"the largest known
  risk this ADR creates"*; `06` puts them in 15. ⚠ **Do not read *forced* as *scheduled here*.**
- **Not implement `Scope.Pool`.** That is milestone 9's hole in the same switch statement, it is a
  **market** rather than a Bin lookup, and `RuleEngine.cs:803` warns that implementing it as a lookup
  *"ships an unconserved economy, and no refusal can catch that."*
- **Not add an automatic overdraft.** `adr/0035` corrects `adr/0024` on exactly this: *"borrowing is a
  player action"*, and an automatic damper *"delete[s] a decision the player should be making."* The
  treasury genuinely empties and the Rules that could not draw **wait**.
- **Not make destitution solvable.** `adr/0024` records the neutrality commitment at length and says
  the temptation *"should be recognised as a departure from `PLAYER GOVERNS` rather than a refinement
  of it."*

---

## Definition of done

`CLAUDE.md`'s cumulative list, plus:

- `Scope.Global` no longer throws, and a `[[rule]]` in a shipped Ruleset executes a transfer in both
  directions.
- The conservation invariant is an **exact** equality over a whole run, and it is in the end-of-run
  tier with `MoneyIsRepresentable` beside it rather than instead of it.
- `HouseholdTable.Money` has a production writer, and `CitizenEvidence` reports finances.
- Every hash-bearing number this milestone chooses has a **row** in `0002` §D naming a machine, a
  world and a quantity — decisions 2 and 3 are both currently rows that do not exist.
- The State Hash moves and all three golden baselines are re-recorded, with a commit subject that
  says why ([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)).
  ⚠ **Milestone 7 is in flight in another session and also moves the hash**; whichever lands second
  re-records again, which is scheduling and not cost.

---

## What scoping found

### F1 — the District Pool is not the independent root `06`'s graph says it is

⚠ **This is why milestone 10 was picked over milestone 9.** `06`'s dependency graph lists the District
Pool as a **root** — *"nothing in the inventory precedes any of them"*, with the warrant *"needs road
connectivity, which shipped in 5a"* — and carries the edge *District Pool → the price surface*.

`adr/0050` and the throw site itself say otherwise. `RuleEngine.cs:803-811` states, citing that ADR by
name, that a pool term *"crosses an ownership boundary, so the Good moves one way and money the other
at the prevailing price, settled atomically with the Rule."* A trade needs **money** to move and a
**price** to move at — so the Pool is downstream of milestone 10 and of the price surface, and the
graph carries that second edge **pointing the other way**.

***A dependency graph derived from what a mechanism needs in order to exist can miss what it needs in
order to function.*** `06`'s own preamble is what makes this cost something: it calls the graph *"the
sequence's warrant"*, and the same document already recorded, on 2026-08-16, that two of its edges
were stated in a milestone's prose and absent from the graph — ***a dependency stated in a row's prose
is not a dependency the graph knows about***. This is the same failure with the copies **disagreeing**
rather than one being silent. **Filed to [`0012`](0012-corpus-audit.md); not corrected here**, because
the correct repair is either an edge or an amendment to `adr/0050`, and choosing between them is
`06`'s to do rather than a brief's.

### F2 — the treasury and the District Pool are two named holes in one switch statement

`RuleEngine.Bin` refuses three of its four scopes, and two of the refusals are milestones: `Scope.Pool`
is **9** and `Scope.Global` is **10**. Both throws name the milestone that fills them, in prose, in
adjacent arms of one `switch`. ***The clearest statement of Phase 2's economic sequence in the whole
corpus is a control-flow statement***, and no inventory or graph cites it.

### F3 — the milestone's acceptance criterion is already written into the build, twice

`Invariant.cs:142-144` and `WorldInvariants.cs:612-613` each say, unprompted, that what is built is the
overflow half and that *"conservation proper needs a treasury and transactions, neither of which
exists."* This is the good case of the pattern milestone 6 found the bad case of: a doc-comment stating
a **consequence** it is holding up. Nothing had to be re-derived — the criterion was waiting where the
code that fails it lives.

### F4 — two hash-bearing numbers are declared owed in prose and held in no ledger

Money's **unit** (`adr/0050`) and the **founding balance** (`01 §3`). The second is worse, because it
writes out its own ratifier — *"needs a named ratifier under `adr/0052`; the ratifier is the first real
play session"* — and no §D row exists to carry it. ***A number that states its own ratifier inside a
design document is still unratified, because the ledger is what schedules the ratification.*** `0002`
§D's whole function is to be the place a future reader looks; a sentence in `01 §3` is not that place.

### F5 — this milestone is the only one in which conservation is exactly checkable

Money's only source and sink is the Outside Connection, which is milestone **14**. So the money supply
is constant for the whole of milestone 10, and the invariant is an equality rather than a sum with a
flow term. **The exactness is a property of the schedule and it expires.** Take the reading here.

---

## Where this sits

Milestone **10** of `06`'s Phase 2, ungated. It is one of the **six roots**, and `06`'s warrant for
that — *"`adr/0024` rests on nothing unbuilt"* — survived scoping intact, which is not true of the
root beside it (**F1**).

Downstream, by `06`'s graph: **13** the price surface, **14** the Hinterland, Upkeep and Policy and
private capital, **16** the residential choice model, and **19** through both. `adr/0091`'s
compulsory-purchase price and `01 §6`'s **money supply** trajectory indicator are produced by nothing
until this lands.
