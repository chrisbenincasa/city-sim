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

### Task 1 — noise and near-road pollution, as point-of-use queries — ✅ **DONE 2026-08-20**

> ✅ **Shipped.** `LineSourceQueries.Noise` and `.NearRoadPollution` compose; `.Amenity` still throws.
> **No Ruleset key and no hash moved** — nothing calls either query yet, and the range and weights go in
> with task 3's so the eight Rulesets take **one** edit rather than two.
>
> ⚠ **The landing site already existed and this document's survey missed it.** `LineSourceQueries.cs`
> shipped in **3c** as a documentation-only stub carrying the two constraints, and **all three** of its
> throw messages said the Road Graph *"does not exist in Phase 1"* — a third stale sentence beside the
> two already found. Its constraints were kept verbatim and the implementation written against them.
>
> ✅ **`02 §2.5`'s seven questions were run and confirmed the classification rather than changing it**:
> line source, superposes, isotropic, no memory, read by a Rule, derived from stored volume → **local
> query**, admitted on *the source set is small or known by construction*.
>
> **Four findings, and three are defects the tests caught rather than things the design said.**
>
> **F8 — the sum's domain was never stated, and both readings are available from the prose.** `02 §2.4`
> says the falloff is logarithmic **and** that the query sums. **Summing log-domain values is wrong** —
> two equal sources are half a bel louder, not twice as loud. Intensities accumulate linearly and
> `Log1P` is applied **once**, which makes the wrong arithmetic unreachable rather than discouraged.
> `Log1P` and not `Log` so **silence returns zero**, which `adr/0123` needs to be an honest reading.
> ***Two correct sentences that constrain the build jointly, and neither says so*** — decision 1's shape,
> a third time.
>
> **F9 — `IntensityPerFlow` is not a scale, and the reasoning that it is one is the trap.** Under a plain
> logarithm it would be. Under `Log1P` it decides **which regime the city sits in**: set too low, every
> intensity lands in the linear stretch, two sources come out *exactly* twice as loud, and the field is
> the linear sum the logarithm was chosen to prevent — **while still looking like a level**. Found
> because the test asserted **sub-linearity** rather than a number; a test asserting a number would have
> been rewritten to match the bug. ⚠ **This makes it a shape parameter, so decision 5's floor reads it
> too.**
>
> **F10 — `Frontage.Locate` looks like the background function and is not.** It answers *which Segment
> does this Address front* and returns nothing unless the Tile lies **exactly on** a lattice line —
> correct for an Address, which is never anywhere else. Used for the ambient background it gave **zero
> everywhere except on the carriageway**, disabling the enumerate-by-loudness crossover **without failing
> anything**. The background is now the nearest local Street, and an off-lattice Street counts while an
> Arterial never does.
>
> **F11 — `RoadFixtures.Chain` is not on the lattice, and it reads as though it is.** Its nodes are 32
> Tiles apart and its Ruleset declares `block_tiles = **512**`, so every Segment it makes is off-lattice.
> Every test written against it exercised the **linear scan**, and the lattice window — the half the
> query exists to be fast in — had no coverage while the file looked thorough. ***A fixture named for a
> shape is not a fixture of that shape.*** Covered now by a local fixture, plus an equivalence assertion
> that both halves of the source set return the **same** answer for the same geometry.
>
> ⚠ **Owed to [`0013`](0013-tick-budget.md) when task 4 lands, not now**: the query walks a
> `ceil(range/block)` window twice plus the off-lattice scan, and task 4 calls it per live Cell every 256
> Ticks. **It is a Tick consumer with a guessed multiplicand and no row yet** — filed on the day the
> caller exists, per [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md).


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

### Task 2 — `Desirability` stops throwing, and composes what exists — ✅ **DONE 2026-08-20**

> ✅ **Shipped**, all three Definition-of-done items, and **no hash moved** — nothing calls it yet.
>
> ⚠ **F12 — the composition is per-Cell and one of its inputs is explicitly sub-Cell, and no document
> had noticed.** Land value is stored per Cell, so `Desirability` took `Cells`; noise's *whole reason
> for not being a Layer* is that its gradient fits inside one Cell. Composing at the Cell would have
> collapsed the sub-Cell term — the ***degrades into is there a road here*** outcome
> [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md) sorted fields by geometry to
> avoid — **and the shipped geometry makes the obvious sample the worst available one**: a Cell is 32
> Tiles and `[roads] block_tiles` is **32**, so Streets run along Cell *edges* and ***a Cell's centre is
> systematically the quietest Tile in it***. A centre sample would have reported the whole city quiet.
>
> ✅ **Settled with the user: the composition works at a TILE.** `Desirability(RoadGraph, weights, Tiles,
> Tiles)` — pollution upsampled from its Cell, noise exact at the Tile. **How a Cell samples it becomes
> an explicit, stated, hash-bearing decision belonging to task 4** rather than something buried in the
> composition. It also serves the Lot- and Address-level consumers, which is what all six of `06`'s named
> ones are.
>
> **Returns Q16.16, not land-value units.** The sum is a weighted count plus a weighted logarithm, and
> neither is in the units the column stores until the weights say so — ***the rounding belongs where the
> value is stored, not where it is computed***, which is also where decision 6's ±1 question will bite.
>
> ✅ **`DesirabilityShortfallTests` is written, and it bites.** A document may say *desirability is
> composed* only if it also names **amenity**. Verified by writing the violation and watching it fire,
> per `CLAUDE.md`'s rule for a diagnostic. It carries **two** tests: the check, and a **vacuity guard**
> asserting something in the corpus actually makes the claim — without it a rewording would leave the
> file green over a corpus it had stopped reading, ***and green is what it looks like when it is
> working***. ⚠ **Its reach is deliberately loose and the file says so**: it asks only that the word
> appear in the document. A proximity rule was available and refused, because it fails on ordinary
> rewording and ***a check that cries wolf is a check somebody deletes***. ⚠ **DELETE IT AT MILESTONE
> 15.**
>
> ⚠ **F13 — a partial composition is exactly what the named-hole discipline does not cover**, now
> recorded in `LayerFieldsTests` itself. That test named five holes and names two. A hole that throws is
> safe because nothing can read it; desirability now returns plausible numbers with its only positive
> term missing, and **no assertion in that file can tell that apart from a finished field**. The property
> is pinned instead by `Desirability_is_never_positive_while_amenity_is_absent` — ⚠ **when that starts
> failing it is amenity arriving, not a regression.**


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

### Task 3 — the weights reach a shipped Ruleset — ✅ **DONE 2026-08-20**

> ✅ **Four `[layers]` keys in all eight Rulesets, and five §D1 entries** — two for `w₂` (a floor and the
> debt), and one each for `w₃`, the range and the intensity. **No `const` in simulation source.**
>
> ⚠ **F14 — decision 5 said *two weights, not five*, and the query brought two more hash-bearing numbers
> with it.** `noise_range_metres` and `noise_intensity_percent` are neither weights nor optional, and
> nothing had counted them. ***A count of the numbers a decision opens, taken before the mechanism is
> built, is a count of the ones the decision could see.***
>
> **Only one of the four has a derivation, and the file says which.** `noise_intensity_percent = 400`
> comes from task 1's **F9**: the level is `log(1+x)`, linear below unity, so this number decides **which
> regime the city sits in**. Measured — at 100 a Street at its **stated capacity** (3,600 Vehicles an hour,
> ≈ 42 a Tick) falls under unity by about **150 m** and its noise adds *linearly*, the arithmetic the
> logarithm exists to prevent; at 400 it stays logarithmic across the whole 300 m. The range is
> `02 §2.4`'s **outer end** of a band six times wide. The two weights are **1:1 and deliberately neutral**
> — measured magnitudes (pollution ≈ 12 in kernel units under a strong source, noise ≈ 3 beside a capacity
> Street) put them within one order of magnitude, so both stay *visible*, which is the only property
> anything can check today.
>
> ⚠ **F15 — *editing a golden Ruleset moves thirty-two committed hashes* is wrong, and it is exactly the
> sentence that makes somebody defer a Ruleset edit.** What moved was a **Ruleset content hash** — a *file
> fingerprint* — in **eight header lines**: three fixture constants, the `ruleset` line in both `.borough`
> logs, and the same line in both traces. ***Not one of the thirty-two State Hash samples moved***, because
> a key nothing reads cannot change the city. ***Saying "moves N committed hashes" of a fingerprint change
> is Cause 5 with the units dropped.*** Corrected in `CLAUDE.md` and in
> [`tests/Borough.Tests/Golden/README.md`](../tests/Borough.Tests/Golden/README.md).
>
> ⚠ **And I nearly deferred this task into task 4 so the baselines would be regenerated once**, which
> [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
> forbids by name — ***never cite hash movement as a reason to defer, narrow or split work.*** The ADR
> caught a live instance of the thing it was written for, and the cost it was guarding against turned out
> to be eight lines.

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

### Task 4 — the producer: something inside `src/` sets the target on the cadence — ✅ **DONE 2026-08-20**

> ✅ **`MapLayers.SetLandValueTargets` runs from phase 5 on `tick % 256 == 16`, retargeting before it
> drifts**, and [`adr/0126`](../docs/adr/0126-a-cell-samples-desirability-at-its-quadrant-centres-and-a-line-sources-area-mean-does-not-converge.md)
> records the Cell-sampling decision F12 sent here. The named hole
> `Land_value_is_zero_everywhere_until_something_computes_desirability` was **inverted rather than
> deleted** — its own remark asked for exactly that — so the hole leaves a test behind instead of a gap.
>
> ⚠ **F16 — the derivation this task was opened to write down was REFUTED by the measurement, and that
> is [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
> working rather than failing.** The plan expected a quadrature order justified by convergence: sample
> the Cell more finely, watch the answer settle, ship the lowest order that does. **It does not settle.**
> A line source falls off with distance and the Segments sit on the Cell's *own edges* — a Cell is 32
> Tiles and `block_tiles` is 32 — so the integrand is unbounded on the boundary and every refinement
> adds samples where the field is large. Measured at orders 1, 2, 4, 8 on one Cell, in Q16.16:
> **−252,011 → −296,734 → −323,982 → −337,116**, moving 17.7%, 9.2%, 4.1% and still going one way.
> ***The sample set defines the Cell's value; it does not estimate one.*** Had this been settled by
> argument the constant would have shipped with a derivation that reads well and is false, and nothing
> would ever have gone red.
>
> ✅ **What replaced it is a weaker claim that is true, and a ratifier that is reachable today.** Land
> value is read by *comparison* and never absolutely, so what a sample order has to preserve is the
> **ordering between Cells** — and it does: **615 of 630 Cell pairs order identically** under order 2
> and order 8 on a varied world, and all 15 that disagree are pairs the fine sample puts within 1% of
> each other. `CellDesirabilitySamplingTests` pins **both** halves, the non-convergence deliberately,
> and it goes red the day a term smooth inside a Cell arrives — which is the signal to reopen, not a
> regression. ⚠ **Contrast task 3 deliberately**: same milestone, same field, and this number got a
> ratifier that a machine can reach this week because the property it ratifies is one a *consumer does
> not have to exist* to produce.
>
> ⚠ **F17 — NO BASELINE MOVED, the plan above said every one would, and the reason is worse than the
> prediction.** The only thing in the whole build that creates a Cell row is `EmitPollution`, and ***no
> shipped Ruleset emits any pollution*** — all eight say so in their own headers, *a dwelling is not
> industry*. `MapLayers.Seal` has **no caller in `src/` at all**. So the producer walks an empty table
> in every world that exists, land value stays zero, and the thirty-two State Hash samples do not move.
> ***The milestone's mechanism is built and unobservable, and it is unobservable for want of Ruleset
> content rather than for want of code.***
>
> 🔴 ⚠ **F18 — and that makes decision 5's *floor* unreachable, which is `adr/0125`'s own failure
> arriving one level down four hours after it was written.** That record established a floor precisely
> because the real ratifier needed a **consumer nobody built**; the floor it chose names
> `rulesets/congested.toml`, and on `congested.toml` the Cell table is empty, so all three of its
> readings — the field varies, both terms are visible, the pollution/noise correlation — are unreadable.
> ***`adr/0052`'s checklist still does not ask whether the named state can occur, and knowing that it
> does not ask was not enough to make me ask.*** Routed: the two §D1 floor rows now carry the 🔴, the
> `plans/0013` row is filed with its multiplicand **guessed at zero**, and task 7 below owes the world.
>
> ⚠ **A Tick-budget row is filed and it prices nothing.** Four `Desirability` calls a resident Cell on
> one Tick in 256; resident Cells is **guessed at zero** for F17's reason. ***A row whose multiplicand
> is zero because the world is empty is not a cheap row, it is an unpriced one.***

### Task 5 — the bound, and decision 6's reading — ✅ **DONE 2026-08-20**

> ✅ **Decision 6 is settled** ([`adr/0127`](../docs/adr/0127-the-land-value-target-never-stops-moving-so-the-question-is-what-the-lag-rests-around.md)),
> and **the milestone's six scoped decisions are all closed.** The reading, on `rulesets/fouled.toml`,
> eight Days to settle and four observed, 262 resident Cells, 32 cadence samples:
>
> | Reading | Value |
> |---|---|
> | Cells moving per sample | **185** of 262 (min 0, max 212) |
> | Cells that never moved | **50** |
> | Widest peak-to-trough swing | **74,373** raw Q16.16 |
> | Mean swing | **22,863** — ≈0.35 units against a deepest Cell of ≈−28 |
> | Mean value, early half vs late | **−567,787** vs **−563,871** — no trend |
>
> ⚠ **F24 — the thing decision 6 worried about is not what the field does, and the SIZE of the motion
> is what says so.** The minimum-step-of-one is safe: [`adr/0122`](../docs/adr/0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md)
> deleted `w₁`, so the target is exogenous, the gap reaches exactly zero and the operator stops — and
> **the fifty still Cells are the direct proof**, clean ground with target zero and gap zero. What moves
> the field is that the *target* never settles (F21). ***A ±1 flicker is a swing of ONE raw unit and the
> observed swing is 74,373, so a test that merely asked whether it moves would have confirmed the wrong
> phenomenon.*** The assertion therefore checks the swing is **large**, and goes red if the motion ever
> collapses to the dead band's size.
>
> ⚠ **F25 — the task asked for a bound and found the bound was already wrong by a factor of ten.**
> `Invariant.LayerMagnitudeIsBounded` bounds a Cell's pollution *source* at
> `SeparableKernel.SourceCeiling` — about **327,000** at the shipped radius. `Desirability` then lifted
> the read value into Q16.16 with `Fixed.FromInt`, which is `checked` and throws above **32,767**. ***So
> the composition threw an `OverflowException` on a world the invariant calls legal***, at whatever Cell
> somebody happened to read. **The repair is a conversion removed rather than a type widened**: pollution
> is a *count* and the weight is a *ratio*, so the product is already Q16.16 and the count never needed
> lifting — `Fixed.Mul`'s own remark that the fix is a range assertion and not a wider type is right, and
> the call site's defect was the conversion. The result saturates rather than throwing, on
> `LineSourceQueries.Saturate`'s reasoning.
>
> ⚠ **And the first version of that test did not bite.** One Cell at the ceiling contributes only
> `source / gain` ≈ **4,041** to its own read value, because the kernel is normalised; it takes a full
> kernel support of ceiling Cells — which the invariant permits — to sum back to the ceiling itself.
> ***A bound stated per Cell is not a bound on what a Cell reads***, because a diffused field is a sum
> over its neighbours. Verified by reverting the fix and watching the test throw.
>
> ⚠ **What is left is arguable and is filed rather than decided**: *should* land value swing with rush
> hour? A land value that rises overnight and falls at eight is a traffic meter with a lag on it, and
> the case for a time-averaged noise term has no number that refutes it. [`0002`](0002-open-questions.md)
> §C, trigger **milestone 13**. ⚠ **Do not close it by tuning `land_value_tau`.**

### Task 6 — something to look at — ✅ **DONE 2026-08-20**

> ✅ **`--land-value`, a mode of its own, printing THREE grids** — the target, the lag, and the gap —
> plus what the field did over the run. Every string is the shell's; `Core` hands over Cell coordinates
> and integers.
>
> ⚠ **F26 — it is not `--layer` grown, and the task's own sentence is why it could not be.** `--layer`
> builds its own world and hand-places sources; land value is a **history**, so a picture of it needs a
> city that has been *running*. ***A lag is not a property of a value, it is a property of a pair***,
> which is why one grid was never going to be enough — the claim `02 §2.4` makes is about the
> *difference* between two fields, and neither field alone can carry it.
>
> ⚠ **F27 — `--layer`'s refusal carried a stale sentence and it is exactly `adr/0093`'s shape.** It read
> *"a Layer dump builds its own world with sources in it, because **no session can place a source until
> Rules exist**"* — true when written, false since slice 7, and **wrong about the trigger** rather than
> about the mechanism. Corrected in place to the narrower thing that is still true: the dump wants a
> field it authored so the halo it prints is attributable to the one source it added.
>
> ⚠ **F28 — the dump prints the HOUR, and that is the most useful line in the header.** F21's
> consequence made operational: at a round multiple of 2,048 Ticks the run lands at midnight, every
> Segment is empty, and the picture is of a **one-term composition wearing a two-term formula** — which
> would read as working. The demonstration command is therefore **21163** Ticks and not 20480, and the
> test pins `THE HOUR IS 08:00`. ***A picture of an instantaneous quantity that does not say which
> instant is not a picture of the city.***
>
> ⚠ **And the mode carries no ordinal.** `ParkingDumpTests` says *the tenth runner mode*, `Options`
> carries a struck sentence saying *a count in prose is a fact that drifts, and the tenth mode is what
> made the drift legible*, and the board records **two branches each shipping a tenth**. ***Count the
> enum.*** The window is derived from the world for the same reason — `--layer`'s fixed box would draw
> a blank grid the first time the generator moved the city.
>
> **Read at ten Days and eight hours on `fouled.toml`**: target peak **29.00**, lag peak **29.19**, gap
> peak **1.07** — so the lag runs about **4%** behind its target, and about **209 of 262** Cells move on
> a typical cadence sample.

### Task 7 — the long acceptance run — ✅ **DONE 2026-08-20**

> ✅ **100,000 Ticks on `rulesets/fouled.toml` at 1,000 Citizens, 49 Days read**, in
> `LandValueLongRunTests`. **The level settles** — a mean of about **−168,000** Q16.16 from Day 13 to
> Day 49, drifting under 10% between the two halves of that window. **The flow does not trend** — Day
> -over-Day movement collapses from **11,135,893** on the fill to a band of roughly 200,000 to
> 1,700,000, and never doubles. **The collection does not grow**: 163 Cell rows, constant.
>
> ⚠ **F29 — THE FLOOR REFUTED ONE OF ITS OWN READINGS, AND THAT IS WHAT A FLOOR IS FOR.** Readings 1
> and 2 pass — the field varies, both terms are visible, pollution running about 8 to 11 times noise.
> **Reading 3 fails**: pollution and noise are rank-concordant across Cells at **86 to 100 percent** on
> every readable Day, so ***no ratio between `w₂` and `w₃` is identifiable in this world at all.***
> [`adr/0125`](../docs/adr/0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md)
> said in its own words **it refutes and never confirms**, and it did.
>
> ⚠ **And the cause is sharper than the one `plans/0002` §D1 anticipated.** That entry expected the two
> to co-vary because *industry and roads are both placed in proportion to the same population*. In
> `fouled.toml` they do not merely correlate — ***they have the same source***: the emitting kind is
> `dwelling` (F19), and dwellings are also what generate the commute. **The simplification that made
> the floor reachable is what makes its third reading unreadable.** What is owed is the hand-authored
> world §D1 already named, and it is now owed for a reason a run produced rather than a reason a
> sitting predicted.
>
> ⚠ **F30 — the instrument had to be fixed twice before the quantity stopped depending on the
> sampling.** A reading every 2,048 Ticks lands at the same hour every Day, and the obvious phase —
> `(tick + 1) % 2048 == 0` — lands at **23:59**, where every Segment is empty and the noise term is
> zero in every Cell. ***A periodic reading of an instantaneous quantity samples ONE HOUR of the day,
> and which hour is a choice somebody has to make on purpose.*** Moved to 08:00: the noise term still
> read **zero on ten of forty-nine Days**, because Shift starts spread over 6 to 10 and a
> 1,000-Citizen city can have nobody on the road at that exact Tick. Four attempts cut it to two Days;
> the shipped band — 06:00 to 12:00 every 32 Ticks — cuts it to **one**, and that one is asserted as a
> quiet morning rather than tuned away. ***A silent Day is a reading about the world; it becomes a
> reading about the instrument only when there are many of them.***
>
> ⚠ **The flatness is asserted on the FLOW, and the exempt axis is named.** Land value is a magnitude
> that is *supposed* to move, so `adr/0006` is asserted on the mean over a window and on the total
> absolute Day-over-Day movement. **The level within a Day is exempt**, because the target itself moves
> with the Day ([`adr/0127`](../docs/adr/0127-the-land-value-target-never-stops-moving-so-the-question-is-what-the-lag-rests-around.md));
> the flow across Days is not exempt and is what is checked.

### Task 7 — as scoped

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

> 🔴 ⚠ **AND `congested.toml` DOES NOT WORK EITHER — task 4's F18.** The only thing that creates a Cell
> row is a pollution emission and **no shipped Ruleset emits any**, so on `congested.toml` the Cell
> table is empty and land value is zero everywhere: all three readings above are unreadable there too.
> **This task therefore owes a world before it owes a run** — a Ruleset with an emitting kind, which is
> a ninth demonstration file with its own header rather than an edit to `congested.toml`, because
> `congested.toml` is a golden baseline artefact and because *what it exists to show* is congestion.
> ⚠ **The new file needs a `[[zone_rule]]` that places the emitting kind without the city filling with
> it**, which `CLAUDE.md` already names as the hazard behind `[[building]] jobs` sitting on `dwelling`.
> ⚠ **And a Ruleset that emits is the first world in which the producer costs anything**, so it is also
> what unblocks [`0013`](0013-tick-budget.md)'s row — the multiplicand is guessed at zero until it
> exists.

> ✅ **THE WORLD IS BUILT, 2026-08-20 — `rulesets/fouled.toml`, the ninth Ruleset.** `congested.toml` in
> every respect except **one Rule that emits pollution**, so the difference between the two files is the
> field and nothing else, which is what makes it a controlled instrument rather than a second city.
> **Readings 1 and 2 are taken and pass**; reading 3, the correlation, is still owed and belongs to the
> run. `FouledRulesetTests` holds them.
>
> ⚠ **F19 — the emitting kind is `dwelling`, and that contradicts the sentence the other eight files
> repeat.** An industrial kind was tried first and abandoned. A Zone Rule creates a Building only while
> the Unplaced Pool is non-empty, the Pool is unhoused **Households**, so ***the only demand signal the
> build has is demand for homes*** — industrial demand is **unbuilt** under
> [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md), and the two-kind
> city that resulted was one this sitting could not explain: `works` outnumbered dwellings three to one
> by day two. ***A demonstration Ruleset nobody can explain is a worse instrument than an honest
> simplification***, and this milestone had already shipped one mechanism it could not observe.
>
> ⚠ **And the reason it could not be explained was a swapped index**, found afterwards: `dwelling` is
> kind byte **1** and not 0, so the diagnostic's two counters were reading each other's kind and the
> city was doing the opposite of what it looked like — it was filling with `works`, which is
> `CLAUDE.md`'s own named hazard, *a workplace kind needs a second `[[zone_rule]]` or the city fills
> with offices*, arriving exactly as written. ***A count printed against a hand-written index is a
> measurement of the index.***
>
> ⚠ **F20 — the emission was tuned across three orders of magnitude before it landed, and the first two
> attempts read as zero for two different reasons.** A diffused Layer is stored **in kernel units** and
> `SeparableKernel.Normalise` divides by the kernel's squared gain at the *read* site, so a raw column
> holding 1,393 reads as **0** — the field existed, was large in storage, and was invisible to every
> query. ***A stored magnitude and a read magnitude are different numbers, and nothing in the diagnostic
> said which one it was printing.*** Settled at `rate = 128`, `amount = 1`: pollution peak **26–29**
> against noise **2.5–3.8**, so **8 to 11 times** rather than 60 or 600, and land value settles at about
> **−28 from day 4** with **177–180 of ~262 Cells holding distinct values**.
>
> ⚠ **F21 — THE NOISE TERM IS ZERO AT MIDNIGHT, in a city where every Household owns a car**, and this
> is a property of the composition rather than of the file. A Segment's volume is the count of Vehicles
> on it **at that instant**, so noise is instantaneous while land value is the only part of the
> composition with memory. ***The land value target therefore oscillates at the Day's own period***, and
> what a Cell settles on depends on where the 256-Tick cadence lands against the commute peak. **That is
> decision 6 arriving as a measurement before its sitting**: the question is not whether the lag can
> rest, it is what it rests around. Pinned by
> `Noise_is_zero_at_midnight_and_that_is_the_composition_not_the_world`.
>
> ⚠ **F22 — `CLAUDE.md` said *all seven Rulesets* of the two `[parking]` keys while eight files stood
> and six carried them.** A count in prose, drifting, in the file whose own rulesets row says ***count
> them rather than quoting a total***. Replaced with the condition rather than a new number: *every
> Ruleset that states `[parking]`*.

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
