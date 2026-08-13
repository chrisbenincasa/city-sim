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
| **Ticks** | Rule `rate`, `revisit_ticks`, `interval`, the Map Layer cadence | **kept at its number**, so unchanged in real seconds and ×4 coarser in-world |
| **Days** | Life Stage countdowns, the demographic arc | **unchanged in Days**, so ×4 faster in real time — which is the point of the change |

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
four times faster. [`plans/0013`](../plans/0013-tick-budget.md) sums to ≥114% of a 15.6 ms budget with
60–67 of those points routing; holding everything else flat, the ledger reads **~305% at 4× and ~76% at
1×**. The simulation as priced still fits where the game is designed to be played and no longer fits at
2×. That row is the ledger's weakest — R6.3 found its multiplicand counts the wrong event and R7 found
its unit is a maximum with a 9.4–10.5 ms spread — but ×4 on the largest uncertain row is not nothing, and
it is recorded here rather than discovered later.

**The Microscopic Cap's per-Tick cost rises 4×.** The sub-step band goes **21–45 → 84–180**, so a Vehicle
held Microscopic costs four times more per Tick.

> **⚠ CORRECTED 2026-08-13, the same day, by [`0096`](0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md) — this paragraph originally claimed a gap of 27–58× and there is no gap, because there is no denominator.**
>
> **The demand figure was 186,624, which is S2 R2's synthetic *fleet* and not a stressed count**, and
> `plans/0013` labels it so in the table cell and warns in the same section that *"a number becoming a
> decision by being the only number in the room"* is where that table would fail. It did, here, one day
> later. **And the supply figure was priced at 15.6 ms while the clock moved**, which double-counts: the
> budget scales on the same ladder. At the design speed the Cap's supply side is **~25,400 Vehicles on
> one core**, which is *exactly* what the old clock gave at its own top rung — so under `0096`'s basis
> this change moves the Cap by **nothing**, and every figure is still one core with a 2- and 4-thread
> measurement owed.
>
> What remains true is the sentence above it: the per-Tick cost quadruples, the Lane kernel is unbuilt,
> the Cap is unset, and [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) forbids
> treating any of it as a constraint until a real stressed-Vehicle count exists.

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
