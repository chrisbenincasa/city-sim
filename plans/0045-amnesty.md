# 0045 — The amnesty

**Read this, not the board. The only thing in flight.**

Opened 2026-08-26. **Ends at a ratio, not on a date: 30 words of prose per line of simulation.**
**57 at 2026-08-30.** `CorpusBudgetTests.The_amnesty_has_not_yet_earned_its_end` goes red the day
it is earned, and that red is the report. One page, and it stays one page.



---

## The situation

**1.17M words of prose against 17,872 lines of executable simulation.** 169 ADRs, one per 106 lines,
30 of them in five days. **236 of 524 commits changed no code.**

🔴 **And that count was low by 636,000 words.** Doc-comments under `src/` and `tests/` are **35% of
all the prose here** and no corpus check could see one: `CLAUDE.md` says the checks are all
document-to-document. The three `Borough.Core` files the amnesty bought are **56–66% comment by
line**. Counted since 2026-08-30 — *the ratchet was reporting a win on the half it could measure.*

Nobody ages, is born or dies. Wages are unbuilt, so money flows one way into Businesses. Every
shipped world decays. No renderer, no plan for one. The corpus grows with elapsed time and has no
sink — `adr/0006`, violated by its citers.

---

## Standing orders

Until the ratio is earned:

1. **No new ADRs.** `CorpusBudgetTests` reddens the build if `docs/adr/` passes 169.
2. **No new entries in `plans/0002` §A–§F.** Frozen at its 2026-08-26 size.
3. **Prose grows only as fast as the simulation it describes.** `CorpusBudgetTests` caps the
   **ratio** — all prose, doc-comments included, over non-comment `src/` lines — at **57**, so a
   page written beside a mechanism is free and a page written alone is refused. ⚠ **This is not a
   claim that a remark beside code is waste**: `adr/0093` asks for exactly that prose, and doc-
   comments are on the numerator only so that prose cannot escape `docs/` by relocating.
   🔴 **It replaced two absolute word ceilings on 2026-08-30**, which went red on all four commits
   that *improved* the ratio and were raised by their own author every time. ***A ceiling that
   reddens on the work paying down the debt is measuring the wrong side of the fraction.***
4. **`adr/0043` and `adr/0052` are suspended.** Choose numbers by taste, stamp them `PROVISIONAL`,
   open no §D row, name no ratifier. *Ratification needs a city and the city needs the numbers, so
   they now prevent every commitment, not only premature ones.*
5. **A session ending with no change under `src/` is not committed.**

To break one, delete its test in a commit saying why — visible, not hard.

---

## Owed when the freeze lifts

🔴 **THE AMNESTY COULD REFUSE A QUESTION AND HAD NOWHERE TO RECORD REFUSING IT.** Standing order 2
seals `plans/0002` §A–§F, which is *the* document for **what needs answering** — so a question raised
under the freeze had a correct home it was barred from, and the only place left was a findings
section nobody reads once the board stops pointing here. ***A freeze with no deferred list loses
exactly the questions it was right to postpone.***

**This list is that sink. Each row is filed in [`0002`](0002-open-questions.md) on the day standing
order 2 expires, and struck from here.** ⚠ **It is a queue and not a discussion** — a row names the
question, its type and its trigger, and never argues it.

| Question | Type | Trigger to open it | Raised |
|---|---|---|---|
| **What is the natural playing speed, and what does a player do at each rung?** Two sittings settled that ***slower rungs are necessary*** and nothing about where the natural one sits. What is owed is four things at once: the **phases** — early, mid, long — and whether they are a design claim or a description; the **cadences that have emerged** and which phase each belongs to; **which rung a player reaches for and when**; and whether *fun* and *legible* want the same rung. ⚠ ***A ladder is a list of speeds; this is a question about what a player is doing at each one.*** Goes to §C under `01-player-experience.md` | *arguable* — no number refutes *this is the speed the game is played at*, and `adr/0094`'s revisit trigger names **a person at the controls**, which is a sitting rather than a machine | **A game with phases in it.** Asking now answers from a city that has one phase, and ***a pacing question answered against a world with nothing to pace is a stopwatch reading rather than a design*** | 2026-08-31, the shell's second sitting |

---

## Definition of done, amended

Hexadecimal was satisfying `CLAUDE.md`'s *"something to look at"*.

> **Done means you watched it and were surprised.**

---

## The queue

Ordered; reordering deletes this.

🔴 **Rows 8–11 were written and replaced within the hour, and the reason is a trap worth keeping.**
The first set was a dwelling sink, a Ruleset window, a tenant's middle and a renderer — chosen
because a failing test or an instrument named each one. ⚠ **But a queue assembled from failing tests
finds only the mechanisms that HAVE tests, and nothing unbuilt has one.** Every row was a repair to
something that already ran. ***This page was opened to build the simulation, not to finish it.***
The rows below were found by asking the code what is missing instead.

| | Work | State |
|---|---|---|
| 1 | `CorpusBudgetTests`, this page, the `CLAUDE.md` pointer | ✅ 26-08 |
| 2 | Write `Citizens.Activity` — saved, hashed, per-Tick, **no writer** | ✅ 26-08 |
| 3 | `--day` — one Citizen, one Day, off `Evidence.OfCitizen` | ✅ 26-08 |
| 4 | ~~Nobody comes home~~ — misread; real defect fixed | ✅ 27-08 |
| 5 | ~~Held parking drifts upward~~ — a ramp, not a drift; `ParkingLongRunTests` owns it | ✅ 27-08 |
| 6 | ~~Wages~~ — `waged.toml`; arrears got a sink | ✅ 27-08 |
| 7 | Life Stages and self-generation — [`0046`](0046-life-stages-and-a-self-generating-population.md) | ✅ 5/5 |
| 8 | **`Govern` throws.** `PolicyEngine.Sweep` runs and a Ruleset can declare a `[[policy]]` — but the verb letting a **player** set one hits `InvalidOperationException` at `Simulation.cs:440`. ⚠ **Two of the six verbs are declared and unapplied**; this is the one with a whole mechanism already sitting under it | ✅ 30-08 |
| 9 | **Needs, and the preference axes.** `Taste` **0 files**, `Preference` **0 files** in `Borough.Core`. `adr/0027` calls them *"the most load-bearing data in the design"*. Until they exist a Household wants nothing a Bin cannot express, and placement satisfices on **distance alone** | ◐ 30-08 |
| 10 | **`Service` throws.** The civic swath — schools, health, safety. `School` is **0 files**. ⚠ **After 9, not before**: a service with no need to satisfy is a Building with a Bin | ✅ 30-08 |
| 11a | **The eye.** Of `05 §2`'s three hot queries only `LayerCells` exists; **`VisibleAgents` and `ChunkAggregates` are nowhere in `src/`** and a **Traveller has no coordinate at all**. ⚠ **Day one of 11b either way** | ✅ 30-08 |
| 11b | **The shell.** `src/Borough.Godot` — Godot 4.7.2, a camera over the city, one MultiMesh per kind of thing, a speed ladder and a readout. ⚠ **Not in `Borough.slnx` and never will be**: that absence is what enforces *the headless runner never requires Godot* | ✅ 30-08 |
| 12 | **Disasters.** `coastal.toml` carries a Hazard Region and **nothing fires on it** | ✅ 31-08 |
| 13 | The `0046` loose ends — the dwelling stock's missing sink, `aged.toml`'s narrow windows. ⚠ **Small on purpose and last on purpose**: `StageDumpTests` pins both with tests that assert the defect, so neither can be lost | |

Items 2 and 3 cost one day and added no Ruleset key, number or ADR. They moved three golden
baselines: a hashed column stopped being zero (`adr/0100`).

## What `--day` found

🔴 ***Nobody comes home* was WRONG** — 468 arrive home a Day; that came off a midnight sample.

**The real defect:** 163 Citizens a Day set off *home* from home, 69 *for work* from work.
`CommuteEngine.Travel` walked both roster lists and never asked where the person was, so **a quarter
of this city's commuting went to where the Citizen already stood.** Invisible until `Activity` had a
writer: home-to-home is a Trip like any other. `CommuteDirectionTests` holds it.

⚠ **It failed three other tests, none a regression**: one horizon too short, two bands calibrated on
phantom traffic. ***Figures off those instruments before 2026-08-27 were partly paid for by a
defect.***

## What `hungry.toml` found

**Sustenance and Satisfaction ship** — two saved columns, one writer (`RuleEngine.MoveNeed`), a
`[[resource]] need` key and a `[needs]` table. Education and Health are refused **by name** and filed
in `docs/deferred.md`. `--day` reports the reading and the city's hunger beside it.

🔴 **And watching it ran the mechanism into a wall: the Need is a TALLY where `04 §6` step 6 asks for a
duration.** It moves on a failed *occasion*, and a blocked Rule has **one** — `RuleEngine.Stop`
subscribes it to the Bin and it sleeps until that Bin changes, which on a world nothing restocks is
never. So *a dry afternoon and a dry month* read **-4** alike, which is the sentence the mechanism was
built from, inverted.

**Measured on `hungry.toml`, 2,000 Citizens over 32,768 Ticks:** deepest **-44**, mean **-28** over
720 Households — **7 degrades each**, where `consume`'s rate of 32 would give 1,024. ⚠ **The 7 is the
rehousing count.** The census reads *tenancies ended* at 13–30 per 64-Tick window over 512 readings,
≈6,600 city-wide, ≈9 a Household; each rehousing builds a fresh Bin and buys exactly one more failure.
***The column is counting tenancies, not hunger.*** 🔴 **The first reading of this said 30 tenancies
total and the attribution was withdrawn for an hour** — the census columns are *first/last/low/high* of
a per-interval **sum**, not a run total, which is `plans/0012` **Cause 5** read off a table header.

***This is `adr/0053` arriving a second time, one subject along*** — the Building's pressure clock was
rebuilt as a duration for this reason and the Household's was not.

✅ **FIXED THE SAME DAY.** `RuleEngine.RefreshNeed` recomputes the depth from `tick − StarvedSince`
on `SweepNeeds`, a daily pass. ⚠ **It RECOMPUTES rather than accumulating**, which is what makes a
staggered pass sound: the depth is a function of the duration, so a Household first visited ten Days
in arrives at the right depth in one step. **The period is derived** — the Ruleset states a degrade
*per Day*, so a Day is where the depth is exact and no cadence number enters the design.
⚠ **Its own pass rather than a ride on the Zone Rule sweep**: `Condemn` reaches a Building only
through a *sample* and only when its kind declares `condemn_after_days` or `tenancy_ends_after_days`,
so hunger would have been a silent function of two keys about Buildings. ***A Household mechanism must
not depend on a Ruleset having opinions about Buildings.***

**Re-measured:** deepest **-31**, mean **-10** — the depth is now days-since-fed, and it is bounded by
`evicted.toml`'s own cycle rather than by the mechanism. `NeedTests.The_depth_is_a_duration_and_not_a_tally`
is the guard; ⚠ **every other test in that class passes against the tally.** 🔴 **A new Tick consumer
with no `plans/0013` row** (`adr/0073`) — the corpus freeze is why.

## What the first frames found

**`--watch` prints the city as ASCII** — Buildings under the Travellers moving over them, scaled to
the Lots that exist. `VisibleAgents(aabb, alpha)` is `05 §2`'s second hot query and the first thing
in the project that answers *where is everybody, right now*.

🔴 **THE DAY IS A COMB AND NOT A CURVE.** Measured on `minimal.toml`, 1,000 Citizens, sampled every
Tick: the morning has **five departure bursts** — peaking at Ticks 486, 570, 652, 733 and 820, one
per in-world hour — with **50–59 consecutive Ticks of a COMPLETELY EMPTY road network** between
them. A Shift starts on an integer hour and the window is 6–10, so there are **five** possible
departure clumps and `arrive_early_max_minutes = 15` is the only thing spreading each one.
***`adr/0101` says the Day's shape is emergent, and what emerges from an hour-granular Shift start
is a comb.*** At a million Citizens that is a fifth of the city departing inside 21 Ticks, five
times a morning.

⚠ **14 of 24 hours have nobody outside at all** — nothing moves before 05:00, after 19:00, or
between 10:00 and 12:00. That is the commute being the only built Trip generator, seen rather than
read.

🔴 **A FRAME EVERY 32 TICKS REPORTS AN EMPTY CITY**, which is the job cadence aliased against itself
and cost the first four readings. ***An instrument sampling a paced mechanism has to pick an
interval coprime with the pacing***, and nothing warned; the dump's header does now.

⚠ **A walker is placed on a straight line it did not walk, and a driver is placed on its Segment.**
A foot Leg is priced once and holds no Segment, so the only thing stored about a walking Traveller
is that it left one Address for another — and **nine shipped worlds have `car_ownership_percent`
absent or 0**, so this is most of the city. The frames show it: walkers cut through blocks, drivers
ride the lattice. `alpha` therefore moves the walkers and cannot move the drivers.

✅ **Placement is total** — `placed` equals `travelling` on every frame of every world tried.

⚠ **A new Tick consumer with no `plans/0013` row** (`adr/0073`) — the corpus freeze is why.

## What the shell found

**`src/Borough.Godot` exists** — Godot 4.7.2, the `.NET` build, on `net10.0` against a `net10.0`
core. A camera framed on the built extent, one `MultiMeshInstance3D` each for the Road Graph,
the Buildings and the Travellers, a speed ladder on `space` and `1`–`4`, and a readout. **It is not
in `Borough.slnx`**, so a root `dotnet build` still neither builds it nor needs Godot: ***the
constraint that the headless runner never requires Godot stopped being vacuous and became a check.***

🔴 **THE FIRST FRAME RENDERED HALF A ROAD NETWORK AND THE ASCII DUMP COULD NOT HAVE CAUGHT IT.**
`Basis.Scaled` scales in the **parent** frame, so every east–west Segment was given 8 m of length
and its own length of width, and the lattice drew as north–south lines with no cross-streets.
⚠ ***The two viewers disagree because they ask different questions***: `--watch` rasterises the line
itself and never asks for a transform, so a bug in the transform is invisible to it. **Two eyes on
one mechanism is worth what it cost.**

🔴 **THE WALKERS WERE FLOATING IN THE MIDDLE OF THE BLOCKS.** At Tick 751 — 08:48, the peak of the
third burst, 63 people out — every Traveller sat off the network. That was `VisibleAgents`'
straight-line placement for a foot Leg, stated in its own remarks and *believed*; seeing it is
different. ***A shell is where an approximation you documented stops being a footnote.***

✅ **FIXED, AND THE FIX IS A MECHANISM RATHER THAN A SNAP.** `TripEngine.Plan` passed
`recordPath: driving`, so a walk computed its path and **threw it away** — the search runs either
way and only the `_via` bookkeeping was conditional. It now records for both, and a walker is placed
at the share of its route's *length* that its elapsed time has bought. ⚠ **`adr/0041` is untouched**:
that ADR decides who is attributed **volume**, and `BeginLeg` tests the mode before it looks at
`RouteHead`, so a walk is still priced once and its hops are never entered or left. **Same Tick,
same 63 people, all of them on a street.** `VisibleAgentsTests.A_traveller_stands_on_a_segment`
holds it, and fails on the old placement at the first Traveller it meets.

⚠ **It moves the State Hash** — a walk's hops are saved state — so all three golden traces were
re-baselined (`adr/0100`: that costs nothing while nobody carries a save). 🔴 **The Tick cost is
BELOW THIS MACHINE'S NOISE FLOOR and is therefore unmeasured rather than small**: 4,000 Citizens
over 4,096 Ticks reads **3.27–5.77 s before against 3.38–4.25 s after**, two runs each, which
separates nothing. ***What it costs at a million Citizens is unknown***, and it owes `plans/0013` a
row (`adr/0073`) that the corpus freeze is why it does not have.

⚠ **Confirmed the comb from the other side**: at Tick 402, 04:42, the readout says `travelling 0`
against a fully built city. Nobody is outside before 05:00.

⚠ **Godot loads the `Debug` assembly.** A tree built only in `Release` fails with *Cannot instantiate
C# script*, which reads as a missing class and is a missing configuration.

## What watching it at 1× found

🔴 **THE LADDER IS 675× TOO FAST TO WATCH A PERSON AND THE DOCUMENT SAYS OTHERWISE.** A Tick is
42.19 s of in-world time, so `01 §1`'s **1× runs the world at 675× real time**: a 20-minute commute
is **1.8 real seconds**, a walker crosses a 128 m block in **0.14 s**, and a car crosses one in
**0.014 s** — ***sub-frame at 60 Hz on every rung the document offers.***

🔴 **`01 §1` says *traffic is visually truthful* at 0.5×, and truthfulness is 1× REAL time — 0.0237
Ticks/s, 1/675 of the ladder's 1×.** `§7`'s recorded concession says an untouched speed control
shows traffic *"roughly twice as fast as its apparent size warrants"*; the figure is **675**.
⚠ **Two requirements 675× apart are sharing one ladder** — a Day inside a two-minute observation
window, and traffic that looks like traffic — and the document asserts both. Filed in `plans/0012`
as a Cause 4 sighting; ***not paid here, because choosing what the ladder should be is design.***

✅ **The shell added rungs of its own below 0.5×** and **prints the × real-time figure beside the
rung name**, so the gap is on screen rather than in a table. **1 Tick/s is where a walker becomes
watchable**: a block in 2.2 s, a commute in 28 s, a Day in 34 minutes.

🔴 **THE WHOLE LADDER IS THEN HALVED, AS AN EXPERIMENT AND NOT AS A DECISION.** Every rung is half
`01 §1`'s Ticks/s — 1× is **8** and a Day is **4m16s** — and 1 Tick/s is kept as `1/8×` because it is
the rung a person actually looked at and liked. ⚠ **`01 §1` is still the design and this shell
disagrees with it on purpose**: the tick rate is host-side and runtime-only, so nothing here moves a
hash or settles anything. ***It exists to produce the playtest `adr/0094`'s own revisit trigger asks
for***, which names a person at the controls as the only instrument that settles a pacing number.

⚠ **`adr/0094`'s revisit trigger predicted the wrong direction.** It says *"Lower is expected to be
the direction if it moves"* — a **faster** Day — and names *too fast* as the case where the
`Observe → Diagnose → Intervene → Wait` loop will not close inside one. **The first sitting anybody
ever watched pushed the other way, and for a reason the trigger does not contain**: the loop is
about **events**, and what broke was **motion**. `01 §1` reasons entirely in events per real second
and `§7` knows apparent speed scales with the tick rate. ***Each section holds half the argument and
nobody wrote the sentence that puts them together.***

✅ **The shell takes `--ruleset` and `--citizens`**, so worlds can be compared. 🔴 **The first
comparison is a surprise: `congested.toml` at 4,000 Citizens has 43 Travellers on the network at
Tick 749, against `minimal.toml` at 1,000 with 63.** Four times the population and a third fewer
people out. ⚠ **The cause is that everybody drives there** — a car is ten times a walker's pace, so
each journey occupies a tenth of the time and concurrency falls with it. ***A city where everyone
drives looks emptier than a walking city a quarter its size***, which is a claim about what a
renderer shows rather than about congestion, and no column would have raised it.

⚠ **The camera frames the whole built extent, which is wrong for a two-centre world** —
`twinned.toml`'s lattices each render as a postage stamp. Drag-pan and wheel-zoom were added rather
than a cleverer framing rule, because which centre you want to look at is a question only the
operator can answer.

⚠ **Nothing about the simulation changed.** The tick rate is host-side and runtime-only
(`CLAUDE.md` → Constants), so a rung moves no hash and settles no design question.

## What the school run found

**`Service` is applied**, `Need` is complete at four, and `docs/deferred.md` named the exact trigger
that un-parked Education and Health: *a civic Building a Household draws on*. Nothing was chosen that
the trigger did not supply.

⚠ **`adr/0118` left this verb's payload examined-not-yet and the answer is clean for a reason it did
not anticipate.** It expected *a Building and a catchment* to be the hard part. ***There is no catchment
in the payload at all***: `adr/0032` demoted coverage from mechanism to overlay, so the field that would
not have fitted turned out not to be a field.

✅ **The degradation rule did not have to be CHOSEN, which was the condition of un-parking.** An attended
occasion is a daily **sweep**, not a subscription, so the per-occasion step already *is* the per-Day
rate. ***The asymmetry that forced `RefreshNeed` one item ago was a property of how the occasion
ARRIVES, not of Needs.***

🔴 **THE ENGINE REWARDED A PLAYER FOR NOT USING IT, AND A RULESET HEADER CAUGHT IT.** The first spelling
returned early where no school stood, conflating *this Ruleset has no schools* with *this city has built
none* — so the one city the verb exists to punish was the one where Education stayed pinned at zero.
`schooled.toml`'s header had already stated the opposite in prose. ***A Ruleset gates the pass; the
state of the city never does.***

🔴 **AND `main` WAS RED BEFORE ANY OF THIS.** The decide guard's default went false on 08-30 and
`RuleEvaluationTests.Deciding_writes_nothing_even_on_a_tick_where_rules_fire` asserts it is ON. Eight
classes were found and opted in; that one was not, and no full run happened afterwards.

**Measured on `schooled.toml`: 2,000 Citizens, 146 Days, 4 schools, 12,122 occasions — `unreached`
ZERO, and ZERO again at 20,000.**

🔴 **THAT 100% IS A PROPERTY OF THE WORLD.** ***The synthetic city is ~1.4 km across against a Commute
Budget that walks 4.2 km***, so nothing in it is out of reach — `adr/0089` backwards.

✅ **BUILD ONE WIDER THAN A BUDGET AND THE NUMBER APPEARS: 61% at 100,000 Citizens, 50% at 200,000**
— 9,841 occasions with a school in the box that no route delivers in time. ***That is the number a
coverage Map Layer could not have produced.*** ⚠ **It is not `adr/0032`'s Arterial**: `arterial_count
= 0` here, so the detour is the grid's — a straight-line box against a right-angled walk.
***Severance stays unmeasured and the instrument for it now exists.***

✅ **The failure half lives at `--schools 0`**: mean depth **−233 by Day 144**, falling at exactly
`education_degrade = 2` a Day. ⚠ **The knob is the instrument's** — how many schools stand is a fact
about the city, and no Ruleset says it.

🔴 **A new Tick consumer with no `plans/0013` row** (`adr/0073`) — the corpus freeze is why. ⚠ **It starts
every school Trip on ONE Tick**; a school day has no hours key to partition on, and the Event Wheel is
the successor.

## What the second sitting found

🔴 **BUILDINGS WERE DRAWN IN THE MIDDLE OF THE ROAD, AND ONLY A PICTURE COULD SAY SO.** A Lot's
`east`/`north` is its **address point on the Segment**, not a plot of ground beside it — Lots hang on
Segments and have no depth (`adr/0078`) — and which kerb it belongs to lives in a **separate `side`
column** the shell was discarding. Every hash, every census and every ASCII dump was correct
throughout: they all ask *which Lot*, and none of them asks *where does it stand*. ***The one
question a renderer asks is the one nothing else in the build had ever asked.***

🔴 **A DAY TAKING 4m16s READ AS A STUCK `Day` COUNTER, AND THE READOUT'S OWN NUMBER IS WHY.** It
printed `338x real time`, which is true — 8 Ticks/s × 42.19 s — and a person reading *338×* does not
then expect to wait four and a quarter minutes for tomorrow. **The reassuring figure was the one on
screen.** ⚠ ***A speed is a rate and a person waiting is holding a duration***, which is `adr/0059`'s
*state the duration, derive the rate* arriving in a readout rather than in a Ruleset. The rung now
prints **a Day in 4m16s** beside the multiple.

⚠ **`BOROUGH_SHOT` did not work on a machine with no screen**, which is the only thing it is for: the
viewport has no texture under `--headless`, so reaching through it threw before both the print and
the quit. ***A run neither said it had arrived nor stopped***, and the timing attempt against it read
its own `timeout` back as the answer.

🔴 **AND THE SECOND ATTEMPT DID IT AGAIN AFTER THE GUARD LANDED, FOR AN UNRELATED REASON.** The
pipeline was `godot | grep -m1`: **`grep` exits on its match and `godot` does not notice**, so the
run continued to the `timeout` and the elapsed figure was the killer's, not the city's — **400s,
then 600s, neither of them a measurement.** ⚠ **The second reading was nearly reported as evidence
of a slowdown that does not exist.** ***A fire-once-and-quit trigger can only be asked "tell me when
you pass N", and a stopwatch wrapped around it cannot tell arrival from being killed***; an
instrument that prints as it goes distinguishes them by construction. `plans/0032`'s axis, arriving
one level down: this was an **instrument** all along, and it was built in the shape of an assertion.

✅ **MEASURED, ONCE THE INSTRUMENT REPORTED CONTINUOUSLY: 8.0 Ticks/s, FLAT.** 128 Ticks per 16 s
across the run, frame time constant, and `_owed` **draining rather than filling** — the host is far
ahead of the ladder, and a Tick costs ~3 ms in Debug at 4,000 Citizens against a top rung of 32
Ticks/s. **So a Day is 4m16s by measurement and not only by arithmetic, and nothing degrades as the
city fills.** ⚠ **Debug, headless, and not the reference machine** (`adr/0106`) — it is a check that
the loop delivers its rung, never a figure about the city.

🔴 **THE PACING QUESTION IS OPEN AND IS BIGGER THAN THE LADDER.** Watching motion settled that
**slower rungs are necessary** and settled nothing about where the *natural* rung sits. ***That is
design, and it is not paid here.*** **It is queued in *Owed when the freeze lifts* above, which is
where it is stated in full** — ⚠ **restating it here would be two copies of one question in one
document**, which is `plans/0012` **Cause 1** written by hand.

## What the flood found

**A Flood ships.** `DisasterEngine` schedules one from the seed and the Tick over the Hazard Region,
spreads it through the floodplain connected to its seed below a rising surge, recedes it to nothing,
and takes what it finds — `rulesets/flooded.toml`, `--flood`, `DisasterTests`, and water on the map
in the shell.

**THREE DURATIONS AND NO SEVERITY KEY**, which is `01 §5.2`'s own sentence kept: *"No severity
constant is authored anywhere; the only constants are a frequency interval and a spread rate, both
durations, both scale-free."* How bad a flood is comes out of **where the world seeded it** —
measured, and it is monotone:

| seed depth | ruined | swept |
|---|---|---|
| 102 | 0 | 224 |
| 378 | 38 | 185 |
| 1,119 | 128 | 51 |
| 1,800 | 235 | 5 |
| 1,876–1,975 | 0 | 0 |

A Hazard Region row holds *the flood level minus its ground*, so a large depth is **low** ground. The
surge opens with the water at the seed's own ground and rises to the flood level; ground **below**
the origin is swept away and its Lot vacates, ground at or above it is **ruined** and the shell
stands. ***Both are existing verbs and the fork is one depth against another.*** A flood seeded high
on the floodplain destroys the city; one seeded in the deepest hollow never reaches it.

⚠ **5 of 9 floods touched nothing at all**, which is `01 §5.3` working: *"Riverside floodplain
inundated — 0 Buildings affected"* is the game telling a player a siting decision was correct.

🔴 **THE FIRST RUN REPORTED FOUR FLOODS AND ZERO BUILDINGS TOUCHED, AND THE MECHANISM WAS FINE.**
`coastal.toml`'s lattice sits at the map's middle on high ground; **0 of 420 Lots were on floodplain
at all.** The synthetic city is ~1.4 km across on a 65.5 km map, so it covers two ten-thousandths of
the ground and *cannot meet a coast by accident* — `adr/0089` arriving where nobody expected it.
`flooded.toml` therefore states a `[[lattice]]` **on the water's edge** and that siting is the whole
demonstration: **240 of 420 Lots exposed.** ⚠ ***A world where a disaster cannot reach the city is
not a demonstration of disasters***, and the dump now prints where the floodplain actually is so the
next person is told in one line rather than after an afternoon.

🔴 **THE FOOTPRINT LEAKED, AND ONLY ITS DEEP HALF.** The end-of-flood drain freed the Cells *below*
the surge — and ground deeper than the seed is never below it, because the surge only climbs back to
where it started. **5,140 Cells still standing after three floods had ended and a fourth had reached
291.** ⚠ **The comment above the line had reasoned from the right fact to the opposite conclusion**:
the deepest ground *is* the last to dry, which is exactly why the recession cannot be what takes it.
`adr/0006`, caught by the dump's own last line rather than by a test — `DisasterTests` holds it now.

🔴 **TWO PHOTOGRAPHS OF A FLOOD WERE TAKEN WITH NO CAPTION ON THEM.** `--start-at` fast-forwards in
`_Ready`, so the world is already past `BOROUGH_SHOT_AT` when the **first** frame draws — and a
`Control` added to a `CanvasLayer` that frame has not been laid out yet. ***The one thing in the
frame that says which Tick it is, is the thing a first-frame capture drops.*** The trigger now waits
for the third frame. ⚠ **This is the third defect `BOROUGH_SHOT` has had and all three were about
*when* it fires**, never about what it draws.

✅ **THE SHELL SHOWS WATER, AND THE FLOOD ARRIVING IN THE CITY.** One MultiMesh for the sea, laid
once; one for the standing water, refilled every frame. **Buildings are sized from their kind** —
`[[building]] occupants`, jittered per Building off its monotonic row id — and every shipped kind
declares **3**, so *today the derivation buys nothing visible and the variation is all jitter*. It
is there so that a kind holding thirty draws a tower without the renderer being told.

🔴 **A RUIN LOOKED EXACTLY LIKE A HOUSE, AND THE FLOOD IS WHAT MADE THAT UNBEARABLE.** In the first
frame at Tick 6,101 the water was unmistakable and **235 ruined Buildings standing in it were
indistinguishable from the dry ones on the bank**. The readout said the number; the picture did not.
***That is the same shape as the hexadecimal the Definition of done was amended over***, one level
along — a state the city knows and the eye cannot find.

✅ **FIXED, AND IT IS `IsAbandoned` RATHER THAN *FLOODED*.** The Buildings MultiMesh carries a colour
per instance now. ⚠ **The wider predicate is deliberate**: a Building abandoned by `adr/0053`'s
failure pressure and one ruined by a flood are the same state — `02 §4.3`'s derelict — and the
renderer has no business knowing which verb put it there. **The visible consequence is that
`declining.toml` now greys out as it decays**, which nobody asked for and is the point: ***one
colour, every mechanism that reaches the state.*** Checked rather than asserted — at Tick 7,001 that
city is a mix of pale standing Buildings and dark shells with no flood anywhere near it.

🔴 ⚠ **AND THE FIRST SPELLING WASHED IT OUT, IN A WAY THAT LOOKS EXACTLY LIKE A BAD PALETTE.** A
MultiMesh instance colour is multiplied into albedo in **linear** space, so sRGB values written
straight through render far brighter than they read: standing came out near white and the contrast
the change exists to create was most of the way gone. ***The colours were right and the space they
were written in was not.*** `SrgbToLinear` at the write site. ⚠ **It cost a second screenshot to
see, and no test could have caught it** — there is nothing in the build that asserts anything about
a colour.

⚠ **The Hazard Region is still not drawn**, so what `01 §5.3` calls the *posted price* — the thing
that makes riverside land a decision rather than an ambush — exists as a number `--flood` prints and
nothing on screen. ***The gap moved from the mechanism to the overlay.***

⚠ **A new Tick consumer with no `plans/0013` row** (`adr/0073`) — the corpus freeze is why. It is
`O(footprint)` a Tick while a flood is live and unmeasured at a million Citizens.
