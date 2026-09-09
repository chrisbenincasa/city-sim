# 0070 — The city spends

**[`0045`](0045-amnesty.md) queue row 32.** Scoped 2026-09-09, against `main` at `36ff4fc`.

## Status

🟡 **SCOPED, NOT STARTED.** The readout half landed first and out of order: the console carries
`Population` and `Treasury` as of this session, because a budget nobody can see is not a budget and
the figure was needed to scope against. `World.TreasuryBalance`, `Main.Console.RefreshConsole`.

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
payment — so charging for a placement means a **third writer of `MoneySupply.Issued`**, alongside an
emigrating Household and a razed Business. ⚠ **That is the money supply's exit door, and it is the
one part of this row that is not assembled from shipped parts.**

## Decisions taken at scoping

1. **The first expenditure is service funding, and a placement cost follows it.** Wages first,
   because payer and payee both exist; the placement cost is task 5 and carries F3's exit door.
2. **The city opens with money.** The treasury opens empty by `adr/0116`, which leaves the player
   waiting on levies before they can fund anything. A world-creation opening balance is added.
   ⚠ **Provisional and chosen by taste** — `0045` standing order 4, so no ratifier and no §D row.
   [`01 §7`](../docs/01-player-experience.md) already names the two observations that would refute
   it: too small if the player cannot reach a first housed Household, too large if the city reaches
   self-sufficiency without a spatial decision.
3. **The player sets a funding level per service.** It is a named Policy's amount, which `Govern`
   and the Policies panel already edit. The panel's own warning stands: the field is the transfer
   amount and not a rate.
4. **Underfunding fires people.** A till that misses too many paydays winds the Business up, which
   is shipped behaviour for a shop and is `04 §5`'s *cutting funding fires people* arriving with no
   new mechanism.

## Tasks

1. **A Policy names the trade it sweeps** — `[[policy]] trade`, refused where the trade is
   undeclared, filtering `PolicyEngine.SweepMembers`. Without it, funding a school pays every shop.
2. **A service kind carries a trade** — the demonstration world declares `serves` and `business` on
   one kind, so raising a school raises its employer. No engine change is expected; confirm the
   loader permits both keys on one kind and record it here if it does not.
3. **The treasury opens with money** — a world-creation opening balance, entering the world through
   the treasury the way `World.Endow` enters it through a Household, and bumping `MoneySupply.Issued`
   so conservation still holds. Saved and hashed.
4. **The funding Policy, and the panel that steers it** — the treasury pays the school's till on a
   cadence; the player raises and lowers it; the console says what it costs per period.
5. **Placing a service costs the treasury** — a cost per kind, withdrawn in `Simulation.ApplyService`,
   refused in words when the treasury cannot pay it. Carries F3: the money leaves the world through
   the gate, and `Invariant.MoneyIsConserved` must stay green across it.
6. **Something to watch** — a driven run in which a school is funded, its staff are paid, the funding
   is cut, the till runs dry and the staff are turned out. ⚠ **The last clause is the one that gets
   dropped**, and it is the only one that shows the decision mattering.
7. **The acceptance run** — a long run with the whole circuit live, money conserved end to end, and
   the treasury neither trending to zero nor accumulating without bound.

## What this must not do

- **It must not build catchment staffing** (F2). That is `adr/0026`'s and it changes who is hired.
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
- **Two provisional numbers are recorded here** — the opening balance and the funding amount — with
  what would move each, per standing order 4.
