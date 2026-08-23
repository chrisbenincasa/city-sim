# Terrain suitability is baked at world creation, and the Layer holes that need it move to milestone 24

> 🔴 **Half superseded by [`0147`](0147-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md), 2026-08-22 — the placement stands and the artefact does not.**
>
> **What stands, entirely:** Fertility, Sealing's decay, Woodland, replanting and Water Bodies belong at
> milestone **24**; they are *placed rather than deferred*; 15 was never available for Water Bodies; and
> Sealing's decay is blocked in more than one place. That is this document's decision and none of it moved.
>
> **What is superseded:** the named artefact — *"terrain suitability **baked at world creation into a
> stored per-Cell column**"*. The quantity is renamed **Base Fertility** and is **Ruleset data keyed by
> terrain type**, never stored and never baked. The per-Cell column this document correctly sensed was
> missing holds **terrain type**, which `CONTEXT.md` → Sealing had already required for the decay rate.
>
> ⚠ **The error came from the name.** Read *terrain* in *terrain suitability* and a terrain-derived
> per-Cell field is the natural inference — while `02 §2.3` says *"the generator places Woodland and
> nothing else. Fertility is not on the map"*, and this document's own citation, `adr/0022`, argues
> against exactly that field at length. **`CONTEXT.md` had no entry for the term to have been checked
> against**, and now has one. ***A badly named term is a design defect waiting for somebody to reason
> from the name.***
>
> ⚠ **And the disposition was the one `adr/0022` forbids for the value beside it**: its consequence list
> says decay rates are *"Ruleset data keyed by terrain type, **never stored per Tile**"*, because storing
> tuning as state freezes it into every save. Two values are keyed by one column and only one of them had
> been thought about as tuning.
>
> **The filename keeps the old term**, because a filename is its claim and this one was made in good
> faith. **This document's third revisit trigger — *a Tick-time consumer of terrain that a stored column
> cannot serve* — is answered in the negative** by `0147`: a stored type plus a Ruleset lookup serves
> every consumer that exists.

**Fertility, Sealing's decay, Woodland, replanting and Water Bodies leave milestone 9 for milestone
24.** None of them is deferred for cost; each needs terrain, and terrain has one source. **And the
milestone that builds terrain owes a specific artefact this corpus had not named: terrain suitability
**baked at world creation into a stored per-Cell column**, because Fertility is composed at the point of
use — inside a Tick — and [`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)
forbids a Tick phase reading a terrain value.** `06`'s milestone 9 row and both inventory rows are
rewritten rather than quietly emptied.

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
Where a milestone's rows sit is a sequencing question and names no number.

## Why

### The destination was forced rather than chosen

[`0034`](../../plans/0034-the-land-value-target-and-the-composed-layers.md) decision 4 offered Water
Bodies a home *"beside Amenity at 15 or beside the generator at 24"*. **15 was never available.**
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) generates terrain
procedurally from the world seed and states that **water is immutable**; nothing places water by hand,
by Ruleset or by player verb. A Water Body has exactly one possible source and it is the generator. The
same holds for Fertility, whose `terrain suitability` term has no other producer, and for Sealing's
decay rate, which `02 §2.4` keys **by terrain type**.

***A row is not deferred when it has nowhere else to be; it is placed, and the placement was already
determined by a decision taken three years of documents ago.***

### The finding that has no home yet, and the reason this is an ADR rather than an edit

[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)'s own table says terrain
**does** *"feed land value and desirability"* and **does not** *"get read by any Tick phase"*, and then
states the rule outright: ***"if a terrain value is read inside a Tick phase, something has gone
wrong."***

Both cells are correct and they constrain the implementation jointly, which neither says. Fertility is
`terrain suitability − Sealing − pollution`, **composed at the point of use and never stored** — and the
point of use is a farm Rule evaluating inside Phase 2. Desirability is the same shape and this milestone
schedules it on `tick % 256 == 16`, inside Phase 5. So a Fertility that reads terrain when it is asked
breaks the checkable rule on the day it is written, and **nothing in the corpus currently says how it
must not**.

**The resolution is the shape Sealing already has**: terrain suitability is computed **once, at world
creation**, from the seed, and stored as an ordinary per-Cell column. A Tick then reads a stored column,
which is not a terrain value any more than Sealing is. The boundary
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) draws is **temporal, not
categorical**, and it says so — *"terrain does not reach the simulation would be wrong"*. This ADR
records the requirement so that milestone 24 finds it rather than rediscovering it, which is
[`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s discipline
applied forwards.

### Sealing's decay is blocked twice, and only one blocker is written down where a tuner looks

`MapLayers.DecaySealing` is honest in its own doc-comment — *"Not scheduled, because its rate has no key
yet"* — and it is accurate. But there are **two independent reasons it does nothing**, and they fail in
different places:

1. **`sealing_decay_tau = 0` in every shipped Ruleset.** The header explains this: `02 §2.4` keys the
   rate by terrain type, there is no terrain, so there is no rate to look up. A **stated absence**.
2. **`MapLayers.Step` does not call it at all.** The schedule runs pollution and land value; Sealing has
   no cadence, and `LayerSchedule.For` answers `Never` for it.

⚠ **A tuner who set `sealing_decay_tau = 8` would get nothing, and the Ruleset header they were reading
does not mention the second reason** — it is a comment about the *rate*, which is what it claims to be.
This is not a defect and not `0093`, because no sentence describes the trigger wrongly; **it is a fact
about the build that lives in one place and is needed in another.** Recorded here and in `06`'s
milestone 24 row.

⚠ **And scheduling it is not the freebie it looks like.** Sealing has no cadence, so giving it one is a
**hash-bearing world-creation number**, and [`0044`](0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md)
makes a Layer cadence the designer's number rather than the profiler's. It needs a ratifier under
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) — a machine, a
world and a quantity — and the world it would be ratified against is one with varied terrain. **So the
cadence belongs with the rate, at 24, and pulling it forward would open an unratifiable number to
schedule an operator that would still do nothing.**

⚠ **The Rulesets are not edited.** `congested.toml` and `scarce.toml` are golden baseline artefacts and
**editing them, comments included, moves committed hashes** — so a clarifying comment would cost
baselines to say something `06` can say for free. ***Where a fact can be routed to a document or to a
hashed artefact, it goes to the document.***

### Moving these rows falsifies `06`'s own note about milestone 24

`06`'s row for 24 carries: *"⚠ **Appended rather than placed**: it depends on nothing in the spine and
nothing in the spine depends on it, so its position is the weakest claim in this table."*

**After this ADR that sentence is false**, and it is false because of this ADR rather than despite it.
Desirability's shoreline term, Fertility, and agriculture's whole Woodland-and-replanting loop now wait
on 24. **The spine depends on it, and its position claim is stronger than any other row's rather than
weaker** — it is the only remaining milestone with a hard, named dependency running into it from the
demand spine.

***A row that absorbs work absorbs the sentences that were true while it was empty***, and leaving that
note standing would be `plans/0012` **Cause 1** created deliberately in the same edit that caused it. It
is rewritten here.

## Consequences

- **Milestone 9 is the smaller milestone**
  [`0034`](../../plans/0034-the-land-value-target-and-the-composed-layers.md) **F7** argued for: noise, a
  two-term composition, two weights and a caller.
- **Milestone 24 gains five inventory rows and a named artefact** — the baked terrain-suitability column,
  which is a deliverable rather than an assumption, and without which Fertility cannot be written at all.
- **Desirability is not fully composed until 24.** Amenity arrives at 15
  ([`0123`](0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md)),
  shoreline at 24. Any consumer reading land value before then is reading a partial field, and the two
  milestones that complete it are far apart.
- **`06`'s milestone 24 note is rewritten**, and its position claim goes from the weakest in the table to
  one of the better-supported ones.
- **Sealing's decay cadence is an open hash-bearing number owned by 24**, not by 9, and it is recorded as
  one rather than left to be discovered by whoever tries to schedule the operator.
- **No Ruleset is edited and no baseline moves.**

## What would trigger revisiting

- **Somebody proposes placing water without a generator** — an authored Water Body in a Ruleset, for a
  demonstration file. That is a real possibility and it would move Water Bodies off 24; it also
  contradicts *water is immutable* and needs its own argument.
- **Milestone 24 is split.** It now carries terrain generation, Shocks, the Intensity Dial and five land
  rows, and the option of a separate *land systems on terrain* milestone after it was considered and not
  taken. If 24 is scoped and found to be two milestones, this is where the split was first available.
- **A Tick-time consumer of terrain is found that a stored column cannot serve.** Then the bake is
  insufficient and [`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)'s
  checkable rule is the thing under pressure, not this placement.
