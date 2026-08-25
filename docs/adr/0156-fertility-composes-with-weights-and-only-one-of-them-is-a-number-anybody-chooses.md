# Fertility composes with weights, and only one of them is a number anybody chooses

**Fertility is `base fertility − w_s·Sealing − w_p·pollution`, in Q16.16, weighted the way
`MapLayers.Desirability` already is.** **Base Fertility is a fraction with `1.0` meaning fully fertile**,
so Fertility is a proportion by construction. 🔴 **`w_s` is DERIVED and is not a Ruleset key**: it is
pinned by the endpoint — a Cell whose every Tile is built on has no farmland — so the Sealing term is
`base × Sealing / 1024` and nobody chooses it. **Only `w_p` is chosen**, and it owes one
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) row rather than
two. **Fertility may go negative and saturates rather than throwing.**

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `LEGIBLE CAUSE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
The scale of a representation and the disposition of a coefficient are settled by argument; the value of
`w_p` is measurable and is filed rather than settled.

## Why

### The bare subtraction was never an implementation, and the units say so by how far apart they are

`CONTEXT.md`, `02 §2.3`, `04 §1` and [`0022`](0022-land-is-a-stock-the-city-spends.md) all state
Fertility as an unweighted subtraction of three quantities:

| Term | What it is | Range |
|---|---|---|
| Base Fertility | Ruleset data keyed by terrain type ([`0155`](0155-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)) | undefined until this ADR |
| Sealing | a **count of Tiles** | 0–1024 per Cell |
| pollution | a **stock in kernel units** | *"about 12 … under a strong source"*, measured, `DesirabilityWeights.Default` |

⚠ **Unweighted, Sealing outweighs pollution by roughly 85 to 1 — and that ratio is an artefact of the
units, not a statement about cities.** A Cell's whole pollution load would move Fertility about as much
as twelve built Tiles out of a thousand. ***A formula whose term magnitudes are set by the
representation is not a design that happens to be badly tuned; it is not a design.***

### The precedent exists, is tested, and its comment states the arithmetic

`MapLayers.Desirability` (`Space/MapLayers.cs:637`) composes `− w₂·pollution − w₃·noise` from a
`DesirabilityWeights` record of Q16.16 ratios, and the comment at `:648` states the rule this ADR
reuses: *"pollution is a count and the weight is a ratio, so the product is already Q16.16 and the count
is never lifted into it."* It also carries the overflow lesson — lifting the count into Q16.16 first
throws on a world `Invariant.LayerMagnitudeIsBounded` calls legal.

**Reusing the shape rather than inventing a second one** is [`0018`](0018-prefer-off-the-shelf-infrastructure.md)'s
discipline applied inside the codebase, and it means Fertility inherits a tested multiplication rather
than a new one.

### `w_s` is pinned by an endpoint, so it is derived and not chosen

`CONTEXT.md` → Sealing: Sealing is *"the count of Tiles in a Cell ever built on"*, and *"roads Seal, and
so does every other built Tile."* **A Cell at Sealing = 1024 has every Tile built on and therefore no
farmland at all.** That is definitional rather than tuning: it fixes the Sealing term at exactly Base
Fertility when the Cell is full, which pins the coefficient.

So the term is `base × Sealing / 1024` — one multiply and one shift, with `1024` already a design
constant (`CellGrid.TilesInCell`) rather than a new one. **There is no `[terrain] sealing_weight` key and
that absence is the decision.**

⚠ **This is [`0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)'s
shape**: derive what an endpoint or a duration already fixes, and author only the residue. It takes the
decision from **two** unratified hash-bearing numbers to **one**. ***A coefficient with an endpoint is
not a tuning knob, and offering it as one invites a Ruleset to state that a fully paved Cell still
farms.***

### Base Fertility is a fraction, and that is what makes `adr/0022`'s Evidence specimen producible

[`0022`](0022-land-is-a-stock-the-city-spends.md) names the readout the whole mechanism is for:

> *"a farm's panel must decompose its own yield — **"41% — ground sealed 12%, pollution from Eastfield
> Industrial 47%"** — naming specific sources. This is the difference between a deep mechanic and an
> inexplicable one."* `LEGIBLE CAUSE`

**With `1.0` meaning fully fertile, that sentence falls out of the arithmetic with no extra machinery**:
Fertility *is* the percentage, and each subtracted term is already the percentage that term cost. Choose
any other scale and the panel needs a conversion nobody has specified, against a denominator nobody has
named.

***The scale was decided by the readout rather than by the storage***, which is the right way round and
is not how the other three Layer quantities were chosen.

### Negative is kept, and clamping would delete the recovery gradient

Fertility could clamp at zero — agricultural capacity below nothing is meaningless. **It is not clamped**,
for a reason that is specific to this quantity: **Sealing decays**, so a dead Cell is on its way back, and
`base − 1.4·base` and `base − 3·base` are two Cells at very different distances from farming again. Clamp
and they are the same number.

⚠ **The decomposition is not the reason** — Evidence reads the *terms*, so clamping would not hurt the
panel. It is the **ordering between exhausted Cells** that is lost, and that ordering is what
[`0022`](0022-land-is-a-stock-the-city-spends.md)'s whole cyclical land-use arc runs on. A consumer that
wants *"is there a farm here"* takes `≤ 0` and loses nothing.

### It saturates rather than throwing, on the precedent's own reasoning

`Desirability` clamps to `int` bounds rather than raising, and `LineSourceQueries.Saturate` states why:
***a read-only query must not throw on a world somebody is allowed to build***, and the instrument that
catches a world gone mad is `Invariant.LayerMagnitudeIsBounded` at end of run — a better place for it
than an exception wherever somebody happened to read a Cell. Fertility is the same kind of query and
takes the same answer.

## Consequences

- **`MapLayers.Fertility` gains a weights parameter**, as `Desirability` has one. The Ruleset carries
  `[terrain] base_fertility_percent` keyed by type and one pollution weight; **it carries no Sealing
  weight.**
  - ⚠ **AMENDED 2026-08-23 by the build, and both spellings moved.** Base Fertility is
    **`[[terrain]] base_fertility_percent`** — an *array of tables*, one per type, all five required
    ([`0158`](0158-terrain-is-five-types-and-base-fertility-varies-across-them-because-a-category-exclusion-is-not-an-overlay.md)).
    The pollution weight is **`[layers] fertility_pollution_percent`**, beside its sibling
    `desirability_pollution_percent`, because it weights a *Layer* and is one number for the world
    rather than one per ground type. ***The decision is unchanged; only where each number is written
    down moved.***
- **One `plans/0002` §D1 row is opened, not two** — the pollution weight. **Named ratifier: milestone
  24's own long run on a world with varied terrain**, and the refuting reading is stated in both
  directions — a farm beside heavy industry that yields unchanged means it is too low, and a farm that
  dies from a neighbour's plume alone means it is too high.
- ⚠ **Base Fertility's own value stays keyed by terrain type and uniform in the shipped Ruleset**
  ([`0155`](0155-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)).
  Expressing it as a percentage does not make varying it free of
  [`0022`](0022-land-is-a-stock-the-city-spends.md)'s amendment.
- **`CONTEXT.md` → Fertility and `02 §2.3` keep the three-term shape and gain the weights**, so the
  formula in the corpus stops being an expression that cannot be evaluated. ✅ **Discharged
  2026-08-23**, and both now also name the symbol rather than the milestone
  ([`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).
- **The `01 §6` / `04` yield readout is reachable at milestone 24** rather than owing a scale decision
  later.

## What would trigger revisiting

- **A Cell is found that should farm while fully sealed** — a rooftop or vertical agriculture mechanism.
  That breaks `w_s`'s endpoint and the coefficient becomes a chosen number with a key, which is a design
  change and not a retune.
- **Sealing stops being a count of Tiles.** The derivation is `Sealing / CellGrid.TilesInCell`; if
  Sealing ever becomes a fraction or a weighted measure, the divisor moves with it.
- **A consumer needs Fertility clamped and cannot clamp for itself** — then the clamp belongs at the
  producer after all, and the recovery ordering needs somewhere else to live.
- **The two terms are found not to be independent.** They are subtracted, so the model assumes sealing
  and pollution damage fertility separately; if a mechanism arrives where paved ground changes how
  pollution settles, the composition is wrong in shape and not in weight.
