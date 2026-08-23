# An unpremised business emigrates so the sink is the one households already use

**A Business that loses its premises is **unpremised** exactly as a Household is **unhoused**: it joins
a pool, waits under a give-up bound, and if nothing tenants it, it **leaves the city and takes its money
with it**. `World.Depart`'s mechanism is reused unchanged — the balance is subtracted from
`MoneySupply.Issued` and the row is freed. **The money is neither destroyed nor confiscated; it is
exported.** What this ADR does **not** settle is what **capitalises** a Business, which is a number and
is owed a ratifier.**
`SOLVE THE ACTUAL PROBLEM` `HONEST DEGRADATION` `LEGIBLE CAUSE`

**This closes a hole [`adr/0006`](0006-no-collection-grows-with-elapsed-time.md) forbids and nothing
had noticed**, and it is the companion to
[`adr/0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md): once a
Business owns Bins and runs Rules, it needs a lifecycle, and the lifecycle it needs already exists.

## Why

🔴 **`World.DestroyBuilding` evicts a Household and orphans a Business.** It walks `Occupants` and
`Unplace`s every Household — **into the Unplaced Pool**, which is a sink with a give-up bound on it. It
then reaches the Businesses (`World.cs:3660`) and does this:

```
IndexList premises = BuildingBusinesses;
while (premises.PopFront(slot) != Rows.NoSlot) { }
```

***It unlists them and frees nothing.*** The row survives with its balance intact and its premises
handle severed. **The comment is honest on its own terms** — there is no pool for a Business, and
freeing the row would destroy its money and fire `Invariant.MoneyIsConserved` — and it ends *"⚠ what
becomes of a Business with no premises is undesigned."*

**So the orphan set grows monotonically with demolitions, for ever**, and demolition is not rare: it is
what `condemn_after` does to every dwelling in `minimal.toml`. ⚠ **It does not fire today only because
nothing in the simulation creates a Business**, which is `adr/0070` holding the door shut from the
other side. ***The moment a Business has a reason to exist, this is a live unbounded collection.***

🔴 **And the invariant whose name suggests it covers this is structurally blind to it.**
`MoneyLedger.Of` (`MoneyLedger.cs:91`) walks **every live Bin slot**, skipping only dead rows and
unconserved Resources, *"whoever owns it — and this does not ask."* An orphaned Business's balance Bin
is still **live**, so it still counts toward `Total`, so **conservation still balances**. The orphan does
not even land in `Elsewhere`, because `OwnerKind` still reads `Business`. ⚠ **The reverse case IS
caught** — `Reference.Required` on the balance handle makes `Invariant.CrossTableHandleResolves` fire on
a *freed Bin under a living owner*. ***The build guards the direction that cannot happen and is silent
on the one that can.***

## Why emigration, and why nothing new is invented

**`World.Depart` (`World.cs:1344`) already does exactly this, for Households, in one call:**

```
MoneySupply.Issued[MoneySupplyTable.Slot] -= new Money(Bins.LevelAt(balance));   // :1370
...
DestroyHousehold(household);                                                     // :1381
```

`MoneySupplyTable.Issued` is declared *"money that has entered this world, **net of anything that has
left it**"*, and `World.Endow` (`:934`) is the mirror — an arriving Household carries a balance in from
its Hinterland's `emigrant_balance_min/max` band
([`adr/0131`](0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)).
**There are two doors and they are the same door.** ⚠ **The split is load-bearing and the code says
why**: the supply write is in `Depart` and deliberately *not* in `DestroyHousehold`, ***because only
Departure means emigration.***

⚠ **It lands on the channel that EXISTS.** `Depart` refuses a **housed** Household —
`Invariant.OnlyAnUnhousedHouseholdGivesUp` — and the housed-departure channel *(a family with a home
choosing to leave)* is **unbuilt**. ***A Business orphaned by `DestroyBuilding` is unpremised by
construction***, so this needs the built half only.

⚠ **The symmetry with [`adr/0069`](0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)
is preserved rather than strained.** *Construction houses nobody* is the reason a stand-in that
auto-tenants a shop was refused — ***and a pool plus a placement pass is what not auto-tenanting looks
like.*** The Business gets the same shape the Household got, for the same reason.

⚠ **A pooled tenant keeps what it owns, and that needs no code.** `World.Unplace` touches the dwelling
handle and the occupant list and **not** the balance handle, which is what makes *"a Household keeps
what it owns when the city stops housing it"* true by omission. `UnplacedTable` has four columns and
**no money column**. The same holds for a Business.

## Consequences

**A give-up bound is a duration and the Business's is a second one.** `[placement]
gives_up_after_days` is required of any Ruleset declaring a gate kind
([`adr/0130`](0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)).
⚠ **Whether a Business shares that number or states its own is undecided here** — but ***a shop looking
for premises and a family looking for a home are not obviously the same patience***, and if it is a
second number it is a second `plans/0002` §D row.

🔴 **What capitalises a Business is UNANSWERED and it is the only thing this session could not
settle.** `BusinessTable`'s own doc comment: *"**Nothing funds one.** A Business opens with an empty Bin
and there is no door that pays it."* A Household's opening balance is an **authored band on the
Hinterland**; a Business has neither a band nor a gate to arrive through. ⚠ **On the day such a band is
written it is hash-bearing and needs a named ratifier — a machine, a world and a quantity**
([`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)), and a
category is not a name. **Until it exists, a Business that arrives with nothing cannot buy anything**,
which makes the source block the purchase exactly as the payer did.

**An unpremised pool is a collection and therefore needs its bound stated on the day it is built**, not
after — which is this ADR's own subject applied to itself.

## Rejected

**Liquidate to the treasury.** Rejected: with no owners modelled, *liquidation* is ***confiscation***,
and the player did not do it. It also invents a revenue line that would silently fund the city off
demolitions.

**Destroy the row and the money with it.** Rejected: it is a money leak, `Invariant.MoneyIsConserved`
exists to catch exactly that, and ⚠ **the existing code declined this option explicitly** — the reason
`DestroyBuilding` unlists rather than frees.

**Transfer the balance to the Building's next tenant, the way a dying District's stock goes to whoever
took its centre Cell.** Rejected. ⚠ **The District case is SUCCESSION — identity passing to an heir —
and a demolished shop has no heir**; the next tenant is a stranger, and handing them the money is a
windfall with no cause a player could read.

**Leave it undesigned until something creates a Business.** Rejected under `adr/0006`: ***the bound
goes in on the day the collection does***, and the collection is `adr/0141`'s.

## What would trigger revisiting

- **A housed-departure channel shipping**, which would give a *premised* Business a reason to leave and
  make this the special case rather than the rule.
- **Business ownership being modelled** — a Citizen or Household owning a Business would give
  liquidation a recipient, and *Rejected*'s first entry stops being confiscation.
- **A relocation that succeeds often enough that the give-up bound never fires**, which would mean the
  sink is theoretical and the pool is the real mechanism.
