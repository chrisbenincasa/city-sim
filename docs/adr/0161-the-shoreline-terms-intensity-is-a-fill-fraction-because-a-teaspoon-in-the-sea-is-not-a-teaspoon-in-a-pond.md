# The shoreline term's intensity is a fill fraction, because a teaspoon in the sea is not a teaspoon in a pond

**Desirability's `− w₅·shoreline` takes its intensity from a Water Body's Bin level *divided by that
body's capacity*, not from the level itself.** The quantity is a **concentration**, bounded in
`[0, 1]`, so a weight means the same thing on a pond and on a sea. **`adr/0034` and `CONTEXT.md` →
Water Body are sharpened rather than contradicted** — both name the quantity and neither states its
units, and this supplies the units they were missing.

Guiding concepts: `LEGIBLE CAUSE`, `HONEST DEGRADATION`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
Nothing consumes land value ([`0125`](0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md)),
so no measurement distinguishes the two readings by their effect on the city. What decides it is a
property of the two readings themselves, and that property is arithmetic rather than taste.

## Why

### The sentence both documents carry, and the word neither of them says

[`0034`](0034-fields-are-sorted-by-source-geometry.md): *"the body's level is the intensity of a
**shoreline line source** onto adjacent land."* `CONTEXT.md` → Water Body: *"a shoreline line source
whose intensity is the Bin's level."*

Both name **which quantity**. Neither says **in what units**, and until milestone 24 task 6b it did
not matter, because a Water Body had no capacity to divide by. [`0160`](0160-a-water-bodys-bin-holds-one-utility-resource-because-a-good-moving-downstream-would-move-with-no-vehicle.md)
derived one — **a body's capacity is its size in Cells times `[water] capacity_per_cell`** — and the
moment it existed, the two readings came apart.

### They come apart by four orders of magnitude, and the absolute reading is the wrong one

The measured sea on `rulesets/coastal.toml` is **33,435 Cells**. A pond on the same map is tens. Under
the absolute reading, the same tonnage tipped into either produces the same level and therefore the
same shoreline intensity: ***a teaspoon in the sea would foul its whole coastline exactly as hard as
it fouls a pond.*** Nobody would author that, and nobody did — it is what the sentence turns into once
a capacity exists under it.

The fraction says the opposite and says it for free: the same tonnage is the pond's whole capacity and
a rounding error in the sea's. **That is not a new mechanism.** It is the *debt-versus-rent gradient*
`CONTEXT.md` → Water Body already claims falls out of the geometry, arriving at the one place that
reads the level.

### And the fraction is what a weight can be authored against

`w₅` is a number a designer sets once and expects to mean the same thing everywhere. Against an
absolute level it cannot: the level's range is the body's capacity, so a weight tuned on a lake is
wrong on a sea by the ratio of their sizes, and **every Ruleset would need a per-body weight or a
per-world one**. Against a fraction the range is `[0, 1]` on every body in every world, and one
number is authorable. ***A coefficient whose units vary per row is not a coefficient.***

## What this rules out

- **A per-body or per-world shoreline weight.** It is what the absolute reading forces and it is the
  taxonomy of water types [`0160`](0160-a-water-bodys-bin-holds-one-utility-resource-because-a-good-moving-downstream-would-move-with-no-vehicle.md)
  spent its argument avoiding.
- **Reading a shoreline intensity off a Bin level in any other consumer without dividing.** The Bin
  level is a stock and is the right thing for drainage, which moves tonnage between bodies. It is the
  wrong thing for anything that asks *how bad is this water*.

## What this does not decide

- **`w₅` itself, its range and its intensity.** All three are unratified and all three are in
  `plans/0002` §D1 with the same named ratifier: the first consumer of land value.
- **Whether the perimeter is the right source set.** That is `CONTEXT.md` → Water Body's *"an area's
  influence on land is its perimeter"*, read rather than decided here.
- **What happens above full.** A Bin cannot exceed its capacity, so the fraction cannot exceed one and
  there is no over-fouled state. Whether there should be is a question for whatever ships dumping.

## The cost

**A body's capacity is now read on a hot path it was not read on before** — every shoreline query
resolves the body and multiplies its Cell count. It is two array reads and a multiply against a query
that already walks a Cell window, so it is not worth caching, and caching it would be the reload bug
[`0015`](0015-all-tuning-data-is-hot-reloadable.md) exists to prevent: `capacity_per_cell`
is hot-reloadable, and a cached denominator would keep answering against the value in force when the
world was made.

⚠ **And the sharpening is a debt against two documents.** `adr/0034` and `CONTEXT.md` → Water Body both
still read *"the Bin's level"*. Neither is wrong — they name the quantity — and neither is complete.
Both are annotated rather than rewritten, because the sentence they carry is the one that made this
question findable.
