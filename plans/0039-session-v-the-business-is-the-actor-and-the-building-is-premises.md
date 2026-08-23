# 0039 — Session V: the Business is the actor and the Building is premises

✅ **RAN AND CLOSED 2026-08-22, with the user in the room.** The brief, the record and the close.

**Four of the five questions closed into two ADRs** — [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md) *(a tenant owns what leaves with it and the premises own the capacity)* and [`adr/0142`](../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md) *(an unpremised business emigrates)*. 🔴 **The fifth did not close and changed type on the way**: *what creates a Business* turned out to be ***what CAPITALISES one***, which is a hash-bearing number and belongs to `plans/0002` §D under `adr/0052` rather than to a sitting. ⚠ **Everything below the brief is the sitting's own record and the findings are numbered V1–V31.**

**What is under stress is the BUILD, and `CONTEXT.md` in two places.** On its main axis this is a
**correction rather than a design change**, and that is the finding that sizes everything else:
[`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)
decided it, [`adr/0114`](../docs/adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)
wrote down the target shape, and **the build never arrived.**

> ***A bakery holds its own flour, its own bread, its own till and its own oven. The building holds a
> street address and some floors.*** That sentence is the whole session.

---

## Why it opened

**Milestone 12 task 7 — now milestone 26 — is the purchase, and a purchase needs a payer.** Money lives on a Business;
`MoneyLedger` resolves `Treasury / Household / Business` and nothing else. A Rule Instance names a
**Building**. A Building holds a **list** of Businesses. ***So the first money term a Rule fires has no
actor to resolve to.***

🔴 **Milestone 10 predicted this exactly, eight milestones early, and wrote it where nothing could
find it** — `BusinessTable`'s own doc comment: *"How many Businesses occupy one Building is
**undesigned** … it blocks the first money term a Rule fires on a workplace, because `local` money must
resolve to **an** actor and a list does not name one."* ⚠ **It is a doc comment**, and every mechanical
check in `tests/Borough.Tests/Corpus/` is **document-to-document**, so ***a blocker named only in a
doc comment is invisible to all of them*** — [`0012`](0012-corpus-audit.md)'s known surface, second
sighting after `adr/0137`'s.

---

## The claim

> **The Business is the economic actor. It holds the inventory, holds the balance, and runs the Rules.
> The Building is premises: a Lot, an address, a footprint, and a capacity for tenants.**

---

## What the corpus already says — this is the cheap half

**V1 — `adr/0113` decided it and diagnosed the build in the same breath.** *"A Business is an entity: a
row in a `BusinessTable`, an **Occupant** of a Building, holding its balance … A Building never holds
money."* And: *"**What the contradiction was actually with is the build** … the Bins that make an
economic actor an economic actor hang off a Building handle."*

**V2 — `adr/0114` wrote the target shape as a consequence, and it was never built.**
***"`World.FindBin` takes an owner rather than a Building slot."*** ⚠ **`BinOwnerKind.Business = 3`
already exists** and is already written by `World.OpenBalance`. The enum arrived; the column did not.

**V3 — `adr/0113`'s revisit trigger names the exact blocker.** *"`BinTable.Owner` is a
`HandleColumn<Building>`, so **no Household, Business or treasury can own a Bin today**."* ***A
revisit trigger that has already fired is a decision waiting to be finished.***

**V4 — `02 §5` lists Business placement as step 3, documented and unbuilt**, beside Household placement
as step 2, which shipped. And `World.CreateBusiness` carries *"**Nothing in the simulation calls
this.** A Business is placed by no pass."* ***The gap is scheduled, not overlooked.***

**V5 — 🔴 [`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md)
shipped the conflation this morning and is corrected on its own face.** It puts a seller's Goods in
*"the selling **Building's** own Bin"* and a seller's money in *"a **Business** balance."* **One seller,
two custodians.** ⚠ **It wrote *Building* because the code says Building** — which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
**inverted**: a description taking its noun from the implementation rather than from the design.
***That is a new failure mode for `plans/0012` and it is worth a Cause of its own.***

---

## What the corpus contradicts itself about — this is the expensive half

**V6 — 🔴 `CONTEXT.md` draws the line at *logistics versus decisions*, NOT at *premises versus actor*.**
→ Building: *"Anything that occupies a Lot, **holds Bins, and runs Rules**"*, and *"**A Building
aggregates logistics. It never aggregates decisions** … It may hold Bins its Occupants draw from … It
may **never** hold a Need, money, a Provider List, a Trip."* Against → Business: *"**consumes inputs,
produces outputs**, employs Citizens, and offers Goods."* ⚠ **Both sentences are authoritative and they
cannot both be whole.** ***A shared loading dock is a real thing and so is a bakery's own flour***, so
the answer is probably not *all Bins move* — **and nothing in the corpus tells them apart.**

**V7 — 🔴 Jobs are declared on the premises and posted by the employer, in two different documents.**
`CONTEXT.md`: *"employment is a **fourth key on the kind**"*, `[[building]] jobs`. Against
[`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md): *"Each **Business** posts a
wage and adjusts it by its own **fill rate**."* ⚠ ***A fill rate is a property of an employer***, and
`World.HasJob` computes it from a Building kind. ⚠ **And the build already knows this is odd**: `jobs`
sits on the **dwelling** kind in every shipped file, with a header explaining that a workplace kind
needs a second `[[zone_rule]]` or the city fills with offices.

**V8 — the cardinality itself.** *How many Businesses occupy one Building* is **undesigned**
(`adr/0070`). ⚠ **The user's answer is ≥1 and the design must be built around it**, which is what makes
this a session rather than a defect. **`GoldenFixtures` already puts two in one Building deliberately.**

---

## What it costs

**Two things move — Bins and Rules — and a third has to be invented.**

**V9 — the Bin half.** `BinTable.Owner` is `HandleColumn<Building>`, bound at construction. **6
type-level sites, 3 behaviour-level, and 27 `CreateBin` plus 19 `FindBin` call sites in `tests/`.** The
intrusive list heads move from `BuildingTable.BinHead/BinTail` to the Business. `RebuildDerived`'s
`Building` case moves and its `Business` case stops being a no-op. ⚠ **`RebuildCapacities` and its
invariant twin both resolve the owner *only* to fetch `Buildings.Kind`** — so they are the two sites
that fire the moment a Bin's owner has no kind byte.

**V10 — the Rule half.** `RuleInstance.Building` — **5 direct reads and 7 implicit**, the implicit ones
being the interesting set: `Fit` (which entity gets the kind's Rules), `Migrate`'s drop pass,
`DestroyBuilding`, condemnation's pressure walk, `Evidence.OfBuilding`, and
`NoBuildingRunsRulesItsKindDoesNotDeclare`. ⚠ **`DestroyBuilding` currently frees the Rules and keeps
the Businesses** — ***two lines pointing opposite ways***, which is only coherent while Rules belong to
premises.

**V11 — 🔴 there is no business KIND, and this is the largest single piece of new work.** No
`[[business]]` table in the loader, no kind type in `Ruleset`, no `Kind` column on `BusinessTable`. So
`Declares(kind)`, `BinsOf(kind)` and `DeclaredCapacity(kind, resource)` have **nothing to key on**.
⚠ **`RulesetLoader` also refuses `sweeps = "business"` by name**, so `PolicySubject.Business` is
declared and unreachable.

**V12 — a construction cycle.** `BinTable(capacity, Buildings)` is constructed **before**
`BusinessTable(capacity, Buildings, Bins)`, and a Business's `Balance` is a handle **into** `Bins`.
***Pointing Bins at Business closes the loop***, and the current order exists to avoid exactly that.

**V13 — two breakages specific to ≥1 tenant, and both are subtle.**
① 🔴 **Identical arming stagger.** `World.ArmingStagger` mixes the **Building's** monotonic id with the
`RuleId`, *"so that two Rules on one Building do not share an offset"* — ***two Businesses running one
Rule reopen that collision on the other axis***, and get identical `NextTick`, in the same Wheel
bucket. **It is hash-bearing.**
② 🔴 **Condemnation merges tenants.** `ZoneRuleEngine.Condemn` takes the max missed-firings over the
**Building's** whole Rule list and demolishes the **premises** — so ***one starving tenant condemns the
other's shop*** — and nothing in `RuleEvidence` names a tenant, so the player-facing answer cannot tell
two rows with one `RuleId` apart. ⚠ **That is `LEGIBLE CAUSE` failing**, not just a modelling gap.

**V14 — emissions need a hop that can fail.** A Rule reaches its footprint through
`Buildings.Lot → Lots.East/North → Cells`. A Business-attached Rule hops `BusinessTable.Building`,
which is declared **`Reference.Severable`** *because demolition invalidates it with no hook* — so the
hop has a real *no premises* branch and **`RuleEngine.Emit` has no failure path today.**

**V15 — the State Hash moves and every golden re-records**, since `RuleInstance.Building` and
`BinTable.Owner` are saved handles that fold target ids. ⚠ **Not a reason to defer, narrow or split**
([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md));
what is owed is **attribution in the commit subject**.

---

## The sitting, 2026-08-22 — **V16** onward

**V16 — 🔴 THE TEST FOR QUESTION 1 IS ALREADY WRITTEN, IN `rulesets/minimal.toml`, AND NOBODY PUT IT
THERE ON PURPOSE.** The dwelling declares **two** Bins and its three Rules sort them cleanly:

| Bin | Rule | `apply` | What that means |
|---|---|---|---|
| `sundries`, capacity 48 | `consume` | `{ derived = "occupancy" }` | **scales with the number of tenants** |
| `repairs`, capacity 4 | `upkeep` | `{ min = 1, max = 1 }` | **fires once, whatever the occupancy** |

***A Rule whose work scales with the tenants is the tenants' work; a Rule that fires once regardless is
the premises' work.*** ⚠ **The apply count is EVIDENCE and not the definition** — the definition is the
one a real city gives: ***does it leave when the tenant leaves?*** Flour goes with the baker; the roof
does not. But the corroboration is worth more than the argument, because the designer sorted these two
Bins onto opposite sides of the line **before the line had been drawn**, and `condemn_after` demolishing
the **premises** off `upkeep`'s failure is the same sorting a third time.

**So question 1's answer is NOT *all Bins move*.** It is: **a Bin belongs to the Occupant whose leaving
would empty it, and to the Building otherwise** — and the shipped file already has one of each.

**V17 — ⚠ `minimal.toml`'s shared larder is a SHORTCUT and must not be read as a shared loading dock.**
Three Households in one dwelling do not share a kitchen, and this file's first line says it *models no
city*. Under [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) the
shortcut is **not evidence**, so it cannot be cited to keep `sundries` on the premises. ***What V16
establishes is that the line exists and is findable; `sundries` still falls on the tenant's side of
it.*** 🔴 **This moves a hash and changes what the city does**: `consume` becomes one Rule per Household
rather than one Rule applied `occupancy` times, and the `derived = "occupancy"` readout — *the one
declared Readout in the project* — loses its only caller.

**V18 — 🔴 QUESTIONS 2, 3 AND 4 ARE ONE QUESTION, and the dependency runs one way only.** Jobs cannot
move to the employer (2) without something that creates employers (3), and nothing can create an
employer without a business **kind** to create it from (4). ⚠ **The reverse is not true** — the Bin and
Rule ownership repair (V9, V10) needs none of the three, because a Household is already an Occupant that
exists, is already created by a shipped pass, and is already `BinOwnerKind.Household = 2`. ***So the
milestone has a cleavage plane, and it is between the Occupant repair and the Business content.***

**V19 — ⚠ the cheap half is reachable through the HOUSEHOLD, not through the Business.** Every
type-level move in V9 — `BinTable.Owner` off `HandleColumn<Building>`, the list heads off
`BuildingTable`, `RebuildDerived`'s owner cases, `RebuildCapacities` fetching `Buildings.Kind` — is
**exactly the same work** whichever Occupant lands on it, and the Household exercises all of it on
`minimal.toml` **today**, with no new kind, no new pass and no new content decision. ***A repair that can
be driven by the tenant that already exists should not wait for the tenant that does not.***

**V20 — 🔴 THE SHARPEST THING IN THE SITTING, AND NOBODY WENT LOOKING FOR IT: `DestroyBuilding`
EVICTS A HOUSEHOLD AND ORPHANS A BUSINESS.** `World.DestroyBuilding` (`World.cs:3600`) walks
`Occupants` and calls `Unplace` on every Household — **into the Unplaced Pool**, which is a sink with a
give-up bound on it. It then reaches `BuildingBusinesses` (`:3660`) and does this:

```
IndexList premises = BuildingBusinesses;
while (premises.PopFront(slot) != Rows.NoSlot) { }
```

***It unlists them and frees nothing.*** The row survives with its balance intact and its `Building`
handle severed. The comment at `:3640` says why, and it is right on its own terms: **there is no pool
for a Business**, and freeing the row would destroy its money and fire
`Invariant.MoneyIsConserved`. It ends *"⚠ WHAT BECOMES OF A BUSINESS WITH NO PREMISES IS UNDESIGNED"*.

🔴 **That is [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md) with the safety catch
still on.** A Business with no premises is never relocated, never liquidated and never freed, so the
orphan set **grows monotonically with demolitions for ever** — and demolition is not rare, it is what
`condemn_after` does to every dwelling in `minimal.toml`. ⚠ **It does not fire today only because
nothing in the simulation creates a Business**, which is `adr/0070` holding the door shut from the other
side. ***The moment milestone 25 gives a Business a reason to exist, this becomes a live unbounded
collection, and it will be milestone 25's own defect rather than an inherited one.***

**V21 — ⚠ the *Bins hang off a Building* problem already has one Bin that hangs off NOTHING.**
`BinTable.Owner` is `HandleColumn<Building>` (`BinTable.cs:100`), and `BinTable.Create(BinOwnerKind,
ResourceId, long)` (`:183`) sets `Owner[slot] = default`. So a Business's balance Bin — the one Bin in
the project already owned by a Business — is discriminated **only by `OwnerKind`** and points at no
owner at all. `World.OpenBalance` (`World.cs:2591`) opens it at `long.MaxValue`. ***The polymorphic
owner column was not deferred; it was routed around, once, and the route is a Bin with a null owner.***

**V22 — ⚠ V19 IS CORRECTED, AND THE CORRECTION IS A DESIGN STATEMENT WORTH MORE THAN THE FINDING.**
`World.CreateBin` (`World.cs:2098`) takes its capacity from
`DeclaredCapacity(Buildings.Kind[buildingSlot], resource)` — **the capacity is keyed on the BUILDING's
kind at the creation site**, not only in `RebuildCapacities`. ⚠ **Neither a Household nor a Business has
a kind byte**, so *"drive the repair through the Household"* does not dodge the kind problem the way
V19 claimed.

***What it does instead is produce the answer:*** **capacity is a property of the premises and the level
is a property of the tenant.** A shop holds what fits in the shop; what is in it is the shopkeeper's.
So `DeclaredCapacity` stays keyed on the **building kind** and needs no business kind at all, and V18's
*questions 2, 3 and 4 are one question* stands while **question 1 detaches cleanly from all three.**

**V23 — no Rule can read a Business's balance, and the readout says so by throwing.**
`Readouts.Read` (`Readouts.cs:206`) throws for `Readout.Balance` because balance is Household-scoped,
and `ReadHousehold` (`:165`) is the only balance readout in the project. ***There is no `ReadBusiness`.***
That is the payer blocker arriving in a second place, on the *read* side rather than the *spend* side.

**V24 — `RuleEvidence` names no subject at all**, so V13②'s legibility gap is not *the wrong subject*,
it is **no subject**. `RuleEvidence` (`BuildingEvidence.cs:61`) carries `Rule, LastRan, Succeeded,
Blocked, Reported, StarvedSince, Rate, MissedFirings` and no handle; the subject is the enclosing
`BuildingEvidence`. `Evidence.OfBuilding` (`Evidence.cs:47`) collects Households and Citizens and
**never reads `BuildingBusinesses`**. ⚠ ***Adding a tenant subject is a field, not a redesign*** — which
makes V13② cheaper than it was written.

**V25 — condemnation confirmed, and the repair is a change to WHAT DIES rather than to how pressure is
measured.** `ZoneRuleEngine.Condemn` (`ZoneRuleEngine.cs:303`) reads the kind's `CondemnAfter`, walks
`BuildingRules`, takes the **longest** pressure by cross-multiplication (`:358`), records one condition
to the trail and calls `DestroyBuilding`. ⚠ **The verdict is already an OR and the walk continues only
for attribution** (`:346`) — so the mechanism does not need to change to become tenant-aware. ***What
changes is that a failing tenant's Rules should end the TENANCY and leave the premises standing***,
which is both the real-city answer and the only one under which V20's orphan gets a sink.

**V26 — question 4 answers itself from the city rather than from the loader: THERE ARE TWO KIND
TABLES, because the two things vary independently.** ⚠ **The premises and the trade are not correlated
and every high street proves it** — ***the same brick shopfront hosts a bakery, then a barber, then
nothing***, and the building did not change. A single kind table cannot express that: it would make
*bakery* a property of the walls, so a tenant leaving would have to demolish the shop to stop being a
bakery. ***That is exactly the defect `Condemn` has today*** (**V25**), arrived at from the other end.

**What each table keeps, and the test is *would this survive a change of tenant*:**

| `[[building]]` — premises | `[[business]]` — the trade |
|---|---|
| `footprint`, `parking`, `occupants` (**tenant capacity**), `condemn_after` | `jobs`, shift hours, the wage |
| Bins the **premises** keep — `repairs` | Bins the **tenant** keeps — `sundries`, stock, the till |
| `[[rule]]`s that fire once regardless — `upkeep` | `[[rule]]`s whose work scales with the trade |
| **Capacity for every Bin on the Lot** (**V22**) | **The level in the ones it owns** |

⚠ **`occupants` changes meaning and does not change value.** It stops being *how many Households* and
becomes *how many tenants of any kind*, which is what `adr/0068`'s eviction already does — ***an
over-capacity Building evicts, and it never asked what the overflow was.***

**V27 — 🔴 the Workplace stops being a Building, and `adr/0101` already said so in a word nobody had
to read closely.** *"A commute is two journeys anchored on a Shift start hour belonging to the
**Workplace**"* — and a Workplace is where you are **employed**, which is an employer. So shift hours
move to `[[business]]` with `jobs`, by **V18**'s dependency rather than as a separate decision. ⚠ **The
Citizen still stores nothing**, which is the property `adr/0101` was actually defending: they read
their hours off their employer instead of off their building, and *"changing employer changes their day
with no write and nothing to invalidate"* becomes **literally true** where it was previously true by
coincidence — a Citizen changing employer within one Building currently changes nothing at all.

**V28 — ✅ QUESTION 3'S SINK IS ALREADY BUILT, FOR HOUSEHOLDS, AND IT IS NOT LIQUIDATION.** The
give-up bound fires `World.Depart` (`World.cs:1344`), which does two things in one call:

```
MoneySupply.Issued[MoneySupplyTable.Slot] -= new Money(Bins.LevelAt(balance));   // :1370
...
DestroyHousehold(household);                                                     // :1381
```

***The money is not destroyed and it is not confiscated — it LEAVES THE CITY.*** `MoneySupplyTable.Issued`
is declared *"money that has entered this world, **net of anything that has left it**"* (`:68`), and
`World.Endow` (`:934`) is the mirror: an arriving Household carries a balance in from the Hinterland's
`emigrant_balance_min/max` band. **There are two doors and they are the same door.** ⚠ **The order is
load-bearing and the code says why**: the supply write is in `Depart` and deliberately *not* in
`DestroyHousehold`, ***because only Departure means emigration.***

🔴 **So the recommendation the sitting was about to make — relocate rather than liquidate — is right and
was incomplete.** The full answer is: **relocate into a pool, and let the give-up bound be the exit, and
the exit is emigration.** A Business that loses its premises is *unpremised* exactly as a Household is
*unhoused*; it waits; if nothing tenants it, it leaves and takes its money with it. ***Nothing new is
invented, no owner has to be modelled, and `Invariant.MoneyIsConserved` is satisfied by the mechanism
rather than by an exception to it.***

**V29 — ⚠ and it lands on the channel that EXISTS rather than the one that does not.** `Depart` refuses
a **housed** Household — `Invariant.OnlyAnUnhousedHouseholdGivesUp` (`World.cs:1348`) — and the
housed-departure channel *(a family with a home choosing to leave)* is **unbuilt and deferred to
milestone 16**. ***A Business orphaned by `DestroyBuilding` is unpremised by construction***, so the
Business sink needs the built half only. **The symmetry is not merely available, it is available on the
easy side.**

**V30 — 🔴 V20 IS WORSE THAN IT WAS WRITTEN, AND THE INVARIANT THAT LOOKS LIKE IT COVERS THIS DOES NOT.**
`MoneyLedger.Of` (`MoneyLedger.cs:91`) walks **every live Bin slot**, skipping only dead rows and
unconserved Resources, and *"whoever owns it — and this does not ask."* ⚠ **An orphaned Business's
balance Bin is still live**, so it still counts toward `Total`, so **`Invariant.MoneyIsConserved`
still balances**. ***The one invariant whose name suggests it would catch this is structurally blind
to it***, and the orphan does not even land in `Elsewhere`, because `OwnerKind` still reads `Business`.
⚠ **The reverse case IS caught** — `Reference.Required` on the balance handle makes
`Invariant.CrossTableHandleResolves` fire on a *freed Bin under a living owner*. ***The build guards
the direction that cannot happen and is silent on the one that can.***

**V31 — 🔴 THE SINK IS SOLVED AND THE SOURCE IS NOT, WHICH RESTATES QUESTION 3 MORE SHARPLY THAN IT WAS
ASKED.** `BusinessTable`'s own doc comment: *"**Nothing funds one.** A Business opens with an empty Bin
and there is no door that pays it."* A Household's opening balance comes from
`HinterlandDefinition.EmigrantBalance` — **an authored band on the Hinterland**, arriving through the
gate. ⚠ **A Business has no such band and no gate to arrive through**, and inventing one is a
`plans/0002` §D1 row on the day it is written (`adr/0052`). ***So question 3 is not "what creates a
Business" — it is "what CAPITALISES one", and that is a number with a named ratifier or it is not a
number.***

---

## What the sitting concludes

**Question 1 — which Bins move: ✅ ANSWERED, and by content rather than argument.** A Bin belongs to
the Occupant whose leaving would empty it, and to the premises otherwise (**V16**). **Capacity is the
premises'; the level is the tenant's** (**V22**). On `minimal.toml` that puts `sundries` on the
Household and `repairs` on the Building, which is where its own Rules already sorted them.

**Question 2 — jobs: ✅ ANSWERED in principle, and it cannot be BUILT alone.** A fill rate is a property
of an employer (`adr/0026`), so jobs belong to the Business. ⚠ **But `jobs = 8` sits on the dwelling
kind, and moving it needs an employer to move it to** — **V18**. ***So the sitting decides it and
milestone 25 does not necessarily build it.***

**Question 3 — ✅ THE SINK IS ANSWERED; 🔴 THE SOURCE IS THE ONLY THING LEFT OPEN IN THIS SESSION.**
A Business that loses its premises joins a pool, waits under a give-up bound, and if nothing tenants it
it **departs and takes its money out of the city** — `World.Depart`'s existing mechanism, unchanged
(**V28**), on the channel that is built rather than the one deferred to milestone 16 (**V29**). ⚠ **The
symmetry with `adr/0069` is preserved**: nothing auto-tenants, because a pool and a placement pass are
what *not* auto-tenanting looks like. 🔴 **What remains is CAPITALISATION** — a Household arrives with a
banded balance from its Hinterland and *nothing funds a Business* (**V31**). ***That is a hash-bearing
number and it needs a named ratifier on the day it is written.***

**Question 4 — ✅ ANSWERED: two kind tables**, `[[building]]` for premises and `[[business]]` for the
trade, because the same shopfront hosts a bakery and then a barber (**V26**). **Smaller than V11
feared**, because **V22** removes `DeclaredCapacity` from the list of things needing a business kind,
and **larger in one place nobody costed** — the Workplace stops being a Building and shift hours travel
with `jobs` (**V27**).

**Question 5 — ✅ ANSWERED.** The stagger mixes the tenant's monotonic id
instead of the premises' (`World.cs:2080`). Condemnation keeps its pressure walk unchanged and **ends a
tenancy instead of demolishing premises** (**V25**), and `RuleEvidence` gains a subject field
(**V24**).

---

## What the sitting must settle

1. **Which Bins move.** V6 is the question: *all* inventory to the tenant, or does a Building keep a
   genuinely shared store? **Name the test that tells a shared loading dock from a bakery's flour.**
2. **Jobs — premises or employer.** V7. ⚠ **A fill rate needs an employer**, so this probably decides
   itself; the sitting must say so rather than leave two documents disagreeing.
3. **What creates a Business**, given `02 §5` step 3 is milestone 13. ⚠ **The symmetry argument is
   sharp and cuts against a stand-in**: `adr/0069` says construction houses **nobody**, and a Zone Rule
   that auto-tenants a shop is that rule broken on the commercial side.
4. **The business kind's shape** — V11, and whether `[[building]]` splits or gains a tenant table.
5. **The tenant-aware stagger and the tenant-aware condemnation** — V13, both hash-bearing, both
   `LEGIBLE CAUSE`.
6. ~~**Where it lands.**~~ ✅ **SETTLED 2026-08-22, with the user in the room: its own milestone.**
   **This is [`06`](../docs/06-roadmap.md) milestone 25**, and milestone **12 is capped at task 6** with
   its risk rewritten to what tasks 1–6 retire. **12's tasks 7–10 and its original risk become milestone
   26, the purchase**, blocked on 25. ⚠ **Scoping it produced a decision about the numbering scheme
   rather than about the city** — [`adr/0140`](../docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md),
   because inserting two rows would have renumbered **276 citations across 73 files** under a rule whose
   own stated premise was that the tail is *the least cited part of the corpus*. ***The premise was
   measured for the first time on the day it was next needed, and it had expired.***

**The one thing this session could not settle, and it is a number rather than a shape: what
capitalises a Business.** Everything else above is closed. ⚠ **It is `adr/0052`'s problem and not this
sitting's** — a band, a world that would exercise it, and a quantity that would ratify it.

⚠ **Nothing here may be closed by pricing it.** Every cost above is a *count of call sites*, and
***a count of call sites is not an argument about what the city is.***
