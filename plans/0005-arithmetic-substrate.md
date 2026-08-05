# 0005 — Slice 2: the arithmetic substrate

> Slice 2 of [`0003-build-plan.md`](0003-build-plan.md). Governed by
> [`adr/0003`](../docs/adr/0003-deterministic-integer-simulation.md),
> [`02 §8`](../docs/02-simulation-model.md), [`05 §4`](../docs/05-technical-architecture.md).

**Everything in `Borough.Core` computes on top of this slice, so it exists before the first thing
that computes.** Typed quantities, a fixed-point library, division and shift helpers with stated
semantics, tabulated `exp` and `log`, and the counter-based `draw()`. No entity, no table, no Tick —
just the arithmetic every one of them will use.

**Risk retired.** Two, and they are different in kind. The first is ordinary: `adr/0003` is explicit
that integer and fixed-point discipline *is the first thing built: before rendering, before the
Ruleset, before any entity exists*, because a float that enters the core later is a defect the State
Hash cannot find — both runs wrap or round identically and the oracle certifies a divergence it
cannot see. The second is structural: **typed quantities are the item in the whole corpus flagged as
most expensive to retrofit**, because they touch every arithmetic site in the core. Written now they
cost a slice; written after ten thousand call sites exist they compete with actual game work and
lose.

---

## Gate

**Cleared, session eight.** `adr/0003` closed Q16.16's scope, the overflow policy, the ratios-only
multiplication rule, typed quantities, the normative `draw()` definition, and the `purpose_tag` rule.
`02 §8`'s rule list and `02 §10`'s testing strategy were grilled alongside it.

~~**One thing inside the gate is not actually settled** — the `exp`/`log` table's **resolution**.~~
**Settled during the slice, in [`adr/0038`](../docs/adr/0038-the-transcendental-tables-are-sized-by-the-representation.md).**

**Status: all seven tasks done.** The slice produced two decisions rather than the one it was gated on —
`adr/0038` for the table resolution, and an **amendment to `adr/0003`'s normative hash**, which had a
structural defect that writing its first known-answer vectors exposed. See *Decisions owed* below.

## Prerequisites

Slice 0. Ideally slice 1, because K1 reports the cost of `checked` and this slice is where `checked`
gets scoped — but the scoping decision is already made and K1 only confirms it, so the two can
overlap.

---

## Tasks

### 1. Typed quantities

`Money`, `Ticks`, `Tiles`, `Ratio` as `readonly record struct` wrappers over their integer
representation. The point is not tidiness — it is that **a convention needs a type or it needs a
lint; it never survives on discipline.**

| Type | Representation | Why |
|---|---|---|
| `Money` | **i64**, signed | Income flow at target population is ~10⁹ per period against an i32 max of 2.1×10⁹ — **i32 is already exceeded**. Stays signed because stocks are non-negative while every delta, flow and balance-of-payments figure is inherently signed; unsigned would arm `balance - cost` while protecting the one column with no bugs in it |
| `Ticks` | **u64** | The clock. There is no other time base |
| `Tiles` | **i32**, whole Tiles | The map. Space in the core is Tiles and nothing else |
| `SubTiles` | **i32 as Q16.16** | Sub-Tile positions, and *only* positions. ±32,768 against a 4096-Tile map is 8× headroom, provably, forever |
| `Ratio` | **i32 as Q16.16**, dimensionless | The only thing that may be multiplied by another fixed-point value |

The operator surface is the deliverable, not the structs:

- **`Tiles × Tiles` must not compile.** `Ratio × Ratio` must. This is what makes *fixed-point
  multiplication operates on dimensionless ratios, never on absolute quantities* structural rather
  than disciplinary.
- **`Ticks + Tiles` must not compile.** Adding a duration to a distance is a real bug wearing valid
  syntax.
- `SubTiles × int` compiles; `SubTiles × SubTiles` does not. `position += velocity × ticks` is the
  stated use and it is fixed × *integer*.
- **`balance - cost` returns something the caller must handle.** The non-negative-Money invariant
  lives once, on the type's operations, rather than at every call site. A `TryDebit`-shaped API, not
  an operator that silently produces a negative stock.
- **Zero runtime cost.** They erase to their underlying integer. Assert it — a benchmark showing
  identical codegen and zero allocation against the raw integer, not an assumption that the JIT
  will oblige.

### 2. The fixed-point library

Q16.16, with `checked` **inside this library and nowhere else**. Ambient arithmetic in the core stays
unchecked: the check is worthless where the width already closes the question, and it is not free on
the hottest code in the project.

- Multiply, divide, lerp. Range assertions that state what they are guarding rather than asserting a
  generic bound.
- The one genuine fixed × fixed site in the whole design is IDM and the VDF — both Phase 2, both
  operating on ratios in roughly [0, 3] whose fourth power sits against ±32,768 with three orders of
  headroom. **Range sizing is deferred to [`03 §5`](../docs/03-agent-architecture.md) legitimately**;
  nothing in Phase 1 is blocked by it.
- Q32.32 is available for a genuine product of absolutes, decided **per site with a written reason,
  never as a default** — it costs an i128 intermediate, which is roughly the price of the `checked`
  branch it would replace.

### 3. Division and shift helpers

Two constructs that are **specified rather than banned**, because banning them would ban arithmetic.

- **Division goes through a stated rounding helper, floor by default.** C# truncates toward zero, so
  `-7/2 == -3` while `7/2 == 3` — deterministic, but a **directional bias at every zero crossing**,
  which is a slow leak in anything that accumulates around one.
- **Non-constant shifts go through a range-checked helper.** Shift counts are silently masked, so
  `x << 32` is `x << 0` — a bug that produces plausible output forever.
- Raw `/` and non-constant `<<` are then a lint (slice 3), not a convention.

### 4. Tabulated `exp` and `log`

Promoted by `adr/0003` from a contingency to a **required component of the core**. The contradiction
it fixed was hard rather than cosmetic: `adr/0003` bans `Math.*`, and `02 §5.4`'s choice model is a
softmax over `exp`, so as written **the choice model could not legally be implemented at all**.

- A table generator with **defined rounding** and an explicitly parameterised resolution.
- `exp` for the softmax; `log` for `02 §2.4`'s noise falloff and for `log(1 + x)` on count-like
  utility terms. **No `sin` is needed anywhere in the design** — worth stating, because a general
  fixed-point trig library is a plausible-looking yak.
- Tests: monotonicity across the whole domain, a stated maximum error bound, and exactness of the
  round trip at table points.
- **Precision here is behavioural rather than cosmetic.** The table's resolution perturbs the
  effective `μ`, and `μ` is what stops the choice model from stampeding. See *Decisions owed*.

### 5. `draw()` — the counter-based RNG

Normative, and written out in `adr/0003` with literal constants precisely so it is re-implementable
from the document alone.

```
mix(z):                                        // SplitMix64 finalizer
    z ^= z >> 30 ;  z *= 0xBF58476D1CE4E5B9
    z ^= z >> 27 ;  z *= 0x94D049BB133111EB
    z ^= z >> 31
    return z

GOLDEN = 0x9E3779B97F4A7C15

draw(seed, entity, tick, purpose):
    h = mix(seed   + GOLDEN + entity)
    h = mix(h      + GOLDEN + tick)
    h = mix(h      + GOLDEN + purpose)
    return h
```

All arithmetic is `u64` and **deliberately wrapping** — one of the two named exceptions to the
overflow policy, and it must be marked as such in the source or a future reader will "fix" it.

- **The RNG is a format, not an implementation detail.** An Input Log reproduces a run only if the
  hash is bit-identical, so changing `draw` is a save-format-class change under `05 §7`. If profiling
  ever demands fewer rounds, that is a deliberate re-baseline and not a free optimisation. Say so in
  a comment at the definition, where somebody profiling will actually read it.
- Tests: known-answer vectors committed to the repository; bijection over a sampled counter domain;
  and — the one that matters — **a second, independent implementation written in the test project
  from the ADR text alone, asserted to agree**. The stated property is that two people can implement
  this from the document and get the same city; the only way to test that property is to do it twice.

### 6. `PurposeTag` — the central enum

A compile-time integer constant from one central enum, **never a string**. A string needs string
hashing, which is banned, and a mistyped one collides silently — correlating two decisions invisibly
with no runtime symptom.

- One enum, explicit values, in one file, with a comment stating that every distinct use gets a
  distinct tag and that reusing one is undetectable at runtime.
- **Uniqueness is a build-time check.** A unit test is not a build-time check and is a stopgap; the
  real detector is an analyser and it belongs to slice 3. Write the stopgap here and the analyser
  there.

### 7. State the overflow posture in code

`adr/0003`'s policy is currently prose. Put it where it is enforced: widths chosen at the type
(task 1), `checked` scoped to one library (task 2), ratios-only made structural (task 1), and a
comment at each of the two wrapping exceptions saying which exception it is.

**Overflow is the one class of bug this project's own oracle cannot see** — both runs wrap
identically, the hashes agree, replay succeeds, and the city is wrong. Determinism does not protect
against overflow; it makes overflow *reproducible*, which is not the same thing. That is why this is
a policy expressed in types rather than a test.

---

## Acceptance

- `dotnet test` green. `dotnet build src/Borough.Headless` builds with no Godot present.
- `Tiles × Tiles`, `Ticks + Tiles` and `SubTiles × SubTiles` each **fail to compile** — asserted by a
  negative-compilation test, not by a comment claiming it.
- Typed quantities allocate nothing and produce codegen identical to the raw integer, shown by a
  benchmark rather than asserted.
- `draw()` matches its committed known-answer vectors, and the independent second implementation
  agrees bit for bit.
- `exp`/`log` are monotonic across the domain and within the stated error bound.
- No `float` or `double` token exists anywhere in `Borough.Core` — by the slice-0 reflection guard
  until slice 3's analyser replaces it.

## Decisions owed by this slice

~~**The `exp`/`log` table resolution is unratified and this slice cannot finish without a number.**~~
**SETTLED in [`adr/0038`](../docs/adr/0038-the-transcendental-tables-are-sized-by-the-representation.md)
— 256 entries per table, rounded linear interpolation, base-2 range reduction.** It did not need a
provisional figure after all, because the question had a stopping rule rather than a range: *the table
must not be the thing limiting the answer*, and one entry count satisfies that where 128 does not and
512 over-buys. It is hash-bearing and world-creation-fixed as expected.

**Two things came out of settling it, both in [`0002`](0002-open-questions.md).** `adr/0003`'s owed
validation was two debts filed as one, and the half that needs no running city — a differential test
against a double-precision oracle on selection probabilities — was runnable from the day the ADR was
written. And the choice model has a **hard horizon at ~11.1 utility units that moves as `1/μ`**, which
means `02 §5.4`'s *free design knob* also decides where options stop existing.

Second, smaller: **whether `Ratio` and `SubTiles` should be one type or two.** They share a
representation and differ in what may be done to them, which is the argument for two. Recorded here
because it will look like duplication to a future reader and the reason should be findable.

**Third, and it was not owed by this slice so much as found by it: `adr/0003`'s normative `draw()` had
a structural defect, and writing the first known-answer vectors is what exposed it.** The opening round
was `mix(seed + GOLDEN + entity)` — two externally chosen coordinates added together, so only their sum
reached the hash. `draw(seed=1000, entity=1)` equalled `draw(seed=1001, entity=0)` bit for bit, at every
Tick and for every purpose: **rerolling the world seed produced the same world shifted by one entity.**
The ADR is amended, the seed now has its own round, and the round is loop-invariant so the hot path is
unchanged — see `WorldKey`. **It cost nothing because no Input Log, State Hash baseline or save exists
yet**, which is the entire argument for `adr/0003`'s instruction that this be the first thing built.

**The independence property task 5 names is not tested and remains owed.** The stated property is that
*two people can implement this from the document and get the same city*; both implementations in
`RandomnessTests` were written by one author in one sitting, so a misreading of the ADR would appear in
both. What the second implementation does close is narrower and worth having: it reduces mod 2⁶⁴
explicitly in `BigInteger`, so it does not inherit C#'s `unchecked` semantics and would catch a wrapping
error, which is the most likely way to get this function wrong. Closing the real property needs a reader
who has not seen this code.

## What this slice deliberately does not do

No tables, no entities, no Tick, no `step()`. No Ruleset parsing. No conservation invariants — those
are the money-overflow detector and they need money to exist and a headless suite to run in, which
are slices 4 and 5. The temptation here is to build "just enough of a world to test the arithmetic
against"; the arithmetic tests better without one.
