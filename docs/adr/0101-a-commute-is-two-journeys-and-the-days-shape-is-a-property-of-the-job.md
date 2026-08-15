# A commute is two journeys, and the Day's shape is a property of the job rather than of a curve

**A Citizen with a Workplace makes two journeys a Day — out and back. Both are anchored on a Shift start
hour that belongs to the *Workplace*, so a Citizen who changes jobs changes their hours. A Building draws
its start hour once from its own id, inside a band its kind authors in the Ruleset; a Citizen draws a
Shift length once, persisting, so the evening is diffuse where the morning is disciplined. A Citizen
departs at `start − their own planned commute − their own punctuality margin`, decided when the job is
taken and saved, not recomputed every morning.** The Day's profile — two peaks, an all-day baseline, a
quiet night — is therefore **emergent**, and `[jobs] commute_peak_factor` is **retired**: the peak becomes
a measured output rather than an authored dial.

⚠ **Three of this decision's own statements were wrong and are corrected below by the build that
implemented them** (§*What building it changed*): the start draw is **triangular** rather than uniform, a
Shift length is drawn in **Ticks** rather than in whole hours, and there is a **fourth** number —
`arrive_early_max_minutes` — that this record originally counted at three.

**And Tick 0 of the Day is midnight**, stated here once because this decision is what spends the freedom.

Guiding concepts: `EMERGENCE`, `UNIQUE INDIVIDUALS`, `LEGIBLE CAUSE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
as to the shape — whether departure follows a per-entity anchor or a population curve is a question about
what the design already says, and no measurement decides it. **The bands are not arguable** and carry a
named ratifier under
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).

## Why

### The standing refusal was argued from mechanisms that do not exist

`CommuteEngine` declines the evening leg in its own remark, and the ground is explicit: *a return journey
makes a Citizen's day a schedule, and a schedule is what arrives when `adr/0067`'s shopping or
`adr/0032`'s school gives it a second entry — so building half of it now is building a structure whose
shape is decided by mechanisms that do not exist*, citing
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md).

⚠ **That is `adr/0070` inverted.** The rule says an unbuilt mechanism is **not** a design constraint, and
the refusal uses two unbuilt mechanisms as exactly that. Both shopping and school are *unbuilt* in
`adr/0070`'s sense — specified, no builder — and neither is *refused*, so neither is evidence about
anything.

The substantive half of the objection also fails on inspection, and it fails in a way worth stating
because it is what makes this journey the right first one. **A return commute is the one journey whose
shape a later generator cannot change.** Its endpoints are fixed and already stored — `Workplace` and the
Household's Building — and its occasion is the end of a Shift, which is a fact about the job rather than
about the person's day. A shopping Trip is genuinely undecided in destination, in occasion and in
frequency; the trip home from work is undecided in none of them. Building it now commits nothing that a
schedule would later have to unpick.

### A departure curve over a population is the thing this design refuses

The alternative shape is a per-Tick profile in the Ruleset that the engine samples: *this fraction leaves
now*. It is less code, and it is
[`0005`](0005-two-fidelity-tiers.md)'s prohibition wearing a clock — a distribution over the population
standing in for the individuals' own reasons, which is what `CONTEXT.md` bans under *Cohort* and what the
absence of an RCI meter bans on the demand side. It also cannot express the one relationship that makes a
real peak broad, because a curve knows nothing about where anybody lives.

**Per-entity anchors give the same profile and give it a cause.** Every feature asked for falls out
without being authored:

| asked for | where it comes from |
|---|---|
| a morning peak | the mass of the Shift-start band |
| a broad morning peak | `start − own commute`: living further out means leaving earlier |
| an evening peak, flatter | one start hour per Workplace, many Shift lengths per Citizen |
| commutes all Day | Buildings whose drawn start sits outside the peak |
| a quiet night | a band with almost no mass there — *generally* dead, not enforced |

That table is the whole argument. `LEGIBLE CAUSE` is the test the curve fails: asked *why is this road
busy at this hour*, the curve answers *because index 731 of the profile is 0.06*, and the anchor answers
*because the works on the next block starts at seven and these people live twenty minutes away*.

### The anchor belongs to the job because that is where the fact lives

Hours are a property of work, not of a person. A baker starts at five and an office at nine, and a Citizen
who moves between them moves their hours with them. Putting the anchor on the Citizen would make the fact
travel with the wrong entity — it would survive a job change, which is observably wrong, and it would make
the profile a property of *who lives here* rather than of *what is built here*.

**So a Citizen stores no start hour at all.** They read it from `Workplace`, and a job change is therefore
a schedule change with no write and no invalidation of its own. This is
[`0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s
disposition on a fifth axis — after a Bin's capacity, a kind's occupancy, its `jobs` ceiling and
`adr/0098`'s car ownership: derived from the Ruleset in force through the Building, so retuning a band
reaches the standing city.

⚠ **The kind authors a band and the Building draws inside it, rather than the kind authoring an hour.**
The corpus has exactly one Building kind today (`dwelling`, carrying `jobs = 8` — 5b-bis task 4's finding,
recorded there as a compromise), so a start hour authored per *kind* would give the entire city one start
hour and one enormous peak: **worse than the uniform window it replaces.** A band plus a per-Building draw
gives a real spread at one kind and gives land use its texture at several, which is the arrangement that
survives the second kind arriving. `adr/0059` a fifth time — the file states the thing a designer has a
reason for, and the engine derives the rest.

### The planned commute is saved because it is a fact about a moment, not about a state

`departure = start − commute` needs a commute duration, and there are two available: the one this journey
will actually take, and the one the Citizen expected when they took the job. **It must be the second.**

The first makes the departure phase move with congestion, which is wrong twice over. It re-rolls a
decision the design says is made once — `CONTEXT.md` → Provider List states it directly, *how I get to
work is decided when the job is taken, not every morning* — and it is
[`0046`](0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)'s Habit read from the other end. It is also
structurally unaffordable: `CommuteRoster` is a `(derived AND rebuilt)` partition keyed on the departure
phase, so a phase that moves with load is a partition rebuilt every Tick.

So the Citizen carries a **saved** departure offset, written when `EmploymentEngine` assigns the job, and
the roster stays a pure function of saved state. ⚠ **This is a real loss and it is worth naming**: today's
phase is derived from the Citizen's monotonic id for free, and this makes it a saved, hash-bearing column.
What is bought is that the departure has a **history** — it is a consequence of which job was taken and
what the journey looked like then — which is the property that makes a Citizen leaving too late for a
commute that has since got worse a diagnosable thing rather than an impossible one.

### Tick 0 is midnight, and saying so is the point

`CommuteEngine` declines an offset key on the ground that *no Rule, no Layer and no Zone Rule asks what
time of day it is, so when the peak falls within the Day is unobservable*, and that is correct today. **A
Day with a quiet night makes it false**, because the profile's own shape distinguishes its ends: a gap
between two peaks is observable without any clock, which `plans/0002` §C reached from the other side —
*a phase is unobservable and a gap between two phases is not*.

So the freedom is spent here whether or not anybody writes it down, and
[`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md)'s finding is that
**a degree of freedom is spent by the first document that uses it, and nothing announces the spending**.
This announces it. Tick 0 is midnight, every later clock-reader inherits that, and no `peak_offset` key is
opened — the anchor is a convention rather than a tuning number, so it has nothing to ratify.

⚠ **An hour does not divide the Day, and the anchor model is why that does not matter.** `Ticks.PerDay` is
2048, which is 2¹¹, and 24 does not divide it: an in-world hour is **85.33 Ticks**. Nothing here repeats an
hourly period. A band is a range of Ticks and a draw lands where it lands, so the twenty-four hour marks
are twenty-four anchor Ticks computed once (0, 85, 171, 256, …) and the ±1 Tick unevenness is **42
seconds**. A profile that sampled an hourly period would have accumulated a third of a Tick an hour, eight
Ticks a Day, for ever.

## Consequences

**The commute doubles the city's Trips, which is the point.** `plans/0002` §C prices *a Day with a shape*
at ~2× and calls it the term that *makes the others legible*; this is that term. It is one of four and it
is not on its own enough to congest anything — see the revisit trigger below.

**`CommuteRoster` becomes two partitions.** An outbound bucket list and a return one, each
`int[Ticks.PerDay]`, plus a second `next` column on `CitizenTable`. ~256 KB, flat, still not sized from the
population. Its derivability argument **survives and must be re-derived rather than assumed**: the phase is
now a function of saved state (the departure offset and the Workplace) instead of of the id, so it is still
a pure function of the save and still exactly reproducible in slot order — but for a different reason than
the one its remark currently gives, and that remark is wrong the moment this ships.

**`CitizenTable` gains exactly one saved column — the departure offset — and the State Hash moves.** Under
[`0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) that costs a
re-record and an explaining commit subject, and nothing else.

⚠ **The Shift length is *not* the second column, and the asymmetry between the two is the interesting
part.** A Shift length is a property of a *person*, so it is a draw on the Citizen's monotonic id against
the band in force — free, no column, and a retuned band reaches the standing city, which is
`adr/0064`'s disposition again. The departure offset cannot be that, because it is not a property of the
person at all: it is what the journey **cost when the job was taken**, and no function of an id and a
Ruleset recovers a fact about a past world. ***A value drawn once is derivable and a value measured once
is not*** — both are decided once and never revisited, and only the second has to be written down.

**`[jobs] commute_peak_factor` is retired, and a `plans/0002` §D row is lost by derivation.** The peak stops
being authored and becomes a reading. That is `adr/0059`'s direction and the **second** time a row here has
lost a quantity rather than gained a value — the first was the Zone Rule's `sample`. Its named ratifier
(task 8's quiet fraction) is superseded along with it, and the assertion that replaces it is over the
*shape* rather than the concentration.

**The loader gains a refusal, replacing a guarantee that arithmetic used to supply.** Today nobody can be
in flight when their next departure falls due, because a Citizen departs once a Day and the Commute Budget
bounds the journey in minutes. With two departures the gap is the Shift length, so the loader must refuse a
minimum Shift length at or below the Commute Budget's ceiling. ⚠ **The old guarantee was a happy accident of
there being one journey, and it read as a design property** — it is now an explicit refusal at the door,
which is where the `[jobs]`-without-a-Budget refusal already lives.

⚠ **~~Three~~ Five new hash-bearing numbers, all Ruleset content**: a kind's Shift-start band (two), the
Shift-length band (two) and the punctuality margin (one), against one retired. *(The count said three and
was wrong twice — it named four quantities and called them three, and it did not know about the fifth.)*
Their named ratifier is **a picture** — the departure profile over a whole Day, read against the shape
this decision claims. That is a real ratifier and not a category: a profile with one spike, or with a busy
night, refutes it on sight, and both readings were taken during the build.

**`TripPurpose` does not gain a member.** A journey home from work is a Commute; the purpose names the
*reason*, and nothing downstream branches on it (`adr/0080`). What changes is that a Commute now has a
direction, which the Trip's endpoints already carry.

## What building it changed

Written on the day this shipped. Everything here is a correction to the record above, and the record is
amended in place rather than quietly rewritten because **three of the four items are this ADR being wrong
about its own mechanism** — which is `adr/0093` with the roles reversed, a *prescription* rather than a
description, and wrong in the same place descriptions are wrong: not about the purpose, about the shape.

### The start draw is triangular, and the warrant came from inside the mechanism

This ADR says a Building draws its start hour *inside a band*, meaning uniformly. **A uniform draw over a
four-hour band gives a plateau, not a peak**, and the first profile measured exactly that: five roughly
equal bars across the morning, because there were **five start hours × five Shift lengths = twenty-five
discrete `(out, back)` pairs in the entire city** and the only continuous term was a four-minute commute
against an eighty-five-Tick hour. *A city of four thousand people was making twenty-five distinct
decisions.*

The start hour is now the **mean of two draws** against the band, which is triangular — peaked in the
middle, thin at the ends — and that is what a morning rush looks like.

⚠ **The warrant for that shape is the load-bearing part, because inventing a distribution here would have
been `adr/0043` violated in one line.** It was not invented: **the evening peak was already triangular and
nobody put it there.** A return is `start + shift`, the sum of two independent draws, and the sum of two
uniforms *is* triangular — so the mechanism had already produced the shape at one end of the Day, for a
reason, and the morning was the end that lacked it. ***A shape a mechanism already produces somewhere is
not a free parameter when it is adopted elsewhere*** — the same construction, one draw against a band and
one against another, generates both. The alternative was to author a curve, which is the thing §*A
departure curve over a population is the thing this design refuses* exists to forbid.

### A Shift length is drawn in Ticks, and the unit was the defect

This ADR says *a Citizen draws a Shift length*, and the first build drew it in **whole hours** because the
band is authored in hours. That is the surface form deciding the denomination, which is `adr/0094`'s
`revisit_ticks` finding and its `capacity`-in-Goods finding, both of them, in a third place: ***the unit a
quantity is authored in is not the unit it is drawn in***. Five whole-hour lengths quantise every return
in the city onto five Ticks of the Day. The band is still authored in hours — a designer has a reason for
*six to ten* and none for *a range of Ticks* — and `Ticks.AtHour` converts the two ends before the draw,
which is `adr/0059` again inside a single expression.

`Ticks.AtMinute` exists for this, and its remark carries the asymmetry: **a Shift length and a punctuality
margin want sub-hour resolution and a Workplace's start hour does not**, because workplaces really do open
on the hour and spreading that would delete the texture rather than smooth it.

### There is a fourth number, and it is what fills the gap between the hour marks

`arrive_early_max_minutes` — how long before their Shift starts a Citizen aims to arrive, drawn per
Citizen and persisting. It is not in this record above because it was not foreseen, and the mechanism that
needed it is arithmetic rather than design: **departure is `start − commute − margin`, and with no margin
the only thing separating a departure from an exact hour mark is the commute duration**, which on a
generated city is about four minutes against an eighty-five-Tick hour. Without it the morning is a comb.

⚠ It is **not** a fix for the quantisation and must not be read as one — the triangular start draw and the
Ticks-denominated Shift length are that. It is a real quantity a real commuter has, and it happens to be
the one that smears the last hour boundary.

### `CommuteRoster` needed a saved bucket, and the reason generalises past this ADR

This record says the roster stays *a pure function of saved state*, and that is true. What it does not say
is that the roster must be able to **unlink** a Citizen, and the first build recomputed the bucket from
the Citizen's `Workplace` handle in order to do it. `Workplace` is a `Reference.Severable` handle:
demolition invalidates it with **no hook and no notification**, so after a Workplace was demolished the
recomputation answered *not rostered*, `Remove` became a silent no-op, and every subsequent re-employment
inserted the Citizen a **second** time. Buckets grew with elapsed time on a Ruleset that demolishes — an
`adr/0006` leak with a quadratic tail, which is how it announced itself: 256 Ticks in 4.9 s and 512 Ticks
not finishing in two minutes.

The repair is a derived packed column, `CitizenTable.CommuteBucket`, written where the Citizen is linked
and read where they are unlinked. ***An intrusive index that unlinks by recomputing its key cannot outlive
a change to that key's inputs.*** ⚠ **The old single-partition roster was safe only by luck**: its key was
the Citizen's own monotonic id, which nothing can change, so the rule had never been tested and reads
nowhere in the corpus. Every other intrusive list in `Borough.Core` is keyed on an owner slot and unlinks
by walking from a stored head, so this is the one that was structurally exposed.

### The middle of the Day is land use, and the direction of the fix is not the obvious one

The shipped profile has **exactly zero** journeys between 10:00 and 11:45, and that is correct rather than
broken: `rulesets/minimal.toml` declares **one** employing kind, so every workplace in the world keeps the
same sort of hours and there is genuinely nobody travelling at eleven. That is `adr/0070`'s *build X*, and
X is a second `[[building]]` kind. It was built — as a test-local Ruleset variant, so the shipped file
keeps its own first line and makes no content decision — and the hole closes.

⚠ **The direction was measured backwards first, and the correction is a property of this ADR's own model
that this ADR does not state.** The obvious second kind is a **later** one, and it does fill the
afternoon — and it also fills the night, because its returns land at `start + shift`. ***A Day's quiet end
is bounded by `latest start + longest Shift`***, and the night is not authored anywhere and cannot be
defended directly, so the only band that adds midday traffic without waking it is one starting **earlier**
than the shipped one. An early shift puts its **returns** in the middle of the Day, which is where a real
city's midday travel actually comes from. Both readings are in `CommuteLongRunTests`, and the one-kind
test now asserts its hole as the control for the two-kind one rather than declining to assert it.

## What would trigger revisiting

- **A third journey in a Citizen's Day** — `adr/0067`'s shopping or `adr/0032`'s school. Two anchored
  partitions is not a schedule; three journeys is, and at that point the Event Wheel argument
  `CommuteRoster` retired reopens on its own terms, because the bucketing stops being a function of a
  constant.
- **A second mechanism that reads the time of day** — shop opening hours, a school bell, a night-shift
  premium. It inherits midnight from here, and the first one that wants a different anchor is the one that
  reopens this.
- **Night shifts becoming a design goal.** A band that wraps midnight is a different structure from a band
  that does not, and nothing here supports one.
- **A measured departure profile that does not look like a city** — the named ratifier firing.
- ⚠ **Congestion still failing to appear.** This is ~2× of `plans/0002` §C's ~20× stack. If the profile
  lands as designed and the road is still empty, the remaining terms are employment, more generators and
  the population — **and the supply side**, where the generated lattice grows with the population it
  serves. This decision must not be read as the fix for that; it is one term of four, and
  [`0090`](0090-the-generator-makes-land-and-the-player-makes-every-road.md) is why the last one is not a
  number at all.
