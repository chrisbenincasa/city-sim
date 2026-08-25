# 0041 — The Business is a thing the city contains

**[`06`](../docs/06-roadmap.md) milestone 27.** ✅ **Ungated** — its one gate was milestone **25**, the
Occupant repair, which closed 2026-08-23.

> ⚠ **This document decomposes; it does not specify.** The specification is
> [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md)'s **tasks 6–9**, kept there
> as written when milestone 25 was capped at group A — the same treatment
> [`0037`](0037-goods-between-buildings-the-district-pool.md) gives milestone 26's. ***`0040` owns what
> the tasks are; this document owns what order they go in, what they need, and what decomposition
> found.*** Where the two disagree about a task's content, `0040` wins.

> ***Milestone 25 made the payer nameable. This one makes one exist.***

---

## Status

✅ **TASK 6 SHIPPED 2026-08-24.** ~~🔴 **NOT STARTED.**~~ Decomposed 2026-08-24 against the tree, and
the first task landed the same day.

🔴 **THE ORDER MOVED AGAIN ON 2026-08-24, WITH THE USER IN THE ROOM: 6 → 8 → 7/9.** ~~Remaining: 9,
then 8, then 7.~~ **Task 9 was opened and put back down** (**G21**). Its stated reason for going
second was that it is *exercisable by fixture* — and ***that is the reason [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md)
**F43** had already refuted, one day before this document was written***: a mechanism exercised only by
hand-built fixtures is a mechanism with no world in it. **Task 8 is next** — it is unblocked, it creates
the actor the milestone's risk names, and it is what gives task 9 a subject that exists.

✅ **TASK 8'S FOUNDING HALF SHIPPED 2026-08-24 — 114 Businesses founded in a shipped world.** The
milestone's named risk is retired on its main axis: the economic actor exists in a city rather than in
a fixture.

✅ **TASK 8 CLOSES 2026-08-24 — THE FOUNDER IS A CITIZEN AND THE JOB IS THE RECORD.**
[`adr/0146`](../docs/adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md)'s
half that 27 can ship: `World.Found` takes a `Handle<Citizen>`, the pass draws **unemployed, housed
Citizens whose Household affords the band**, and the founder is `Employ`ed into the new Business.
**Measured on `founded.toml`: 148 founded-trade Businesses, 148 of them staffed.** ⚠ **The income half
is `adr/0026` at milestone 15 and is deliberately NOT proxied.** 🔴 **One new refusal** (`adr/0048`:
152 → 153): a `[[business]]` with no `jobs` in a file stating `[founding]`, because a trade nobody can
work at is a trade nobody can found — and `founded.toml`'s two trades gained `jobs` and a Shift band.
⚠ **No golden artefact moved**, and that is a fact rather than luck: no golden Ruleset states
`[founding]`. Tier green at **2,126**.

✅ **TASK 7 SHIPPED 2026-08-24, IN TWO HALVES, AND THE SECOND WAS NOT IN THIS PLAN.** The handle move
landed and **emptied every shipped city** — 66 assertions failing on one sentence, *nobody is employed
anywhere* — because employment now needs a Business and the only thing that creates one is
`[founding]`, which one shipped file states. ⚠ **This document predicted that failure word for word in
task 7's own entry** and held that task 8 discharged it; task 8 put founding in the **build**, not in
the **worlds** (**G35**). [`adr/0148`](../docs/adr/0148-a-premises-kind-may-declare-its-trade-and-instantiating-one-is-not-housing-anybody.md)
is the content half: a `[[building]]` kind may name one trade and construction instantiates it. **All
fifteen shipped Rulesets changed**, `[[building]] jobs` and the Shift band are now **refused**, and the
assertion tier is green at **2,125** with **every golden artefact re-recorded**. Findings **G35**–**G38**.

🔴 **THE ORDER MOVED A THIRD TIME, 2026-08-24, AND TASK 7 IS NOW THE CRITICAL PATH.** Two things did it
on the same day and they are unrelated:

1. ✅ **Decision 2 is SETTLED and was never actually open** (**G30**) — `jobs` and shift hours go on the
   business **kind**, per [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)'s
   *Declares* row, which this document never cited. ⚠ **And the task SHRANK**: the wage is milestone
   15's, so only two of the three things `0141` names move here.
2. 🔴 **[`adr/0146`](../docs/adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md)
   makes task 7 a PREREQUISITE of task 8's founder half.** Founding now costs a **Citizen** as well as
   the Household's money, the founder is recorded by the **employment link** rather than a column, and
   ***an unpremised Business has no Building for a `Workplace` handle to address.***

🔴 **THE ORDER MOVED A FOURTH TIME, 2026-08-24, AND THIS ONE IS A CORRECTION** (**G32**).
~~Remaining: 7, then task 8's Citizen half, then 9.~~ **Task 7 cannot go next.** Nothing tenants a
Business, so pointing a Workplace at one puts employment at **zero in every shipped world** — thirteen
of fifteen Rulesets declare no trade, and `founded.toml`'s 114 are all unpremised until they leave.
⚠ **`PlacementEngine.cs:190` says this milestone owes the pass and none of its four tasks names it.**

✅ **THE PLACEMENT PASS SHIPPED 2026-08-24, and the decision it needed went to the user in the room**
— [`adr/0147`](../docs/adr/0147-a-business-takes-premises-by-placement-and-one-ceiling-counts-both-kinds-of-tenant.md):
**one ceiling over both kinds**, so a shop takes a family's room. `PlacementEngine.Tenant`,
`World.Premise`, `World.Tenants`, three `purpose_tag`s (27, 28, 29) and **no new Ruleset number**.

**MEASURED on `founded.toml`, 4,096 Ticks at 2,000 Citizens: founded 113, premised 69, standing 38,
pooled 75.** ⚠ **The flow and the standing count differ BY DESIGN and it is the best thing the run
showed**: `founded.toml` descends from `minimal.toml`, which condemns Buildings throughout, and
`World.Destroy` unpremises every tenant of a Building it takes down. ***So 31 shops took premises, lost
them to condemnation and went back to the pool to look again*** — which is
[`adr/0144`](../docs/adr/0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md)
running for the first time on a Business the simulation created.

**Remaining: 7, then task 8's Citizen half, then 9.**

⚠ **The census below was taken on 2026-08-24 and it corrects the risk statement in three numbers**
(**G1**). ***The risk stands; the figures stating it have drifted*** — which is the second time in two
days that a milestone's own risk cell has been the thing that was stale.

🔴 **Decomposition changed the ORDER, and that is this document's result** (**G5**). ⚠ **A second
result arrived by correction on the same day: THIS DOCUMENT'S OWN *blocked* READING OF TASK 8 WAS
WRONG** (**G13**), so ***nothing blocks the milestone's first three tasks.*** `0040` lists the
tasks **6, 7, 8, 9**. Run in that order, **task 7 empties the city of jobs** — every commute, every
work Trip and the traffic, parking and commute suites with them. The order is **6 → 9 → 8 → 7**, and
the reason is in the Ruleset's own header rather than in any ADR.

---

## The named risk, as `06` states it

**That the Business is a TABLE and not an actor the city can create.** It has no kind, so `Declares`,
`BinsOf` and `sweeps = "business"` have nothing to key on; `jobs` sits on the **dwelling** kind in every
shipped Ruleset, so a fill rate has no employer to belong to ([`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md));
and 🔴 ***nothing funds one.***

⚠ **`06`'s cell states this risk with three numbers and all three are stale.** See **G1**. The
milestone is not smaller than advertised — ***it is the same risk with a bigger table under it.***

---

## What the build already holds — surveyed 2026-08-24

**Counted against the tree**, and ⚠ ***a count of call sites is not an argument about what the city
is*** — it sizes the work and settles nothing.

| Symbol | `src/` | `tests/` | Note |
|---|---|---|---|
| `World.CreateBusiness` | **0** | **17** | `World.cs:931`. ***Nothing in the simulation creates a Business*** |
| `World.Endow` | 2 | — | `SyntheticCity.cs:277` and `World.cs:1242`. **Household-only; no Business overload** |
| `World.Depart(Handle<Business>)` | 1 | — | `World.cs:1524`. ⚠ **The exit already exists and is symmetric** |
| `World.BalanceOf(Handle<Business>)` | **0** | — | `World.cs:1037`. Exists, unused |
| `Readouts.DeclaredSet` | — | — | `Readouts.cs:119`. **Exactly two** |
| `Readouts.ScopeOf` | 2 | — | `Readouts.cs:152`. **A ternary, not a switch** |
| `RulesetNames` maps | — | — | `RulesetNames.cs:52-55`. **Four, and all id→name only** |
| Refusal sites in `RulesetLoader.cs` | **140** | — | Machine-checked against `adr/0048:78` |
| Sites assuming a Workplace is a Building | **33** | — | Enumerated by the task 7 survey |

### `BusinessTable` — six columns, and still no kind

`BusinessTable.cs:67-72`. **Saved:** `building` (`Reference.Severable`), `bin_head`, `bin_tail`.
**Derived:** `balance` (`Reference.Required`), `building_next`, `pool_slot`. ⚠ **`06` and `0003` both
say *three columns*** — true before milestone 25 tasks 1 and 5, false since (**G1**).

### What already works, and it is more than the risk implies

- **The sink is complete.** `Depart(Handle<Business>)` (`World.cs:1524-1554`) mirrors the Household's
  exactly: refuse a premised Business, subtract the balance from `MoneySupply.Issued`, leave the pool,
  destroy the row. **`UnpremisedTable`, the give-up clock and `PlacementEngine.Retire` all shipped at
  milestone 25 task 5.**
- **A Business-owned Bin is representable.** `BinOwnerKind.Business` is live and `CreateBusiness`
  already opens a balance Bin through it (`World.cs:944`).
- **`World.BalanceOf(Handle<Business>)` exists** and has zero `src/` callers.

***So what is missing is a kind, a source, a band, and a third subject — not a subsystem.***

### 🔴 Precondition 1 — nothing creates a Business, and no fixture-free path exists

`World.CreateBusiness` has **no `src/` caller**. Three source comments say so in place:
`PlacementEngine.cs:184-185`, `UnpremisedTable.cs:19-27`, `BusinessTable.cs:100-106`. ⚠ **Tests fund a
Business by TRANSFER and never by endowment** — `GoldenFixtures.cs:535-541`, `BusinessTests.cs:55-66`,
`UnpremisedPoolTests.cs:78-86` all withdraw from a Household and deposit into the Business, because
there is no door.

### 🔴 Precondition 2 — task 8 is blocked from inside the milestone

[`0002`](0002-open-questions.md) **§D2**, *what capitalises a Business* — hash-bearing, and owed a
named ratifier: **a machine, a world and a quantity**
([`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md),
amended twice). **A category is not a name.**

---

## Open decisions

### 1. ⚠ What capitalises a Business? — **inherited as `0040` decision 4, and RETYPED 2026-08-24**

`0002` §D2 owns it. 🔴 **IT IS NOT A BLOCK AND THIS DOCUMENT SAID IT WAS** (**G13**).
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
opens by refusing that reading outright — *"**This does not require ratifying a number before choosing
it.** Often that is impossible and forcing it is worse than the disease… ***The rule governs the record,
not the timing.***"* ⚠ **What task 8 owes is a ratifier NAMED on the day the band is written** — a
machine, a world and a quantity — **not a settled number.**

**The world is milestone 27's own demonstration Ruleset**, the first file to declare `[[business]]`,
which **task 8 itself produces**. ***So the circularity dissolves the way [`0002`](0002-open-questions.md)
§D2's tâtonnement row already dissolved it***: *a number is unratifiable because no world exercises it,
not because it is new.* ⚠ **The quantity is still owed and a category is not a name.**

✅ **So nothing blocks tasks 6, 9 or 8**, and the only genuine open decision is **2**, which blocks
**task 7** — the last one.

⚠ **`0040` decision 3 already settled the adjacent half**: a Business shares
`[placement] gives_up_after_days` as a **stand-in**, so no second bound and no second ratifier is owed.
`adr/0142`'s argument against sharing is **deferred to this milestone**, and task 8 is the first day a
Business exists in numbers anybody can have an opinion about.

### 2. ~~🔴 NEW — does `jobs` move to the business kind, or to the Business row?~~ — ✅ **SETTLED 2026-08-24 BY AN ADR THIS DECISION NEVER CITED**

✅ **`jobs` and shift hours go on the business KIND. The wage is not milestone 27's at all.**
🔴 **This decision was framed against the wrong ADR and therefore looked open when it was not**
(**G30**). It asked what `adr/0026` means by *employer* and concluded *"the ADR uses the word without
ever saying whether it means the kind or the row."* **That is true of `0026` and irrelevant**, because
[`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)
answers it directly, in a table whose row is literally headed **Declares**:

| | `[[building]]` — premises | `[[business]]` — the trade |
|---|---|---|
| **Declares** | `footprint`, `parking`, `occupants`, `condemn_after` | `jobs`, shift hours, the wage |

***Both columns of that table are KIND namespaces***, so *declares* can only mean the kind. ⚠ **And
the build already reads it that way** — `RulesetShape.cs:217`: *"`adr/0141` gives the trade `jobs`,
shift hours and the wage."* **The fork was in this document and in nothing else.**

**The *on the row* horn rests on a misreading, and naming it is worth more than the answer.** It argued
that a row column *"lets two bakeries of one kind employ different numbers, which is what `adr/0026`'s
fill rate could be read to require."* 🔴 ***A fill rate is not `jobs`.*** It is `filled ÷ jobs`, and
**`filled` is per-Business by construction** — it is a count of who actually took the work. So
`adr/0026`'s *each Business adjusts by its own fill rate* is satisfied **with `jobs` on the kind**: the
kind supplies the denominator and the row supplies the numerator. ⚠ **The horn needed the row to carry
`jobs` and the row only ever needed to carry `filled`.**

**The precedent is exact and already shipped.** `[[building]] occupants` is declared by the **kind**
while occupancy is counted **per Building**, and
[`adr/0068`](../docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)
is the same shape under a different name. ***A capacity is declared and a filling is counted.***

### 🔴 But `adr/0141` and `adr/0026` disagree about the WAGE, and neither says so

⚠ **`0141` puts *the wage* in the Declares row with `jobs`. `0026`'s whole mechanism is that the wage
MOVES** — *"each Business posts a wage and adjusts it by its own fill rate — raise it when vacancies
persist, let it fall when applicants queue."* **A kind declaration is Ruleset data**: identical across
every Business of that kind, hot-reloadable, and `adr/0015`-refused on reload where it is
world-creation. ***A number that adjusts per-Business cannot be one.***

**The reading that makes both true**: the trade declares the **posted anchor**, and the **current wage
is a Business row column**. ⚠ **That is a reading and not a decision** — it is offered here so the
tension is on the record, and ***it is not this milestone's to settle***.

✅ **Because the wage is NOT milestone 27's.** [`06:99`](../docs/06-roadmap.md) places wages at
**milestone 15**, *"attended services, wages and Skill Tiers"*, citing `adr/0026` by name; `Readouts.cs:69`
says the same in its own doc comment — *"income is a **flow** that arrives with wages in milestone 15"* —
and [`adr/0146`](../docs/adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md)
turns on it. ⚠ **`adr/0140` is what makes this coherent**: a milestone number is an identity and the
roadmap's order is the sequence, so **15 comes after 27** and there is no contradiction in the ordering.

🔴 **There is one in a doc comment, though.** `RulesetShape.cs:217` says `jobs`, shift hours and the wage
*"all three arrive with milestone **27 task 7**"*. **Two of the three do.** Filed to `plans/0012` as
**Cause 4** — a description of the code wrong about the trigger.

***So task 7 is smaller than this document had it***: two of three things move, and the third was never
in scope.

### 3. ⚠ NEW — what does the `sweeps` refusal say once half of it is answerable?

`RulesetLoader.ReadSubject` refuses `"business"` and `"building"` from **one** case block whose
message gives a **separate reason per subject**: *"A Business has a balance and no pass that moves it;
a Building population needs the predicate that selects it, and neither exists."*
🔴 ~~**Task 6 makes the first clause false and leaves the second standing.**~~ **IT DOES NOT, AND THIS
DECISION DOES NOT OPEN YET** — checked against the message on 2026-08-24, the day task 6 shipped
(**G20**). The refusal's stated reason for `business` is ***"a Business has a balance and no pass that
moves it"***, and **task 6 added no pass** — it added a *kind*. The clause is still true word for word,
so the message needs no edit, the block needs no split, and **the refusal count stays where task 6 left
it**. ⚠ **The prediction reasoned from a reason the message does not give**: decomposition assumed
`sweeps = "business"` was refused for having *nothing to key on*, which is what the milestone's risk
cell says about `Declares` and `BinsOf` — but this refusal never said that. **The trigger is a pass that
moves a Business's balance, which is task 9 at the earliest.** `0040` **F9** found the block; what it
did not settle is whether the block splits, the message is rewritten, or `sweeps = "business"` becomes
legal here. 🔴 **Splitting it moves the refusal count and `RefusalCountTests` will say so** (**G3**).

---

## Tasks

**Ordered by what the next task needs, and ⚠ the order is NOT `0040`'s.** See **G5**.

### 6. **The `[[business]]` kind table.** *(first — everything needs a kind)*

`0040` task 6 owns the content. What decomposition adds:

- **A fifth namespace is cheaper than it sounds** (**G4**). `RulesetNames` holds **id→name only**
  (`RulesetNames.cs:86-103`); the name→id direction lives in the loader's private dictionaries and is
  discarded when `Read()` returns. So the edit is five fields, one internal constructor, one accessor
  and **one** construction site (`RulesetLoader.cs:228`) — and the `byte` `Invert` overload already
  exists. **No test asserts *four*.**
- **The pattern to mirror is exact**: declare at `RulesetLoader.cs:276-288`, parse at `ReadKinds`
  (`:988-1183`), hash the names into `KindKeys` (`:250`), store as `Saved<byte>("kind")`, resolve
  through `Ruleset.Declares`/`Kind`/`BinsOf`/`RulesOf`.
- 🔴 **A saved `kind` byte on `BusinessTable` widens the saved row**, so it folds into the State Hash
  and `FactorioTests.Every_saved_column_reaches_the_file_and_no_other_one_does` must see it corrupted
  in a fixture. `business` is reachable only through `GoldenFixtures.Build()`.
- 🔴 **Owed on the day: `adr/0048`.** See **G3**.

✅ **SHIPPED 2026-08-24.** A fifth namespace (`RulesetNames`), `[[business]]` parsed and registered
(`RulesetLoader.cs`), `Ruleset.BusinessKindCount`/`BusinessKindKeys`/`BusinessKindKey`/`DeclaresBusiness`,
a saved `BusinessTable.Kind`, `World.CreateBusiness(premises, kind = 0)`, the migration walk
(`RulesetMigration.BusinessKind`), two `RulesetShape` members, and
[`rulesets/tenanted.toml`](../rulesets/tenanted.toml) — the fourteenth shipped file — guarded by
`BusinessKindLoadTests`. **The whole assertion tier is green at 2,108.** Findings **G14**–**G25** below.

⚠ **There is no `BusinessKindDefinition` type and that is the decision, not an omission.** A
`[[business]]` carries a `name` and nothing else, because `adr/0141` gives the trade `jobs`, shift hours
and the wage and ***all three arrive with task 7***. `RulesetShape` compares identity and has no
`CompareBusinessKind`, for the same reason. **On the day task 7 lands, both grow.**

⚠ **The `kind` parameter on `World.CreateBusiness` is OPTIONAL and the argument is about consequence
rather than convenience.** `CreateBuilding`'s kind is required because a Building cannot be fitted
without one; a Business kind declares nothing until task 7, so requiring it would make **seventeen test
call sites** name a value nothing reads.

### 9. **A Rule can read a Business's balance.** *(✅ CLOSED 2026-08-24)*

✅ **SHIPPED 2026-08-24, into
[`adr/0149`](../docs/adr/0149-a-business-is-a-population-a-policy-sweeps-and-a-readout-names-every-entity-it-reads-against.md)
— and it is not the task this section described.** 🔴 **The plan pointed at a Rule Instance column and
the answer was a Policy**; **G39** below is that finding and it is `adr/0093`'s ordinary shape.

- ✅ **`sweeps = "business"` is accepted, and the refusal it replaced was the specification.** The loader
  had refused it with *"A Business has a balance and no pass that moves it"* — ***task 9's sentence,
  written by the build in advance.*** `PolicyEngine.SweepHouseholds` became `SweepMembers` and takes its
  slot count, liveness test and balance Bin from the subject; nothing else in it moved.
- ✅ **The Readout half landed as `G11` predicted and one step wider.** `ReadoutScope` gained
  `Business`, `Readouts.ReadBusiness` is `World.BalanceOf(Handle<Business>)`'s first caller — and
  `ScopeOf` did not stop being a ternary, it **stopped existing**: `balance` belongs to a Household *and*
  a Business, so a Readout declares the **set** it can be read against and the loader's equality became
  `IsReadableAgainst`.
- ✅ **`Readouts.cs`'s own rule was followed rather than collapsed** — three entry points, not one switch
  over an `(entity kind, slot)` pair.
- ✅ **[`rulesets/levied.toml`](../rulesets/levied.toml) ships**, the sixteenth file: `founded.toml` plus
  five lines, so the diff *is* the demonstration. `BusinessLevyTests` runs it against `founded.toml` as
  the control at one seed. 🔴 **Its numbers ratify nothing** — a shop here has no revenue — and both its
  `[founding]` numbers and the levy's `percent` are now in `plans/0002` **§D1**, ⚠ **including the pair
  task 8 owed and did not file.**
- ⚠ **`adr/0048`'s count does not move.** One `Refuse(` call site served `business` and `building`
  together; it now serves `building` alone. **`sweeps = "building"` is untouched and is a different kind
  of absence** — a Business population is every live row, a Building population is whichever rows a
  predicate selects, and the predicate does not exist.

**What this task no longer contains, and it is worth the space.** The section below is the original
plan, kept because `G39` is about it:

> - 🔴 **The real content is a THIRD SUBJECT on the Rule Instance, and the build already says so**
>   (**G10**). `RuleInstanceTable.cs:91-95`: *"A Business gets its own column when a Business runs a
>   Rule, which is milestone 27."* `World.FindLocalBin` (`World.cs:3098-3111`) branches on
>   `RuleInstances.Household[instance].IsNone` — **a binary**. ⚠ **This is milestone 25 task 2's shape a
>   second time**, and that task is the precedent for how to do it.
> - **The readout half is small** (**G11**): `ReadoutScope` gains a third member, `ScopeOf` stops being a
>   ternary, `Read`/`ReadHousehold` gain a third entry point, and `World.BalanceOf(Handle<Business>)`
>   **already exists** and gets its first caller.
> - ⚠ **`Readouts.cs:105-116` predicted this and chose the shape in advance** — *"two entry points rather
>   than one switch … a single method taking an `(entity kind, slot)` pair would be two switches wearing
>   one signature."* ***Follow it rather than collapsing it.***

### 8. **What creates a Business, and what capitalises one.** *(✅ CLOSED 2026-08-24)*

✅ **THE FOUNDER HALF SHIPPED 2026-08-24, and it needed task 7 first.** `adr/0146` said so at the time —
*"the founder is recorded by the employment link, an unpremised Business has no Building, and a
`Workplace` handle addressing `BuildingTable` cannot point at one"* — so this is the ordering working
rather than the plan drifting.

- ✅ **The subject moved from Household to Citizen.** `World.Found(Handle<Citizen>, …)` resolves the
  Household for the money, because **a Citizen owns nothing** — `CONTEXT.md` puts the balance on the
  Household and `adr/0146` declined to move it for this.
- ✅ **The trigger is three predicates over columns that already existed**: `Workplace` does not
  resolve, the Household's `Dwelling` does, and the balance covers the band. ⚠ **The first is what
  makes founding a CHOICE**: the employment pass and this one draw from the same people and neither
  knows the other exists (`adr/0017`).
- ✅ **No `founder` column, and the job is the record** — `adr/0146`'s own argument, which is that
  declaring a severable `BusinessTable` → `CitizenTable` handle makes the two tables mutually
  dependent at construction and they are built in one ordered pass.
- 🔴 **`PurposeTag.FoundingTrade` is now drawn on the CITIZEN's monotonic id**, not the Household's.
  Same tag, different subject: the decision is unchanged and the old subject is gone entirely.
- 🔴 **The refusal was found by asking what happens to a founder of a jobless trade**: they are over
  the ceiling from the instant they are hired and `EvictOverflow` sacks them on the next sweep. **It is
  every declared trade rather than at least one**, because the draw is uniform.

✅ **THE FOUNDING HALF IS BUILT AND MEASURED. `rulesets/founded.toml` is the FIRST SHIPPED FILE IN
WHICH THE SIMULATION CREATES A BUSINESS** — **114 founded** over 4,096 Ticks at 2,000 Citizens.
***That is milestone 27's named risk retired on its main axis***: the economic actor now exists in a
world rather than in a fixture. `World.Found`, `FoundingRuleset`, `PlacementEngine.Found`,
`UnpremisedTable.Gate`, `purpose_tag` 25 and 26, **seven new refusals** (`adr/0048`: 141 → 148), and
the assertion tier green at **2,114**.

- ✅ **`adr/0006` is satisfied and MEASURED rather than argued**: with the bound patched to one Day,
  **170 founded, 138 retired, the pool settling at 32**. The size is bounded because the drain rate is
  proportional to the stock — which is the claim `adr/0006` actually makes, and is not *the pool
  empties*.
- 🔴 **The seventh refusal is the one that matters and it was found by trying to write the Ruleset**
  (**G25**): `[founding]` is an inflow into the unpremised pool and **nothing tenants a Business**, so
  a file stating it without `gives_up_after_days` grows a collection with elapsed time. ⚠ **The base
  file states no such key**, so the first draft of `founded.toml` was an `adr/0006` violation.
- 🔴 **The arrival channel is NOT built.** `adr/0145` names two channels; this is one. `Gate` is
  declared and every row reads `default` until it lands.
- 🔴 **Nothing tenants a Business, so `founded.toml` LEAKS BY CONSTRUCTION** — its header says so at
  length. Founding is a transfer; departing is an export.

✅ **THE SOURCE IS SETTLED: [`adr/0145`](../docs/adr/0145-a-business-is-founded-by-a-household-or-arrives-through-a-gate-and-both-land-in-the-pool.md),
2026-08-24, with the user in the room.** **Two channels** — a **Household founds** one, spending part
of its balance; or one **arrives through a gate** carrying a band. Both create it **unpremised**, into
the pool milestone 25 built, and neither tenants anything, so `adr/0069` is preserved. ⚠ **The founding
channel is a TRANSFER and issues nothing**, so **G8**'s already-miscounted map of money's doors gains
**one** rather than two.

- 🔴 **THREE numbers are owed, not one** — a founding band, an arrival band and a founding **rate**.
  `0002` §D2 holds one row and must become three. ⚠ **The rate has no precedent to copy.**
- ✅ **The founding TRIGGER is settled by `adr/0145`'s own amendment, 2026-08-24**: ***a Household
  founds on its own MEANS and never on the city's NEED.*** A pass on the placement interval draws a
  bounded sample of **housed** Households; a drawn one founds if its balance covers the band, and the
  band **moves** Bin to Bin. **Nothing consults how many shops exist** — a trigger that reads a
  shortage is the RCI meter whatever it is called. The duration is authored and the sample derived
  (`adr/0059`), the draw is with replacement (placement's shape, so `1/e` goes unlooked-at), and it
  takes **`purpose_tag` 25**.
- 🔴 **A CONSEQUENCE NEITHER ADR SHOWS ALONE, and it is the thing to watch in the balance run**
  (**G24**): ***a founded Business that never finds premises EXPORTS its founder's money.*** Founding
  is a transfer and conserves; `Depart` subtracts from `MoneySupply.Issued` when the give-up bound
  expires. **So found-then-fail is a one-way leak of household wealth**, at a rate the founding
  duration sets. Not obviously wrong — the entrepreneur emigrated with their capital — but ***the
  founding duration is therefore not a free parameter.***
- ✅ **`UnpremisedTable.Gate` should now be declared**, and the table's own sentence is what licenses
  it: it was omitted because *"a column meaningless for every one of its rows is worse than one
  meaningless for half of them"*, and with two channels it is meaningful for half.

- ⚠ **Smaller than `0040` reads, because the exit shipped at milestone 25** (**G9**). The pool, the
  give-up clock, `Retire` and `Depart(Handle<Business>)` all exist. **What is owed is a source and a
  band.**
- 🔴 **`UnpremisedTable`'s two absences are this task's checklist, written by milestone 25**
  (**G12**). `Gate` is absent *"because a Business has no arrival door … what capitalises a Business is
  unanswered"* (`:29-35`); `Considered` is absent because *"nothing looks at premises on a Business's
  behalf"* and it *"arrives with the placement pass that gives it something to count"* (`:36-43`).
  ***A table declaration named this task's contents before this document existed.***
- ⚠ **`adr/0069` is preserved rather than strained**: construction houses **nobody**, and a pool plus a
  placement pass is what not auto-tenanting looks like. A Zone Rule that auto-tenants a shop is that
  rule broken on the commercial side.
- 🔴 **The issuance map is wrong by one before this task makes it wrong by two** (**G8**).

### 7. **Jobs and shift hours move to the employer.** *(last — 🔴 and it CANNOT be first)*

🔴 **This is the re-ordering** (**G5**). Today `jobs = 8` sits on the **dwelling** kind in all thirteen
shipped Rulesets, and `rulesets/minimal.toml:204-211` says why in its own words: *"IT IS ON THE DWELLING
RATHER THAN ON A WORKPLACE KIND … Living above the shop is the smallest arrangement in which the
assignment pass has somewhere to send anybody."* ***That is a stand-in for a workplace kind that does
not exist.*** Move `jobs` to the employer before anything creates an employer and `World.HasJob` returns
`false` everywhere: **employment goes to zero, every commute stops, and the traffic, parking and commute
suites go with it.**

- ✅ **Decision 2 is SETTLED and the answer is the KIND** — `adr/0141`'s Declares row, see above
  (**G30**). ⚠ **And only TWO of its three things move**: `jobs` and shift hours. **The wage is
  milestone 15's** (`06:99`), so a `[[business]] wage` key is out of scope here and
  `RulesetShape.cs:217` is wrong to say otherwise.
- 🔴 **This task is now a PREREQUISITE of task 8's founder half**, not the last thing in the milestone
  ([`adr/0146`](../docs/adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md)):
  the founder is recorded by the employment link, an unpremised Business has no Building, and a
  `Workplace` handle addressing `BuildingTable` cannot point at one. ⚠ **The ordering argument below
  still stands** — moving `jobs` before an employer exists still empties the city — ***so the two
  constraints bite in opposite directions and task 8's founding pass had to land first.*** It did.
- **33 sites assume a Workplace is a Building** — storage, Ruleset, World mutators, engines, invariants,
  evidence and content. The enumeration is the survey's; the load-bearing ones are
  `CitizenTable.Workplace` (`HandleColumn<Building>`, `CitizenTable.cs:64-65`), the worker list
  (`World.cs:2046-2047`) and `CommuteRoster`.
- 🔴 **Hash-bearing by construction, and not because of a column** (**G6**). The shift-start draw is
  keyed on **the Building's monotonic id** (`CommuteRoster.cs:186`); its own comment at `:77-79` says
  *"Drawn on the Building, which is what makes hours a property of the job."* Change the subject and a
  `purpose_tag` coordinate changes, so ***every Citizen in the city re-rolls their shift start.***
  **Every golden artefact re-records.**
- ⚠ **The search half is separable, because it is measured inert** (**G7**). `EmploymentEngine.cs:52-61`
  records the Commute-Budget box holding **100.0%** of the world's Buildings up to ~160,000 Citizens.
  ***So the employer can move without the search's behaviour moving*** — and equally, no shipped world
  tests the box at all.
- ⚠ **Thirteen Ruleset files, not twelve.** Two of them (`bordered.toml`, `crowded.toml`) declare a
  second building kind, `port`, which declares no `jobs` and no band.
- 🔴 **Owed on the day: `WorldInvariants.cs:1014`** (**G7a**).

### Closing

10. **Something to look at, and the long run.** *(✅ CLOSED 2026-08-24)*

    ✅ **`F43`'s question was asked first and the answer is the opposite of milestone 25's.** *Which
    world contains a Business the city created?* **All sixteen** — `adr/0148` made construction
    instantiate a kind's declared trade — and `rulesets/levied.toml` is the only one holding all four
    quarters of the risk at once. ⚠ **There is no longer a shipped world without an economic actor in
    it**, which `BusinessDumpTests` had to hand-build a Ruleset to test the refusal against.
    - ✅ **`--business` ships**, the twelfth runner mode: five panels, one per claim in the risk, plus
      a stock-over-time table. 🔴 **Two of the five flows are DERIVED rather than counted** —
      nothing counts a Business instantiated by `World.Fit` or razed by `World.DestroyBuilding`, so
      the panel prints the Zone Rule's `created` and `demolished` beside them and says the equality
      is an inference. **G42.**
    - 🔴 **THE LONG RUN FOUND A DEFECT AND IT IS THE TASK'S WHOLE VALUE** (**G43**). `adr/0148`
      identified *the trade this kind came with* **by kind**, and `[founding]` draws uniformly over
      every declared trade — so a founded shop and the instantiated one were interchangeable in a
      Building's list. **Two defects, pointing opposite ways**: the founded shop's capital left the
      city through `Raze` (23,983 of 354,562 per 20,480 Ticks) and the instantiated one outlived its
      premises into the pool (52 stranded, immortal). `BusinessTable.Origin` is the repair;
      `adr/0148` carries the amendment; every golden re-recorded.
    - ✅ **The three collections are NAMED IN THE TEST rather than found by it** (`0040` **F44**):
      the `business` table, the `unpremised` pool, and every Business's Bins.
      `BusinessLongRunTests` asserts all three on slot high-water marks, which is **F45**'s shape.
    - 🔴 **What bounds this city is the SOURCE EXHAUSTING and not a sink firing** (**G44**), so the
      test asserts a **deceleration** rather than a ceiling. ***That bound reopens the day anything
      refills household money*** — milestone 11's gate, milestone 26's revenue.

---

## What this milestone must not do

- **Must not make `Scope.Pool` resolve.** That is milestone **26**, and it is blocked on this one.
  `RuleEngine.cs:875` throws deliberately and the loader accepts the name on purpose.
- **Must not auto-tenant.** `adr/0069`, on the commercial side.
- **Must not settle decision 1 by argument where a measurement would do**
  ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)),
  and must not write the capitalisation band without a named ratifier
  ([`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)).
- **Must not cite hash movement as a reason to defer, narrow or split.**
  [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md).
  **Task 7 moves every commute in every golden artefact**; what is owed is attribution in the commit
  subject.
- **Must not run task 7 before task 8.** **G5**, and it is the only ordering constraint in this document
  that fails loudly rather than subtly.

---

## Definition of done

`CLAUDE.md`'s cumulative list, refined for this milestone:

- **`dotnet test` — the whole suite, unfiltered — green on the reference machine.** The commit gate
  stays the assertion tier ([`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)).
- **The invariants pass**, including `DerivedRebuildAuditTests` — task 6 adds columns and its hard count
  `Assert.Equal(41, all.Length)` moves.
- **The long-run test passes.** ⚠ **Name the collection before the run rather than after it** — 25's
  closing task got this wrong and had to be re-aimed (`0040` **F44**).
- **There is something to look at** — and ⚠ **check that it exists before the closing task**, not
  during it.
- **The risk is named and retired**: the economic actor **exists in the build** — the city creates one,
  funds one, employs through one, and a Rule can read its balance.

---

## What task 6 found

**G14 — 🔴 `FactorioTests` carried THREE wrong column counts and the corruption test could not see any
of them.** Two comments said a table's saved columns numbered *five*: `business` has **four** (and had
**three** before this task, so the number was never right), and `unpremised` has **two** — that one was
copied from the paragraph above it on the day it was written. And the union total lived in **two
places at two values**: the `UnreachableColumns` doc said *187* while the remarks eleven lines away
said *249*. ⚠ **The test itself was green throughout and would have stayed green**, because it asserts
on an *empty residue* and prints the totals; nothing compares a prose count to `SavedColumns`. ***A
number in a comment beside the code it describes is exactly as exposed as one in a document***, and
`tests/Borough.Tests/Corpus/` is document-to-document by construction, so no mechanical check reaches
it. **Repaired by counting from `BusinessTable`'s constructor and by deleting the totals rather than
correcting them** — the run prints `250 of 250`, so the document had no business holding a copy.

**G15 — the golden re-record needed a FIXTURE change before the number was taken, and this is
`Golden/README.md`'s own lesson arriving for the fourth time.** Both Businesses in
`GoldenFixtures.Build()` carried kind `0`. A straight re-record would have moved the baseline on the
day the column landed and then **stayed put for ever however the trades were shuffled** — covering the
column's *existence* and never its *value*. They now carry kinds **1** and **2**, and the two hashes
differ (`0xD92277A7748AC804` at kind 0 against the committed `0xD32D3335AA6D991E`), which is the proof
the value is folded. ⚠ **Both trades are DERELICT** — `minimal.toml` declares no `[[business]]` — which
is legal exactly as a derelict Building is, and is why `tenanted.toml` exists to cover the other half.

**G16 — only ONE of the four golden artefacts moved, and which one is the finding.** `world-hash.txt`
moved; **neither session trace did, and no Ruleset content hash did**. `GoldenFixtures.Build()` is the
only golden world that holds a Business at all — the sessions run `populate` over `minimal.toml` and
nothing creates one — so a column added to a table with no live rows folds nothing. ***A saved column
can move one baseline and leave the others alone, and that is the baselines reaching different tables
rather than a hash being missed.***

**G17 — `RulesetWithLayersTests` caught the two new properties being dropped on the hot-reload copy,
and it exists for that.** `Ruleset.WithLayers` is hand-spelled, so `BusinessKindCount` and
`BusinessKindKeys` had to be added to it by hand; `Every_settable_property_is_carried_across` went red
on the first run. ⚠ **They are `init` properties rather than constructor parameters, on `KindKeys`'
precedent** — the constructor's nine positional parameters are named by every hand-built test Ruleset,
and adding a tenth would edit all of them to say nothing.

**G18 — `tenanted.toml` and `minimal.toml` produce the IDENTICAL city, and that equality is the task's
demonstration.** 200 Ticks at 10,000 Citizens, State Hash sampled every 64: `0xE1C095A33C529A68`,
`0xBD0CF73451B88C49`, `0x992AA56D04096835` on both files. ***Two trades declared and neither
instantiated leaves the simulation bit-identical***, which is *a Business kind declares nothing until
task 7* stated as a number instead of as a promise. **Guarded by `BusinessKindLoadTests.The_two_trades_change_nothing_about_the_city`**, which carries its own control — `evicted.toml` is traced alongside and asserted *different*, because two traces of four zeroes are equal and an equality that compares nothing passes. ⚠ **It is a tripwire and is meant to go red**: the day task 8 creates a Business the two files stop agreeing, and the response then is to **delete the test rather than loosen it** — an equality weakened to a tolerance asserts nothing.

**G19 — a Ruleset cannot state its own content hash, and this header did for the length of one edit.**
The fingerprint covers the comments (`CLAUDE.md`'s `congested.toml` cell says so about a *golden*
file), so writing the value into the file it fingerprints invalidates it on the next keystroke — which
is what happened: `0x3291B0CCD977EB3A` became `0xB690D3419F285764` because the sentence quoting it was
added. ⚠ **No shipped Ruleset does this and none ever did** — the recorded fingerprints live in fixture
constants and trace headers, *outside* the file — so this is a deviation caught the same hour rather
than a corpus defect. **The repair is to name the runner that prints it, not to keep the digits
current.**

**G20 — decomposition's own open decision 3 rested on a false premise, and task 6 disproved it by
landing.** The decision predicted task 6 would falsify half the `sweeps` refusal message. It did not:
the message refuses `business` because ***"a Business has a balance and no pass that moves it"***, and
task 6 added a **kind** rather than a **pass** — verified against `World`, where every write to
`Businesses.Balance` is a creation, a lookup or a rebuild and none is a per-Tick pass. **Caught only
because the message was read instead of the plan's description of it.** ⚠ **The prediction reasoned
from a reason the message does not give**: decomposition assumed `sweeps = "business"` was refused for
having *nothing to key on*, which is what the milestone's risk cell says about `Declares` and `BinsOf`
— a true sentence about a different thing. ***This is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
catching a description written by this milestone's own decomposition eight hours earlier***, which is
the shortest gap between writing a wrong description and being misled by it that this corpus has
recorded. **The refusal count stays put; decision 3 reopens at task 9.**

⚠ **The same entry cited `RulesetLoader.cs:2170-2192` and `ReadSubject` now starts at 2188** — a line
reference that drifted within one task, because task 6 added lines above it. Replaced with the
**symbol**, which is `adr/0093`'s writing half — *name a symbol, never a time* — arriving on the same
sentence that had just been caught by its reading half.

**G21 — 🔴 THIS DOCUMENT'S TASK ORDER REPEATED THE MISTAKE MILESTONE 25 CLOSED ON, ONE DAY LATE.**
Task 9 was ordered second on the stated ground that it is ***exercisable by fixture*** — *"it does not
need a Business the city created; it needs a Business."* **`0040` F43, recorded 2026-08-23, is the
refutation**: milestone 25 shipped a mechanism every test of which built its Ruleset by hand, nothing
noticed it was invisible in every shipped world, and the closing task had to ship a **fourteenth
Ruleset file** to make it observable. ***A mechanism exercised only by hand-built fixtures is a
mechanism with no world in it.*** This document was written **2026-08-24** and used the refuted
reasoning as an ordering criterion.

⚠ **Task 9's case is STRONGER than F43's, not weaker.** F43's mechanism at least fired in a world once
a Ruleset was written for it. Task 9's would be reachable by **no Ruleset at all**, for two independent
reasons found on opening it (**G22**): nothing creates a Business, and nothing can give one a Rule.

**Reordered to 6 → 8 → 7/9 with the user in the room, 2026-08-24.** Task 8 creates the actor, which is
the risk `06` names, and it is what makes task 9's subject exist.

**G22 — 🔴 NOTHING CAN GIVE A BUSINESS A RULE, AND NO TASK IN THIS MILESTONE OWNS THAT.**
`World.ArmOccupant` takes a **Household**, reads the **Building's** kind, and hands every
`BinTenancy.Occupant` Bin and Rule from `BinsOf(kind)`/`RulesOf(kind)` to that Household. ⚠ **Run the
same function for a Business and a bakery inherits the Rules of the families living upstairs**, which
is [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)'s
own argument inverted — *the same shopfront hosts a bakery and then a barber* is the claim that the
trade is **not** a property of the walls. ***So a Business's Bins and Rules must come from its
`[[business]]` kind, and task 6 deliberately gave that kind nothing but a name.***

🔴 **`BinTenancy` is `Premises | Occupant` and cannot express the distinction.** Both a Household and a
Business are Occupants, so the enum that decides whose Bin it is cannot say *which tenant*. **Task 6's
own `RulesetShape` note said the trade gets `jobs`, shift hours and the wage at task 7 and named no
Bins and no Rules** — so the gap is real rather than deferred. **Unassigned by tasks 6, 7, 8 and 9
alike; it is the first thing task 9 needs and it is nobody's.**

**G23 — task 9's readout half has a fork `0040` and this document both missed.** `Readouts.ScopeOf`
returns **exactly one** `ReadoutScope` per Readout id and the loader tests it with an **equality**
(`RulesetLoader.ReadApply`). `Readout.Balance` is declared `Household`-scoped. ***A Business has a
balance too, so one declared scalar now belongs to two entities and the declaration cannot say so.***
⚠ **G11 called this half *genuinely small* and it is not** — it read the entry points, which are small,
and not the scope declaration, which is a fork. **Left open; it is task 9's to settle and it is likely
an ADR** (0145–0149 are reserved to this milestone).

**G24 — the source question was never a number, and the ledger had it typed as one for a day.**
[`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md) **V31** concluded
*"question 3 is not 'what creates a Business' — it is 'what CAPITALISES one', and that is a number."*
**Opening task 8 found there is no first line of code in that reading**: a band with nothing to draw it
is not a source. ⚠ ***A question that changes type has not necessarily been answered; it may only have
been narrowed*** — and the narrowing was recorded as a closure, so `0002` §D2 carried a *number* row
while the *shape* it depended on was owned by nobody. Settled by
[`adr/0145`](../docs/adr/0145-a-business-is-founded-by-a-household-or-arrives-through-a-gate-and-both-land-in-the-pool.md)
and its amendment, both 2026-08-24, with the user in the room.

🔴 **Settling it TRIPLED the ledger row rather than discharging it.** One band became a founding band,
an arrival band and a founding duration — because *what a founder spends* and *what an immigrant
carries* are different quantities with different arguments, which the single-channel reading had hidden.
***A row that looks like one number because nobody has decided the shape is a row that will grow when
somebody does.***

**G25 — the demonstration Ruleset found a refusal the ADR had not, and it found it by refusing to be
written.** `founded.toml` is `taxed.toml` plus a trade and a channel — and `taxed.toml` states no
`[placement] gives_up_after_days`, because it has no gate and therefore no inflow into the Unplaced
Pool. ⚠ **`[founding]` is an inflow into the OTHER pool**, so the first draft founded Businesses into
a collection with no sink, which is `adr/0006` outright. ***The defect was in the file rather than in
the code, and it surfaced because the file had to be written at all*** — which is `plans/0040` **F43**
paying for itself one milestone later: had task 8 shipped fixture-only, the mechanism would have been
correct, tested, green, and unbounded in the first world anybody wrote for it.

⚠ **`adr/0130` had already made this argument for the other pool and could not see this one.** *Whoever
builds the gate owes the give-up rule* is now two checks over two collections, and neither is reachable
from the other. ***A rule stated once about one collection does not generalise itself.***

**G26 — 🔴 `adr/0145` ARGUED FROM A COLUMN THAT DOES NOT EXIST, AND THE CODE SHIPPED THE SAME DAY
WITHOUT IT.** The ADR's `UNIQUE INDIVIDUALS` paragraph reads *"a Business founded by a named Household
has a founder the player can inspect — the money came from somewhere the player can point at."*
`BusinessTable` declares seven columns — `building`, `kind`, `bin_head`, `bin_tail`, `balance`,
`building_next`, `pool_slot` — and **not one records a founder**. `World.Found` moves the band and the
link is severed in the same statement. ⚠ **Nobody can point at anything.** ***The sentence was written
about the build the argument implied rather than the build the task produced***, which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
inverted — a description that is not where to look because there is nowhere to look. Filed to
`plans/0012` **Cause 5**; repaired by
[`adr/0146`](../docs/adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md),
which makes the claim true through the **employment link** rather than the column.

⚠ **It was not found by any corpus check and could not have been.** Every mechanical check in
`tests/Borough.Tests/Corpus/` is document-to-document — `RefusalCountTests` is the sole exception and it
counts one thing in one file. ***A prose claim about a table's contents is invisible to all of them***,
so the only instrument that sees it is somebody reading the table.

**G27 — the founder record and the workplace handle CANNOT BOTH be columns, and task 7 is what makes
that true.** `World` builds `Citizens` at `World.cs:147` and `Businesses` at `:167`, so declaring
`BusinessTable.Founder` as a handle into `citizens.Rows` works **today**, one line, on `Building`'s own
Severable precedent. ⚠ **Task 7 repoints a workplace at a Business**, and then `CitizenTable` needs
`Businesses` while `BusinessTable` needs `Citizens` — a **constructor cycle** in a single ordered pass.
The corpus's standing answer is a handle one way and an intrusive list the other (`DwellingNext`,
`WorkerNext`, `BuildingBusinesses`), so **exactly one may be the column**. ***Choosing the workplace
makes the founder fall out for free and costs nothing***, which is why `adr/0146` declines the column.
🔴 **Consequence for this document's task order: task 7 is now a PREREQUISITE of the founder record**,
not parallel work — an unpremised Business has no Building, so a `Workplace` handle addressing
`BuildingTable` cannot point at one.

**G28 — the income half of the founding cost is `adr/0026` and is UNBUILT, so 27 must not approximate
it.** The intent was *"the founder has a job that periodically has no income until the Business has
income."* `Readouts.cs:69` states the position in its own doc comment: *"income is a **flow** that
arrives with wages in milestone 15"*, `adr/0070`-classified **unbuilt**. ⚠ **And it is not merely
unbuilt, it is DESIGNED** —
[`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md) *wages are posted locally
and never cleared*, each Business adjusting its own wage by its own fill rate. ***So the described
mechanism is `adr/0026` running on a Business with an empty Bin***, and `adr/0114` already built the
Bin to be blamed and waited on. **Under `adr/0070` the answer is build wages at 15**, and a 27-shaped
proxy would put a second, worse answer in front of an ADR that already exists. **27 ships the labour
cost only**, which is real on its own: the founder is occupied and the employment pass will not hire
them.

**G29 — `CitizenTable.Employment` is SAVED, therefore HASHED, and nothing in `src/` writes or reads
it.** Declared `_rows.Saved<byte>("employment")` at `CitizenTable.cs:68`. The only writer in the
repository is `tests/Borough.Tests/Golden/GoldenFixtures.cs:510`, `(byte)(i % 3)` — an arbitrary value
in a fixture, with no enum, no constant and no consumer anywhere. ⚠ **So the golden baseline covers a
byte that means nothing**, and every State Hash containing it is folding a fixture's arithmetic.
***This is the mirror image of the `car_park.segment_next` defect `DerivedRebuildAuditTests` caught at
milestone 7***: that was a `Derived` column nothing rebuilt, this is a `Saved` column nothing writes,
and **no test asks the second question**. `adr/0146` gives the column its first meaning, which is
**defining** it rather than extending it. 🔴 **The general defect is unowned** — there is no audit that
names saved columns with no simulation writer, and this one was found by grepping for something else.

**G30 — an open decision was framed against an ADR that is silent and never cited the one that
answers.** Decision 2 asked whether `jobs` moves to the business **kind** or the Business **row**, and
concluded it was *"forced by what `adr/0026` means by employer"* — an ADR that never says.
[`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)
does say, in a table row headed **Declares**, whose two columns are the two **kind** namespaces; and
`RulesetShape.cs:217` was already reading it that way in a doc comment when decomposition ran. ***The
decision was open in this document and settled everywhere else.***

⚠ **The tell is that it named the ADR it could not get an answer from.** A question typed *arguable*
and pinned to one silent source looks blocked with no further work, because **the next step it implies
is a sitting rather than a search**. ***Typing a question does not discharge the duty to look for its
answer***, and the cheapest check — grep the term across `docs/adr/` — was never run.

🔴 **It cost the milestone an ordering.** Decision 2 was recorded as the one open decision blocking task
7, which is part of why task 7 went last; and `adr/0146` has since made task 7 a **prerequisite** of
task 8's founder half. ***A phantom block on the task that turned out to be the critical path.***

⚠ **And settling it SHRANK the task.** `0141`'s Declares row names three things and **the wage is not
milestone 27's** — `06:99` places wages at milestone 15 and `Readouts.cs:69` agrees. Only `jobs` and
shift hours move at task 7.

## Task 7's survey — taken 2026-08-24, and the task is SMALLER than this document had it

⚠ **Counted against the tree, not quoted.** ***A count of call sites sizes the work and settles
nothing*** — the same caveat the census above carries, and this survey exists because **G1** found all
three of the risk cell's numbers stale.

### The 85 is 64 doc comments

`grep -r Workplace src/` returns **85 lines across 24 files**. **Twenty-one are not comments**, and
**four of those are string literals** — two headings in `CommuteDump`, one refusal message in
`RulesetLoader`, and the `Invariant.CitizenIsInExactlyOneWorkplace` enum member. ***So the code surface
naming `Workplace` is seventeen lines.*** ⚠ **This does not contradict the *33 sites assume a Workplace
is a Building* figure**, which counts sites that assume it **without naming it** — `jobs` on the
building kind, the worker list, `HasJob`. **Both are true and they count different things.** ***What is
wrong is reading 85 as the size of the change.***

### The one door, and it is the reason this is tractable

**`World.Employ` (`World.cs:3966-3995`) is the only write onto `CitizenTable.Workplace`**, and its own
doc comment says so: *"The one door onto `CitizenTable.Workplace`, and that is what the reverse index
costs."* `World.cs:4107` clears it on the way out. ***So the signature change is one mutator and one
clear***, and every other site is a **read** that follows the handle.

### What does not exist yet, and is therefore the first piece of work

🔴 **There is no `BusinessKindDefinition`.** Task 6 shipped the second namespace as **names only** —
`Ruleset.BusinessKindKeys`, `BusinessKindKey(byte)`, `BusinessKindCount`, `DeclaresBusiness(byte)`
(`Ruleset.cs:2566-2735`). **There is no member holding `jobs` or shift hours.** `RulesetShape.cs:217`
already anticipates it in a comment — *"there is no `BusinessKindDefinition`… on that day this grows a
shape check"* — ⚠ **and that comment is wrong about the wage** (`plans/0012`, Cause 4). ***Building
`BusinessKindDefinition` is step one and nothing else can start before it.***

### The hash move is ONE line, and it is exactly where G6 said

`CommuteRoster.cs:186` — `ShiftStartOf(key, buildings.Rows.IdAt(workplace), definition)`. **The shift
start is drawn on the *Building's* monotonic id.** Point it at the Business's and ***every Citizen in
the city re-rolls their shift start***, so the Day's shape changes everywhere at once. **All four
golden artefacts re-record** — `world-hash.txt`, both session traces, and the driving trace. ⚠ **Under
[`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
that costs nothing and must not be cited as a reason to defer, narrow or split**; what it buys is a
commit whose subject explains it.

### ✅ The spatial search does NOT change, and that was worth checking

`EmploymentEngine.cs:352` samples **Buildings** — `BuildingsInCells.NthIn(box, …)` — and then asks
`HasJob(building)`. ***The box is over Buildings because that is where locations live, and a Business
has no location of its own.*** So the search becomes **sample a Building, then walk its Businesses**
through `World.BuildingBusinesses`, which milestone 27 task 8 already uses. ⚠ **Only the employer's
identity moves; the geometry is untouched** — which is **G7**'s *the search half is separable* holding
up under inspection rather than being assumed.

### What moves tables

- **`BuildingTable.WorkerHead` / `WorkerTail`** (`BuildingTable.cs:34-35`, both `Derived`) → **`BusinessTable`**.
  `CitizenTable.WorkerNext` stays where it is — it hangs off the Citizen and does not care what it
  points into. `World.cs:1884-1889` clears all three; `World.cs:2167` composes the `IndexList`.
- **`World.TryDeclaredJobs(byte kind, out int jobs)`** (`World.cs:3676`) reads `Rules.Kind(kind).Jobs`,
  the **building** kind → reads the business kind. ⚠ **Its *derelict kind keeps its workers* rule is
  load-bearing and must survive the move** — *"a designer deleting a paragraph must not sack a
  District."*
- **`World.HasJob(int buildingSlot)`** (`World.cs:3935`) → takes a Business slot.
- **`Evidence.cs:57`** and **`CommuteDump.cs:423`** both read `Rules.Kind(kind).Jobs` for display.

### 🔴 The survey's own finding — an unpremised Business employs nobody, and that is CORRECT

**A Business with no premises has no location**, so the spatial search cannot find it and the commute
roster cannot place a Trip to it. ⚠ **This lands directly on
[`adr/0146`](../docs/adr/0146-founding-costs-a-citizen-and-the-households-money-so-the-founder-is-the-first-worker.md)**,
which makes the founder the Business's first worker — **and the founder founds it UNPREMISED**.

***The existing code already produces the right answer and it should be left to.*** `CommuteRoster`'s
hours lookup returns `false` the moment the workplace handle does not resolve to a Building
(`CommuteRoster.cs:164-165`); after the move it resolves Business → premises, and an unpremised
Business fails the second hop the same way. **So a founder is employed, is counted as employed, and
makes no commute until placement tenants their Business.** ⚠ ***That is a legible story rather than a
hole*** — they have a business and nowhere to work yet — and it falls out of a guard that already
exists.

🔴 **It does need deciding rather than discovering, and the decision is task 7's**: whether `Employ`
**accepts** an unpremised Business at all. **Accepting is what `adr/0146` requires**; refusing would
make the founder unrecordable until placement and put the founder link back to needing a column.
✅ **`CommuteRoster.Add` tolerates it cleanly — READ, not assumed** (`CommuteRoster.cs:237-248`). It
guards the whole body on `TryPhasesOf`, so a `false` inserts into neither bucket and writes no
`CommuteBucket`. And `Employ`'s **remove-rewrite-add** order means `Remove` has already zeroed the
bucket (`:283`), ***so an unrosterable founder ends at zero rather than stale*** — which is the value
`Remove` itself treats as *not rostered*. **No new guard is needed anywhere.**

### 🔴 And the survey's second finding — NOTHING RE-ROSTERS A WORKER WHEN THEIR EMPLOYER IS PLACED

⚠ **This one is a gap the change CREATES, and it exists in no form today.** A founder employed by an
unpremised Business is correctly unrostered. **When placement later tenants that Business into a
Building, the Citizen's workplace becomes reachable — and nothing calls `Commutes.Add` at that
moment.** ***So the founder would stay unrostered for ever, employed by a Business that now has
premises, and never make a commute.***

**It cannot happen today**, because a Workplace is a Building and a Building never stops having a
location. ***The severable handle only ever goes one way — a demolition takes a workplace away, and
nothing has ever given one back.*** ⚠ **Pointing the handle at a Business makes the reverse
transition real for the first time**, because a Business acquires premises. **Task 7 owes a
re-roster at the placement site**, and `Employ`'s own doc comment names the failure shape it would
otherwise produce: *"a Building whose worker list disagrees with the Citizens pointing at it — and
the disagreement is invisible, because the list is derived and therefore folds into no hash."*
🔴 **This one would be invisible in the same way**: an unrostered worker makes no Trip, and no
invariant counts Trips that should have happened.

**G31 — 🔴 ~~THERE IS NO UNKNOWN-KEY CHECK IN `RulesetLoader`, SO EVERY TYPO IN EVERY RULESET IS
SILENT.~~ CLOSED 2026-08-25, and it was not empty when it opened — it had EIGHTEEN FILES IN IT.**
Found 2026-08-24 by task 7 writing a test that asserted one exists. ***It does not.*** Every
table reads the keys it wants by name — `TryInteger(table, "jobs", …)` — and **nothing ever enumerates
a table's actual keys**, so a key the loader does not look for is not merely unread, it is
**unnoticed**. `occupent = 3`, `revisit_tics = 1024`, a `[[building]]` key spelled for a
`[[business]]`: all load clean, all do nothing, none says a word.

⚠ **It is the exact failure class this loader has 150 refusals to prevent**, arriving underneath all
of them. `adr/0048`'s standing rule is *refuse a file that would load and run and mean nothing*, and
***a mistyped key is that file by construction*** — the designer wrote a number, the number has no
effect, and the only symptom is a city that behaves as though they had not.

🔴 **The claim was made three times before it was checked**, which is this session's own second
instance of the thing `plans/0012`'s new survival section is about. Task 7's first draft asserted the
unknown-key check in `BusinessKindDefinition`'s doc comment, in `ReadBusinessKinds`' remark and in
`tenanted.toml`'s header — **all three written from the same assumption, none from the code** — and
the test written to *demonstrate* it is what refuted it. ***Writing the test is what made the claim
falsifiable; writing it three times in prose did not.***

⚠ **Not fixed here, and the reason is scope rather than cost.** ~~Closing it means every table
declaring its permitted key set, which touches every reader in the file and would move the refusal
count by a lot.~~ 🔴 **BOTH HALVES OF THAT ESTIMATE WERE WRONG, and the shape was the reason.** It
cost **two** refusal sites rather than "a lot" (170 → 172) and touched **no** reader, because the
permitted set was not authored at all: `Find` is the one method every key read in this file funnels
through, so ***recording what it is ASKED for derives the permission from the code that does the
reading***. ⚠ **The proposed fix was worse than not obviously cheaper — it was the wrong shape.**
Twenty-two hand-authored name lists is twenty-two things to forget, and `adr/0048` records the
unknown-*section* list doing exactly that at the merge the day before this was written: eighteen
tables named on one branch, nineteen on the other, `[water]` on neither.

🔴 **WHAT IT FOUND ON ITS FIRST RUN IS WHY THIS ENTRY MATTERS MORE THAN ITS FIX.** Four `[layers]`
keys — `noise_range_metres`, `noise_intensity_percent`, `desirability_pollution_percent`,
`desirability_noise_percent` — sat **above** the `[layers]` header in **all eighteen shipped
Rulesets**, stranded inside `[placement]` (or `[founding]` in `founded.toml` and `levied.toml`),
***from milestone 9 task 3 until the day this check ran***. ⚠ **Every one of them authored exactly
the loader's default**, which is the whole reason sixteen months of green tests never saw it: the
city was right, the numbers were right, and the file said nothing to the simulation. Re-homing all
nineteen keys moved three Ruleset **content fingerprints** and ***not one State Hash***.

⚠ **The cost was never a wrong city and stating it as one would miss it.** It is that a designer
opening `minimal.toml` and retuning `noise_intensity_percent` — a key whose comment block runs to
twelve lines and carries the one derivation on the page — would have had **no effect and no
message**. `adr/0015` promises hot-reloadable tuning; ***a key that is not in the table it appears to
be in is not tuning, it is decoration.*** ⚠ **A nineteenth, `fertility_pollution_percent` in
`varied.toml`, was written during milestone 24 and had the same defect** — so this was a shape the
format admitted rather than one old slip, and it was still being made this month.

⚠ **One refusal MESSAGE was teaching the mistake.** The unquoted-decimal refusal advised writing
`decline_rate = "0.15"`, and `decline_rate` is not a key of `[[building]]` — so an author following
the advice in front of them wrote a line that did nothing, and the test asserting that advice worked
(`A_quoted_decimal_is_not_refused`) passed for the same reason the eighteen files did. ***A test can
only see what the build can see.*** The example no longer names a key, and the test now asserts the
refusal it gets is the key's and not the decimal's.

⚠ **A FIFTH dead key turned up in a TEST FIXTURE, and it is the most telling of them.**
`PopulatorDoorTests.Rules()` authored `kind = 1` on a `[[building]]`. A kind's id is its
**declaration order** — the loader assigns it and reads no such key — so the line restated what
position had already decided and would have gone on being right by accident for ever. ***A fixture
that states a number the loader never sees is a test asserting its own assumption***, and the only
thing that could ever have found it is the check that enumerates what a table actually contains.
**Deleted rather than corrected**, because there was nothing to correct. **What task 7 shipped instead is one named refusal for `wage`**, because
[`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)
gives a designer positive reason to write that key and the general gap would swallow it. ***A named
refusal for the one key somebody will actually type is not a substitute for the check and must not be
recorded as one.***

**G32 — 🔴 NOTHING TENANTS A BUSINESS, THE BUILD SAYS THIS MILESTONE OWES IT, AND NONE OF ITS FOUR
TASKS NAMES IT.** Found 2026-08-24 on opening the handle move. `PlacementEngine.cs:190` states the
debt in its own words: *"nothing tenants a Business — `World.CreateBusiness` has no production caller
and the placement pass that would is milestone **27**'s."* ⚠ **Tasks 6, 7, 8 and 9 are the kind table,
jobs to the employer, what creates and capitalises one, and a Rule reading a balance.** ***A pass that
puts a Business into premises is in none of them.***

**The evidence, counted rather than argued.** `PlacementEngine` names Business eleven times and
**never writes `Businesses.Building`** — the only writers anywhere are `World.cs:970`
(`CreateBusiness`, taking premises from its caller, and every caller is a test) and `World.cs:1612`
(clearing it on departure). `World.Place` is Household-shaped throughout: `Handle<Household>`,
`Households.IsUnplaced`, and a `HasRoom` that counts Households.

🔴 **So the handle move CANNOT go next, and the reason is arithmetic rather than judgement.** Point
`CitizenTable.Workplace` at a Business and employment is the number of Citizens working at a
**premised** Business. **Thirteen of the fifteen shipped Rulesets declare no `[[business]]` at all**;
`tenanted.toml` names two trades and instantiates neither; `founded.toml` founds 114 and **every one
of them is unpremised by construction**, waits out the give-up bound and leaves. ***So employment
would be zero in every world that ships***, and the commute, traffic and parking suites go with it.

⚠ **This document predicted the symptom and misattributed the cause, and that is the correction worth
keeping.** The status block says task 7 *"empties the city of jobs"* and concluded task 8's founding
pass had to land first — **which it did, and it was necessary and not sufficient.** ***Founding fills
a POOL; employment needs PREMISES***, and nothing carries a Business from the first to the second.
🔴 **Task 7's own survey repeated the error one step later**, closing with *task 8's founding pass had
to go first, and did* — treating a discharged precondition as the only one. ⚠ **The tell was in the
survey's own finding**: it recorded that *an unpremised Business employs nobody, and that is correct*
without asking **how many Businesses are premised in any shipped world.** ***The answer is none, and
it was one grep away from a paragraph that had already framed the question.***

**What the pass needs is a decision this milestone has not taken.**
[`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)
says `occupants` *"stops being how many Households and becomes how many tenants of any kind"* — so a
Business placed into a dwelling **competes with families for the same capacity**, and
[`adr/0068`](../docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s
*an over-capacity Building evicts* then reaches both. ⚠ **That is a design change with an eviction
consequence and not a mechanical one**, which is why this is filed rather than built on sight.

**G33 — `World.Unpremise` never removed the Business from its Building's list, and the defect was a
save/reload divergence hiding behind an unreachable path.** Shipped at milestone 25 task 5, it set
`Businesses.Building[slot] = default` and joined the pool — and **left the row threaded into the
premises it had just left.** ⚠ **`BuildingBusinesses` is derived**, so a **rebuilt** world walks
`Businesses.Building` and omits the ghost while a **maintained** one keeps it. ***That is a
`(derived AND rebuilt)` disagreement, which folds into no hash and is therefore invisible to every
determinism check the build has.***

🔴 **It was unreachable rather than benign.** Nothing called `Unpremise` from `src/` except
`World.Destroy`, which drains the list with `PopFront` **before** calling it — so the missing line
never mattered. `adr/0147`'s pass is what made it reachable, and **`EvictOverflow` is what would have
found it the hard way**: that loop drains until `Tenants(slot)` falls, and a ghost tenant is a count
that never falls. ***A silent divergence would have surfaced as a hang.***

⚠ **The fix is a `Remove` and not an assert, because one caller legitimately arrives with the row
already gone.** `Destroy`'s `PopFront` drain means `Remove` walks, fails and returns `false` — a
no-op bounded by `occupants`, which is a Ruleset constant. ***An assert would have been correct about
the invariant and wrong about the caller.***

**G35 — a mechanism can be in the BUILD and in no WORLD, and this plan's own mitigation was the first
kind.** Task 7's entry states the risk exactly — *"move `jobs` to the employer before anything creates
an employer and `World.HasJob` returns `false` everywhere: employment goes to zero, every commute stops,
and the traffic, parking and commute suites go with it"* — and records it as discharged because task 8's
founding pass *"had to land first. It did."* ⚠ **It landed in one file of fifteen.** The check that would
have caught it is [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md) **F43**'s own
question, which this milestone already carries in task 10 and asked one task too late: ***which world
contains one, and does any shipped file produce one?*** 🔴 **A prerequisite is discharged by the WORLDS
the next task runs against, never by the code existing.** The symptom was 66 red assertions whose
messages all said the same sentence, which is the cheapest possible diagnosis and was still a full
triage after the fact.

**G36 — 🔴 A SOURCE IS PAIRED WITH THE SINK THAT INVERTS IT, AND A TIMEOUT IS NOT ONE.** `adr/0148`'s
first draft had a kind-declared shop land in the unpremised pool on demolition, like any other tenant —
which reads as correct and is an `adr/0006` violation. Measured on `minimal.toml`, which condemns every
dwelling it raises: **121 Businesses became 1,095 over 32,768 Ticks**, the unpremised pool carrying 907
of them, and the Unplaced Pool climbing behind it because re-premised shops took housing slots in
Buildings that already had their own. ⚠ **`gives_up_after_days` does not close it and that is the
finding**: `founded.toml` declares one and reached **1,275** on the same run. ***A bound drains a stock
at a rate; construction-creates-and-nothing-destroys is a source with no sink at all.*** With demolition
made `Fit`'s inverse the same run holds Businesses **equal to Buildings** at every reading. 🔴 **The
`adr/0006` obligation was satisfied by ARITHMETIC, not by a long run** — the long-run tests had been
failing on it for two edit cycles and I read them as content drift.

**G37 — 🔴 `SyntheticCity` SIZED THE CITY BY THE WRONG CEILING, AND THE TWO QUESTIONS HAD BEEN THE SAME
QUESTION UNTIL THAT DAY.** `occupants` is one ceiling over both kinds of tenant (`adr/0147`), so the day
a premises could come with a shop, *how many tenants fit* and *how many families fit* stopped having the
same answer — and `WantedBuildings` divided the Household count by the tenant ceiling, building **a
quarter too few homes** and queueing the difference for ever. ⚠ **Raising `occupants` 3 → 4 in the
content is what made the bug reachable AND is what makes it invisible**: the number moved and its
meaning moved with it, so a reader checking that housing was preserved would confirm it from the
Ruleset and still be wrong about the generator. `World.TryDeclaredHousing` now carries the distinction
and says in its own remarks which callers must ask it.

**G38 — clearing a severable handle is not tidier than leaving it stale; it deletes the mechanism.**
`DestroyBusiness` was first written to sever `Citizens.Workplace` to `default` along with draining the
worker list. Two tests refuted it from opposite sides: `ColumnBytesTests` could no longer construct a
stale severable handle **because nothing in the build produced one any more**, and `EvidenceTests` found
its severable branch unreachable. ⚠ **The two writes look like one act and are not**: the intrusive list
*must* be drained, because it is `(derived AND rebuilt)` and a recycled row with a live `WorkerHead`
hands its successor somebody else's staff; the handle *must not* be cleared, because `Reference.Severable`
exists to answer *my employer is gone* and `default` answers *I never had one*.

**G34 — the first draw over a MIXED population is the first place the distinct-tag rule has teeth.**
`CLAUDE.md`'s randomness rule — *every distinct use gets a distinct `purpose_tag`; reusing one
correlates two decisions invisibly* — has been satisfied by construction until now, because **every
prior draw ranged over one table** and one tag was one id space. `adr/0141` made a Building's
occupancy hold tenants of any kind, and `World.Loser` draws on an entity's **monotonic id**. 🔴
**Household ids and Business ids are independent sequences from different tables**, so Household 5 and
Business 5 both exist and under one tag would draw the **identical value** — ***two tenants of one
Building perfectly correlated in the decision about which of them loses their place.***

⚠ **It generalises past eviction and cost three tags rather than one.** The same argument applies to
*which pooled Business is tried* against *which pooled Business is asked whether it gave up* (both
over the unpremised pool, both on the same Tick), and to *which Lot a shop looks at* against *which
Lot a family looks at*. **27, 28 and 29.** ***A rule that has never bound is not a rule anybody has
tested, and this one bound in three places the moment a second population appeared.***

## What decomposition found

**G1 — 🔴 the risk cell states three numbers and all three have drifted.** `06:95` and `0003:250` say
the Business *"has three columns and no kind"* (**six** since milestone 25 tasks 1 and 5), that `jobs`
sits on the dwelling kind in *"all twelve shipped Rulesets"* (**thirteen** since `evicted.toml` landed
2026-08-23), and that `CreateBusiness` has *"twelve in `tests/`"* (**17**, across 7 files). ⚠ **The
risk itself is untouched** — no kind, no source, no band. ***A risk restated in counts is a risk that
goes stale without becoming wrong***, which is `plans/0012` **Cause 1** on a cell nobody thought of as
status.

**G2 — the `sweeps` citation is right about the symbol and wrong about the address.** `0040` task 6 and
`0040` **F9** both cite `RulesetLoader.cs:2026`; it is `src/Borough.Formats/RulesetLoader.cs:2170-2192`,
in **Formats** rather than Core. [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
exactly: *where such a sentence is wrong it is wrong about the trigger.*

**G3 — 🔴 task 6 trips the corpus's ONLY document-to-code check.** `RefusalCountTests` reads
`RulesetLoader.cs`, counts `Refuse(` call sites, and asserts the number of record in
[`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md):78
— **140** today. ⚠ **Every other mechanical check in `tests/Borough.Tests/Corpus/` is
document-to-document**, so this is the one that notices code. A `[[business]]` section adds refusals;
the ADR's *enumeration* is owed the new ones too, and the test's own failure message says so.

**G4 — `RulesetNames` is id→name only, and that makes a fifth namespace cheap.** `RulesetNames.cs:52-55`
holds four `string[]`; every accessor goes id→name (`:86-103`). The **name→id** direction is the
loader's private `Dictionary` set, used during parsing and discarded at `Read()`. So the fifth map is
five fields, one internal constructor, one accessor and one construction site — and the `byte` `Invert`
overload already exists. ⚠ **Nothing asserts *four*.**

**G5 — 🔴 THE SPECIFICATION'S TASK ORDER IS WRONG, AND RUNNING IT EMPTIES THE CITY OF JOBS. ✅ MEASURED 2026-08-24, NOT ARGUED.**

> **The experiment.** `rulesets/minimal.toml` patched to `jobs = 0` with its Shift band removed — the
> pairing is a two-way loader refusal, so the band has to go with the posts — the assertion tier run,
> the file reverted. ⚠ **This is a PROXY for task 7 and not task 7**: the task moves `jobs` to an
> employer, and while nothing creates one the effect on `World.HasJob` is identical. ***What it
> measures is the blast radius of a city with no posts, which is the city task 7 ships if it goes
> first.***
>
> **53 failures of 2,094**, in three buckets. **4** are `GoldenHashTests` and are a **confound** — a
> Ruleset content hash moved because a golden baseline artefact was edited, which is a file
> fingerprint and not the city. **2** are the two tests that perform *their own* `("jobs = 8",
> "jobs = 0")` edit and whose `Assert.Contains` anchor had already gone. ***The remaining ~47 are the
> city.***
>
> 🔴 **`DerivedRebuildAuditTests.Every_derived_column_is_exercised_by_some_world` is among them, and
> it was not predicted.** With no posts, `BuildingTable.WorkerHead`/`WorkerTail` and
> `CitizenTable.WorkerNext` are populated by **no world at all**. ⚠ **That is the one test `CLAUDE.md`
> names as the only thing that asks whether a `Derived` column is actually rebuilt** — so a task 7 run
> out of order does not merely break commuting, ***it hollows out the audit whose job is to notice
> hollowing-out.***

 `jobs = 8`
sits on the **dwelling** kind in all thirteen shipped Rulesets. `rulesets/minimal.toml:204-211` states
that this is a **stand-in**: *"Living above the shop is the smallest arrangement in which the assignment
pass has somewhere to send anybody."* Move `jobs` to an employer that nothing creates and `World.HasJob`
(`World.cs:3815`) returns `false` for every Building: employment is zero, `CommuteRoster` rosters
nobody, and every Trip-, traffic- and parking-dependent suite fails. ***The dependency the cut at
milestone 25 recorded — jobs cannot move to the employer without something that creates employers — was
stated and then not applied to the order the tasks were written in.*** ⚠ **The correct order is
6 → 9 → 8 → 7**, and its cost is that the milestone stalls at its **second** task on `0002` §D2 rather
than its third.

**G6 — 🔴 task 7 is hash-bearing through a `purpose_tag` coordinate and not through a column.** The
shift-start draw is keyed on the **Building's** monotonic id (`CommuteRoster.cs:186`), with the comment
at `:77-79` saying that is *what makes hours a property of the job*. Re-subject it and every Citizen
re-rolls their shift start, so ***the change is not confined to the Citizens who change employer.***
Every golden artefact re-records.

**G7 — ⚠ the job-search box is measured INERT, which makes the search separable from the employer.**
`EmploymentEngine.cs:52-61` records the Commute-Budget box holding **100.0%** of the world's Buildings
up to ~160,000 Citizens; `Radius` and `Home` are `internal` so `JobSearchBoxTests` reads the production
derivation. ***So moving the employer need not move the search*** — and no shipped world exercises the
box at all.

**G7a — 🔴 a doc comment is wrong about the city, and no corpus check can see it.**
`WorldInvariants.cs:1014` reads *"no shipped Ruleset declares a job — so the exemption carries almost
every row."* **All thirteen declare `jobs = 8`.** So `Invariant.CitizenIsInExactlyOneWorkplace` is
described as near-vacuous and is not, and the sentence is wrong about the **trigger**
([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).
⚠ **Every mechanical check in `tests/Borough.Tests/Corpus/` is document-to-document**, so a claim living
only in a doc comment is invisible to all of them — which is `0040`'s own **F-class** finding about
`BusinessTable`'s comment arriving a second time. **Routed to [`0012`](0012-corpus-audit.md) on the day**
([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)).

**G8 — 🔴 the map of money's doors is wrong by one, and task 8 adds a third.** `SyntheticCity.cs:257`
says its `Endow` is **"THE ONLY PRODUCTION ISSUANCE OF MONEY IN THE BUILD"**, and the same comment names
the reason it was true — *"`adr/0024` makes the Outside Connection money's only source and that is
milestone 11."* **Milestone 11 shipped**, and `World.cs:1242` endows an arriving Household from its
Hinterland band. ***So there are two production issuances, the comment says one, and task 8 proposes a
third.*** ⚠ **The comment is not merely stale — it names its own expiry and the expiry passed.**
**Routed to `0012`.**

**G9 — task 8 is smaller than `0040` reads, because milestone 25 shipped its exit.**
`Depart(Handle<Business>)` (`World.cs:1524-1554`) already mirrors the Household's: refuse a premised
Business, subtract the balance from `MoneySupply.Issued`, leave the pool, destroy the row.
`UnpremisedTable`, the give-up clock and `PlacementEngine.Retire` are all live. ***What is owed is a
source and a band, not a subsystem*** — and the band is the blocker.

**G10 — 🔴 task 9's real content is a third subject on the Rule Instance, and the build named it.**
`RuleInstanceTable.cs:91-95`: *"A Business gets its own column when a Business runs a Rule, which is
milestone 27."* `World.FindLocalBin` (`World.cs:3098-3111`) branches on `Household.IsNone` — a binary.
⚠ **This is milestone 25 task 2's shape repeated**, so that task is the precedent rather than a
comparison. ***The readout was the visible half and the subject is the work.***

**G11 — task 9's readout half is genuinely small, and the file chose its shape in advance.**
`ReadoutScope` is a two-member enum, `Readout` has three members of which two are declared, `ScopeOf` is
a ternary (`Readouts.cs:152`), and `World.BalanceOf(Handle<Business>)` already exists with no caller.
⚠ **`Readouts.cs:105-116` argues for a third *entry point* rather than a wider switch**, and predicted
this exact arrival. ***Follow the prediction rather than collapsing it***, because the collapse it warns
against — an `(entity kind, slot)` pair — would let a Building slot be read as a Household.

**G12 — milestone 25 wrote task 8's checklist into a table declaration.** `UnpremisedTable` has **two**
columns against `UnplacedTable`'s four, and both absences are argued forward by name: `Gate` *"because a
Business has no arrival door"* (`:29-35`), `Considered` because it *"arrives with the placement pass
that gives it something to count"* (`:36-43`). ⚠ **A column deliberately not declared, with the reason
and the milestone written beside it, is the cheapest specification in this corpus** — and it is also
invisible to every corpus check, which is **G7a**'s class again.

---

**G13 — 🔴 THIS DOCUMENT CALLED TASK 8 *BLOCKED* AND `adr/0052` REFUSES THAT READING IN ITS OPENING
PARAGRAPH.** The record says: *"This does not require ratifying a number before choosing it… **The rule
governs the record, not the timing.**"* And
[`0037`](0037-goods-between-buildings-the-district-pool.md):249 had **already made this exact
correction** for an identical situation — *"3 is an obligation, not a fork."* ⚠ **The wording was
inherited from [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md) task 8 and
carried forward without being checked against the ADR it cites**, which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
pointed at a decision record rather than at code.

🔴 **It is the same shape as G5 and it was committed by the document that reported G5.** G5's finding is
that a correct sentence sat in `plans/0003`'s gate column while the task order two lines away ignored it.
Here a correct sentence sits in `adr/0052`'s **first paragraph** while a plan citing that ADR by name
called the number a blocker. ⚠ **Citing a record is not applying it** — which is
`adr/0052`'s **own** stated failure mode, at its line 39. ***So the ADR predicted the way it would be
misread, and this document then misread it that way.***

**No mechanical check reaches this either**, and the two sightings now differ in a way that matters:
G5's correct sentence lived in a *plan*, and this one lives in an *ADR's opening claim*. A check that
asked whether a plan's use of a decision agrees with that decision's own summary is conceivable and is
**not proposed** — it needs the corpus to know which sentence of an ADR is its claim, and nothing does.

**G39 — 🔴 A DOC COMMENT NAMING A FUTURE MILESTONE SIZED A TASK, AND THE SIZE WAS WRONG BY A
SUBSYSTEM.** `RuleInstanceTable.cs:92` says ***"A Business gets its own column when a Business runs a
Rule, which is milestone 27"***, and **G10** built task 9's plan on it: *"the real content is a third
subject on the Rule Instance, and the build already says so."* The column was implemented — `[[rule]]
trade = "<name>"`, `BusinessTable.RuleHead`/`RuleTail`, `World.ArmTrade`, `RuleEngine.Band` dispatching
on the instance — and it **loaded and then crashed on the Tick it fired**:

```
StaleHandleException: handle {index 0, generation 0} into table 'building' is stale.
   at Borough.Core.Rules.RuleEngine.Fire(RuleVerdict verdict, Ticks tick)
```

⚠ **`RuleEngine` resolves a Building from the instance at `Fire`, not only at `Band`** — and through
evidence, `on_fail`, the wake targets and every local Bin lookup. **The column was the visible tenth of
a subsystem.** Reverted; `adr/0149` takes the route the loader's own refusal named, and task 9 is a loop
over a second table plus a membership test.

**This is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
working rather than failing, and the distinction is worth being exact about.** The comment is right about
**where to look**; what is in there was never its claim to make. ***G10 read a location as an estimate***,
which is the one use of a description that ADR forbids outright.

⚠ **What makes this sighting different from `plans/0012` Cause 4's four is that nothing was decided
wrongly** — no ADR rests on it, no number moved, no world behaves differently. **What it cost was
sizing**, and sizing is invisible to every check in this corpus. Filed to
[`0012`](0012-corpus-audit.md), where the entry directly above it proposes the check that would have
caught it: *a doc comment naming a future milestone is a citation, and a citation to a closed milestone
is checkable.* 🔴 **This one was a citation to an OPEN milestone that was simply wrong about what that
milestone would do**, which that check does not reach either.

**G40 — a Readout's scope was a value because there had only ever been one entity, and the widening is
the thing to watch.** `ScopeOf` returned one `ReadoutScope`; `balance` was declared `Household`-scoped
while `World.BalanceOf(Handle<Business>)` had sat in the build unused since milestone 25. ⚠ **A single
scope made over-permission unreachable** — a Readout could be wrong about *which* entity, never about
*how many* — and `IsReadableAgainst` gives that up to say something true. ***A Readout carelessly added
to two sets is a Rule reading a quantity that means a different thing on each***, and no test in this
corpus can catch it. Recorded in `adr/0149`'s revisit triggers rather than guarded, because the guard
would be a claim about what a scalar *means*, which nothing in the build represents.

**G41 — the shipped file could not assert the property the shipped file demonstrates.** `PolicyCounter`
is **one set of counters for the whole engine**, so a run of `levied.toml` reports every Policy's
`considered` and `applied` together — and its two Household Policies outweigh the trade levy about ten to
one. ⚠ **A counter that cannot be attributed to a Policy cannot assert a property of one**, so
`BusinessLevyTests` builds its fixture by cutting `founded.toml` at its first `[[policy]]` **table** and
appending the levy alone. 🔴 **The first cut was on the string `[[policy]]` and landed in the file's own
header**, four hundred lines above the tables — *"a [households] table and two `[[policy]]` tables"* —
producing a world with no Buildings in it. ***A fixture built by text surgery cuts on syntax, never on a
word the prose also uses.***

⚠ **The attribution gap is not filed as a defect and should not be.** Per-Policy counters would be a
Census surface nothing has asked for; what exists is enough to instrument the engine, and the test that
needed attribution got it by building a world with one Policy in it. **Recorded so the next person to
want a per-Policy figure knows it is absent by circumstance rather than by decision.**

**G42 — the two largest flows in the Business table are counted by nothing, and the dump has to
infer them.** `PlacementActivity` counts `Founded`, `Premised` and `Retired`; **instantiation and
razing are not placement events and no engine owns them**, so `World.Fit` and `World.DestroyBuilding`
create and destroy Businesses silently. On every shipped file the dwelling kind declares a trade, so
`ZoneCounter.Created`/`Demolished` *are* those counts — ⚠ **derived and not measured, and the equality
breaks on the first shipped kind that declares no trade.** `--business` prints the two rows adjacent
and says so in the panel rather than in a comment. **Not filed as a defect**: a counter nobody has
asked a question of is a column, and the inference is sound in every world that exists.

⚠ **A second, smaller gap in the same place**: `PlacementCounter` has no `Founded` or `Premised`
member, so those two flows exist only in the activity and **survive into no census series**. The dump
keeps the running total by hand — and the first draft did not, handed the whole `Simulation` to
`Census.Observe` (which drains every engine), and printed **0 founded** in a world visibly full of
founded shops. ***A flow that reads zero looks exactly like a mechanism that did not run.***

**G43 — 🔴 A KIND IS NOT AN IDENTITY, AND THE LONG RUN IS THE ONLY THING THAT COULD HAVE FOUND IT.**
`adr/0148` made demolition destroy *the trade this kind came with* and gave it the **kind** to identify
it by. `[founding]` draws uniformly over **every** declared trade, so a Household founding a `shop` put
a second Business of the dwelling's own trade into the same list, and demolition razed whichever came
first.

| | Before | After |
|---|---|---|
| Pooled Businesses holding nothing, `levied.toml`, 24,576 Ticks | **52** | **0** |
| Same, `founded.toml` | **60** | **0** |
| Same, `minimal.toml` and `taxed.toml` (nothing founds) | 0 | 0 |
| Money supply, `levied.toml`, 20,480 Ticks | 354,562 → **330,579** | 354,562 → **354,562** |
| `business` slots, `levied.toml`, 131,072 Ticks | 652 | **536** |

***Two defects from one line, pointing opposite ways.*** The founded Business's capital left the city
through `Raze`'s money-supply write; the instantiated Business outlived its premises into the
unpremised pool, where nothing ever collected it — because the give-up bound does not fire in this
world either. **The repair is `BusinessTable.Origin`**, a severable `Handle<Building>` naming the
premises that instantiated it, read by `DestroyBuilding` and by `Fit`'s idempotence guard, which had
the same bug on the creation side.

⚠ **`adr/0148` argued explicitly against the thing that fixes it**, and the sentence is worth quoting
because it is *nearly* right: *"a stored `came with the premises` bit would separate two Businesses
identical in every column, and the only case it decides differently is one where both answers are the
same."* ***The refused thing was a property of the Business and what was needed was a property of the
edge.*** A bit travels with the Business into whatever premises it is later placed in and gets it razed
there; a handle naming one Building stops meaning anything the moment it leaves. **The old
`HoldsTrade` doc-comment named the alternative, called it "the flag `adr/0148` refuses", and described
its own behaviour as "a deliberate over-refusal"** — so the cost was seen, priced and accepted, and the
measurement is what showed the price was wrong.

🔴 **What delayed finding it was a HEADER.** `founded.toml` opened with a long paragraph predicting
that its money supply would drain out through the Hinterland *on purpose*. The supply was draining;
the paragraph explained it; nothing looked further. ***Naming an expected symptom is how you stop
noticing the unexpected one that looks identical*** — and both of that header's claims were wrong, since
`adr/0147` had already shipped the placement half it said was outstanding. **No mechanical check
reaches this**, and the class is `plans/0012` **Cause 4** with the description in a Ruleset comment
rather than in code.

**G44 — the shop count is bounded by the SOURCE EXHAUSTING and no sink has ever fired.** Over 131,072
Ticks at 2,000 Citizens: **7,165 premisings, 0 give-ups.** `gives_up_after_days` is 30 Days and
placement re-premises a pooled Business long before that, so `adr/0142`'s bound — which `adr/0148`
correctly refused to rely on as *the* sink — has still never been reached in any shipped world. What
actually bounds the count is that founding is a **means test that runs out of means**: each founding
moves money out of a Household and employs a Citizen, so each one makes the next less likely.

⚠ **So `BusinessLongRunTests` asserts a DECELERATION rather than a ceiling**, because the count is
still rising at the end of the run and asserting a plateau would be asserting something the data does
not show. 🔴 ***A bound that rests on a source drying up reopens the day anything refills it***, which
is milestone **11**'s gate endowment and milestone **26**'s revenue. **Recorded rather than fixed**: a
sink that fires is not missing, it is *unreached*, and `adr/0070` says the difference matters.
