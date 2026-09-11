# 0070 — The city spends

**[`0045`](0045-amnesty.md) queue row 32.** Scoped 2026-09-09, against `main` at `36ff4fc`.

## Status

🟡 **SCOPED, NOT STARTED.** The readout half landed first and out of order: the console carries
`Population` and `Treasury` as of this session, because a budget nobody can see is not a budget and
the figure was needed to scope against. `World.TreasuryBalance`, `Main.Console.RefreshConsole`.

⚠ **Amended 2026-09-09, from the symbols rather than from the scope.** The task list was read back
against the code it names and **five of its seven tasks moved**. F3 was wrong about the build, F4–F8
were absent, and two of those would have been met as failures of the wrong task: **a school standing
empty reads as *funding does nothing*** (F4), and **a bankruptcy on a schedule reads as *underfunding
fires people*** (F6). ***Both are this row succeeding at showing the opposite of what it claims***,
which is why they are findings before task 1 rather than surprises during task 6.

## Why this exists

**Money moves and the city does not spend.** Wages travel Business → Household, shopping travels
Household → Business, a levy travels either → treasury, and a rebate travels back; `MoneyLongRunTests`
asserts the treasury is *a conduit rather than an accumulator* and it is right. Nothing the city does
costs it anything: `PlacementEngine` raises Buildings with no withdrawal, `Simulation.ApplyService`
places a school for nothing, and `ServiceEngine` and `CivicEngine` run it for nothing — every `Cost`
in both is a `TravelTime`.

So the player has a treasury and no fiscal decision, and [`04 §5`](../docs/04-economy-and-goods.md)'s
three levers — tax rates, service funding, borrowing — have nothing under them. ***A budget is an
expenditure the player can steer, and this row builds the first one.***

## What the build already holds

Read from the symbols.

| | What | Where |
|---|---|---|
| ✅ | A treasury holding money, one Bin per conserved Resource | `World.CreateTreasuryBin`, `TreasuryTable` |
| ✅ | Money in and out of it, both directions, as `[[policy]]` transfers | `PolicyEngine.Move` |
| ✅ | A player lever over a named Policy's amount, with a panel | `Simulation.ApplyGovern`, `Main.Panels` |
| ✅ | Wages from an employer's till to a worker's Household | `WageEngine.Pay` |
| ✅ | A Business raised **with its premises**, no founder and no capital | `adr/0148`, `World.CreateBuilding` |
| ✅ | A trade that goes under when it cannot make payroll | `[[business]] goes_bankrupt_after_short_paydays` |
| ✅ | Service Buildings, attendance and catchment | `[[building]] serves`, `ServiceEngine`, `CivicEngine` |
| 🔴 | **No service Building tenants a Business in any shipped world**, so nobody works at one | — |
| 🔴 | **No door for money into the treasury at world creation** — `World.Endow` deposits to a Household | `World.Endow` |
| 🔴 | **No cost on any player act** | `Simulation.ApplyService` |

### F1 — a Policy sweeps a whole table, and that is what sizes task 1

`[[policy]] sweeps` accepts `household` or `business` and nothing else — no kind, no trade, no
District (`RulesetLoader.ReadSubject`). So a transfer from the treasury to `local` reaches **every
Business in the city**, and *fund the schools* is unauthorable: it would pay every shop the same
subsidy. ***The funding lever needs a Policy that can name whose trade it sweeps***, and that is a
loader key and a filter in `PolicyEngine.SweepMembers` rather than a new mechanism.

### F2 — staffing is floor area, and demand-determined staffing stays unbuilt

A Business's jobs come from `[capacity] floor_tiles_per_job`, so a school would employ what its
floor area divides into. [`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md)
wants public jobs **demand-determined by catchment** — teachers per child — and the build's own
comments already class that `adr/0070` *unbuilt*. ⚠ **This row does not build it.** A funded school
with floor-area staffing is still a school the treasury pays for; catchment staffing changes who is
hired, not who pays.

### F3 — the placement cost needs a payee, and the design already names it

Money is conserved and the loader refuses a money term with no counterparty. `adr/0035` §2 settles
where construction money goes: it buys Materials, and **imported, it leaves through the gate**. No
import path exists — a Hinterland price is a ceiling that seeds a Pool's price and no Bin receives a
payment — so charging for a placement writes `MoneySupply.Issued` down.

⚠ **This finding first said *a third writer*, alongside an emigrating Household and a razed Business,
and both halves of that were wrong.** There are **four** decrements shipped —
`WageEngine.Bankrupt`, `World.Depart(Household)`, `World.Depart(Business)` and `World.Raze` — and
what makes them alike is not that they are few. ***Every one of them reads a Bin that is about to be
freed***, which is why each sits immediately before its `Destroy` call and says so in a comment.

***So the exit door is not the novelty; a LIVE Bin losing money with no counterparty is.*** That is a
smaller thing than this finding claimed, and it moves where the risk sits: not in whether the supply
may fall, but in whether the withdrawal and the write-down stay paired. All four shipped sites are
inside `World`, where the Bin's owner is. **Task 5 writes from `Simulation.ApplyService`, which is
outside it** — so the pair goes behind a `World` door and `Simulation` calls that, rather than
reaching into the column. Nothing catches an unpaired write before `Invariant.MoneyIsConserved` at
end of run, with nothing pointing at the cause.

### F9 — row 29 shipped the kind task 2 was going to author

`rulesets/schooling.toml` arrived with row 29 and its **`college`** declares all four keys together:
`serves = "education"`, `level = 3`, `premises = true`, `business = "tuition"`, and a money Bin owned
by the business. ***So a service Building that comes with its own employer is shipped content and not
this row's to invent***, and task 2 is a verification against a standing world rather than an
authoring task.

⚠ **F5 is therefore live today, in a file nobody wrote for this row.** A `college` is premised, so a
grocer or an office founded by a Citizen may take its tenancy. Whether that has happened in a
`schooling.toml` run is unmeasured; **look before assuming it has not.**

⚠ **And this row's demonstration world is now a choice, not a given.** `schooled.toml` has one
service kind with no trade at all; `schooling.toml` has four, one of which is already staffed. Pick
against `schooling.toml` unless task 2's assertion says otherwise, and record which and why.

### F4 — the gate on a school's employer is in `World`, and task 2 was checking the loader

The loader permits `serves` and `business` on one kind: they are read independently and the only
coupling is that a declared trade demands `premises = true`. **So task 2's stated check passes, and
passing it proves nothing.**

`World.CreateBuilding` asks a second question the Ruleset cannot answer: it instantiates the kind's
trade only where `roomBeside` holds, which is **the declared occupancy ceiling being greater than
one**. A Business takes a tenancy exactly as a Household does (`adr/0147`), and on ground that
divides into a single tenancy the trade would take the only one. ⚠ **A school whose floor divides
into one tenancy gets no employer, no refusal and no diagnostic** — it stands, it is attended, and
nobody works there. The demonstration reads as *funding does nothing* and the cause is in neither the
Ruleset nor the Policy.

***What task 2 must confirm is a Building that came with its trade, on the ground the demonstration
world actually lays*** — not a file that loaded.

### F5 — `premises` is a permission and not a reservation

`World.HasRoomForPremises` reads the kind's `premises` flag and the room left under the ceiling. **It
never reads the trade.** It is the predicate `PlacementEngine`'s unpremised pass asks, so the moment
the school kind states `premises = true` to carry its teachers, ***every unpremised Business in the
city becomes eligible to move into a school***, and under `adr/0147`'s single ceiling the shop that
does so takes a slot the teachers needed.

⚠ **This is the `plans/0053` failure with the roles swapped.** There, a trade took the only tenancy
and the dwelling housed nobody. Here a foreign trade takes a tenancy and the school teaches with a
short staff. The mechanism is one ceiling counting both kinds of tenant, working exactly as designed.

**It is unowned by any task.** The cheapest containment is ground the demonstration controls — a
school with more tenancies than its trade needs is not a fix, only a margin. ***Whether a kind may
reserve its own tenancy is a question this row should ask and need not answer***, and it belongs to
`adr/0147` rather than here.

### F6 — payroll is swept before the funding Policy, in the same Tick

`Simulation` sweeps wages and **then** sweeps Policies. So on any Tick where both fire, a till pays
what it holds before the treasury tops it up, and ***a school placed today meets its first payday
with an empty till***.

⚠ **`goes_bankrupt_after_short_paydays` is counting from that first one.** Whether a fully funded
school survives long enough to be funded is therefore a relation between the Policy's `interval` and
the bankruptcy tolerance, and **decision 4 named neither.** A tolerance shorter than the funding
interval winds up every school in the city on a schedule, and it looks exactly like the underfunding
the row is trying to demonstrate.

***So the two numbers are one number, chosen together***, and the honest reading of decision 4 is
that underfunding fires people *once the relation is right*. Both are provisional under standing
order 4 and both are recorded at the end of this plan.

### F7 — a Policy pays in full or not at all, and a dry treasury stops the sweep

`PolicyEngine.Move` moves the whole amount or moves nothing: a payer short of the full transfer pays
zero. `SweepMembers` then returns on the first such failure where the payer is the treasury, so
***the remainder of that sweep does not happen***.

The consequence with three schools and money for two is not two-thirds each. It is **two paid in
full, one paid nothing, and which one is decided by the sweep's seeded start slot** — deterministic,
replayable, and rotating between periods because the start is drawn per Tick.

⚠ **This is `02 §4.2`'s *pays whom it reaches and reports where it stopped*, already built**, and it
is defensible: a part-paid wage bill is not a thing a payroll can spend. But it is what the player
watches, so **task 4 states it rather than meeting it**, and the console figure task 4 asks for has
to say what was *paid*, never what was *owed*.

### F8 — an opening balance must be absent by default, and refused on reload

The shape is precedented: `[households] opening_balance_min`/`max` is authored per world and read at
synthesis, and a `[treasury]` sibling reads the same way. Two constraints on it, neither in task 3.

**It is refused on reload**, the way `[layers] kernel_metres` is. A Ruleset is hot-reloadable
(`adr/0015`) and world creation is not; a reload that re-read an opening balance would mint money
into a standing city on every hot reload, and `Invariant.MoneyIsConserved` would stay green because
the issuance is recorded.

**It defaults to absent, and `levied.toml` is why.** That file's own comment records that `adr/0116`
chose an empty opening treasury *deliberately* — to make the dry-sweep branch reachable on the first
sweep rather than after a long run somebody has to construct. Decision 2 overrides `adr/0116` for the
world this row demonstrates and must not override it everywhere: ***a defaulted opening balance would
delete another file's demonstration to enable this one.***

## Decisions taken at scoping

1. **The first expenditure is service funding, and a placement cost follows it.** Wages first,
   because payer and payee both exist; the placement cost is task 5 and carries F3's exit door.
2. **The city opens with money.** The treasury opens empty by `adr/0116`, which leaves the player
   waiting on levies before they can fund anything. A world-creation opening balance is added.
   ⚠ **Provisional and chosen by taste** — `0045` standing order 4, so no ratifier and no §D row.
   [`01 §7`](../docs/01-player-experience.md) already names the two observations that would refute
   it: too small if the player cannot reach a first housed Household, too large if the city reaches
   self-sufficiency without a spatial decision. ⚠ **Per world and absent by default** — F8, because
   `levied.toml` needs the empty treasury `adr/0116` chose.
3. **The player sets a funding level per service.** It is a named Policy's amount, which `Govern`
   and the Policies panel already edit. The panel's own warning stands: the field is the transfer
   amount and not a rate.
4. **Underfunding fires people, once the funding can arrive in time.** A till that misses too many
   paydays winds the Business up, which is shipped behaviour for a shop and is `04 §5`'s *cutting
   funding fires people* arriving with no new mechanism. ⚠ **The tolerance and the funding interval
   are one choice and not two** (F6): payroll sweeps before Policies, so a school meets its first
   payday with an empty till, and a tolerance shorter than the interval winds up a fully funded city
   on a schedule. ***A bankruptcy that a solvent treasury could not have prevented is the row's
   demonstration failing, dressed as the row's demonstration succeeding.***

## Tasks

1. **A Policy names the trade it sweeps** — `[[policy]] trade`, refused where the trade is
   undeclared, filtering `PolicyEngine.SweepMembers`. Without it, funding a school pays every shop.
2. **A raised school holds a live employer** — ⚠ **the authoring is already done** (F9):
   `schooling.toml`'s `college` states `serves`, `premises`, `business` and a business-owned money
   Bin. ⚠ **And the loader is not what this task checks** (F4): it permits those keys already, and
   `World.CreateBuilding` withholds the trade where the ground divides into one tenancy. ***Assert on
   a raised `college` that holds a live Business with jobs above zero***, on the ground that world
   lays, and treat a school standing empty as this task failing rather than as task 4 failing. Record
   what the ground divided into, and whether a foreign trade took the tenancy first (F5).
3. **The treasury opens with money** — a `[treasury]` opening balance authored per world, entering
   through the treasury the way `[households] opening_balance_min`/`max` enters through a Household,
   and bumping `MoneySupply.Issued` so conservation still holds. Saved and hashed. ⚠ **Absent by
   default and refused on reload** (F8) — a default would empty `levied.toml`'s demonstration and a
   reload would mint money into a standing city.
4. **The funding Policy, and the panel that steers it** — the treasury pays the school's till on a
   cadence; the player raises and lowers it; the console says what it costs per period. ⚠ **The
   console figure is what was PAID and never what was owed** (F7): a dry treasury pays some schools
   in full, pays the rest nothing, and stops the sweep where it ran out. Choose the interval against
   the bankruptcy tolerance (F6) and record both here.
5. **Placing a service costs the treasury** — a cost per kind, refused in words when the treasury
   cannot pay it. ⚠ **`Simulation.ApplyService` calls a `World` door and does not write the column**
   (F3): the withdrawal from a live Bin and the write-down of `MoneySupply.Issued` are one operation,
   and every shipped decrement is inside `World` for that reason. `Invariant.MoneyIsConserved` must
   stay green, and it is an end-of-run check that names no cause, so pair the two at the write site.
6. **Something to watch** — a driven run in which a school is funded, its staff are paid, the funding
   is cut, the till runs dry and the staff are turned out. ⚠ **The last clause is the one that gets
   dropped**, and it is the only one that shows the decision mattering.
7. **The acceptance run** — a long run with the whole circuit live, money conserved end to end, and
   the treasury neither trending to zero nor accumulating without bound.

## What this must not do

- **It must not build catchment staffing** (F2). That is `adr/0026`'s and it changes who is hired.
- **It must not build a tenancy a kind reserves for its own trade** (F5). `premises` is a permission
  and `adr/0147` counts one ceiling over both kinds of tenant, so a shop may move into a school and
  that is the ceiling working as designed. ⚠ **Watch for it in task 6 and record it**; contain it
  with ground rather than with a rule. ***Whether a kind may reserve its own tenancy is `adr/0147`'s
  question***, and this row asks it without answering it.
- **It must not price construction of anything the city raises by itself.** Private construction has
  no payer — `PlacementEngine` withdraws from nobody, and there is no developer or landlord actor.
  A cost there would need an actor invented, which is a decision and not a task.
- **It must not price compulsory purchase** to unblock the placement cost. `adr/0091` refused that
  number deliberately, and it is still blocked on the land value target.
- **It must not route a wage through the treasury.** `WageEngine`'s own remark: that makes it a
  tax-and-dividend, and a wage has to preserve which employer paid it. The treasury funds the till;
  the till pays the wage.

## Definition of done

`CLAUDE.md`'s cumulative list, plus:

- **A player watches a school open, staff it, fund it, and cut the funding until it closes** — and the
  causal chain reads off the interface rather than off a log.
- **Money is conserved across every new path**, the placement leak included, with the invariant green.
- **Four provisional numbers are recorded here** — the opening balance, the funding amount, the
  funding interval and the bankruptcy tolerance — with what would move each, per standing order 4.
  ⚠ **The last two are chosen against each other** (F6) and neither is readable alone.
