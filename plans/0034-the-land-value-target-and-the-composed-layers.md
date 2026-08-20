# 0034 — The land value target and the composed Layers

`06` milestone **9**. The producer for a structure that has been in the tree since slice 6.

---

## Status

🟡 **SCOPED 2026-08-20, unstarted. Decision 1 of six is SETTLED** — `w₁` is deleted,
[`adr/0122`](../docs/adr/0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md).
**Five remain, four of them before the task that composes anything**, because between them they decide
whether the composition this milestone exists to write is arithmetically well-formed at all. ✅ The
first one was the one that decided whether it was well-formed *at all*, and the answer was that it was
not.

⚠ **This milestone had no row anywhere until the day it was scoped.** Not in
[`0000`](0000-board.md)'s ranked table, not in its per-milestone gate table, not in
[`0002`](0002-open-questions.md). Milestones **6**, **7**, **8** and **10** were scoped, built and
closed around it, and **10 is behind it in [`06`](../docs/06-roadmap.md)'s own order and shipped
anyway**. ***A milestone with no row is not a milestone with no gate*** — the answer here happens to be
that no document names a gate on it, but that answer was arrived at by `grep` on the day of scoping and
not by anybody assessing it. The per-milestone table that lists the cleared milestones too is what
should have made the absence visible; it did not, because **a table of the milestones somebody thought
about is not a table of the milestones**.

⚠ **The plan number was claimed by reading `plans/`, not `git log --all`.** Milestone 7's sitting took
two numbers off `git log --all` and lost both within the day, because that command sees refs and the
branches behind them go on committing. `0034` is free in this tree at the time of writing and that is
the strongest claim available.

---

## Why this milestone exists, in one paragraph

`MapLayers` has held a land value column, a momentum operator that moves it toward a target, and a
cadence to run that operator on, since slice 6. **Nothing has ever set the target.** The column
therefore converges to zero and stays there, which the build says out loud in
`MapLayers.DriftLandValue`'s own doc-comment — and `MapLayers.Desirability`, the thing that would
compute the target, **throws** rather than returning a number, on the reasoning that a placeholder
returning zero is a value that will be read, believed and tuned around. So this milestone is a
**producer for a structure already in the tree** rather than a subsystem: the storage, the operator,
the schedule, the double buffer and the end-of-run bound are all built and exercised, and what is
missing is an argument to one method.

---

## The named risk

`06`: ***that six decisions read a field nothing sets*** ([`02 §2.4`](../docs/02-simulation-model.md)).
The six it names are [`adr/0091`](../docs/adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md)'s
compulsory-purchase price, `02 §5.9`'s contagion damper, `02 §5.6`'s initial prices, `04 §7`'s
gentrification damper, `01 §6`'s recovery path, and `05 §9`'s hash-bearing `tick % 256 == 16` offset.

⚠ **The risk is real and the count should not be quoted as six live ones.** The sixth — `05 §9`'s
offset — is the land value **cadence**, which shipped in 3c and is exercised every run; it reads the
field's *schedule* rather than its *value*. Quote the risk as `06` states it and count the live
consumers when one of them is actually built.

---

## What the build already holds — surveyed 2026-08-20

Read from `src/`, not from any description of it.

| Thing | Where | State |
|---|---|---|
| The land value column, saved and hashed | `LayerCellTable` | ✅ ships |
| `MapLayers.DriftLandValue` — the momentum, a first-order integer lag | `MapLayers.cs:280` | ✅ ships, runs on cadence, **converges to zero because the target is never set** |
| `MapLayers.SetLandValueTarget` — the landing site | `MapLayers.cs:319` | ✅ ships. **Callers: `LayerFieldsTests`, `LayerQueryTests`, `FactorioTests`. None in `src/`** |
| The land value cadence, `tick % 256 == 16` | `LayerSchedule` | ✅ ships, hash-bearing, `adr/0044` |
| `MapLayers.Desirability` | `MapLayers.cs:550` | 🔴 **throws.** Three of **four** inputs do not exist, post-[`adr/0122`](../docs/adr/0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md) |
| `MapLayers.Fertility` | `MapLayers.cs:523` | 🔴 **throws.** Terrain suitability does not exist |
| Sealing — `Seal`, `Sealing` | `MapLayers.cs:338`, `:349` | ✅ ships, clamped at the write site |
| `MapLayers.DecaySealing` | `MapLayers.cs:361` | ⚠ **ships and is never scheduled**, because its rate is keyed by terrain and there is no terrain to key it by |
| Industrial pollution — source, diffusion, decay | `MapLayers`, `LayerDiffusion` | ✅ ships |
| Noise, near-road pollution | — | 🔴 **nothing.** `02 §2.4` makes both point-of-use queries rather than Layers |
| Amenity — the walkable catchment | — | 🔴 **nothing** |
| Water Bodies, the shoreline source | — | 🔴 **nothing.** No `WaterBody` symbol exists in `src/` |
| Woodland, replanting | — | 🔴 **nothing** |
| Desirability weights `w₁`–`w₅` | `LayerRates`, `LayerConstants` | 🔴 **no keys.** `LayerRates` holds `LandValueTau`, `SealingDecayTau`, `PollutionTau` and nothing else |
| `--layer` — the field dump | `Borough.Headless/LayerDump.cs` | ✅ ships. Prints a Layer's Cell grid and the halo that moved |

---

## What this milestone is, and what closes it

⚠ **`06`'s one-line row understates it by four inventory rows.** The row reads *the land value target
and the composed Layers*, and the same document's inventory, two tables further down, places **four
separate mechanisms** at milestone 9:

| Inventory row | Owner | What it needs |
|---|---|---|
| The land value Layer and the desirability composition | `02 §2.4` | Noise, Amenity, shoreline |
| Sealing, composed Fertility, Woodland, replanting | [`adr/0022`](../docs/adr/0022-land-is-a-stock-the-city-spends.md), [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md) | **Terrain suitability**, which is the world generator |
| Water Bodies as Bins on the water graph | [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md) | A water graph |
| Point-of-use noise and near-road pollution **queries** | [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md) | The Road Graph, which **shipped in 5a** |

**What closes it is the land value target being set by something inside `src/`, on the cadence, from a
composition that does not throw.** Everything else in the table above is either an input to that or a
neighbour that got filed here because it is also about land. Decisions 2 and 3 below decide how much of
the neighbourhood ships with it, and the recommendation in each is *less than `06` says*.

---

## Open decisions this milestone owes, before the task that needs them

**Five of the six are owed before any task that composes.** They are not independent: decision 1 can
delete a term, decision 2 can defer two, and what is left is what decision 5 has to find ratifiers for.

### 1. Is `w₁` a weight at all? ✅ **SETTLED 2026-08-20 — it is not, and it is deleted.** Typed *arguable*

> ✅ **Settled with the user in the room, 2026-08-20:
> [`adr/0122`](../docs/adr/0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md).**
> `w₁·land_value` is **deleted**. The composition is `− w₂·pollution − w₃·noise + w₄·amenity −
> w₅·shoreline`; the momentum operator supplies the persistence `w₁` looked like it was for. The
> question put was *accident of notation, or prestige feedback somebody meant to build?* and the answer
> was **accident**. Prestige feedback is not refused — it is **undesigned** under
> [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md), and if it returns
> it returns as its own mechanism with a stated gain and a bound, never as the first item in a list of
> five weights. ⚠ **No bound and no load-time refusal are written**, because policing a deleted term is
> machinery with no subject. Three copies of the formula amended: `02 §2.4`, `plans/0009` §7 and
> `MapLayers.Desirability`'s doc-comment.

`02 §2.4` says two things which have never been read together:

> Desirability is `w₁·land_value − w₂·pollution − w₃·noise + w₄·amenity − w₅·shoreline`

> Land value … moves slowly toward the current desirability

**Land value drifts toward a target whose first term is land value.** Substituting the drift into the
composition, with `X = w₂·pollution + w₃·noise − w₄·amenity + w₅·shoreline`:

```
gap  = desirability − land_value = (w₁ − 1)·land_value − X
LV*  = X / (w₁ − 1) = (w₄·amenity − w₂·pollution − w₃·noise − w₅·shoreline) / (1 − w₁)
```

So **`w₁` is a gain of `1/(1 − w₁)` on the other four terms and not a weight among five**, it is stable
only for `w₁ < 1`, and at `w₁ ≥ 1` the field **diverges** — every Cell runs away from every other at a
rate set by the momentum's own time constant. ***A term that appears on both sides of a lag is a gain,
and calling it a weight hides which way the loop runs.***

This is arithmetic and not a measurement, so [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
sends it to argument rather than to a machine. The formula appears in exactly two places in the corpus
— `02 §2.4` and [`0009`](0009-map-layers.md) §7 — and **neither mentions the self-reference**.

**Recommendation: delete `w₁`, and amend `02 §2.4`.** The momentum already supplies the persistence
`w₁` looks like it is there to supply, and one mechanism holding a property is cheaper than two holding
it jointly. If it stays, it stays with the gain stated beside it and a bound `w₁ < 1` that something
refuses at load.

### 2. What does this milestone compose, when two of four inputs are unbuilt? **Owed before the composition task.** Typed *arguable*

⚠ **Four inputs rather than five, since decision 1 deleted `w₁·land_value`.** Of them: **pollution
ships**; **noise** is unbuilt and, since 5a, buildable; **amenity** is unbuilt and is *the count of
distinct Business types reachable on foot* ([`CONTEXT.md`](../CONTEXT.md) → Amenity), which needs
Businesses and the Provider List and is placed at milestone **15**; and **shoreline** needs Water
Bodies, which nothing has built. So this decision is now *two of four*, and shape (c)'s cost fell with
it — holding `Desirability` closed no longer waits on a term that has stopped existing.

Three shapes, and the first is refused rather than merely unattractive:

- **(a) Compose what exists and let the absent terms be zero.** This is the placeholder the whole named-hole
  discipline exists to refuse: *a value that will be read, believed, and tuned around*. And it is worse here
  than usual, because the absent terms are the two that would make land value **vary** — a city composed
  from pollution alone is high everywhere industry is not.
- **(b) Build noise here, defer amenity and shoreline, and compose three terms with the absence stated.**
  Noise is the one unbuilt input whose blocker has already cleared.
- **(c) Hold `Desirability` closed until milestone 15 supplies amenity.** Honest, and it means this
  milestone ships no producer at all — which is the whole milestone.

**Recommendation: (b), with the deferred terms named in the composition's own doc-comment** rather than
defaulted. ⚠ **(b) is a partial composition and must not be recorded as *desirability, built*** — that
is `plans/0000`'s ***a partially-shipped milestone reports as shipped***, which hid the District Pool
from forty inventory rows.

### 3. Does Fertility close in this milestone? **Owed before the task list is fixed.** Typed *arguable*

`06`'s milestone 9 row says *"the Fertility and Desirability holes beside it close in the same row"*.
**Fertility cannot close here.** It is `terrain suitability − Sealing − pollution`; Sealing and
pollution ship, and terrain suitability needs the world generator, which `06`'s own inventory places at
milestone **24** ([`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)).
The same absence is why `DecaySealing` ships unscheduled: its rate is *keyed by terrain type*.

**Recommendation: Fertility, Sealing's decay key, Woodland and replanting all move to the generator's
milestone, by a written amendment to `06` — not by being quietly left out of this one.** The
amendment is the deliverable; ***a task dropped without a document is a task nobody will find again***.

### 4. Do Water Bodies and the shoreline source ship here? **Owed before the task list is fixed.** Typed *arguable*

`w₅·shoreline` is the fifth term, and `CONTEXT.md` → Water Body already says what it does: *a Water
Body's effect on land is a shoreline line source whose intensity is the Bin's level*, so a fouled beach
degrades adjacent land value **and removes a walkable Amenity destination**. That second half is
milestone 15's, which is where amenity is.

**Recommendation: no.** It is a water graph, a Bin per Water Body, a line-source query and a
generator that places water — a milestone's worth of work wearing an inventory row, and it lands
naturally beside Amenity at 15 or beside the generator at 24. Defer with the row rewritten, per
decision 3's form.

### 5. What ratifies each weight — a machine, a **world**, and a **quantity**? **Owed before any weight is written down.** Typed *measurable*

Every surviving weight is **tuning, hash-bearing** Ruleset data and needs a named ratifier on the day it
is written ([`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md),
as amended twice). Under decision 1's recommendation and decision 2's, that is **`w₂` and `w₃`** — two,
not five.

⚠ **The world is the hard half and this corpus has been caught by it four times.** A generated city
sizes its demand and its supply from one population, so *the same number sizes both* — it is why
occupancy was flat at every population for parking, why `v/c` peaked at 0.44 at three city sizes, and
why `foot_crossing_every` is inert. **A land value field composed from pollution and noise over a
generated city is a candidate for the fifth sighting**: industry and roads are both placed by the
generator in proportion to the same population. Name the world before the machine, and expect it to be
a hand-authored Ruleset rather than a sweep over `--citizens`.

### 6. Does the minimum-step-of-one survive a target that moves? **Owed before the producer is scheduled.** Typed *measurable, then arguable*

`MapLayers.Step` removes the integer lag's dead band: when `RoundDiv(gap, tau)` rounds to zero and the
gap is not, it moves by one anyway. Its doc-comment gives the reason, and the reason is sound — without
it a Cell settles up to `tau/2` short of its target *and on whichever side it approached from*, which is
path dependence in stored state, which under `05 §4` is two cities.

⚠ **That argument was made against a target somebody else supplies, where the gap reaches exactly
zero and the operator stops.** Against a target that is a function of land value, the fixed point is
in general **not an integer**, so the gap never reaches zero and every Cell oscillates by ±1 for ever
— in **saved, hashed** state, on a 256-Tick cadence, for the life of the world. ***Two decisions each
correct alone can be incorrect jointly, and the seam is invisible from either side.***

It is measurable first — run the composition and count Cells whose land value is still moving at
steady state — and only then arguable. Decision 1 does not dispose of it: the fixed point is
non-integral whenever the composition is, `w₁` or no `w₁`.

---

## Tasks

⚠ **This list is contingent on decisions 1 to 4** and is written in the shape the recommendations
above imply. If decision 2 goes to (c) there is no milestone here and that is the finding.

### Task 1 — noise and near-road pollution, as point-of-use queries

The one unbuilt desirability input whose blocker has cleared. `02 §2.4` and `adr/0034` have already
decided everything about it except the code:

- A **line source**, 50–300 m, logarithmic falloff, **never stored** and never a Layer. ⚠ Run
  `02 §2.5`'s seven questions before writing anything — *"add a Map Layer" was the reflex answer four
  times running and was the right answer once*.
- It **sums** rather than taking the nearest source, because noise superposes and a nearest-source
  query understates a Lot caught between two busy roads.
- It enumerates **by loudness, not by road class** — every linear source in range whose contribution
  exceeds the ambient background, where the background is the local-Street level the query already
  computes. A crossover rather than an authored threshold, so **nobody authors a number**, and it is
  what catches [`adr/0029`](../docs/adr/0029-transit-is-in-and-right-of-way-is-the-only-axis.md)'s
  Reserved band, which puts Arterial-scale volume on an ordinary grid Street.
- Near-road pollution is the same query with different weights.

⚠ **The doc-comment on `MapLayers.Desirability` says these queries need *"a Road Graph that does not
exist in Phase 1"*. That sentence is stale — the Road Graph shipped in 5a.** Fix it in this task.

### Task 2 — `Desirability` stops throwing, and composes what exists

Three terms under decision 2's recommendation, with the two deferred ones **named in the method** and
not defaulted to zero. Derived at the point of use and **never stored** — a stored desirability Layer
would need invalidating whenever any input changed, and would drift.

### Task 3 — the weights reach a shipped Ruleset

New `[layers]` keys, hot-reloadable, **no `const` anywhere in simulation source**. Each one goes into
[`0002`](0002-open-questions.md) **§D1** on the day it is written, with the ratifier decision 5 named
— a machine, a world and a quantity.

### Task 4 — the producer: something inside `src/` sets the target on the cadence

The milestone. `SetLandValueTarget` gets its first non-test caller, on `tick % 256 == 16`, from
`Desirability`. ⚠ **Every baseline moves here** and that is expected: the land value column stops
being zero, so it stops folding as zero. A hash move gets a commit whose subject explains it, and
under [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
it is never a reason to defer, narrow or split the work.

### Task 5 — the bound, and decision 6's reading

`WorldInvariants` already bounds Layer magnitudes as an overflow guard. This task asks the sharper
question: **at steady state, how many Cells are still moving?** That number is decision 6's, and it is
the difference between a field that has settled and a field that is oscillating quietly inside its
bounds for ever.

### Task 6 — something to look at

`--layer` already prints a Cell grid and the halo that moved. Grow it to print **land value beside
desirability**, so that a reader can see the momentum lagging its target rather than infer it from two
hashes. ⚠ **Every string belongs to the shell** — `Core` hands over Cell coordinates and integers.

### Task 7 — the long acceptance run

100k+ Ticks, **no collection and no magnitude trending upward at steady state**. Land value is the
awkward one: it is a magnitude that is *supposed* to move, so state the exception the way milestone 6
did — assert flatness on the **flow** rather than the level, and say which axis is exempt and why.

### Task 8 — Fertility, Sealing's decay, Woodland, Water Bodies — the written deferral

Decisions 3 and 4's deliverable. `06`'s milestone 9 row and its two inventory rows are rewritten to
name the milestone that can actually close them, with the reason. **Not a deletion.**

---

## What this milestone must not do

- **Do not store desirability.** `02 §2.4`, and the reason is not performance: a stored composite needs
  invalidating whenever any input changes, and drifts.
- **Do not add a Map Layer without running `02 §2.5`.** Noise is the field that nearly broke the
  classification once already.
- **Do not return zero from a hole.** If a term is not built, the composition says so; it does not
  default.
- **Do not build the residential choice model (16), the agglomeration terms (15), or the Provider List
  (14).** Land value is an input to all three and none of them is this row.
- **Do not put a weight in a `const`.** `adr/0015`: a `const` where a Ruleset value belongs is a defect.
- **Do not read `06`'s milestone 9 row as the scope.** It is four inventory rows and it says one.

---

## Definition of done

[`CLAUDE.md`](../CLAUDE.md)'s list, refined per [`0003`](0003-build-plan.md) → *Definition of done*:

- `dotnet build` green with no GPU and no Godot; **the whole unfiltered `dotnet test` green** — the
  milestone gate, which [`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)
  did not move.
- **`MapLayers.SetLandValueTarget` has a caller in `src/`**, and a test that fails if it stops having
  one. ⚠ The risk this milestone retires is *a field nothing sets*, so the acceptance criterion is
  about the **caller**, not about the value.
- **Land value is not uniform** over the acceptance world, and the assertion names the spread rather
  than the mean. A field that is non-zero everywhere and equal everywhere has not retired the risk.
- The long run: no collection growing, and land value's exception **stated on the flow**.
- `--layer` shows land value lagging desirability.
- Every weight that reached a Ruleset has a row in `0002` §D1 with a machine, a world and a quantity.

---

## What scoping found

### F1 — `06`'s milestone 9 row contradicts `06`'s own inventory, two tables down

The row says the Fertility hole closes here. Fertility needs terrain suitability; the same document
places the world generator at **24**. Neither table is wrong on its own and they were written months
apart. ***A document that stores the same commitment in two granularities drifts at the finer one
first***, which is `plans/0012` **Cause 1** inside a single file — and `06` is that audit's control
case, the one large document that came back clean, because it stores no status. **It stores no status
and it does store scope**, and scope drifts the same way.

### F2 — the land value target is a root by an existence condition on the wrong side

`06`'s roots table justifies milestone 9 with *"The Layer, its drift and its cadence shipped in 3c"* —
which is a fact about the **consumer** of the thing this milestone builds, not about its **inputs**.
Its inputs are noise, amenity and a shoreline, and one of them is placed six rows later at 15.

⚠ **This is the third sighting of one shape.** [`0033`](0033-conserved-money-and-the-treasury.md) **F1**
struck the District Pool from the roots table for exactly this reason — it was listed on the strength
of *"needs road connectivity, which shipped in 5a"* — and `06`'s own commentary calls that *an existence
condition*. ***A root is a row with no unbuilt input, and a row whose listed reason names something
already built has not been checked, it has been justified.*** The roots table should be re-derived by
asking each row for its inputs, not re-read.

### F3 — `w₁` is not a weight, and no document has ever said so

Decision 1's algebra. The formula puts land value on both sides of a first-order lag, which makes `w₁`
a gain of `1/(1 − w₁)` on the other four terms and makes the field divergent at `w₁ ≥ 1`. ⚠ **It was
findable at any point since slice 6** — both halves have been in `02 §2.4` in adjacent paragraphs the
whole time — and it was not found, because nobody had to compose anything until now. ***A formula nobody
evaluates is not checked by being read.*** ✅ Settled and deleted the same day,
[`adr/0122`](../docs/adr/0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md).

⚠ **This paragraph said *"two documents state the formula"* and there are THREE.** The third is
`MapLayers.Desirability`'s own doc-comment — the copy a *programmer* reads, and the one that would have
been consulted by whoever wrote the producer. It is **invisible to every check in
`tests/Borough.Tests/Corpus/`**, because those are document-to-document by construction, which
`CLAUDE.md` states as a known blind spot and which this is a worked instance of. ***A count of where a
claim lives, taken by reading the documents, is a count of the documents.*** Corrected on the day the
decision was settled, by a `grep` over `src` and `tests` that the scoping session had not run.

### F4 — the dead-band repair and a moving target are correct alone and wrong together

Decision 6. `Step`'s minimum step of one exists to stop a Cell settling short of its target on
whichever side it approached from, which is path dependence in hashed state. It reaches a clean stop
only because the target holds still. Give the target a term that moves when land value moves and the
fixed point stops being an integer, so the operator never stops. **Neither doc-comment is wrong; the
seam between them is.** `adr/0093` governs a description being wrong about a *trigger* and milestone 6
found one being silent about a *consequence*; this is a third form — **two descriptions, each complete,
whose conjunction nobody wrote down**.

### F5 — the weights need a world, and the generator is the recurring reason they will not get one

Decision 5. Four times now a hash-bearing number has been sent to a generated city for ratification and
come back with a flat reading, because the generator sizes the thing being measured and the thing
measuring it from one population. Pollution and roads are both generator outputs. **Name the world
first this time**, which is what milestone 7 did after 5c did not.

### F6 — a live doc-comment names a blocker that cleared five milestones ago

`MapLayers.Desirability` refuses on the grounds of *"a Road Graph that does not exist in Phase 1"*. It
shipped in 5a. The sentence is right about the symbol and wrong about the trigger, which is exactly
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
case — and ⚠ **no mechanical check in this corpus can see it**, because every one of them compares a
document to another document and this is a doc-comment. Same blind spot that hid the fourteen stale
`milestone N` strings in `src/`.

### F7 — the milestone is two milestones, and the smaller one is the one worth having

Strip the terrain-blocked rows (decision 3) and the water graph (decision 4) and what is left is
**noise, a three-term composition, two weights and a caller** — small, ungated, and it retires the
named risk entire. What is stripped is not small and is not this row's. ⚠ **Recording the strip is the
task, not the stripping** — `06`'s rows get rewritten to name a milestone that can close them, or they
become invisible the way Evidence was when `01 §6` and `00-vision` each named the other.

---

## Where this sits

**Upstream:** nothing. Every input this milestone actually uses — the Road Graph, Segment volume,
industrial pollution, the land value column and its cadence — is in the tree.

**Downstream:** the six consumers `06` names, of which the live ones are `02 §5.6`'s initial prices,
`02 §5.9`'s contagion damper, `04 §7`'s gentrification damper, `01 §6`'s recovery path and
`adr/0091`'s compulsory-purchase price. Milestone **16**, the residential choice model, is the deepest
of them — [`0002`](0002-open-questions.md) already records that *"land value and desirability are named
holes that throw"* as the reason a compactness question cannot be asked.

**Sideways:** milestone **15** owes amenity and milestone **24** owes the generator, and this milestone
hands each of them a term of a composition that is already written and already refuses.
