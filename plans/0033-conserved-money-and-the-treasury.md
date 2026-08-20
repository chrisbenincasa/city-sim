# 0031 — Conserved Money and the treasury

`06` milestone **10**. The brief.

---

## Status

**SCOPED 2026-08-18, ungated, IN FLIGHT.** Picked because milestone **7** is running in a concurrent
session and the District Pool — then milestone **9**, now **12** — turned out not to be the independent
root [`06`](../docs/06-roadmap.md)'s dependency graph says it is. See *What scoping found* → **F1**.

✅ **All six decisions are settled**, `adr/0113`–`adr/0118`, every one with the user in the room. Two of
them were hash-bearing numbers the corpus declared owed in prose while they sat in **no ledger**, so
they had no ratifier — [`0002`](0002-open-questions.md) §D had never held a row for either until this
brief filed them, and ⚠ **one of the two dissolved rather than closing**: `adr/0115` found money has no
unit to choose, so what the milestone owed was an **instrument** rather than a value, and task 5 built
it.

✅ **Tasks 1, 2, 3, 4, 4b, 4c, 5, 7 and 8 are DONE**, and task 6 was **discharged at scoping**. ⚠ **Two of
those tasks did not exist when this document was written** — 4b and 4c — and 4c is the one that
matters: it is decision 6 built, ***the milestone's largest decision had no task at all***, and task 5
rested wholly on it while naming decisions 1 and 5 and not 6. **What is left is 9** — the long
acceptance run.

⚠ **Task 7 was taken ahead of 8 and moves no baseline**, which is worth stating because the milestone's
other tasks all did: the Census is an instrument outside the world, so the picture could land at any
point in the ordering without scheduling a re-record against milestone 7's concurrent hash move.

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
| ~~A money magnitude in the Census~~ ✅ **BUILT by task 7, 2026-08-19** | ~~No `Metric` member is a money one, so [`01 §6`](../docs/01-player-experience.md)'s **money supply** trajectory indicator is produced by nothing~~ — two families now: `MetricSource.Money`, six levels, and `MetricSource.MoneyFlow`, two flows |

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

### ~~3. The founding balance~~ — ✅ **SETTLED 2026-08-18 with the user in the room. The treasury opens empty; the founding balance is a ratio this milestone holds neither side of; gift or loan stays open and that is free.** [`adr/0116`](../docs/adr/0116-the-treasury-opens-empty-and-a-founding-balance-is-a-ratio-this-milestone-holds-neither-side-of.md)

**Three answers, and none of them is a number.**

1. **The treasury opens at zero and nothing authors it — derived, not chosen.** Task 5's tax flows in before its transfer pays out, so the circuit never needs an opening stock; and an empty treasury is what makes `02 §4.2`'s *"pays whom it reaches and reports where it stopped"* branch reachable on the **first sweep** rather than after a long run somebody constructs. ***A baseline records what a run did***, and a comfortable opening stock would have hidden the branch this milestone exists to demonstrate. ⚠ **It sits inside the founding balance's own range** — session F's placeholder trap — so what separates them is the **missing consumer**, not the value, and the guard is a sentence in the fixture's header on `congested.toml`'s precedent.
2. **The founding balance is deferred to the first playable build.** `01 §3`'s two ends — *"enough to get started and not enough to win"* — are both denominated in **what things cost**, and there is no construction cost, no wage, no price surface and no gate. A figure here is a numerator with no denominator (`plans/0012` **Cause 5**). Its `§D2` row is **filed**, with `01 §3`'s own ratifier.
3. **Gift or loan stays open, and the ADR checks that it is free rather than asserting it.** A loan is the same balance with `Govern`'s borrow lever pre-pulled — borrowing is already settled as a player action that **adds** money — and **a debt is not negative money** (`adr/0003`), so a principal never enters the conservation sum and **task 4 reads identically under either branch**. ***An axis left open is free only when both branches need the same representation, and that is checkable rather than assumed.***

⚠ **It blocked task 4 as well as task 9, which this brief did not say** — the conservation anchor is a founding-balance question — and it resolves there to *zero contribution*, which makes the equality tighter.

⚠ **Two findings outrank the decision**, both in the ADR: `01 §3` quoted **half** a sentence whose other half reverses it, from the wrong `CONTEXT.md` entry; and ***the damper cannot reach a command*** — `adr/0035` §3a covers a **Rule** that cannot draw, the first ten minutes are made of **commands**, and a command cannot wait. The second is filed to [`0002`](0002-open-questions.md) §C as **undesigned**.

⚠ **The entry as scoped follows, kept rather than struck** — it is the record of a ratifier that sat in a design document for the life of the project with no ledger behind it.

⚠ **Blocks task 9.** [`01 §3`](../docs/01-player-experience.md) settles the *mechanism* — *"the city
is founded with money and the player did not ask for it. Money's only door is the Outside Connection,
so a city that exports nothing earns nothing and a founding balance of zero is a game that cannot
start"* — and leaves open whether it is *"a **gift** or a **loan to be serviced**."* It also states
its own standing: *"it is world-creation state that enters the treasury Bin, so it is hash-bearing and
needs a named ratifier under `adr/0052`; the ratifier is the first real play session."*

⚠ **That sentence is a §D row that was never written.** It names the ratifier and the refuting
observations, in the document, and no ledger carries it — which is `plans/0012` **Cause 1** in its
one-copy form: *a fact with no copy at all*. File it, then choose.

### ~~4. Does Upkeep land in this milestone?~~ — ✅ **SETTLED 2026-08-18 with the user in the room. No. It leaves for 12, and the blocker is a Rule with no actor.** [`adr/0117`](../docs/adr/0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md)

**Four blockers, each independently sufficient, and the fourth was in nobody's document.**

1. **The loader already refuses it, by name, and has since slice 7.** Refusal 4's own remark: *"a wage… and **an import payment** both have real counterparties that **no scope can currently name**, so both would be refused. Neither is writeable anyway, and a refusal that says so is better than a leak that does not."* `adr/0035` sends Upkeep's money to exactly there. So it is **unloadable**, not merely awkward — `adr/0093` working forwards.
2. **Neither term of `construction cost ÷ effective life` exists.** **No Ruleset key anywhere authors a cost of anything**, and `adr/0035` denominates the money price in **Lane-Tiles**, whose entity is milestone **21**; design life values are *"**not authored by hand**… derived from the share of a mature city's budget"*, and there is no budget. One term has no unit, the other a derivation with no inputs.
3. **The shape is a transfer's syntax over a purchase's semantics.** `plans/0011` finding 6 settled that these are *different shapes rather than two spellings of one*; `adr/0035` writes an **authored money amount** and sends it to a supplier at a market price. Its own title says which one it is — ***priced by what it consumes, never by a budget*** — so whether the authored quantity is Money or **Materials** is undecided and it decides the mechanism.
4. ⚠ **An Upkeep Rule has no actor.** `adr/0035` names the **family** and the **cadence** and never what the Rule is attached to. `RuleEngine.Bin(World, **int building**, …)` resolves every scope through a Building, and `adr/0114` enumerated a Bin's four owners — Building, Household, Business, treasury — **with no Segment**, correctly, since a Segment holds no money. **Subject, payer and counterparty are three different things and the engine has no Rule whose subject is not its payer.** Task 5's tax is the near miss: a Household is payer *and* subject, so it needs nothing new. ***Naming a Rule family says how often a Rule runs and never what it is attached to.***

⚠ **12 is where the counterparty becomes nameable and it discharges one blocker of four.** `Scope.Pool` is the only market spelling the enum has. Grounds 2, 3 and 4 survive the move and are written into `06`'s row for whoever picks it up.

⚠ **`06` disagreed with its own dependency graph.** The graph carries **Money → Upkeep**; the table put Upkeep inside Money's row. ***An edge from A to B says B comes after A, and putting B inside A's row reads that edge as "at the same time"*** — third sighting, after the District Pool inside 3a and milestone 10 being two milestones wearing one number.

⚠ **The entry as scoped follows, kept rather than struck.**

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

### ~~5. Does a `Govern` command fit the Input Log's four fields?~~ — ✅ **SETTLED 2026-08-18 with the user in the room. It fits, with room left, and the hard part is elsewhere.** [`adr/0118`](../docs/adr/0118-govern-fits-the-four-fields-and-the-hard-part-is-that-it-writes-to-the-ruleset-rather-than-to-the-world.md)

**The format stays at version 1.** A lever is *which lever*, *what value* and *where* — and `CONTEXT.md`'s scoping rule maps straight onto the fields: *"anything attached to a place can be overridden per District… **global is the default level, not a separate category**."* So `Zone`'s sixteen bits carry the lever and its value, and `East`/`North` carry the override's place.

⚠ **A District is named by a point and never by an index**, and that is required rather than convenient: **there is no `DistrictTable`**, a District is `RoutingPartition`'s partition at a **provisional, unratified 4 Cells**, so an index in a log is a log whose meaning moves when that constant is ratified. `Connect` already carries an origin and derives the far endpoint for the same reason. ***A log names places and never derived indices.***

⚠ **Global is a bit in `Zone`, never a sentinel coordinate** — every Tile coordinate is a legitimate place, so none can mean *unset*. **Third application of session F's placeholder rule in this milestone**, after the money unit and the opening balance.

⚠ **The examination's real finding is not about the fields.** ***Every verb before this one wrote to the world; `Govern` writes to the Ruleset in force.*** `Zone`, `Connect`, `Trip` and `Populate` are one-shot mutations on world entities; a tax rate is a **standing setting** that is saved, hashed state keyed on a **`RuleId`** — a dense **positional** index whose name lives only in `Borough.Formats` (`adr/0048`). **Replay is safe by construction** (`TickInput.RulesetHash` is per Tick and the reload runs at the top of Phase 0) **and the live hot reload is not**: inserting a `[[rule]]` shifts every later index, `RulesetDegradation` re-arms the world's Rules, and ***a saved setting cannot be re-derived***. What a Policy is keyed on is filed to [`0002`](0002-open-questions.md) §C.

✅ **And it does not block task 5.** That task's tax and transfer are `[[rule]]`s a demonstration Ruleset states; nothing has to set a rate from outside, so **milestone 10 builds no `Govern` command at all**. Fourth decision running whose expected requirement turned out not to be one. The examination stands because `Command.cs` asked for it **in writing**, and an unexamined claim in a doc-comment is what `adr/0093` is about — ⚠ **`Service` is still unexamined** and that half of the sentence stands.

⚠ **Borrowing is not this milestone's, and `06` said otherwise inside a bundled row.** Its inventory placed *"Conserved Money, treasury, balance of payments, **borrowing**"* at 10 while milestone 10's own row never mentions it — **fourth sighting of a mechanism hidden inside another row**. Split out and marked unplaced. A borrow amount is a `long` that sixteen bits cannot hold; `East`/`North` are sixty-four bits and are **free exactly when the lever is global**, which `CONTEXT.md` says borrowing alone is — **noted as a fit and deliberately not designed**, since what a borrow command carries is *undesigned*.

⚠ **The entry as scoped follows, kept rather than struck.**

⚠ **Blocks task 5.** `Command.cs:24-28` records that *"Service and Govern have not been examined
against that test"* — the test being whether their payload fits the log's four fields. A Policy is a
Rule with, per `01 §2`, *"a named payer and named beneficiaries"*, and per `CONTEXT.md` → Policy it
*"moves conserved Money between named parties."* That is more structure than a tax rate. Examine it
**before** writing the command, because the Input Log's shape is replay's shape.

### ~~6. How a Rule reaches a balance~~ — ✅ **SETTLED 2026-08-18, and ✅ BUILT 2026-08-19 by task 4c — which was added because this decision was the only one of the six with no task.** [`adr/0114`](../docs/adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)

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

### ~~Task 1 — the treasury is a global Bin, and `Scope.Global` stops throwing~~ — ✅ **DONE 2026-08-18**

`Scope.Global` resolves. A `[[rule]]` drawing `local` money and returning `global` money executes,
money is conserved across it, and a Building that cannot pay **waits on its own Bin** — which is
[`adr/0114`](../docs/adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)'s
entire reason, asserted. **1,574 tests green; one golden baseline re-recorded.**

**What it built.** `BinOwnerKind` — `None`, `Building`, `Household`, `Business`, `Treasury`, all four
enumerated because `adr/0114` enumerated them, with the two unbuilt ones **throwing by name** on
`Scope.Pool`'s precedent. `BinTable.OwnerKind`, a `(saved AND hashed)` column beside `Owner`, which is
`adr/0114`'s *the kind is part of the value* satisfied as a second folded column rather than inside the
handle. `TreasuryTable` — one row, allocated in its constructor on `RulesetTrailTable`'s precedent,
holding nothing but the head and tail of the treasury's Bins. `World.FindTreasuryBin`,
`CreateTreasuryBin` and `FitTreasury`, which gives the treasury **one Bin per *conserved* Resource**,
unbounded, empty.

⚠ **`Owner` stayed a `HandleColumn<Building>` and that was a real fork.** A single polymorphic column
addressing four tables needs a new column type whose fold dispatches over the kind and whose dangling
check does the same — machinery paid by every Bin in the world so one singleton need not be spelled
separately. The treasury has no owner row to point at under either shape, so its handle is unset either
way. **`adr/0114`'s wording — *gains an owner kind* — is the cheaper half and it is the one built.**

⚠ **The sharpest finding is in `RebuildDerived`, and it would have shipped silently.** The existing Bin
relink is `if (IsLive(slot) && Buildings.Rows.TryResolve(Bins.Owner[slot], out …))`. A treasury Bin's
owner handle is unset **by design**, so `TryResolve` fails and the Bin would have been dropped from
every list **on every load** — the money still in a saved row that nothing could reach, with no error.
***A walk that cannot tell "points at nothing by design" from "points at something that is gone" drops
both.*** And `HandleColumn.IsDangling` says exactly this in its own remark — *"The unset handle is not
dangling… a walk that could not tell those apart would report every empty field in the city"* — so
**the column had already made the distinction and the walk had not**. The relink now branches on the
**kind**. A test asserts the round trip and that the State Hash is unchanged across it.

⚠ **The invariant could not have caught the capacity defect, because it recomputes the producer's own
expression.** `RebuildCapacities` derives a ceiling from `(Building kind, Resource)`; the treasury has
no kind, so it would have been handed the **zero** `DeclaredCapacity` returns for a kind nobody
declared — a treasury that can hold nothing, and therefore one that every transfer into it fails
against. `BinCapacityMatchesItsDeclaration` asserts `Capacity == DeclaredCapacity(kind, resource)`, the
same call, so it would have read **true** over the wrong number. ***An invariant that recomputes the
producer's expression checks that the write happened and never what was written.*** Both sites now
branch on the kind, and the invariant asserts the treasury against `04 §2`'s *"Money is a Resource too,
and its Bin is unbounded"* instead.

⚠ **`FitTreasury` runs in the constructor as well as in `Adopt`, and only one of those is obvious.**
`Adopt` is the **swap** path; a world constructed with a Ruleset never adopts one. Fitting only on swap
would have left every world that loaded its Ruleset at construction with a `global` scope resolving to
nothing — *the same hole this task exists to close, differently spelt*. ***A reconcile that runs on
change never runs on the first one.***

⚠ **`TreasuryTable` is deliberately NOT in `World._tables`**, and the rule is 5a's: **a wholly-derived
table cannot join it**, because `Rows.Fold` folds the allocator's four scalars *before* consulting any
column's disposition, so such a table hashes its own allocation history. It is harmless here only
because the one row is allocated once and never freed — and a constant is not a reason to add a row to
a list whose order is a re-baseline to change. **The treasury's state is in `Bins.Rows`, which is in
the list.** The reason is a comment at the list, where the next person will look.

⚠ **The named hole moved rather than closing.** `A_scope_this_build_does_not_have_is_a_named_hole` lost
its `Global` row and kept `Pool`, and `global` naming a **non-conserved** Resource now refuses by name:
`02 §4.3` is narrow about what `global` is for, so a city-wide larder of Flour is a different mechanism
nothing has designed. **The negative test was narrowed and replaced by positive assertions rather than
deleted** — milestone 8 task 10's *a negative test was a closed door*, applied on the way out.

*Original scoping text follows.*

### ~~Task 1's brief~~

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

### Task 2 — a money Resource reaches a shipped Ruleset — ✅ **DONE 2026-08-18**

**`rulesets/monetised.toml`**, the sixth shipped Ruleset and the first in the project's life to
declare `family = "money"`. It is `minimal.toml` **verbatim** with one `[[resource]]` block added,
on `severance.toml`'s and `congested.toml`'s precedent rather than `diagnosed.toml`'s, and the
header carries why. `TreasuryFromAFileTests` holds it to three claims — that it is `minimal.toml`
plus exactly three content lines, that a world on it opens with one empty unbounded treasury Bin
named `money`, and that **every other shipped file gives the treasury none**. 1,581 green, **no
golden baseline moved**.

⚠ **Its content is three lines and that is the decision rather than a stub.** `World.FitTreasury`
walks the Ruleset's Resources at world creation and fits the treasury a Bin per **conserved** one
([`adr/0116`](../docs/adr/0116-the-treasury-opens-empty-and-a-founding-balance-is-a-ratio-this-milestone-holds-neither-side-of.md)),
so a `[[resource]]` block is the **entire vocabulary a Ruleset has** for making the treasury real:
no table names it, no kind declares it, nothing tunes it. A three-line block is the difference
between a treasury with a Bin in it and a treasury with none, and that is asserted in both
directions rather than in one — a guard covering one of two near-identical files is worse than no
guard, which is 5a-bis's finding and the reason the negative runs over all five older files.

⚠ **It declares no Bin and writes no money term, and the reason is the task's sharpest sentence.**
A money term needs a Bin, a Bin needs an owner, and
[`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)
settled **two days before this file was written** that a Building never holds money — a Building
holds a *list* of Occupants, so money on it is an average wearing a total. The two actors that may
hold money are the Household and the Business, and `BinOwnerKind` names both and **throws on both**;
they are tasks 5 and 4b. So the **one money-Bin door the loader has** is `[[building]] bins`, whose
money branch refuses an authored capacity in a sentence written for exactly this case — *"a finite
one would mean an actor too full of money to be paid"* — and **that door now opens onto a shape a
decision refuses**. ***A Resource declaration is the whole of what a shipped Ruleset can say about
money today, because every other spelling names an owner that does not exist.***

⚠ **This task's brief said the unit is spent here and it is not.**
[`adr/0115`](../docs/adr/0115-moneys-unit-is-fixed-by-the-smallest-fraction-the-design-multiplies-by.md)
fixes money's unit by the smallest fraction the design multiplies by, and that is a discipline about
**amounts**. This file writes no amount, so there is nothing for it to bind on: the first money
quantity in a shipped Ruleset is the tax rate in **task 5**. ***A scale discipline binds on the first
number, not on the first declaration*** — and the brief conflated the two because a Resource
declaration is where a *unit key* would have gone, in the design where there is one. There is not
(`adr/0115`), which is the same finding arriving from the other side.

⚠ **The cost is measured rather than argued.** Against `minimal.toml` at 2,048 Ticks on the
4,000-Citizen fixture, **every census row is identical but one**, and that one moves by exactly one
in every column — `bin` live **962 → 963**, slots **962 → 963** — with the State Hash moving from
the first sample because `BinTable.Rows` has one more row in it. That is `adr/0094`'s rescale
precedent again: a byte-identical city with only the hash moving.

⚠ **The Resource is named `money`, after its family, and that is `sundries`' argument on a second
axis.** `minimal.toml` picks *"a word for goods too unimportant to name"* so that nothing in it reads
as a decision about content; a currency is different in kind, because its name is on every panel a
player sees. Naming one here would be the file making the one decision its first line says it makes
none of.

*Original scoping text follows.*

### ~~Task 2's brief~~

**No shipped Ruleset declares one.** Follow the precedent the last three demonstration files set —
*the same file with one thing changed*, with the header carrying why — rather than editing
`minimal.toml`, whose own header says it models no city. This is where decision 2's unit is spent.

### Task 3 — refusal 4 is relaxed to exactly the extent `global` widens it — ✅ **DONE 2026-08-18, and nothing was relaxed**

**Refusal 4 needed no change, and the task's real content turned out to be a refusal it did not have.**
`RefuseUnbalancedMoney` sums a Rule's money terms **across every scope**, so `local` money in and
`global` money out of the same amount balances and always passed. ***A refusal that counts a sum is
indifferent to how the sum is spread across scopes, so widening the scope set could not have moved
it.*** The proof was already in the tree and green:
`RulesetLoaderTests.A_rule_moving_money_between_two_named_scopes_is_accepted` has asserted exactly
this milestone's transfer since slice 7, **in the same file as the doc-comment the brief read**. And
that doc-comment was never wrong — it says the guard over-refuses **a wage and an import payment**,
both of which are *unbalanced* and both of which are still refused and still unwriteable. ⚠ **What was
wrong is this brief**, which read *refuses more than it needs to* as covering its own transfer:
`adr/0093` exactly, and wrong about **reach**, which is that ADR's own stated tell.

⚠ **What `global` widens is what the loader must CHECK, not what it must stop refusing.** So the task
built one refusal instead of removing one: **a `global` term naming a non-conserved Resource is
refused at load**. That is `02 §4.3`'s own sentence — *"`global` names the treasury… **that is the
shape the loader accepts**"* — acquiring an implementation. The loader accepted every other shape for
six slices and nothing noticed, because `Scope.Global` threw in the Rule engine first. ***A scope that
throws is a scope nothing has to validate***, and task 1 removed the throw.

⚠ **Only the family half of that sentence is enforced, and the other half would have broken task 5.**
It also says *local money out, global money in* — one direction — and the mechanism has two: a Policy
paying **out** of the treasury is a `global` **input**, which `02 §4.2` asks for by name. ***A sentence
describing the first use of a shape is not a specification of the shape.***

⚠ **`pool` is deliberately not refused beside it, and the asymmetry is the argument.** `pool` is
*unbuilt* (`adr/0070`) and arrives with the District Pool, so refusing it would refuse a file that is
going to be legal; the Rule engine's named hole is the right instrument for an absence with a date on
it. `global`-on-a-Good is not early — no Bin is fitted for it in any world this design describes.
**Both directions are asserted**, one line apart in the suite, because the next person to widen one
will read the other.

⚠ **`02 §4.3` bundled the two under one reason and the reason was `pool`'s.** *"Neither is refused at
load — deliberately, because a Ruleset naming a Pool is well-formed and merely early."* That is true
of `pool` and false of `global` the moment `global` is built. ***An `and` in a consequence is two
consequences*** (session F), and the tell is one reason offered for two holes. Amended there, together
with *"Only `local` and `map` are implemented"* and *"No transfer executes today, since `global`
throws"*, both now false.

⚠ **The refusal count of record was stale by 36, and the recount is the task's largest finding.**
Three documents carried it at *twenty-two at load and a twenty-third on reload*, corrected to that on
2026-08-11. A walk of the loader puts it at **58** before this milestone's own, so **59 at load and a
sixtieth on reload**. **Seventeen of the thirty-six sit in `[[rule]]`, `[[resource]]` and
`[[building]]` — sections that all existed on the day of the correction** — so the number was an
undercount of its own scope when it was last written down: ***a count corrected by adding what you
remember adding is still a count nobody has taken.*** Repaired in two moves: `adr/0048` keeps the
number and gains the enumeration, `adr/0015` and `plans/0003` now **cite** and state none
(***the cheapest way to stop two copies drifting is to have one copy***), and — because the single
copy went stale anyway — **`RefusalCountTests` holds it to the build**, counting `RulesetLoader.cs`'s
`Refuse(` call sites. ⚠ **It is the corpus's first document-to-*code* check**; `plans/0012`'s proposed
**check 10** named that direction and is still unbuilt. It holds the **site** count, which is a fact,
not the semantic subset, which is a judgement — ***the checkable part of a claim about the build is
the part that is counted.***

*Original scoping text follows.*

### ~~Task 3's brief~~

⚠ **CORRECTED 2026-08-18 while writing task 2: the loader does *not* refuse this milestone's
transfer.** `RefuseUnbalancedMoney` (`RulesetLoader.cs:1403-1416`) sums a Rule's money terms
**across every scope**, so `local` money in and `global` money out of the same amount balances and
passes today. What the guard refuses is what the doc-comment's own example names — a **wage** and an
**import payment**, both of which are *unbalanced* explicit money terms because their counterparty
cannot be spelt. So this task is a **stale sentence** rather than a stale guard, and the arithmetic
underneath it was right all along. ***A doc-comment saying a guard refuses more than it needs to is
a claim about the guard's reach, and the guard is the thing to open*** (`adr/0093`). Read the rest
of this row with that correction in front of it.

⚠ **The loader currently refuses the transfer this milestone is built to write, and says so.**
`RulesetLoader.cs:1372-1375`: *"**It refuses more than it needs to today, deliberately.** A wage… and
an import payment both have real counterparties that no scope can currently name, so both would be
refused."* Once `global` names one, the over-refusal is no longer deliberate — it is a stale guard.

⚠ **Relaxing it moves the refusal count of record, which three documents carry and which has drifted
once already**: `adr/0048` names it, `adr/0015` names it, and `adr/0048` records that the number went
out of step before. Update all three in the same commit, or the next reader re-derives the wrong one.

### Task 4 — the conservation invariant `adr/0031` promises — ✅ **DONE 2026-08-19**

**`Invariant.MoneyIsConserved` is in the end-of-run tier, and it is an exact equality against a saved
anchor.** Every live Household's `Money` and `Savings`, plus every live Bin holding a conserved
Resource, against **`MoneySupplyTable.Issued`** — a new one-row saved table, on `ClockTable`'s
precedent, appended to `World._tables`. `MoneyIsRepresentable` is kept and is registered **first**, so a
sum that has run away reports as unrepresentable rather than as unconserved; that ordering is a test
rather than a property of the list.

⚠ **The task's shape was decided by a fact the brief could not have known: *nothing in the build writes
a Household's money at all*.** The only writers in the tree were **six test fixtures poking the
columns**, so *the Households' opening money* is a quantity the simulation has no door for. That makes
the anchor unrecoverable at check time — and recovering it by summing the balances is precisely task
1's finding, ***an invariant that recomputes the producer's own expression checks that the write
happened and never what was written***. So the task built the door as well as the check:
**`World.Endow` is the one site money enters a world by**, and it moves the balance and the supply of
record together, on `World.Deposit`'s argument (*there is no second spelling in which the other half can
be forgotten*). ***A conservation invariant is a claim about doors, so it cannot be built before the
doors can be counted.***

⚠ **The anchor is deliberately not a column on `TreasuryTable`, and `01 §6` is what decides it.** That
document separates the two by name where it prices a trade deficit — *"a different bill — the money
supply, not the treasury"* — and the separation is structural rather than editorial: at the founding
the treasury holds **nothing** (`adr/0116`) and the whole supply is in Households. There is a mechanical
reason underneath the editorial one: a `Saved` column has to be in the hash composition, and task 1
kept `TreasuryTable` **out** of `World._tables` on 5a's finding that a wholly-derived table folds its
allocation history rather than any state. ***A table excluded from the composition cannot acquire a
saved column without re-opening the exclusion.***

⚠ **It fired on the golden fixture within a minute of being registered, and what it found is a design
question nobody had asked.** `GoldenFixtures.Build()` retires household 5 to take the free list off its
initial value, and `World.DestroyHousehold` frees the row with whatever is in those two columns — so an
endowed fixture burnt **14,185** at the founding. **Where a departing Household's balance should go is
*undesigned***: the Outside Connection is money's only sink (`CONTEXT.md` → Money) and it is milestone
**11**, so there is no legitimate destination today. Filed to [`plans/0002`](0002-open-questions.md) §C
rather than answered here, and the fixture endows the other seven with the reason written beside the
`if`.

⚠ **A write-site refusal was considered and rejected on an argument worth keeping.** `02 §10` prefers
the cheapest tier, and *a Household may not be freed holding money* is an `O(1)` check at the free
site. It would not have worked: **the refusal is satisfiable by zeroing the column first**, which
destroys the money by a second spelling the free site cannot see. ***A guard at the site where a
quantity disappears can only ever check that it disappeared tidily; the whole-world walk is what checks
that it did not disappear at all.***

⚠ **The Bin side walks every owner rather than the treasury's list, and that is a diagnosis decision.**
Conservation is a claim about totals and not about placement. A Building holding money is refused by
`adr/0113` — and ⚠ **the loader does not enforce that**: a `[[building]]` may declare a money Bin
today, and the only thing refused is a `capacity` key on it (`RulesetLoader.cs:1275-1290`). Counting
such a Bin here would report it as money **destroyed**, which is the wrong diagnosis and would go on
being wrong after the placement rule was enforced. The missing refusal is filed rather than smuggled in
— it belongs beside task 4b, which is the milestone that gives a Business somewhere legitimate to hold
a balance.

⚠ **It is correct and temporarily trivial, and that is the good side of 5b's distinction.** With no
production writer, every world the simulation builds on its own is founded on zero and both sides of
the equality are zero with it. Nothing about the assertion's shape is wrong and no edit is needed to
make it load-bearing — task 5's tax circuit is what makes it so. The transfer test (`a Household paying
the treasury conserves money`) is what stops that triviality from being vacuous: it moves money on the
moneyed Ruleset and the check stays quiet, which is the only assertion here that could distinguish a
real walk from an equality on a constant.

**Cost:** one new saved table, so the State Hash moves and **`world-hash.txt` and `session-trace.txt`
re-record** (`0xAEA76319A8CB1A06`; neither Ruleset content hash moves, so the two literals inside
`session.borough` are untouched). **1,592 tests green**, seven of them new.

*Original scoping text follows.*

### ~~Task 4's brief~~

*Nothing is created or destroyed except at the gate*, as a whole-world end-of-run check, with
`MoneyIsRepresentable`'s overflow half kept rather than replaced — they catch different failures and
`02 §10` sorts invariants by frequency, so both sit in the end-of-run tier at no per-Tick cost.

**In this milestone there is no gate**, so the check is an equality against a constant rather than a
sum with a flow term. Write it that way and let milestone 11 add the term; ***an assertion that is
correct and temporarily strict*** is 5b's distinction and the good side of it.

⚠ **The constant is the Households' opening money alone** ([`adr/0116`](../docs/adr/0116-the-treasury-opens-empty-and-a-founding-balance-is-a-ratio-this-milestone-holds-neither-side-of.md)): the treasury opens **empty** and contributes nothing to it. **Decision 3 blocked this task and the brief said it blocked only task 9** — an anchor is a founding-balance question — and it resolves to a *zero term* rather than to a value, which makes the equality tighter rather than looser.

### Task 4b — `BusinessTable`, and the second Occupant kind the build has never had — ✅ **DONE 2026-08-19**

**`BusinessTable` exists with a Building handle, a `Money` column and a `BuildingNext` link;
`BuildingTable` gains `BusinessHead`/`BusinessTail`; `World.CreateBusiness` is the door.** The
conservation walk gained its third place and nothing else changed, **which is the property a
balance-on-the-actor has and a Bin-on-the-container would not**.

⚠ **The demolition path is where the decision actually was, and the brief did not mention it.** A
Building carries three lists now and `DestroyBuilding` treats them differently: Households are
**evicted to the Unplaced Pool** (`adr/0054`), workers are **unlisted and keep a severed handle**, and
a Business takes the **worker** branch — unlisted, row kept, premises handle severed, balance intact.
The Household branch was unavailable for a reason that is about money rather than about occupancy:
*there is no pool for a Business, and freeing the row would take its balance out of the world through
a demolition*, which is exactly the hole `adr/0024` exists to close and which task 4's invariant would
have reported far from the cause. ⚠ **It leaves a row nothing will ever free**, which is `adr/0006`'s
shape — unreachable today because no pass creates a Business, and a leak the moment milestone **13**
places one. Filed to [`plans/0002`](0002-open-questions.md) §C, where it **widened the existing row
rather than opening a second**: ***two actors were given different eviction rules by two milestones, on
the strength of what each happened to have somewhere to go to.***

⚠ **`Clear` was written first and is wrong, and the file that warned about it is the file it was written
in.** `IndexList.Clear` drops the two heads without touching any element's `next`, which is correct only
when the elements are about to be freed or re-linked — and `DestroyBuilding`'s **worker** branch says so
in a comment four lines above where the Business branch went. A cleared list leaves each Business
pointing at its old sibling, in no list, which `RebuildDerived` would never produce: **a
`(derived AND rebuilt)` disagreement no hash can see**, because a derived column folds into nothing.
Drained instead, and `The_maintained_business_list_is_what_a_rebuild_produces` puts demolition in the
fixture so the two paths are compared on the one operation that unlinks.

⚠ **Three guards failed on the day the table was declared, and all three failed for one reason: no
fixture held a Business.** `DerivedRebuildAuditTests` (a derived column no world populates),
`FactorioTests.Every_saved_column_reaches_the_file_and_no_other_one_does` (five saved columns
unreachable, because a corruption test cannot speak about an empty table), and
`SaveHashTests.A_flipped_byte_in_the_body_is_refused_by_the_load`. ***A table with no production
writer needs a fixture named for it, or its columns are carried by the format and checked by
nothing*** — and the repair is a Business in `GoldenFixtures.Build()`, funded by a **transfer** from a
Household rather than by a second endowment, which also puts all three of the conservation walk's
places in the committed baseline at once. **Two of them**, sharing one Building, because
`business.building_next` is zero for a list of one.

⚠ **The third failure is the sharpest and it is not about Businesses.** That test flips `bytes[^9]` —
*"near the end of the file on purpose"*, to miss the `id`/`generation`/`free_next` walk of the first
table — and appending an empty `business` table put four allocator scalars at the end of the file. The
flip landed in the free list, `Rows.Restore`'s structural walk refused it, and **the load still
refused**, so only the message assertion noticed that a different mechanism had taken over. ***A byte
index counted from the end of a file is a claim about which table is last***, and `World._tables`'
own remark calls appending *"the only edit to this list that does not move rows relative to one
another"* — which is true of the composition and false of the file's tail.

⚠ **The first repair was a fixture and it would have re-broken on the next appended table** — put a
Business in that world so the last table has data, and the flip lands in a data column again *today*.
`bytes[^9]` still meant *the ninth byte from the end*, so milestone 11's first table would have moved the
mechanism a second time, and the paragraph above would have been the thing somebody read on the way to
adding a row to *that* table instead. ***An annotation that names a trap exactly is not a removal of
it***, and the tell is that it has to be read at the moment of a change nobody is thinking about this
test during. **Corrected the same day**, flagged by the milestone 8 session — whose subject area the save
format is, and which had swept the suite and established the class has exactly one member, the
distinguishing property being not *counts from the end* but *counts from the end **and** must land in a
particular kind of byte*. `SaveHashTests.ByteIn(world, "household", "money", slot: 0)` derives the offset
from the field declaration in force — per table, `SaveFile.ScalarBytes` then each saved column at
`BytesPerRow × SlotCount`, which is what `adr/0086` makes the format — and **refuses an allocator column
by name** rather than trusting a position to miss one. Appending a table now cannot reach it, adding a
column moves it correctly, and *renaming* `household.money` fails loudly where it used to fail silently.
The Business stays in the fixture for a **different** reason, which is the durable one:
`business.building` is a saved **severable** handle column, and a fold over a copy differs from a fold
over a live column exactly there. ***A trap removed and a fixture kept are two changes, and the fixture
needed a reason that survives the removal.*** One smaller thing fell out: `SaveFile.ScalarBytes` was
restated privately in `SaveHash` and about to be restated a third time in the test, so it is **internal
now with one home** — `plans/0012` *Cause 1* in code, caught before the third copy rather than after it.

**Cost:** one new table with two saved columns and three derived ones, so **`world-hash.txt` and
`session-trace.txt` re-record** (`0x47E1A7AAEB34043E`); neither Ruleset content hash moves.
`DerivedRebuildAuditTests`' machine count goes **32 → 35**. **1,598 tests green**, six of them new.
`CONTEXT.md` → Occupant now states the two-list shape and the two eviction rules.

*Original scoping text follows.*

### ~~Task 4b's brief~~

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

### Task 4c — a balance is a Bin, and decision 6 had no task — ✅ **DONE 2026-08-19**

`adr/0114`'s second half, built. `HouseholdTable.Money`, `HouseholdTable.Savings` and task 4b's
`BusinessTable.Money` are **deleted**; each actor carries one saved `HandleColumn<Bin>` named
`balance`; `World.FitBalances` opens one per actor for the conserved Resource the Ruleset in force
declares, on `FitTreasury`'s trigger and for its reason; and all five shipped Rulesets gained a
three-line `[[resource]]` money block.

⚠ **THE MILESTONE'S LARGEST DECISION HAD NO TASK, AND TWO TASKS BUILT AGAINST THE THING IT
DELETED.** Decision 6 is settled — *"`HouseholdTable.Money` and `.Savings` are both deleted"* — and
no task in this document claimed it. Task 1 built the cheaper half and said so (*"`adr/0114`'s
wording — **gains an owner kind** — is the cheaper half and it is the one built"*), leaving
`BinOwnerKind.Household` and `.Business` throwing by name. Task 5's brief says *"needs decisions 1
and 5"* and does not name 6, though its whole content — a Rule drawing a tax from a Household — is
unbuildable without it. **A settled decision with no owner is invisible to a reading of the task
list**, which is `plans/0012` *Cause 1* on the axis the board is normally the guard for: the decision
was recorded correctly, in the right document, and nothing scheduled it.

⚠ **The cause is an ordering inside one sitting, and the build was right where the prose was wrong.**
Task 4b's brief is *"NEW 2026-08-18, from decision 1"* and specifies a `Money` **column** *"on
`HouseholdTable.Money`'s precedent"*. Decision 6 was **opened by** settling decision 1 and landed
after that brief was written — so the brief cites a precedent the same document retires nine sections
above it, and nothing recomputed the brief. Meanwhile `Rules.cs:57`, written in **task 1** after
decision 6 settled, says `BinOwnerKind.Business` is *"declared and not yet owned — plans/0033 task
4b"*, and `World.RebuildDerived`'s throw says *"a Household's balance arrives with task 5 and a
Business's with task 4b, **each with the list head its own table has to declare**"*. ***Two sites in
one repository disagreed about what a task was, and the one that was right is the one written after
the decision rather than before it.*** `adr/0093` assumes prose is wrong about code; here the code
was the record and the plan was the drift.

**How a Bin names an actor — settled with the user in the room, and `adr/0114` underdetermined it.**
That ADR's hash consequence (*"two owners of different kinds may share an id, so the kind is part of
the value"*) only makes sense for **one** column holding handles into several tables, which is not
what task 1 built: `BinTable.Owner` stayed a `HandleColumn<Building>` with the kind folded beside it,
justified on the ground that *"the treasury has no owner row to point at in any case"*. ⚠ **That
ground holds for a singleton and expires the moment a non-singleton owner arrives** — a Household is
a real row. Three shapes were weighed; the chosen one puts a saved `HandleColumn<Bin>` **on the
actor**, so `BinTable` is untouched, `RebuildDerived` has nothing to rebuild for either new kind, and
the lookup is O(1) rather than a list walk. The two rejected: two more typed handle columns on
`BinTable` (symmetric, and ~16 bytes on every Bin in the world including the ~500k Building Bins that
never use either — ~13 MB at a million Citizens), and a polymorphic handle column (what the ADR's
sentence implies; needs a new column type whose fold and dangling check dispatch over the kind, and it
breaks `SaveHash.TargetsOf`, which resolves exactly one target table per handle column).
***The asymmetry with a Building is cardinality and not carelessness***: a Building holds many Bins
because its kind declares many Resources, and an actor holds one because money is one Resource.
`World.TryMoneyResource` **throws** on a second conserved Resource rather than silently balancing on
the first — which would give every actor a balance in one currency and none in the other, while every
conservation sum still added up, because money in a Resource nobody can hold is money nothing can
lose.

✅ **The conservation walk got *smaller*, which is the strongest evidence the decision is right.**
`MoneyIsConserved` and `MoneyIsRepresentable` enumerated the actors — two Household columns, a
Business column, then the conserved Bins — and each new actor owed them an edit, which task 4b duly
paid. Both now share `TrySumBalances`, one owner-agnostic walk over live Bins filtered on
`Rules.IsConserved`. ***An enumeration of owners is a list somebody has to remember to extend; a
filter on the Resource is not.*** Shared rather than written twice, so the two cannot disagree about
where money lives — if one walked a place the other did not, an overflow there would be reported
under the other's name, which is the exact confusion their registration order exists to prevent.

⚠ **A LIVE SAVE/LOAD DEFECT FELL OUT, AND IT HAD BEEN THERE SINCE TASK 1.** A Bin's `capacity` is
`(derived AND rebuilt)`, and `RebuildCapacities` **skipped** every non-Building Bin — its comment
saying the treasury's unbounded ceiling *"is set once at creation and left"*. A load creates a Bin
through `Rows.Restore` and never through `CreateTreasuryBin`, so *at creation* never happens on that
path: **a reloaded treasury would have come back with a ceiling of zero**, which is a treasury every
transfer into it fails against — precisely the outcome the comment named as its reason for skipping.
It was invisible because **no fixture in the rebuild audit had a treasury Bin**: `Ruleset.Empty` names
no money, so `FitTreasury` made none, and it surfaced within a minute of the golden fixture gaining a
currency. ***A derived column with one write site outside the rebuild is a column that does not
survive a load, and a fixture that never creates one cannot report it.*** `adr/0093` on the
**lifecycle** axis rather than the trigger axis: the comment was right about where the number comes
from and wrong about when it is written. The ceiling now derives from the **Resource** (`04 §2`:
*"Money is a Resource too, and its Bin is unbounded"*), so no owner is resolved for it, and a
non-Building Bin holding a non-conserved Resource **throws** rather than defaulting to a zero ceiling.

⚠ **Money became a property of the Ruleset, and that reached further than the code.** A Bin exists
only for a declared Resource, so a Household on a moneyless file cannot hold money **at all** — where
a column held it whatever the file said. Four fixtures broke on exactly that and each is a statement
now rather than an accident: `OccupancyTests` and `UnplacedPoolTests` name money in their own
Rulesets because they endow somebody, and `StateHashTests`, `InvariantTierTests` and `GoldenFixtures`
share `TestRulesets.MoneyOnly`. ***Making a quantity conditional on the Ruleset turns every fixture's
Ruleset into a statement about what that fixture can test.*** ⚠ **The obvious repair for the golden
world was wrong and the suite said so**: putting it on the session's `minimal.toml` — which would
have made the two golden artefacts agree about the Ruleset for the first time — turns on the
`[roads]` table, so the world generates a Road Graph, `Invariant.VacantLotHasFrontage` stops
returning early on a world with no Streets, and Lot 4 fails. That world's Lots were placed by hand
before this project had roads. ***A Ruleset is not a bag of settings a fixture can take one of***:
loading a bigger one to obtain a small property turns on every mechanism in between.

**All five shipped Rulesets name money, not just the two the golden pair needs.** Each of
`severance.toml`, `congested.toml` and `diagnosed.toml` says in its own header that it is
*minimal.toml with N changes*, and that sentence is what keeps them auditable — so leaving them
moneyless would have made three headers false to save duplicating a block in files that are already
whole copies by construction. ⚠ **`monetised.toml` is thereby redundant and is NOT deleted in this
commit**: it carries an uncommitted edit belonging to a concurrent session, and its 74-line header —
the survey finding that opened this milestone, and the *why it declares no Bin* half — has been
migrated into `minimal.toml` rather than dropped. ***Deleting a Ruleset deletes an argument, not a
duplicate***, which is the milestone 8 session's correction to a claim of mine that the two files
would become byte-identical: on **content** they differ by three lines and on **text** by 78, and
`RulesetFile.HashOf` treats comments as content by design. `TreasuryFromAFileTests` is repointed at
all five and its negative half — the one that makes the positive mean anything — moved to a Ruleset
built in code, where it now asserts **both** ends of `adr/0114`: no treasury Bin *and* no actor
balance.

**Two smaller things.** `World.Endow` takes one amount where it took two, and **refuses** a Household
with no balance rather than founding one silently — ***a door that creates the thing it was asked to
fill cannot report that the request was wrong***. And it deposits through `World.Deposit` rather than
writing the level, because founding a balance is exactly the arrival a waiter is waiting for; a
transfer is now two Bin writes summing to zero, and **both drain a wait list**, which is the property
the pair of column writes did not have and the whole reason the Rule engine can fail on money at all.

**Cost:** three saved columns deleted and two added, so **all three baselines re-record** —
`world-hash.txt` `0x47E1A7AAEB34043E` → **`0x70666EA6FD05EBC6`**, and **both Ruleset content hashes
move**, which means the two literals inside `session.borough` had to be corrected before the trace
could be regenerated at all. **1,602 tests green.** `CONTEXT.md` → Bin, Eviction and Occupant carry
the new shape.

### Task 5 — a Policy is the Sweep family's second member — ✅ **DONE 2026-08-19**

`02 §4.2` built. A `[[policy]]` triggers on an interval, sweeps a **whole population** and moves money
between one Bin and another. `PolicyEngine` runs in Tick phase 6 **ahead of** placement, employment and
the Zone Rules — a fifth engine and the first to run in front of the other three, because a Policy
*changes what an actor holds* and the three passes behind it should respond to the city it just
changed. That is phase 5 → phase 6's own argument one level in, and **nothing downstream reads a
balance yet**, which is precisely why the ordering is stated now rather than discovered later: `02 §1.1`
calls the phase order the determinism contract, and a contract is cheapest to write before anybody
relies on it.

**What shipped besides the engine.** `PolicySubject` declaring four populations and building one;
`Readouts.Balance` and a new `ReadoutScope`; `[households] opening_balance_min/max`, read by
`SyntheticCity` through `World.Endow` so the issuance lands in `MoneySupply.Issued` and conservation
stays exactly checkable **across** it rather than in spite of it; two `purpose_tag`s; `PolicyActivity`,
the **eighth** Census family, printed by `--census` on the day it was built (5b-bis task 6's finding
taken as an instruction: *a Census family whose only reader is the suite is a family no operator can
see*); and `rulesets/taxed.toml`, the **seventh** shipped Ruleset, `minimal.toml` verbatim plus three
appended tables. **1,633 tests green and no baseline moves** — the golden session runs on
`minimal.toml`, which states no `[[policy]]`.

⚠ **THE LEVY CANNOT FAIL, AND THAT IS A PROPERTY OF THE READOUT RATHER THAN OF THE FIXTURE.** A derived
apply count taken off the same Bin the term then draws from can **never overdraw**, because `n` is a
fraction of what is there. ***A Rule whose apply count is read off the Bin it spends is unfailable by
construction*** — so a levy on holdings never joins a wait list, never reports bankruptcy, and touches
none of the failure surface `adr/0114` built. It is the transfer in the **other** direction, paid out
of a treasury that can empty, that exercises any of it. This was not foreseen: the brief describes both
ends as *"`local`↔`global` transfers, both Sweep Rules"*, symmetric in every sentence, and they are
asymmetric in the one property the milestone is about. It is also the reason `Readouts.Balance` needed
a paragraph rather than a line — a balance is **both** a thing a Rule spends and a thing a Rule
consults, which is the one line `Readout`'s own summary draws.

⚠ **A Readout gained a *scope*, and the loader gained a refusal that is new in kind.** Every Readout
before `balance` hangs off a **Building**, because the only consumer was a Bin Rule and a Bin Rule is
attached to one. A Policy sweeps **Households**, so the entity a Readout hangs off is now part of its
declaration (`Readouts.ScopeOf`) rather than a property of its consumer. What that buys is that
`occupancy` in a `[[policy]]` and `balance` in a `[[rule]]` are **both real names with no row to read
them from** — a mismatch the loader refuses by name, which is `adr/0048`'s division of labour exactly:
*the loader refuses an unusable name, the interpreter refuses an unusable id*. ⚠ **The file predicted
this would need a wider switch and it needed a second entry point**, which is the cheaper answer and
the one a reading of the existing enum would not have produced.

⚠ **`[[policy]]` moves money and gets NO money-balance refusal, and the reason is worth more than the
count.** Refusal 4 exists because a `[[rule]]`'s money terms are two free-form lists that have to be
walked to know whether they balance. A Policy states a `from` and a `to`, so the same quantity leaves
one Bin and enters the other **by construction**. ***A transfer written as a direction cannot leak; one
written as two lists has to be checked.*** So a whole new section arrived and the refusal count went up
by **less than the surface did** — eleven, taking `adr/0048`'s count of record to **seventy** at load
and a seventy-first on reload, with the enumeration in that ADR. **A shape can be validated by being
unable to express the error**, which is a cheaper guard than one that detects it.

**The rotation is drawn rather than strided, and that is a number avoided rather than a number chosen.**
`02 §4.2` asks for the scan start to move per trigger so that a payer running dry produces *a gradient
across the population rather than a permanent boundary*. A fixed stride meets the letter and adds a
second hash-bearing number that can **resonate with the population size** — a stride sharing a factor
with the row count visits a subset for ever, which is the failure the rotation exists to prevent,
reintroduced by the mechanism meant to fix it. A draw needs no number and cannot resonate, so
`adr/0052` has nothing to ratify. ⚠ **It is keyed on the Policy's *index***, which is the one place in
`PurposeTag` where the entity coordinate is not an entity: two Policies triggering on one Tick and
sharing a start would tax and pay the same Households first for ever, so **who the treasury runs out on
would correlate with who was taxed hardest** — `02 §8` rule 5's bias arriving *between* two Rules
rather than inside one.

**Exhaustion stops the sweep; a member who cannot pay does not.** The two look alike at the call site
and are opposite facts. A treasury that cannot cover the next payment cannot cover any of the ones
behind it, so continuing is a walk that pays nobody — and `02 §4.2` asks for *where it stopped*, which
only exists if it stops. A Household that cannot cover a levy says nothing about the next Household,
and **nobody is entitled to be taxed**. They are separate counters for that reason (`sweeps run dry`
against `could not pay`), and `exhausted` counts **sweeps** where everything else counts **members**,
so the row says *sweeps* in its own name: a reader who sums that column gets a number about nothing.

⚠ **TWO TEST DEFECTS, AND THEY ARE THE SAME MISTAKE — READING A QUANTITY AT THE WRONG MOMENT.** Both
passed review as prose and failed as arithmetic.

`A_percentage_that_floors_to_zero...` classified balances **after** the sweep and said so in a comment
defending it. A Household holding 10 pays 1 and lands on 9, so the after-picture counts it among the
floored when the levy floored nothing on it — 16 counted against 18 classified. The levy floors on the
balance it was **handed**. ***A balance after a transfer cannot say what the transfer saw***, and the
comment is the tell: it stated the reading it wanted rather than the one the mechanism supports.

`Two_triggers_of_an_exhausting_sweep...` seeded the treasury **once**, so the second sweep met an
**empty** treasury rather than a **short** one. It paid nobody, and a sweep that pays nobody says
nothing about the scan order the test exists to assert — the assertion would have failed for a reason
unrelated to the rotation, which is worse than not having it. Seeded before each trigger, both sweeps
are short and the rotation is readable. ***Exhausted and empty are different states, and only the first
is what "pays whom it reaches and reports where it stopped" is about.***

**The circuit's numbers are derived except one, and the exception is chosen to make a branch reachable
rather than to model anything.** `interval = 2048` is `Ticks.PerDay` on `adr/0059`'s argument;
`percent = 10` is `adr/0115`'s own fraction, found in both existing sites (`02 §5.6`'s tatonnement
clamp and `02 §4.1`'s apply count), which makes this file's flooring threshold **exactly** that ADR's
floor of ten smallest units — so the counter reads *the discipline* rather than a number chosen beside
it. The rebate's `amount = 100` is the one free number: at a mean opening balance of 500 the levy takes
about 50 a Household a Day against 100 owed, so the treasury runs dry about **halfway through every
sweep**. ⚠ **A city that paid everybody would demonstrate less, not more** — the exhaustion branch would
never run and the rotation would be decoration.

✅ **A consequence nobody claimed in advance, and it runs in the helpful direction.** A proportional
levy with a flat rebate drives every balance toward the same figure: take is `0.1b` and the payment is
flat, so a Household above the crossing point loses and one below it gains. The total never moves —
money is conserved and this file has no gate — so what changes is only the **spread**. That means the
rebate lifts the poorest **out of** the flooring band, so `floored to zero` is largest on the first
sweeps and falls. ***A transfer cures the rounding artefact a levy produces***, which is visible in
`--census` in a few Days and was not predicted by `adr/0115`, this brief, or the file's own author until
the numbers were written down.

**Two smaller things.** `PurposeTag.OpeningBalance` is consumed **once** where `CarOwnership` is
re-derived on every read, and the difference is the mechanism rather than the tag: ownership is a
standing property, so a retuned rate moves a nested set; an endowment is **issued** — it enters a Bin,
is spent, and cannot be recovered by redrawing. What that costs is that the band is not retroactive on
reload, which is correct, because **money already issued is money in the world**. And the sweep is a
sweep rather than a sample **on semantics, not on cost**: `02 §4.2` makes a transfer an *entitlement*,
so paying a random subset of the eligible is a defect rather than a behaviour model — anything reaching
for a sample to make this affordable has confused the two. It is affordable anyway, at about ten
integer comparisons a Tick amortised for eight Policies over ten thousand Households.

### ~~Task 5's brief~~

The circuit that needs neither a price nor a wage: **a Policy sweeps a tax from Households into the
treasury, and a transfer pays it back out.** Both ends are `local`↔`global` transfers, both are Sweep
Rules, and `02 §4.2` supplies the two properties that make it correct — *"a transfer is not a
behaviour, it is an **entitlement** — paying a random subset of the eligible would be a bug"*, and a
Policy paying out of a treasury that runs dry *"pays whom it reaches and reports where it stopped."*
⚠ **The rotation that makes exhaustion fair is specific to this case and does not generalise** —
`02 §4.2` narrows it to *because a treasury is exhausted*, and
[`adr/0055`](../docs/adr/0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md) refuses it for Zone Rules.

⚠ **It also builds the instrument decision 2 left owed** ([`adr/0115`](../docs/adr/0115-moneys-unit-is-fixed-by-the-smallest-fraction-the-design-multiplies-by.md)): a Census count of **percentage applications that floor to zero**. `FloorDiv(readout × percent, 100)` is zero below `100 ÷ percent`, so a coarse money unit makes this tax collect nothing from the poorest and the stated rate from everybody else — a **regressive** outcome nobody chose, in the mechanic `adr/0024` requires the game take no position on. **A loader cannot check it**, because a readout's magnitude is not known at load time, which is why it is a counter and not a refusal. ***A discipline the loader cannot check needs a counter or it is a comment.*** It is the tax circuit's own instrument and belongs here rather than to a later milestone: **this is the first Rule in the project that ever multiplies money by a fraction.**

Needs decisions 1 and 5. ⚠ **And decision 6, which this brief did not name and which had no task at
all** — see **task 4c**, which is that decision built. A Rule cannot draw a tax from a Household whose
balance is a column, so the whole of this task rested on it.

### ~~Task 6 — Upkeep~~ — ✅ **DISCHARGED AT SCOPING 2026-08-18, and it is the written record rather than the mechanism**

Decision 4 returned **defer**, so this task is done before the milestone starts. The deferral is written into [`06`](../docs/06-roadmap.md) — struck from milestone 10's row, added to **12**'s with the three surviving blockers, and the inventory row moved — and into [`adr/0035`](../docs/adr/0035-infrastructure-is-priced-by-what-it-consumes.md), which gains an amendment recording that it names a Rule family and no actor and that its formula disagrees with its own title. **Not left in this file**, which was the condition: *a milestone quietly shedding a row its roadmap assigns it is how milestone 10 came to be two milestones wearing one number.*

### Task 7 — something to look at — ✅ **DONE 2026-08-19**

The circular flow, printed. **Two Census families and a tenth runner mode**, and the split between
them is the task's own sentence made structural: `MetricSource.Money` is six **levels** — supply,
held, treasury, households, businesses, elsewhere — and `MetricSource.MoneyFlow` is two **flows**,
money to the treasury and money out of it. `--money` prints both halves against
`rulesets/taxed.toml`, and `--census` grew two blocks on the day the families were built (5b-bis task
6's finding taken as an instruction again: *a Census family whose only reader is the suite is a family
no operator can see*).

**1,716 tests green, +22, and NO baseline moved.** The Census is an instrument outside the world and
`PolicyEngine`'s two new accumulators are counters, so nothing here is in the State Hash — which is
what makes this the one task of the milestone that could land after the golden re-record without
scheduling anything.

⚠ **TASK 5's SCOPE LINE WAS RIGHT ABOUT THE SPLIT AND WRONG ABOUT WHERE IT GOES, AND THE CORRECTION IS
THE TASK'S SHARPEST SENTENCE.** `PolicyActivity`'s remark left the money magnitude out with an argument
— *"counting here and measuring there keeps this family answering did the Policy run and whom did it
reach"* — and the argument is sound about **commensurability** and wrong about **location**. A count
and an amount are produced by one method on one Tick; draining them separately is two drains a caller
can get one of right, and a caller that forgot the second would fold a whole reading's money into the
next one silently. So they ride together and the **Census** splits them, into families that never share
a block of a report. ***The line that keeps two quantities from being read as one belongs in the
reader, not in the producer*** — a producer that splits them buys nothing a report cannot lose and
costs a drain that can be missed.

⚠ **A NET WOULD HAVE HIDDEN THE ENTIRE CIRCUIT, AND THE SHIPPED FILE IS THE PROOF.** On `taxed.toml`
at 2,000 Citizens over 8,192 Ticks: **140,491 moved to the treasury, 140,400 moved out, and the
treasury ends holding 91.** The gross is fifteen hundred times the net. One netted counter would print
a city that taxes almost nothing; two print a city that taxes heavily and pays it nearly all back, and
those are different cities. ⚠ **These figures are one seed at one population** and are quoted as the
shape of the circuit rather than as properties of it — the levy is a fraction of holdings and the
rebate is flat, so both scale with the city and their *ratio* does not.

**`Elsewhere` is a residue rather than a fourth holder, and it is exercised rather than declared.** It
is `held` less the three named kinds, so it sums by construction and reads zero today because
`adr/0113` says a Building never holds money and there is no fifth owner kind. What it buys is that
the decomposition **cannot silently stop decomposing**: without it, money held somewhere unnamed would
appear in no row at all, and a reader cannot see a row that is not printed. ⚠ **Milestone 6 task 7's
finding is why it has a test that makes it non-zero** — ***a count that reads zero because nothing
exercises it and one that reads zero because nothing writes it are different facts, and a Census cannot
tell them apart*** — so `MoneyLedgerTests` opens a Building-owned money Bin, which is a world this
build does not produce, and watches the residue catch it.

⚠ **A THIRD TEST DEFECT OF THE MILESTONE'S OWN KIND: READING A QUANTITY AT THE WRONG MOMENT.** The
circuit assertion compared a flow's **last reading** against a level's, and they are not comparable —
a flow reading is what happened since the previous reading and the reading **drains** it, so the last
one covers the last interval alone, where a level's last reading covers the whole run. It read −1
against an expected 21 and looked for a moment like a conservation failure. ***A flow and a level do
not answer "what is it now" with the same arithmetic***, and the helper that fixes it carries the
sentence, because the mistake is available to every future reader of this instrument. Task 5 found two
of these and called them one mistake; this is the same mistake in the family the task exists to build.

⚠ **`--money` REFUSES WHERE `--evidence` PRINTS, AND THE DIFFERENCE IS NOT A PREFERENCE.** Milestone 6
chose a *legible absence* — an empty trail under a heading naming the file that fills it — and it works
because an empty trail is **visibly** empty. A balance sheet over a city with no money is not: every
row is zero, `supply == held` is **true**, and the report says money is conserved. ***A conservation
identity that holds vacuously reads exactly like one that holds***, so this absence cannot be made
legible and the input is refused instead. Two refusals: no `[[policy]]` at all, and Policies that move
nothing conserved.

**`TrySumBalances` left `WorldInvariants` and became `MoneyLedger`, and its own sentence gained the
reader it predicted.** That method's remark already said it was *"shared rather than written twice,
because the two checks must not be able to disagree about where money lives"*; a third reader arrived
and the walk moved out rather than being copied. It carries `Representable` and `Overflowed` so the
instrument can decline to believe a saturated figure without **reporting** one — reporting stays
`Invariant.MoneyIsRepresentable`'s, so one bug does not get two names.

**Two smaller things.** `MoneyFlow` is a second flow type rather than a widened `RuleFlow`: `Peak` has
to be a `long` because an amount moved on one Tick has no ceiling the city's size supplies, where a
count of events does — `Money.cs`'s *"income flow at the target population is on the order of 10⁹"* is
exactly one sweep of a million Households compressed into one reading. Widening `RuleFlow` would have
reached every counter family in the Census to serve two members, and the two types being distinct is
itself what stops a magnitude being folded into a family of counts by accident. And the dump's reading
cadence is **derived** — one row per sweep round of the shortest-interval Policy — so no row ever
splits a sweep and `adr/0052` has nothing to ratify.

### ~~Task 7's brief~~

The circular flow, printed: where money is, and what moved. **A balance sheet is a level and a flow at
once**, and `01 §5.1` requires the two money aggregates be reported **separately** — *"a different
bill — the money supply, not the treasury"* — so a picture showing one is a picture that hides the
one the endgame turns on. The Census can carry it: `Series.cs:17` widened a sample to 64 bits
*"because a magnitude is a `Money` or a `Fixed` rather than a count"*, and no `Metric` member has ever
been one.

### Task 8 — Evidence answers the question it currently declines — ✅ **DONE 2026-08-19**

`CitizenEvidence.HouseholdBalance`, a `Money?`, and a finances panel in `--evidence`. **Milestone 6's
assembler earned its scoping claim exactly**: no accumulator, no column, no save-format change — the
assembler reads the Bin the Household already names, and a fact that did not exist in any report
became readable in nine lines of `Evidence.OfCitizen`. **1,722 tests green, +6, and no baseline
moved.**

⚠ **THE CONDITION HAD TWO HALVES AND ONLY ONE WAS PAID BY A WRITER ARRIVING.** The omission read: the
columns had no production writer, *and* **a Household with no money and a Household in a world with no
money read the same**. Tasks 4c and 5 paid the first — a balance is a Bin and the populator endows it.
The second was still true on the day this task started, and no writer could ever have paid it, because
it is not a claim about whether the number exists. It is paid by the **shape**: absent where the world
names no money, present where it does, so a zero is now somebody who has spent everything and nothing
else. ***A writer arriving does not discharge a condition about what a reader can tell apart.***

⚠ **`World.BalanceOf` CONFLATES THE SAME TWO FACTS, DELIBERATELY, AND BOTH ARE RIGHT.** That method
returns zero for a Household with no money Bin and argues it: *"a world with no currency and a
Household with none behave identically at every call site money has."* True of a call site that
**spends** — both refuse the purchase — and false of a **reader**, which is what an assembler is. Both
use the same discriminator (`HouseholdTable.Balance` is a handle and can be unset) and reach opposite
conclusions correctly. ***Two facts a mechanism may treat as one are still two facts to somebody
reading them.*** A test holds the disagreement open, so that tidying the two into agreement fails
rather than passes.

⚠ **THE PANEL FOUND THAT NO SHIPPED RULESET PRODUCES THE ABSENT READING, AND IT FOUND IT BY BEING
WRONG ON SCREEN.** The first run against `diagnosed.toml` was expected to print *this world names no
money* and printed **2,000 balances at zero** instead. **Task 2 put a `family = "money"` Resource in
all seven shipped files**, so every Household in every shipped world holds a money Bin — and six of
the seven then read zero for everybody, because money enters a world only through `[households]
opening_balance_min/max` and only `taxed.toml` states it. So the shipped corpus demonstrates
**destitution** against a **distribution**, and the third reading is exercised by the suite alone.
***A branch no shipped content reaches is still a branch content can reach, and the test is what says
which*** — milestone 6 task 7's `TripFate.Stranded` finding arriving **before** the branch shipped
rather than a milestone later. The panel names which of the two it is showing rather than leaving a
column of zeroes to be read either way.

**Two smaller things.** The absence when the Household handle does not resolve is *not* a second
meaning, because it is not a state: `Invariant.CitizenIsInExactlyOneHousehold` makes it a defect, and
reporting it as a distinct absence would hand a reader a distinction with nothing on the other side.
And it is **one figure rather than two** — `adr/0024`'s reserve is a *behaviour* over one pool rather
than a second account, which is task 4c's finding when it deleted `HouseholdTable.Savings`; a second
member would have to be written by something and nothing writes one.

⚠ **`adr/0070`'s line is closer than it looks and is worth having drawn.** A nullable meaning *nobody
has built this* is the per-clause availability flag that ADR refuses; a nullable meaning *this world
has no currency* is a state of the world, authored in a Ruleset, and true of a world running the
finished game. ***An absence the content can produce is a reading; an absence only the roadmap can
produce is a flag.***

### ~~Task 8's brief~~

`CitizenEvidence.cs:54-62` omits household finances and names the exact condition for putting them
back. This task is that condition being met, and it is milestone **6**'s assembler earning its
scoping claim — that Evidence is an assembler rather than a store, so a new fact becomes readable
without a new accumulator.

### Task 9 — the long acceptance run

100,000+ Ticks. **The money sum is invariant to the unit**, so it is asserted as an exact equality and
not a band — the only exact conservation assertion this project will ever have, and it stops being
available the moment milestone 11 opens the gate. ⚠ **Take it while it is exact.** ~~Needs decision 3.~~ ✅ **Unblocked** — the treasury opens empty, so the sum's opening term is the Households' money and nothing else.

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

Downstream, by `06`'s graph: **11** the Hinterland, **12** Upkeep (moved there 2026-08-18, `adr/0117`),
**13** the price surface, Policy and private capital, **16** the residential choice model, and **19**
through both. ⚠ *This line read "**14** the Hinterland" until 2026-08-18 — pre-reorder numbering,
corrected rather than struck.* `adr/0091`'s
compulsory-purchase price and `01 §6`'s **money supply** trajectory indicator are produced by nothing
until this lands.
