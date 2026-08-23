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

## Open decisions — **OPEN: 1 and 3 for milestone 25; 2 and 5 SETTLED; 4 TRAVELLED to 27**

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

### 2. ✅ SETTLED 2026-08-23 — **a Bin hangs off its OWNER**, [`adr/0143`](../docs/adr/0143-a-bin-hangs-off-its-owner-and-the-polymorphic-column-stays-unbuilt.md)

**A Household and a Business each gain a saved `BinHead`/`BinTail`; `BinTable` gains a saved
`OwnerNext` whose links are `Handle<Bin>`. `BinTable.Owner` stays a `HandleColumn<Building>`.**
🔴 **The shape was found in the build rather than argued** — `DistrictPoolTable`'s own remarks say the
existing answer is *"a Household and a Business each hold their Bin's handle on the **owner** row"*, and
that it became a join only because a District holds one Bin *per Good*. ***Owner-side ownership is the
pattern; the join is what it becomes at one cardinality; many-per-owner is a list.***
⚠ **Both alternatives were refused on where their cost lands.** The **polymorphic column** would edit
`Column.Fold`, `TargetIds` and `SaveHash.TargetsOf` — `adr/0112`'s machinery, and what lints 5 and 6
prove determinism with — and reopen the construction cycle; the **join** would have to ship the index
`DistrictPoolTable` was allowed to defer, because a tenant's `local` term is resolved **per term per
evaluation** and a Pool draw is cold. 🔴 **What it gives up is one direction**: a Bin cannot name its
owner, and `MoneyLedger.Of`'s *"whoever owns it — and this does not ask"* becomes load-bearing.
⚠ **No construction cycle and no two-phase bind** — both tables already take `bins` in their
constructors, which is how `Balance` works today.

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

1. ✅ **SHIPPED 2026-08-23 — a Bin hangs off its owner.** `HouseholdTable` and `BusinessTable` each gain
   a saved `BinHead`/`BinTail`; `BinTable` gains a saved `OwnerNext` whose links are `Handle<Bin>`;
   `BinTable.Owner` stays a `HandleColumn<Building>` and `OwnerKind` stays the discriminator.
   **Decision 2 settled first, into
   [`adr/0143`](../docs/adr/0143-a-bin-hangs-off-its-owner-and-the-polymorphic-column-stays-unbuilt.md).**
   🔴 **`Balance` became DERIVED on both tables and that was not in the plan** (**F12**) — a saved list
   plus a saved handle to one of its entries is two saved facts that can disagree, so `RebuildDerived`
   gained a balance rebuild and the handle is now maintained at its write site like any other derived
   column. ⚠ **There was no construction cycle to break** (**F11**): precondition 2 above was answering
   a question the chosen shape does not ask. 🔴 **The State Hash moved and four golden artefacts were
   re-recorded** — the world hash and both session traces — ⚠ **and `World.HashSeed`'s version byte was
   NOT bumped**, because that byte is for a change to the **fold** and this is a world with more columns
   in it. Findings **F11**–**F18** below. **2,064 assertion-tier tests green.**
2. ✅ **SHIPPED 2026-08-23 — a Household owns Bins, its Rules follow them, and the stagger mixes the
   tenant.** ⚠ **Entries 2, 3 and the stagger half of 4 are ONE task and `adr/0141` said so in its
   *Rejected* section** — **F19**, found before a line was written. A Ruleset says `owner = "occupant"`
   on a `[[building]] bins` entry; a **Rule's** side is **derived** from its own `local` terms and a
   mixed one is refused at load (138 → **140** refusal sites). `RuleInstanceTable` gains a saved
   `Household`, unset for a premises Rule; `World.FitOccupant`/`UnfitOccupant` open and close a
   tenant's Bins and Rules at the two ends of a tenancy; `RuleEngine`'s `local` resolve goes through
   `World.FindLocalBin`; `ArmingStagger` mixes the **subject's** monotonic id. 🔴 **Twelve Ruleset
   files edited**, `consume` is now `{ min = 1, max = 1 }`, and **`derived = "occupancy"` — the one
   declared Readout in the project — has lost its only caller.** 🔴 **The State Hash moved and four
   golden artefacts were re-recorded**; ⚠ **`World.HashSeed`'s version byte was NOT bumped**, because
   the fold did not change. **The shipped city now holds three times the stock** (**F25**), which is
   `adr/0141` being right rather than the file being tuned, and every edited Ruleset says so in its own
   header. Findings **F20**–**F29** below. **2,072 assertion-tier tests green.**
3. ✅ **FOLDED INTO 2 — see F19.** *Rules follow their Bins*, and a Rule's inputs and outputs are
   `BinRef`s, so there was never a moment at which the Bins had moved and the Rules had not. ⚠ **The
   numbering is left alone rather than closed up**, on `adr/0140`'s logic one level down: renumbering
   buys tidiness and costs every citation.
4. ✅ **SHIPPED 2026-08-23 — condemnation ends a tenancy and leaves the premises standing.**
   `ZoneRuleEngine.Worst(building, tenant, threshold, tick)` walks the Building's Rule Instances **once**
   and filters on the subject asked for — `default` is the premises, a handle is that tenant (**F31**) —
   so the premises are judged first and a demolition returns before any tenancy is looked at. A tenant
   past the threshold is evicted through `World.Unplace`, and the loop **restarts** because `Unplace`
   mutates the occupant list it is walking (**F32**). 🔴 **This removes a defect TASK 2 SHIPPED**
   (**F30**): pressure was taken across the Building's whole Rule list, so once a tenant had Rules of
   its own, ***one starving Household condemned the Building its two neighbours were living in*** — and
   no test failed, because nothing in the suite had two tenants failing differently.
   `ZoneCounter.Ended` counts it, separately from `Demolished`, through the census and `--zones`.
   **F28 is discharged and uncovered a second hole in the same panel** (**F33**): `RuleEvidence` and
   `BinEvidence` both gained `Tenant`, and the bin table now shows the **tenants' Bins too**, because a
   panel naming a subject while hiding that subject's state is worse than one naming neither.
   ⚠ **No golden artefact moved** — a tenancy ending is reachable only past `condemn_after`, which no
   golden session reaches — so this task is hash-bearing in principle and moved nothing in practice.
   🔴 **Nothing records *why* a tenancy ended and the trail that exists is the wrong shape** (**F35**);
   that channel is `adr/0130`'s and ships with task 5. Findings **F30**–**F35** below.
   **2,075 assertion-tier tests green.**
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

---

## What building it found

### Task 1, 2026-08-23 — **F11** to **F18**

**F11 — the construction cycle never existed for the shape that was chosen, and precondition 2 was
answering a different question.** `BinTable` is built before `BusinessTable` because a Business's
balance is a handle **into** `Bins` — which only bites if the *Bin* has to name the Business. Under
`adr/0143` it does not: both tenant tables already take `bins` in their constructors, exactly as
`Balance` has since milestone 10. ⚠ **`0039` **V12** costed a cycle that is real for the polymorphic
column and absent for the list**, and this document repeated it as a precondition without noticing the
dependency ran the other way.

**F12 — 🔴 `Balance` HAD to become derived, and nothing had said so.** `adr/0143` says the balance is
*"one entry in"* the list; what it does not say is that leaving `Balance` **saved** alongside would put
**two saved facts about one Bin** in the file, free to disagree after any edit to either. ***A saved
list makes every saved handle into it redundant***, so both columns moved to `Disposition.Derived` and
`RebuildDerived` re-finds them by walking each actor's own list for the money Resource.
⚠ **`DerivedRebuildAuditTests` noticed immediately** — 38 derived columns became 40 — which is the audit
doing its job on the first world that changed a disposition rather than added a column.

**F13 — 🔴 a latent ordering hazard in `FitBalances`, created by F12 and caught by reading rather than
by a test.** It opens a balance for any actor lacking one, and its emptiness test was
`Balance[slot].IsNone`. With `Balance` derived, that column is **empty until `RebuildDerived` runs** —
so any call ordered before the rebuild would have opened a **second** money Bin for every actor that
already had one, and `Invariant.MoneyIsConserved` would not have complained because both Bins are live
and empty. ***The test moved to the list, which is the saved truth and is populated the moment the
save is read.*** ⚠ **No test covers that ordering**, which is why this is a finding and not a fix.

**F14 — `DestroyHousehold` freed one Bin and now frees the list, and the walk has an order it must
keep.** `Bins.OwnerNext[binSlot]` is read **before** `Bins.Rows.Free`, because `Rows.Free` zeroes the
row — reading the link afterwards would return the unset handle and ***silently truncate the walk at
the first entry***, leaking every Bin after it. ⚠ **It is unreachable today** and becomes live at task
2, when a Household owns a second Bin: the failure would have been a leak nothing asserts against,
arriving one task after the code that caused it.

**F15 — 🔴 `plans/` IS NOT A CITING SOURCE, and an ADR cited only from a plan is still orphaned.**
`CitationTests.CitingFiles` enumerates `docs/` minus `docs/adr`, plus `CONTEXT.md`, `CLAUDE.md` and
`PROCESS.md` — and nothing else. `adr/0143` was cited twice from `plans/` and failed anyway. ⚠ **The
rule behind it is `adr/0042`'s** — *a decision must reach the design document that owns the mechanism* —
***and being forced to obey it is what found where the ADR belonged***: `05`'s list-classification rule
says a list is derived only if its **order** is recoverable, and assumes membership is. A tenant-owned
Bin names nobody, so ***the prior condition is the one that binds***, and `05` now states it.
**A mechanical check produced a design sentence.**

**F16 — `plans/0002` §F2's ADR count was stale for the FOURTH time**, and `CoverageMapTests` says so in
its own message: the rows have been present through every drift and it is the **count above them** that
rots. ⚠ **Second sighting of that defect in this file on this day** — §A's header was the first.
***A count is a fact stored in prose.***

**F17 — a test that is not about a column broke because a column changed disposition.**
`SaveHashTests.A_flipped_byte_in_the_body_is_refused_by_the_load` names `household.balance` **by
string** to find a byte to corrupt; `balance` stopped being saved, so the test failed on *no saved
column of that name* rather than on anything it asserts. Re-pointed at `household.bin_head`, which is
the saved handle a Household now holds into `BinTable`. ⚠ **The test is unchanged in what it proves**;
what it needed was any saved column, and it named one.

**F18 — the hash moved, four artefacts re-recorded, and the version byte deliberately left alone.**
`world-hash.txt` and both session traces. ⚠ **`World.HashSeed`'s version byte is for a change to the
FOLD** — the composition order's rules, `Randomness.Mix`, what a column contributes — and this is a
world with more columns in it, so bumping it would make the byte a change counter and stop it
distinguishing *a hash that means something different* from *a hash of something different*. The
README beside the baselines states that rule and it was followed rather than reasoned out.

### Task 2, 2026-08-23 — **F19**, found before a line was written

🔴 **F19 — TASKS 2, 3 AND HALF OF 4 ARE ONE TASK, AND `adr/0141` SAYS SO IN ITS *REJECTED* SECTION.**
This document ordered them as three and the ordering is wrong. ***Reading `rulesets/minimal.toml`
against `World.Fit` is what showed it, and it took one reading.***

**The dwelling declares three Rules and all three are `kind = "dwelling"`**, so `Fit(building, kind,
…)` creates one Rule Instance per Rule **per Building**. Two of them touch `sundries` — `restock`
(`outputs = [{ scope = "local", resource = "sundries" }]`) and `consume` (the same as an input) — and
only `upkeep`, on `repairs`, touches a Bin that stays with the premises.

***So the moment `sundries` belongs to a Household, a `local` term on a Building's Rule Instance has
no actor to resolve to*** — and a dwelling holds `occupants = 3` of them, so it is the payer problem
in miniature: **a list does not name one.** ⚠ **`adr/0141` refused this exact split in advance**:
*"Leave Rules on the premises and move only Bins. **Rejected** because a Rule's inputs and outputs are
`BinRef`s, so a Rule whose Bins all belong to a tenant is a tenant's Rule wearing the premises'
name."* ***The decomposition split what the ADR had already declined to split***, which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
failure mode with the direction reversed: a plan describing work rather than reading the record that
governs it.

🔴 **And the arming stagger comes with them rather than after them.** `World.ArmingStagger` mixes the
**Building's** monotonic id with the `RuleId` *"so that two Rules on one Building do not share an
offset."* Three Households in one dwelling each running `consume` would mix **the same Building id and
the same `RuleId`** — identical offsets, identical `NextTick`, the same Wheel bucket. ⚠ **That is not a
quality problem to tidy up in task 4; it is a correctness bug the moment task 2 lands**, and `0039`
**V13①** named it without noticing it was on this task's critical path.

⚠ **What does NOT merge is condemnation.** Task 4's other half is about *what dies* when pressure is
reached, and it is separable: a Building whose tenant starves can go on being demolished, wrongly, for
one more task without anything else breaking. ***It is a design defect rather than a blocker***, which
is the property that lets it keep its own entry.

**So group A is five entries and not six:** ① the owner column *(shipped)*, ② **the tenant's Bins, the
tenant's Rules and the tenant-mixed stagger, together**, ③ condemnation ends a tenancy, ④ the
unpremised pool and the emigration sink, ⑤ the closing task. ⚠ **The numbering below is left alone**
under `adr/0140`'s logic one level down — renumbering to close a gap buys tidiness and costs every
citation.

### Task 2, 2026-08-23 — **F20** to **F27**, the merged task built

🔴 **F20 — THE TENANCY NEEDED A RULESET WORD AND `adr/0141` DOES NOT HAVE ONE FOR A HOUSEHOLD.** That
ADR's *What changes* table puts the tenant's Bins on `[[business]]` — *"those the tenant keeps —
stock, the till"* — and **a Household has no kind table and never will**. ⚠ **So the marker had to go
on the `[[building]]` bin entry**: `{ resource = "sundries", capacity = 48, owner = "occupant" }`.
***That is not a workaround, and the ADR's own *Why* is why***: `DeclaredCapacity` stays keyed on the
**building** kind and *"needs no business kind at all"*, so the Bin must be declared where its ceiling
is declared whoever holds the level. 🔴 **The two halves of `adr/0141` cannot both be whole**, and
milestone 27 has to say which table names a Business's Bins — filed as a correction owed rather than
settled here, because nothing in group A can test the answer.

**F21 — a Rule's tenancy is DERIVED and `fills` is what makes a chain answerable in one comparison.**
`adr/0141` declined to split a Rule from its Bins, so an `owner` key on a `[[rule]]` would state a
second time what the terms already state. The loader computes it from the Rule's own `local` terms and
refuses a Rule addressing both sides — `adr/0050` at the parse site, since a term crossing an
ownership boundary is a **trade**. ⚠ **Counting `fills` as a local term is what removes the chain
walk**: `adr/0045` makes a fallback chain a ladder over *one* Bin, so a link relieving a tenant's Bin
from a premises Bin is the same mixed case arriving one link along.

🔴 **F22 — A TENANT'S BINS AND RULES LIVE EXACTLY AS LONG AS THE TENANCY, AND THAT IS FORCED RATHER
THAN CHOSEN.** A ceiling is a function of `(building kind, Resource)` and an unhoused Household has no
kind, so a Bin outliving its tenancy is a Bin `RebuildCapacities` cannot give a ceiling to. ⚠ **What
it costs is the tenant's stock, destroyed on eviction.** The **money** is not: it is unbounded, its
ceiling names no premises, and it stays with the Household under `adr/0054`. ⚠ **This is NOT the
answer for a Business** — `adr/0142` has one go on existing unpremised holding what it had — so it is
a data point for **open decision 1** and does not settle it. ***The Household case is answerable
because a Household has somewhere to go; that is the same asymmetry `DestroyBuilding` already
records.***

**F23 — a Ruleset reload frees every Rule Instance in the world, tenants' included, so `Migrate`
needed a second loop.** A Building-only refit leaves every Household holding Bins and running nothing
— a world that loads clean, has stock and never touches it. ⚠ **It is keyed on the Household and not
on the Building** because an unhoused one has no premises to refit through.

**F24 — `RebuildCapacities` became owner-walking exactly where
[`adr/0143`](../docs/adr/0143-a-bin-hangs-off-its-owner-and-the-polymorphic-column-stays-unbuilt.md)
said it would.** That record gave up *a Bin cannot name its owner*, and this is the first caller to
pay for it: the cases a Bin can answer alone stay in the Bin loop, and a tenant's Bin is reached from
the Household, whose `Dwelling` is the only route to a kind. ***A prediction in a Consequences section
came true within a day, which is the argument for writing them.***

🔴 **F25 — THE SHIPPED CITY NOW HOLDS THREE TIMES THE STOCK AND THE DRAW IS UNCHANGED.** `consume`
went from one Rule applied `occupancy` times to one Rule per Household, which is the same total draw;
`restock` went from **one greedy Rule filling one Bin of 48** to **three filling three**. ⚠ **The
Rule Instance count per dwelling went 3 → 7**, which is a `plans/0013` row and is filed there. ***The
supply change is `adr/0141` being right rather than the file being tuned*** — three Households do not
share a kitchen — and it is stated in every edited Ruleset's own header rather than left to be found.

**F26 — `MigrateBins` does not reach a tenant's Bins.** It rotates one *Building's* list, and a
tenant-owned Bin is not in one, so a Resource leaving the Ruleset leaves a Household holding a Bin
naming an id the incoming file does not declare. ⚠ **`RebuildCapacities` guards the read** — a
Resource past `ResourceCount` keeps the ceiling it has, which is `FitTreasury`'s answer to the same
situation — so nothing crashes and the Bin is simply inert. **Unbuilt, not refused** (`adr/0070`), and
filed rather than worked around (`adr/0073`).

**F27 — the refusal count went 138 → 140 and `RefusalCountTests` found it before I did.** ⚠ **It is
the corpus's only document-to-*code* check**, and this is the second milestone running in which it
was the thing that noticed. `adr/0048`'s enumeration gained its thirteenth recount alongside the
number, which is what that ADR asks for and what a bare number would not have bought.

🔴 **F28 — THE EVIDENCE PANEL NOW PRINTS THREE IDENTICAL `restock` ROWS AND CANNOT SAY WHOSE.** A
dwelling holding three Households runs three `restock`s and three `consume`s, and `RuleEvidence`
(`BuildingEvidence.cs:61`) **names no subject at all** — which `adr/0141` had already written down as
*"a field, not a redesign"*. ⚠ **It is left open deliberately and it is task 3's**, because **F19**'s
own cut put the *stagger* half of task 4 in this task and left the *condemnation* half — which the
subject field belongs to — its own entry. ***Taking it here would be reopening a cut I had just
argued for.*** `EvidenceDumpTests` was changed to take the first row **in each state** rather than the
first row with each name, and says in its own remarks that the claim is unchanged and the panel got
harder to read.

🔴 **F29 — A SHIPPED BUILDING NOW HOLDS ONE BIN, SO `bin.bin_next` STOPPED BEING WRITTEN ANYWHERE.**
Every shipped dwelling declares `sundries` and `repairs`; with `sundries` gone to the tenant, the
Building's own Bin list is one element long in every world the simulation builds on its own, and the
link in it is never non-zero. ⚠ **`DerivedRebuildAuditTests` caught it on the first run**, which is
exactly what that test is for: ***the column stayed derived, stayed rebuilt and stayed correct, and
stopped being provable*** — a coverage loss arriving as a side effect of a change three files away.
**Repaired with a fixture** (`TestRulesets.Stocked`, one kind with two premises Bins) **and not with a
second Bin on a shipped kind**, because what a shipped Ruleset declares is *content* and adding to one
so a test has something to walk is tuning the city to suit the instrument.

### Task 4, 2026-08-23 — **F30** to **F35**

🔴 **F30 — TASK 2 SHIPPED A DEFECT AND TASK 4 IS WHAT REMOVES IT, WHICH IS THE CUT WORKING RATHER
THAN FAILING.** `ZoneRuleEngine.Condemn` took the **longest pressure across the Building's whole Rule
list** and demolished on it. That was correct while every Rule in that list belonged to the premises.
The moment task 2 gave a tenant Rules of its own, the list was full of **other people's**, and ***one
starving Household condemned the Building its two neighbours were living in*** — three Households into
the Pool because one of them ran out of food. ⚠ **It was live for the length of one commit and no test
failed**, because nothing in the suite had a Building with two tenants failing differently;
`TenancyEndsTests.A_fed_tenant_keeps_its_tenancy_while_its_neighbour_loses_one` is that test and it
did not exist until today. ***A cut that lands a defect and the fix in adjacent tasks is not the same
as one that lands a defect***, but the gap is real and the test is what closes it.

**F31 — one walk answers both verdicts, and the filter is the whole difference.**
`Worst(building, tenant, threshold, tick)` walks the Building's Rule Instances once and skips every
instance whose `Household` is not the subject asked for — `default` selects the premises, a handle
selects that tenant. ⚠ **The alternative was two walks over the same list with two accumulators**, and
it was rejected for a reason that is not performance: the verdicts must be **mutually exclusive**, and
a single function returning *the worst instance belonging to X* makes that structural rather than
something the caller has to remember. **The premises are asked first and a demolition returns**, so a
condemned Building never also ends a tenancy.

**F32 — the condemnation loop restarts after every eviction, and this is the SECOND restart-scan this
milestone.** `World.Unplace` removes the Household from `Occupants`, which is the list `Condemn` is
walking — so continuing the walk past the removal reads a link that has just been rewritten. The loop
breaks out and starts again while anything ended. ⚠ **`UnfitOccupant` needed the same shape in task 2
for the same reason**, and there it was caught by *reading* rather than by a test, because `IndexList`
stores `next` slot-plus-one encoded and the naive version walked one row past every element. ***A walk
that mutates its own list is now a recognisable shape in this codebase and it has bitten twice in one
milestone.*** Filed here rather than fixed generically: a `WalkRemovable` helper is **unbuilt, not
refused** (`adr/0070`).

**F33 — F28 is discharged, and closing it uncovered a SECOND hole in the same panel.** `RuleEvidence`
gained `Tenant` and the rows now read `premises` / `tenant 1` / `tenant 2` — which is what F28 asked
for. ⚠ **But the panel was then showing Rules drawing from Bins it did not display at all**: the bin
table was assembled from `BuildingBins` alone, so every `restock` row named a `sundries` Bin that
appeared nowhere above it. `BinEvidence` gained the same field and the tenants' Bins now follow the
premises' in occupant order. ***A panel that names a subject and hides that subject's state is worse
than one that names neither***, because the reader now knows there is something to look for.
⚠ **The balance is deliberately excluded** — it is money, it is unbounded, and `Household finances` is
where a reader meets it.

**F34 — 🔴 THE TEST THAT CHECKS THE PANEL AGAINST THE SIMULATION'S OWN LISTS KNEW ABOUT ONE LIST.**
`EvidenceTests.A_buildings_answer_matches_the_lists_the_simulation_itself_walks` walked
`world.BuildingBins` and asserted the count matched `evidence.Bins.Length`. It failed the moment
tenant Bins were added — correctly, and **as an equality rather than as a subset**, which is the only
reason it caught anything. ⚠ **Had it asserted *every Building Bin appears* instead, it would have
stayed green while the panel grew a whole second source nothing checked.** ***The agreement test's
value is in the length assertion, not in the field comparisons***, and it survived a change to what it
agrees with only because somebody wrote the strong form.

**F35 — nothing records WHY a tenancy ended, and the trail that exists is the wrong shape for it.**
`CondemnationTrail.Record` names a **Lot**, a kind and the condition behind a demolition; a tenancy
that ends leaves the Lot, the kind and the Building exactly as they were, so an entry there would be
**a demolition record for a Building still standing**. The census counts the event
(`ZoneCounter.Ended`) and the evidence panel shows the pressure while the tenancy lasts, but once the
Household is in the Pool there is nothing to ask. ⚠ **The channel that carries *why is this Household
unhoused* is [`adr/0130`](../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)'s
and ships with the Pool's give-up bound**, which is task 5. Filed rather than improvised
(`adr/0073`) — a second trail keyed on the Household would be the wrong thing to have to delete.
