# 0040 — The Business is the actor and the Building is premises

**[`06`](../docs/06-roadmap.md) milestone 25 — tasks 1–5 and the closing task.** Ungated. ⚠ **This
document also holds milestone 27's tasks 6–9**, kept as written after 25 was capped at group A on
2026-08-23; **one plan document, two milestone rows**, exactly as
[`0037`](0037-goods-between-buildings-the-district-pool.md) holds milestone 26's. Scoped by session **V**,
[`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md), which ran and closed
2026-08-22 into [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)
and [`adr/0142`](../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md).

> ***A bakery holds its own flour, its own bread, its own till and its own oven. The building holds a
> street address and some floors.***

⚠ **On its main axis this is a CORRECTION and not a design change**, and that is what sizes it.
[`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)
decided it, [`adr/0114`](../docs/adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)
wrote the target shape — ***"`World.FindBin` takes an owner rather than a Building slot"*** — and the
build never arrived. ***A revisit trigger that has already fired is a decision waiting to be finished.***

---

## Status

🔴 **NOT STARTED. Decomposed 2026-08-23 into ten tasks in two groups, and CAPPED AT GROUP A the same
day, with the user in the room.**

**`0039` V18 found the cleavage plane and declined to place it** — *"the sitting decides it and milestone
25 does not necessarily build it."* This document ordered the whole of it, **marked** the cut, and the
cut was then **made at task 5**. 🔴 **So milestone 25 is tasks 1–5 plus the closing task, and
[`06`](../docs/06-roadmap.md) REWRITES ITS RISK** to what group A actually retires — ***that a Rule
Instance names premises rather than an actor, so no money term can resolve to a payer.*** ⚠ **Group A
makes the actor NAMEABLE; it does not make one EXIST.**

🔴 **Tasks 6–9 are milestone 27 — *the Business is a thing the city contains*** — which is **25's
original risk**, unretired. ⚠ **They are kept below as written**, because they are the specification 27
inherits (the same treatment `0037` gave its tasks 7–10 when milestone 12 was capped). ⚠ **Open decision
4 travelled with them** and is now 27's.

⚠ **This is the second cap in two days and it is the numbering scheme working rather than failing**
([`adr/0140`](../docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md)):
27 is allocated **next-free** and sits **between 25 and 26** by row order. ***And unlike milestone 12's,
this cap was placed by decomposition before any code was written, rather than by a milestone running out
of road.***

⚠ **The census under *What the build already holds* was taken 2026-08-23 against the tree, not read off
`0039`.** It **corrects session V in two places** (**F1**, **F2**), **confirms it in four**, and
**found one thing neither ADR answers** (**F3**) — which is now open decision **1** and is the reason
this document exists rather than a task list written straight from the ADRs.

---

## The named risk, as `06` states it

**That the economic actor does not exist in the build, so no Rule can spend money.** A purchase needs a
payer; money lives on a **Business**; a Rule Instance names a **Building**; a Building holds a **list**
of Businesses, and a list does not name an actor.

⚠ **Milestone 10 predicted this eight milestones early and wrote it where nothing could find it** —
`BusinessTable`'s own doc comment. **Every mechanical check in `tests/Borough.Tests/Corpus/` is
document-to-document**, so a blocker named only in a doc comment is invisible to all of them. Filed in
[`0012`](0012-corpus-audit.md).

---

## What the build already holds — surveyed 2026-08-23

**Counted against the tree. Every figure below is a call-site count**, and ⚠ ***a count of call sites is
not an argument about what the city is*** (`0039`'s closing line) — it sizes the work and settles
nothing.

| Symbol | `src/` | `tests/` | Note |
|---|---|---|---|
| `BinTable.Owner` | 5 | 1 | `HandleColumn<Building>`, `BinTable.cs:100`. Saved, dangling-checked |
| `BinTable.OwnerKind` | 8 | 4 | The discriminator that already exists |
| `World.CreateBin` | **1** | **27** | One `src/` caller — `Fit`, `World.cs:2014` |
| `World.FindBin` | **3** | **17** | |
| `BuildingTable.BinHead`/`BinTail` | 4 | 0 | `Derived`; everything goes through the `IndexList` view |
| `World.BuildingBins` | 9 | 7 | |
| `RuleInstanceTable.Building` | **6** | **0** | 1 write, 5 reads. **No test reads it at all** |
| `World.BuildingRules` | 8 | 8 | |
| `World.ArmingStagger` | 1 | 0 | `World.cs:2056`. Single call site, in `Fit` |
| `World.CreateBusiness` | **0** | **12** | ***Nothing in the simulation creates a Business*** |

### What exists and works

- **`BinOwnerKind` has all six members and every one is live** — `None`, `Building`, `Household`,
  `Business`, `Treasury`, `District`. ***The enum arrived; the column did not.***
- **`BusinessTable` exists** with three columns: `Building` (`Reference.Severable`), `Balance`
  (`Reference.Required`), `BuildingNext`. **No kind, no bins list, no jobs.**
- **`World.BalanceOf(Handle<Business>)` exists** (`World.cs:976`) and has **zero `src/` callers**.
- **`World.Depart` and `World.Endow` are the two doors** and they are the same door — `MoneySupply.Issued`
  is *"money that has entered this world, net of anything that has left it."* **`adr/0142`'s sink needs
  no invention.**

### 🔴 Precondition 1 — **nothing creates a Business**

`World.CreateBusiness` (`World.cs:874`) has **no `src/` caller**, and `DestroyBuilding`'s own comment
says so. **Group A is exercisable anyway** — `GoldenFixtures.cs:531–532` already puts **two Businesses
in one Building** deliberately (**F7**), so tenant-aware work has a fixture to run against. **Group B
does not have that luxury.**

### 🔴 Precondition 2 — **the construction cycle is real**

`Bins = new BinTable(…, Buildings)` at `World.cs:129`; `Businesses = new BusinessTable(…, Buildings,
Bins)` at `World.cs:166`. The constructor comment states the constraint outright: *"since `adr/0114` a
Household and a Business each hold a saved handle INTO this table — their balance — so this one has to
exist before either constructor can name its Rows."* ***Pointing Bins at Business closes the loop***
(**F4**). ⚠ **Construction order is not composition order**, so re-ordering is free — but only while the
graph stays acyclic, and this makes it cyclic.

### ✅ A correction, made on the day — the Rule half is **cheaper** than `0039` costed it

See **F1**. It is the one place the census made the milestone smaller.

---

## Open decisions — **OPEN: 1, 2 and 3 for milestone 25; 4 TRAVELLED to 27; 5 SETTLED**

### 1. 🔴 What is an unpremised Business's Bin capacity? — **found by decomposition, answered by neither ADR**

**`adr/0141`**: capacity is the premises' and stays keyed on the **building kind**, read at the creation
site from `Buildings.Kind[buildingSlot]` (`World.cs:2093`) and again on every rebuild
(`RebuildCapacities`, `World.cs:2670`). **`adr/0142`**: an unpremised Business is a **legitimate steady
state** — it joins a pool and waits under a give-up bound.

🔴 ***So a Business with stock and no premises owns Bins whose capacity is declared by premises it does
not have***, and the code already has an answer nobody chose: `RebuildCapacities`'s `TryResolve` returns
`kind = 0` on a severed handle, and `DeclaredCapacity` maps an undeclared kind to **0**. **A rebuild
would set an unpremised Business's stock Bins to capacity 0 while they hold stock.**

⚠ **This is not `adr/0043`-measurable and it is not a preference.** It is forced the moment `adr/0141`
and `adr/0142` are both true, and it is the first thing that would have gone wrong. Candidates, none
argued yet: stock is **sold or lost on eviction** so an unpremised Business holds only its till; capacity
becomes **unbounded** while unpremised, as the treasury and Pool Bins already are; or the Bin's capacity
is **saved rather than derived** for tenant-owned Bins, which contradicts `adr/0064`.

### 2. How does a Bin name a non-Building owner? — **the cycle, and three exits**

⚠ **One has precedent shipped six days ago.** `Space.DistrictPoolTable` (milestone 12 task 5) is a
**saved join** naming which Bin belongs to whose Pool, chosen *"because `BinTable.Owner` cannot hold a
District and the alternative — a derived list — is only derivable when the element names its owner."*
***The same sentence is true of a Business.*** The other two: a **two-phase `Rows` binding**, and a
**polymorphic handle column**, which `BinTable.cs:91–99` argues against **by name**.

### 3. Does a Business share `[placement] gives_up_after_days`, or state its own?

**`adr/0142` leaves this open in as many words**, and gives the argument against sharing: ***a shop
looking for premises and a family looking for a home are not obviously the same patience.*** ⚠ **If it
is a second number it is a second [`0002`](0002-open-questions.md) §D row on the day it is written**
([`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)).

### 4. 🔴 What capitalises a Business? — **a number, not a shape**

*"**Nothing funds one.** A Business opens with an empty Bin and there is no door that pays it."* A
Household's opening balance is an **authored band on its Hinterland**; a Business has neither a band nor
a gate to arrive through, and `World.Endow` has **no Business overload** (**F10**). ⚠ **Hash-bearing, so
it needs a named ratifier — a machine, a world and a quantity — and a category is not a name.**
🔴 **It blocks task 8, and it blocks milestone 26 exactly as the payer did.**

### 5. ✅ SETTLED 2026-08-23 — **the cut is made at task 5, and group B becomes milestone 27**

**Deferred to decomposition on purpose, and decomposition's answer was the two groups below.** Group A
needs nothing that does not exist; **every entry in group B needs something that does.** ⚠ **The
consequence taken with it, and it is not a side effect**: 25's risk is **rewritten**, because group A
makes a payer **nameable** and does not make an actor **exist** — and a milestone must name a risk it
actually retires. 🔴 **Milestone 26 gains a blocker rather than losing one**: it was *blocked on 25* and
is now blocked on **25 and 27**, which is the honest reading of what was always true.

---

## Tasks

**Decomposed 2026-08-23. Ordered by what the next task needs.**

⚠ **Read the ordering claim before trusting the order.** ***Group A is a repair and group B is a
feature.*** Group A moves Bins and Rules onto the tenant that already exists, is driven end to end by
the **Household** on `minimal.toml`, and needs no new kind, no new pass and no new number. Group B
builds the Business as a thing the city contains. **The cleavage between task 5 and task 6 is
`0039` V18's, and it is where this milestone can stop.**

### Group A — the Occupant repair. **Nothing here needs anything that does not exist.**

1. **A Bin names a non-Building owner.** The type-level move: `BinTable.Owner` off
   `HandleColumn<Building>`, both `BinTable.Create` overloads, and `RebuildDerived`'s owner switch
   (`World.cs:1676–1719`). **Blocked on decision 2** — the construction cycle has to be broken first.
   ⚠ **`RebuildDerived`'s `Household` and `Business` cases are a shared no-op today and the comment says
   why**: the actor's balance is a *single* saved handle, so the link comes out of the file already made.
   ***That premise dies at task 2*** (**F5**), so this task's real content is turning the no-op into a
   rebuild. ⚠ **The city does not change here** — every Bin is still owned by a Building when this lands.
2. **A Household owns Bins — `sundries` moves to the tenant.** `adr/0141`'s line, applied to the one
   Occupant that already exists. 🔴 **This changes what the city does**: `consume` becomes one Rule per
   Household instead of one Rule applied `occupancy` times, and `derived = "occupancy"` — **the one
   declared Readout in the project** — loses its only caller. ⚠ **Twelve Ruleset files**, not one
   (**F6**). ⚠ **Capacity stays keyed on the building kind** (`adr/0141`), so the creation site needs
   **both** the owner and the premises — a Household reaches its through `Households.Dwelling`. ⚠ **Do
   not read `minimal.toml`'s shared larder as evidence for anything**: three Households in one dwelling
   do not share a kitchen, the file's first line says it models no city, and under `adr/0070` a shortcut
   is not evidence.
3. **Rules follow their Bins.** ⚠ **Cheaper and differently shaped than `0039` V10 costed it** (**F1**):
   the six sites it named all reach the Rule Instance through the **derived `BuildingRules` list**, not
   through the saved `Building` handle, whose only consumers are `RuleEngine` (4) and the rebuild (1),
   with **zero test readers**. ***So the blast radius is the list, not the column.***
4. **The tenant-aware stagger and the tenant-aware condemnation.** Both hash-bearing, both
   `LEGIBLE CAUSE`. `World.ArmingStagger` (`World.cs:2056`, **one** call site) mixes the **tenant's**
   monotonic id — *two Businesses running one Rule* reopen on the other axis the collision the Building
   mix was there to prevent. ⚠ **`Condemn`'s pressure mechanism does not change**: the verdict is already
   an OR and the walk continues only for attribution (`ZoneRuleEngine.cs:346`). ***What changes is what
   dies*** — a failing tenant's Rules end the **tenancy** and leave the premises standing. `RuleEvidence`
   (`BuildingEvidence.cs:61`) **names no subject at all**, so this is ***a field, not a redesign***.
5. **The unpremised pool and the emigration sink** —
   [`adr/0142`](../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md).
   `DestroyBuilding` (`World.cs:3644`) stops unlisting-and-freeing-nothing; an orphaned Business joins a
   pool, waits, and if nothing tenants it **departs and takes its money out of the city**. ⚠ **The
   mechanism exists** — `Depart` subtracts from `MoneySupply.Issued` and `Endow` is its mirror — and it
   lands on the **built** channel, since `Depart` refuses a *housed* Household and an orphaned Business
   is unpremised by construction. **Blocked on decisions 1 and 3.** 🔴 **The bound goes in on the day
   the collection does** (`adr/0006`), and the collection is task 1's. ⚠ **Exercised by fixture**
   (**F7**), because nothing creates a Business until **milestone 27's task 8** — ***so this task ships a
   sink for a collection that cannot yet fill***, which is `adr/0142` applied to itself: **the bound goes
   in on the day the collection does**, and the collection is task 1's.

> ### ✅ THE CUT, MADE HERE 2026-08-23 — `0039` **V18**'s cleavage
>
> 🔴 **MILESTONE 25 ENDS AT TASK 5.** Everything above is driven by the Household and needs nothing new.
> Everything below needs something that does not exist. ⚠ **The dependency runs one way only**: jobs
> cannot move to the employer without something that creates employers, and nothing creates an employer
> without a **kind** to create it from. ***The reverse is not true***, which is what makes the cut
> available at all.
>
> ⚠ **The cost was taken with eyes open and is recorded rather than discovered later: milestone 26 stays
> blocked**, because a purchase needs a seller that exists and is capitalised. **This is a second
> milestone capped at a cleavage plane**, which is what produced 25 and 26 in the first place —
> ***but this one was capped by decomposition before any code was written, and 12 was capped by
> running out of road.***

### Group B — **MILESTONE 27**, *the Business is a thing the city contains*. **Every entry needs something that does not exist.**

**Moved out of 25 on 2026-08-23 and kept here as written**, because they are the specification 27
inherits — the treatment [`0037`](0037-goods-between-buildings-the-district-pool.md) gave its tasks 7–10
when milestone 12 was capped. ⚠ **The task numbers do not change**, under
[`adr/0140`](../docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md)'s
own logic applied one level down: renumbering to close a gap buys tidiness and costs every citation.
🔴 **27 carries 25's ORIGINAL risk** — *that the economic actor does not exist in the build* — and
**open decision 4, the capitalisation band**, travelled with it.

6. **The `[[business]]` kind table.** `adr/0141`'s second kind table — *the same shopfront hosts a
   bakery, then a barber, and the building did not change.* A loader section, a kind byte on
   `BusinessTable`, a second kind namespace in `RulesetNames` (which carries exactly four maps today).
   ⚠ **Smaller than `0039` V11 feared**, because **V22** removed `DeclaredCapacity` from the list of
   things needing a business kind. ⚠ **`RulesetLoader.cs:2026` refuses `sweeps = "business"` and
   `sweeps = "building"` in ONE case block**, with a test asserting both by `[InlineData]` (**F9**) —
   ***a refusal that speaks for two subjects, and only one of them is being answered.***
7. **Jobs and shift hours move to the employer.** `adr/0026`: *a fill rate is a property of an
   employer.* 🔴 **The Workplace stops being a Building**, and `adr/0101` already said so — a Shift start
   hour belongs to the **Workplace**, and a Workplace is where you are employed. ⚠ **The Citizen still
   stores nothing**, and the property `adr/0101` was defending gets *stronger*: *"changing employer
   changes their day with no write"* becomes **literally true**, where today a Citizen changing employer
   inside one Building changes nothing at all. ⚠ **Twelve Ruleset files again**, and `jobs = 8` sits on
   the **dwelling** kind in every one.
8. **What creates a Business, and what capitalises one.** **Blocked on decision 4** — a hash-bearing band
   with a named ratifier, or it is not a number. ⚠ **`adr/0069` is preserved rather than strained**:
   construction houses **nobody**, and ***a pool plus a placement pass is what not auto-tenanting looks
   like.*** A Zone Rule that auto-tenants a shop is that rule broken on the commercial side.
9. **A Rule can read a Business's balance.** `Readouts.Read` **throws** for `Readout.Balance`
   (`Readouts.cs:206`) and `ReadHousehold` is the only balance readout in the project. ⚠ **`ScopeOf` is a
   binary and `DeclaredSet` is exactly two** (**F10**), and the class remark says two entry points is
   deliberate rather than incidental — so a third actor is **a third scope and a third entry point**.
   ***This is the payer blocker arriving on the read side rather than the spend side.***

### Closing

10. **Something to look at, and the long acceptance run.** The definition of done requires both. ⚠ **The
    thing to look at is a tenancy** — a shop failing and the premises surviving it is the whole milestone
    in one frame, and it is `LEGIBLE CAUSE` becoming visible. ⚠ **The long run's new obligation is the
    unpremised pool**: `adr/0006` asks whether a collection trends, and task 5 creates the project's
    newest one.

---

## What this milestone must not do

- **Must not make `Scope.Pool` resolve.** That is milestone **26**, and it is blocked on this one.
- **Must not build freight, Upkeep, or `adr/0088`'s `min()`.** All three are **UNPLACED** —
  [`adr/0136`](../docs/adr/0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md),
  [`adr/0138`](../docs/adr/0138-freight-is-unbuilt-so-the-min-follows-it-and-neither-is-at-twelve.md) —
  and ***three mechanisms were already found parked on an assumption their authors did not check.***
- **Must not auto-tenant.** `adr/0069`, and it is the commercial side of a rule that already exists.
- **Must not cite hash movement as a reason to defer, narrow or split.**
  [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md).
  **The State Hash moves and every golden baseline re-records** — `RuleInstance.Building` and
  `BinTable.Owner` are saved handles that fold target ids. ***What is owed is attribution in the commit
  subject.***
- **Must not settle decision 1 by pricing it**, and must not settle any of the five by argument where a
  measurement would do ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)).

---

## Definition of done

`CLAUDE.md`'s cumulative list, refined for this milestone:

- **`dotnet test` — the whole suite, unfiltered — green on the reference machine.** ⚠ **The commit gate
  stays the assertion tier** ([`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)).
- **The invariants pass**, including `DerivedRebuildAuditTests` — ⚠ **which is the only thing that asks
  whether a `Derived` column is actually rebuilt**, and task 1 turns a no-op into a rebuild. ***A column
  declared `Derived` while the structure that derives it lives outside the `World` is not derived state,
  however it is declared.***
- **The long-run test passes** — and **the unpremised pool is a new collection with a bound to prove**.
- **There is something to look at** — a tenancy ending while the premises stand.
- **The risk is named and retired** — ⚠ **and it is the REWRITTEN risk**: a Rule Instance names an
  **Occupant** rather than premises, so a money term has an actor to resolve to and a tenant's Bins
  leave with the tenant. 🔴 **It is NOT *the economic actor exists in the build***, which needs something
  that creates a Business and is **milestone 27**. ***A milestone must name a risk it actually
  retires***, and this sentence is the second time in two days that rule has forced a rewrite.

---

## What decomposition found

**F1 — 🔴 the Rule half is cheaper than `0039` V10 costed it, and differently shaped.** V10 named *"5
direct reads and 7 implicit, the implicit ones being the interesting set"* — `Fit`, `Migrate`'s drop
pass, `DestroyBuilding`, condemnation's pressure walk, `Evidence.OfBuilding` and
`NoBuildingRunsRulesItsKindDoesNotDeclare`. ***Every one of those six reaches the Rule Instance through
the derived `BuildingRules` list, not through the saved `Building` handle.*** The handle has **5 readers
— `RuleEngine` ×4 and `RebuildDerived` ×1 — and zero test readers.** **So the blast radius is the list,
and the column is nearly free.**

**F2 — `NoBuildingRunsRulesItsKindDoesNotDeclare` is an INVARIANT, not a test.**
`WorldInvariants.cs:973`, registered at `:51` in the **`EndOfRun`** tier. `0039` V10 listed it among
call sites as though it were a test. ⚠ **It changes which tier notices the move** — an end-of-run
invariant fires on the balance runs, not on the 42-second gate.

**F3 — 🔴 THE CAPACITY PATH HAS A SEVERABLE HOP AND NEITHER ADR ANSWERS IT.** See open decision **1**.
`adr/0141` keeps capacity keyed on the building kind at the creation site; `adr/0142` makes *unpremised*
a legitimate steady state; `Businesses.Building` is `Reference.Severable`. ***An unpremised Business owns
Bins whose capacity is declared by premises it does not have***, and the existing failure path silently
resolves it to **0** while the Bin holds stock. ⚠ **`0039` V14 found the severable hop on the EMISSION
path and stopped there** — the same hop is on the capacity path, which is load-bearing every rebuild
rather than only when a Rule emits.

**F4 — the construction cycle has three exits and one shipped six days ago.** `Space.DistrictPoolTable`
— a **saved join** naming the owner — was chosen at milestone 12 task 5 on an argument that is true of a
Business word for word: *"a derived list is only derivable when the element names its owner."* The other
two are a two-phase `Rows` binding and a polymorphic handle column, and `BinTable.cs:91–99` argues
against the latter **by name**.

**F5 — `RebuildDerived`'s `Household` and `Business` Bin cases are a shared no-op, and its comment names
the premise that task 2 kills.** The actor's balance is a **single** saved handle, so the link comes out
of the save already made and nothing needs rebuilding. ***The moment an actor owns a second Bin, that
stops being true.*** **Task 1's real content is that a no-op becomes a rebuild** — and a rebuild needs
bin→owner, which is **F4**'s cycle.

**F6 — `derived = "occupancy"` has no second caller, so `adr/0141`'s revisit trigger has NOT fired — and
it stands in ALL TWELVE shipped Rulesets.** The same `consume` rule in every one. ⚠ **`congested.toml` is
a golden baseline artefact since milestone 7 task 8**, so editing it moves a recorded **Ruleset content
hash** in three fixture constants, two session logs and two trace headers — ***which is a file
fingerprint and not a State Hash.***

**F7 — group A is exercisable by fixture and group B is not.** `World.CreateBusiness` has **zero `src/`
callers and 12 test call sites**, and `GoldenFixtures.cs:531–532` already puts **two Businesses in one
Building** deliberately — `0039` **V8** confirmed. ***So the tenant-aware stagger and the tenant-aware
condemnation can be driven today, by the two tenants a golden fixture already contains.***

**F8 — the columns are cheap and the TEST SURFACE is the milestone.** `BinTable.Owner` and
`RuleInstanceTable.Building` are **6 sites each**. `CreateBin` and `FindBin` are **4 sites in `src/` and
44 in `tests/`**. ***The repair is small and the churn is where the assertions are***, which is the
opposite of how `0039` V9 reads.

**F9 — the `sweeps` refusal speaks for two subjects and only one is being answered.**
`RulesetLoader.cs:2026` refuses `"business"` and `"building"` in **one case block**, with
`PolicyLoadTests.cs:120` asserting both by `[InlineData]`. `PolicySubject` has four members and **two are
declared and unreachable**. ⚠ **Making the Business half reachable leaves a refusal whose text argues for
a subject that is still refused**, and a message that has become half-true is `adr/0093`'s failure mode
in a string literal.

**F10 — the actor asymmetry is in three places, not one.** `World.Endow` is **Household-only** — there is
no Business overload. `Readouts.ScopeOf` is a **binary** and `DeclaredSet` is exactly two, with a class
remark saying two entry points is deliberate. And `World.BalanceOf(Handle<Business>)` **exists with zero
`src/` callers**. ***The Business is half-present in three subsystems and load-bearing in none.***
