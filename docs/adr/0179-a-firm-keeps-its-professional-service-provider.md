# A firm keeps its Professional Service provider

Status: accepted, 10/15/2026.

> **Amended 10/17/2026 by export-base slice 3a** ([`plans/export-base.md`](../../plans/export-base.md)).
> **The decision is untouched** — a firm keeps its provider while that provider supplies it, and a
> contract names the provider rather than a District Pool. What changed is **how reach is tested**:
> this ADR asked for a refreshed travel-time field and the slice that built local providers routes
> instead, one bounded walk per candidate. The reach bullet and the last consequence are amended in
> place below, each marked.

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
  firm". A contract checks reach when the firm chooses, and a firm chooses on the purchase that
  finds its provider unable to supply the batch — so reach is tested per purchase that re-matches,
  not per purchase.
  > **Amended 10/17/2026: *once, when the firm chooses* was read as a cached verdict and it is not
  > one.** Capacity binds per purchase (`plans/export-base.md` Decision 11), so a client of a
  > spent office re-matches that same Tick and the test runs again — for the kept provider and for
  > every candidate. What bounds the cost is the candidate set, not a cached answer.
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
  > **Amended 10/17/2026: a purchase from a city office is expensed on the Day.** The office books
  > the revenue, so a client booking nothing would leave the pair manufacturing profit and
  > `BusinessTaxEngine` taxing it. A Good defers its cost on the stock Bin and recognises it as it
  > sells; a service is consumed in the firing that bought it and no Bin can carry the cost forward,
  > so there is nothing to defer. An *imported* service is still expensed nowhere, which makes a
  > firm's taxable profit depend on which provider it got: the same service at the same price is an
  > expense from a city office and not from a Hinterland, so a firm that switches to an office sees
  > its profit and its tax bill fall.
- The offices slice adds local providers. It needs a provider handle on the contract, a capacity per
  provider, and a reach test.
  > **Amended 10/17/2026: the reach test routes, and `02 §5.8` is amended for it.** A straight-line
  > box bounds the candidates — `EmploymentEngine.Radius` over the authored reach, which over-supplies
  > and never under-supplies — and one Foot walk per candidate inside it decides. That is
  > `ServiceEngine`'s existing shape for a Household judging a school, both halves of it.
  >
  > **The refreshed field was built first and does not work here**, which is `adr/0047`'s own
  > reopening condition: *a measured entry error that no affordable granularity brings inside what
  > the choice loop can absorb*. An access-node entry overstates a real walk by up to 9.2 minutes at
  > each end, and an authored 30-minute reach is a threshold that error straddles, so the field
  > cannot answer the question this decision asks. It is not the cheap half either — a rebuild on a
  > map-wide road lattice ran 16,384 one-to-all searches, settled 4.3 billion nodes and held 1.07 GB
  > of entries, against one search per office in a box.
  >
  > A remote service skips the box and the walk, so a Ruleset whose services are all remote asks the
  > Road Graph nothing.
