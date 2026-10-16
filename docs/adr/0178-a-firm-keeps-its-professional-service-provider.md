# A firm keeps its Professional Service provider

Status: accepted, 10/15/2026.

## Decision

- A Professional Service is a Resource with `family = "service"`. It is never stocked, so no Bin
  holds it. A `[[building]] bins` entry, a `fills` or any term other than a `pool` input naming one
  is refused at load.
- A firm buys a service through a `pool` input. The Resource's family picks the mechanism: a Good
  draws a seller from the District market each purchase (`adr/0167`), and a service pays the
  provider the firm keeps.
- `ServiceContractTable` saves one row per firm and service: the buyer, the service, the provider
  and the Tick it was chosen. A firm keeps its provider while that provider still supplies the
  service. Only when the provider cannot does the firm choose again.
- In this build every provider is a Hinterland. A Hinterland supplies a service when it prices it
  and a gate stands on its edge. A new choice takes the cheapest such Hinterland, with ties to the
  one declared first.
- A firm with no supplying provider stalls on its District's market row for the service, like a
  starved Rule. Placing a gate wakes it through the same path that wakes buyers of Goods.
- Each service states its reach once: `reach_minutes` of free-flow travel, or `remote = true` for
  any distance. A Hinterland is reached from anywhere, so reach binds only once the city has its
  own providers.
- Payment uses the existing settlement path. The buyer's balance falls, and the Money leaves the
  city through `PayOutside`.
- A `pool` term on a utility or labour Resource is refused at load, because no market delivers it
  (issue #177).

## Why

- Reach is a relation between a buyer and a provider (`plans/export-base.md` Decision 3). A District
  Pool is shared by every buyer in a District, so it cannot express "within 30 minutes of this
  firm". A contract checks reach once, when the firm chooses, rather than on every purchase.
- `CONTEXT.md` says a provider near its clients "meets them cheaply and keeps them". A purchase that
  draws a new seller each time keeps no client.
- A contract names who failed, so a stall can be explained as "this firm's provider closed" rather
  than "the District ran short".
- This does not reopen `adr/0167`. That record rejects a buyer that waits on one fixed seller. A
  firm here never waits on its provider. When the provider fails, the firm chooses again or waits on
  the market.

## Consequences

- The new saved table moves the State Hash. The goldens are re-recorded, and save format version
  22 carries the table. With the table left out of the hash, every golden passes unchanged.
- Rulesets without a service behave as before. Their District Pools, contracts and refusals are
  unchanged.
- A firm pays more than the cheapest price after a cheaper provider appears. That is the intended
  cost of keeping a provider.
- A closed firm's contracts are freed with it.
- Service purchases land on no stock Bin, so they are not yet a recorded production cost. Production
  cost is still undesigned (`plans/0072` D15).
- The offices slice adds local providers. It needs a provider handle on the contract, a capacity per
  provider, and a reach test that reads a refreshed travel-time field rather than routing
  (`02 §5.8`).
