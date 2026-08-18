# 0031 — Conserved Money and the treasury

`06` milestone **10**. The brief.

---

## Status

⚠ **SCOPED 2026-08-18, ungated, not started.** Picked because milestone **7** is running in a
concurrent session and the District Pool — then milestone **9**, now **12** — turned out not to be the
independent root [`06`](../docs/06-roadmap.md)'s dependency graph says it is. See *What scoping
found* → **F1**.

**Six decisions, of which two are settled and four are open**, each named against the task it blocks.
Two of the open ones are hash-bearing numbers that the corpus declares owed in prose and that sit in
**no ledger**, so they have no ratifier — [`0002`](0002-open-questions.md) §D had never held a row
for either until this brief filed them.

✅ **Decision 1 settled 2026-08-18 with the user in the room** —
[`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md):
**a Business is an entity, and this milestone builds it.** ⚠ **It was posed as a contradiction
between two ADRs and there is none** — a Business is an Occupant rather than a Building, so both were
correct and neither is amended; what they contradict is the **build**, which has no Business at all.

✅ **Decision 6 settled the same day** —
[`adr/0114`](../docs/adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md):
**a balance is a Bin, and `BinTable.Owner` becomes discriminated.** It was opened by settling decision
1 and it was the larger of the two. ⚠ **The deciding evidence is not about storage**: the Rule engine
keys blame and waking on a **Bin slot**, so a balance in a column cannot be named as a blocker, joined
as a wait list, woken, or reported with a `Blocking`. ***A balance a Rule can fail on is a Bin,
because the failure surface is what a Bin is for.*** ✅ **`HouseholdTable.Savings` is deleted with
it** — one pool, and what `adr/0024` actually asks for beside it is a **reserve** rather than a second
account.

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
is milestone **11**. So no money enters or leaves the city in this milestone, and the money supply is
fixed at the founding balance for the whole of it.

That is the strongest possible test of the thing the milestone exists to build. **A closed system
makes the conservation invariant exact rather than statistical**: the sum over every balance and the
treasury is a constant, checkable to the unit on every Tick of a long run, with no gate flow to
subtract first. The balance of payments — which is `adr/0024`'s endgame and `01 §5.1`'s *"a different
bill — the money supply, not the treasury"* — is milestone 11's, and it is built **on top of** an
invariant that was exact before there was a gate to relax it.

**What must therefore be true at the end**: money moves, in both directions, through the treasury;
the sum never changes; the Household balance sheet has a production writer; and Evidence answers the
question `02 §9` asks and `CitizenEvidence` currently declines.

---

## Open decisions this milestone owes, before the task that needs them

### ~~1. Where a Business's balance lives~~ — ✅ **SETTLED 2026-08-18 with the user in the room. A Business is an entity, and this milestone builds it.** [`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)

**A Business is a row in a `BusinessTable`, an Occupant of a Building, holding its balance as a
column on its own row exactly as a Household does. A Building never holds money.** This milestone
builds the table and the balance and **nothing else of a Business** — no inputs, outputs, employment
or market behaviour, all of which belong to milestones **12**, **13** and **15**.

⚠ **This entry was posed as a contradiction between two ADRs and there is none.** `CONTEXT.md` had
already settled it: → Occupant is *"a Household or a **Business**. What fills a Building"*, and →
Business is *"the commercial or industrial economic actor **occupying** a Building."* A Business is
not a Building, so `adr/0024`'s *"Businesses hold money"* and `adr/0025`'s *"[a Building] may never
hold a Need, **money**, a Provider List, or a Trip"* are both correct as written and **neither is
amended**.

⚠ **The contradiction was between both ADRs and the build**, which has no Business at all — the word
survives in two doc-comments, `BuildingTable.OccupantHead` links `HouseholdTable.DwellingNext`, and
`BinTable.Owner` is a `HandleColumn<Building>`. So a Business's money had nowhere to go but the one
place `adr/0025` forbids, **because the actor it belongs to does not exist**. ***An apparent
contradiction between two documents can be a contradiction between both of them and the build, and
the tell is that neither document is wrong when read on its own.*** Under `adr/0070` the Business is
**unbuilt**, so *should a Building hold money instead* was void and the answer is to build it.

⚠ **The cheap exit was refused on `adr/0025`'s own test.** A money Bin on the Building fails *"a
Building field that would have to be averaged across its Occupants is a Cohort forming"* — because
`OccupantHead` is the head of a **list**, so such a Bin is a sum over however many Occupants are in
it, which is an average wearing a total. **The clause is the Cohort prohibition applied to the
container, not a preference about where fields go.**

⚠ **Its largest consequence is a schedule finding**: the word *Business* appears **nowhere** in
[`06`](../docs/06-roadmap.md) — not in the milestone table, not in either inventory — though the
entity is fully designed. ***An inventory row naming a mechanism that acts on an entity reads as
scheduling the entity***: *Commercial and industrial placement* is placed at 13, and it places
Buildings rather than creating actors. **Fifth recorded blind spot in that table, and the first where
the mechanism is designed rather than undesigned.**

⚠ **What it does not decide is decision 6 below**, and settling this is what exposed it.

### ~~2. Money's unit~~ — ✅ **SETTLED 2026-08-18 with the user in the room. There is no unit to choose: it is fixed by the smallest fraction the design multiplies money by.** [`adr/0115`](../docs/adr/0115-moneys-unit-is-fixed-by-the-smallest-fraction-the-design-multiplies-by.md)

**It no longer blocks task 2**, and it produced no number. Two sites multiply money by a fraction and **floor** — `02 §5.6`'s tâtonnement, `clamp(demand / supply, 0.9, 1.1)`, chosen against UrbanSim's ±25% *"so players see prices drift rather than snap"*, and `02 §4.1`'s percentage apply count, `FloorDiv(readout × percent, 100)` at `RuleEngine.cs:763`, whose own remark says *"floor division, because a fraction of an application is not an application"*. A quantity `Q` under a step `f` moves only where `f·Q ≥ 1`, so the design's 10% gives `Q ≥ 10` **exactly and with no judgement in it**: a price of 9 units is frozen for ever. ***A design that has chosen its fractions has already chosen its unit.***

⚠ **There is no key, and `adr/0065` already said so without being read that way.** *"Money's unit is a Ruleset choice"* describes the scale being **implied by the money quantities a Ruleset writes down**, not a `unit = X` key — so nothing is declared, nothing can be authored wrongly, and there is no value for `adr/0052` to want a ratifier for. [`0002`](0002-open-questions.md) §D2's row is **retired** rather than filled, the fourth to lose its quantity rather than gain a value.

⚠ **The reason it earned an ADR rather than a paragraph is distributional.** `FloorDiv(readout × 15, 100)` is **zero** for every readout below 7, so under a coarse unit a percentage tax collects **nothing** from the poorest Households and the stated rate from everybody else — a **regressive** outcome produced by rounding, in the mechanic [`adr/0024`](../docs/adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) calls *"the most politically loaded mechanic in the design"* and where it requires the game *"take no position"*. ***An arithmetic artefact that lands on a distributional outcome is a design position taken by accident***, and it is worse than a wrong number, which is at least visible in the file. **What this milestone owes is therefore an instrument rather than a value** — the floor-to-zero counter, in task 5.

⚠ **The entry as scoped follows, kept rather than struck** — it is the record of what was owed, and of the fact that it had gone unowned since `adr/0050` was written.

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

### ~~6. How a Rule reaches a balance~~ — ✅ **SETTLED 2026-08-18 with the user in the room. A balance is a Bin, and a Bin's owner is discriminated.** [`adr/0114`](../docs/adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)

**Money an actor holds lives in a Bin. `BinTable.Owner` becomes a discriminated handle — Building,
Household, Business, treasury — and `HouseholdTable.Money` and `.Savings` are both deleted.**
Opened by settling decision 1, and it was bigger than the decision that exposed it.

**A balance is a column and a Rule can only touch a Bin.** `HouseholdTable.Money` is
`Saved<Money>("money")`, a column (`HouseholdTable.cs:44`). `RuleEngine.Bin` resolves `Scope.Local`
through `World.FindBin(buildingSlot, resource)` (`World.cs:1808`). And **`BinTable.Owner` is a
`HandleColumn<Building>`** (`BinTable.cs:59`) — typed, saved, and the only ownership a Bin has.

⚠ **So no Household, no Business and no treasury can own a Bin, and therefore no money any actor
holds is reachable by any Rule.** Every flow this milestone exists to build crosses that line: a tax
drawn from a Household, a transfer paid to one, `02 §4.3`'s treasury transfer — *"`local` money out,
`global` money in, balancing within the one atomic Rule"* — and `adr/0050`'s margin.

⚠ **`adr/0065` left exactly this open and said so**: *"**what this does not settle:** whether money
belongs in a Bin at all."* It has been open since that ADR and nothing has needed it until now.
⚠ **`CONTEXT.md` states the side the build does not implement** — *"the Household's Money is a `long`
and money is a Resource held in a **Bin**"* — while `HouseholdTable` holds a column, so the corpus
and the build already disagree about the one actor that has a balance.

**Three shapes, and none is obviously right:**

| | What it does | What it costs |
|---|---|---|
| **Widen `BinTable.Owner`** | A Bin's owner becomes a discriminated handle — Building, Household, Business, treasury | A tag in a hot saved column, in a codebase where lint 7 forbids reference types; every Bin lookup pays for a case most Bins never take |
| **Money gets its own term kind** | Balances stay columns; a money term resolves per owner beside the Bin path rather than through `FindBin` | Two paths through the Rule engine. ⚠ But `02 §4.3` already spells a treasury movement as a **transfer** naming two *parties* rather than two Bins, so this may be what the corpus already decided |
| **A Bin table per owner type** | Homogeneous handles throughout | Duplicates the wait-list machinery, which is the thing a Bin exists for |

**The build settled it, and the deciding evidence is not about storage.** `RuleEngine.Check` keys
everything on a **Bin slot**: `_touchedBin[]` dedupes by slot identity, affordability reads
`Bins.LevelAt` and `Bins.Capacity`, failure returns `RuleVerdict.Stopped(instance, rule, **bin**,
blocking)`, and the sleeper queues on that Bin's `SupplyHead` and is woken by a write to it. A
balance in a column is unreachable **four ways at once** — it cannot be named as a blocker, joined
as a wait list, woken, or reported with a `Blocking`. ***A balance a Rule can fail on is a Bin,
because the failure surface is what a Bin is for*** — so option 3 is refuted rather than weighed,
and with it `adr/0050`'s bankruptcy diagnosis, which is *two Bins, two blame targets, two sentences from Evidence*.

⚠ **A money Bin cannot be owned by a Building, which is what forces the discriminator.** An evicted
Household keeps its balance by design (`World.EvictToPool`, `adr/0054`) and `adr/0024`'s destitution
argument turns on a Household that *"cannot afford to move"* — so an actor with no Building holds
money. The treasury is nobody's Building at all, which is the ground `RuleEngine.cs:813` refuses
`Scope.Global` on.

⚠ **Option 2 was refused on where the tag is paid, not on taste.** A table per owner type does not
remove the discriminator, it moves it into the hot path: `_touchedBin` is a flat `int[]` compared on
every term of every evaluation, and a `(table, slot)` key is paid there for ever where one saved
byte per Bin row is paid once at rest. ***Splitting a table to keep a handle typed duplicates
whatever the table was for*** — here, the whole wait-list machinery.

✅ **`Savings` is deleted, and it was never a second account.** Every design sentence describes a
**threshold**: `adr/0024`'s *"reserve **sized by** its Life Stage"* and *"saving has a purpose and
therefore a ceiling"*, its revisit trigger's *"savings **buffer**… where **velocity** is set"*, and
`CONTEXT.md`'s *"savings drain, discretionary spending stops first"*, which is one balance falling.
A Household has **one pool**; what varies is how much of it it will spend, which is a reserve derived
from the Ruleset in force through Life Stage. ⚠ **The reserve is not built here** — nothing spends
discretionary money until **14** and Life Stages are **20**, so choosing its size now would be a
hash-bearing number with no consumer and no ratifier. ⚠ ***A threshold stored as a stock reads as a
second account, and every document that later names the pair inherits it*** — `adr/0054`, `adr/0068`
and `CONTEXT.md` all write *"Money **and** Savings"*, correctly, because by then two columns
existed. **This is `adr/0093` running from the code *into* the documents**, which that ADR does not
consider: its whole subject is prose being wrong about the build.

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
sum with a flow term. Write it that way and let milestone 11 add the term; ***an assertion that is
correct and temporarily strict*** is 5b's distinction and the good side of it.

### Task 4b — `BusinessTable`, and the second Occupant kind the build has never had

⚠ **NEW 2026-08-18, from decision 1** ([`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)).
A `BusinessTable` with a Building handle and a `Money` column, plus a `BusinessHead` on
`BuildingTable`. **The balance is a column on the actor**, on `HouseholdTable.Money`'s precedent and
for its reason — `adr/0024`'s *"one integer per Household and per Business"* is only trivial if both
actors spell it the same way.

⚠ **The occupant list stays homogeneous.** This is a **second list on the Building**, not one
polymorphic list holding two row types — `BuildingTable` already carries `OccupantHead` into
`HouseholdTable.DwellingNext` and `WorkerHead` into `CitizenTable.WorkerNext`, so a Business list is
the **third axis on the precedent of the second** and every handle stays typed. `CONTEXT.md` →
Occupant is therefore a **concept spanning two lists** rather than a discriminated union, which is
what lint 7 wants anyway.

⚠ **Build the balance and stop.** A Business fully modelled *"consumes inputs, produces outputs,
employs Citizens, and offers Goods or services to the market"* — which is milestones **12**, **13**
and **15**. This milestone needs exactly one property: somewhere for conserved money to sit that is
not a Building.

⚠ **How many Businesses occupy one Building is *undesigned*, and nothing in the corpus states it.**
It does not block the table — a balance on the actor is right at any cardinality, which is the
property a Bin on the Building lacks — but it **does** block the first money term a Rule fires on a
workplace, because `local` money must resolve to *an* actor and a list does not name one. It lands
with decision 6.

### Task 5 — the Household balance sheet gets a production writer

The circuit that needs neither a price nor a wage: **a Policy sweeps a tax from Households into the
treasury, and a transfer pays it back out.** Both ends are `local`↔`global` transfers, both are Sweep
Rules, and `02 §4.2` supplies the two properties that make it correct — *"a transfer is not a
behaviour, it is an **entitlement** — paying a random subset of the eligible would be a bug"*, and a
Policy paying out of a treasury that runs dry *"pays whom it reaches and reports where it stopped."*
⚠ **The rotation that makes exhaustion fair is specific to this case and does not generalise** —
`02 §4.2` narrows it to *because a treasury is exhausted*, and
[`adr/0055`](../docs/adr/0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md) refuses it for Zone Rules.

⚠ **It also builds the instrument decision 2 left owed** ([`adr/0115`](../docs/adr/0115-moneys-unit-is-fixed-by-the-smallest-fraction-the-design-multiplies-by.md)): a Census count of **percentage applications that floor to zero**. `FloorDiv(readout × percent, 100)` is zero below `100 ÷ percent`, so a coarse money unit makes this tax collect nothing from the poorest and the stated rate from everybody else — a **regressive** outcome nobody chose, in the mechanic `adr/0024` requires the game take no position on. **A loader cannot check it**, because a readout's magnitude is not known at load time, which is why it is a counter and not a refusal. ***A discipline the loader cannot check needs a counter or it is a comment.*** It is the tax circuit's own instrument and belongs here rather than to a later milestone: **this is the first Rule in the project that ever multiplies money by a fraction.**

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
available the moment milestone 11 opens the gate. ⚠ **Take it while it is exact.** Needs decision 3.

---

## What this milestone must not do

- **Not author a price anywhere.** `04 §4` — prices are *"not set by the player and not authored in
  the Ruleset"* — and `adr/0050` forbids the syntax that would let one in. The price surface is 13.
- **Not build a wage.** `adr/0024` makes wages mandatory *eventually* and calls it *"the largest known
  risk this ADR creates"*; `06` puts them in 15. ⚠ **Do not read *forced* as *scheduled here*.**
- **Not implement `Scope.Pool`.** That is milestone 12's hole in the same switch statement, it is a
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
- A Household's **money Bin** has a production writer, and `CitizenEvidence` reports finances.
  `HouseholdTable.Money` and `.Savings` are both **deleted**, and `MoneyIsRepresentable` is rewritten
  over Bins rather than retargeted ([`adr/0114`](../docs/adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)).
- Every hash-bearing number this milestone chooses has a **row** in `0002` §D naming a machine, a
  world and a quantity — decisions 2 and 3 are both currently rows that do not exist.
- The State Hash moves and all three golden baselines are re-recorded, with a commit subject that
  says why ([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)).
  ⚠ **Milestone 7 is in flight in another session and also moves the hash**; whichever lands second
  re-records again, which is scheduling and not cost.

---

## What scoping found

### F1 — the District Pool is not the independent root `06`'s graph says it is

⚠ **This is why milestone 10 was picked over the District Pool, then numbered 9.** `06`'s dependency graph lists the District
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
rather than one being silent. **Filed to [`0012`](0012-corpus-audit.md).**

✅ **PAID 2026-08-18, and paying it turned up two more of the same kind — see F6.** The graph gains
three edges, the roots table loses the District Pool, and `06`'s economic rows are re-ordered.

### F6 — the same blind spot twice more, and `06` contradicted itself in two adjacent rows

⚠ **Repairing F1 found that every missing edge in that graph is an *anchor* edge.** The price surface
was sequenced **before** the Hinterland, while `06`'s own row for the Hinterland says that milestone
retires *"that **no price in the design has an anchor**"*. The ADRs are unambiguous —
[`adr/0050`](../docs/adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md):
*"**Prices anchor to the Hinterland**… An emergent price needs an anchor or it can run away"*, with
local prices *"bounded above by **Hinterland price + haul**"*;
[`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md): *"every price system in
this design now anchors to the same **authored object**"*, for Goods, rents **and** wages;
[`04 §4`](../docs/04-economy-and-goods.md): *"the Outside Connection price is a **ceiling**."* **The
graph carried none of the three.**

***A dependency graph derived from existence conditions is systematically blind to bounding
conditions***, and the tell is that its edges all read *"needs X, which shipped"*. `adr/0050` names
what building a price with no anchor produces: *"an unbounded price is an unbounded integer arriving
at a money Bin."*

⚠ **The Pool and the price surface then looked mutually dependent, and the cycle is broken by a
distinction the ADRs already draw.** A pool term settles *"at the prevailing price"*, and a price is
the price of what is in a Pool (`04 §6`) — but `adr/0050`'s ladder is *"not only a **source** ladder;
it is a **price** ladder, monotone increasing"*, guaranteed by the import ceiling. **So a price
bounded by the Hinterland exists before any tâtonnement does**: the Hinterland supplies a ceiling, the
Pool trades at it, and local prices responding to Pool supply are the price surface's own work.
Settled with the user in the room.

⚠ **The resulting order is *forced*, which is the part worth keeping.** Given Money → the Hinterland,
the Hinterland → the Pool, the Pool → the price surface and land value → the price surface, slots 9
through 14 admit exactly one arrangement: **9** land value, **10** money, **11** the Hinterland,
**12** the District Pool, **13** the price surface, **14** the Provider List. Four rows permute, 10
and 13 keep their numbers, and everything from 15 up is untouched. ***A re-derivation that comes out
forced is evidence the constraints were real*** — session K's had freedom and said so.

⚠ **And the renumber found a defect in the machinery that exists to absorb renumbers.**
`06`'s retired-numbering table is two columns, *Was → Is now*, which **assumes exactly one renumber**.
There have now been two, so *"milestone 12"* resolves differently depending on whether the citation
predates 2026-08-18. ***A retired-numbering table is generation-scoped and nothing in its two-column
form says so.*** Each block now carries the window it applies to.

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

Money's only source and sink is the Outside Connection, which is milestone **11**. So the money supply
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
