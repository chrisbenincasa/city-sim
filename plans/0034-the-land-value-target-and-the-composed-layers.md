# 0034 — The land value target and the composed Layers

`06` milestone **9**. The producer for a structure that has been in the tree since slice 6.

---

## Status

🟡 **SCOPED 2026-08-20, unstarted. Decisions 1 and 2 of six are SETTLED** —
[`adr/0122`](../docs/adr/0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md)
deletes `w₁`, and [`adr/0123`](../docs/adr/0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md)
settles the composition at **two terms, `− w₂·pollution − w₃·noise`**. **Decisions 3 and 4 are SETTLED too** — [`adr/0124`](../docs/adr/0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md) places Fertility, Sealing's decay,
Woodland, replanting and Water Bodies on milestone **24**, and **task 8 is DONE ahead of the rest of the
milestone**, because the amendment *is* their deliverable. **Decision 5 is SETTLED too** — [`adr/0125`](../docs/adr/0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md) gives each weight a
reachable **floor** and an **owed** real ratifier, because nothing in the city reads land value.
**One decision remains: 6, the ±1 oscillation**, and it is *measurable then arguable*, so it wants a run
rather than a sitting.

✅ **The formula went from five terms to two, and the milestone got smaller both times.** Decision 1
asked whether the composition was well-formed at all and the answer was that it was not; decision 2
found that *absent in the world* and *absent in the build* are different absences and only **one** term
is the second kind. **Two weights to author rather than five**, which is what decision 5 has to find
ratifiers for. ⚠ **What ships is bounded above by zero** — a **disamenity field**, because amenity is the
only positive term and it belongs to milestone 15.

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

### 2. What does this milestone compose, when two of four inputs are unbuilt? ✅ **SETTLED 2026-08-20 — two terms, and the caveat gets a test.** Typed *arguable*

> ✅ **Settled with the user in the room, 2026-08-20:
> [`adr/0123`](../docs/adr/0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md).**
> **Shape (b), and the composition is `− w₂·pollution − w₃·noise`.** Noise is built here; **amenity and
> shoreline are absent from the formula rather than defaulted**; `MapLayers.Desirability` keeps its name
> and stops throwing.
>
> ⚠ **The framing below is superseded on its central point, and by a fact read out of `src/`.** This
> section lists the unbuilt terms together. They **split**, and the split decides the milestone:
> ***absent in the world and absent in the build are not the same absence.*** Noise where nobody drives
> is zero **and zero is true** — [`adr/0098`](../docs/adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md)
> already says a city before the motor car is a legitimate city. Shoreline is zero and **true of every
> world that exists**: nothing in `src/` mentions a Water Body at all. **Only amenity is a false zero**,
> so shape (a)'s objection reaches exactly one term and the decision was only ever about that one.
>
> ⚠ **And amenity is the only *positive* term**, so what ships is **bounded above by zero** — a
> **disamenity field**, whose maximum is clean empty ground far from everything. That is not a hole that
> fails loudly; it is a working mechanism that says something false, and it is the opposite failure from
> the one the named-hole discipline was built for. It ships because **the producer is the milestone** and
> holding `Desirability` closed means shipping no caller at all.
>
> ⚠ **A Ruleset base value to restore an upside was offered and refused**: a number whose only job is to
> stand in for an absent term can be ratified against nothing, and
> [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
> asks for a machine, a world and a quantity.
>
> ✅ **The caveat gets a mechanical check, not just prose** — a corpus test refusing any document to say
> desirability is composed without naming amenity's absence, extending the **disqualifier registry**'s
> idiom from a figure to a claim. **Owed by task 2 and written by it**, never before: today no document
> makes the claim, so the test would pass vacuously, and ***a vacuously-passing obligation is an unread
> one***.

⚠ **Four inputs rather than five, since decision 1 deleted `w₁·land_value`.** Of them: **pollution
ships**; **noise** is unbuilt and, since 5a, buildable; **amenity** is unbuilt and is *the count of
distinct Business types reachable on foot* ([`CONTEXT.md`](../CONTEXT.md) → Amenity), placed at milestone
**15**; and **shoreline** needs Water Bodies, which nothing has built.

⚠ **This paragraph said amenity *"needs Businesses and the Provider List"* and both of those exist.**
Businesses shipped with milestone **10** — [`06`](../docs/06-roadmap.md)'s own note says it *"builds the
entity and its balance and nothing else of it"* — and `BusinessTable` holds exactly `building`, `balance`
and `building_next`. What is missing is that **a Business has no kind**, so *distinct Business types* has
nothing to be distinct in. One column and a walkable catchment query, both milestone 15's. ***A blocker
named by listing what a mechanism needs is not the same as the one found by opening the table.*** So this decision is now *two of four*, and shape (c)'s cost fell with
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

### 3. Does Fertility close in this milestone? ✅ **SETTLED 2026-08-20 — no, and it is placed rather than deferred.** Typed *arguable*

> ✅ **Settled with the user in the room, 2026-08-20: [`adr/0124`](../docs/adr/0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md).** Fertility, Sealing's decay,
> Woodland and replanting move to milestone **24**, which **absorbs** them rather than a new milestone
> being created. ⚠ **`06`'s note on 24 — *it depends on nothing in the spine and nothing in the spine
> depends on it* — was made FALSE by this move and is rewritten in the same edit**, because ***a row that
> absorbs work absorbs the sentences that were true while it was empty***, and leaving it would be
> `plans/0012` **Cause 1** created deliberately.
>
> ⚠ **The sitting found an artefact 24 owes that no document named**: terrain suitability **baked at
> world creation into a stored per-Cell column**. [`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)'s
> own table says terrain **does** *feed land value and desirability* and **does not** *get read by any
> Tick phase*, with the rule stated outright — *if a terrain value is read inside a Tick phase, something
> has gone wrong*. Fertility is composed **at the point of use**, inside a Tick. Both cells are correct
> and they constrain the implementation **jointly**, which neither says, so a Fertility written the
> obvious way breaks the checkable rule on the day it is written.
>
> ⚠ **And Sealing's decay is blocked twice.** `sealing_decay_tau = 0` is a stated absence the Ruleset
> headers explain — **and `MapLayers.Step` never calls `DecaySealing` at all.** `MapLayers`' own
> doc-comment says *"not scheduled"* and is accurate, so this is **not** a wrong description and not
> [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md);
> it is a fact living in one place and needed in another. Scheduling it is **not** a freebie: Sealing has
> no cadence, and a Layer cadence is a hash-bearing world-creation number owing a ratifier
> ([`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md),
> [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)),
> ratifiable only against a world with varied terrain. ⚠ **No Ruleset is edited** — `congested.toml` and
> `scarce.toml` are golden baseline artefacts whose **comments move hashes**, so the fact was routed to
> `06`'s milestone 24 row instead. ***Where a fact can go to a document or to a hashed artefact, it goes
> to the document.***

`06`'s milestone 9 row says *"the Fertility and Desirability holes beside it close in the same row"*.
**Fertility cannot close here.** It is `terrain suitability − Sealing − pollution`; Sealing and
pollution ship, and terrain suitability needs the world generator, which `06`'s own inventory places at
milestone **24** ([`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)).
The same absence is why `DecaySealing` ships unscheduled: its rate is *keyed by terrain type*.

**Recommendation: Fertility, Sealing's decay key, Woodland and replanting all move to the generator's
milestone, by a written amendment to `06` — not by being quietly left out of this one.** The
amendment is the deliverable; ***a task dropped without a document is a task nobody will find again***.

### 4. Do Water Bodies and the shoreline source ship here? ✅ **SETTLED 2026-08-20 — no, and 15 was never available.** Typed *arguable*

> ✅ **Settled with the user in the room, 2026-08-20: [`adr/0124`](../docs/adr/0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md).** Water Bodies, the water graph
> and the shoreline line source go to milestone **24** with the rest.
>
> ⚠ **The recommendation below offers 15 *or* 24 and only one of those existed.**
> [`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)
> generates terrain from the world seed and states that **water is immutable** — nothing places water by
> hand, by Ruleset or by player verb — so a Water Body has **exactly one possible producer** and it is
> the generator. ***A row with nowhere else to be is not deferred; it is placed, and the placement was
> already determined by a decision nobody re-read.***
>
> ⚠ **Consequence worth carrying**: desirability gains amenity at **15** and shoreline at **24**, so the
> composition is not complete until the last milestone in `06`'s table, and the two halves are far apart.

`w₅·shoreline` is the fifth term, and `CONTEXT.md` → Water Body already says what it does: *a Water
Body's effect on land is a shoreline line source whose intensity is the Bin's level*, so a fouled beach
degrades adjacent land value **and removes a walkable Amenity destination**. That second half is
milestone 15's, which is where amenity is.

**Recommendation: no.** It is a water graph, a Bin per Water Body, a line-source query and a
generator that places water — a milestone's worth of work wearing an inventory row, and it lands
naturally beside Amenity at 15 or beside the generator at 24. Defer with the row rewritten, per
decision 3's form.

### 5. What ratifies each weight — a machine, a **world**, and a **quantity**? ✅ **SETTLED 2026-08-20 — a floor and a debt, two §D1 entries each.** Typed *measurable*

> ✅ **Settled with the user in the room, 2026-08-20: [`adr/0125`](../docs/adr/0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md).**
>
> ⚠ **The decisive fact was read out of `src/`: nothing in the city reads land value.** Outside
> `MapLayers`, the only readers of `LayerCellTable.LandValue` are the headless **layer dump** — a picture
> — and `RulesetLoader` resolving the layer's name. So **the quantity that would refute an absolute
> scale does not exist**, because every quantity of that kind is produced by a consumer and every
> consumer is unbuilt. ***A ratifier that needs a consumer nobody built is not reachable***, and writing
> one anyway repeats **milestone 7 task 8** one milestone after it happened — a ratifier that named all
> three correctly and was still unreachable, because ***nothing in `adr/0052`'s checklist asks whether
> the named state can occur***. This time it is visible before the number is written.
>
> **The floor** — reachable inside this milestone. *Machine*: the acceptance run (task 7). *World*:
> `rulesets/congested.toml`, because [`adr/0123`](../docs/adr/0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md)
> established only it and `scarce.toml` have any driver, and **a floor run on a world where one term
> cannot vary is not a floor**. *Quantity*: that the field varies at all; that **both** terms are visible
> in it; and the **pollution/noise correlation across Cells**.
>
> **The debt** — the real ratifier, **owed**, triggered by **milestone 13, the price surface**, the first
> named consumer (`02 §5.6`'s initial prices). ⚠ **The weights are unratified from 9 until 13**, and
> `06`'s dependency graph forces 13's position, so no re-ordering shortens it.
>
> ⚠ **The two entries must not be read as one.** The floor can **refute** a weight and can never confirm
> one — ***a reachable check standing in the place of an unreachable one is how a number comes to look
> settled***.
>
> ⚠ **The fifth-sighting check is in the floor deliberately.** Industry and roads are both placed in
> proportion to the same population, so pollution and noise may co-vary; if they do, **no ratio is
> identifiable** and what is owed is a **hand-authored world** rather than a different number. It is the
> pre-flight the shed radius never got.
>
> ⚠ **And `w₃` is not merely unratified — it is not yet meaningful.** Noise is unbuilt, so **its output
> units are task 1's free choice**, and `w₃` absorbs them exactly: only the product `w₃·noise` is
> constrained. **`w₃` is chosen after task 1, never before.** `w₂` does not share this — pollution is
> already stored pre-normalised in kernel units.

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

**Two terms** under [`adr/0123`](../docs/adr/0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md)
— `− w₂·pollution − w₃·noise` — with amenity and shoreline **named in the method** and not defaulted to
zero. Derived at the point of use and **never stored**: a stored desirability Layer would need
invalidating whenever any input changed, and would drift.

**Definition of done for this task, and the second item is not optional:**

1. `MapLayers.Desirability` returns rather than throwing, and its doc-comment names both absent terms and
   says the field is bounded above by zero.
2. ✅ **The corpus check `adr/0123` schedules** — a test in `tests/Borough.Tests/Corpus/` refusing any
   document to claim desirability is composed or built without also naming amenity's absence. It is
   written **here**, by the task that makes the claim true, because before this task it passes vacuously.
   ⚠ **It is deleted at milestone 15**, not kept: a check policing an absence outlives its subject the
   day the absence ends.
3. `02 §2.4` carries the shortfall in prose, and so does the acceptance run's own output.

⚠ **`Fertility` still throws** and is untouched — its blocker is the world generator at milestone 24.

### Task 3 — the weights reach a shipped Ruleset

New `[layers]` keys, hot-reloadable, **no `const` anywhere in simulation source**.

⚠ **`w₃` cannot be written before task 1** — noise's output units are task 1's choice and `w₃` absorbs
them ([`adr/0125`](../docs/adr/0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md)).

**Definition of done — each weight gets TWO §D1 entries on the day it is written, and one is not enough:**

1. **The floor.** *Machine*: task 7's acceptance run. *World*: `rulesets/congested.toml` — **not**
   `minimal.toml`, which has no driver and therefore no noise. *Quantity*: the field varies across Cells;
   **both** terms are visible in it; and the pollution/noise correlation across Cells.
2. **The debt.** The real ratifier, marked **owed and unreachable until a consumer exists**, trigger
   named as **milestone 13, the price surface** (`02 §5.6`'s initial prices).
3. Both entries say in their own text that the floor **refutes and never confirms**, so neither can be
   read as the other.

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

✅ **This run is also decision 5's *floor* machine** ([`adr/0125`](../docs/adr/0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md)), so it carries three more
readings and they are not optional:

1. **The field varies.** A `w₂` small enough rounds every Cell onto the same value, and a uniform field
   is visibly working while carrying no information.
2. **Both terms are visible.** If `w₃·noise` is negligible beside `w₂·pollution` everywhere, this is a
   one-term field wearing a two-term formula — [`adr/0123`](../docs/adr/0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md)'s
   concern arriving as a number instead of as an absence.
3. **The pollution/noise correlation across Cells** — the fifth-sighting check. High correlation means
   **no ratio is identifiable**, and what is owed is a hand-authored world rather than a better number.

⚠ **On `rulesets/congested.toml`, not `minimal.toml`** — nobody drives in `minimal.toml`, so noise is
identically zero and readings 2 and 3 are unreadable there. ⚠ **The floor refutes and never confirms.**

### Task 8 — Fertility, Sealing's decay, Woodland, Water Bodies — the written deferral

✅ **DONE 2026-08-20, ahead of the rest of the milestone**, because decisions 3 and 4 settled together and
the amendment **is** their deliverable — [`adr/0124`](../docs/adr/0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md). `06`'s milestone 9 row, its two inventory rows
and **its milestone 24 row** are rewritten; nothing is deleted. ⚠ **The fourth edit was not in this
task's scope as written**: absorbing five rows into 24 falsified 24's own *depends on nothing* note, and
***a task that moves work moves the sentences that were true before it***.

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
