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
   **ratio** — all prose, doc-comments included, over non-comment `src/` lines — at **55** as of
   2026-08-31, so a page written beside a mechanism is free and a page written alone is refused.
   ⚠ **It is a RATCHET and the number moves down**: it opened at 57, the flood put simulation in
   the denominator, and the ceiling followed the measurement the same day. ***A ratchet not lowered
   to the reading banks the gain as slack to spend on prose later.*** ⚠ **This is not a
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
| **Should two children pair into one Household, or does each form its own?** `World.SpawnChildren` gives every child its own Household and nothing in the build pairs anybody, so a formed Household holds **one adult** and exact replacement is **one child** — where [`adr/0011`](../docs/adr/0011-household-life-stages-and-self-generating-population.md) derives the threshold as **two**, *"two children replacing two adults"*, which is airtight for a Household of two. ⚠ **The ADR is not wrong; it assumes a mechanism nobody built.** What is owed is the decision and not the arithmetic: ***who pairs with whom***, and whether the answer is a marriage market, a draw at formation, or nothing at all. ⚠ **The readout is already honest** — `--stages` measures `adults per Household` and takes the threshold from it — so nothing is blocked, and what is at stake is whether a Household is a person or a family. Goes to §C under `adr/0011` | *arguable* — no measurement says how many adults a Household ought to hold; the census already says how many it does | **A mechanism that treats two adults in one Household differently.** `plans/0046` decision 3 names exactly that as `Citizens.Age`'s revisit trigger, and it is the same trigger: ***until something can tell two adults apart, a pair and a single are the same row twice*** | 2026-08-31, the 1,200-Day run |
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

🔴 **AND IT HAPPENED A SECOND TIME ON 2026-08-31, WITH THE SAME CAUSE WEARING A DIFFERENT COAT.**
Rows 15–18 were written and re-scoped within the hour. This time they were not repairs — every one
named a mechanism that genuinely does not exist — but every one was ***named by a symbol***: a column
with no reader, a call site that only ever fires one way, a remark quoting its own missing half.
⚠ **A queue assembled from symbols finds only what fits in a `grep`, and a swath does not.** The first
list found things with tests; the second found things with names. ***Neither question asks what the
project is missing at the size of a milestone.*** Asking that one instead produced 15a–15e: the shell
issues **no `Command` at all** — `Command`, `CommandKind` and `TickInput` are zero references in
`Main.cs` — so all six player verbs are applied in `Simulation` and reachable only from an Input Log
or a test.

**The risk 15 retires:** *that the design's central claim has never been tested by a person.* Pillar 3
is **govern, don't place**; nobody can govern, and nobody can place. Every pillar and every anti-goal
in `00` is asserted in prose and unexercised, and a city nobody can act on is a simulation rather than
a game. ⚠ **Three other swaths were sized and passed over**, each verified rather than assumed, and
they are the next candidates: **traffic's second tier** (`Lane`, `Stress`, `Microscopic` are zero
non-comment references in `Borough.Core`, and `RoadSegmentTable.Fidelity` is a `Derived` column
`RoadGraph` sets to 0 and nothing ever raises — row 2's shape again); **the shopping occasion**
(`TripPurpose.Shopping` is declared with a full `adr/0067` remark and ***nothing in `src/` starts
one***, so every Commute Budget rung and congestion figure in the corpus is calibrated against a
single generator); and **the eye's other half** (`ChunkAggregates` 0 references, `Notification` 0
non-comment references, **19 headless dumps against a three-line on-screen readout**).

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
| 9 | **Needs, and the preference axes.** `Taste` **0 files**, `Preference` **0 files** in `Borough.Core`. `adr/0027` calls them *"the most load-bearing data in the design"*. ⚠ **This row said placement *satisficed on distance alone* and that was WRONG** — it did not satisfice at all, and `adr/0069` says so itself: *"no acceptance filter, no sampler bias, no scored choice, no `μ`"*. A Household had no preference **and** no mechanism that could have expressed one | ✅ 31-08 |
| 10 | **`Service` throws.** The civic swath — schools, health, safety. `School` is **0 files**. ⚠ **After 9, not before**: a service with no need to satisfy is a Building with a Bin | ✅ 30-08 |
| 11a | **The eye.** Of `05 §2`'s three hot queries only `LayerCells` exists; **`VisibleAgents` and `ChunkAggregates` are nowhere in `src/`** and a **Traveller has no coordinate at all**. ⚠ **Day one of 11b either way** | ✅ 30-08 |
| 11b | **The shell.** `src/Borough.Godot` — Godot 4.7.2, a camera over the city, one MultiMesh per kind of thing, a speed ladder and a readout. ⚠ **Not in `Borough.slnx` and never will be**: that absence is what enforces *the headless runner never requires Godot* | ✅ 30-08 |
| 12 | **Disasters.** `coastal.toml` carries a Hazard Region and **nothing fires on it** | ✅ 31-08 |
| 13 | The `0046` loose ends — the dwelling stock's missing sink, `aged.toml`'s narrow windows. ⚠ **Small on purpose and last on purpose**: `StageDumpTests` pins both with tests that assert the defect, so neither can be lost | ✅ 31-08 |
| 14 | **The audit.** Walk this page for what it recorded as owed and never paid. ⚠ **A findings section is not a ledger**, and a debt written in a narrative paragraph is a debt nobody sums | ✅ 31-08 |
| 15a | 🔴 **Picking.** A click becomes a Tile. The camera is an orbit, and **nothing in the shell converts a screen position to anything at all** — no ray, no ground-plane intersection, no Tile. ⚠ **Every row below is blocked on this one**, and it carries a hover readout of its own: ***a verb you cannot aim is a verb you cannot test***, so what you are pointing at has to be on screen before anything commits |  |
| 15b | **`Zone`, `Connect`, `Demolish` — the three that change the ground.** ⚠ **`Zone` SUBDIVIDES and does not paint** — found by building it. ⚠ **`Demolish` addresses the LOT's Tile and never the cursor's**, because `ApplyDemolish` matches exactly and refuses rather than substituting | ✅ 31-08 |
| 15c | 🔴 **`Govern` and `Service` — the two that change the rules.** The tuner already exists and already reloads a Ruleset, so `Govern` is a dial per `[[policy]]` that writes a **`Command`** instead of a file. ⚠ **`Service` is `01 §5`'s acknowledged placement exception** and the reason `schooled.toml`'s school is never built: a run with no `--schools` builds none, so ***the only world with an `education` Need has nothing that answers it*** |  |
| 15d | 🔴 **The session is a log, and this is the row that makes the other three worth anything.** The shell writes `.borough` through `Borough.Formats` — ⚠ **using the codec and never implementing one** (`adr/0039`). ***A verb that is not in the log is a state change no replay reproduces and no State Hash divergence explains***, which is the sentence `Populate` and `Arrive` each already carry in their own remark. **The proof is the round trip**: a city played by hand in the shell, replayed in `Borough.Headless`, same State Hash |  |
| 15e | ⚠ **A refusal reaches the player as a sentence.** Every verb's refusals are `InvalidOperationException` today, which is correct for a log and wrong for a person. ⚠ **`Core` returns ids and numbers, never strings**, so the shell owns every word, resolved through the Ruleset — ***which is the real leak vector `CLAUDE.md` names***: not `using Godot;`, but a method that returns a formatted string because a panel wanted one |  |
| 15f | 🔴 **A CLASS WENT MISSING FROM THE COMMIT GATE AND NOTHING NOTICED.** `RulesetSchemaTests` — two tests — was absent from every lane run on 2026-08-31 until 16:49, including **the one that gated `c2e9ff3` while the class was already failing on that tree**. Ruled out: the `tier!=instrument` filter (the class runs and fails under it), a skip (0 in every run) and a missing file. ⚠ **The cause is UNKNOWN and this row is not the fix — it is the instrument.** ***Nothing in the suite asserts how many tests ran***: `TierBudgetTests` times them and `TierDeclarationTests` counts the instrument share, and neither would see a class disappear |  |
| 16 | 🔴 **A Need has no consequence.** `Sustenance` and `Satisfaction` are saved, hashed, degraded on a duration and recovered on supply — and **the only thing in `src/` that reads either is `Evidence`**, which is a panel. ***A Household starves to the floor and nothing in the city is different.*** ⚠ **After 9 and 10 and because of them**: those built the reading, and a reading nothing acts on is an instrument rather than a mechanism |  |
| 17 | 🔴 **Nobody moves house.** Four call sites reach `World.Unplace` — over-capacity eviction, the premises emptying, the tenant's decline threshold, and shedding — and ***every one of them is the Household LOSING its home***. A housed Household can never re-enter the Unplaced Pool by choosing to. ⚠ **This is why `choosy.toml` had to be built on `declining.toml`**: a preference about where to live is unreachable for anybody already living somewhere, and `adr/0011` calls life stage *"one of the primary drivers of residential mobility"* against a build that has none |  |
| 18 | 🔴 **A dwelling costs nothing.** `PlacementEngine`'s own remark: *"acceptance needs rent, a commute and a tolerance; none exists, so any member would take any dwelling."* ⚠ **One of the three shipped** — `EmploymentEngine` says *"this is where the commute exists"* — so the sentence is now two thirds true rather than wholly. ***`rent` is `adr/0027`'s third preference axis and the thing that would make 17's move a decision instead of a shuffle*** |  |
| 19 | ⚠ **The Day is a comb, and two mechanisms cut the teeth.** `CommuteRoster.ShiftStartOf` sums two draws and halves them, then rounds the result to an **hour**; `ServiceEngine.Attend` returns unless `tick.Raw % Ticks.PerDay == 0`, so ***every school Trip in the city starts on one Tick of 2,048***. Measured on `minimal.toml` at 1,000 Citizens: **1,341 of 2,047 Ticks with nobody out at all**, longest empty run 486. ⚠ **Last, because it is the only row here that repairs something that already runs** |  |

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
separates nothing. ***What it costs at a million Citizens is unknown***, ~~and it owes `plans/0013` a
row~~ ✅ **and `plans/0013` carries the row as of 2026-08-31**, saying exactly that.

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

~~⚠ **The Hazard Region is still not drawn**~~ ✅ **DRAWN 2026-08-31**, by the audit pass below —
`Main.Hazard`, one flat colour laid once under the roads and under the sea. What `01 §5.3` calls the
*posted price* is now on screen from the first frame instead of being a number `--flood` prints.
⚠ **The DEPTH is deliberately not drawn**: a Hazard Region row holds *the flood level minus the
ground*, so a shade ramp on it would read backwards — the polarity that made this file's
worst-looking seed the one that ruined nothing.

~~⚠ **A new Tick consumer with no `plans/0013` row**~~ ✅ **FILED 2026-08-31.** It is `O(footprint)`
a Tick while a flood is live and still unmeasured at a million Citizens, but the ledger now carries
the row and says so — and the row makes a point the engine's own remark could not: ***the footprint
is ground, so this is the only consumer in the ledger whose multiplicand does not fall when the
population does.***

## What the loose ends found

**The dwelling stock has a sink.** `[[building]] abandoned_when_empty_after_days` abandons a Building
nobody has lived in for its kind's duration — `adr/0069`'s build predicate mirrored, and `02 §5.5`'s
redevelopment floor, *the case where nobody wants the land*. ⚠ **It ABANDONS rather than demolishing,
so `adr/0091` is untouched**: the city stops maintaining an empty house and never sends a bulldozer a
player would have had to pay for. **`Days the stock fell` goes 0 → 141 of 400.**

⚠ **It is not `condemn_after_days` and the difference is the whole point.** That key reads Failure
Pressure, so a kind stating it declines whether anybody wants it or not — on `aged.toml`, whose
`upkeep` can never be supplied, it would be a fixed **lifespan** and every dwelling would die on one
clock. This reads occupancy. ***Only surplus stock dies, so the sink is the demand signal read from
the other end.***

🔴 **AND IT DOES NOT RESTORE `jobs = 8`, WHICH IS THE FINDING RATHER THAN A FAILED REPAIR.** Swept at
5, 10, 20, 40 and 80 Days, `posts per Citizen` moves **1.61 → 1.98 across a sixteenfold range**,
against 2.06 with no sink and a derived 0.96. ***A shrinking city does not consolidate***: placement
takes the first Lot with room out of a draw of three and nothing biases it toward a fuller house, so
the families left after a trough are spread **one per dwelling**. **Over a fifth of the housing
capacity stands empty while a thirtieth of the houses do**, and a sink keyed on an empty house can
only collect the tail of that. ⚠ **Neither half is a defect** — a family has no reason to prefer the
house with neighbours in it, and steering the sample would be the optimiser `adr/0017` refuses. ***So
`1000/360 × occupants` assumed every dwelling was FULL, which is a city under housing pressure, and a
demographic city is under it half the time.***

🔴 **THE EMPTY CLOCK'S FIRST SPELLING TOOK THE WRONG NEIGHBOUR'S ENCODING AND A FIXTURE CAUGHT IT.**
`AbandonedSince` uses zero-as-sentinel and this column copied it — but a Building is empty **from the
Tick it is raised**, so zero-as-sentinel loses every Building raised on **Tick 0**. That is not a
corner: it is every fixture in the suite and every Building `SyntheticCity` lays. ⚠ **It was invisible
on the shipped world** because the populator fills what it raises in the same call, and the remark
above the line said the case was *unreachable*. ***A sentinel is a claim about which values cannot
occur, and it was written by looking at the neighbour rather than at the mechanism.***

**The stage windows are as wide as their own floors.** `busiest ÷ mean` **7.0× → 3.3×**, Days with no
transition at all **116 → 19** of 400.

🔴 **THE WINDOW COULD NOT BE WIDENED ALONE, AND A NUMBER IN THREE DOCUMENTS SAID OTHERWISE.** A wake
is drawn uniform on `[N, N+W)`, so a life is `N + W/2` **on average** — and `aged.toml`'s mean was
already **188 Days against `adr/0094`'s ~190**, where `plans/0046`, the file's own header and the
`--stages` panel all called the chain **160**. ***160 is the FLOOR and the ceiling is about the
MEAN***, so there was no room at all. Widening at fixed floors would have put the mean at 236. The
floors are halved to pay for the widths and the mean is now exactly 160. ***A number that is one end
of a distribution says which end*** — `plans/0012` **Cause 5**, on a distribution rather than on a
ratio.

⚠ **IT DAMPS THE ECHO AND DOES NOT REMOVE IT, AND 400 DAYS CANNOT SEE WHICH.** Over **1,200 Days**
the widened city converges — the swing falls **3.67× → 2.55× → 1.19×** across three 400-Day thirds —
where the narrow one is still swinging **2.47×** in its last third. ***`plans/0046`'s definition of
done was answered on a run too short to answer it.***

🔴 **AND THE LONG RUN FOUND THE REPLACEMENT THRESHOLD WRONG BY A FACTOR OF TWO.** `adr/0011` derives
exact replacement as *two children replacing two adults* — airtight for a Household of **two** adults,
and `World.SpawnChildren` gives **every child its own Household**. Nothing pairs anybody, so exact
replacement is **one child**, and ***the census says so rather than the arithmetic***: `working age`
and the Household count come back **exactly equal**. ⚠ **So 1.45 against 2.00 was a city growing 45% a
generation reported as one in decline**, in the panel, in `plans/0046` and in `aged.toml`'s header at
once. **720 → ~1,100 Households over 1,200 Days**, bounded by housing rather than by fertility. The
panel now **measures** the threshold; the design question is queued in *Owed when the freeze lifts*.

⚠ **A new saved column and no `plans/0013` row** (`adr/0073`) — the corpus freeze is why.


## What the preference found

**A Household has a Taste and placement reads it.** `adr/0027` in one expression: a Life Stage
supplies a **base** and a **width**, each Household draws its own **position** inside them, and only
the range moves — so a stage transition slides the band under a fixed position and a family that
always wanted room still wants room once it is an Empty Nest. `Ruleset.CentralityTaste`,
`rulesets/choosy.toml`, `PlacementEngine.TryHouse`.

🔴 **THE BOARD WAS WRONG ABOUT WHAT WAS MISSING, AND THE WRONG HALF WAS THE MECHANISM.** Row 9 said
placement *satisficed on distance alone*. It did not satisfice at all: `TryHouse` drew three Lots and
took the **first with room in it** — three boolean filters over a uniform draw, no score, no
comparison, no second thought. `adr/0069` says so in its own words, *"no acceptance filter, no sampler
bias, no scored choice, no `μ`"*. ***A row describing an absence can be wrong about which absence it
is***, and this one understated the hole by a whole mechanism.

🔴 **THE FIRST DEMONSTRATION RULESET DEMONSTRATED NOTHING, AND EVERY TEST OF IT PASSED.**
`choosy.toml` was built as `aged.toml` plus ten keys, because `aged.toml` is the file with the Life
Stages in it. The Ruleset loaded, `CentralityVaries` was true, the tastes spread correctly across
their bands, and the file produced a State Hash **identical to `aged.toml`'s at every sample** over
20,480 Ticks. The Unplaced Pool was **empty on every one of those Ticks**: `minimal.toml` stopped
condemning anything at milestone 17 (`adr/0164`), so nobody ever lost a home, so `TryHouse` never
ran. ***A preference about where to live is unreachable in a world where nobody is looking for
somewhere to live.*** ⚠ **The three tests that passed were all asking the Ruleset and none was asking
the city** — the one that found it asserts the Pool is non-empty on at least one Tick, which is a
sentence nobody writes until they have been bitten. It is `flooded.toml`'s lattice origin arriving
again, one row along: **the mechanism was right and the world could not exercise it.**

The file is now `declining.toml` plus the stages, because the smallest shipped world that condemns is
the smallest one in which anybody is ever **rehoused** — and rehousing is exactly the moment a
preference is worth having. ⚠ **The slow path would have worked and is not a fix**: `aged.toml`'s
chain reaches `children_become` after 24 + 48 + 48 = 120 Days, which is **245,760 Ticks**, and a
demonstration nobody will sit through is not one.

⚠ **THE LOADER CAUGHT THE `adr/0006` LEAK BEFORE THE RUN DID.** A `[[life_stage]]` stating
`children_become` opens a second door into the Unplaced Pool, and `declining.toml` states no
`[placement] gives_up_after_days` — so the file was **refused at load** with a sentence explaining
that a Pool with a door and no give-up rule grows without bound. ***A bound a Ruleset can violate
belongs in the loader***, and this is what that rule buys: the alternative was a slow leak over a
hundred thousand Ticks that nothing was watching for.

✅ **MEASURED, AND WITH A PLACEBO UNDER IT.** On `choosy.toml` at 2,000 Citizens over 20,480 Ticks,
Households wanting the centre live **161–162 Tiles** from the nearest lattice origin and Households
wanting room live **172–173** — stable across a 6× longer run, so it is a signal rather than noise.
⚠ **Eleven Tiles in a city ~350 Tiles across is small enough to owe the reader a reason to believe
it**, so the same Households are split again on a taste they **do not have**, drawn from an unrelated
stream over the same ids on the same Tick. That sham split moves the mean by **1 Tile against the
real preference's 11**. ***A signal that survives a sham grouping is a property of the map and not of
the preference***, and this one does not survive it.

⚠ **THE EFFECT IS BOUNDED BY THE SAMPLE AND NOT BY THE TASTE, WHICH IS `adr/0069` HOLDING.** A
Household compares the **three** Lots it was shown and nothing biases which three it sees — so
best-of-three is the ceiling on how central anybody can get, however hard they want it. It is also
diluted by the founding population, which `SyntheticCity` places directly without ever calling
`TryHouse`. ***Neither is a defect and both cap the number***: a preference that steered the sample
would be an optimiser, and `adr/0017` refuses one.

⚠ **The mechanism is a comparison and never a threshold.** Nothing refuses a dwelling — a family that
dislikes all three still moves in. A preference that could refuse would fill the Pool for a reason no
Ruleset authored, and on a file with no `gives_up_after_days` it would fill it for ever.

✅ **29 of the 30 shipped worlds produce the same city, State Hash for State Hash.** The gate is
`Ruleset.CentralityVaries` — *does any stage state an opinion* — and not the taste of the Household
in hand, and the reason is the **draw count** rather than the score: a neutral Household would make
the same choice and consume a different number of candidate draws getting there. ⚠ **A neutral taste
weighs exactly zero and is not a special case anybody wrote**: placement scores `distance × (2T − 1)`,
so `centrality_base_percent = 50` ties every candidate and falls through to the first-with-room accept
the build already had. ***The mechanism is continuous with the behaviour it replaces at the midpoint
of the axis.***

🔴 **ONE AXIS OF THREE, AND THE OTHER TWO ARE UNBUILT RATHER THAN REFUSED** (`adr/0070`). Centrality
shipped first because it is the only one the world can already measure — distance to the nearest
`[[lattice]]` origin needs no new state. **Quiet** needs a pollution Layer that is zero on every
shipped file but `fouled.toml`; **rent** needs a price a dwelling does not have. ⚠ **And no
centrality number can be ratified until rent exists**: a preference for the centre is only meaningful
against something that makes the centre cost more, so ***the numbers `choosy.toml` produces ratify
nothing.***

## What the audit found

**Row 14 read this page for its own unpaid debts rather than for a mechanism**, which is the one kind
of sitting the queue's opening warning does not cover: a debt written into a narrative paragraph is a
debt nobody sums. Six were standing. Five are paid below and one became row 18.

✅ **`plans/0013` has the five rows it was owed** — `ServiceEngine.Attend`, `RuleEngine.SweepNeeds`,
`DisasterEngine.Sweep`, a walk's recorded path, and the shell's per-frame world walk — plus a note on
the Zone Rules row saying its unit was measured before the empty-clock verdict existed.
⚠ **Every one says UNMEASURED**, and that is the ledger working rather than failing: `adr/0073`'s rule
is that a cost reaches the document on the day it is found, not on the day somebody has a number.
🔴 **Three source remarks said *the corpus freeze is why it has none*, and all three were wrong by the
time anybody read them** — the freeze caps a **ratio**, and a row filed beside the mechanism it prices
costs the ratio nothing. ***An excuse written into a comment outlives the thing it was excusing.***

✅ **THE HAZARD REGION IS DRAWN.** `Main.Hazard`, `FloodCells` as one flat colour laid once, at 0.02 m
— **under** the roads at 0.1 m and under the sea at 0.6 m, because the risk is a property of the
*ground* and a Street across a floodplain must read as a Street on a floodplain rather than as a
floodplain with a hole in it. ⚠ **The depth is deliberately not shaded**: a row's depth is *the flood
level minus the ground*, so a ramp on it reads backwards — ***a shade ramp on a quantity that reads
backwards teaches the wrong thing faster than no ramp at all.***

🔴 **AND LOOKING AT IT IS WHAT MADE THE NUMBER MEAN ANYTHING.** *240 of 420 Lots exposed* has been in
`flooded.toml`'s header and in `CLAUDE.md` for days. On screen it is **the whole northern half of the
city sitting on rust-coloured ground**, with the boundary running diagonally through blocks so that
one side of a Street is exposed and the other is not. ⚠ **The floodplain is far larger than the
water** — 11,063 Cells at risk, 4% of the map, against a sea that is a strip along one edge of the
frame — because five percentage points between `sea_level_percent` and `flood_level_percent` buy a
great deal of flat coastal ground. ***That is the posted price `01 §5.3` asks for, and it is not a
thing a dump can print.***

🔴 **`BOROUGH_SHOT` HAS A FOURTH DEFECT AND IT IS THE FIRST ONE ABOUT WHETHER IT CAN DRAW AT ALL.**
The three before it were about *when* it fires. This one: the guard was `GetViewport().GetTexture() is
{ } texture`, and under `--headless` that is **not null** — Godot returns a `ViewportTexture` whose
RID the dummy renderer has nothing behind. So `GetImage()` returned null, `SavePng` threw, and the run
neither wrote a picture nor stopped: **41,350 error lines in two minutes, killed by a timeout.**
⚠ **The comment above the guard described that exact symptom, in the past tense, as something already
prevented.** ***A guard checks the handle and the emptiness is in what the handle points at*** — the
same shape as a flood depth reading backwards, the wrong end of an indirection. It now asks
`DisplayServer.GetName()` first, because asking the dummy renderer for a picture is itself two red
lines in a log whose whole job is to be read: **zero errors, exit 0, and a sentence saying a
screenshot needs a real display.**

⚠ **The one debt that could not be paid here became row 18.** The comb is two mechanisms — an
hour-granular Shift start and a school pass that fires on one Tick in 2,048 — and both need a Ruleset
key and a loader refusal, which is queue work rather than audit work. Re-measured live on
`minimal.toml` at 1,000 Citizens: **1,341 of 2,047 Ticks with nobody travelling**, longest empty runs
486, 341, 172. ***It is not stale.***

🔴 **AND THE AUDIT'S OWN GATE HAD A HOLE IN IT, FOUND BY WALKING INTO IT.** `RulesetSchemaTests`
went red on the first lane run after picking landed — *`building.abandoned_when_empty_after_days`
read by the loader and absent from the schema*, which is **row 13's debt**: the key shipped and
`rulesets/ruleset.schema.json` was never regenerated. ⚠ **The alarming half is not the stale file.**
The class **fails on `c2e9ff3`'s own tree** — proven by stashing and running it — and the lane that
gated that commit reported **`Failed: 0, Total: 2496`**. The lane now reports **2,498**, and the
delta is exactly this class's two tests. ***It was not collected, and the run said everything
passed.***

⚠ **Three causes were ruled out and none of them was it**: the `tier!=instrument` filter (the class
runs, and fails, under exactly that expression), a skip (`Skipped: 0` in every run today) and a
missing file (the test asserts its existence separately, with its own message). 🔴 **The cause is
still unknown, and saying so is the finding** — [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s
discipline applied to a defect rather than a mechanism: *unexplained* is not *explained away*.

🔴 **What it exposes is that the suite counts everything about itself except how much of itself
ran.** `TierBudgetTests` times every test and fails a slow one; `TierDeclarationTests` refuses a
third tier and holds the instrument share under a quarter. **Neither would notice a class vanishing
from the run**, because both reason about the tests they were handed. ***A gate that cannot say how
many tests it ran cannot tell a green run from a short one***, which is row 15f.

## What the player's hands found

**15a and 15b, and the shell now issues `Command`s.** `v` looks, `z` subdivides, `x` lays a Street,
`b` demolishes; a left click acts and a left drag still pans, told apart **on release** because any
held button pans and the difference is not knowable until the button comes up.

🔴 **`Zone` IS NOT A BRUSH, AND THE VERB'S NAME SAYS OTHERWISE.** `LotSubdivider.Face` returns zero
on a frontage `World.Frontage` has already claimed, so the verb **creates Lots on virgin faces and
can never repaint a block that has them**. ⚠ **The first test of it zoned a block `SyntheticCity`
had already carved and reported the verb broken** — 102 live Lots, zones `1` and `2`, and the brush's
`0x0008` on none of them. ***The commonest misuse of this verb is silent***: the command is accepted,
creates nothing and reports nothing. The panel now counts the block's unclaimed faces and says
*a click does nothing* before the click, and the mode reads **SUBDIVIDE**.

⚠ **The player's loop is Street THEN zoning, and that is what `PlayerVerbTests` asserts** — lay a
Segment on lattice ground the city never paved, subdivide the block, and Lots appear admitting the
Zone Rule that asked for them. **A Lot's `Zone` is a bitmask** (`ZoneRuleDefinition.Admits`), so a
brush painting the rule's *index* would make Lots that read as zoned and that nothing ever builds on
— ***a city that silently never grows.***

🔴 **`Demolish` is addressed at the LOT's Tile and never the cursor's.** `Simulation.BuildingOn`
matches a Lot's coordinate exactly and `ApplyDemolish` refuses rather than clearing the nearest —
*"a mistyped command must not be indistinguishable from the demolition somebody meant"*. A cursor
lands on a Tile that is almost never a Lot's own, so the shell resolves the click to a Building,
names it in the hover, and sends **that Building's address**. ***The refusal stays exact and the aim
becomes possible.***

⚠ **The shell DECLINES rather than catching, and the reason is not tidiness.** Commands apply at the
top of a Tick, so an exception out of `Apply` aborts `Step` half way and leaves a world no invariant
covers — ***a crash is not the worst outcome of an unguarded click; a half-stepped world is.*** Each
guard reads the **same field** the core reads, so no rule is restated: `Demolish` asks
`IsAbandoned`, `Connect` asks `block_tiles`. `15e` is what turns a disabled click into a player's
sentence.

🔴 **A QUEUED COMMAND BUYS ONE TICK, EVEN PAUSED, and that is a decision.** A verb pressed at rung 0
would otherwise sit until somebody started the clock and look broken. Every reference builder lets
you edit while paused; the cost is that acting is the one input that moves a paused world, by
exactly one Tick.

⚠ **`src/Borough.Godot` is not in `Borough.slnx` and cannot be tested**, so `PlayerVerbTests` pins
the **core's half** of each of the shell's three translations — the brush word, the lattice snap and
the address. ***A shell built on a misreading of the core goes red there rather than being wrong on
screen where nothing watches.*** ⚠ **`Demolish` is deliberately absent from it**: `DemolishVerbTests`
already owns the verb, and `Demolishing_empty_ground_is_refused` is the assertion the Lot-addressing
exists to satisfy.

⚠ **Two of those tests failed first and each failure was the fixture rather than the city.** A world's
constructor lays no roads, so both `Connect` arms bulldozed edges that were not there, two no-ops
hashed the same, and the *two axes differ* assertion **failed by agreeing** — the answer a
difference-assertion gives when neither side did anything. And an A/B on a command that sampled the
hash before and after `Order` was comparing two **different Ticks**, with every clock and cadence in
the world moved; ***an A/B on a command has to hold the Tick count equal on both arms.***

✅ **The hover is a stack, and a line is OMITTED when the thing it would describe has no row.** Nine
of the shipped worlds have no Layer row, no District and no water, so a fixed template would print
`pollution 0 · land value 0 · district 0` over every Tile of every one of them. ***Zero and absent
are different answers***, and a panel rendering them identically teaches that a city has a quantity
where it has no mechanism. ⚠ **Terrain is the one exception and is stated rather than hidden** — the
table is dense, so `ordinary` everywhere is a uniform answer and not a missing one.

⚠ **`TerrainKind.Floodplain` and the Hazard Region share a word and are two mechanisms.** The first
is a quantile of `TerrainGenerator`'s noise field; the second is the water generator's flood level
against the height field. The panel printed both and read as if it were repeating itself, so the
second says **AT FLOOD RISK**.

⚠ **The window opens at 1920×1080, maximised.** Godot's 1152×648 default is a third of a modern
screen against a readout that is one long line and an orbit over a 65.5 km map.
