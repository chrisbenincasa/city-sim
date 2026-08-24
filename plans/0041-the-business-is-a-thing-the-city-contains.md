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

### 2. 🔴 NEW — does `jobs` move to the business kind, or to the Business row? — **found by decomposition**

**Task 7 says *jobs and shift hours move to the employer* and does not say which.** The build makes it
a real fork, because the two land in different places:

- **On the business kind** — `KindDefinition.Jobs` in a second kind namespace. Mirrors the building
  kind exactly, costs no new column, and keeps a fill rate authored.
- **On the Business row** — a saved column. Lets two bakeries of one kind employ different numbers,
  which is what `adr/0026`'s *a fill rate is a property of an employer* could be read to require.

⚠ **This is not `adr/0043`-measurable** — no number refutes either. It is forced by what `adr/0026`
means by *employer*, and ***the ADR uses the word without ever saying whether it means the kind or the
row.*** **Blocks task 7 and nothing else.**

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
`BusinessKindLoadTests`. **The whole assertion tier is green at 2,108.** Findings **G14**–**G24** below.

⚠ **There is no `BusinessKindDefinition` type and that is the decision, not an omission.** A
`[[business]]` carries a `name` and nothing else, because `adr/0141` gives the trade `jobs`, shift hours
and the wage and ***all three arrive with task 7***. `RulesetShape` compares identity and has no
`CompareBusinessKind`, for the same reason. **On the day task 7 lands, both grow.**

⚠ **The `kind` parameter on `World.CreateBusiness` is OPTIONAL and the argument is about consequence
rather than convenience.** `CreateBuilding`'s kind is required because a Building cannot be fitted
without one; a Business kind declares nothing until task 7, so requiring it would make **seventeen test
call sites** name a value nothing reads.

### 9. **A Rule can read a Business's balance.** *(second — it needs only the kind, and a fixture)*

⚠ **Moved ahead of 7 and 8** because it is the one entry in group B that is **exercisable by fixture**,
exactly as milestone 25's group A was — `GoldenFixtures.cs:531-532` already puts two Businesses in one
Building. ***It does not need a Business the city created; it needs a Business.***

- 🔴 **The real content is a THIRD SUBJECT on the Rule Instance, and the build already says so**
  (**G10**). `RuleInstanceTable.cs:91-95`: *"A Business gets its own column when a Business runs a Rule,
  which is milestone 27."* `World.FindLocalBin` (`World.cs:3098-3111`) branches on
  `RuleInstances.Household[instance].IsNone` — **a binary**. ⚠ **This is milestone 25 task 2's shape a
  second time**, and that task is the precedent for how to do it.
- **The readout half is small** (**G11**): `ReadoutScope` gains a third member, `ScopeOf` stops being a
  ternary, `Read`/`ReadHousehold` gain a third entry point, and `World.BalanceOf(Handle<Business>)`
  **already exists** and gets its first caller.
- ⚠ **`Readouts.cs:105-116` predicted this and chose the shape in advance** — *"two entry points rather
  than one switch … a single method taking an `(entity kind, slot)` pair would be two switches wearing
  one signature."* ***Follow it rather than collapsing it.***

### 8. **What creates a Business, and what capitalises one.** *(NEXT, as of 2026-08-24)*

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

10. **Something to look at, and the long run.** ⚠ **Read `0040` F43 before scoping this.** Milestone 25
    went to show a tenancy ending and found **the thing to look at did not exist in any shipped world**.
    ***The same question is owed here on the day, not at the end***: which world contains a Business the
    city created, and does any shipped file produce one? **No shipped Ruleset declares a `[[business]]`,
    because the section does not exist.**

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
