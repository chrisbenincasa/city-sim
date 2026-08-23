# Radiation is industrial pollution's mechanism with its own source and its own rate, because a shared decay is wrong for one of them

**Radiation is a point source over kilometres, isotropic and superposing, diffused through the same separable tent kernel as industrial pollution — the same mechanism, the same implementation, the same geometry.** It is not a new kind of field and needs no new machinery.

**It is nonetheless its own `Layer`, with its own source column and its own decay rate, and the reason is persistence rather than geometry.** `DecayPollution` reads one `tau` from `LayerRates` and applies it to every Cell's source, so a reactor and a foundry emitting into one column decay together. ***A shared decay is wrong for one of them in exactly the way a shared kernel was wrong for one of them***, which is the defect [`adr/0034`](0034-fields-are-sorted-by-source-geometry.md) was written to undo, arriving one axis over.

**And the rate is small, never zero.** A source that never decays is a collection that grows with elapsed time, which [`adr/0006`](0006-no-collection-grows-with-elapsed-time.md) forbids. A long half-life is a sink; an absent one is not. `EMERGENCE` `LEGIBLE CAUSE` `NO VERDICT`

## Why

**The question was whether the player-facing taxonomy is the mechanism's taxonomy, and it is not.** SimCity 4 ships four pollution types — air, water, garbage, radiation — sorted by what the player calls them. This project sorts fields by **the geometry of the thing that emits them** (`adr/0034`, `02 §2.4`), which produces a different partition and has already split air pollution in two: **industrial** is a point source over kilometres on the Cell grid, and **near-road** is a line source over metres answered as a query, because *"one of them was always wrong"* under one kernel. Water pollution is sorted out again, into a Bin per Water Body with network transport rather than spread.

So *radiation* had to be tested against that partition rather than admitted because a reference game has one. Under `02 §2.5` guard rule 1 — *one field, one geometry, one range* — it passes: it is a point source, isotropic, superposing, with a plume much wider than a Cell. **On geometry it is industrial pollution.**

### The guard rule sorts on geometry and range, and is silent on persistence

**That silence is the finding, and it nearly produced the wrong answer here.** Applied literally, guard rule 1 says radiation and industrial pollution are one field, since they share both properties it tests. **The build then refutes that**: `MapLayers.DecayPollution` takes a single `tau` and walks every live Cell, so persistence is a property of the **Layer** and not of the emitter. Two sources sharing a column share a half-life, and there is no per-emitter rate to give them.

***A shared decay produces the same defect a shared kernel did.*** `adr/0034`'s case was that industry and traffic fed one `Pollution` row through one kernel and *"one of them was always wrong"*. Here a reactor whose contamination should outlive the city and a foundry whose plume should clear within days would share one number, and one of them would be wrong on every Tick. **The argument transfers whole; only the parameter changes.**

**So guard rule 1 is necessary and not sufficient**, and this ADR records the missing clause rather than quietly working around it: ***two sources are one field only if every per-field parameter is right for both.*** Geometry and range are two such parameters; the source decay is a third and was never enumerated.

### What it costs, which is almost nothing, and why that is the point

**The partition this produces is the one `Noise` and `NearRoadPollution` already demonstrate.** Those are two fields sharing one implementation with different parameters — *"the same call with a different `LineSource`, and that is the whole of the difference"* — which `02 §2.4` names as the shape it wants, against two fields sharing one *kernel instance*, which is the shape it refuses.

Radiation is that, one level up: one `LayerDiffusion`, one separable tent, one cadence mechanism, and its own `Layer` member, its own source column, its own rate. **No new geometry, no new query, and no new *kind* of representation** — `02 §2.4`'s table gains a row, and that row differs from industrial pollution's in one cell. Whether it also wants its own kernel radius is an ordinary Ruleset question and not this decision.

### Deposition is the mechanism and not the metaphor

**"Deposited in the ground" is what makes the slow rate honest rather than an authored permanence.** `adr/0051` already makes a Layer's source a stock the environment absorbs, so the persistent thing is the **source**, not the field: fallout lands, becomes the source, and the source stays. That is why the plume outlives the plant, and it needs no second field for the deposit.

**The alternative — an airborne plume and a ground deposit as two Layers — was considered and fails guard rule 1 in the ordinary way.** It would need a second geometry to qualify and does not have one; both are the same isotropic spread from the same point.

### Zero is available in this corpus and is refused here

⚠ **`sealing_decay_tau = 0` means never, and `rulesets/minimal.toml` calls it a *stated absence* rather than a defect** — so a zero rate is not automatically `adr/0006` in this project, and the reason it is admissible there does not reach here. **Sealing has a ceiling by construction**: `MapLayers.Seal` clamps to `CellGrid.TilesInCell`, so a Cell cannot seal past being wholly sealed and the quantity is bounded however long the city runs. **A pollution source has no such ceiling.** The only thing standing between a never-decaying reactor and an unbounded stock is `Invariant.LayerMagnitudeIsBounded`, which is a **defect detector and not a bound** — it reports that the city went mad, it does not stop it.

***So the ceiling has to come from the rate, and a rate of zero supplies none.*** What is authored is a half-life long enough that a player experiences contamination as permanent within a session, which is a felt quantity and a designer's number. `pollution_decay_ticks` is the precedent for its shape: **the Ruleset states a duration and the tau is derived**, because authoring the tau makes the felt quantity move whenever a cadence is retuned.

## Consequences

- **`Layer` gains a member, `LayerRates` gains a rate, and `LayerCellTable` gains two columns** — a source and a field — declared through `Rows.Saved` like the rest, so the State Hash covers them by construction. **`02 §2.4`'s representation table gains a row** that differs from industrial pollution's in one cell: the update rate.
- **The cadence is a second designer's number** under [`adr/0044`](0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md), hash-bearing, and it needs an offset that collides with neither pollution's nor land value's. ⚠ **The existing stagger is `0 mod 64` and `16 mod 256`**; a third has to be chosen against both, and *"not congruent to 0 modulo 64"* is the only property required.
- 🔴 **It opens `plans/0002` §D1 rows and fills none of them.** The decay duration, the cadence, the kernel radius if it differs, and the desirability weight are all hash-bearing and all unratified. ⚠ **The weight is unreachable for the reason [`adr/0125`](0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md) records for `w₂` and `w₃`** — nothing reads land value, so no quantity refutes a scale.
- **Nothing emits it, and that is a stated absence rather than an oversight.** No `[[building]]` kind is a reactor and no Rule declares a radiation `map` output. ***A Layer with no emitter is zero everywhere by construction***, which is exactly the state milestone 9's land value producer was in — built, correct and unobservable — and the lesson `rulesets/fouled.toml` exists to prevent repeating. **Whichever milestone builds this owes a Ruleset that emits into it.**
- **It does not follow that garbage is a fourth flavour.** Waste is a **Good** hauled by Vehicle (`adr/0031`), not a field, and it is sorted out by geometry the same way water pollution is. The partition this ADR defends is the reason that question is already answered.

## What would trigger revisiting

- **A per-emitter decay rate.** If a source's persistence ever becomes a property of the emitter rather than of the Layer — which nothing today wants — then the reason for a separate column disappears and radiation collapses back into industrial pollution with a per-source rate. **That is the one change that reverses this decision**, and it is a change to `adr/0051`'s absorption model rather than to this.
- **A measured cost from a third diffused Layer.** Pollution's pass is already the second-largest Layer consumer in [`plans/0013`](../../plans/0013-tick-budget.md); a third at the same kernel radius is roughly a third more of it, and the incremental scheme's dirty set is what decides whether that matters. **Unmeasured, and it should be measured on the day it is built rather than argued now.**
- **A remediation verb.** If cleaning contaminated ground becomes a player action it is a **sink the player operates**, which changes what the decay rate is for — it stops being the only bound and becomes the floor under one. That sits beside the *is Terraform a seventh verb?* question already open in `plans/0002` §C, and probably shares its answer.
