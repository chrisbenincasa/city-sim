# 0039 — Session V: the Business is the actor and the Building is premises

**⚠ OPENED 2026-08-22. NOTHING BELOW IS SETTLED.** The brief, not the record.

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

⚠ **Nothing here may be closed by pricing it.** Every cost above is a *count of call sites*, and
***a count of call sites is not an argument about what the city is.***
