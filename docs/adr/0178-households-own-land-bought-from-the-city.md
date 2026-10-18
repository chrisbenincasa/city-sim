# Households own land, bought from the city

Status: accepted, 10/16/2026.

## Decision

- Every Lot has one owner. A Household that owns a Lot is its Landowner, and it owns the Building
  the Lot holds. A Lot no Household owns is held by the city.
- The city holds every newly carved Lot and every Lot world creation builds. It sells a Lot to a
  Household at the `World.DemolitionPrice` formula, so it sells and buys back at one price.
- A Zone Rule still decides when to build. A Household able to pay the land price and the
  construction charge while keeping a reserve buys the Lot, drawn with equal chance. With no such
  Household, the Lot stays vacant for want of capital. The city never builds.
- Tenants pay Rent to the Landowner each Day. A resident Landowner pays none. While the city
  holds a built Lot, its Rent goes to the treasury.
- Streets, `Demolish` and a player purchase of a vacant Lot are compulsory purchase. They pay the
  Landowner, not the tenants.
- The full decision table, L1–L14, is in `plans/money-circulation.md`.

## Why

- Rent had no recipient. A Building never holds money (`adr/0113`), and `adr/0169` rejected a
  landlord counterparty because no table named one.
- A Household as Landowner makes rent a second income channel. A city landowner makes it a tax,
  and the treasury is already where taxes pile up.
- Land banking (`plans/0074`), construction pricing and compulsory purchase each needed an actor
  who holds vacant land. A Household owning the Lot is that actor.
- Selling new land, rather than handing it out, makes construction depend on savings. That is
  the private capital `docs/02` says should pace growth.

## Rejected

- **The city owns all land.** Cheapest, but rent becomes a tax and nobody can decline to build.
- **A landlord Business.** It would be a Business without premises, which `adr/0142` sends out of
  the city.
- **Owning the Building only.** Vacant land would have no owner, so neither land banking nor a
  land price could exist.
- **Free land to a random Household.** A windfall with no price, which makes land value
  something nobody pays.

## Consequences

- `[[building]] rent` becomes a rate per floor Tile. Placement and `HousingUtility` must read the
  same rent the tenant pays, so every Ruleset that sets the key changes.
- Growth slows in a city with little saved Money. A city with none could stall. The Day-400 run
  in L13 checks this together with whether Landowner balances stay bounded.
- Vacant-Lot assembly can be blocked by another Landowner's Lot. The player's purchase breaks the
  holdout at the land price.
- A generated city starts with the treasury collecting most Rent until its Lots sell.
