# A Day is 2048 Ticks, because Ticks per Day is a sampling rate and not a length of life

**`TICKS_PER_DAY = 2048`. A Tick is 42.1875 s of in-world time and a Day is 2m08s at 1×.** Nothing about
the world changes: a car still does 50 km/h, a bakery still bakes what it baked, a commute is still 20
clock minutes and still 1.39% of a Day. What changes is that the simulation **samples that world four
times more coarsely**, so four times as much of it passes per second the player is sitting there.

The rule that decides what moves with the constant has three classes and one question:
**what is the quantity denominated in?**

| Denominated in | Examples | What happens |
|---|---|---|
| **In-world time** | speeds in km/h, the Commute Budget in minutes, pollution decaying over a Day | **unchanged as a quantity, ×4 more of it per real second** |
| **Ticks** | Rule `rate`, ~~`revisit_ticks`~~, `interval`, the Map Layer cadence | **kept at its number**, so unchanged in real seconds and ×4 coarser in-world |
| **Both at once** | conversion factors — `Speed.PerKilometrePerHour`, `TravelTime.RawPerDay` | **⚠ ADDED 2026-08-13 when the change was built.** A factor denominated in two units belongs to no row above, and one of these was a literal |
| **Ticks, but priced in another unit** | Bin `capacity`, and every buffer in the game | **⚠ ADDED 2026-08-13 when the change was built.** It holds *firings × rate*, which is a duration, and it is **written in Goods** — so **the number must move ×4 to keep the quantity still**. Same unit as `amount`, opposite row |
| **Days** | Life Stage countdowns, the demographic arc | **unchanged in Days**, so ×4 faster in real time — which is the point of the change |

⚠ **`revisit_ticks` is struck from the Ticks row, 2026-08-13, with the user in the room.** It is a
**duration** — [`0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md) makes it
*how long the development industry takes to look at every Lot once* and derives the sample from it, and
`rulesets/minimal.toml` says on that very line that **8192 IS TICKS_PER_DAY**. Kept at its number it
would have made the survey take **four Days** in silence, and the development industry would have been
the one Day-scale process in the simulation that did *not* speed up with the clock — which is the whole
point of the change. It goes to 2048 in all three Rulesets. **The cost is real and is accepted**: the
derived sample quadruples, so the Zone Rule costs ×4 per Tick and exactly what it always did per Day.

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) as to
the class rule, and **its value is not**: 2048 is a pacing choice with a named ratifier — playtest — and
the two numbers that would refute it are written into *What would trigger revisiting*.

## Why

### This is `adr/0019`'s forbidden move, and `adr/0082` is what makes it legal

[`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md) closes with a sentence aimed
directly at this ADR:

> *"Nothing about pacing, session length, or 'days feel too long' should ever reopen this. Those live in
> the speed ladder."*

and its central argument is *Why shortening the Day is not a pacing change*: a commute takes a fixed
number of Ticks, so halving the Day doubles the share of a life spent in transit — **"same city, same
population, same roads, twice the vehicles."**

**That argument is sound, and its premise stopped being true on 2026-08-12.** It requires a Tick to have
a duration fixed independently of the Day, which is exactly what `adr/0019` believed, because it derived
the Tick from car-following at **0.2326 s** and then called 8192 of them a Day.
[`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md) inverted that
chain: a Day is 24 in-world hours and **a Tick's duration is derived from `TICKS_PER_DAY`**. Under the
new chain the commute's length in Ticks is not fixed — it scales with the constant, and the ratio
`adr/0019` calls *"the only time-related quantity in the design that is about the world"* is invariant:

```
commute share of a Day  =  (D × 60 × TICKS_PER_DAY / 86400)  ÷  TICKS_PER_DAY  =  D × 60 / 86400
```

**The constant cancels.** A 20-minute commute is 1.39% of a Day at 8192 and 1.39% of a Day at 2048.
There is no doubling of vehicles, no earlier congestion and no wider road. `adr/0019`'s conclusion
survives its own test and fails the derivation it was written on — the opposite way round from the last
time, when `adr/0082` kept the number and replaced the reason.

**So the objection is answered rather than overruled**, which matters because `adr/0019` is right about
the shape of the danger. What it forbids is tuning a system-wide outcome through a hidden global with no
causal story — *"structurally the same object as SimCity's RCI scalar"*. That is a real prohibition and
it still binds. What this ADR changes is not an outcome. It is how fast the world is stepped through.

**This is the third time in two days a re-derivation left a consequence standing on nothing.**
`adr/0089` named the shape: ***the obligation a deletion creates is a re-derivation, not a retraction.***
`adr/0075` removed a Leg's path and deferred `adr/0041`'s volume without saying so. `adr/0082` replaced
`adr/0019`'s clock and left *Why shortening the Day is not a pacing change* in place, unmarked, still
reading as current — **and it left a live wrong instruction in a revisit trigger**, which the next
section is about.

### `adr/0019`'s revisit trigger now points the wrong way

> *"If profiling shows 8192 Ticks/Day of vehicles-in-flight is unaffordable, then 4096 with doubled
> vehicle speed is not a compromise — it is the correct response."*

Under `adr/0082` this makes the problem **worse, by the factor it was reached for to improve.**
Vehicles in flight is `arrival rate × journey duration`; lowering the constant leaves both alone in
in-world terms, so the population on the road is unchanged. What moves is the **sub-step ratio**: car
following needs a fixed in-world Δt, so a Tick spanning four times more in-world seconds must integrate
four times more sub-steps inside it. Halving the Day does not halve the vehicle cost; it doubles it.

That instruction has stood since `adr/0082` shipped and nobody read it after the inversion, which is
[`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s failure on an
axis that ADR does not cover — not a description of the **build** going stale, but a **consequence** of a
decision whose premise was replaced elsewhere. It is `plans/0012` *Cause 2* with the write landing in the
wrong half of the file.

### What the change actually buys, and it is the only thing it buys

**Four times as much world per hour of play.** Every duration the player cares about at the twenty-hour
horizon is denominated in Days:

| | 8192 | 2048 |
|---|---|---|
| One Day at 1× | 8m32s | **2m08s** |
| Days in a 20-hour campaign at 1× | 140 | **562** |
| Days in a 2-hour session at 1× | 14 | **56** |

`01 §4`'s twenty-hour marker asks the player to read **Replacement Rate** — a demographic quantity that
needs generations to move. At 140 Days a twenty-hour campaign does not contain one generation at any
plausible Life Stage length; at 562 it contains several. Under `01 §1`'s rule that *the hour markers are
expectations, not a script*, a marker is a prediction the design must make **plausible** — and under the
old constant this one was not plausible at all: **the deepest skill the game asks for was scheduled to
arrive after the campaign ended.**

**The speed ladder cannot supply this, which is `adr/0019`'s one substantive error of fact.** It says
what shortening the Day buys is *"available free in another currency"*. It is not free: a speed
multiplier buys world by running more Ticks per second, and a Tick costs what a Tick costs. `TICKS_PER_DAY`
buys world by putting more of it **inside** a Tick, and the two are only interchangeable for the things
that are Tick-denominated. For everything denominated in in-world time they are not interchangeable at
all — which is the same distinction that makes the class table at the top of this ADR necessary.

### Why the classes are not scaled evenly

The obvious move is to divide every Tick-denominated number by four and call the world unchanged. It is
wrong, and the reason is that the two families are authored against different things.

A `rate` of 64 Ticks was chosen so that a Rule fires **four real seconds apart at 1×** — a pace the
player watches. Dividing it by four holds its in-world meaning and quadruples its cost. Leaving it holds
its cost and its visible pace and quadruples its in-world period. **The second is right**, because the
authored intent was the visible pace: `rulesets/minimal.toml` says in its own header that it models no
city, so its periods encode nothing about bread.

The test to apply per key is not *what did this mean in-world* but **does it still resolve the in-world
process it samples**. A Map Layer diffusing every 64 Ticks now advances in 45-minute in-world steps
against a decay period of one Day — about 32 steps per decay, which resolves it. A cadence that failed
that test would be rederived; none in the shipped Rulesets does.

**Goods quantities are the exception and they scale ×4.** A Rule that moves *n* units every 64 Ticks now
moves them over four times more in-world time, so the *rate in Goods per Day* fell by four. That one is
denominated in-world — a Household eats a fixed amount a Day — so the quantity moves and the cadence does
not. **This is the only rescaling in the change**, and it is a Ruleset edit rather than a code one.

### The costs, in full, and two of them are large

**Routing costs 4× per real second.** A route search fires per Trip, Trips are per-Day, and Days arrive
four times faster. [`plans/0013`](../../plans/0013-tick-budget.md) sums to **≥17.8 ms** — ≥114% of a
15.6 ms budget — with **9.4–10.5 ms** of it routing; holding everything else flat, the ledger becomes
**~47.6 ms**, which is **~305% at 4× and ~76% at 1×**. ⚠ *Those are one bill and two rungs, not two
results: **carry the bill, not the percentage** (`plans/0013`).* The simulation as priced still fits where the game is designed to be played and no longer fits at
2×. That row is the ledger's weakest — R6.3 found its multiplicand counts the wrong event and R7 found
its unit is a maximum with a 9.4–10.5 ms spread — but ×4 on the largest uncertain row is not nothing, and
it is recorded here rather than discovered later.

> ⚠ **AMENDED 2026-08-16 by session T ([`plans/0027`](../../plans/0027-session-t-the-target-speed.md),
> [`0105`](0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md)):
> every ledger figure in the paragraph above is superseded, and this ADR is the reason they were
> wrong.** `plans/0013` never applied the ×4 this section states, so the row it sums stayed at its
> pre-clock value **inside the one file that owns the sum**, while the correction sat in `01 §1`, in
> `CLAUDE.md`, and twice in `plans/0013`'s own sidebar. Re-summed at the shipped clock the ledger is
> **≥44–50 ms on one core of the reference class**, of which routing is **37.6–42.0 ms — 85% of the
> whole bill**; against the target settled by `0105` that is **283–318% at 4×**, replacing *~47.6 ms*,
> *~305%* and *~76%*. ***A correction attached to a number does not travel with it any more readily
> than a caveat does*** — `plans/0012` **Cause 5**'s third form, and the only document that was wrong
> is the one that owns the sum, so **no document-to-document check could ever have seen it**; the only
> instrument that finds this is somebody re-summing the table by hand, and nothing schedules that.
> **Carry the bill and the machine class, never the share alone**
> ([`0106`](0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)).

**The Microscopic Cap's per-Tick cost rises 4×.** The sub-step band goes **21–45 → 84–180**, so a Vehicle
held Microscopic costs four times more per Tick.

> **⚠ CORRECTED 2026-08-13, the same day, by [`0096`](0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md) — this paragraph originally claimed a gap of 27–58×. It is 7.3×.**
>
> **Two errors, and neither was the figure.** 186,624 was quoted here as *"~186,600 demand estimate"*
> with **no clause at all**: it is 2,592 Segments over an 80% stress threshold × 72 Vehicles a
> Microscopic Segment, and it is an **upper bound**, because R2's uniform origin-destination draw is the
> longest-trip distribution available — a qualification `adr/0082` and `plans/0002` both state and this
> paragraph dropped. **And the supply side was priced at 15.6 ms while the clock moved**, which
> double-counts, since the budget scales on the same ladder.
>
> At `0096`'s basis — the **design speed**, one core — the ratio is **1.8× at 8192 and 7.3× at 2048**.
> So the sentence above is right that this change costs 4× here; what was wrong is where it lands. A 7.3×
> gap against an upper bound, on one core, for an unbuilt kernel, with a 2- and 4-thread measurement
> owed, is a thing to watch and not a crisis — and [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)
> still forbids designing against it until a real stressed-Vehicle count exists.

**Within-Day scheduling resolution falls 4×, and it is the whole of what sets this constant.** `adr/0082`
found that of `adr/0019`'s three reasons for declining a shorter Day, visual honesty falls with the
65×-adrift column it rested on and queue fidelity moved to the sub-step ratio, leaving the makeweight
standing alone. So the argument for 2048 has to be made on it directly: the finest within-Day event this
design schedules is a **departure**, the commute window is `TICKS_PER_DAY / commute_peak_factor` = **683
Ticks** at the shipped factor of 3, and departures spread across 683 buckets at 42.19 in-world seconds
apiece. Nothing in the design asks a question finer than that — there are no signals, no timetable and no
shift boundary — so the resolution is sufficient with two and a half orders of magnitude to spare.
**That is the case for 2048 and it is the case that would have to fail to move it.**

**One instrument gets coarser.** `--trips` can resolve a duration no finer than one Tick, which goes from
0.176 to **0.70 clock minutes**. 5b-bis task 7 found that formatter truncating the sub-Tick fraction —
7% on the 2.5-minute band the crossing cost's ratifier is read off — and the same defect under this
constant would be four times larger. It is fixed, and the histogram's shortest band is now 3.6 Ticks
wide rather than 14.2.

## Consequences

**`TICKS_PER_DAY = 2048` and `WHEEL_SIZE = 2048`.** The two move together, and `adr/0019`'s note that
their equality is *"a coincidence worth stating"* holds: the wheel is sized by the longest routine sleep,
which is bounded by one Day, and one Day is now 2048 Ticks. Multi-Day Life Stage countdowns exceed any
wheel and need the overflow tier exactly as before.

**Every hash-bearing number denominated in Ticks keeps its value and changes its in-world meaning**, and
that is a design change under `05 §4` even though no Ruleset text is edited, because the world the
numbers describe is different. All three golden baselines are re-recorded. **The Goods quantities that
scale ×4 are a second, separate Ruleset edit** and should be a commit of their own so the two are
separable in the history.

**The Commute Budget is denominated at 1.4222 Ticks per clock minute**, down from `adr/0082`'s 5.6889.
Nothing authored in minutes changes; `TravelTime` is Q16.16 and carries the fraction.

**`01 §1`'s speed ladder gains a 3× rung and keeps 4×.** The refusal rule that produced *there is no 8×*
is restated in that section: it is keyed on **events the player can perceive per second**, not on a Day's
length in seconds, and under the class table above the event rate per real second does not move at all.
What does move is that **Day-scaled phenomena have a lower top watchable speed than everything else** —
the commute peak at 4× is an 11-second event — which is `01 §7`'s Study argument on a second axis rather
than a new problem.

**`adr/0019` is amended a second time**, and this time its conclusion falls rather than its derivation.
The banner marks *Why shortening the Day is not a pacing change*, the *"twice the vehicles"* claim, the
*"free in another currency"* claim and the revisit trigger's direction.

**`adr/0082`'s *"`TICKS_PER_DAY = 8192` stands"* is superseded and its two clocks are not.** The
behavioural-clock-plus-sub-step structure is what makes this change possible and is untouched.

**`plans/0013` gains a routing multiplicand of ×4** and a note that its 15.6 ms column is now the 4×
rung of a five-rung ladder with 20.83 ms at 3×.

**The three-class rule is the reusable part.** It is stated here rather than in a fourth inference ADR
because it answers a narrower question than `0043`, `0052`, `0070` and `0093` do — those govern what a
sitting may conclude, and this governs what one arithmetic change touches. If a second global constant
ever moves, this table is what to read first.

## What building it found

**Built 2026-08-13, `plans/0003` queue item 7.** `Ticks.PerDay` and `EventWheel.Size` are 2048; all
three golden baselines re-recorded; 1,279 tests green.

### The rescaling inventory was wrong in two places, and both would have shipped in silence

This ADR says the Goods quantities are ***"the only rescaling in the change"***. There were three, and
**two of them move down by four while the Goods move up by four**.

**`Speed.PerKilometrePerHour`, in code: 48,000 → 192,000.** A road's free-flow speed is authored in km/h
and stored in Q16.16 Tiles per Tick, so the conversion factor depends on how long a Tick is. It was a
**literal**, with the derivation written out in the comment above it. Left alone, an authored
`walk_speed_kph = 5` would have walked the city at **1.25 km/h** — every commute four times longer in
clock minutes, and the Commute Budget refusing very nearly every job. **Nothing would have failed to
compile and no Ruleset text would have been wrong.**

Why the table above missed it: **a conversion factor is denominated in two units at once**, so it
belongs to none of the three classes. The table has a fourth row now.

**`revisit_ticks` and `pollution_decay_ticks`, in three Rulesets: 8192 → 2048.** Both are durations the
files themselves describe as *one Day, TICKS_PER_DAY*. Kept at their numbers they would quietly have
begun meaning **four Days** — a plume outlasting the week, a development industry surveying the city
once a fortnight. `pollution_decay_ticks` was covered by the *in-world* row all along and simply was not
enumerated; `revisit_ticks` was **actively misclassified** and is struck from the Ticks row above.

Why the table missed that one: **it read the classes off the key names**, and a key spelled `_ticks`
whose meaning is a Day looks Tick-denominated from outside. ***The name of a quantity is not its
denomination***, which is the same shape as the conversion-factor miss one level down.

**The fix in every case was a derivation, not a new value.** `TravelTime` had always written its half as
an expression, and its own remark says *"the same derivation `Speed` runs, and if one moves both move"*.
**One fact, stated in two files, spelled as an expression in one and as a value in the other — and the
value is the copy that drifted.** `plans/0012` **Cause 1**, in code rather than in prose. The metre and
the second now live in one place each (`Tiles.Metres`, `Ticks.SecondsPerDay`), which is what let the
factor be written as arithmetic at all.

### `amount` and `capacity` are both in Goods and belong to different rows of the table above

**The Bin capacities had to move with the Goods: `sundries` 12 → 48, `repairs` 1 → 4.** This ADR says
the Goods quantities scale ×4 and treats *the Goods quantities* as one group. They are two, and **the
unit is what made them look like one**.

- **`amount` is Days-denominated.** What must hold is Goods per **Day**, and a Day now holds a quarter
  as many firings, so the per-firing number goes ×4. That is the *Days* row.
- **`capacity` is Ticks-denominated.** What must hold is **firings held** — `capacity ÷ (amount ×
  occupants)` — and firings × `rate` is a **duration**. That is the *Ticks* row, where a number is
  normally kept as written. This one is written in Goods, so keeping the quantity still **requires
  changing the number**, and it follows `amount` for that reason and no other.

***The unit of a quantity is not its denomination***, which is the sibling of `revisit_ticks`' lesson
one heading up — *the name of a quantity is not its denomination*. **Twice in one build, the table was
wrong because a class was read off a surface form**: first a key name, then a unit.

**What the shortfall looked like.** Left at 12 against a 12-unit firing, the larder held **one** firing
where it had held four. In in-world time the two are identical — 22.5 minutes of supply either way — so
measured in Goods nothing was wrong. Measured in **firings held**, every Bin in the game shrank by four,
and a Bin that must be *exactly full* for one `consume` to succeed is a knife edge. It fired
`Invariant.WaiterIsBlockedByTheBinItNames` and exposed a live defect in the wake path, filed unfixed as
`plans/0003` queue item 8.

**Doing it was the other half of a trade this ADR already made.** *What it costs* above considers
dividing every `rate` by four — holding transaction size and paying four times the evaluation cost — and
rejects it **on cost**. Having taken the coarse-transaction side, resizing the buffers is what that side
owes. The consequence was never written down, so the first Ruleset to meet it met it as a crash.

### ⚠ And the rescale was measured behaviour-neutral, against a first claim that it was not

**The commit shipped saying *a dwelling now holds 90 in-world minutes of sundries where it held 22.5,
which is a change to the world rather than a neutral rescale*. That is wrong, and the measurement is
flat.** Running the pre-rescale Ruleset and the shipped one for 4,096 Ticks gives a **byte-identical
86-line census** — buildings 1,201 → 642, the Unplaced Pool, rule evaluations, Trips, every row. Only
the State Hash trace moved, because Bin levels are literally four times larger. Every quantity scaled
and every `rate` stayed put, so every Rule fires on exactly the same Tick it did before.

**The 90 minutes is this ADR's own cost, arriving in one more place.** The larder is 4 firings × `rate`
32 = **128 Ticks**, and it was 128 Ticks before the clock moved as well. 128 × 42.1875 s is 90 minutes;
128 × 10.546875 s was 22.5. ⚠ ***The figure is HEAD against the pre-clock world, not against the
previous commit***, and the sentence never said which — `plans/0012` **Cause 5**, *a number that is one
half of a ratio says which half*, committed by the author of the paragraph that coined it.

**Nothing in the file came out of proportion, and the reason is that everything in it is denominated in
firings** — including `condemn_after`, which says so in its own comment. A dwelling's whole life is 4
missed `upkeep` firings × `rate` 16 = **64 Ticks**, which went 11.25 → 45 in-world minutes by the same
×4. The clock stretched the file uniformly.

**A sweep then found the larder is a cost dial rather than a balance dial in this fixture.** At
capacities 24, 36, 48 and 96 **no row of the census moves at all**; only `rules due` and `rules
evaluations` move, and they move **up** with capacity — 20,134 → 25,023 evaluations at the first reading
going 48 → 96 — because a deeper larder gives `restock` more headroom to keep working. **A bigger buffer
costs more, not less.** At 12 the city is likewise unchanged and simply crashes. The reason the larder
decides nothing is that **it is deeper than the building's lifespan**: nothing in this Ruleset produces
`repairs`, so `upkeep` condemns every dwelling at 64 Ticks and the 128-Tick larder is a shock absorber
for a shock that never comes.

**So 48 stands and no number was tuned to make it stand.** Restoring 22.5 in-world minutes would mean
either 2 firings held — one above a known crash — or quartering `consume`'s `rate`, which is the option
refused above on cost. ***And 22.5 was never chosen***: it is `capacity = 12` in a file whose first line
says it models no city. `adr/0070` — restoring it would be compensating for something that was never a
decision.

### `adr/0071`'s two illustrations moved in opposite directions

[`0071`](0071-travel-time-is-sub-tick-and-q16-16-is-a-scale-rather-than-a-meaning.md) rests on two
readings, and this constant moves them apart.

- **The sub-Tick claim got four times stronger.** A 32-Tile Street at 50 km/h was **0.87 Ticks** and is
  **0.22**. Under whole-Tick resolution every Street in the city would now be **free** rather than merely
  cheap, so the argument that ADR was written on is more load-bearing than when it was made.
- **The flooring error got four times smaller.** A 5 km/h walk was **3.66 Tiles/Tick**, where flooring to
  whole Tiles cost **20%**; it is **14.65**, where it costs **4.4%**. A fixed rounding step is a smaller
  share of a larger number.

***One constant, two of an ADR's arguments, opposite directions*** — which is why `adr/0071` is not
re-derived from either. Both readings are asserted in `TravelTimeTests` with the movement written beside
them.

**A third, smaller:** halving a speed no longer exactly doubles a traversal cost. It differs by **1** in
Q16.16 — `1/65,536` of a Tick, **0.64 ms of in-world time** — because a floored quotient doubles only
when the fraction falls right, and it did at 8192 by luck. The test asserts a one-ULP tolerance now. This
is the *"one instrument gets coarser"* consequence above, showing up in the simulation's own arithmetic
rather than only in `--trips`.

### Five tests were asserting a number where they meant a relation

Every one of them named a value that is a function of `Ticks.PerDay`, and none of them could say so —
three were `[InlineData]` literals, where an attribute argument cannot be an expression.
`The_departure_window_is_derived_from_the_peak` now asserts the **definition of a ceiling division**;
`A_trigger_evaluates_exactly_its_derived_sample` asserts that a hundred times the Lots is a hundred times
the sample; the pollution tau is computed from the period. ***Restating the old numbers would have left
the same trap set for the next change.***

**And one instrument was reading one instant where it meant a run — while coverage went *up*.**
`GoldenSessionCoverageTests` counts commute Trips in flight at the final Tick, and found **zero**. Not a
regression: the departure window is `ceil(TICKS_PER_DAY ÷ commute_peak_factor)`, which fell 2,731 → 683,
so the session now covers **every** departure phase instead of three quarters — and a Trip is in flight
for a quarter as long, so by Tick 2048 the Day's commuting is over. **The baseline got better and the
assertion measuring it went to zero.** It samples eight points across the run now.

**Two long-run tests broke on the same axis, and neither was measuring what it claimed.**
`LayerLongRunTests` reads a contraction — the sweep-to-sweep step shrinking toward zero — from a window
starting fifteen sweeps in. The pollution tau fell 128 → 32, so the field converges four times sooner in
sweeps and that window now contains **no transient at all**: it read `0 → 0` and failed, *not because
nothing converged but because it had finished converging before the instrument started looking*. The rise
is read from the start of the run now. And `LotLongRunTests`' vacuity guard — *did laying the Street
carve any Lots?* — fired on a run where **41 of 97 edits carved Lots perfectly well**, because it read
`afterLay[0]` alone and the Zone Rule's quadrupled sample had put Buildings on that face by the first
edit. ***A guard against vacuity that reads one sample can itself be defeated by timing***, which is the
fourth time in two days that a single draw has been mistaken for a property.

### What did not move, and one thing that did

`--commute`'s cost histogram keeps its shape and its mode — the 8–16 minute band — which is this ADR's
*"nothing authored in minutes changes"* holding end to end from a Ruleset's `20` to a printed
distribution. `CommuteRoster` simply allocates fewer buckets. `LayerSchedule.DefaultPollutionDecayTicks`
was already `Ticks.PerDay` and needed nothing.

⚠ **Employment on the shipped Ruleset fell from 6,844 to 2,791 of 10,000 over 2,048 Ticks**, and it is
the `revisit_ticks` decision rather than the clock: the Zone Rule now surveys the whole city every Day
instead of every four, so it condemns four times as fast per Tick and the standing stock is smaller.
That is a property of a file whose own header says it models no city, and it is recorded rather than
tuned.

## What would trigger revisiting

**Playtest, and the two numbers are named.** 2048 is a pacing choice and the only instrument that
settles it is a person playing. It is **too fast** if a player at 1× cannot complete one turn of the
`Observe → Diagnose → Intervene → Wait` loop inside a Day — the Day is 128 real seconds and the loop's
own cadences are 64 — and it is **too slow** if a twenty-hour campaign still does not contain enough
generations for Replacement Rate to be readable, which at 562 Days means a Life Stage life longer than
about 190 Days. **Lower is expected to be the direction if it moves**, and the floor is the departure
window: below about 512 Ticks a Day the commute peak stops being a spread and becomes a step.

**Within-Day scheduling resolution acquiring a finer consumer.** Signals, a timetable, shift boundaries
or anything else that asks what is happening inside 42 in-world seconds refutes the section that carries
this decision, and it should be reopened on that rather than on cost.

**The Lane kernel being built and the Microscopic Cap turning out to bind in ordinary play.** No gap has
been measured — see `0096` — because the Cap's demand side does not exist. If a real traffic model
produces a stressed-Vehicle count above the supply figure and `adr/0007`'s *"it scales"* clause fails,
the levers in order are **threading** (unmeasured, up to 4× on the supply side), then the **sub-step
ratio** per `adr/0082`, then this constant.

**Routing measured in a real city.** `plans/0013`'s routing row has never met a running world. If a
measurement lands near its guess, ~305% at 4× makes 4× a rung a large city withdraws early, which is
`HONEST DEGRADATION` working as designed; if it lands well above, the ledger and not this constant is
what has to move.
