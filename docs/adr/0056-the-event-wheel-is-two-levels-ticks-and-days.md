# The Event Wheel is two levels — Ticks and Days

> ⚠ **Corrected 2026-08-21 by milestone 18's scoping ([`plans/0036`](../../plans/0036-the-coarse-day-wheel.md)). The decision stands; its arithmetic and its sizing *method* do not.** Every number below was written when `TICKS_PER_DAY` and `WHEEL_SIZE` were **8192**. [`0094`](0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md) moved both to **2048**, and the sweep that followed corrected [`02 §7`](../02-simulation-model.md) and [`05`](../05-technical-architecture.md) in place — with strikethrough and a dated note — and reached neither this document nor [`0011`](0011-household-life-stages-and-self-generating-population.md).
>
> **Untouched and load-bearing**: two levels and no more; the coarse bucket being one Day; the refusal of both the wrap and the flat overflow list; one wheel per scheduled table; and the partition of every scheduled row into {armed, waiting}. **What changed**: the fine wheel is **2048** buckets, and the coarse wheel's bucket count is **withdrawn rather than restated** — [`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md) sizes a wheel by *the longest common event horizon* and this ADR sized it by **symmetry with the fine wheel**, which is not a rule this corpus has.
>
> ***A conclusion computed from a constant does not move when the constant does***, and the end-stop under *Consequences* is that conclusion. A restatement is findable by search; a conclusion is not.

**The Event Wheel has two levels: a fine wheel of ~~8192 buckets of~~ one bucket per Tick, spanning
exactly one Day, and a coarse wheel of ~~8192 buckets of~~ one bucket per Day, cascading into the fine
wheel at each Day boundary. There is one wheel per
scheduled table, not one wheel over tagged entities. A sleep longer than the fine period is carried by
the coarse wheel and never by a wrap — and until a consumer for a long sleep exists, an over-long
arming is refused rather than represented.** `FAST ITERATION` `HONEST DEGRADATION`

## Why

### The period is one Day, and the design already schedules in Days

`WHEEL_SIZE` is ~~8192~~ **2048** Ticks and `TICKS_PER_DAY` is ~~8192~~ **2048**, so **the wheel's
period is exactly one Day**. `02 §7` justifies that as *"at least as long as the longest routine sleep"*
and cites a Citizen at work for a third of a Day, which fits.

> ⚠ **The conclusion survives the correction and the reasoning behind it is not what it looks like.**
> Both constants moved together under `0094`, so the period is still exactly one Day — but
> [`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md) is emphatic that this is
> **not a definition**: *"`WHEEL_SIZE` is set by the longest common event horizon, **not by the Day**.
> These are independent questions that happen to share an answer."* `02 §7`'s own correction note says
> the same — it *"moved with the Day, for the reason stated — **independently**, and to the same
> number."* ***Two numbers that agree for two reasons are one number in every document that quotes
> them***, and the consequence below is where this ADR spent that confusion.

[`0011`](0011-household-life-stages-and-self-generating-population.md) then put Life Stages on the same
wheel — *"a Household holds a stage and a countdown in **Days**, transitioning on the Event Wheel"* —
and defended the choice at length against [`0010`](0010-one-clock-and-demographics-by-sorting.md)'s
two-clock objection: *"a per-Household countdown denominated in Days is an ordinary event on the Event
Wheel — the same clock, just a rare event on it."*

**The two cannot both hold, and the failure is not at the tail.** A countdown of *two Days* already
exceeds the period. Every Life Stage transition the design has specified is unrepresentable on the
wheel it was specified to run on. `EventWheel.Arm` does not wrap it — it throws, which is why this
surfaced as a design question rather than as a city where a Household's next event is silently in the
past.

### A flat overflow list is not an alternative at target size

The obvious repair — one unsorted list of far-future sleepers, rescanned at each wrap — fails on
distribution rather than on total cost. At 1,000,000 Households essentially *every* Household is
mid-stage at any moment, so the list holds ~1,000,000 entries permanently. Rescanned once per Day that
is ~122 entries per Tick **amortised**, and a single-Tick burst of a million **in fact**. Staggering the
burst is the whole content of the repair, and a staggered overflow list is a coarse wheel with its
buckets hidden. The structure is forced, not chosen.

### Two levels, because there are two time bases and no more

A Day is a fixed integer number of Ticks. The coarse wheel is therefore a **radix over one clock**, not
a second clock, which is what `0010` forbids and what `0011` correctly anticipated being accused of.
There is no third time base in the design, so there is no third level.

### One wheel per table, because the alternatives are all a collection object

[`0004`](0004-typed-tables-over-ecs.md) rejected an ECS and [`0036`](0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md)
bans reference types in simulation state, so a single wheel over heterogeneous entities cannot hold a
polymorphic element. It would need a `(kind, slot)` pair, and that pair lives either in a side table
whose rows are allocated and freed — a collection needing a sink under
[`0006`](0006-no-collection-grows-with-elapsed-time.md) — or in a per-entity structure, which the
intrusive-index-list rule exists to prevent. A wheel per table needs neither: each table already
carries its own `next` column, and cross-wheel ordering is fixed by declaration order.

## Consequences

- ~~**The coarse wheel is 8192 Day buckets**, symmetric with the fine wheel and another 64 KiB per
  consumer. That is ~22 game-years of compressed stages, which means **a third level can never be
  needed** — worth more than the memory saved by sizing it tightly to the longest Life Stage, because a
  tight size is a number somebody would have to defend and this one is a structural end-stop.~~
  - 🔴 **WITHDRAWN 2026-08-21. The bucket count was chosen by symmetry, and symmetry is not this
    corpus's sizing rule.** [`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md)
    sizes a wheel by **the longest common event horizon**. The fine wheel's horizon is the longest
    routine sleep, which is bounded by one Day; **the coarse wheel's is the longest Life Stage arc**,
    which lives in [`0011`](0011-household-life-stages-and-self-generating-population.md)'s stage
    table. ***So the count is derived rather than chosen***, which is `lots_per_segment`'s standing and
    a `[[kind]]`'s employment's — and under
    [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) **a derived
    number owes a derivation and not a ratifier**.
  - ⚠ **The derivation cannot be completed yet, and saying so is the answer rather than a failure.**
    *A Household's life in Days* is a `plans/0002` **§D2** gap whose only named ratifier is
    **playtest**, and no `[[life_stage]]` table exists for an arc to be read off. **The count lands
    with the stage table, at milestone 20.** Until then the coarse wheel has a *shape* and no *size*,
    and nothing may quote one.
  - 🔴 **And the end-stop is withdrawn with it.** *"A third level can never be needed"* was not an
    argument; it was **arithmetic on 8192 Days** — ~22 game-years, against a Household life `0011`
    puts *"on the order of a thousand Days"*. Sized symmetrically at the corrected constant it would be
    2048 Days, **~5.6 game-years, about two Household lives** — reachable, and therefore not an
    end-stop at all. ***Whether a third level can be ruled out is a property of the derived size and
    not a claim this ADR is entitled to make in advance.***
  - ⚠ **The memory figure goes too.** *"Another 64 KiB per consumer"* is 8192 buckets × a head and a
    tail `int`; at the fine wheel's 2048 the same structure is **16 KiB**. Neither is the coarse
    wheel's, because the coarse wheel has no count. **Quote neither.**
- **The cascade is a Day-boundary step costing `O(entities waking that Day)`**, not `O(entities
  asleep)`. That is `02 §7`'s own claim, applied one level up.
- **Every scheduled row is in exactly one of {armed, waiting}, and is unlinked when its owner row is
  freed.** `World.Unlink` already does this for Rule Instances across demolition; every new consumer
  inherits the obligation. This is what keeps the wheel bounded under `0006` — membership is a
  *partition* of the live rows rather than an accumulation, so the wheel structurally cannot grow with
  elapsed time.
  - **Amended by slice 9: the partition holds at a phase boundary, and that is its domain rather than a
    caveat.** Between Phase 1's `CollectDue` and the end of Phase 3 a due row is in **neither** set — it
    is held in the Rule engine's own array, with `blocked` still reading `Nothing` — and `RuleEngine`
    said so in a comment before this ADR said the opposite. Stated unqualified, the sentence is false
    for two of the eight phases, and because the bullet then extends it to *every new consumer*, what a
    Phase 2 author inherits is a claim they will find false the first time they check it mid-Tick, with
    nothing to tell them whether they have a bug or a bad invariant. The in-flight window is bounded by
    one Phase 3 apply and by nothing a consumer chooses.
  - **The third state is what makes the `O(1)` check possible, which is why it is worth naming rather
    than hiding.** `blocked` cannot separate *armed* from *in flight*; `next_event_tick` can, because
    `Arm` refuses a delay of zero — so an armed row is due strictly later than now and a popped one is
    due exactly now. That is `EventWheel.IsArmed`, and it is what lets a double arming be refused where
    it happens instead of being counted at the end of a run.
  - **All three checks slice 9 added are relative to *now*, and the wheel has no *now* of its own.** It
    reaches one only through its callers, so each check rests on a property nobody had written down:
    the double-arm refusal on **time being monotone**, the period bound on the **caller passing a
    truthful Tick**, and `Unlink` on the **phase order**. That is the same wall slice 8's `World.Adopt`
    hit when it had to take the Tick as a parameter, and it is a property of the World rather than of
    the wheel.
- **A Life Stage's wake is a uniform draw over `[N, N+W)` Days**, with `W` authored per `[[life_stage]]`
  in the Ruleset and hot-reloadable under [`0015`](0015-all-tuning-data-is-hot-reloadable.md). The
  countdown keeps its meaning as a **floor** — *"at least N Days in this stage"* stays literally true —
  and `W` is how much longer it took.
  - **This is a hash-bearing number and it owes a ratifier under
    [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)**, recorded in
    `plans/0002` §D. It is deliberately *not* the same shape as slice 7's arming stagger, which turned
    out to have no number to choose: the Tick *within* a Day is unconstrained, so uniform is the only
    draw that stays spread, whereas a **width in Days is a free parameter**. The distinction is the
    whole reason one needed a ratifier and the other did not.
  - **It damps the cost spike and deliberately leaves the demographic echo.** A founding generation
    really does age together and real cities echo for decades; that is `EMERGENCE` and it stays. What
    does not stay is a Tick whose cost is a function of how the player built.
- **Slice 9 builds the fine wheel only.** Every `rate` in `rulesets/minimal.toml` is 8–32 Ticks and the
  Zone Rule interval is 32, so **the coarse wheel has no consumer until Life Stages arrive in Phase 2**.
  Until then `Arm` continues to refuse an arming of zero or of a whole period, which is `HONEST
  DEGRADATION` in the direction where the honest thing is to refuse: a wrap would put a Household's next
  event in the past and nothing would say so.
- **`02 §7`'s *"every Building, Household, and Citizen carries a `next_event_tick`"* is corrected.** The
  column belongs to whichever table is scheduled, and a Building has no event of its own today. A wheel
  is added when its consumer exists.

## What would trigger revisiting

- **A third time base entering the design.** The two-level shape rests on there being exactly two, and
  `0010` is what guarantees that. A mechanism denominated in something other than Ticks or Days would
  reopen the level count rather than the wheel.
- **A single Day bucket's cascade showing up in a profile.** The remedy is a wider `W`, or a finer
  coarse bucket, never a return to rescanning a flat list.
- **A consumer whose sleep exceeds the coarse wheel's derived period.** ~~~22 game-years is an
  end-stop chosen to be unreachable~~ **the period is derived from `0011`'s stage table and is unset
  until that table exists** — but the diagnosis is unchanged and is the useful half: something reaching
  it is evidence the compression in `0011`'s stage table has drifted, **and the stage table is the
  thing to look at first.**
- ⚠ **`TICKS_PER_DAY` moving again.** It has moved once — 8192 → 2048, `0094` — and the sweep that
  followed reached `02` and `05` and not this document. **Every number here is denominated in Ticks or
  in Days**, and the two kinds fail differently: a *ratio* claim survives such a move, and a **derived**
  one does not. ***It is the derived ones that read as settled while being stale***, because they carry
  no constant a search would find — an end-stop stated in **years**, a memory figure stated in **KiB**.
  On the day the Day moves, this document is read for its conclusions and not grepped for its digits.
- **The refusal firing in a build rather than in a test.** That would mean a long sleep acquired a
  consumer without the coarse wheel being built, and the fix is to build it, not to relax `Arm`.
