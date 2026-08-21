# 0130 — The Pool's bound is a duration, and the unhoused channel ships with the gate

**Guiding concept: a bound that cannot trip in its own headline case is not a bound.**

**Status:** accepted, 2026-08-20, with the user in the room. Closes
[`plans/0035`](../../plans/0035-hinterlands-and-arrival-through-the-gate.md) decisions **3** and **3a**.

---

## The decision

**The unhoused Departure channel ships in the same milestone as the gate.** A Household that has been
looking longer than it will keep looking gives up and leaves through a gate.

**The bound is a duration.** The Ruleset authors how long a Household keeps looking; the engine derives
the occasion count from `[placement] revisit_ticks`.

**The count of dwellings considered is recorded, and is not the bound.** A second bound, on **refusals**,
is authored when acceptance exists — milestone 16 — and the two then run first-to-trip.

The **housed** and **destitute** channels do not ship here.

---

## Why the sink is owed by this milestone and not by 19

`CONTEXT.md` → Unplaced Pool states it: *"the day immigration arrives, that reason evaporates and
Departure becomes load-bearing. **Whoever builds the gate owes the give-up rule in the same
milestone.**"*

⚠ **The build says otherwise, and the build is out of date rather than wrong.** `UnplacedTable`'s
doc-comment routes the give-up counter and refusal reasons to *"milestone 9a"* — now **19** — on
[`adr/0054`](0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md)'s reasoning — *"The Pool is a minimal list, not 9a's mechanism. No refusal reasons, no immigration"*. That was
correct when nothing created a Household after world creation: the Pool was a subset of a fixed
population and could not grow with elapsed time whatever it did.
[`adr/0006`](0006-no-collection-grows-with-elapsed-time.md) was satisfied **for a reason that had
nothing to do with Departure**, and the gate is what removes the reason.

***A minimal list stays minimal until something needs more, and an inflow is something needing more*** — and `adr/0054` named immigration as the thing it was deferring, so this is that record's own trigger firing rather than a reversal of it.

## Why only the unhoused channel

`CONTEXT.md` gives Departure three channels, and they are not three sizes of the same thing:

| Channel | What it needs | Where it lands |
|---|---|---|
| **Unhoused** — entered the Pool, failed repeatedly, gave up | a bound and a threshold | **here** |
| **Housed** — was living here and chose to leave | `02 §5.4`'s comparison ([`adr/0102`](0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md)) | 16, with the comparison ([`adr/0128`](0128-the-gate-ships-before-the-comparison-that-walks-through-it.md)) |
| **Destitute** — could not work and could not afford to leave | Unemployment, and a floor | later |

**Only the housed channel is a comparison**, so it is the only one `adr/0128` pushes downstream. The
unhoused channel needs nothing that does not exist.

**And it has exactly one reason, which is honest rather than a stub.** `PlacementEngine` is blind by
design — *"Acceptance needs rent, a commute and a tolerance; none exists, so any member would take any
dwelling"* — so the only thing that can go wrong is **no room**. That is precisely the **capacity**
diagnosis `CONTEXT.md` assigns to this channel, whose remedy is *build more*. ***A single reason is
honest when the mechanism admits one; it is a stub only when the mechanism admits more and records
one.***

---

## Why a duration and not a count

This is [`adr/0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)
one level down. That record struck an absolute sample because *"it makes the quantity the city actually
feels, which is a fraction of the city per cycle, depend on the size of the city."* Here the quantity
the city feels is **how long a family looks for a home**, and a count of occasions makes it
`N × revisit_ticks` — so retuning the **placement cadence** silently changes how long people wait, and
nobody editing `[placement]` would expect to.

🔴 **And a count of dwellings *considered* cannot fire in the case this channel exists for.** A city with
no vacancies offers nothing, so nothing is considered, so the counter never advances — and the Pool
grows without bound in **exactly** the failure whose diagnosis is *build more*. ***A bound that cannot
trip in its own headline case is not a bound.***

## Why the count is still recorded

`00-vision.md`'s flagship Evidence example is *"Considered 20 dwellings over 4 months."* **Two numbers:
one bounds, one describes**, and the describing one is what distinguishes a Household that saw plenty
from one that saw nothing. `CONTEXT.md`'s rule — *"a channel names a remedy; a reason names a utility
term"* — is what keeps that from needing a fourth channel.

⚠ **The refusal bound must not be authored now.** `PlacementEngine` never refuses anything; it fails to
find room. So a refusal count is **identically zero** until acceptance exists, and a number that cannot
move is one no world can ratify — [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
as amended. This is milestone 9's `w₃` exactly: *not choosable until the mechanism that gives it units
ships*. ***An inert number in a Ruleset is one a designer tunes expecting an effect.***

---

## Consequences

- **One hash-bearing number opens here** — how long a Household keeps looking — with a `plans/0002` §D1
  row naming a machine, **a world** and **a quantity**. ⚠ **The world is reachable**: arrivals are
  Command-driven at 11, so a world where arrivals outpace housing can be authored deliberately.
- **`CONTEXT.md` → Unplaced Pool is corrected in the same sitting.** It said *"a limited number of
  failed attempts"*.
- **`UnplacedTable` gains an attempts-or-since column** beside `adr/0129`'s gate column.
- **`adr/0006` is discharged for the Pool by a mechanism rather than by an absence**, for the first time.
- **Milestone 16 inherits the refusal bound and the reason split**, named rather than implied.
