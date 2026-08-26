# 0044 — The purchase, and the Provider that answers it

**Milestone 26.** `06`'s row, [`0003`](0003-build-plan.md)'s ledger entry, and the specification it
inherits from [`0037`](0037-goods-between-buildings-the-district-pool.md) tasks 7–10.

> ⚠ **THIS DOCUMENT SUPERSEDES [`0037`](0037-goods-between-buildings-the-district-pool.md)'s TASKS
> 7–10 AS AN ORDER, AND NOT AS A SPECIFICATION.** Every one of those four entries survives here, with
> its caveats. What changes is **the sequence**, and it changes because four ADRs landed on 2026-08-25
> — [`adr/0163`](../docs/adr/0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md)
> through [`adr/0166`](../docs/adr/0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md)
> — after those entries were written, and one of them puts a subsystem **in front of** the task the
> old order put first. ***Read `0037` for what each piece is; read this for what order they go in.***

---

## Status

🟢 **TASK 7 LANDED 2026-08-26, OUT OF ORDER AND AT THE USER'S INSTRUCTION — A SHOP CAN NOW GO BROKE.**
[`adr/0169`](../docs/adr/0169-a-standing-cost-needs-a-counterparty-so-a-trade-pays-rates-until-there-is-a-supplier-to-pay.md):
a trade carries one recurring cost, a **levy to the treasury**, because money is conserved and a cost
paid to nobody is refused at load. ⚠ **Eleven lines of TOML and no engine change** (**F35**).
🔴 **Task 6 was about to be designed around this task's absence, and the user stopped it** (**F34**) —
`adr/0163`'s claim reads a shop's failure pressure, and until today no shop could be under any.
🔴 **THE LEVEL SHIPPED FIRST WAS UNOBSERVABLE AND ONLY A RUN SAID SO** (**F36**); **the test passed
for the wrong reason twice** (**F37**). Measured on `provisioned.toml` at 2,000 Citizens: **2 of 20
live shopfronts starving at end of run**, treasury 4,702,208, money conserved. 🔴 **AND A BROKE SHOP CANNOT BE TURNED OUT** (**F42**).
**Moves the hash** — a new `[[rule]]` and a new Bin on a kind, on a file no golden fixture runs.

🟢 **TASK 4 LANDED 2026-08-26 — `Scope.Pool` RESOLVES, AND `rulesets/provisioned.toml` TRADES.** A
`pool` input is one term and **three** Bin deltas — the seller's stock down, the buyer's balance down,
the seller's balance up — netted and settled atomically with the Rule. Open decisions **2 and 3 are
closed by the user**. **Assertion tier green at 2,317 tests, 7m47s**, which is the reading the previous
commit recorded, so the mechanism costs the tier nothing. ⚠ **It moves no golden hash**: every golden
fixture runs on a file with no `[districts]` table, and no saved column was added — the two new indexes
are `(derived AND rebuilt)`.
🔴 **THE MEASUREMENT `adr/0139` DEMANDED IS TAKEN AND IT IS NOT THE ANSWER THAT RECORD EXPECTED** (**F26**).
🔴 **THE WORLD TASK 3 SHIPPED COULD NOT DEMONSTRATE ANYTHING AND ONE RUN SAID SO** (**F23**).

🟢 **TASKS 1, 2 AND 3 LANDED 2026-08-25.** Task 3 is `rulesets/provisioned.toml` — **W-Q4 answered**,
and the first shipped file in which anything is built to *sell*. ⚠ **It moves no hash**: a new file
changes no existing world, and no golden is touched. **Assertion tier green at 2,317.**

🟢 **TASKS 1 AND 2 LANDED 2026-08-25.** Task 2's own account is *What task 2 found* below, and the
short version is that ***the split's mechanism was never the hard part***: `ZonedLots` reproduces
`main`'s city to the digit on a world with no trade land, and every difficulty was the fixture
absorbing the land the split took. **Assertion tier green at 2,311 tests.** ⚠ **All three golden
artefacts re-baselined this time** — `session.borough` as well as both traces, because task 2 moved
three zone commands in `GoldenFixtures` and the committed log carries them.

🟢 **TASK 1 LANDED 2026-08-25.** Decomposed against the tree at `22c6fb0`; open decision 4 settled by
reading, and the reading **refuted** the paragraph that proposed it. **Assertion tier green at 2,311
tests.** ⚠ **Two golden traces re-baselined** — `session-trace.txt` and `driving-session-trace.txt`, which
between them are what all **three** failing `GoldenHashTests` read — because a new saved column moves
the State Hash and
[`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
makes that an attribution question. **Tasks 2–10 unstarted.**

🔴 **THE INHERITED ORDER IS BACKWARDS, AND THAT IS THE FINDING** (**P1**).
[`0037`](0037-goods-between-buildings-the-district-pool.md) has task 7 the purchase and task 8 the
Provider. **The purchase cannot resolve until a seller exists that can hold stock**, and a seller that
can hold stock is
[`adr/0166`](../docs/adr/0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md) —
which [`0000`](0000-board.md) and [`0003`](0003-build-plan.md) both record as widening **the decline
half only**. ***It is a precondition of the purchase itself.***

⚠ **The board's *"which Ruleset carries the Provider is 26's own first task"* is right that the world
comes early and wrong that it can come first** (**P2**): the file must attach a Rule to a trade, and
whether that syntax exists at all is **open decision 4** below.

✅ **The content gate is discharged.** Session W settled what raises a Provider
([`adr/0163`](../docs/adr/0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md)),
what declines one ([`adr/0166`](../docs/adr/0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md))
and the land-use split ([`adr/0165`](../docs/adr/0165-a-zone-permits-building-kinds-so-the-split-is-exclusive-and-the-instrument-paints-it.md)).
🔴 **W-Q4 — which Ruleset — is still open and is this milestone's task 3**, and it is the named
ratifier for `adr/0163`'s two [`0002`](0002-open-questions.md) §D2 numbers.

🔴 **Four decisions were open and decomposition found all four. Three remain.** None is in any ledger.
✅ **Decision 4 was settled 2026-08-25 by reading the symbol it named**, and it made the work *larger*
rather than smaller — the reading that would have shrunk it was refuted. **Decisions 1, 2 and 3 are
still open** and land in tasks 6, 4 and 4 respectively.

---

## The named risk, as `06` states it

**That the Rule engine ships a scope it refuses** —
[`02 §4.3`](../docs/02-simulation-model.md)'s own worked example, a bakery drawing from the market,
is unloadable, and no chain in [`04`](../docs/04-economy-and-goods.md) can cross the ownership
boundary it names.

⚠ **The `throw` is the symptom and not the obstacle.** `RuleEngine.Bin`'s `case Scope.Pool`
(`RuleEngine.cs:875`) is four lines. What stands behind it is a counterparty that does not exist, a
seller with nowhere to keep stock, and a market row nothing can look up from a Bin.

---

## What the build already holds — surveyed 2026-08-25

### What exists and works

- **The market row.** `Space.DistrictPoolTable` — `District`, `Bin`, `Price`, `Rate`, `Consumed`, all
  saved, keyed by `(District, Good)`. `World.FitDistrictPools` opens one Bin per Good per live
  District; `World.RepriceDistrictPools` runs once a Day from `Simulation.cs:944` and damps the price
  against the import ceiling. **Correct and inert on every shipped world**, in its own doc's words.
- **The money side of an actor.** A Business owns a Bin **list** (`BusinessTable.BinHead`/`BinTail`,
  `adr/0143`) with `Balance` derived off it, opened by `World.OpenBalance`. Milestone 27 makes
  Businesses exist two ways and `founded.toml`/`levied.toml` demonstrate both.
- **The elapsed arithmetic `adr/0163` needs.** `ZoneRuleEngine.Worst` (`ZoneRuleEngine.cs:444`)
  already computes `tick.Raw − StarvedSince[instance].Raw` against `threshold × rate` and
  cross-multiplies the tie-break. ***The condemnation path has been doing tier 1's sum since 5c.***
- **The zone gate.** `ZoneRuleEngine.Create` tests `Lots.Zone[lot] & definition.Admits`, where
  `ZoneRuleDefinition.Zone` is a **bit index** and `LotTable.ZoneBits` is 16. So `adr/0165`'s
  exclusive split needs **no new mechanism** — only a painter.
- **The occupant Bin.** `[[building]] bins` already takes `owner = "occupant"`, and
  `World.CreateOccupantBin` gives the ceiling to the premises and the level to the tenant
  (`adr/0141`). ***The rule a Provider's stock needs is shipped; only its Household typing is in the way.***

### 🔴 Precondition 1 — the seller has nowhere to keep stock

[`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md),
**as corrected on the day it was written**: a buyer's `pool` input resolves to **one seller's Bin**,
and *"the seller is the **BUSINESS**: it holds the inventory, it holds the balance, and it runs the
Rule."* The correction's own words for the alternative are ***"one seller with two custodians, and it
is wrong."***

**A Business owns exactly one Bin today, and it is the balance.** `World.Unpremise`'s remark says so
in terms: *"a Business's stock Bins are **unbuilt**, and writing the sweep for them now would be a
mechanism with no rows to walk. The rule is decided and the code for it is milestone 27's."*
🔴 **Milestone 27 did not build it.**

And nothing can hang a Rule on one: `RuleInstanceTable` has `Building` and `Household` and **no
Business** (`RuleInstanceTable.cs:47-48`), with the unset Household handle as the discriminant.

***So the purchase has nothing to resolve against.*** That is `adr/0166` in full, and it stands in
front of the purchase rather than beside it.

### 🔴 Precondition 2 — the land-use split has no painter

`SyntheticCity` writes `zone: 1` at **three call sites and two decisions** — `SyntheticCity.cs:663`
and `:693` in `Subdivide`, and `:829` in `CarveEdgeBlock`. `LotTable.Create` (`LotTable.cs:223`) is
the **sole writer** of `Lots.Zone` in the project, confirmed by reference search. So a Ruleset naming
any bit but 0 **loads clean and builds nothing** — the failure is silent, and it is silent in the one
world that would exercise the Provider.

### 🔴 Precondition 3 — there is no seller lookup and no way back from a Bin to its price

- **No Provider List exists.** `adr/0066` specifies one as an intrusive `IndexList`; every occurrence
  in `src/` is prose — `TripDump.cs:35`, `TripPurpose.cs:39`, `Entities.cs:36`. **No table, no column,
  no type.**
- **No `(District, Resource) → Bin` index.** `World.FindDistrictPoolBin` (`World.cs:3254`) is a full
  scan of the pool table. `DistrictPoolTable`'s own doc: *"the hot path arrives with the purchase.
  **Task 7 owes it.**"*
- **No `Bin → market row` reverse lookup at all**, so a purchase that has already resolved a Bin
  cannot read the `Price` beside it without another scan.
- **A Building carries no District handle, deliberately.** `DistrictResidency.Of` is the lookup, via
  `Buildings.Lot → Lots.East/North → CellGrid.ToCells` — the same chain `RuleEngine.Emit` already
  walks for a Map emission.

---

## Open decisions this milestone owes

**Four, all found by decomposition, none in any ledger. Two of them can make the work smaller.**

### 1. 🔴 What does a claim write?

[`adr/0163`](../docs/adr/0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md)
makes demand **subtractive** — *"a claim makes the demand a stock that answering it depletes"* — and
takes `02 §5` step 2e's *consume the dwelling* over a build-rate throttle **by name**.

**The build has nowhere to subtract from.** Elapsed unserved need is `tick − StarvedSince` per Rule
Instance; `RuleInstanceTable.StarvedSince` is written by `RuleEngine.Stop` and zeroed by
`RuleEngine.Fire`, and nothing else touches it. A claim that reset it would **destroy the starvation
record** the evidence path reads (`Evidence.cs:309` → `RuleEvidence.StarvedSince`); a claim stored
anywhere else is a new column with `adr/0006`'s question on it.

***`adr/0163` decided that a claim happens and not what it writes.*** **Arguable**, and it is the
decision the two §D2 numbers are denominated against.

### 2. ✅ SETTLED 2026-08-26 — the Good leg is touched first, so a purchase blames the MARKET ROW

**Taken by the user.** A purchase can be short of stock or short of money, and when it is short of both
the Good leg is the one that names the blocker. It falls out of `RuleEngine.Check`'s existing rule —
*the first short Bin, in touch order* — because `Buy` touches the seller's stock before either money
leg. ⚠ **And the stock leg blames the market row rather than the seller's Bin**, which is the half that
is not free: `Touch` gained a **blame** parameter for it, because a buyer parked on one shop's Bin is
woken by that shop alone and sleeps through every other seller in the District restocking.

**Why the Good and not the money.** A district-wide shortage is the more informative cause and the one
a player can act on; destitution is the buyer's own and is reached anyway, because a woken buyer
re-`Check`s, fails on money and re-subscribes on its own balance. ***Both converge and only one of them
tells you something about the city.*** The bounce **P5** describes is therefore real, accepted and
unrecorded — `adr/0137`'s field is task 5's.

⚠ **A third case turned out to exist and neither the decision nor P5 saw it**: *no market at all*. See
**F22** — it is not a blame question, because there is no Bin to blame.

### 2a. 🔴 Which Bin does a purchase blame? — *the question as first written*

`RuleVerdict` carries **one** `int Bin` (`RuleEngine.cs:19`) and `Succeeded => Bin == Rows.NoSlot`.
`RuleEngine.Check` blames **the first short Bin, inputs before outputs**. `RuleEngine.Requirement` is
netted per `(instance, binSlot, blocking)`, so `World.Drain`'s affordability test prices **only the
half it is asked about**.

A purchase short of money but not of stock would therefore be woken by a Good arrival, re-`Check`,
fail on money, and re-subscribe on the money Bin. **That chain is legal and it means a purchase's wait
can bounce between two Bins.**
[`adr/0137`](../docs/adr/0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md)
names the field `RuleEvidence` is missing; ***it does not arbitrate which Bin a two-sided term
blames.*** **Arguable.**

### 3. ✅ SETTLED 2026-08-26 — a counter-based START and a first-fit walk from it

**Taken by the user.** A seller is a **Business-owned Bin of a Good, standing in the buyer's District**
— one Bin is exactly one `(Business, Good)`, which is the cardinality the question has.
`PurposeTag.SellerChoice` draws a start offset over the market row's seller list on
`(seed, buying Rule Instance id, tick)`, and the walk takes the first seller holding a whole batch —
`floor × amount` — from there.

⚠ **Cheapest was not on the table.** `DistrictPoolTable.Price` is per market row, so every seller in a
District charges the same and *cheapest* has nothing to discriminate. `adr/0139` says the `Price` field
*"moves from the market row to the seller"*; the build kept it on the row and `CONTEXT.md` records that
reading. **When per-seller prices arrive — `06` milestone 13, which is `adr/0139`'s own revisit trigger
— the draw is retired rather than ratified**, because cheapest becomes a discriminator.

⚠ **First-fit from the head was refused as `02 §8` rule 5's own worked failure** — *"the same Building
would win every contested draw for the life of the city"* — with **list position** standing in for
entity id. ***A list order nobody chose is still an order somebody profits from.***

⚠ **It is keyed on the BUYER and not on the market row.** Keyed on the row, every buyer in a District
would be sent to the same seller on the same Tick and the dispersion the draw exists to create would be
a rotation the whole city performed in step.

**A seller below one batch cannot sell, and that is what being out of stock is** — `adr/0139`'s own
surviving consequence, and a Rule fails only when *no* seller in the District holds one.

**Its cost is measured and it is [`0013`](0013-tick-budget.md)'s row.** See **F26**, which is the part
of this decision that did not go as the ADR expected.

### 3a. 🔴 What is a seller, and how is one chosen? — *the question as first written*

`adr/0139` says resolution chooses **one** seller at term-resolution time and says **nothing about
which one**. Reach is the District, so the candidate set is every Business in it selling that Good.
Cheapest? First-fit? Round-robin? **It is hash-bearing, it is in no ADR, and the choice is what makes
the market a market rather than a queue.** ⚠ Its cost is *measurable* and unmeasured, and `adr/0139`
says so itself — a [`0013`](0013-tick-budget.md) row is owed on the day.

### 4. ✅ SETTLED 2026-08-25 — the key is on the Bin, not the Rule, and money must be declarable

**The reading below was written before `RuleDefinition.Tenancy` and `ApplyTenancies` were opened. It
was WRONG in its conclusion and half-right in its mechanism**, which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
working: the paragraph named the symbol, the symbol was read, and the reading refuted it.

> ***What it said:*** milestone 27 task 9 built `[[rule]] trade = "<name>"`, crashed and withdrew it
> ([`0041`](0041-the-business-is-a-thing-the-city-contains.md) **G39**) — but `RuleDefinition.Tenancy`
> is derived from a Rule's own `local` terms, so *"a Rule whose local Bins are `owner = "occupant"`
> may already classify as an occupant Rule, and the missing piece may be only the arming arm rather
> than a key."* ***If it holds, task 1 loses a loader key.*** **It does not hold. Task 1 gains one.**

**What the build actually does.** `RulesetLoader.ApplyTenancies` (`RulesetLoader.cs:1782`) walks each
Rule's `local` terms — inputs, outputs **and `fills`** — looks each addressed Resource up in the kind's
`[[building]] bins`, takes that declaration's `owner`, and **refuses a Rule whose local terms
disagree**. So the derivation is real and it works. `BinTenancy` (`Ruleset.cs:318`) has **two values**:
`Premises` and `Occupant`.

🔴 **Two values answer *premises or tenant* and cannot answer *which tenant*, and the ambiguity is
already shipped.** `minimal.toml`'s `dwelling` kind declares an occupant Bin (`sundries`), two occupant
Rules (`restock`, `consume`) **and `business = "shop"`** — so under
[`adr/0148`](../docs/adr/0148-a-premises-kind-may-declare-its-trade-and-instantiating-one-is-not-housing-anybody.md)
that one kind hosts a Household occupant and a Business occupant in the same Building, holding two of
the same four `occupants` slots. ***Arming on `Tenancy == Occupant` alone would give every instantiated
shop a larder and a `consume`***, on every shipped world, silently.

🔴 **And the derivation cannot reach money at all, which is the half that matters most.**
[`adr/0166`](../docs/adr/0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md)'s
decline is a Rule consuming **money** — and a money Bin is **never declared in `[[building]] bins`**.
`World.OpenBalance` (`World.cs:3889`) opens it at `long.MaxValue` tagged `BinOwnerKind.Business`, and
`rulesets/taxed.toml:248` records that the loader **refuses a capacity key on a money Resource**: *"a
money Bin has no ceiling, and the family is what says so."* So `ApplyTenancies`'s lookup finds **no
declaration**, falls through to its `holds = BinTenancy.Premises` default, and ***the bankruptcy Rule
derives to the landlord***. ⚠ **It is a silent wrong answer and not a refusal**, which is **P6**'s shape
arriving in the loader instead of in the painter.

**The decision, taken by the user 2026-08-25.** ***Both halves are stated on the Bin and neither on the
Rule***, so [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)
is **unamended** and `taxed.toml`'s *"a `[[rule]]` has no `owner` key, because the terms already say
it"* stays true as written:

1. **`BinTenancy` gains a third value and `owner` gains `"business"`.** A Provider's stock is a Good,
   and its ceiling is forced to be the premises' — `adr/0141` plus `RebuildCapacities`, whose ceiling
   is a function of `(building kind, Resource)` and which `FitOccupant`'s own remark says an unhoused
   tenant has no kind to read one from. ***That is forced rather than chosen.***
2. **A money Resource becomes declarable in `[[building]] bins` with an `owner` and no `capacity`**,
   purely so `ApplyTenancies` has something to look up. ⚠ **The row opens no Bin** — `OpenBalance`
   still does, and the capacity refusal stays exactly as it is. It is a declaration that states a
   tenancy and allocates nothing, and **that oddity is the price of leaving `adr/0141` alone**; it is
   written down here so the next reader meets it as a decision rather than as a bug.

**Rejected: `[[rule]] owner = "business"`.** Direct, reaches money with no loosening, and the loader
could still refuse an authored owner that disagreed with the derivable Good terms. **Refused because it
amends `adr/0141` to buy tidiness**, and that ADR's refusal is quoted in a shipped Ruleset header.

**Rejected: reviving `[[rule]] trade = "<name>"`** (27 task 9's key). It matches `jobs` having moved to
`[[business]]` under `adr/0148` — *"a Building employs nobody; the trade tenanting it does"* — but **a
Bin's ceiling cannot follow it**, so the Rule and its Bins would be keyed on different things. ***That
is the split `adr/0141` declined***, arriving by the other door.

⚠ **What this costs, stated rather than left to be found: a Business's behaviour is a property of its
PREMISES KIND and not of its trade.** So `tenanted.toml`'s *"the same shopfront hosts a bakery, then a
barber, and the building did not change"* buys identity and **not** conduct — two trades in one kind
run the same Rules. Two trades that behave differently are **two `[[building]]` kinds**, which is what
[`adr/0165`](../docs/adr/0165-a-zone-permits-building-kinds-so-the-split-is-exclusive-and-the-instrument-paints-it.md)
zones over anyway. ***It is not a defect and it is not free***; it is the reading of `adr/0141` this
decision commits the build to, and the trigger to reopen it is a Ruleset that wants one shopfront kind
hosting two trades with different Rules.

---

## Tasks

**Ordered by what the next one needs.** Every entry says whether it moves the State Hash, because
three of them do and [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
makes that an attribution question rather than a scheduling one.

1. ✅ **DONE — A Business runs Rules and owns Bins** — [`adr/0166`](../docs/adr/0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md).
   `RuleInstanceTable` gains a Business subject; `World.FitOccupant`/`UnfitOccupant` grow a Business
   arm; `CreateOccupantBin` stops being Household-typed; `FindLocalBin` becomes a three-way
   resolution. 🔴 **The blast radius is every `RuleInstances.Building[instance]` read and not the
   column** — `0041` **G39**: ***"the column was the visible tenth of a subsystem."*** ✅ **What makes
   it tractable now and not at 27 is milestone 25's tenancy rule**: a Business's Rule Instances die
   with its premises, so the `StaleHandleException` that crashed task 9 cannot arise.
   **Moves the hash** (a new saved column). Discharges [`0012`](0012-corpus-audit.md)'s
   `RuleInstanceTable.cs:92` entry. ✅ **Open decision 4 is settled and it ADDS to this task**: a third
   `BinTenancy`, `owner = "business"` on `[[building]] bins`, and a money Resource declarable there
   with no `capacity` so `ApplyTenancies` can reach the decline Rule. **No `[[rule]]` key**, so
   `adr/0141` is unamended.
2. ✅ **DONE — The land-use split** — [`adr/0165`](../docs/adr/0165-a-zone-permits-building-kinds-so-the-split-is-exclusive-and-the-instrument-paints-it.md).
   `SyntheticCity`'s three `zone: 1` literals become index arithmetic: every *N*th block carries the
   trade bit, at the `:693` loop that already has `column` and `row` in hand. **No number, no
   `purpose_tag`, no §D row** — the ADR *removes* a number. 🔴 **Its own commit**: it moves the State
   Hash of **every generated world**, so every golden baseline is re-recorded, and folding it into a
   feature task would hide the attribution.
3. ✅ **DONE — The Provider Ruleset** — `rulesets/provisioned.toml`. **W-Q4, and the world.** A second `[[building]]` kind, its `[[business]]`
   trade, a second `[[zone_rule]]` on the trade bit, the input and output Goods. On `twinned.toml`'s
   two-lattice base, because `[districts]` needs two centres and `RefuseUnpricedGoods`
   (`RulesetLoader.cs:5194`) then demands a `[[hinterland]] prices` entry for **every** Good.
   ⚠ **It loads and does not run**, and that asymmetry is the task's own acceptance test: `TryScope`
   accepts `pool`, `RuleEngine.Bin` throws. ⚠ **Its header must say the stride is not how any city is
   zoned** — `adr/0165` requires it. **Moves no hash of its own**; it is a new file.
4. ✅ **DONE — The purchase — `Scope.Pool` resolves.** The District from the Building's Cell, then a seller,
   then Good one way and money the other, settled atomically with the Rule. Owed with it: the
   `(District, Resource) → Bin` index `DistrictPoolTable`'s doc says **task 7 owes**; a `Bin →` market
   row lookup for the price; and `DistrictPoolTable.Consumed` gets its **first writer**.
   ⚠ **Open decisions 2 and 3 land here.** 🔴 **`Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool` is
   armed by this task** — `World.cs:3639` says it is safe today *"because `Scope.Pool` throws"* and
   names this as *"what will fail on the day task 7 opens the scope."*
5. ✅ **DONE — Evidence — [`adr/0137`](../docs/adr/0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md)'s
   two halves.** `RuleEvidence` gains the blocking Bin — it has `Blocking` and `ConditionId` and no
   Bin today (`BuildingEvidence.cs:82`) — **and** the money check produces a verdict *naming the money
   Bin* rather than failing without subscribing. ⚠ ***The second half is the one that gets skipped***,
   because nothing about the money leg is authored and so nothing about it is prompted.
   🔴 **AND IT WAS SKIPPED — not by omission but by a subscription that shared code cancelled a
   line later.** `Requirement` walks terms, the payment has none, so `Stop`'s own drain woke every
   buyer it had just parked. **F28**–**F33**, and `adr/0137` is amended with both halves.
6. **`adr/0163` tier 1 — demand for a shop.** The reach query over `WalkScratch.SettleAll`, the elapsed
   sum, and the claim. **Replaces `UnplacedPool.Count == 0` for the trade rule only**; the housing rule
   stays tier 0. ⚠ **Open decision 1 lands here**, and both §D2 numbers are chosen here and move to
   §D1. **A [`0013`](0013-tick-budget.md) row is owed on the day, with a measured multiplicand and
   not a guessed one.** Corrects `ZoneRuleEngine.Create`'s summary to say **which rule** it describes
   ([`0012`](0012-corpus-audit.md)).
7. ✅ **DONE, AND RUN BEFORE TASK 6 — the decline half** —
   [`adr/0169`](../docs/adr/0169-a-standing-cost-needs-a-counterparty-so-a-trade-pays-rates-until-there-is-a-supplier-to-pay.md).
   A money-consuming Rule on the trade, so an empty balance is `Blocking.Supply`
   on a money Bin — which is pressure, is **bankruptcy** rather than starvation, and is the world
   [`0037`](0037-goods-between-buildings-the-district-pool.md) task 10 has been waiting for. ⚠ **A
   shop nobody buys from is IMMORTAL** and this is why: unsold stock stops on `Blocking.Space`, and
   `RuleEngine.Stop` clears the pressure clock for every reason but `Supply`.
   🔴 **THE ORDER CHANGED AND THE USER CHANGED IT**, on the reasoning in **F34**: task 6 was about to
   be designed around this task's absence. **Moves the hash** — a new `[[rule]]` and a new Bin on a
   kind. **No engine change at all**, which is the finding.
8. **Something to look at** — a runner mode showing a Pool with stock, a price that moves, and **a
   Building that could not afford it**. ⚠ **The third clause is the one that would be dropped**, and
   it is the only one that shows the market having a consequence.
9. **The long acceptance run.** Conservation across trades with `adr/0024`'s equality **exact**, no
   collection or magnitude trending at steady state (`adr/0006`), and bankruptcy distinguishable from
   starvation in `Evidence`. 🔴 **Look for `adr/0163`'s own revisit trigger**: shops built, condemned
   for want of customers, and rebuilt on the demand their condemnation restored. ***That is the
   threshold and the claim disagreeing, and it is the first thing to look for rather than the last.***
10. **Closing — the ledger debts this milestone owes and nothing else discharges.**
    [`0003`](0003-build-plan.md) **queue item 17** is *"retired by design rather than fixed, and it
    must be STRUCK DELIBERATELY when the code lands"*; [`0012`](0012-corpus-audit.md)'s two doc
    comments at `UnpremisedTable.cs:19-25` and `PlacementEngine.cs:645-650`, both of which say
    *"nothing tenants a Business"* while the method that does is four hundred lines above one of them;
    and [`06`](../docs/06-roadmap.md)'s nine-Resource placement row, which says outright ***"whoever
    picks up 26 owes this row a re-read."***

---

## What this milestone must not do

**Inherited from [`0037`](0037-goods-between-buildings-the-district-pool.md) unchanged, and read it
there for the arguments.**

- ⚠ **Must not implement `Scope.Pool` as a wider Bin lookup.** It ships an unconserved economy and
  **no test in this repository would catch it.**
- **Must not author a price in a Rule.** `adr/0050`: the quantity is the term's `amount`, and
  `BinRef` is two fields on purpose.
- **Must not add a demand scalar** — ⚠ **and read [`01`](../docs/01-player-experience.md)'s sentence
  rather than `CLAUDE.md`'s compression.** What is refused is *a synthesised scalar with no
  constituents*; `adr/0163` **counts** rather than adds. ***A rule quoted without its qualifier does
  not merely mislead; it forecloses*** ([`0012`](0012-corpus-audit.md), and it cost session W an
  exchange).
- **Must not let Upkeep in by the back door** because the counterparty arrived. Upkeep is **unplaced**
  ([`adr/0136`](../docs/adr/0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md)).
- 🔴 **Must not permit universal mixed use.** It is `adr/0011`'s named exploit and the tolerance that
  closes it is unbuilt (`adr/0165`).

---

## Definition of done

`CLAUDE.md`'s cumulative list, plus [`0037`](0037-goods-between-buildings-the-district-pool.md)'s,
of which these survive unchanged:

- **`02 §4.3`'s own bakery example loads and runs** — the named risk stated as an artefact.
- A Ruleset in `rulesets/` demonstrating a chain that **crosses the ownership boundary**, with a
  header saying what it exists to show and what it must not be read as.
- **Conservation holds across a trade**, asserted rather than argued — Goods and money both.
- **Bankruptcy and starvation are distinguishable** in `Evidence`, on a world that produces both.
- Something to *look at*, including a Building that could not afford it.

**And added here:**

- 🔴 **`adr/0163`'s two §D2 rows move to §D1 with a value**, or the milestone says why they could not.
  They name **this milestone's own Ruleset** as their world, so nothing else can ratify them.
- 🔴 **The three ledger debts of task 10 are struck**, not merely satisfied. ***A gate is discharged by
  the work and struck by somebody, and only the first happens on its own.***

---

## What task 1 found

**F1 — 🔴 `adr/0166`'S DECLINE RULE CANNOT BE WRITTEN AS THAT ADR DESCRIBES IT, AND THE LOADER SAYS SO
AT PARSE TIME.** The ADR says *"give it a Rule that **consumes money** — an upkeep, a rent, any
recurring cost — and an empty balance is `Blocking.Supply` on a money Bin."* **A Rule with a local
money input and no money output is REFUSED**, by a check that predates this milestone:

> *"this rule destroys 1 money per application. Money is conserved — never created or destroyed inside
> the city, the Outside Connection being its only source and sink — so every money term needs a
> counterparty. **A cost paid to nobody is a leak, not a cost.** If the counterparty is a market, do
> not write the payment at all: a pool term settles its own (`adr/0050`)."*

***So the decline Rule is two terms and not one***, and the second one is a **decision the ADR does not
make**: money in from `local`, money out to `global` — the treasury — or no money term at all because
a `pool` term settles its own. ⚠ **It is not a defect in `adr/0166`**; it is `adr/0024` arriving one
level down, and the ADR's *"whose magnitude is hash-bearing and owed a §D row when it is written"*
already anticipated that the term needs authoring. **What is new is that the COUNTERPARTY needs
authoring too**, and *rent to the treasury* and *cost of goods to a market* are different cities.
🔴 **Task 7 owes this choice**, and it is not in any ledger. `BinTenancyLoadTests`'s `Trading` fixture
pays the treasury **because a fixture must pick one**, and that is not a recommendation.

**F2 — ✅ THE BLAST RADIUS WAS THE VISIBLE TENTH IN THE OTHER DIRECTION, AND `adr/0141` IS WHY.**
[`0041`](0041-the-business-is-a-thing-the-city-contains.md) **G39** — *"the column was the visible
tenth of a subsystem"* — is what task 1 was sized against. It did not hold: `RuleInstances.Household`
has **four read sites in `src/`**, because `adr/0141` had already made `RuleInstances.Building` *the
place rather than the subject*, so every Building read stayed correct unchanged. ⚠ **The estimate that
was wrong was task 9's, and it was wrong because the subject split had not happened yet.** ***A
widening is cheap in proportion to how completely the previous one finished.***

**F3 — ⚠ `RebuildCapacities` HELD A `NotSupportedException` FOR THIS EXACT CASE AND NAMED THE DECISION
THAT WOULD DISCHARGE IT.** A Business-owned, non-conserved Bin threw, with a comment reading *"which is
open decision 1 of `plans/0040` and not an oversight — nothing creates a Business's stock Bin yet."*
Task 1 creates them, so the throw became an owner walk beside the Household one. ***A refusal that
names the decision that would retire it is the opposite of a landmine***, and it is worth noting as the
thing that made this task's ceiling question answer itself.

---

## What decomposition found

**P1 — 🔴 THE PURCHASE CANNOT SHIP BEFORE `adr/0166`, AND BOTH LEDGERS SAY OTHERWISE.**
[`0000`](0000-board.md) and [`0003`](0003-build-plan.md) record `adr/0166` as widening 26's **decline
half**. It is more than that. `adr/0139`'s correction puts the seller's **inventory** on the Business
alongside its balance; a Business owns one Bin and it is the balance; and nothing can hang a Rule on a
Business at all. ***A buyer's `pool` term resolves to a seller's Bin, so with no seller there is
nothing to resolve.*** The old order — purchase, then Provider — is the order in which neither works.

**P2 — ⚠ THE RULESET CANNOT BE FIRST EITHER, AND THE REASON IS A SYNTAX RATHER THAN A DECISION.**
Session W's close routes W-Q4 to *"milestone 26 as its own first task."* The world does come early,
and it cannot come **first**: the file must attach a production Rule to a trade, and whether the
build can express that is **open decision 4**. ⚠ **The board's sentence is right about the reason** —
*a Ruleset gets written when the milestone that needs it is built* — **and the milestone needs one
thing built before the Ruleset can name it.**

**P3 — ✅ TWO PIECES OF `adr/0166` ARE ALREADY SHIPPED, WHICH IS WHY IT IS SMALLER THAN 27 FOUND IT.**
`[[building]] bins` already takes `owner = "occupant"`, and `World.CreateOccupantBin` already gives
the **ceiling to the premises and the level to the tenant** — `adr/0141`, in its own doc comment:
*"a shop holds what fits in the shop, and what is in it is the shopkeeper's."* ***The rule a
Provider's stock needs is shipped and only its Household typing is in the way.*** And milestone 25's
tenancy rule removes the crash: task 9 *"built the column before the tenancy rule existed to copy."*

**P4 — 🔴 `adr/0163` DECIDED THAT A CLAIM HAPPENS AND NOT WHAT IT WRITES.** Open decision 1. The
signal is `tick − StarvedSince`, and `StarvedSince` has exactly two writers, both in `RuleEngine`.
***A claim is subtractive by that ADR's own argument and the build has nothing to subtract from.***

**P5 — 🔴 A PURCHASE IS TWO BINS AND EVERY STRUCTURE UNDER IT HOLDS ONE.** `RuleVerdict.Bin` is one
`int`; `RuleEngine.BinAt` maps term **position** to a single Bin and `AccumulateClaims` is written
over that bijection; `RuleEngine.Requirement` nets per Bin, so `World.Drain` prices one half of a
purchase. ⚠ **None of this is a defect** — it is the shape of a 1:1 engine meeting a 1:2 term, and
`0037` task 7 named it. **What is new is that the wait can *bounce*** between the Good Bin and the
money Bin, and no record arbitrates that.

**P6 — ⚠ THE LAND-USE SPLIT'S FAILURE MODE IS SILENT, AND IT IS SILENT IN THE ONE WORLD THAT MATTERS.**
`LotTable.Create` is the sole writer of `Lots.Zone`, `SyntheticCity` paints bit 0 only, and a
`[[zone_rule]]` naming an unpainted bit **loads clean and builds nothing**. A Provider Ruleset written
before task 2 would run, produce no shops, and look like a demand-signal failure. ***Order 2 before 3
or the first run of the new world is a false negative about `adr/0163`.***

**P7 — ⚠ THREE LEDGER DEBTS NAME MILESTONE 26 BY NAME AND NONE IS IN ITS TASK LIST.**
[`0003`](0003-build-plan.md) queue item 17, [`0012`](0012-corpus-audit.md)'s two doc comments *"owed
with 26's first code task"*, and [`06`](../docs/06-roadmap.md)'s nine-Resource placement row. ***All
three were found by reading the ledgers for this decomposition and none by reading `0037`***, which is
the same shape as the payer and the Provider's own content decisions: **a blocker named inside one
document's entry is invisible to the ledger that owns what is next.**


## What task 2 found

**The split itself was three lines. Everything below is what the rest of the build did about it.**

**F4 — `ZonedLots`: a seeker draws over land that admits a dwelling, and `candidates` stops being a
land-use dial.** `PlacementEngine` drew a Household's candidates uniformly over the *whole* Lot
table. Its own remarks argue at length for why the draw moved to Lots — over Buildings,
`candidates` *"meant something the file could not state"*, because freed slots made three looks buy
about 1.3. ⚠ **Painting one block in eight commercial does the identical thing from the other side**:
three looks bought about 2.6, and the commercial share silently became a placement tuning knob.
**Measured: vacancy 18.5% → 26% at an unchanged capacity of ~188, which is fourteen dwellings nobody
found.** ✅ **The index is provably neutral where it should be** — with trade land off it reproduces
`main` at 35 of 189 with 206 queued, to the digit. ⚠ **The dead look the engine defends SURVIVES**: a
vacant Lot that admits a dwelling is a home not yet built and looking at it is a real
disappointment; a Lot that admits only a trade is not a home at all.

**F5 — `adr/0055` forbids the same repair one level up, and that is why the Zone Rule keeps its
diluted sample.** *"A Zone Rule's permission set scopes what it builds, never which Lots it looks
at"* — filtering its sample would let a player repaint a Lot and put the Building on it beyond every
Rule's reach, which is **immortality by paintbrush**. So the Zone Rule spends part of every sample on
land it cannot build on, deliberately, and placement was the only draw legitimately narrowable.

**F6 — 🔴 THE STRIDE SATURATES, so most of its values are the same world.** `(column + row) % stride`
spans `0..2(n−1)` on a grid of *n* blocks a side, and at 1,000 Citizens that is a handful — so
**strides 8, 12, 16 and 32 all produce 124 housing Lots and 10 trade Lots, byte for byte**, and a run
on each returns identical numbers. ⚠ ***This is why three successive guesses at the placement failure
each returned the reading unchanged***: the changes were real and the worlds were not. **A stride is
only a dial while it is smaller than the city.**

**F7 — 🔴 A SMALLER stride yields MORE housing land, which refutes the reading everyone starts with.**
The lattice is sized from the housing Lots it must hold, so a denser commercial grid forces a wider
city: **166 Lots = 126 housing + 40 trade at stride 4**, against **134 = 124 + 10 at stride 8**.
⚠ **The `adr/0055` dilution theory — that commercial land slows construction by wasting Zone Rule
samples — is REFUTED by the same sweep**, because stride 4 carries four times the trade land and
produces *lower* vacancy. ***The cost is land, not wasted looks***, and vacancy tracks housing Lot
count monotonically: 134 → 18.5%, 126 → 21.9%, 124 → 25.0%.

**F8 — 🔴 STRIDE 4 WAS ADOPTED AND WITHDRAWN THE SAME DAY, on a recommendation that rested on one
test.** It passes the placement long-run test untouched, which is what it was recommended on. The
full tier then showed it introduces an [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md)
trend in `EvidenceLongRunTests` — people carrying a reach-failure history rising **32.9** across the
tail against a 3-sigma band of **23.2**. ***A bound traded for an invariant is not a trade this
project makes.*** ⚠ **The lesson is about the evidence and not the number**: a 2×2 of the stride
against `PavedTiles`' compensation reads **6 / 11 / 8 / 9** failures, and no single test predicts it.

**F9 — ⚠ `PavedTiles`' compensation is NOT a Lot-supply fix, and its first comment said it was.** It
was written to keep housing capacity constant across the split and its comment claimed exactly that.
**Measured: it does not move the Lot counts by one Lot** — 134 = 124 + 10, with it and without it —
because the extent is quantised to whole blocks a side (`blocks = sqrt(wanted / perBlock)`, an
integer), so a 14% bump in `wanted` either moves that integer or does nothing. ✅ **What it actually
moves is the paved EXTENT, and that is load-bearing**: without it five `TripCommandTests` fixtures
and `CarOwnershipTests` fail. ***It was recorded as a Lot-supply fix, measured, and found to supply
no Lots*** — [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
failure mode, caught by the instrument rather than by review.

**F10 — the placement test's tolerance was denominated in the wrong quantity, and the split is what
exposed it.** Both its bounds took *a quarter of capacity*, calibrated on a world where **every** Lot
admitted a dwelling. It is now derived from the housing land the city actually has, which is **the
same number to the digit on a world with no split** and widens only in proportion to the land
removed. 🔴 ⚠ **CHOSEN AND NOT DERIVED**: the observed rise is *steeper* than inversely proportional —
scaling 18.5% by 134/124 predicts 20.0% where the run gives 25.0% — so it under-corrects and leaves
**3 places of margin on the vacancy bound and 2 on the residue**, against `main`'s 12. ***Nobody has
derived why vacancy rises faster than the land it lost.***

**F11 — the golden fixture is 548 Lots and not 134, and a report of this sitting said otherwise for
an hour.** `PlacementLongRunTests` declares its own `Population = 1_000`; `GoldenFixtures.Population`
is **4,000**. Measured across populations: 1,000 → 134 Lots (7.5% trade), 4,000 → **548** (10.9%),
8,000 → 1,104 (12.9%) — so the trade share converges on the intended eighth as the city grows, and
**F6's saturation is a property of the 1,000-Citizen world only**. ⚠ ***A conclusion about "the
fixture" that was measured on a different fixture*** — and it was load-bearing, because it made
*grow the golden world* look like the root-cause fix when a bigger city has **longer** commutes and
would worsen the one failure it was meant to cure.

**F12 — `JobSearchBoxTests` lost a property the fixture genuinely had.** It asserted *every* accepted
commute is Fast — *"what a city smaller than a commute looks like from inside the instrument."*
Re-measured: **2,294 employed, 2,293 / 1 / 0**, `beyond` still **0**. The cause is F9's extra
block-ring, and one commute at the far corner crossed the 20-minute rung. ⚠ **`beyond` and
`unsavoury` stay pinned at zero** — `adr/0095` is explicit that only the ceiling refuses — and only
the fast-against-moderate line was relaxed, to **99% Fast**, *as a bound and not a baseline*.
***The city stopped being strictly smaller than a commute, and that is a real loss recorded rather
than absorbed.***

**F13 — routed under `adr/0073`, not worked around.** A corrupted **handle** column is refused by the
resolver and never reaches `adr/0112`'s hash check, because folding a handle *resolves* it — so which
refusal you get depends on what the corrupt bytes happen to address. 🔴 **The load refuses either
way, so this is not a hole in invariant 6**; what was fragile is a test that named *which* refusal.
`SaveHashTests` flipped a byte in `household.bin_head`, the split changed the bin table's contents,
the byte started addressing a freed slot, and it went red for a reason with nothing to do with
saving. **Filed as `plans/0003` hash-moving queue item 19**; the flip now targets `lot.zone`.
⚠ **The `World.Migrate` finding this sitting owed was already filed** — queue item **15** states it
in full, including that `RemapBins` is applied by walking Buildings and *"that is the whole of where
it is applied."*


## What task 3 found

**F14 — ✅ the *loads and does not run* asymmetry is real, and the throw does NOT come from a Rule
firing.** The header and the plan both describe it as `TryScope` accepting `pool` while
`RuleEngine.Bin` throws, which is true and is not where it fires. Measured: the exception arrives from
the **end-of-run invariant** — `NoWaiterSleepsOnANonBlockingBin` → `CheckQueueStillBlocks` →
`AccumulateClaims` → `RuleEngine.Bin`. ***That invariant walks every claim of every waiting Rule
Instance whether or not it fired***, so a `pool` term is unreachable in this build even by a Rule that
never runs. `ProvisionedRulesetTests` says so at the symbol, because a reader debugging it would
otherwise look at the Rule engine's firing path and find nothing.

**F15 — the file could not choose its own base, and two refusals decide it.** A District derivation
over one peak returns one basin however it is written, so a Provider file on `minimal.toml` would have
no market to sell into; and the moment a file states `[districts]`, `RulesetLoader.RefuseUnpricedGoods`
demands a `[[hinterland]]` price for **every** declared Good. ***So the `[districts]` table, the second
lattice and the Hinterland prices arrive together or not at all***, which is why this is `twinned.toml`
plus a Provider rather than `minimal.toml` plus one. It declares **no new Good** for the same reason.

**F16 — ⚠ two exclusivity tests named `twinned.toml` by name and both went red on a file that had to
inherit from it.** `TwinLatticeTests.Only_twinned_authors_two_lattices_and_only_coastal_authors_another`
and `DistrictWatershedTests.Only_twinned_states_a_districts_table`. ✅ **Both were widened by name with
the reason attached, which is what the first one's own failure message instructs** — *"if this is
deliberate, say why in the file's header and add it to the exemptions above rather than widening the
test."* ***A file-scoped exclusivity claim is a corpus assertion wearing a test's clothes***, and the
right repair is to name the second file rather than to loosen the predicate, so a third file growing
either table by accident still goes red.

**F17 — 🔴 THE TRADE IS RAISED BY DEMAND FOR HOUSING AND THE FILE CANNOT FIX IT.**
`ZoneRuleEngine.Create` builds only while the Unplaced Pool is non-empty — tier 0, and the only demand
signal that exists. So `provisioned.toml`'s `trade` rule opens a shop *because somebody is unhoused*.
⚠ **This is recorded as a header warning rather than worked around**, because `adr/0163`'s tier 1 is
task 6 and authoring a stand-in here would be a mechanism nobody asked for. ***Until task 6, every shop
this file builds was built for the wrong reason, and no number out of it is evidence about siting.***
It is `crowded.toml`'s header shape exactly — a demonstration whose emitting kind contradicts what the
demonstration is about — and the two are worth reading beside each other.

**F18 — ⚠ the decline half is deliberately absent, and ~~`condemn_after` is declared anyway~~ the key
is no longer declared here at all.** `plans/0043` **W17**: an unsold Bin fills, the Rule stops on
`Blocking.Space`, and `RuleEngine.Stop` clears the pressure clock for every blocking reason but
`Supply` — so **a shop nobody buys from is immortal**. The failable Rule that kills one is
money-consuming, needs `adr/0166`'s Business Rules, and is **task 7** — ***which is also where
`condemn_after` returns to this file***, at a value chosen for a trade rather than inherited from a
dwelling.

⚠ **THE STRUCK HALF WAS TRUE FOR ONE DAY AND WAS CORRECTED BY MILESTONE 17, NOT BY THIS SESSION.**
The key was declared on both of this file's kinds *so the mechanism has somewhere to land*, and it was
removed along with the same key in **seventeen other shipped Rulesets**: it stood at **4** in every one
of the eighteen and no author had ever set it to anything else, which is
[`adr/0164`](../docs/adr/0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md)'s
*would a designer ever set this?* answered by the tree rather than argued. ⚠ **What made it matter is
not this file**: abandonment means a condemned Building leaves a **shell standing**
([`adr/0091`](../docs/adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md)),
so every world was demonstrating blight on the way to demonstrating whatever it was built for. Decline
now lives in `rulesets/declining.toml` and `rulesets/diagnosed.toml`. ***A key that arrives with the
mechanism needing it is the thing that change is for***, so F18's own reasoning is what removed it.

**F19 — 🔴 it is `adr/0163`'s own named ratifier and cannot serve as one yet.** Those two §D2 numbers
name *milestone 26's own demonstration Ruleset* as their world, which is this file — and nothing in it
steps, so every price sits at its ceiling, every consumption bucket is zero and no shop earns anything.
***The earliest run that could ratify them is task 9's.*** The header says so rather than leaving the
§D2 rows pointing at a file that looks ready.


## What task 4 found

**F20 — 🔴 `Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool` IS NOT ARMED BY THIS TASK, AND THE TASK
ENTRY ABOVE SAYS IT IS.** That entry quotes `World.cs`'s own comment — *"what will fail on the day task
7 opens the scope"* — and the scope is open and it did not. The comment was written the day **before**
[`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md),
which makes the Pool a **market and not a store**: the stock stays in the selling Business's own Bin,
so nothing ever deposits into a Pool Bin, `held` is zero for ever, and `RetirePool`'s transfer branch is
unreachable. ***The conclusion held and its stated cause was retired without anybody noticing***, which
is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
exactly. **Corrected at the symbol under `adr/0073`**, and `ProvisionedRulesetTests` now asserts the
Pool Bins hold nothing on **every Tick** of the one world that trades — which is the assertion
[`0037`](0037-goods-between-buildings-the-district-pool.md)'s *must not implement `Scope.Pool` as a
wider Bin lookup* had no test behind.

**F21 — ⚠ `adr/0139` SAYS THE `Bin` COLUMN GOES AND THE BUILD KEEPS IT, AND THE MECHANISM WINS.** That
record's *Consequences* list *"what goes"*: the `Bin` column, `BinOwnerKind.District`, `RetirePool`'s
transfer and the invariant above. But the same record requires a wake target — *"a waiter names **one**
Bin … so subscribing to N sellers is not expressible and never will be. The market row is therefore the
subscription target"* — and a subscription target **is** a Bin. ***So the column survives with a level
that is always zero***, which is what `CONTEXT.md` already records: *the row is the price, the wake
target and the reachable sellers*. **The consequences list and the mechanism disagree and this is filed
rather than resolved**; what is actually dead is the transfer and the invariant, which is F20.

**F22 — 🔴 A BUILDING CAN RUN A RULE ON GROUND NO DISTRICT HAS CLAIMED, AND IT CRASHED THE FIRST RUN.**
The watershed runs on `[districts] revisit_ticks` — **2,048** on this file, one Day — and admission is
unconditional but happens only at an evaluation. So a Building raised between two evaluations stands
outside every District for up to a Day, and its `pool` term has no market row to resolve against. ⚠ **It
is neither a defect nor a blame question**: there is no Bin to wait on, so subscribing is not
expressible. ✅ **It resolves to a SUCCESS AT ZERO APPLICATIONS, and `RuleVerdict.Succeeded`'s own remark
is the argument, written five milestones ago**: *"it re-arms on its rate, waits on nothing, and moves
nothing, because there is no Bin that could ever wake it."* ***No third outcome, no new mechanism, and
the sentence that authorises it was already in the file.***

**F23 — 🔴 `provisioned.toml` GAVE HOUSEHOLDS NO MONEY, SO THE WORLD TASK 3 SHIPPED AS THE
DEMONSTRATION COULD NOT DEMONSTRATE ANYTHING.** It states no `[households]` table; every Household opens
an empty balance at Tick 0; and `adr/0024` makes the Outside Connection money's only source. There is no
wage (`adr/0026`, milestone 15), no gate, no `[[policy]]`. ***So the first run after `Scope.Pool`
resolved failed every purchase on the MONEY leg, at Tick 0, for ever*** — the shops filled and nobody
bought. **Found by running it, not by reading it**, and it is milestone 11's **F25** a third time: a task
specified against a world that does not exist. ✅ Repaired with `[households] car_ownership_percent = 0`
and an `opening_balance` band, **whose ceiling is derived** — 256 sundries a Day at an import ceiling of
100 is 25,600, one Day's shopping — and whose floor is zero for `taxed.toml`'s own reason. Two
[`0002`](0002-open-questions.md) §D1 rows. ⚠ **It is a stock spent down and never replaced, so a run of
this file has an END rather than a steady state**, and that is the shape task 9's acceptance run has to
be written against rather than surprised by.

**F24 — 🔴 TASK 1 SHIPPED A BUSINESS STOCK BIN THAT VIOLATES AN END-OF-RUN INVARIANT, AND NO WORLD COULD
REACH IT.** `Invariant.BinCapacityMatchesItsDeclaration` exempts a **Household**-owned non-conserved Bin
because its ceiling is the premises kind's (`adr/0141`) and checks it from the owner instead; `adr/0166`
gave a **Business** the same arrangement and `RebuildCapacities` grew the same owner walk for it, and
this check did not. So a Business-owned Good Bin fell through to the *must be `long.MaxValue`* branch and
failed. ⚠ **`provisioned.toml` is the only shipped file declaring `owner = "business"` on a Good, and
`Scope.Pool` threw before the end-of-run walk ever ran** — so the defect was shipped, correct-looking and
unreachable, for one day. ***A widening that reaches the rebuild and not the check is invisible until a
world exercises both***, which is F2's *"a widening is cheap in proportion to how completely the previous
one finished"* read from the other end.

**F25 — ⚠ `VerifyDecideWritesNothing` IS THE ENTIRE COST OF A TEST ON THIS FILE: 49 s BECAME 2 s.** A
6,144-Tick, 1,000-Citizen run in the assertion tier, unchanged but for the guard. The guard folds the
whole world twice a Tick and this world is `twinned.toml`'s **two paved lattices**, so it is
`bordered.toml`'s recorded ~75× arriving on a second file. ⚠ **It is a GUARD's cost and not the city's**
and must never be quoted as one — `plans/0013` already carries that sentence about a different capture.
***What is worth keeping is that the first instinct was to cut the Tick count***, which would have
bought the same 47 seconds by testing less.

**F26 — 🔴 THE MEASUREMENT `adr/0139` DEMANDED IS TAKEN, AND IT SPENDS THAT RECORD'S OWN ESCAPE.** The
ADR routed *the per-firing seller lookup* to a machine, said `adr/0043` binds until a number exists, and
named the fallback: *"the fallback is **not** a return to a shared store — it is an index on the market
row, which is the shape `DistrictPoolTable` already has."* ⚠ **Task 4 built that index on day one**, so
the reading is *with* it. **Measured on the reference machine against `provisioned.toml` with ONE LINE
changed** — `restock`'s `inputs` from a `pool` term to `[]`, so the shops, the split, the lattices and
the Districts are in both arms: **0.237 / 0.585 / 1.267 ms a Tick at 10,000 / 20,000 / 40,000
Citizens.** 🔴 **That is ~n^1.2 and the only super-linear consumer in [`0013`](0013-tick-budget.md)** —
linear extrapolation to 1M gives ~32 ms a Tick and carrying the exponent gives order 60, against a
**15.6 ms whole-Tick budget**, so ***the two disagree by 2× at the target and neither is an answer.***
⚠ **The attribution is conservative**: without the `pool` term `restock` never fails, so the cheap arm
runs *more* Rule firings. ⚠ **And it is the WHOLE purchase, not the seller walk** — a stopwatch cannot
separate the District lookup, the seller draw, the settlement, `RingMarket` and the index rebuilds.
**Routed to [`0002`](0002-open-questions.md) §B as a NEW question, because `adr/0139`'s is answered and
its fallback is gone.** ***Do not optimise the seller walk on this number until something has said the
seller walk is where it goes.***

**F27 — ⚠ A RULESET WITH A `pool` TERM AND NO `[districts]` TABLE WOULD HAVE SILENTLY DONE NOTHING,
WHICH IS THE FAILURE MODE F22's FIX INTRODUCED.** Firing at zero applications is right for a Building
waiting on a boundary that is coming; it is wrong for a city that has no Districts at all, because that
condition is **permanent**. The two are told apart by `DistrictRuleset.Runs` and the second throws.
⚠ **The refusal belongs at LOAD with a file and a line** (`adr/0048`) and the loader has no such check —
filed rather than assumed. ***One fix's correct case is another's silent one, and the tell was two
existing tests continuing to pass for a reason that had changed***; both had their summaries rewritten
rather than their assertions, which is what `adr/0093` asks for.

---

## Task 5's findings — Evidence, and the defect the instrument found in task 4

**F28 — 🔴 A BUYER BLOCKED ON MONEY NEVER SLEPT, AND `adr/0137` PREDICTED THE OUTCOME WHILE NAMING THE
WRONG MECHANISM.** That record warned the money leg would be skipped because *"a Pool draw that fails
for want of money has no term and therefore no Bin to subscribe to"*, and that the cheapest
implementation *"returns insufficient funds and subscribes to nothing"*. **Task 4 did subscribe** —
`RuleEngine.Buy` pushes all three legs through `Touch`, so the affordability walk blames the purse by
the same rule as any authored term — and reading that code, ***this session concluded before running
anything that the second half was already satisfied by shape.*** It was not.

`RuleEngine.Stop` drains the Bin it has just joined (queue item **11**'s repair, and correct);
`World.Drain` asks `RuleEngine.Requirement` what the waiter needs; **`Requirement` walks the Rule's
terms**, and under `adr/0050` the payment has none. It answered **0**, and a requirement of nothing is
satisfied by a Bin holding nothing, ***so the buyer was woken by its own stop.*** Measured on
`provisioned.toml` with every purse emptied and every shop held full: **323,438 stops correctly named a
money Bin and the wait list held 0 of them**; after the fix, **61**, with spinning armed buyers falling
from 60 to 3.

⚠ **A wait undone by the drain that follows it is indistinguishable from no wait at all** — the buyer
re-evaluated every 8 Ticks for ever, paid the purchase's full cost each time, and reported itself
**armed**. 🔴 ***So the build's single super-linear consumer was ALSO doing its most expensive work in
a loop that could never terminate***, and F26's measurement was taken with that loop running. **The
figures are not withdrawn** — the A/B arms shared the defect — but they are an upper bound now.

**The fix is `RuleEngine.PoolDraw`**, which prices the money leg from the **market row's** price rather
than from a re-drawn seller. ⚠ **It is derivable at drain time only because `adr/0167` put the price on
the row**, which is the same fact that made *buy from the cheapest* unavailable — ***the constraint that
cost the design a discriminator is what let this stay derived***, so `adr/0063`'s *derived rather than
stored* is kept rather than excepted.

**F29 — ⚠ IT WAS FOUND BY AN INSTRUMENT AND COULD NOT HAVE BEEN FOUND BY READING, WHICH IS THE WHOLE
ARGUMENT FOR THIS TASK.** The sequence: build `adr/0137`'s field, point the panel at a world, and watch
the bankruptcy column read `—` where it had to read something. Every reading of the source said the
money leg subscribed — the ADR said so, the code said so, and this session said so in writing.
***`adr/0093` names this exactly: a description of the build is where to look and never what you
found***, and the sharper form is that **a prediction naming the wrong mechanism gets checked off by
the wrong evidence.** `adr/0137` is amended with both halves rather than superseded.

**F30 — ⚠ `adr/0137` SAYS *ONE FIELD* AND IT IS TWO, BECAUSE TASK 4 CREATED A WAIT TARGET IT COULD NOT
HAVE SEEN.** `RuleEvidence` carries the blocking Bin's **Resource** and its **`BinOwnerKind`**. When
`0137` was written every Bin that could stop a Rule belonged to the Building running it, so the
enclosing `BuildingEvidence` answered *whose*. A buyer now sleeps on the **District market row**
(`adr/0139`, `adr/0167`), so ***a Resource alone reports `sundries` for a tenant with an empty larder
and for a District with no sellers alike*** — one is a household's problem and one is the market's.
**Not a redesign**: both are ids, both are cold-path, and classification stays in the shell.

**F31 — ⚠ THE PANEL WAS PRINTING `ok` OVER A LIVE PRESSURE CLOCK, AND ON `minimal.toml` THE PAIR NEVER
OCCURS.** `RuleEvidence.Succeeded` means *armed rather than asleep* — its own summary says so — and a
woken-but-not-yet-refired Rule is armed with `StarvedSince` still set, because `World.Unlink` clears the
wait and only `RuleEngine.Fire` clears the clock. The dump printed **`ok` beside 459 missed firings**.
⚠ **It is a real third state and `EvidenceDump.State` now names it `woken`** — but it was *this* defect's
loudest symptom, and reading it as the benign transient it also is would have closed the investigation.
***The market world is what made a nine-milestone-old display ambiguity visible***, and a spinning buyer
is what made it loud.

**F32 — ⚠ `larder: sundries` WAS PRINTED AGAINST A BIN THAT WAS FULL.** `Blocking.Space` is a full
output, not an empty input, and the first cut of the `waiting on` column read as the exact opposite of
the truth. Caught by running the panel on `minimal.toml`, where a `restock` stops on `Space` — ***not by
reading the column back***, which is F29 again at one tenth the scale. `full:` is now its own word and
`EvidenceDumpTests` anchors on it.

**F33 — ⚠ BANKRUPTCY IS REACHABLE ON `provisioned.toml` AND WAS NOT BEFORE THE FIX, AND THE REASON IS
WORTH RECORDING BEFORE TASK 7 CHANGES IT.** On this file **stock is the binding constraint, not money**:
one shopfront per `TradeBlockStride = 8` blocks produces 8 sundries per 8 Ticks against four occupants
per dwelling each drawing 4 every 8, so the market is empty far more often than a purse is. With
stock-first blame (`adr/0167`), a broke buyer facing an empty market reads as **`market`** and not as
**`broke`**. ⚠ **Over 40,960 Ticks the defect gave 549,899 market blocks and zero money blocks**; the
fix makes money blocks appear in the ordinary run with no fixture at all. 🔴 **The three-way test
asserts the DISCRIMINATOR and never the volume**, because a test counting blocked Rules would have
passed against the defect throughout.

---

## Task 7's findings — the decline half, run out of order at the user's instruction

**F34 — 🔴 THE ORDER WAS WRONG AND THE USER CAUGHT IT, AND WHAT HE CAUGHT WAS ME ARGUING FROM AN
ABSENCE WHILE QUOTING THE RULE THAT FORBIDS IT.** Task 6 is `adr/0163`'s claim — *a shop in reach
counts as serving unless it is itself under failure pressure* — and the pressure it reads is the thing
task 7 creates. Designing 6 first meant designing a claim rule around a world where **no shop is ever
under pressure**, and the candidate I was defending had shops counted as serving whether or not they
held any stock. The user's words were *"why would we want to count resources served for a shop that has
none? that is like… completely wrong"*, and then, when I answered that the truthful model needed
mechanisms that do not exist: *"so are you recommending we sacrifice depth of the engine just because we
haven't built the mechanisms yet?"* ***That is [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)
verbatim, aimed at me, three messages after I had cited it.*** The rule's own sentence is *the answer to
"given X does not exist, should Y compensate?" is **build X***. Task 7 **is** X, it was already in the
milestone, and it was two tasks away. ⚠ **The failure mode is not that the rule was unknown — it is that
a rule about absences is invisible from inside the absence.** I was reasoning about what could be
written *given the world as it stood*, which is precisely the frame the rule exists to break.

**F35 — 🟢 IT NEEDED NO ENGINE CHANGE AT ALL, AND THAT IS THE FINDING RATHER THAN A CONVENIENCE.** The
whole of task 7 is **eleven lines of TOML**: a `{ resource = "money", owner = "business" }` Bin on the
`shopfront` kind, and a `[[rule]]` with one `local` money input and one `global` money output. No new
column, no new `purpose_tag`, no engine edit, no test helper. ⚠ **That is tasks 1 and 4 being load-bearing
rather than this task being small** — `adr/0166` gave a Business its own Rules and Bins, `adr/0143` gave
it a Bin *list*, and `RulesetLoader.ApplyTenancies` derives a Rule's subject from its **local** terms
against the kind's declared Bins. ***The trade owns the levy because the kind declares the till***, and
nothing had to be told about money for that to work.

**F36 — 🔴 THE FIRST LEVEL SHIPPED WAS PRESENT, CORRECT AND UNOBSERVABLE, AND ONLY A RUN SAID SO.** At
`amount = 2048` — ~6% of a measured median shop's revenue — the world produced **no money block of any
kind, at any Tick, on any shop**. Not *few failures*: zero. The Rule fired, the treasury filled, money
stayed conserved, and every assertion anybody would think to write passed. ⚠ **The level was raised to
8192 by the user against a measured median, not argued to**, and the failure it now produces is a tail —
**2 of 20 live shopfronts starving at end of run**. 🔴 ***This is milestone 9's land-value producer
exactly*** — built, correct, and observable by nothing — and it is the third sighting of that shape in
this corpus. **The instrument that found it was the falsification run**: the level was put back to 2,048
and the new test was watched to fail, which is `CLAUDE.md`'s *ships with a test that writes the violation
and watches it fire* applied to a **number** rather than to a diagnostic.

**F37 — ⚠ THE TEST PASSED FOR THE WRONG REASON TWICE BEFORE IT PASSED FOR THE RIGHT ONE, AND BOTH
WRONG REASONS WERE *A SHOP TOO NEW TO HAVE EARNED ANYTHING*.** A shopfront opens at a **zero** balance —
`adr/0148` instantiates the kind's trade and nothing on this file founds one — and it cannot sell until
the watershed gives it a District at `[districts] revisit_ticks`. So its first levies fail **by
construction**. Version one asserted *a levy Rule is blocked on a money Bin* and passed at Tick 6,144 on
the first shopfront ever raised. Version two required the shop to have held money and passed at the same
Tick, because the set was updated in the same sample as the check and *some* money is not *enough*
money. ***The assertion that means anything is: it once held at least one levy's worth, and later could
not pay one*** — threshold at the levy's own `amount`, set updated **after** the check. ⚠ **Both wrong
versions were green, fast and quotable.** A passing test whose subject is *new* rather than *declining*
is `adr/0093`'s failure mode wearing a green tick: right about the outcome, wrong about the trigger.

**F38 — ⚠ `0168` WAS FREE ON `main` AND IS NOT FREE, AND THE END OF THE COLUMN IS NOT EVIDENCE OF A
FREE NUMBER.** `ls docs/adr` ends at `0167`; `milestone-17-decline-and-cleared-land` already holds
`0168-a-decline-threshold-is-a-duration-and-the-premises-and-the-tenant-get-one-each.md`. This ADR is
`0169` and the gap closes at merge. ⚠ **`plans/0002` §F2 has recorded this hazard seven times in the
form *a gap is not a missing decision*, and never in the form that bit here.** Both halves are now on
that row. ***It was avoided by `git ls-tree` over every branch and would not have been avoided by
anything on `main`.***

**F39 — 🔴 THAT BRANCH REFUSES `condemn_after` BY NAME, AND THIS TASK'S SLACK ARGUMENT IS WRITTEN ON
IT.** `adr/0168` supersedes `adr/0053`'s authored unit: a Ruleset states `condemn_after_days` and
`tenancy_ends_after_days` in **Days**, and the old key is *refused at the parse site* because a file
keeping it would load clean and decline sixteen times too slowly. `rulesets/provisioned.toml` states
`condemn_after = 4` twice and its new `rates` header reasons from it — 4 missed firings at `rate = 1024`
is 4,096 Ticks of rope, which is what reaches past the watershed. ⚠ **The quantity survives the unit
change and the spelling does not**: 4,096 Ticks is 2 Days, so the migration's value is
`tenancy_ends_after_days = 2` and the header's arithmetic has to be restated, not just its key.
**Routed to the branch that owns it under [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)
rather than worked around here**, because the migration is theirs and `provisioned.toml` is simply one
more file in it.

**F40 — ⚠ I QUOTED A SENTENCE TO THE WRONG DOCUMENT, IN AN ADR, WHILE WRITING ABOUT CONSERVATION.**
*"A cost paid to nobody is a leak, not a cost"* is **`RulesetLoader`'s refusal 4**, cited by
[`adr/0117`](../docs/adr/0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md).
`adr/0024` argues conservation and never puts it that way. I attributed it to `0024` in `adr/0169`, in
`provisioned.toml`'s header and in the test's doc comment — three places, one act — and found it only
by grepping for the phrase after the links failed. ⚠ **The corpus's citation tests cannot reach this**:
they check that a link *resolves*, not that a quotation is *where it says it is*. 🔴 ***A quotation
attributed to a plausible neighbour is `plans/0012` **Cause 5** with the digits replaced by a
sentence*** — the caveat travelled, the attribution did not, and the wrong document now reads as having
said something load-bearing that it never said. All three are corrected.

**F41 — 🔴 A CORPUS CHECK HAD A SILENT EXEMPTION, AND IT WAS ON THE ONE CELL IT EXISTED FOR.**
`BoardShapeTests` rule 2 holds a board cell to three sentences. `Cells` split the row on `|` and
iterated `1 .. parts.Length - 2` — right for `| a | b |`, and it **drops a real cell** from `| a | b`,
which markdown also accepts. Exactly one row on the board omitted the trailing pipe, it was **row 1**,
and the cell it hid was **1,724 characters and seven sentences**. ⚠ **The check reported green the
whole time**, including the run this session made against it two tasks ago. ⚠ **That it was row 1 is
not luck**: the row somebody appends to daily is the row whose punctuation eventually goes wrong, so
***the cell a length check most needs to see is the cell most likely to break the parser feeding it.***
Fixed by normalising the row rather than by fixing the board, with the no-trailing-pipe case added to
the synthetic board so it fires; filed to [`0012`](0012-corpus-audit.md), which notes that every corpus
check parses markdown by hand and none of the others has been asked this question.

**F42 — 🔴 A SHOP CAN GO BROKE AND CANNOT BE TURNED OUT, AND TASK 7'S TEST ASSERTED OTHERWISE AND
PASSED.** `ZoneRuleEngine.Condemn`'s tenancy loop walks `_world.Occupants` — the **Households** in a
Building. A Business occupies through `World.BuildingBusinesses`, *"the second Occupant list"*
(`adr/0113`), and **nothing walks it**; `Worst` is typed `Handle<Household>`, so the gap is visible in
a signature. ***A Business's Failure Pressure therefore never reaches any threshold, at any Ruleset
value.*** ⚠ **Found by the milestone-17 session across four Ruleset variants with every threshold armed
on both kinds, and verified here by reading `ZoneRuleEngine.cs:386` and `:444`.**

🔴 **The half that matters is HOW THE ASSERTION PASSED.** Task 7's test asserted `ended > 0` off
`Zoning.Drain().Ended.Sum`, **which counts every tenancy end in the world**. What it counted were
**dwellings**: the tenant-side clock was `condemn_after` 4 against `restock`'s rate of 8 — **32 Ticks** —
while no District, and therefore no market to buy from, exists until `[districts] revisit_ticks` = 2048.
So every Household on this file starved *by construction* for 2,048 Ticks and was evicted after 32 of
them. ***The assertion's own failure message — "nothing was actually turned out by going broke" — was
TRUE ON THE DAY IT PASSED.***

⚠ **This is F37 a third time and it is no longer a coincidence.** That finding was *the test passed for
the wrong reason twice, and both wrong reasons were a shop too new to have earned anything*. This is the
same shape one level out: **a counter that aggregates over the whole world was read as though it were
scoped to the subject under test.** ***An assertion on a global counter is an assertion about the world
and not about the thing you named in the test's title***, and no amount of care inside the test body
fixes it — the fault is in the reading of what the counter counts.

⚠ **The repair is NOT to widen what `ended` counts**, which is what was silently happening. The
assertion is now **accumulated failure pressure on the broke shop's own levy Rule Instance** — `Stop`
sets `StarvedSince` and `Fire` clears it *totally* (`adr/0053`), so a clock still running at end of run
is a shop that has not paid since it began failing. That is a property of the subject and not of the
world. 🔴 **What a broke shop's eviction should DO is undecided and is filed in
[`0002`](0002-open-questions.md) §A owned by this milestone**: `Unplace` sends a Household to the
Unplaced Pool, and whether a Business goes to `UnpremisedTable` or is destroyed decides ***whether its
capital survives***, which is a money-conservation question and not a plumbing one.
