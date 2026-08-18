# 0028 — Evidence, the accumulators

`06` milestone **6**. The brief.

---

## Status

✅ **DONE 2026-08-17. Scoped 2026-08-16; all seven tasks shipped. Ungated throughout** — it was the
first row of the re-derived Phase 2 spine and no session, spike or milestone stood in front of it.
**Every open decision it owed is closed** — the last of them, *what task 4
answers where the mechanism is missing*, on 2026-08-17. ⚠ **Settling it added a seventh task**: a Trip's
Fate is freed on the line after it is computed, which is this milestone's residue by its own definition
and was missed at scoping. ⚠ **That task then found the brief wrong about its own shape** — it was
scoped as a trail, on task 2's pattern, and the subject of a Trip is the **Citizen**, who survives it.
***What is freed is not always the subject, and it is the subject that decides the shape.***

⚠ **Task 6 found that this milestone's two halves fail `06`'s long-run obligation in opposite
directions.** The **collection** cannot grow — the trail's 257 rows are allocated in its constructor —
so that assertion is a regression guard rather than a discovery; the **magnitude** must grow, because
*attribution decays to magnitude* is the point, so the run **states the exception** and asserts
flatness over the flow that feeds it. Its sharpest finding is about the *other* magnitude:
`CitizenTable.ReachFailures` is a saved per-Citizen count whose own doc-comment says its saturation is
*"a wrap guard rather than a chosen bound"* — so the declared position is that it has no bound — and it
satisfies `adr/0006` **only because `World.Employ` clears it**. That reset **is** named there, as an
**attribution** rule; what nothing said is that it is the bound. ***A sentence can name a mechanism
exactly and still not state the property that mechanism is holding up.*** Removing it puts **3,868 of
4,000** Citizens on a history that can never be cleared. ⚠ **And the run shipped a fix ahead of itself**: the aggregate's count was an `int`,
which wraps after ~162 hours of play at a million Citizens.

⚠ **Task 5 added a fifth Ruleset and the corpus's first name-resolution path.**
`rulesets/diagnosed.toml` exists because **the condition column was `None` in every world this project
can build** — measured at five run lengths — and `RulesetNames` exists because `05 §1` has the shell
resolving strings *"through the Ruleset"* and **nothing implemented that**. Its sharpest finding is
that the reason `minimal.toml` gave for having no condition was ***an argument about a link applied to
a terminal***, which the loader exempts by name: the thing said to be impossible was one key, and it
had been legal all along.

⚠ **Task 4 shipped without moving a baseline, which is the scoping claim discharged**: the assembler
adds no state, so it cannot move the State Hash — and a test asserts that directly, which is
`ColdPathAttribute`'s claim checked from the side a machine can check. Its four findings are under the
task; the two that outlive it are that **every branch of the Lot answer is unreachable in a generated
city** (the generator sizes both sides of the question, a fourth sighting) and that **a copied predicate
has the test standing in for the shared symbol**, because both copies compile whatever they say.

**Four decisions were settled with the user in the room on 2026-08-16**, before any task was written,
and they are recorded under *What was settled before scoping* below. **Three remain open** and are
listed under *Open decisions this milestone owes*. ⚠ **This line said *none of them blocks task 1* and
that was false** — a fixed-size table's capacity and columns cannot be declared without the bound and
the kind count, and both of those decisions said *owed before task 1* forty lines below. Both are now
**closed**: the trail is **one concrete trail for abandonment** and the bound is **256**, a reasoned
guess rather than a measurement. ~~**One decision remains open** — whether task 3 is this milestone's or
19's — and it blocks task 3 alone.~~ ✅ **CLOSED 2026-08-17 as recommended: built here, read there.**
**Nothing this milestone owes is open now.**

⚠ **A fifth task and a fourth open decision were scoped here on 2026-08-16 and removed the same day,
before any code.** Attributing budget refusals to the Building they were aimed at is Evidence-shaped,
but the **trailing window** it needs is a property of the **decline model**, which is milestone **17** —
and the derivation this brief recommended for that window is refused by name by
[`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md).
Both moved to 17. ***Evidence reports pressure and does not produce it***, and the line between the two
is the line between this milestone and that one. Reasoning under *What this milestone must not do*.

⚠ **Two things to read before touching this milestone.** Its position in [`06`](../docs/06-roadmap.md)
is argued from **cost of delay** rather than from a dependency, which is unique in that table — so
nothing downstream will fail if it slips, and that is exactly why it slipped for the life of the
project. And the 2026-08-15 corpus sweep found [`01 §6`](../docs/01-player-experience.md) and
[`00-vision`](../docs/00-vision.md) **each naming the other** as the document that schedules Evidence,
with no milestone building it — [`0012`](0012-corpus-audit.md) *Cause 1* with the two copies pointing
at each other rather than drifting apart, which is why no reader of either ever noticed.

---

## Why this milestone exists, in one paragraph

[`02 §9`](../docs/02-simulation-model.md)'s general rule is that **every aggregate figure must be able
to name its constituents**, and today **not one can**. The Census is city-wide by construction — its
store is sample-major over a fixed metric set (`Census.cs:141`) and **no structure in it can name an
entity**. The condition behind a demolition is computed, is correct, and is discarded on the next line:
`ZoneRuleEngine.cs:291-298` says so in its own comment, *"nothing consumes it: `01 §5`'s notification
surface does not exist, and the row it would be copied off is freed on the next line."*
[`adr/0097`](../docs/adr/0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md)'s
reach-failure count is **decided and unbuilt**. And the specimen sentence `CONTEXT.md` uses to define
what `LEGIBLE CAUSE` means — *"abandoned: 74% of work trips exceeded commute budget over 30 days"* —
is unproducible in exactly one of its five clauses ([`0012`](0012-corpus-audit.md)). This milestone
builds the place those answers go.

---

## The named risk

**That Evidence is retrofitted.** `02 §9` prices it *"cheap if designed in and expensive if
retrofitted"*, and every milestone below this one — the District Pool, Money, land value, the Provider
List, the price surface, the residential choice model, Needs, Departure, Life Stages — adds a mechanism
that would each have to be **reopened** to emit into it. Fifteen reopenings against one.

⚠ **The risk is not symmetric with the others in `06`'s table, and the difference matters when this
milestone is under time pressure.** Every other Phase 2 row retires a risk that *fails loudly* — a
scope the engine refuses, a field nothing sets, a conservation law that breaks. This one retires a risk
that fails **silently and later**: nothing breaks if Evidence is absent, the city runs, the tests pass,
and the cost is paid fifteen times by other people. ***A risk that nothing will report is the one a
schedule discounts***, and that is the whole reason this row is third rather than sixteenth.

---

## What was settled before scoping — the grilling of 2026-08-16

Recorded here because the reasoning is load-bearing and two of the four reversed an earlier
recommendation on evidence found mid-session.

### D1 — the axis is **who reads it**

The corpus held two decisions about Evidence pointing opposite ways, neither citing the other. The
Census is emphatically **outside** the World — *"It is owned by whoever runs the session, never by the
World… Putting it on the World would also have made it state — something the State Hash and the save
would each need an answer for"* (`Census.cs:36-42`). `adr/0097` puts an Evidence datum **inside** the
hash, as a saved count on the Citizen.

**What separates them is the reader.** A number the simulation reads is state whatever it is called;
a number only a human reads is instrumentation. That axis is already written down in this project —
`ColdPathAttribute` grants its exception to a type when *"no code path from `step()` reaches it"*, and
names `Evidence` as a passing case.

### D2 — Evidence is an **assembler**, not a store, and Core assembles

Most of `02 §9` needs no accumulator: a Building's occupants and Bin levels are live state, a shortage
is a scan for unmet Rules, *which Trips are on this Segment* is a Traveller scan, and all of it is
legal on a cold path where a human is waiting. **The residue that genuinely accumulates is events whose
subject has left or whose moment has passed.**

Core owns the assembly, because only Core can re-run a Zone Rule predicate or walk an intrusive list.
The host owns every word, per
[`adr/0002`](../docs/adr/0002-simulation-is-an-engine-agnostic-library.md). Core therefore returns a
**structured id-and-number result per question**, and designing those result types is a real cost of
this milestone rather than an afterthought.

### D3 — the accumulator is **inside the World**, on `RulesetTrailTable`'s pattern

⚠ **This reverses the session's own earlier recommendation, which was to keep counts inside and named
constituents outside. Two findings killed it.**

**It put a shared mutable buffer where the determinism machinery is blind.** `step()` is entirely
serial today — zero threading constructs in `src/` — but `TickPhase.cs` deliberately encodes two
functions, `Phases.Permission[]` (what the design allows) against `Phases.Runs()` (what this build
does), and **Move and Layers are `Parallel` in Permission**. Departures and Trip Fates are produced in
Move. An unhashed sample buffer written there is exactly the *parallel loop accumulating into shared
state* the architecture bans — and because it is unhashed, **lint 4 cannot see it**: thread-count
equivalence compares State Hashes, so the buffer would scramble and every test would stay green.
⚠ S5's clean 2-thread scaling result does **not** cover this case, and `spike-results` says why: it
held *by construction, because the Lane pass is wholly Lane-local*, and is explicitly **not a
discharge**. ***The good number came from there being nothing shared.***

**And for the canonical case there is no entity to hang a count on.** A departed Household is gone.
The project has exactly one precedent for a tally that outlives its subjects, and it is a better answer
than the split: **`RulesetTrailTable`**.

| Property | `RulesetTrailTable` | Why it transposes |
|---|---|---|
| Fixed-size `[Table]`, capacity `Retained + 1` | `:87` | The `adr/0006` sink is the cap itself |
| All six columns `Rows.Saved`, `Touch.Cold` | `:87-99` | Survives reload; costs nothing per Tick |
| **Aggregate row at slot 0** — the oldest entry folds its counts in and its identity is dropped | `:184-197` | ***Attribution decays to magnitude***, which is `02 §9`'s *"a bounded sample of them rather than only totals"* implemented |
| Dense and chronological by slide-down copy, **never a ring** | `:131-139` | *"index order is hash composition order, so a ring buffer with a cursor would make two worlds that survived the same transitions hash differently"* |
| A filter at the write door — a transition that cost nothing is never recorded | `:163-166` | *"an all-zero entry would push a real one out of a window sized for diagnosis"* |
| No handle in, no handle out | `Rules.cs:39-48` | Entries slide, so a handle to one would go stale |

### D4 — the bound is hash-bearing, and it is a `const`

Everything above is saved, so the retention bound is hash-bearing and `adr/0052` applies. It is a
**`const` rather than Ruleset data**, on the trail's own argument transposed one step: that file says a
*designer* must not be able to shrink the window that records their own edits, *"on the authority of
the same file whose adoption the history is about"*. Here it is a **player** who must not be able to
shrink the window that explains their own city.

**Named ratifier** — machine, world and quantity, per
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
as amended: **the first real diagnosis that had to reach past the window**, on a city that has actually
declined, measured as *how far back the answer was*. Refuting readings in both directions: a window
never filled means it is too large, and a diagnosis that ran out of entries means it is too small.

---

## Tasks

**Tasks 1, 2 and 4 are the milestone.** Task 3 is a cheap repair the grilling turned up that belongs
here because it is the first producer; 5 and 6 are the standing obligations. ⚠ **Task 7 was added on
2026-08-17, while scoping task 4** — a second clause of `02 §9` whose answer is freed before anybody can
ask for it, which is this milestone's residue by its own definition. **It is task 2 a second time**, so
it is a task rather than a design question.

### Task 1 — the Evidence trail — ✅ **DONE 2026-08-16**

`CondemnationTrailTable`, in `World._tables`, nine tests, all three golden artefacts re-recorded. The
two decisions it closed are recorded below. *Original text follows.*

The `RulesetTrailTable` pattern, generalised to more than one event kind: a fixed-size `[Table]` in
`World._tables`, all-`Saved`, all-`Touch.Cold`, entries dense and chronological, an aggregate row at
slot 0 allocated at world creation so no reader needs a liveness branch, and a filter at the write
door. **Appended to the table list**, never inserted — appending is the one edit that moves no row
relative to another.

⚠ **Open decision 3 lands here**: one window shared by every event kind, or one per kind. Do not
choose it by taste; see below.

### Task 2 — keep the abandonment reason — ✅ **DONE 2026-08-16**

`ZoneRuleEngine.cs:291-298` named this gap in its own comment for two milestones and has been waiting
for somewhere to put it. `Condemn` now copies the Lot, the kind, the Tick and
`RuleInstanceTable.Reported` into the trail **before** `World.DestroyBuilding` frees the row. This is
`02 §9`'s hardest requirement — *"For a **Lot**: why it is vacant. Not 'vacant' — *why*"* — reaching the
one case where the answer is genuinely not recomputable, because the entity holding it is about to cease
existing. Six tests in `CondemnationCauseTests`, `session-trace.txt` re-recorded.

⚠ **The task had one decision in it and the brief did not see it: *which* Rule's condition.** The
condemnation predicate is an **or** — the loop broke on the first Rule Instance past
`CondemnAfter × rate` — but **a trail entry names one cause**, and several Rules can be past their
thresholds at once. The brief said *the one that broke the threshold* and there is no such thing in the
singular. The answer was already written eight lines above the loop and had never had a consumer:
***the Building's pressure is the longest of its Rules', measured in missed firings*** (`adr/0053` as
amended), followed by *"the maximum is never stored anywhere"* — true only because until now nothing
read it. So the walk no longer breaks, and it keeps the Rule with the most missed firings. ***A remark
that a quantity is never needed expires the moment something needs it***, and the sentence that recorded
the gap and the sentence that answered it were in the same doc-comment.

**The comparison is a cross-multiply and not a division**, `elapsed × worstRate > worstElapsed × rate`,
for the reason the paragraph above the loop already gives for multiplying the threshold rather than
dividing the duration: the division would be spelled through `IntegerMath` for an answer nothing keeps.
Ties keep the earlier Rule, so the choice is a function of the Building's Rule list rather than of the
order two equal pressures were met in. The cost is one full walk of a handful of Rules on condemnation
Ticks only — the *not condemned* branch always walked the whole list.

⚠ **The discriminating test is a `[Theory]` over the two declaration orders, and it is the only shape
that could fail.** A mechanism taking the first qualifying Rule agrees with the correct one on **half**
the orders, so a single-row test would have been a coin toss dressed as an assertion. Verified by
mutation: reverting the selection to *first past threshold* fails 2 of the 6 tests and exactly one of
the two `[Theory]` rows. The fixture surveys every **128** Ticks on purpose — a fast survey condemns on
the Tick the *quicker* Rule crosses its threshold, when there is only one candidate and nothing is being
chosen between. ***Looking once, late, is what puts two qualifying causes in front of the choice.***

⚠ **The two golden artefacts separated for the first time on a *behavioural* change.**
`session-trace.txt` moved from sample 1 onward — the committed session demolishes throughout, so the
trail fills — and `world-hash.txt` did **not move at all**, because it is a hand-built world that never
runs a Zone Rule and therefore has no condemnation in it. Neither Ruleset content hash moved. **Sample 0
is unchanged and every later sample moved**, which is worth keeping: a session shortened below the
second sample would cover this mechanism exactly as poorly as task 1's re-record did, and would say so
with a full set of freshly correct hashes.

### Task 3 — `adr/0097`'s reach-failure count — ✅ **DONE 2026-08-17**

`CitizenTable.ReachFailures`, a saved `ushort`; `World.RecordReachFailure` writes it and
`World.Employ` clears it; `EmploymentEngine.TryEmploy` is the one producer. Six tests in
`ReachFailureTests`, 1,461 green, all three golden baselines re-recorded. `02 §9`'s Citizen row has its
first honest constituent: `jobs beyond budget` could report *distance rather than supply separates
them* and name nobody, and now it can name somebody.

⚠ **The task had a decision in it, the brief did not see one, and it goes against the ADR's own
title.** `adr/0097` is *a **candidate** refused … increments a saved count*, and `TryEmploy` looks at
`[jobs] candidates` candidates per occasion — so a per-candidate count is **that tuning number times
the quantity anybody wants**. A Ruleset moving `candidates` from 3 to 5 would inflate every Citizen's
history by 5/3 with nothing saying so, and milestone 19's threshold would mean different things in
different Rulesets. That is [`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)'s
refusal — ***a derivation that reuses a constant inherits every decision that constant is already
carrying*** — which **this milestone applied to the Evidence window one task ago and did not apply to
its own counter**. The unit is the **occasion**. Settled with the user in the room; `adr/0097` carries
an amendment banner rather than a rename, on its own 2026-08-14 precedent, because under this project's
convention the filename is the claim and renaming it would break every inbound citation to buy a better
title for a decision that is not changing.

⚠ **The discriminating test took three attempts and the first two passed under mutation.** Reverting
the increment into the candidate loop is the mutation, and it must fail something. It did not fail
*summed increments ≤ seeking occasions*, because **employment erases the evidence**: a Citizen refused
three candidates and then employed in the same pass reads as nought either way, and the erasure is
heaviest exactly where the refusals are. It did not fail any assertion about *who* carries a history
either — ⚠ **the carrier set is identical under both denominations**, 483 people at a three-minute
ceiling, because what changes is the size of each history and not whose it is. What separates them is
the **shape of the histogram after a single pass**: as built, all 36 carriers read **1**; under the
mutation, all 36 read **3**, and at two and four passes the spikes are at 3, 6 and 9. So the assertions
are *somebody carries exactly one* — which a per-candidate counter cannot produce, its smallest nonzero
value being `candidates` — and *nobody carries as many as `candidates`*. Neither is a tolerance.
***A test that cannot fail under the mutation it was written for is not a test of that decision***, and
the only way to find that out is to run the mutation.

⚠ **The width is an `adr/0052` number the ADR said it was not opening, and it is chosen to be inert
rather than ratified.** `adr/0097` records *"a width is owed and is deliberately not chosen here… the
width follows from 19's threshold, which does not exist"*, and the count must saturate rather than wrap
under `adr/0003`. Those two together are a trap: any **reachable** cap decides when attribution stops
being exact, on behalf of a consumer nobody has designed, which is `adr/0070`'s forbidden move. A
`ushort` is the way out — at the shipped `[jobs] revisit_ticks` a Citizen is looked at roughly twice a
Day, so 65,535 is on the order of **32,000 Days against a campaign of 562**, and no world this project
can build reaches it. The saturation is therefore a **wrap guard and not a bound**, the choice 19 was
promised is still 19's, and narrowing the column the day it sets a threshold is one edit and one
re-record (`adr/0100`). ⚠ **A byte was recommended and refused** with the user in the room: 255 is
~128 Days, which *is* reachable in a campaign, and a reachable cap is a decision.

⚠ **The mechanism is exercised by no shipped Ruleset and no committed baseline**, and that is a fact
about the cities this project can build rather than about the code. At the shipped fifty-minute ceiling
the golden fixture's whole population commutes inside the Budget, so `beyond` is 0 and nobody carries a
history; the tests reach it by tightening the ceiling, which is `EmploymentRungTests`' lever and its
reasoning — the paved extent is derived from the population, so a bigger fixture is a bigger city with
the same commutes in it. The negative is asserted with the value at which it stops being true beside
it, on 5b-bis task 4's precedent.

### Task 4 — the assembler — ✅ **DONE 2026-08-17**

The cold query surface: one Core-side entry point per `02 §9` question, returning a structured
id-and-number result the host renders. Live state is read, predicates are re-run, the trail is read,
and the three are composed into one answer. `[ColdPath]` where a result type needs it. **No strings.**

✅ **Its scope was settled 2026-08-17, before code, by walking `02 §9`'s three subjects against the
column inventory** — see *Open decisions* → **3**. The clauses split three ways: **assembled** from live
or recomputable state, **accumulated** because the row holding the answer is freed, and **omitted**
because no mechanism produces the answer. ⚠ **The walk found one clause in the middle group that
scoping had missed**, and it is task 7 below.

**What shipped.** `Borough.Core.Evidence`, four files: three `[ColdPath]` result types
(`BuildingEvidence`, `CitizenEvidence`, `LotEvidence`, with `BinEvidence`, `RuleEvidence`,
`TripEvidence`, `CondemnationEvidence` and the `VacancyReason` flag set beside them) and one static
`Evidence` class with `OfBuilding`, `OfCitizen` and `OfLot`. Static and `World`-taking on
`Readouts.Read`'s precedent, ids and numbers throughout, **1,471 tests green and no baseline moved** —
which is the scoping claim discharged: the assembler adds no state, so it cannot move the State Hash.

⚠ **The assembler stores nothing and the two places it does more than forward are the two the tests are
about.** `RuleEvidence.LastRan` is *derived* — `02 §9` asks which Rule a Building last ran and whether
it succeeded, and **no column holds either**; the recovery rests on `RuleInstanceTable`'s own invariant
that an Instance is armed on the Wheel **or** asleep on a wait list and never both, so an armed one
re-armed at `+rate` after a firing that worked and a sleeping one left `NextTick` at the Tick it failed
on. And `BuildingEvidence.Pressure` computes the maximum `ZoneRuleEngine` says *"is never stored
anywhere"*.

#### Four findings

⚠ **1. Every branch of the Lot answer is unreachable in a generated city, and both were found by a
guard assertion rather than by reasoning.** The tests were written with *this-branch-was-never-exercised*
guards on both sides, and three of them fired on the first run. Measured on the golden fixture across a
whole run: **0 of 150 vacant Lots lack frontage** — `RoadGenerator` lays the lattice the subdivider
carves the Lots out of, so frontage is not a property a generated world varies — and **every vacant Lot
is admitted by a Zone Rule**, because `SyntheticCity` paints bit 0 on every Lot and the shipped
`[[zone_rule]]` admits bit 0. So `VacancyReason.NoFrontage` and `VacancyReason.NotZoned` are reachable
only in a city somebody **zoned** and **roaded** by command. ***A flag whose world cannot set it is
tested by a hand-built world or not at all*** — this is the **fourth** sighting of a dial inert at the
shipped configuration, after `foot_crossing_every`, the job-search box and `[traffic]`, and the second
where the cause is that *the generator sizes both sides of the question*.

⚠ **2. The copied predicate is the one thing here that needs a test to exist at all.**
`ZoneRuleEngine.Create` is private and mutates — it raises a Building and bumps a counter — so there is
nothing for a pure assembler to call, and its admission clause is **re-expressed** rather than shared.
That is `plans/0012` **Cause 1** committed on purpose: two copies of one fact, and **both compile
whatever they say**, so no mechanical check in this corpus can separate them. What stands in for the
shared symbol is a **behavioural** test — a Lot the assembler calls unzoned is one the engine never
builds on, and a Lot it does not is one the engine does — and inverting the clause fails exactly that
test and nothing else. ***Where a predicate must be copied, the test is the shared symbol.***

⚠ **3. Being in flight is a spike and not a level, so a fixed Tick was the wrong instrument.** The
first Trip test read the world at Tick 1,024 and found **nobody travelling**. Measured over a whole Day
at 32-Tick intervals the in-flight count runs **0, 0, 1, 101, 2, 0, 18, 64, 0, 0** — most Ticks have
nobody on the road at all. That is `adr/0101`'s Day working as designed (a Shift band puts departures in
a narrow window, and a commute is 1.39% of a Day), so a fixed Tick would have been a test that passed or
failed on **where the window happened to be**. It now steps until somebody is in flight. ***A test that
samples a spiky quantity at a chosen moment is testing the choice.***

⚠ **4. `TickInput.Empty` is a reload request, and it is safe in the fixtures that use it only by
coincidence.** Stepping a `Replay.Start` session with `TickInput.Empty` throws — the input names Ruleset
hash **0**, `Simulation.Reload` compares it against the one in force, and no catalogue holds zero. The
hand-built fixtures elsewhere in the suite get away with it **because zero is also the hash they opened
on**, which is `_opened` latching the first input's hash rather than any decision that empty means *no
change*. Not repaired — the guard's polarity is `adr/0015`'s and is correct — but a continuation step
now restates the Ruleset in force, and says why in a comment. ***A sentinel that happens to equal the
value in force is a sentinel that has never been tested.***

### Task 5 — something to look at — ✅ **DONE 2026-08-17**

A runner mode. The obvious shape is `--evidence`, printing what the trail holds and expanding one
aggregate into its constituents, which is the milestone's whole claim rendered in text. It is the
**ninth** runner mode and the third that steps the world.

**What shipped.** `--evidence` in three panels — the **trail** with its aggregate printed as a row of
the same table, one **Building** assembled in full (the worst-off one standing, chosen by scanning),
and **why the vacant Lots are vacant**. Plus a **fifth Ruleset**, `rulesets/diagnosed.toml`, and
`RulesetNames` in `Borough.Formats`. **1,478 tests green and no baseline moved.**

⚠ **It must be run past 2,048 Ticks.** The trail holds 256 and the 4,000-Citizen fixture condemns 187
by Tick 1,024, so at half a Day the aggregate is **empty** and the panel that exists to show
***attribution decays to magnitude*** shows nothing decaying. At 2,048 it is 256 retained and 76
folded. ***A demonstration of a cap has to be run past the cap.***

⚠ **The mode does not refuse a Ruleset that names no condition, and every picture before it would
have.** `--traffic` refuses because its two panels would be *identical* and an uninterpretable picture
reads as a broken instrument. Here the trail comes out fully populated and exactly one column is
dashes, under a heading naming the file that fills it. ***An instrument that refuses to show a gap is
an instrument that cannot report one*** — a test holds the departure open so it cannot be tidied into
a refusal by somebody applying the house polarity uniformly. What **is** refused is a Ruleset that
condemns nothing at all (no `[[zone_rule]]`, or no `condemn_after`), because a trail with **no
entries** says nothing about anything where a trail whose entries lack one field says exactly what is
missing.

#### Four findings

⚠ **1. The reason `minimal.toml` gave for having no condition was an argument about a *link*, applied
to a *terminal* — and the loader exempts terminals from it by name.** That file gives two reasons, and
only one of them holds. Its header's *"a chain here would be a Rule rescuing itself"* is **correct**
and this milestone does not contradict it: `diagnosed.toml` writes no link and there is still no rescue
anywhere in that city. But the comment at `upkeep` — *"writing a terminal here would claim a source of
supply this file has just finished saying it has no way to name"* — transplants that argument onto a
reporting terminal, which `RulesetLoader`'s refusal 3 exempts in as many words: *"A reporting terminal
is exempt, and that is not a loophole. It rescues nothing by design."* ***An argument against one
member of a category is not an argument against the category.*** The sentence was written when the
distinction had no consequence and acquired one two milestones later when the trail was built; **the
thing said to be impossible was one key, and the loader had been accepting it all along.** Verified by
measurement, not by reading: `diagnosed.toml` against `minimal.toml` at 2,048 Ticks is **identical on
every count** — 285 buildings, 741 pool, 488 Lots, 256 retained, 339 condemnations, 83 aggregated, the
same Lots on the same Ticks — with `withCondition` going **0 → 256** and only the State Hash moving.
That is `adr/0094`'s rescale precedent, and it is what a diagnosis *should* cost.

⚠ **2. `05 §1` has the shell resolving every human-readable string *"through the Ruleset"*, and there
was no resolution path.** The loader builds four name-to-id maps while parsing and **dropped all
four**; a `Ruleset` is `Borough.Core` and holds no strings, so the architecture's own sentence had no
implementation. **The eight modes before this one all get away with it because they print *quantities*
rather than *names*** — a grid of v/c ratios needs no vocabulary, and a condemnation reason is nothing
but one. ⚠ **And the gap was camouflaged**: `Ruleset.ResourceKeys` and `KindKeys` are content
**hashes** of the same authored names, carried into `Core` under `adr/0048`, which says plainly that
*"the core never renders it and never resolves a name with it."* ***A key answers "are these two the
same"; a name answers "what do I call it."*** A reader checking whether names survive loading finds a
table that looks like the answer and is a one-way hash. `RulesetNames` is the second half, in
`Borough.Formats` where the strings belong.

⚠ **3. No dump before this one had ever identified an individual entity, and it turns out none could.**
`Handle<T>.Index` and `Generation` are `internal` — deliberately, so nothing outside `Core` can sort by
one or treat it as a name — so the trail's Lot and the assembled Building are printed as **resolved
slots**, with a dash where a handle no longer resolves. Every picture before this drew a grid or a
distribution, so the question never came up.

⚠ **4. The Lot panel prints a row of noughts on every world this runner can build, and it is kept.**
Task 4 measured why; task 5 makes it visible. `no frontage`, `nobody seeking` and `not zoned` are all
**0**, and `the build cannot say` is everything — because `RoadGenerator` lays the lattice the Lots are
carved out of, `SyntheticCity` paints bit 0 and the shipped `[[zone_rule]]` admits bit 0, and **no
runner mode issues any Command but `Populate`** (`CommandKind.Zone` and `.Connect` have zero production
call sites anywhere in the project). ***A row of noughts under a heading that explains them is a
measurement; the same row with no heading is a defect.*** Making it non-trivial means the runner
issuing a player verb, which is a scope decision and is **not** taken here.

### Task 6 — the long acceptance run — ✅ **DONE 2026-08-17**

`EvidenceLongRunTests`, three tests, 49 whole Days — **100,352 Ticks** — at 4,000 Citizens on
`diagnosed.toml` with the Commute Budget's ceiling brought down to three minutes. **No baseline moved
and no simulation code changed**: the run is an instrument.

#### Four findings

⚠ **1. The milestone's two halves fail `06`'s obligation in opposite directions, and saying so is most
of the run's work.** The **collection** cannot grow — `CondemnationTrailTable` allocates `Retained + 1`
rows in its constructor and never allocates another, so slots read **257 on every one of the 49 Days**
and the assertion is a regression guard against an edit to that constructor rather than a discovery.
The **magnitude** must grow: the aggregate climbs **86 → 12,164** with no sink, because
*attribution decays to magnitude* is what the milestone is for. So the run **states the exception**,
on this milestone's own D1 axis — nothing inside `step()` reads `Condemnations`, so an unbounded value
cannot change what the city does — and asserts flatness over the **flow** instead, which comes in at
**251.4 condemnations a Day**, flat across the 41-Day tail. ***An accumulator fed at a rising rate is a
city changing under the instrument; one fed at a flat rate is an instrument working.***

⚠ **2. The magnitude that *can* leak is held up by a mechanism whose documentation never says it is
holding it up.** `CitizenTable.ReachFailures` is a saved per-Citizen count with **no cap anybody
chose** — `adr/0097`'s `ushort` saturation is a wrap guard at ~32,000 Days and its own doc-comment says
*"a wrap guard rather than a chosen bound"*, so **the declared position is that this magnitude has no
bound**. It is exactly `adr/0006`'s shape: a Citizen nobody can employ accumulates roughly two a Day
for ever. **It does not**, because `World.Employ` zeroes it — and the longest history settles at **9.3**
over the tail with **1,011 carriers**, both flat.

⚠ **The first draft of this finding said no document connected the two, and that was wrong — the
correction is sharper than the claim.** `CitizenTable.ReachFailures` names `World.Employ` by symbol,
one paragraph below, which is `adr/0093`'s writing half done properly. What it does **not** say is that
the reset is the `adr/0006` bound: it describes it as an **attribution** rule, *whose history is this*,
which is what it was designed as. Both sentences are accurate and neither states the load. ***A
sentence can name a mechanism exactly and still not state the property that mechanism is holding up***
— `adr/0093` governs a description being wrong about a **trigger**, and this is a description being
silent about a **consequence**. The cost is concrete: anybody reopening *should employment really wipe
the history?* weighs an attribution question and never sees the unbounded-magnitude one. **Repaired in
place** with a paragraph on the column and a pointer at the test that now fails on that line.
**Verified by mutation**: with the reset removed the longest history goes **14.8 → 24.8** across the
tail's halves against a 3σ band of **3.7**, and carriers go from ~1,011 to **3,868 of 4,000** — the
whole population carrying a history that can never be cleared.

⚠ **3. The ceiling had to be tightened, and the negative that forced it is the run's own reading.**
At the shipped fifty minutes `ReachFailures` is **0 for every Citizen at every sample across 100,000
Ticks** — so the second magnitude this milestone added would have been asserted flat on a column no
world writes to, which is the vacuity slice 5 task 7 withheld an invariant over. `ReachFailureTests`
owns the negative at the shipped value; this run turns its three-minute lever. ***The lever is the
Budget rather than the city because the paved extent is derived from the population***, so a bigger
fixture is a bigger city with the same commutes in it.

⚠ **4. `CondemnationTrailTable.Aggregate` states an invariant in a doc-comment that nothing had ever
checked, and only a saturated window can check it.** That method sets the aggregate's Tick to the newest
condemnation folded into it and says this is *"what makes the trail readable as a timeline: everything
before this Tick is a count, everything after it is named"* — the assumption every reader of the trail
makes, asserted nowhere. It is now, and ⚠ **it is vacuous in every short test**: with nothing folded the
aggregate's Tick is 0 and *every* entry is after it. ***An invariant that only bites once a window has
overflowed cannot be held by a test shorter than the window.*** The second guard beside it is that a
returned entry names a real condition — the aggregate row's `Condition` is never written and stays
`ConditionId.None`, so an off-by-one in `EntrySlot` would hand a reader an entry with no reason in it on
a Ruleset where every condemnation has one.

#### What it says about the 256-entry window, which is not a ratification

**One of the two refuting readings is retired and the other is still unreachable.** Open decision 1
named them: a window **never filled** across a long run means 256 is too large, one that **ran out
mid-episode** means it is too small. The first is now definitively out — the window fills **inside the
first Day** and stays pinned at 256 for all 49, on the smallest population this project ships. The
second cannot be read here for the reason that defeated the original measurement: ***there is no
episode***, because every shipped Ruleset inherits `minimal.toml`'s `upkeep` Rule drawing on a Resource
nothing produces, so decline is a permanent uniform grind rather than a wave. The ratifier is still
milestone 13/14/17's Ruleset that models a city.

⚠ **What the run does add is a scaling objection nobody had raised.** The window is denominated in
**events** and the argument for its size is about a **duration** — *the smallest window that holds a
whole decline episode plus the ordinary background of the period around it*. At 4,000 Citizens 256
entries is **just over one Day** of this fixture's condemnations; scaled by `World`'s own Lot
allocation to a million it is **under five Ticks**. So a single number in entries cannot mean the same
thing at two city sizes, and the sizing argument as written is unsatisfiable by any constant.
***The unit a bound is written in is not the unit its argument is about*** — `adr/0094`'s `revisit_ticks`
lesson on a third axis, and it is a question for whoever ratifies rather than a defect: 4,000 is also
where **158 vacant Lots keep their reason and 85 have already lost it**, so the decay this milestone is
named for is visible on the smallest city in the repository.

*Original scoping note follows.*

⚠ **Scouted 2026-08-17 before it was written, and the scouting produced a fix that shipped ahead of
it.** A 100,000-Tick probe at 1,000 Citizens on `diagnosed.toml` says what the run will find:

| | reading over 100,000 Ticks |
|---|---|
| trail slots | **257, flat from Tick 0** — allocated once in the constructor, never freed |
| entries retained | 183 → **256 and pinned** |
| the aggregate's count | **0 → 2,838, strictly monotone, no sink** |
| `CitizenTable.ReachFailures` | **0 for every Citizen at every sample** |

**So the collection half cannot fail and the magnitude half must.** The trail's storage is fixed by
construction, which makes the `adr/0006` assertion a regression guard against an edit to the
constructor rather than a discovery. The **count**, on the other hand, climbs for ever on purpose —
that is what *attribution decays to magnitude* means — so the run has to **state the exception rather
than assert flatness**. The ground for the exception is this milestone's own D1 axis, *who reads it*:
nothing in `step()` reads `Condemnations`, so an unbounded value cannot change behaviour. ⚠ **What it
can do is overflow, and that was a live defect** — see below.

⚠ **Task 3's count is inert over the whole run and the run cannot exercise it.** `ReachFailures` is 0
throughout, which is `ReachFailureTests`' finding at 100,000 Ticks instead of 1,024: nothing refuses a
commute at the shipped fifty-minute ceiling. The magnitude half has to tighten the ceiling the way that
file does, or it is asserting flatness on a column no world writes to.

**Two things for whoever writes it.** Use `diagnosed.toml` — a trail full of `ConditionId.None` tests
less, and it is behaviour-identical so the readings above carry. And build the world as
`new World(population, rules, key)`: the two-argument constructor makes a **different city**, and a
probe that used it read a Trip slot high-water mark still climbing at Tick 100,000 that vanished on the
correct one. Every long-run test in the suite uses the three-argument form and the difference is
silent.

#### ✅ Shipped ahead of the run — the aggregate's count is `long`

⚠ **`Condemnations` was an `int`, and 32 bits is not a campaign.** It is the **one column in the
project with no sink at all**: every other column here is overwritten as entries slide down, and this
one only grows. Measured — 0.031 condemnations a Tick at 1,000 Citizens, which scaled by `World`'s own
Lot allocation is **~57 a Tick at a million** — so it wraps after roughly **162 hours** of play at the
4× target. Reachable, and the failure is silent in the worst way: the count goes **negative** and the
trail reports that the city has un-condemned Buildings.

***A counter with no sink is denominated in the life of the world, not in the size of the city.*** That
is why it was missed. Every other count in this project is sized by a population, and 32 bits is
enormous against any population this design admits — so the width looked right by the standard the rest
of the corpus is judged against, and the standard was the wrong one. **`adr/0065` on a third axis**:
that decision found a Bin's `level` narrowing 64 bits to 32 because the corpus held *two widths for one
quantity*; here there is one quantity, and the question is whether its width matches its **lifetime**.

⚠ **The guard is a compile error rather than a test failure, which is stronger than intended.**
`The_aggregate_carries_a_count_wider_than_an_int` seeds the aggregate one short of `int.MaxValue` and
pushes it over — reaching two billion condemnations is not a test anybody can run — and its
`int.MaxValue + 1L` assertion **does not compile** against a narrow column. Without it the width would
be **decorative**: `long` and `int` behave identically on every number any other test in this suite
produces, so nothing could have told them apart. **It moved the golden session trace and nothing else**
(`adr/0100`: the re-record is one command and the failing test prints it).

*Original scope:* 100,000+ Ticks with no collection and no magnitude trending upward. ⚠ **The trail's
own discipline is what is being tested here**: entry count is monotonic to the cap and then constant, so
the assertion is a **slot high-water mark that saturates**, not a live count. 5c task 8 found that
distinction the hard way and its record carries the reasoning.

### Task 7 — a Trip's Fate outlives its Trip — ✅ **DONE 2026-08-17**

Two saved columns on `CitizenTable` — `LastTripFate` (a `byte`) and `LastTripEndedDay` (a `ushort`) —
written through `World.ResolveTrip`/`World.RecordTripFate`, assembled into
`CitizenEvidence.LastTrip`, and shown by a fourth `--evidence` panel. Ten tests across
`LastTripFateTests` and `EvidenceDumpTests`. **`session-trace.txt` and `world-hash.txt` re-recorded;
neither Ruleset content hash moved**, because no Ruleset changed.

#### Four findings

⚠ **1. The brief said *"task 2's situation verbatim"* and it is not. The two differ at the subject,
and the subject is what decides the shape.** Task 2 copies a condemnation into
`CondemnationTrailTable` because its subject — the **Building** — is destroyed, so there is no entity
left to hang the fact on; that is this milestone's own **D3** argument in its own words. Here the
subject is the **Citizen**, who outlives the journey by design, and `02 §9` asks the question *of a
Citizen*. ***What is freed is not always the subject, and it is the subject that decides the shape.***
The two situations are genuinely indistinguishable **from the freeing site** — a row computed, checked
and given back on the next line — which is why the brief read them as one, and reading from the
freeing site is what made it wrong. **Settled with the user in the room.**

**And a trail would have been wrong rather than merely indirect.** A commute is two journeys a Day, so
a million Citizens end roughly **two million Trips a Day — about a thousand per Tick** — and a
256-entry window would cover **a quarter of one Tick**. It would have answered *what happened in the
city lately* while `02 §9` asks *what happened to this person*. That is task 6's finding 4 arriving
one task early and from the other end: ***the unit a bound is written in is not the unit its argument
is about***. As built there is no window to size, no `adr/0052` number, and every Citizen's answer is
exact for ever.

⚠ **2. `TripFate.Stranded` is produced by no site in the build**, which is stronger than the corpus
knew. 5b-bis task 8 read `trips stranded` as **0** over 100,000 Ticks and filed it as *unexercised by
any run*; in fact `TripEngine.cs:563` says in its own comment that the Fate *"is a question for whoever
builds `TripFate.Stranded`"*, and grep confirms the enum member is declared, documented with exactly
when it applies, counted in the Census — and **written by nothing**. `adr/0070`'s **unbuilt** class.
***A count that reads zero because nothing exercises it and one that reads zero because nothing writes
it are different facts, and a Census cannot tell them apart.*** So the store is tested over the **Fate
set** rather than over today's producers: three Fates through the engine, the fourth through the door,
with the reason beside it.

⚠ **3. Two new saved columns moved the *world* hash on a fixture that has never run a Trip**, so both
columns are all zeros there. That is the per-field declaration working exactly as `05 §3` claims —
declaring a column is what allocates it and what puts it in the fold, so a column **nothing has ever
written still enters the State Hash** and cannot be silently absent. Worth stating because the
comfortable expectation is the opposite: an all-zero column reads like it should be invisible.

⚠ **4. The three Fates the engine does produce need three different worlds, and one of them cannot be
a generated city.** `NoRouteFound` arrives through `Start`'s first branch — `adr/0079`'s hole, an
Address that does not exist — and task 4 measured that **0 of 150** vacant Lots in a generated city
lack frontage, because `RoadGenerator` lays the lattice the Lots are carved from. So that branch is
unreachable in every world `CommandKind.Populate` can build, and the fixture is a hand-built world
with Lots, Buildings and **no Segments at all**. ***A branch unreachable in a generated city is
reachable in a hand-built one, and which of the two you use is a statement about what you are
testing.*** The test is a `[Theory]` over the three because they are written at three structurally
different places — two inside `Start` before a Traveller exists, one in `AdvanceTravellers` after it
has been freed — so **a mechanism wired at one of the three passes a single-Fate test**. **Verified by
mutation**: dropping the `AdvanceTravellers` write fails 3 of 9, and reverting the over-budget site to
the bare table door fails **exactly 1**.

#### The door, and what it can and cannot enforce

`World.ResolveTrip` resolves the Trip **and** records whose journey it was, in one method, on
`World.Employ`'s argument: the two are one event, and a caller that can do one without the other
eventually will. **Requiring the Citizen is what makes that structural, and it cost nothing** — all
four Fate sites already had one, since `TripEngine.Start` takes a Citizen as its *first parameter* and
`AdvanceTravellers` reads `TravellerTable.Citizen`. A fifth site cannot now be written without deciding
whose journey it was.

⚠ **`TripTable.Resolve` went `internal` and is no longer the one door onto the event.** It is still the
one door onto the **column**, and it cannot be the other, because a table does not reach across to
another table. The distinction is written at the declaration, because the phrase *"the one door a Fate
goes through"* was already in that doc-comment and is now true of less than it says. ***A sentence that
was exactly right becomes a false description of the build when a second consumer appears, and nothing
re-reads it.***

#### Something to look at

A fourth `--evidence` panel, on 5b-bis task 6's finding — ***a Census family with no reader is a family
nobody can see***, which 5b committed by building `TripCounter`, wiring it through the Census, testing
it, and printing it nowhere. It prints a **distribution** rather than one Citizen, because a single
Fate is a value a test already asserts. What it shows that nothing else can is the **silent
population**: on the shipped fixture, **2,208 of 4,000 Citizens have never finished a journey**, which
is exactly the 4,000 less the 1,792 employed — nobody commutes without a job and the commute is the
only generator there is. The test asserts that **identity** rather than a threshold, so a panel
counting the wrong thing cannot pass by looking plausible.

*Original scope follows.*

### ~~Task 7 — a Trip's Fate outlives its Trip~~ — **ADDED 2026-08-17**

`02 §9`'s Citizen row asks for *"current or last Trip with its Fate"*. **The *current* half is a scan
and the *last* half is unrecoverable today.** `TripEngine.Release` asserts the Trip carries a
Fate and frees the row **on the next line** (`:782-790`), and `TripEngine.AdvanceTravellers` resolves
the Fate and frees the **Traveller** four lines later (`:690-693`) — which holds
the only Citizen→Trip link there is (`TravellerTable.Citizen`, `TravellerTable.Trip`) — in the same
pass. So the Fate is computed, checked, and both it and the association with the person who made the
journey cease to exist at the same instant.

**That is this milestone's own test of its residue, verbatim** — D2's *events whose subject has left or
whose moment has passed* — and **it is task 2's situation verbatim**, down to the phrasing: `Condemn`'s
comment said *"the row it would be copied off is freed on the next line."* The machinery exists;
`CondemnationTrailTable` is the pattern and task 2 is the precedent for applying it.

⚠ **It is the third sighting of one defect, and the first two are already written down.** `02 §9`'s
general rule is *every aggregate figure must be able to name its constituents*. `jobs beyond budget` was
an aggregate with no entity reference — repaired by `adr/0097` and built as task 3.
`TripCounter.ExceededCommuteBudget` is the same figure on the **Building** — filed in
[`0012`](0012-corpus-audit.md) and moved to milestone 17. **The four `TripFate` Census flows are the
same thing again**, and nobody had walked that axis either.

⚠ **Why scoping missed it, and the form generalises.** `02 §9`'s Citizen row was read for the clause
that already had a known defect against it — *where there is no workplace, why*, which `adr/0097` had
flagged and annotated in `02 §9` itself — and the reading stopped there. ***A row of a requirements list
that carries one known defect reads as a row somebody has checked***, and the annotation is what makes
it read that way. That is [`0012`](0012-corpus-audit.md) *Cause 1*'s family on a new surface: not two
copies drifting, but **one copy whose repaired half vouches for its unrepaired half**.

**Not part of task 4**, because it is a hash-bearing table and task 4 is a query surface; mixing them
would put a re-record inside a commit whose subject is *no new state*.

⚠ **Its number is last and its content is not.** Task 4 assembles `02 §9`'s Citizen answer, and this is
one of that answer's clauses — so a Citizen result type designed before this lands gains a field when it
does. **That is accepted rather than resolved by reordering**: adding a field to a result type is
additive, nothing already returned changes meaning, and the alternative — designing the field now
against a table that does not exist — is the thing `02 §9`'s own *"cheap if designed in"* does **not**
license, because a shape designed for unbuilt state is a guess with a struct around it. Task 4 states in
its result type's own remarks that the clause is owed and to whom, so the gap is a sentence somebody can
find rather than an omission.

---

## What this milestone must not do

- **Not the drill-down graph.** It is Phase 3 and is named there. This row owns the accumulators.
- **Not trajectory detection or notification.** Phase 3, and its indicators fall out of milestones
  **10**, **15**, **19** and **20** — none of which exist.
- **Not store what a click can recompute.** The default is the assembler; the trail is the exception
  and needs an argument each time, namely *the entity holding this answer will not exist when the
  question is asked*.
- **Not put a shared mutable accumulator in a phase `Permission[]` marks `Parallel`.** If a trail write
  must happen in Move, it goes through a serial commit point, and the reason is written at the site.
- **Not attribute Trip failures to the Building they were aimed at, and not choose a window.** Both
  moved to milestone **17** on 2026-08-16, and the reasoning is the sharpest thing this scoping
  produced. `CONTEXT.md` names **three** sources of Failure Pressure — Trips failing, Rules reaching a
  reporting terminal, conditions below tolerance — and **only the second is built**: `starved_since` is
  started solely by `Blocking.Supply` (`RuleEngine.Stop:598-607`). Making the first one count is a
  **decline** decision, and ***Evidence reports pressure and does not produce it***.
  ⚠ **And the window derivation this brief recommended is refused by name.** It proposed reporting the
  rate over `kind.CondemnAfter × rule.Rate`, which `ZoneRuleEngine.Condemn` already computes.
  [`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)'s
  revisit triggers forbid exactly that: *"not bolted onto `0053`, whose predicate is about Bins and
  whose `CondemnAfter` is denominated in a **Rule's rate**. Two pressure sources sharing one threshold
  would make the number mean two things."* A Ruleset halving a Rule's rate would silently halve the
  Evidence window. ***A derivation that reuses a constant inherits every decision that constant is
  already carrying***, which is `adr/0094`'s `Speed.PerKilometrePerHour` and `02 §2.1`'s Cell/Chunk
  split on a third axis. 17 is the right home either way: `06`'s own inventory parks a **sibling window
  question** there — `01 §6`'s sustained-detection duration, *"derived from the time contagion takes to
  reach a neighbour"* — so the two belong together.
- **Not build a general Trip→Building link.** A `Handle<Building>` column on `TripTable` is a different
  mechanism with a different cost, and 17's counter will not need one either: `TripEngine.Start` holds
  `toBuilding` and `purpose` as live parameters at the line that reaches the verdict.

---

## Definition of done

The four cumulative obligations from `CLAUDE.md`, plus:

- Every aggregate the trail produces can be expanded into named entities, and **a test asserts the
  expansion**, not merely the total. A count that agrees with its constituents' length is the check.
- ✅ The trail's cap holds across a 100,000-Tick run — **slots saturating**, live count flat. **Done by
  task 6, and the wording was optimistic in one place**: slots do not *saturate*, they are **constant
  from Tick 0**, because the table allocates its rows in its constructor. Saturation is what a table
  that grows to a cap does; this one is born at it.
- `RebuildDerived` is unaffected: the trail declares no derived column, so there is nothing to rebuild
  and nothing that could rebuild to a different value.
- The State Hash moves, deliberately, once, with a commit whose subject says why
  ([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md):
  moving it costs nothing while nobody is carrying a save, and citing hash movement as a reason to
  defer is itself a defect). All three golden baselines re-recorded.
- `--evidence` prints something a human can read that no other runner mode can produce.

---

## Open decisions this milestone owes, before the task that needs them

### ~~The window~~ — **MOVED TO MILESTONE 17, 2026-08-16.** Not this milestone's

*Kept as a struck row rather than deleted, because the reason it moved is the boundary between this
milestone and the decline model and a future reader will otherwise re-derive it here.* See *What this
milestone must not do*. The live question — how a trailing-window rate is denominated without
authoring a decay rate `adr/0053` deleted, and without welding to a constant `adr/0079` forbids
reusing — travels with it.

### ~~1. The bound's value~~ — **CHOSEN 2026-08-16: 256. A guess we think is right, and it is not a measurement**

⚠ **Read this as *we believe this is fine and here is the analysis*, never as *this was measured*.**
The measurement was attempted first, on purpose, and **the world defeated it** — which is
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)'s
amendment arriving for the second time in three days: ***a ratifier names a machine, a world and a
quantity***, and here only the world is missing.

**What was run.** `--series --ruleset rulesets/minimal.toml --ticks 20480 --hash-every 32` — ten Days,
640 samples. No new instrument was needed; `ZoneCounter.Demolished` has been a Census flow since
milestone 10. **The city falls from 1,201 Buildings to ~570 and grinds there for ever, demolishing
9–19 every 32 Ticks without pause** — roughly 9,000 demolitions in ten Days, with `created` tracking
`vacant` and `demolished` tracking `occupied` sample for sample, which is the shipped Ruleset's
*"the Zone Rule's one-Lot sample rebuilds at the rate it demolishes"* visible in a series. The Unplaced
Pool runs 0 → 2,204.

***There is no wave, because abandonment here is not an episode.*** Every shipped Ruleset inherits
`minimal.toml`'s `upkeep` Rule drawing on a Resource nothing produces, so every Building is condemnable
64 Ticks after it is raised and decline is a permanent uniform grind at the fixture's maximum rate. A
window sized to hold an episode cannot be sized in a city that has none, and **all four files say in
their own headers that they model no city**.

**Why 256 ships anyway, and on what argument.** `RulesetTrailTable.Retained = 16` is the standing
precedent: hash-bearing, documented as *"a guess… an argument about a working habit and not a
measurement"*, shipped **unratified with a named ratifier that could not fire yet**. ⚠ **And the cap is
nearly free, which changes what the argument is about.** Entries are `Touch.Cold`, so they cost nothing
per Tick and the cost is bytes — an entry is on the order of 20, so 256 is ~5 KB against 86 MiB of
tables at 1M. **This is not a memory decision**, and sizing it small to be safe would be optimising the
one axis that does not bind. `adr/0006` requires a bound, not a tight one. So: *the smallest window
that holds a whole decline episode **plus the ordinary background of the period around it**, so the
episode is legible against its own baseline* — a diagnosis needs the contrast and not only the
casualties. ⚠ **Do not copy 16 from the trail** ([`0012`](0012-corpus-audit.md) *Cause 5*): that number
was derived from a **designer's** working habit and this one is sized by a **player's** diagnosis. Two
quantities sharing a unit.

**Named ratifier — machine, world and quantity.** The first real decline diagnosis on **a Ruleset that
models a city**, which is `06`'s own content row spanning milestones **13, 14 and 17** *(12, 13 and 17 before the 2026-08-18 economic reorder)*. Refuting
readings in both directions: a window **never filled** across a long run means it is too large; one that
**ran out of entries mid-episode** means it is too small. ⚠ **A bespoke declining fixture is
deliberately not built here** — milestone 17 owns decline and needs that world regardless, and building
it in the milestone least able to judge it duplicates the work and hides the judgement.

### ~~2. One window, or one per event kind~~ — **DISSOLVED 2026-08-16: this milestone has one kind**

The question was posed as *if demolitions, departures and budget refusals share one window, a busy
demolition period evicts the departure history that explains it*, and it recommended one window per
kind on that ground. The rate argument behind it stands and is stronger than the crowding one —
demolitions are rare and budget refusals could be thousands a Day, so a shared ring is whichever kind
is noisiest. **But walking the tasks shows milestone 6 has exactly one kind**: task 2 records
abandonment, task 3 is a count on the Citizen and not a trail entry, departures are milestone **19** and
unbuilt, and the budget-refusal counter left for **17**.

***So a generic N-kind trail would be an abstraction with one caller.*** `CLAUDE.md` is explicit that
three similar lines beat a premature abstraction, and `adr/0070` that an unbuilt mechanism is not a
design constraint. **One concrete trail, named for what it records**, and the arrival of a second kind
decides whether a shared abstraction exists — at which point there will be two real callers to derive
it from instead of one and a guess.

⚠ **This was found by starting task 1 rather than by rereading the brief**, and it came with a defect in
this document: the *Status* block said no open decision blocked task 1 while two of them said *owed
before task 1*. Both were right that they blocked it — a fixed-size table's capacity and columns cannot
be declared without them — and the *Status* line was the wrong copy. [`0012`](0012-corpus-audit.md)
*Cause 1* inside a brief written to prevent it, caught by the work.

### ~~2. Whether task 3 belongs to this milestone or to 19~~ — **CLOSED 2026-08-17: built here, read there**

**Taken as recommended, with the user in the room.** *Original body follows, because the recommendation
is the reasoning and nothing was added to it.*

`adr/0097` names milestone 19's Departure as the count's consumer, and `UnplacedTable.cs:9-14` routes
the give-up counter and Departure to the same place, warning that naming them earlier *"would be the
trespass the ADR was written to avoid."* **Recommendation: build it here, read it there.** The count is
an Evidence accumulator by `02 §9`'s Citizen row, and the standing rule is that the producer ships with
the mechanism that makes it legible, not with the one that eventually consumes it — which is
`adr/0069`'s finding, that a mechanism with one caller inside another mechanism's predicate is a
mechanism nobody built.

⚠ **What the split costs is one line in `UnplacedTable`'s warning and it is worth stating**, because
the warning is right and the closure has to survive it. `UnplacedTable` refuses to name refusal
reasons, a give-up counter or a Departure, on the ground that those are 19's. **This column is not one
of them.** A give-up counter is a *decision* — how much failure a Household tolerates before it leaves
— and that is a threshold, which is exactly what `adr/0097` refuses to choose. This is a *measurement*
of what the network did, taken at the only place that knows: inside the walk the assignment pass has
already paid for. ***A producer with no threshold in it is not a trespass on the milestone that owns
the threshold***, and the test of that is that nothing here reads the count — `grep` finds one writer,
one clearer, and no reader in `src/`.

### ~~3. What task 4 answers where the mechanism is missing~~ — **CLOSED 2026-08-17: assembled, accumulated, or omitted**

**Posed and settled before task 4's code**, because at least four of `02 §9`'s named answers have no
producer and the difference between answering three subjects and one and a half is a change in the
task's size rather than in its implementation. **Settled by walking the three subjects against the
column inventory rather than against this brief** — which is `adr/0093`, and see the sharpening at the
end of this entry.

**Assembled — live or recomputable, and task 4 owns all of it.** *Building:* occupants (the occupant
list), Bins with levels, **which Rule last ran and whether it succeeded** — recomputable, because a Rule
that succeeded re-arms at `+rate`, so `next_tick − rate` orders a Building's Rules by when they last
fired and `Blocking.None` says the last one worked — and **which fallback chain it walked and where it
terminated**, `RuleInstanceTable.Reported` being the terminal `ConditionId` and the walk itself
re-runnable on the cold path. *Citizen:* home, workplace, activity, **why there is no workplace**
(`CitizenTable.ReachFailures`, task 3), and the **current** Trip with its Fate, by scanning
`TravellerTable.Citizen`. *Lot:* **no frontage** (`LotTable.HasFrontage`) and **whether the Unplaced
Pool is empty** (`UnplacedTable.Count`), plus whether any `[[zone_rule]]` admits the Lot's zone bits at
all (`Lots.Zone[lot] & definition.Admits`).

⚠ **CORRECTED 2026-08-17, within the hour, and the correction is the entry's own lesson committed
against itself.** This paragraph first put *conditions below tolerance* here, on the reasoning that a
Zone Rule decides whether to build and therefore weighs conditions, so the assembler could **re-run its
permit predicate**. **`ZoneRuleEngine.Create:236-241` was not opened.** Its predicate is **two clauses**
— the zone bit match and `UnplacedPool.Count == 0` — and **there is no tolerance term in it, or
anywhere**: `MapLayers.Desirability` *throws* by design (`02 §2.4`, `adr/0034`). So there is no
predicate to re-run, and the clause moves to *omitted* below. ***I reasoned from what a Zone Rule is
for rather than from what `Create` contains***, which is `adr/0093` exactly, wrong about the **content**
of a mechanism whose **purpose** I had right — and it happened in the same entry that argues *open the
mechanism* has to mean find the writer. **A rule you have just written down is not thereby a rule you
are following.**

**Accumulated — the row holding the answer is freed.** The Lot's *why it is vacant* has this and task 2
built it. `02 §9`'s Citizen row has a second one nobody had seen: the **last** Trip's Fate. Task 7 above.

⚠ **So `02 §9`'s hardest question comes out half-answerable, and it is worth saying in the sentence the
document uses.** It calls *why is nothing building here* **"the hardest and the most valuable"** and
names four reasons. **Two are computable today** — no frontage, and nobody in the queue — **one is
recorded** by task 2 where the Lot was vacated by demolition, and **two of the four are named holes**
with no mechanism at all. The assembler reports what it has and does not manufacture the rest; the
milestone that closes it is **17**, which owns decline and desirability alike.

**Omitted — no producer, and omitted rather than returned as zero.** Need satisfaction (no table exists
anywhere; `adr/0103` designs it and nothing builds it). **Conditions below tolerance** — there is no
tolerance term in the Zone Rule's predicate and no desirability to read, `MapLayers.Desirability` being
a named hole that throws. **Household finances** — and this one is the
trap: `HouseholdTable.Money` and `Savings` are declared, `Saved`, hashed and in the save file, and
**every writer in the repository is a test**; the only production code that touches them is an invariant
*reading* them. The Lot's *no capital* is the same column. And the Lot's *no household in the queue
**that would accept it*** — the Pool's membership exists, but the acceptance predicate is `02 §5.4`'s
**residential choice model**, which the 2026-08-15 sweep found had been invisible to `06`'s inventory
for its whole life.

⚠ **Why omitted rather than zeroed, and the reason is deliberately not the absence.** A Household with
no money and a Household in a world with no money both read **£0**. That is session F's finding —
***a placeholder whose value sits inside the range of legitimate answers cannot announce itself*** — and
zero is inside money's range. So the rule task 4 follows is *do not publish a number that cannot be
distinguished from a real one*, which would hold just as well if money were built and merely unread.
**Reasoning from the absence instead would be the void form**: *given money does not exist, should the
assembler compensate?* is `adr/0070`'s forbidden shape verbatim, and it would have produced
availability machinery — a status per clause — that exists only because something is missing.

**The omission needs no mechanism to undo.** The assembler recomputes, so the day money acquires a
writer the field appears and nothing here is reopened. That is `02 §9`'s *"cheap if designed in and
expensive if retrofitted"* paying out in the direction it was written for, and it is the strongest
argument for the assembler being an assembler.

⚠ **The sharpening this produced is about how to check a mechanism exists.** `HouseholdTable.Money`
looks built from every angle a *document* can see — declared, typed, hashed, saved, invariant-checked —
and `grep` for its writers is the only thing that says otherwise. Session K found the identical shape
(*"two of the six roots are half-built… declared with only test callers"*), so this is the second
sighting and the first inside a milestone. ***`adr/0093`'s *open the mechanism* has to mean find the
**writer**, not find the declaration*** — a declaration is a description of the build in the build's own
language, which is the most persuasive kind and still not evidence that anything happens.
