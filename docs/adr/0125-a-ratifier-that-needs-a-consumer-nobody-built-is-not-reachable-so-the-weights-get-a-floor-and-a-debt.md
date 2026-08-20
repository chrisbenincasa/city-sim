# A ratifier that needs a consumer nobody built is not reachable, so the weights get a floor and a debt

**`w₂` and `w₃` are registered in `plans/0002` §D1 with *two* entries each, not one.** A **floor** that is
reachable inside milestone 9 — machine: the milestone's acceptance run; world: `rulesets/congested.toml`;
quantity: that the field varies at all, that **both** terms are visible in it, and the pollution/noise
correlation across Cells. And the **real** ratifier recorded as **owed**, triggered by **milestone 13,
the price surface**, which is the first named consumer of land value (`02 §5.6`'s initial prices).
**Neither entry may be read as the other.**

Guiding concepts: `HONEST DEGRADATION`, `SOLVE THE ACTUAL PROBLEM`.

**Measurable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
in its floor half — every quantity above names a machine that produces it — and the decision recorded
here is what to write on the day, which is
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)'s question rather
than a measurement.

## Why

### Nothing in the city reads land value, so the scale has no quantity to be refuted by

Read from `src/`: outside `MapLayers` itself, the only readers of `LayerCellTable.LandValue` are
`Borough.Headless`'s layer dump — a picture — and `RulesetLoader`, which resolves the layer's name. **No
simulation consumer exists.** That is `06`'s named risk for this milestone stated as a fact about the
tree rather than as a warning.

[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) as amended twice
asks a ratifier to name a **machine**, a **world** and a **quantity**. The quantity that would refute an
absolute scale for land value has to be produced by a consumer — what a compulsory purchase costs,
whether a gentrification damper fires, what an initial price comes out at. **Every one of those is
unbuilt, and none arrives in this milestone.**

⚠ **Writing a ratifier anyway would repeat milestone 7 task 8 exactly.** The shed radius's ratifier
named a machine, a world and a quantity correctly and was **still unreachable**, because the state it
asked to observe — occupancy approaching 1 — cannot occur. ***Nothing in `0052`'s checklist asks whether
the named state can occur***, and that is the failure available here, one milestone after it was found.
The difference is that this time it is visible **before** the number is written rather than by the run
that was meant to ratify it.

### `w₃` is not merely unratified, it is not yet meaningful

Noise is unbuilt, so **the units it returns are a free choice made by task 1**. Whether the query yields
decibel-scaled integers or a normalised range, `w₃` absorbs the difference exactly — the product
`w₃·noise` is what enters the composition, and only the product is constrained. **So `w₃` cannot be
chosen before the noise query's output units are fixed**, and that is a sequencing fact rather than a
question anybody may answer here. `w₂` does not share it: pollution is already stored pre-normalised in
kernel units.

### The floor exists because four milestones is a long time to carry an unchecked field

Milestone 13 is where the real ratifier becomes reachable. Between 9 and 13 sit **three other
milestones**, and for all of them the weights would be in the city, in saved and hashed state, with
nothing at all asking whether they are sane. Two failures are cheap to detect and expensive to discover
late:

- **A field quantised to nothing.** Land value is an integer and the composition rounds. A `w₂` small
  enough makes every Cell land on the same value, and the field is uniform — visibly working, carrying
  no information.
- **An inert second term.** If `w₃·noise` is negligible beside `w₂·pollution` everywhere, the build is a
  one-term field wearing a two-term formula. That is
  [`0123`](0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md)'s
  concern arriving as a number rather than as an absence, and it would not announce itself.

**The floor is a floor and not a ratification.** It can refute a weight; it cannot confirm one. It is
recorded as a separate §D1 entry for that reason — ***a reachable check standing in the place of an
unreachable one is how a number comes to look settled***, which is `plans/0012` **Cause 5**'s shape
applied to status rather than to digits.

### The world is the half this corpus keeps getting caught by, and here it is checkable in advance

A generated city sizes its demand and its supply from **one population**, so the same number sizes both.
It is why parking occupancy was flat at every population, why `v/c` peaked at 0.44 at three city sizes,
and why `foot_crossing_every` is inert. **A land value field composed from pollution and noise over a
generated city is the fifth candidate**: industry and roads are both placed in proportion to that same
population, so the two source fields may co-vary strongly across Cells.

⚠ **If they do, no ratio of `w₂` to `w₃` is identifiable** — every ratio fits equally well, and a
ratifier asking whether the weights are right cannot answer. **This is why the pollution/noise
correlation is in the floor's quantity list rather than being left for the ratifying run to discover.**
It is the pre-flight check the shed radius never got. If the correlation is high, the answer is a
**hand-authored world** rather than a different weight, on `congested.toml`'s and `scarce.toml`'s
precedent — a demonstration file that separates the two sources deliberately.

**`congested.toml` is named as the floor's world** because
[`0123`](0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md)
established that only it and `scarce.toml` have any driver, and noise is identically zero in the other
six Rulesets. **A floor run on a world where one of the two terms cannot vary is not a floor.**

## Consequences

- **Two §D1 entries per weight**, written on the day the weight is written — task 3's Definition of done
  carries the exact text, because ***an obligation nobody scheduled is indistinguishable from one nobody
  wrote down***.
- **The milestone's acceptance run gains the floor's three quantities**, which is task 7's.
- **Task 1 fixes the noise query's output units, and `w₃` is chosen after it**, never before.
- **The weights are unratified from milestone 9 until milestone 13.** That is recorded as a duration
  rather than left implicit, and `06`'s dependency graph already forces 13's position, so it is not a
  span anybody can shorten by re-ordering.
- **A hand-authored Ruleset may turn out to be required** before the ratio means anything. It is not
  built here and it is not promised here; the correlation reading is what decides.

## What would trigger revisiting

- **The correlation comes back high.** Then the ratio is unidentifiable on a generated city, the floor's
  second quantity cannot be read, and what is owed is a world rather than a number.
- **A consumer of land value is built before milestone 13.** The real ratifier becomes reachable early
  and the debt closes early; nothing about it depends on 13 specifically beyond 13 being the first.
- **`w₂` is found to move something other than scale.** The argument here treats it as setting the
  field's scale and the ratio as setting its shape. If a consumer turns out to read the field's
  *sign* or its *zero point*, that is a different number with a different ratifier.
