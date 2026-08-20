# The land value target never stops moving, so the question is what the lag rests around

**Milestone 9's decision 6 is settled, and the thing it worried about is not what the field does.** The
minimum-step-of-one in `MapLayers.Step` is safe: with `w₁` deleted by
[`0122`](0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md)
the target is **exogenous**, the gap reaches exactly zero, and the operator stops. What moves the field
is that **the target itself never settles** — the noise term reads a Segment's volume at the instant it
is asked, so desirability swings with the commute and land value chases a daily cycle for ever.

**Measured, not argued**: at steady state **185 of 262 resident Cells move on a typical cadence sample**,
the widest peak-to-trough swing is **74,373** raw Q16.16 units, and **the field does not trend** — the
mean over one half of a four-Day window is −567,787 against −563,871 over the other.

⚠ **The ±1 flicker decision 6 was written about would be a swing of ONE raw unit.** The observed swing
is four orders of magnitude larger. ***The two phenomena are not the same size, so a test that merely
asked "does it move" would have confirmed the wrong one.***

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`.

**Measurable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md),
and it was — `LandValueSteadyStateTests`, on `rulesets/fouled.toml`, eight Days to settle and four
observed. The **arguable** half is separated out below and is *not* settled here.

## Why

### The dead band was repaired against a target somebody else supplies, and that is no longer the case

`MapLayers.Step`'s minimum step of one exists because `RoundDiv(gap, tau)` is zero for a small gap, so
without it a Cell settles up to `tau/2` short of its target **and on whichever side it approached from**
— path dependence in saved state, which under `05 §4` is two cities. That argument is sound and stands.

Decision 6's worry was that the argument was made against a *constant* target. If land value were a term
in its own target the fixed point would in general be non-integral, the gap would never reach zero, and
every Cell would flicker by ±1 for ever in **saved, hashed** state on a 256-Tick cadence.
[`0122`](0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md)
removed the term, so the fixed point is whatever the composition says and the gap **does** reach zero —
which `LayerFieldsTests.Land_value_converges_on_its_target_from_either_side` already pins, from both
sides, exactly.

### But the target is not constant, and the reason is a property of the composition

The noise term is `LineSourceQueries.Noise`, which reads `RoadSegmentTable.VolumeForward` — **the count
of Vehicles on the Segment at that instant**. Land value is the only part of the composition with
memory. So in `rulesets/fouled.toml`, where every Household owns a car, the noise term is **zero at
midnight** in every Cell of the city and non-zero at eight in the morning
(`FouledRulesetTests.Noise_is_zero_at_midnight_and_that_is_the_composition_not_the_world`).

The land value cadence is `tick % 256 == 16` against a 2,048-Tick Day, so eight samples a Day land at
fixed hours. The field therefore tracks a **daily orbit**, and what a Cell settles on is the average the
lag takes over those eight hours rather than any single reading.

### The measurement, and what each number is for

On `rulesets/fouled.toml` at 4,000 Citizens, eight Days to settle, four Days observed, 32 cadence
samples over 262 resident Cells:

| Reading | Value | What it settles |
|---|---|---|
| Cells moving per sample | **185** of 262 (min 0, max 212) | The field is not at rest and will not be |
| Cells that never moved | **50** | Clean, quiet ground: target zero, value zero, gap zero — the operator *does* stop |
| Widest swing | **74,373** raw | Not the dead band. A ±1 flicker is a swing of **1** |
| Mean swing | **22,863** raw | ≈0.35 units against a field whose deepest Cell is ≈−28 |
| Mean value, early half | **−567,787** | — |
| Mean value, late half | **−563,871** | **No trend.** `0006`'s requirement, asserted on the average because the level is *supposed* to move |

⚠ **The fifty still Cells are the load-bearing row.** They are the proof that the operator terminates:
where the target is zero the gap reaches zero and nothing flickers. Had decision 6's worry been real
they would be moving too.

## What this refuses

**A dead band.** Restoring one to stop the motion would reintroduce exactly the path dependence the
minimum step exists to remove, and would not stop it anyway — the motion is 74,000 units wide and a
dead band is one unit deep.

**A slower lag as a fix.** `land_value_tau` is a designer's number about how long a neighbourhood takes
to notice a change. Raising it to damp a daily cycle would be tuning a tuning knob to hide a modelling
choice, and the modelling choice would still be there.

## What is left open, and it is arguable rather than measurable

⚠ ***Should land value swing with rush hour at all?*** This record settles that it **does**, by how
much, and that it does not trend. It does not settle whether that is the right field. A land value that
rises overnight and falls at eight is a traffic meter with a lag on it, and the case for the other
reading — that land value should reflect *typical* conditions, so the noise term wants a time-average
rather than an instant — is a design argument with no number that refutes it.

**It is filed in `plans/0002` rather than decided here**, because under
[`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) the measurable half
is done and the remainder is a claim no machine settles. ⚠ **And it is not urgent**: nothing reads land
value yet ([`0125`](0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md)),
so the first consumer is the first thing that can be wrong about it. Milestone 13, the price surface, is
the trigger for both.

## When to revisit

**When the noise term acquires memory** — a rolling average of volume, or a Segment storing a typical
day. That is the change that would make this record's central sentence false, and
`The_field_oscillates_within_a_bound_and_does_not_trend` asserts the swing is **large** precisely so
that it goes red rather than quietly passing when the motion collapses to the dead band's size.

**When a consumer reads land value.** A daily orbit is invisible while nothing looks; the first thing
that prices land off it inherits the orbit.
