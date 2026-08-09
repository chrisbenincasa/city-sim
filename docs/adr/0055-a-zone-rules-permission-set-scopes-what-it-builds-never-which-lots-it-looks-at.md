# A Zone Rule's permission set scopes what it builds, never which Lots it looks at

**A Zone Rule samples from every Lot. The permission bit it declares is a term in its *create*
predicate — *this Lot is vacant and admits my kind* — and never a filter on the population it draws
from.** So repainting a Lot's permission set changes what may be built there next and has no effect
whatever on the Building already standing on it.
`PLAYER GOVERNS` `LEGIBLE CAUSE` `EMERGENCE`

## Why

**The alternative hands the player an immortality exploit with no counter.** If a Zone Rule only ever
looked at Lots carrying its own bit, then clearing a Lot's permissions would remove its Building from
every Rule's reach for ever. Nothing could then condemn it however badly it failed. The player's
paintbrush would be a preservation order, discoverable by accident and invisible in every readout.

**And it would invent a coupling `02 §5.9` does not have.** Failure pressure is sourced from three
things — Trips failing, Rules reaching their terminal, local conditions below the Occupants'
tolerance. **Zoning is not one of them and permission is never mentioned.** A Building's mortality
therefore has no documented dependence on what its Lot currently admits, and scoping demolition by
permission would create one by implementation accident rather than by design.

**Repainting doing nothing immediate is what the corpus already says, in three places that have to be
read together.** [`adr/0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md) states
*"upzoning a built block does nothing until its Buildings go"*; the symmetric reading of downzoning is
the same sentence. `01 §5` lists the recovery path for a dead District as *remediation, **clearance of
abandoned stock**, a tax override, service funding, transit, and **rezoning to a lower band***, with
rezoning and clearance as **two separate levers** — if rezoning demolished, clearance would be
redundant. [`adr/0030`](0030-crime-is-an-incident-with-no-perpetrator.md) calls rezoning one of *"the
levers that refill empty buildings"*, which is a thing done to vacant stock.

**Zoning is permission and never instruction**, which is `adr/0025`'s core and the reason there is no
RCI meter anywhere in this design. A permission set that killed things would be an instruction wearing
a permission's name.

**The cost is wasted samples, and that cost is the behaviour model rather than a defect.** A housing
Zone Rule that samples a Lot zoned industrial-only cannot build there and the sample is spent. On a
specialised map most samples land where their Rule cannot act. But `CONTEXT` → Zone Rule justifies
sampling because *developers do not evaluate every Lot either* — and a developer looking at a plot and
finding it zoned for something else is precisely that. The rejection is the model working, not the
instrument idling.

## Consequences

- **Nothing in the city is ever immortal.** Every Building on every Lot is in some Zone Rule's
  sampling population, so every Building is eventually looked at and condemned if its failure pressure
  says so.
- **Slice 8's derelict Buildings decline like anything else.** `02 §4.3` says a hot reload marks
  Buildings whose kind no longer exists **derelict rather than deleted**; under this ADR such a
  Building is still sampled and still dies of its own failures, rather than becoming a permanent
  monument to a Ruleset edit.
- **"Zone size" is not a property of a Zone Rule's cost.** Its per-trigger cost is set by its sample
  size alone, which is what slice 10's tripwire measures, and the tripwire is now measuring something
  structurally true rather than a consequence of how the population was filtered.
- **A Lot with an empty permission set is a real and useful state** — land the player has deliberately
  taken out of development while whatever stands on it lives out its life.
- **Two Zone Rules may sample the same Lot in one trigger.** Contention is resolved by scan order,
  with the start rotated per trigger (`02 §4.2`), because `02 §5.5`'s bid-price contest needs prices.

## What would trigger revisiting

- **A demolition verb the player drives directly.** Clearance is listed in `01 §5` as its own lever
  and does not exist. When it arrives, *the player removed permission* and *the player demolished*
  become two distinct actions, and this ADR only governs the first — it should not be read as saying
  the player may never clear a Lot.
- **Wasted samples proving to be a real cost** rather than a modelled rejection. If a mature city with
  many specialised Zone Rules spends most of its sampling budget on Lots its Rules cannot use, the
  answer is a cheaper population, not a permission filter — the filter would bring the immortality
  back. `02 §4.2`'s Chunk partition is the intended lever.
- **A non-conforming-use mechanic being wanted deliberately** — a Building whose use is no longer
  admitted decaying faster because it is non-conforming. That is a fourth failure-pressure source, and
  it would need adding to `02 §5.9` rather than being smuggled in through the sampling population.
