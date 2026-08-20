# Land value is not a term in its own target, and a term on both sides of a lag is a gain

**`w₁·land_value` is deleted from the desirability composition.** Desirability is
`− w₂·pollution − w₃·noise + w₄·amenity − w₅·shoreline`, composed at the point of use, and land value
moves toward it under the momentum operator that has been in the tree since slice 3c. **The persistence
`w₁` appears to supply is already supplied by the lag**, and holding one property in two mechanisms is
what made the composition ill-formed rather than merely redundant.

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `LEGIBLE CAUSE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
The algebra below is arithmetic and is not what is being decided; what is being decided is whether the
term should exist, and no number refutes an answer to that.

⚠ **No city changes and no State Hash moves.** `w₁` never had a value: no Ruleset declares a
desirability weight, `LayerRates` holds three time constants and no weights, and `MapLayers.Desirability`
throws rather than composing. This ADR deletes a term from a formula that has never been evaluated.

## Why

### The term puts the field on both sides of its own lag

`02 §2.4` states two things in adjacent paragraphs, and they have never been read together. The first
is the composition, with land value as its opening term. The second is that land value *"moves slowly
toward the current desirability rather than tracking it, which is both realistic and a stabiliser
against oscillation"*. Substituting the second into the first, and writing `S` for the four terms that
are not land value:

```
target   = w₁·land_value + S
gap      = target − land_value = S − (1 − w₁)·land_value
rest at  = S / (1 − w₁)
```

**So `w₁` multiplies the other four terms rather than joining them.** It is a gain of `1/(1 − w₁)`, it
is stable only for `w₁ < 1`, and at `w₁ = 1` the gap is a non-zero constant — every Cell walks away
from every other at `S/τ` per cadence, for ever, in saved and hashed state. Above one it accelerates.
At `τ = 8`, which is what all eight shipped Rulesets set, and on the 256-Tick cadence:

| `w₁` | Where a Cell with `S = −200` comes to rest |
|---|---|
| 0 | −200 — the other terms land as authored |
| 0.25 | −267 |
| 0.5 | −400 |
| 0.9 | −2,000 |
| **1.0** | **never; the Cell falls 25 every cadence for ever** |
| **1.25** | **never; the gap grows each cadence** |

***A term that appears on both sides of a lag is a gain, and calling it a weight hides which way the
loop runs.***

### The harm is to the four numbers nobody was arguing about

The divergence at `w₁ ≥ 1` is the dramatic failure and it is the less likely one, because a designer
authoring five weights is unlikely to type a one. The failure that would actually have happened is
quieter: **a designer authors `w₂` for pollution and the city uses `w₂/(1 − w₁)`**. The number tuned is
not the number in force, and nothing in the field's behaviour says so — the Cell simply rests somewhere
else, and the next tuning pass corrects for a gain nobody named. That is `plans/0012` **Cause 5** with
the clause not merely detached from the digits but never written: **the sentence saying what `w₂` means
would have been wrong in every document that carried it**, and `LEGIBLE CAUSE` is the concept it fails.

### Three documents claim stability and each is describing the loop it cannot see

`02 §2.4` and [`plans/0009`](../../plans/0009-map-layers.md) §7 both call the momentum *"a stabiliser
against oscillation"*. `MapLayers.Step`'s doc-comment goes further and states it outright: *"It cannot
oscillate: the step never exceeds the gap, because a gap of one moves by one."* All three are true of a
lag chasing a target somebody else supplies. None of them was ever tested against a target that moves
when the field moves, because no such target has ever existed — **`MapLayers.SetLandValueTarget` has
three test callers and none in `src/`**. The sentences are not wrong today; they are sentences whose
truth was resting on the producer being unbuilt, and the milestone that builds the producer is the
commit that would have falsified them without editing one of them.

⚠ **Deleting `w₁` restores the first two and does not settle the third.** With the target exogenous the
lag genuinely does stabilise. What survives is the integer question: the minimum-step-of-one exists to
remove a dead band, and against any target whose fixed point is not an integer a Cell oscillates by ±1
regardless of `w₁`. That is [`0034`](../../plans/0034-the-land-value-target-and-the-composed-layers.md)
decision 6 and this ADR deliberately does not reach it.

### The formula has a third home and no mechanical check can see it

`0034` recorded the formula as appearing in *"exactly two documents"*. It appears in three places —
`02 §2.4`, `plans/0009` §7, and `MapLayers.Desirability`'s own doc-comment. The third is the copy a
programmer reads, and it is **invisible to every corpus check in `tests/Borough.Tests/Corpus/`**,
because those are document-to-document by construction. ***A formula nobody evaluates is not checked by
being read, and a formula in a doc-comment is not checked at all.***

### What was considered and is not refused

There is a real phenomenon `w₁` could have been reaching for: valuable land becoming more valuable
because it is valuable. Prestige feedback, and `04 §7`'s gentrification damper already reads this field
and implies somebody expects it. **That is not refused here — it is separated.** Deliberate positive
feedback is a mechanism with a stated gain, a stability bound something refuses at load, and a
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) ratifier naming a
machine, a world and a quantity. Under [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)
it is **undesigned** rather than unbuilt or refused. What is refused is acquiring it by accident, as the
first item in a list of five weights, from a formula in which it reads as the least remarkable term.

## Consequences

- `02 §2.4` and `plans/0009` §7 are amended, and `MapLayers.Desirability`'s doc-comment with them. Three
  copies, one of which no test can reach.
- **Two weights survive this milestone, not five.** Composed against
  [`0034`](../../plans/0034-the-land-value-target-and-the-composed-layers.md) decision 2, amenity and
  shoreline being unbuilt, what needs authoring is `w₂` and `w₃` — which is what decision 5 has to find
  ratifiers for, and it is a smaller problem than the one it was.
- **The stability bound and its load-time refusal are not needed and are not written.** A `w₁ < 1` check
  is machinery whose only purpose is to police a term that no longer exists.
- Desirability is now a composition of things **outside** land value, so the field has one input path and
  the momentum is the only thing giving it history. That is the property the end-of-run bound and the
  `adr/0006` long-run assertions are written against.
- Nothing in `src/` changes but a doc-comment. No Ruleset key is added or removed, no baseline moves.

## What would trigger revisiting

- **Somebody wants prestige feedback and can say what it is for.** Then it returns as its own mechanism
  with its own ADR, its gain stated where the number is authored, a bound something refuses at load, and
  a ratifier. It does not return as a term in this formula.
- **`04 §7`'s gentrification damper turns out to need land value in its own target** rather than merely
  to read the field. That would be the same loop arriving from the other direction, and it is the one
  place in the corpus where the requirement could plausibly be real.
- **The composed field is found to have no persistence in play** — land value tracking pollution so
  closely that the momentum is doing no perceptible work. That is the observation `w₁` would have been
  the wrong fix for, and the right fix is `τ`, which is already Ruleset data.
