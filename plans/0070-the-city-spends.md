# 0070 — The city spends

**[`0045`](0045-amnesty.md) queue row 32.** Scoped 2026-09-09, against `main` at `36ff4fc`.

## Status

🟢 **SCOPED, RE-VERIFIED AND NUMBERED, NOT STARTED.** Re-read against `main` at `be07abc` on
2026-09-12, after row 33 shipped. **Two findings below were falsified by that row and are struck in
place; task 1 is void because row 33 built it.** The readout half landed earlier and out of order —
the console carries `Population` and `Treasury`, and row 33 added the budget panel beside them.

⚠ **The largest correction is F10.** A school with no staff teaches at full capacity, so the
demonstration this row is named after produced no observable change. `adr/0026` had already decided
the missing link and nobody had built it. **Every number this row needs is now chosen and recorded
in *The five numbers*; nothing is left to the implementer.**

## Why this exists

Money moves and the city does not spend. Wages travel Business → Household, shopping travels
Household → Business, a levy travels either → treasury, and a rebate travels back. `MoneyLongRunTests`
asserts the treasury is a conduit rather than an accumulator, and it is right. Nothing the city does
costs it anything. `PlacementEngine` raises Buildings with no withdrawal, `Simulation.ApplyService`
places a school for nothing, and `ServiceEngine` and `CivicEngine` run it for nothing, because every
`Cost` in both is a `TravelTime`.

So the player has a treasury and no fiscal decision, and
[`04 §5`](../docs/04-economy-and-goods.md)'s three levers — tax rates, service funding, borrowing —
have nothing under them. A budget is an expenditure the player can steer, and this row builds the
first one.

## What the build already holds

Read from the symbols.

| | What | Where |
|---|---|---|
| ✅ | A treasury holding money, one Bin per conserved Resource | `World.CreateTreasuryBin`, `TreasuryTable` |
| ✅ | Money in and out of it, both directions, as `[[policy]]` transfers | `PolicyEngine.Move` |
| ✅ | A player lever over a named Policy's amount, with a panel | `Simulation.ApplyGovern`, `Main.Panels` |
| ✅ | Wages from an employer's till to a worker's Household | `WageEngine.Pay` |
| ✅ | A Business raised with its premises, no founder and no capital | `adr/0148`, `World.CreateBuilding` |
| ✅ | A trade that goes under when it cannot make payroll | `[[business]] goes_bankrupt_after_short_paydays` |
| ✅ | Service Buildings, attendance and catchment | `[[building]] serves`, `ServiceEngine`, `CivicEngine` |
| 🔴 | No service Building tenants a Business in any shipped world, so nobody works at one | — |
| 🔴 | No door for money into the treasury at world creation — `World.Endow` deposits to a Household | `World.Endow` |
| 🔴 | No cost on any player act | `Simulation.ApplyService` |

### F1 — a Policy sweeps a whole table

`[[policy]] sweeps` accepts `household` or `business` and nothing else — no kind, no trade, no
District (`RulesetLoader.ReadSubject`). A transfer from the treasury to `local` therefore reaches
every Business in the city, and *fund the schools* cannot be authored, because it would pay every
shop the same subsidy. The funding lever needs a Policy that can name whose trade it sweeps, which is
a loader key and a filter in `PolicyEngine.SweepMembers` rather than a new mechanism. This sizes
task 1.

### F2 — staffing is floor area

A Business's jobs come from `[capacity] floor_tiles_per_job`, so a school would employ what its floor
area divides into. [`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md) wants
public jobs determined by catchment — teachers per child — and the build's own comments already class
that as `adr/0070` *unbuilt*. This row does not build it. A funded school with floor-area staffing is
still a school the treasury pays for, and catchment staffing changes who is hired rather than who
pays.

### F3 — the placement cost needs a payee

Money is conserved and the loader refuses a money term with no counterparty. `adr/0035` §2 settles
where construction money goes. It buys Materials, and imported Materials leave through the gate. No
import path exists — a Hinterland price is a ceiling that seeds a Pool's price, and no Bin receives a
payment — so charging for a placement writes `MoneySupply.Issued` down.

This finding first said *a third writer*, alongside an emigrating Household and a razed Business, and
both halves of that were wrong. Four decrements ship today, in `WageEngine.Bankrupt`,
`World.Depart(Household)`, `World.Depart(Business)` and `World.Raze`. What makes them alike is that
every one reads a Bin that is about to be freed, which is why each sits immediately before its
`Destroy` call and says so in a comment.

The novelty task 5 introduces is a live Bin losing money with no counterparty. That is smaller than
this finding claimed, and it moves where the risk sits. The thing to watch is whether the withdrawal
and the write-down stay paired. All four shipped sites are inside `World`, where the Bin's owner is.
Task 5 writes from `Simulation.ApplyService`, which is outside `World`, so the pair goes behind a
`World` door that `Simulation` calls rather than reaching into the column. Nothing catches an
unpaired write before `Invariant.MoneyIsConserved` at end of run, and that check names no cause.

### ~~F9 — row 29 shipped the kind task 2 was going to author~~ 🔴 **FALSIFIED, see F12**

`rulesets/schooling.toml` arrived with row 29, and its `college` declares all four keys together —
`serves = "education"`, `level = 3`, `premises = true`, `business = "tuition"` — plus a money Bin
owned by the business. A service Building that comes with its own employer is shipped content, so
task 2 verifies against a standing world rather than authoring one.

F5 is therefore live today, in a file nobody wrote for this row. A `college` is premised, so a grocer
or an office founded by a Citizen may take its tenancy. Whether that has happened in a
`schooling.toml` run is unmeasured. Look before assuming it has not.

This row's demonstration world is now a choice. `schooled.toml` has one service kind with no trade at
all. `schooling.toml` has four, one of which is already staffed. Pick against `schooling.toml` unless
task 2's assertion says otherwise, and record which and why.

### F4 — the gate on a school's employer is in `World`

The loader permits `serves` and `business` on one kind. It reads them independently, and the only
coupling is that a declared trade demands `premises = true`. Task 2's stated check therefore passes,
and passing it proves nothing.

`World.CreateBuilding` asks a second question the Ruleset cannot answer. It instantiates the kind's
trade only where `roomBeside` holds, which means the declared occupancy ceiling is greater than one.
A Business takes a tenancy exactly as a Household does (`adr/0147`), and on ground that divides into
a single tenancy the trade would take the only one. A school whose floor divides into one tenancy
gets no employer, no refusal and no diagnostic. It stands, it is attended, and nobody works there.
The demonstration reads as *funding does nothing*, and the cause sits in neither the Ruleset nor the
Policy.

Task 2 must confirm a Building that came with its trade, on the ground the demonstration world
actually lays. A file that loaded is not enough.

### F5 — `premises` is a permission and not a reservation

`World.HasRoomForPremises` reads the kind's `premises` flag and the room left under the ceiling. It
never reads the trade. `PlacementEngine`'s unpremised pass asks that predicate, so the moment the
school kind states `premises = true` to carry its teachers, every unpremised Business in the city
becomes eligible to move into a school. Under `adr/0147`'s single ceiling, the shop that does so
takes a slot the teachers needed.

This is the `plans/0053` failure with the roles swapped. There, a trade took the only tenancy and the
dwelling housed nobody. Here a foreign trade takes a tenancy and the school teaches short-staffed.
One ceiling counts both kinds of tenant, and it is working exactly as designed.

No task owns this. The cheapest containment is ground the demonstration controls, and a school with
more tenancies than its trade needs buys a margin rather than a fix. Whether a kind may reserve its
own tenancy is a question this row should ask and need not answer, and it belongs to `adr/0147`.

### F6 — payroll is swept before the funding Policy, in the same Tick

`Simulation` sweeps wages and then sweeps Policies. On any Tick where both fire, a till pays what it
holds before the treasury tops it up, so a school placed today meets its first payday with an empty
till.

`goes_bankrupt_after_short_paydays` counts from that first payday. Whether a fully funded school
survives long enough to be funded is a relation between the Policy's `interval` and the bankruptcy
tolerance, and decision 4 named neither. A tolerance shorter than the funding interval winds up every
school in the city on a schedule, and that looks exactly like the underfunding this row is trying to
demonstrate.

The two numbers are one number, chosen together, and decision 4 reads honestly as *underfunding fires
people once the relation is right*. Both are provisional under standing order 4 and both are recorded
at the end of this plan.

### F7 — a Policy pays in full or not at all, and a dry treasury stops the sweep

⚠ **TRUE OF `PolicyEngine` AND FALSE OF `SubsidyEngine`, which row 33 added — see F11.**

`PolicyEngine.Move` moves the whole amount or moves nothing, so a payer short of the full transfer
pays zero. `SweepMembers` then returns on the first such failure where the payer is the treasury, and
the remainder of that sweep does not happen.

With three schools and money for two, the outcome is not two-thirds each. Two are paid in full, one
is paid nothing, and the sweep's seeded start slot decides which. That is deterministic and
replayable, and it rotates between periods because the start is drawn per Tick.

`02 §4.2` already asks for *pays whom it reaches and reports where it stopped*, and this is it, built.
It is defensible, because a part-paid wage bill is not a thing a payroll can spend. The player
watches it happen, so task 4 states it rather than meeting it, and the console figure task 4 asks for
has to say what was paid and never what was owed.

### F8 — an opening balance must be absent by default, and refused on reload

The shape is precedented. `[households] opening_balance_min`/`max` is authored per world and read at
synthesis, and a `[treasury]` sibling reads the same way. Task 3 is missing two constraints on it.

It is refused on reload, the way `[layers] kernel_metres` is. A Ruleset is hot-reloadable
(`adr/0015`) and world creation is not. A reload that re-read an opening balance would mint money
into a standing city on every hot reload, and `Invariant.MoneyIsConserved` would stay green because
the issuance is recorded.

It defaults to absent, and `levied.toml` is why. That file's own comment records that `adr/0116`
chose an empty opening treasury deliberately, so the dry-sweep branch is reachable on the first sweep
rather than after a long run somebody has to construct. Decision 2 overrides `adr/0116` for the world
this row demonstrates and must not override it everywhere. A defaulted opening balance would delete
another file's demonstration to enable this one.

### F10 — a school with no staff teaches at full capacity, and `adr/0026` already decided the fix

`World.DeclaredPlaces` (`World.cs:6176`) divides `FloorTilesOf(slot)` by `[capacity]
floor_tiles_per_place` and **never reads whether the Building holds a live trade, nor how many people
work in it**. `WageEngine.Bankrupt` says in as many words that ***the premises are left standing***
(`WageEngine.cs:310-313`) and does not abandon them, and `ServiceEngine.Gather` (`ServiceEngine.cs:1156`)
skips only an abandoned Building. So a school whose teachers have all been dismissed **teaches the
same children on the same Day**.

🔴 ***The demonstration this row is named after therefore produced nothing to watch.*** Cutting the
funding drains a till, folds a trade, dismisses its workers — and the service is unchanged. That is
F4's *funding does nothing* arriving through a different door: F4 is the trade never instantiated,
and this is service capacity unlinked from staffing when the trade existed and died.

**The fix is not a new decision.** [`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md)
already states it: *"understaffing degrades service quality **proportionally** — a school 15%
understaffed is 15% worse, visible and purchasable."* That is decided design and it is **unbuilt**
under `adr/0070`, so the answer is *build it*. ⚠ **It is NOT what F2 refuses.** F2 refuses the ADR's
*other* half — teachers determined by catchment — and this row still must not build that. Comparing
**workers against the jobs the Building's own floor already declares** needs no catchment model.

⚠ **A service kind that declares no trade must keep its full places**, or every shipped school world
changes meaning. The scaling applies only where the kind declares `business`.

### F11 — row 33 added a second payment path, with the opposite rationing

`SubsidyEngine.Pay` (`src/Borough.Core/Rules/SubsidyEngine.cs:100`) apportions a short pot by largest
remainder through `Apportionment.Apportion`, so ***a short pot cuts every claimant proportionally and
drops nobody.*** `PolicyEngine.Move` (`PolicyEngine.cs:401`) is all-or-nothing and `SweepMembers`
returns on the first dry-treasury failure. **Two payment paths now exist and F7 describes only one.**

A subsidy also pays `rate × workers` and skips a claimant with no workers (`SubsidyEngine.Gather`
L184-215), which makes it **a wage subsidy and not the funding of an institution**. This row funds a
school rather than its payslips, so it uses the **transfer** tool and F7 stands for it unchanged.

### F12 — neither shipped world can demonstrate this row

- **`schooled.toml`'s `school` declares neither `houses` nor `premises`**, so `TryDeclaredOccupancy`
  gives it **occupancy 0** and it can never hold a Business. It is the world `--school` expects.
- **`schooling.toml`'s `college` is PRIVATE** — `BusinessKindDefinition.Charges` is
  `TuitionPerDay > 0` (`Ruleset.cs:1078`), so *private* is **derived from the trade charging a
  Household** and is not a flag. A college is funded by its students, which is the opposite of this
  row. ⚠ And whether a raised college gets its trade **is a lottery on which Lot it lands**: at
  `floor_tiles_per_occupant = 25`, the four floor areas that file has measured (64, 54, 48, 144 Tiles)
  give ceilings of **2, 2, 1 and 5**, and the 48-Tile site is raised with **no teachers, silently**
  (`World.cs:4300-4337`, nothing logged).

***So a public school is a service kind whose trade has a wage and no tuition***, and no new concept
is required to express one. This row authors its own world, which contains F5 for free.

### F13 — a school `level` with no stage claiming it empties the world, and the loader permits it

**Found 2026-09-12 while wiring `rulesets/funded.toml`.** The file declared `[[building]] school
level = 1` and produced **zero education occasions over 100 Days**. It lints, it loads, it raises
four staffed schools, and no child ever attends one.

The mechanism is a pair with one half missing. **One** service kind declaring a non-zero `level`
makes `Ruleset.DeclaresSchoolLevels` true (`Ruleset.cs:4319`). `ServiceEngine.Collect` then reads
each Household's owed level from its Life Stage and **skips the Household where that level is zero**
(`ServiceEngine.cs:486`), which is correct — a stage whose children are too young for school has no
occasion rather than a failed one. But `school_level` is declared on `[[life_stage]]`, and a file
stating levels on its **kinds** and not on its **stages** owes every Household level zero. So the
guard skips all of them, for ever.

⚠ **Nothing refuses it and nothing reports it.** The row's fix was to delete the key, and the file's
header now records the measurement. ***The defect is the missing refusal***: `level` on a service
kind is meaningless unless some `[[life_stage]]` claims a level, and the loader has both halves in
front of it. Routed under `adr/0073` to `plans/0003`'s queue rather than fixed here — this row does
not own the loader's refusal surface.

## Decisions

Decisions 1-4 were taken at scoping. **5-9 were taken on 2026-09-12 against the code as it stands
after row 33, and they close every branch the task list would otherwise have left to the
implementer.**

1. **The first expenditure is service funding, and a placement cost follows it.** Wages come first
   because payer and payee both exist. The placement cost is task 5 and carries F3's exit door.
2. **The city opens with money.** The treasury opens empty by `adr/0116`, which leaves the player
   waiting on levies before they can fund anything, so a world-creation opening balance is added. It
   is per world and absent by default (F8), because `levied.toml` needs the empty treasury
   `adr/0116` chose. ⚠ **Amended by decision 8**: `adr/0116` deferred this number because it is a
   ratio and *neither* side existed. Task 5's placement cost is the first price that makes one side
   exist, so the opening balance and the placement cost are **one choice**, taken together.
3. **The player sets a funding level per service.** It is a named Policy's amount, which `Govern`
   and the Policies panel already edit. The panel's own warning stands, because the field is the
   transfer amount and not a rate.
4. **Underfunding fires people, once the funding can arrive in time.** A till that misses too many
   paydays winds the Business up, which is shipped behaviour and gives `04 §5`'s *cutting funding
   fires people* with no new mechanism. The tolerance and the funding interval are one choice (F6).
5. **A public school is a service kind whose trade has a wage and no tuition** (F12). *Private* is
   already derived from `TuitionPerDay > 0` and needs no flag, so *public* is its absence. ***The
   distinction the corpus already draws for universities extends to schools with no new concept.***
6. **The funding tool is `transfer`, not `subsidy`** (F11). A subsidy pays `rate × workers`, which
   funds payslips; this row funds an institution. The transfer pays a flat grant per school per
   interval into its till, out of which `WageEngine` then pays the teachers. **F7's exhaustion
   branch therefore stays live and is the thing task 4 states rather than meets.**
7. **Understaffing degrades service proportionally, against declared jobs** (F10). `DeclaredPlaces`
   becomes `FloorDiv(places × workers, jobs)` for a service kind **that declares a trade**, and is
   unchanged for one that does not. Integer throughout; no catchment model; F2 untouched.
8. **The placement cost is an eighth treasury flow.** Money leaving at a placement reduces the
   balance and reaches nobody, because no import path exists (F3, and `adr/0035` §2 — imported
   Materials leave through the gate). ⚠ **`TreasuryFlowsExplainTheBalanceTests` asserts the balance
   is income less expenditure over seven flows and would break**, so the cost is counted as
   expenditure, gets its own `MoneyFlowCounter`, and appears as a **`placement · out`** row in the
   budget panel and in `--income`.
9. **The demonstration world is a new file, `rulesets/funded.toml`** (F12). Neither shipped world
   can hold a publicly funded school. ⚠ **Authoring the ground is what removes the Lot lottery**:
   at `floor_tiles_per_occupant = 16` every measured floor area on this lattice gives an occupancy
   above one, so the trade is instantiated wherever the school lands, and jobs settle at
   `Holds(16, 3) = 5` independent of the floor. **It is not hash-bearing** — the three hash-bearing
   files are `declining.toml`, `declining-tuned.toml` and `congested.toml`.

10. **The grant is per DECLARED JOB, not per school** — amending decisions 6 and number 3, forced by
   a measurement rather than by taste. ⚠ **Decision 9 claimed jobs settle at `Holds(16, 3) = 5`
   independent of the floor, and `rulesets/funded.toml` measured 5, 6, 5 and 6** on the four sites a
   city actually lays: jobs are `Holds(FloorDiv(floor, tenancies), 3)` and a tenancy share of 18
   Tiles gives six. **A flat per-school grant therefore underfunds every six-job school**, so a
   *fully funded* city still folds — ***which is F6's failure exactly, the demonstration failing
   while it looks like it succeeding.***

   The repair is a **`jobs` Readout readable against a Business**, so the Policy reads
   `apply = { derived = "jobs", percent = 100 }` and `transfer.amount` becomes **a per-job rate the
   player sets**. The grant is then a school's full payroll at any size, flat at steady state for
   every school rather than only the five-job ones.

   ⚠ **This is NOT the subsidy in another costume** (F11). A subsidy pays `rate × workers` and skips
   a claimant with no workers; this pays `rate × DECLARED jobs`, so ***a school the city has funded
   for six teachers draws six teachers' funding whether or not it has hired them***. That is
   institutional funding, and it is what makes the understaffed school of decision 7 show a **rising
   till beside falling places** — money piling up in a school that is getting worse, which names the
   cause as hiring rather than money.

   The extension point is precedented: row 33 added `Readout.Emission` against a Business the same
   way (`Readouts.IsReadableAgainst`, `Readouts.ReadBusiness`), and `World.DeclaredJobs` already
   exists.

## The five numbers

**Chosen 2026-09-12 by taste under [`0045`](0045-amnesty.md) standing order 4, at the user's explicit
instruction to choose rather than defer.** No ratifier and no `plans/0002` §D row. ⚠ **Each is
recorded with what would move it**, and ***the first two are one choice and the last two are
another.***

| # | Number | Value | Why this one, and what would move it |
|---|---|---|---|
| 1 | **Treasury opening balance** — `[treasury] opening_balance` | **4,194,304** | Places four schools and funds them for roughly 38 Days before tax revenue must carry them. The scale is set by row 33's own finding that a whole demonstration city's take is ~4.1M, so this is about one city-lifetime of revenue. **Moves if** the player cannot reach a first funded school, or reaches self-sufficiency without a spatial decision (`01 §3`'s two refuting observations). |
| 2 | **Placement cost** — `[[building]] placement_cost` on the school kind | **262,144** | One-sixteenth of the opening balance, so four schools spend a quarter of it and the fifth is a real decision. Chosen *with* number 1 and readable only against it (`adr/0116`). **Moves if** the player never feels the choice, or cannot afford the first school. |
| 3 | **Funding rate** — the Policy's `transfer.amount`, per declared job per Day | **4,096** | ⚠ **REVISED 2026-09-12 from a flat 20,480 per school, by decision 10.** It is exactly `wage_per_day`, so the grant is a school's full payroll at **any** size. The flat version was arithmetic on an assumption; `funded.toml` measured jobs of 5, 6, 5 and 6, and a flat 20,480 underfunds a six-job school by 4,096 a Day and folds it on a schedule. ⚠ **Set at payroll rather than above it so the till is FLAT at steady state** — a grant above payroll makes a till a magnitude trending upward, which `adr/0006` refuses. **Moves if** `wage_per_day` moves. |
| 4 | **Funding interval** — the Policy's `interval` | **2048 Ticks — one Day** | The grant must land before whatever Day a staggered payday falls on, and `IsPayday` scatters paydays across `pay_period_days`, so the only interval that is safe for every school is daily. **Moves if** paydays stop being staggered. |
| 5 | **Bankruptcy tolerance** — `[[business]] goes_bankrupt_after_short_paydays` on the teaching trade | **3 short paydays** | It counts **consecutive paydays, not Days**, so at `pay_period_days = 7` it is up to 21 Days of non-payment. ⚠ **It must exceed 1**: payroll sweeps at `Simulation.cs:1883` and the Policies at `1904`, same Tick, wages first, so a school can meet a payday before its first grant. **3 against a 1-Day funding interval is F6's relation with a wide margin, so a funded school never folds on a schedule.** **Moves if** the funding interval or `pay_period_days` moves. |

**A sixth number is authored and is not one of these**: `[capacity] floor_tiles_per_occupant = 16` in
`funded.toml`, which is ground rather than tuning and exists to make decision 9's ceiling reliable.

## Tasks

⚠ **Task 1 is VOID — row 33 built it.** `[[policy]] trade` ships with its loader key
(`RulesetLoader.cs:4896`), both refusals, and the filter in `PolicyEngine.Eligible`
(`PolicyEngine.cs:344`). Numbering is kept so the findings above still name the right task.

1. ~~**A Policy names the trade it sweeps.**~~ ✅ **Shipped by row 33.**
2. **A raised school holds a live employer**, on the ground the demonstration world lays. Assert a
   raised school kind holds a live Business with jobs above zero. ⚠ **A file that loaded is not
   enough** (F4): `World.Fit` withholds a declared trade wherever the occupancy ceiling is not above
   one, **silently, with nothing logged** (`World.cs:4300-4337`). Record what the ground divided into
   and whether a foreign trade took the tenancy first (F5).
3. **The treasury opens with money** — `[treasury] opening_balance`, authored per world, entering
   through a `World` door the way `[households] opening_balance_min`/`max` enters through a
   Household, and bumping `MoneySupply.Issued` so conservation holds. Saved and hashed. **Absent by
   default and refused on reload** (F8) — the precedent to copy is `MapLayers.Adopt`
   (`Space/MapLayers.cs:378`), a comparison that throws, reached from `World.Adopt` before anything
   moves.
4. **The funding Policy, and the panel that steers it** — a `transfer` Policy naming the teaching
   trade pays each school's till every Day; the player raises and lowers it from the existing
   Policies panel; the budget says what it cost. ⚠ **The figure is what was PAID and never what was
   owed** (F7), because a dry treasury pays some schools in full, pays the rest nothing, and stops
   where it ran out.
5. **Placing a service costs the treasury** — `[[building]] placement_cost`, refused in words when
   the treasury cannot pay it. ⚠ **`Simulation.ApplyService` is outside `World`**, so the withdrawal
   and the `MoneySupply.Issued` write-down go **behind one `World` door** that `Simulation` calls
   (F3), matching all four shipped decrement sites. Add the refusal to `RefuseService`.
6. **Understaffing degrades service** (F10, decision 7) — `World.DeclaredPlaces` scales by
   `workers / jobs` for a service kind that declares a trade. ⚠ **This moves `schooling.toml`'s
   behaviour and the golden hashes**, because its `college` is the one shipped staffed service kind;
   re-record with the command in `tests/Borough.Tests/Golden/README.md`.
7. **A `jobs` Readout against a Business** (decision 10) — `Readout.Jobs`, admitted in
   `Readouts.IsReadableAgainst` for `ReadoutScope.Business`, returning `World.DeclaredJobs` from
   `Readouts.ReadBusiness`, with its name in `ReadoutNames` and its sentence in `RulesetKeyNotes`.
   ⚠ **Follow `Readout.Emission` exactly** — row 33 added it against a Business on this pattern.
   Then `funded.toml`'s funding Policy reads `apply = { derived = "jobs", percent = 100 }` with
   `transfer.amount = 4096`.
8. **The eighth flow** (decision 8) — a `MoneyFlowCounter` for placement, a `placement · out` row in
   the budget panel and in `--income`, and `TreasuryFlows.Expenditure` counting it, so
   `TreasuryFlowsExplainTheBalanceTests` stays green on a world that places a school.
9. **Something to watch** — a driven run in which a school is placed and paid for, staffed, funded,
   then defunded until its places fall and it folds. ***The last two clauses are the ones that get
   dropped, and they are the only ones that show the decision mattering.***
10. **The acceptance run** — a long run with the whole circuit live, money conserved end to end, and
   the treasury neither trending to zero nor accumulating without bound.

## What this must not do

- **It must not build catchment staffing** (F2). That belongs to `adr/0026` and it changes who is
  hired. ⚠ **Decision 7 is the ADR's OTHER half and is not this**: staffing degrading service reads
  the jobs a floor already declares, and ***nothing in this row lets a catchment decide how many
  teachers a school wants.***
- **It must not build a tenancy a kind reserves for its own trade** (F5). `premises` is a permission,
  and `adr/0147` counts one ceiling over both kinds of tenant, so a shop may move into a school and
  that is the ceiling working as designed. Watch for it in task 6 and record it, and contain it with
  ground rather than with a rule. Whether a kind may reserve its own tenancy is `adr/0147`'s question,
  and this row asks it without answering it.
- **It must not price construction of anything the city raises by itself.** Private construction has
  no payer, because `PlacementEngine` withdraws from nobody and there is no developer or landlord
  actor. A cost there would need an actor invented, which is a decision and not a task.
- **It must not price compulsory purchase** to unblock the placement cost. `adr/0091` refused that
  number deliberately, and it is still blocked on the land value target.
- **It must not route a wage through the treasury.** `WageEngine`'s own remark says that makes it a
  tax-and-dividend, and a wage has to preserve which employer paid it. The treasury funds the till and
  the till pays the wage.

## Definition of done

`CLAUDE.md`'s cumulative list, plus:

- **A player watches a school be paid for, open, staff itself, be funded, and then lose its funding
  until its places fall and it closes**, with the causal chain reading off the interface rather than
  off a log. ⚠ **The falling places are the clause F10 was about** — without them the last half of
  this sentence is invisible.
- **Money is conserved across every new path**, the placement leak included, with
  `Invariant.MoneyIsConserved` green. ⚠ It is an **end-of-run** check that names no cause
  (`WorldInvariants.cs:1211`), so pair the withdrawal and the write-down at the write site rather
  than relying on it.
- **The treasury balance still equals income less expenditure**, now over eight flows rather than
  seven — `TreasuryFlowsExplainTheBalanceTests` green on a world that places a school.
- ~~Four provisional numbers are recorded here~~ ✅ **Five are, in *The five numbers*, each with what
  would move it.** ⚠ **The scoping draft promised this record and did not write it**; that omission
  is what made the row unstartable.
