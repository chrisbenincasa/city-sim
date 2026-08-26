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

### 2. 🔴 Which Bin does a purchase blame?

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

### 3. 🔴 What is a seller, and how is one chosen?

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
3. **The Provider Ruleset** — **W-Q4, and the world.** A second `[[building]]` kind, its `[[business]]`
   trade, a second `[[zone_rule]]` on the trade bit, the input and output Goods. On `twinned.toml`'s
   two-lattice base, because `[districts]` needs two centres and `RefuseUnpricedGoods`
   (`RulesetLoader.cs:5194`) then demands a `[[hinterland]] prices` entry for **every** Good.
   ⚠ **It loads and does not run**, and that asymmetry is the task's own acceptance test: `TryScope`
   accepts `pool`, `RuleEngine.Bin` throws. ⚠ **Its header must say the stride is not how any city is
   zoned** — `adr/0165` requires it. **Moves no hash of its own**; it is a new file.
4. **The purchase — `Scope.Pool` resolves.** The District from the Building's Cell, then a seller,
   then Good one way and money the other, settled atomically with the Rule. Owed with it: the
   `(District, Resource) → Bin` index `DistrictPoolTable`'s doc says **task 7 owes**; a `Bin →` market
   row lookup for the price; and `DistrictPoolTable.Consumed` gets its **first writer**.
   ⚠ **Open decisions 2 and 3 land here.** 🔴 **`Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool` is
   armed by this task** — `World.cs:3639` says it is safe today *"because `Scope.Pool` throws"* and
   names this as *"what will fail on the day task 7 opens the scope."*
5. **Evidence — [`adr/0137`](../docs/adr/0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md)'s
   two halves.** `RuleEvidence` gains the blocking Bin — it has `Blocking` and `ConditionId` and no
   Bin today (`BuildingEvidence.cs:82`) — **and** the money check produces a verdict *naming the money
   Bin* rather than failing without subscribing. ⚠ ***The second half is the one that gets skipped***,
   because nothing about the money leg is authored and so nothing about it is prompted.
6. **`adr/0163` tier 1 — demand for a shop.** The reach query over `WalkScratch.SettleAll`, the elapsed
   sum, and the claim. **Replaces `UnplacedPool.Count == 0` for the trade rule only**; the housing rule
   stays tier 0. ⚠ **Open decision 1 lands here**, and both §D2 numbers are chosen here and move to
   §D1. **A [`0013`](0013-tick-budget.md) row is owed on the day, with a measured multiplicand and
   not a guessed one.** Corrects `ZoneRuleEngine.Create`'s summary to say **which rule** it describes
   ([`0012`](0012-corpus-audit.md)).
7. **The decline half.** A money-consuming Rule on the trade, so an empty balance is `Blocking.Supply`
   on a money Bin — which is pressure, is **bankruptcy** rather than starvation, and is the world
   [`0037`](0037-goods-between-buildings-the-district-pool.md) task 10 has been waiting for. ⚠ **A
   shop nobody buys from is IMMORTAL** and this is why: unsold stock stops on `Blocking.Space`, and
   `RuleEngine.Stop` clears the pressure clock for every reason but `Supply`.
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
