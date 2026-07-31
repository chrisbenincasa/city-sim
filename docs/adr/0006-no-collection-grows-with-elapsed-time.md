# No collection may grow with elapsed time

**No collection in the simulation may grow as a function of elapsed game time.** Every collection must be bounded by something physical — map area, zoned land, built capacity, a fixed cap — or must have an explicit sink that removes entries at a rate comparable to the rate they are added. This is a standing constraint on all future design, not a one-off fix.

## Why

Dwarf Fortress's framerate death is the canonical case: its cost grows with *elapsed playtime* rather than with fortress size, because item stacks accumulate monotonically and every stack permanently taxes hauling and spatial queries. A fortress does not become slow because it is large; it becomes slow because it is old. RimWorld hit the same wall from a different direction and had to ship a garbage collector for demoted world-pawns, which the community still found insufficient.

We have now written this bug twice in two days, in two different documents, without noticing either time — first as an ungarbage-collected Cohort population, then as an unplaced Household pool with no exit. Both looked locally reasonable. That is the point: this failure mode is not caught by reviewing a decision in isolation, so it needs to be a standing rule that every new collection is checked against.

Most of our collections are naturally bounded and that is not an accident worth relying on: Citizens are bounded by dwellings, dwellings by Lots, Lots by zoned land, zoned land by the map. The dangerous collections are the ones representing *pending* or *historical* state — queues, archives, logs, "might come back later" sets — because nothing physical limits them.

## Consequences

- **Households that cannot find housing give up and leave** after a bounded number of failed attempts, rather than waiting indefinitely. See `02-simulation-model.md` §5.
- **Departure is permanent.** No archive of departed Households, no "would return if conditions improved" set. The return mechanic is mechanically redundant anyway: a city that fixes its housing sees attractiveness rise and immigration generate *new* Households, which is the outcome the player wanted. If an individual return ever matters narratively, regenerate one deterministically from a seed rather than storing it.
- **History is aggregate, never individual.** Departure counts by reason and by month are a fixed-size time series and are what the diagnostic UI actually needs. Individual records of departed people are not kept.
- **A headless test asserts this.** Run 100k+ ticks and check that no collection grows monotonically. This is the only reliable way to catch the next instance, since the failure is invisible at design time and takes hours of play to manifest.
- We lose the ability to answer "who used to live here?" That is accepted.
- **Dissolution joins Departure as a population sink.** [`0011`](0011-household-life-stages-and-self-generating-population.md) added Households that end at the close of their life cycle. Dissolution removes its Citizens outright, with no record retained — the same rule, applied to a channel that did not exist when this was written.
- **Pins are the one bounded exception.** A Pinned Household retains a **fixed-size ring** of recent Trips. Fixed-size is what makes it legal: the number of Pins is player-bounded, but a game's worth of Trips is not.

## What would trigger revisiting

**Nothing.** This is the one ADR here with no reversal criteria, and that is deliberate rather than an oversight.

Every other decision in this directory is a trade-off with a plausible other side. This one is not: a collection that grows with elapsed time is a defect in every case, and the only thing a "reversal" could mean is deciding to tolerate a known performance death spiral. What *can* change is how a particular collection satisfies the rule — a cap replaced by a sink, a ring buffer resized, an archive replaced by deterministic regeneration from a seed. Those are implementation choices beneath the constraint, not challenges to it.

If a future design seems to require an unbounded collection, the correct reading is that the design is wrong, not that this rule is.
