# The transcendental tables are sized by the representation, not by the caller

**`exp` and `log` are tabulated at 256 entries each over a unit interval, with rounded linear interpolation and base-2 range reduction. The resolution is chosen so the table stops being the limiting factor: at 256 entries the table contributes about 0.12 ULP of the roughly 1 ULP total error, and the rest is Q16.16's own rounding, which no table size can improve.** These are hash-bearing world-creation constants — the entry count, the rounding, and the committed table values.

`SOLVE THE ACTUAL PROBLEM`, `EMERGENCE`

[`adr/0003`](0003-deterministic-integer-simulation.md) promoted tabulated transcendentals from a contingency to a required core component, and required the resolution to be *a stated figure, not an implementation detail*, because it perturbs the effective `μ` and `μ` is what prevents the stampede [`02 §5.4`](../02-simulation-model.md) describes. **The figure appeared in no document until this one.**

## Why

### The stopping rule is the decision; the number falls out of it

A resolution can be argued to any value, and the argument is always available in both directions — finer is more accurate, coarser is cheaper. That shape of question does not settle, which is why it sat open.

The rule that settles it: **the table must not be the thing limiting the answer.** Q16.16's own ULP is `1/65536 ≈ 1.526e-5`, and every result is rounded to it whatever the table does. So the question is not *how accurate can the table be* but *at what point does the table stop mattering* — and that has one answer rather than a range.

| Entries | exp2 interpolation | log2 interpolation | Bytes/table |
|---|---:|---:|---:|
| 128 | 0.48 ULP | **0.72 ULP** | 516 |
| **256** | **0.12 ULP** | **0.18 ULP** | **1,028** |
| 512 | 0.03 ULP | 0.045 ULP | 2,052 |

At 128 the table contributes about as much error as the representation does — it is still a term in the answer. At 512 it pays a further kilobyte to shrink a term already an order below the floor. **256 is where the table's share drops decisively below the representation's own noise, and that is the whole justification.**

Measured end to end, total error is **1.02 ULP for `exp2` and 0.99 ULP for `log2`** — dominated by the representation, exactly as intended.

### The precision demand is far weaker than a numerics instinct suggests

Worth stating, because the obvious failure here is over-engineering. `02 §5.4` scales utilities *so that meaningful differences are 1–3 units*, and only differences matter in a logit model. A table error of one ULP is an effective utility error of about `1.5e-5` against differences of order 1 — **five orders of magnitude below the smallest difference the design intends anyone to notice.**

The differential test in `TranscendentalTests` measures the quantity that actually matters rather than the intermediate: running `02 §5.4`'s softmax through the committed table and through a double-precision oracle, over candidate sets including exact ties and near-ties, **the worst divergence in selection probability is below 0.001.** Meaningful utility differences move probability by tens of points.

### Errors are in the safe direction, and that is checkable

Quantisation perturbs utilities, which is equivalent to *lowering* `μ` — toward more randomness, away from the `μ → ∞` limit where `02 §5.4` says the city degenerates into stampede-and-crash. The failure mode this resolution could have caused is therefore not available to it. A test asserts the tabulated softmax is never sharper than the oracle.

**The real risk of coarseness was never instability; it was loss of discrimination** — a table so coarse that genuinely different options collapse to the same value and preference flattens into a coin toss. The near-tie candidate set in the differential test is what covers that.

### Base 2, so the integer part is exact

`exp(x) = 2^(x·log₂e)` and `log(x) = ln2 · log₂(x)`. After range reduction the integer part is a shift — exact, no error — and only the fractional part is interpolated. Error therefore does not accumulate with magnitude.

Precisely: **absolute error stays around one ULP for non-positive arguments, and relative error is preserved for positive ones.** The two halves genuinely differ, because a right shift floors while a left shift scales the mantissa's error along with the mantissa. The softmax only ever sees non-positive arguments — the max is subtracted first — so it gets the absolute guarantee, which is the one normalisation needs.

### The tables are committed data, verified rather than trusted

Generating them at startup would need floating point, which `adr/0003` bans here. So the values are literals in the source, and the test project — which may use doubles, because it is not the core — regenerates both tables in double precision and asserts every one of the 514 entries. Rounding is to nearest, half away from zero, which is what the error analysis assumes.

**Interpolation rounds rather than truncates.** Truncating costs a further half ULP and biases every result downward; the measured difference is 1.43 ULP against 1.02. A uniform downward bias on `exp` is a uniform downward bias on every selection probability before normalisation, which is the kind of thing that would never show up as a bug and would quietly shift the city.

## Consequences

### The choice model has a hard horizon at ~11.1 utility units, and it moves with `μ`

**This is the consequence to argue with, not the resolution.** Q16.16's smallest positive value is `1/65536`, and `exp(x)` falls below it at `x < -11.09`. Past that the result is exactly zero, so a candidate more than about 11 utility units below the best is **impossible rather than merely unlikely.**

At `μ ≈ 1` with meaningful differences of 1–3 units, that is 4 to 11 meaningful differences down — an option overwhelmingly worse than the best available, and cutting it off is defensible and arguably desirable. But note what it does to `μ`, which `02 §5.4` calls *a free design knob* and suggests exposing as a difficulty setting:

> **`μ` does not only set sharpness. It also sets where options stop existing.** The argument to `exp` is `μ·V`, so doubling `μ` halves the horizon to 5.55 utility units. Turning `μ` up to make the city more decisive simultaneously deletes the tail of the candidate list, and the two effects compound in the same direction.

Nobody had noticed this coupling. It is a property of representing probabilities in fixed point at all, not of this resolution — a finer table does not move it, and only a wider representation would.

### It is a world-creation constant

By `05 §4`'s test: changing the entry count, the rounding, or any table value changes every choice the model makes, and therefore the State Hash. It cannot be tuned against a profile. Changing it is a deliberate re-baseline that invalidates stored replays, in the same class as changing `draw()`.

### 2 KiB, resident

Both tables together are 2,056 bytes and are read on the hot path of every choice. They will sit in L1 and stay there. Size was never the constraint and the entry-count table above should not be read as though it were.

### `adr/0003`'s owed validation is half discharged

It required the figure be *validated against the herding behaviour `adr/0005` describes*, and `adr/0005` is 🔴. The half needing a running city — does it feel herdy — cannot be checked until the choice model exists, and remains owed. **The half that does not need a city is discharged here**: the table is validated against a higher-precision oracle on the quantity the choice model actually exposes, which is a selection probability. That is a stronger position than the ADR anticipated, and it was available the whole time.

## What would trigger revisiting

- **Gumbel-max replacing the softmax.** `02 §5.4` names it as *exactly the same distribution*, needing no `exp` on the hot path, and cheaper. It would make this table irrelevant to the choice model and leave only `log(1 + x)` and the noise falloff as consumers. The switch is hash-breaking but distributionally neutral, and this ADR does not argue against it.
- **`μ` wanting to exceed about 2.** The horizon tightens as `1/μ`, and past `μ ≈ 2` the tail is cut inside 5.5 utility units, which is close enough to the 1–3 unit band of meaningful differences to start deleting options a designer intended to be live.
- **Utilities widening beyond Q16.16.** If the choice model ever needs values outside ±32,768, the representation changes and this analysis is rerun from its first line, because every figure here is derived from the ULP.
- **A consumer appearing that needs relative rather than absolute accuracy at small magnitudes.** The softmax does not; something computing a ratio of two tiny exponentials would, and would find under a percent of relative precision at `2^-10`.
- **Not performance.** Two kilobytes of L1-resident lookup with one multiply and one shift is not going to appear in a profile. If it somehow does, the answer is Gumbel-max, not a coarser table.
