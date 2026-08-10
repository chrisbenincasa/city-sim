# 0016 — Slice 9: the Event Wheel, and the period it is a modulus of

> Slice 9 of [`0003-build-plan.md`](0003-build-plan.md).
> Governed by [`adr/0056`](../docs/adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md),
> [`02 §7`](../docs/02-simulation-model.md),
> [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md),
> [`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md),
> [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md),
> [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).

**The wheel exists. What does not exist is any check that a row is on it exactly once, at a Tick that
is actually in the future.** Slice 7 built a bucket per Tick, an arming with a refusal, and a drain,
and session C settled the semantics around them. This slice closes the gap between the two: it states
the partition `adr/0056` asserts *with the domain it actually holds over*, refuses at the write site
what today is only caught at the end of a run, and strengthens a whole-world check that is blind to
the one error the wheel's own arithmetic makes possible.

**The risk it retires** is narrower than the slice's name and worth stating exactly, because the wide
version is not true: *the Wheel is the single largest performance lever in the design (`02 §7`) and
every guarantee it makes is checked by one function that runs at the end of a run.* Two mechanisms
that will exercise it — a Ruleset reload's drop-and-re-arm and a save/reload — are being written now
and not built at all respectively, and **the check that would catch either getting it wrong is the one
session C found has never run** (invariant 6, the Factorio test).

**The second risk is this slice's own, and it is the opposite of every other slice's.** Slice 9 is
billed in [`0003`](0003-build-plan.md) at four tasks and in the board as *"finish the fine wheel slice
7 half-built"*. Planning found that the fine wheel is not half-built — it is built, and both halves of
the invariant session C extracted are in the tree, registered and tested, and have been since slice 7.
**The danger here is manufacturing work to fill a row.** What follows is deliberately smaller than the
row it discharges, and says so.

---

## Status

**All four tasks are done. 776 tests, up from 772, and no baseline moved** — which is the acceptance test this slice set itself.

What the code found is worth more than the four tasks, and it is in *What building it found* below.

~~Slice 8 is in flight in another session, at task 4 sub-tasks B and C. **The two slices touch the same
mechanism**, and unlike slices 8 and 10 — which collided on a *file* and a *baseline* — these two
collide on a *claim*: sub-task C is the project's first bulk unlink-and-re-arm pass over the world,
`0015` says of it that *"nothing in the shape of a re-arm loop makes the exclusion obvious"*, and tasks
2 and 3 below are the refusals that would notice a re-arm loop getting it wrong.~~

**Slice 8's task 4 has since landed** ([`0015`](0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md)),
green at **772 tests**, and it is **not a re-arm pass**: `World.Migrate` frees every Rule Instance and
`Fit` allocates fresh ones, so nothing is ever armed twice and the collision this section anticipated
did not exist. Tasks 2 and 3 keep their derivations and lose their urgency; task 4 gains a second
caller. See *Ordering against slice 8*, which is kept as written with the refutation on top of it.

## Gate

✅ **Cleared by session C** →
[`adr/0056`](../docs/adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md). The gate was `02 §7`
plus `adr/0006`, and the board's instruction to read `02 §7` **against `adr/0033`** rather than fresh
is what found the Life Stage arithmetic. Nothing further is owed before code.

## Prerequisites

| | State |
|---|---|
| `EventWheel`, `WheelBucketTable`, `Arm`, `PopDue`, `Armed` | **In the tree** since slice 7, `src/Borough.Core/Rules/EventWheel.cs` |
| The buckets folded into the State Hash | **Yes** — `World.cs:131`, `Buffering.OneCopy`, `Saved`, 64 KiB |
| `Invariant.RuleInstanceIsArmedOrWaiting` | **In the tree**, both the write-site half (`World.cs:1006`) and the whole-world half (`WorldInvariants.RuleInstancesAreQueuedExactlyOnce`, `InvariantTier.EndOfRun`) |
| `Invariant.NoFreedRowIsStillLinked` | **In the tree** and tested (`InvariantTierTests.cs:294`) |
| The current Tick, inside an invariant | **Available** — `InvariantRegistry.Tick`, set at `InvariantRegistry.cs:171`. No new plumbing |
| The wake-rate instrument `02 §7` owes a number for | **Available** — `RuleCounter.Due` × `Aggregate`, slice 7 task 9 |
| A second scheduled table | **Does not exist**, and that is load-bearing below |

---

## What planning found

Seven things. Three remove work, three are corrections, one is the slice.

### 1. The fine wheel is not half-built, and the invariant is already in the tree — both halves

`adr/0056`'s consequence bullet asks slice 9 to state that *"every scheduled row is in exactly one of
{armed, waiting}, and is unlinked when its owner row is freed."* Both clauses exist.
`RuleInstancesAreQueuedExactlyOnce` walks the two Bin wait lists and all 8,192 buckets, counts each
live row's appearances, and reports `RuleInstanceIsArmedOrWaiting` for any count that is not one; and
`Tally` reports `NoFreedRowIsStillLinked` for a dead row found in any of them. Registered at
`InvariantTier.EndOfRun`, exercised by `BinTests` and `InvariantTierTests`.

**So the deliverable session C named was delivered before the session ran.** That is not a criticism of
the session — it settled the *semantics*, which is what the gate was for, and the code being ahead of
the ADR is the same shape as `adr/0033`'s wait lists, found by the same session. But it means slice 9's
content is whatever the existing checks *do not* catch, and finding that out is the whole job. Tasks 2
through 4 are the answer, and they are three specific holes rather than a generalisation.

### 2. The partition has a third state, and `adr/0056` states it unqualified

`RuleEngine.CollectDue` pops the whole bucket into `_due` and Phase 3 puts each row back. Between those
two points a due row is **on no queue at all** — neither armed nor waiting. The code says so exactly
(`RuleEngine.cs:217`: *"Between here and the end of Phase 3 a due row is on no queue at all, which is
the one window in which `RuleInstancesAreQueuedExactlyOnce` would fail"*), and it is why the check runs
at the end of a run rather than mid-Tick.

`adr/0056` states the partition with no domain. **The code's comment is more accurate than the ADR**,
which is the second time in two sessions that has been true of the Wheel. The claim as written is false
for two of the eight phases, and the ADR then extends it to every future consumer — *"every new consumer
inherits the obligation"* — so what a Phase 2 author inherits is a claim they will find false the first
time they check it mid-Tick, with nothing to tell them whether they have a bug or a bad invariant.

The repair is a sentence, and the sentence is **not** *"except in flight"* — it is the domain: the
partition holds **at a phase boundary**, and the in-flight window is bounded by *one phase-3 apply*
rather than by anything a consumer chooses. An amendment to `adr/0056`, owed by this slice, not a code
change.

### 3. `Arm` has no write-site refusal, `Subscribe` does — and the missing predicate is free

`World.Subscribe` requires `!IsWaiting(slot)` at the write site, so a double subscribe is caught `O(1)`
where it happens. `EventWheel.Arm` requires nothing: arming a row that is already armed appends it to a
second bucket, and the only thing that notices is `RuleInstancesAreQueuedExactlyOnce`, at the end of a
run, having lost every bit of context about which call site did it.

The reason the asymmetry survived is that *armed* looks unrepresentable. `Blocked == Blocking.Nothing`
is true of an armed row **and** of an in-flight one, so it cannot be the predicate. But the wheel
already stores what distinguishes them:

| State | `Blocked` | `NextTick` against `now` |
|---|---|---|
| waiting | not `Nothing` | stale, and meaningless |
| **in flight** | `Nothing` | `== now` — it was popped for this Tick |
| **armed** | `Nothing` | `> now`, strictly, because `Arm` refuses a delay of zero |

`NextTick < now` is unreachable while the drain runs every Tick. So **armed ⇔ `Blocked == Nothing &&
NextTick > now`**, `Arm` already takes `now`, and the refusal is one comparison with no new column and
no hash consequence. **`Arm`'s existing refusal is what makes this exact** — a delay of zero would
collapse the two rows of that table into one, which is a second reason not to relax it and one the ADR
does not give.

### 4. The whole-world check is blind to a whole period, which is the one error the wheel can make

`armedHere` is `Blocked == Nothing && EventWheel.BucketOf(NextTick) == bucket`. `BucketOf` is
`NextTick.Raw % 8192`. **That predicate is invariant under adding a whole period to `NextTick`**: a row
due 8,192 Ticks ago and a row due next period sit in the same bucket and both pass. The check cannot
distinguish *scheduled* from *a period out of date*, and the quantity it is checking is the one the
wheel is a modulus of.

Today it is unreachable, because the drain visits every bucket in order for ever. **The two things that
can reach it are a Ruleset reload's drop-and-re-arm and a save/reload** — the first being written in
another session this week, the second being the invariant whose machinery session C found does not
exist. That is a hole whose only defence is a mechanism nobody has built, guarded by a check written
modulo the very number the error is a multiple of.

The strengthening is `report.Tick < NextTick && NextTick < report.Tick + Size`, and note it has to be
spelled as an addition on the right: `Ticks` deliberately has no subtraction operator (`Ticks.cs:14`),
which is the arithmetic substrate's rule doing its job here rather than getting in the way.

### 5. `Unlink` discards the one signal that says whether it unlinked anything

`IndexList.Remove` returns `bool` and returns `false` when the node is not in the owner's list.
`World.Unlink` calls it for an armed row and **discards the result** (`World.cs:1160`). For an in-flight
row — `Blocked == Nothing`, not on the wheel — `Unlink` takes the armed branch, removes nothing, says
nothing, and the row is then re-armed or subscribed by Phase 3 *after having been freed*. The end-of-run
check reports `NoFreedRowIsStillLinked`, a whole run later, about a row whose slot has probably been
recycled twice.

**It is unreachable today only because Zone Rules demolish in Tick phase 6, which is after Phase 3.**
That is a property of the phase order and nothing in the tree states it. This is the shape slices 7 and
10 kept finding — a green check that holds by accident of ordering — and the cheap repair is to stop
discarding the bool, because a removal that removed nothing is either a bug or a state the caller must
have thought about.

### 6. The scope argument names a mechanism that is not on the wheel — and the conclusion is stronger than the argument

`adr/0056`, [`0003`](0003-build-plan.md) and [`0000`](0000-board.md) all justify *fine wheel only* the
same way: *"every `rate` in `rulesets/minimal.toml` is 8–32 Ticks and the Zone Rule interval is 32."*
**A Zone Rule never touches the Event Wheel.** It triggers on `tick.Raw % definition.Interval != 0`
(`ZoneRuleEngine.cs:172`), and `adr/0033` says it must not be on the wheel — a Sweep Rule was admitted
*because* subscription is wrong for a population, so a wheel *"buys nothing and costs a wheel entry
apiece"*.

The conclusion survives and gets stronger. The fine wheel does not have *a set of consumers whose
sleeps are all short*; it has **exactly one consumer, structurally**, and the second Rule family is
barred from ever being a second one. That is `adr/0044`'s *citing is not applying* again — the interval
was cited as a wheel arming without checking that it is one — and this is its first appearance in a
scope argument rather than in a decision or a measurement.

**And it removes the largest piece of work anybody might have read into the slice.** `EventWheel` is
hard-typed to `RuleInstanceTable` — it reads `QueueNext`, `NextTick` and `WaitingOn` by name.
*One wheel per scheduled table* is therefore **unexercisable in slice 9**: generalising a type against
one consumer produces the generalisation that consumer happens to want, and `adr/0056`'s own rule is
that *a wheel is added when its consumer exists*. **Slice 9 must not make the wheel generic**, and that
has to be written down, because the ADR's per-table clause otherwise reads as an instruction this slice
skipped.

### 7. `02 §7`'s unmeasured number is already instrumented, and so is `W`'s ratifier

Session C typed *"a few hundred out of hundreds of thousands"* as **measurable** with S0b named, and
the refuting number is the mean armed rows drained per Tick at 1M. That instrument shipped with slice 7
task 9: `RuleCounter.Due`, read as a **sum and a peak** over the reading interval, drained on read.
Because there is exactly one wheel (finding 6), *due Rule Instances per Tick* **is** the wake rate.

So `02 §7`'s number needs **no new instrument** — it needs a run, which is S0b's and not this slice's.
The same is true one Phase out: `W`'s named ratifier in [`0002`](0002-open-questions.md) §D2 states its
refuting number as *"peak armed rows drained on a cascade Tick"*, which is this counter, read as a peak.
**One caveat, and it is the price of finding 6:** `Due` is a Rule-engine counter, so a second wheel
would need a second counter rather than inheriting this one — the Census's per-family counter blocks
have the same one-per-table shape the wheels do, and nobody has noticed that yet.

---

## Ordering against slice 8

> **OVERTAKEN, and the argument below is half refuted by the code that overtook it.** Slice 8's
> sub-task C landed first, green at 772 tests, and **`World.Migrate` does not re-arm in place**: it
> walks every Building, `Unlink`s and **frees** every Rule Instance, remaps the Bins, re-kinds the
> Building, and then `Fit` **allocates fresh instances** through `CreateRuleInstance`. A row is
> therefore never armed twice, **structurally** — so task 2's refusal would have caught nothing in the
> pass this section called the first caller that could trip it. **The urgency argument was wrong and
> the derivation was not**: `Arm` still has no write-site check where `Subscribe` does, and the
> predicate is still free. Task 2 survives on correctness and loses its ordering claim.
>
> **Task 3 narrows the same way.** `Fit` arms on slice 7's `[1, rate]` stagger, so every `NextTick` the
> reload path writes is inside the period and finding 4's blind spot is not reachable through a reload
> either. Its one remaining path is **save/reload**, which does not exist — so the period bound is a
> check written in front of a mechanism rather than behind a caller.
>
> **Task 4 is the one that got stronger.** `Unlink` now has **two** callers relying on phase order for
> their safety — Zone Rule demolition at phase 6, after Phase 3, and `Adopt` at phase 0, before Phase 1
> — and both are safe because no row is in flight at either point, and *still* nothing anywhere states
> it. One caller relying on an undocumented ordering property is a smell; two is the argument.
>
> **The general lesson is the corpus's own, arriving at a plan instead of at an ADR.** This section
> predicted the shape of code that had not been written and got it wrong in the safe direction — it
> assumed a re-arm loop would re-arm. `0015`'s own worry (*"nothing in the shape of a re-arm loop makes
> the exclusion obvious"*) was answered by a guard at the top of `Fit` rather than by the invariant
> either document expected. What follows is kept as written, because the reasoning is what was wrong
> and striking it would hide that.

The collision is on a claim, not a file, and it points one way.

| | Slice 8 sub-task C | Slice 9 tasks 2–4 |
|---|---|---|
| What it does | unlinks every Rule Instance in the world and re-arms it, inside `World.Adopt` | refuses a double-arm at the write site; strengthens the whole-world check to the period; stops `Unlink` discarding its removal |
| What it says about itself | *"nothing in the shape of a re-arm loop makes the exclusion obvious"* | these are the three things that would notice |
| Baselines | re-records, deliberately — *"the refit changes when Rule Instances are armed"* | **hash-neutral by construction**: a refusal that does not fire and an invariant that does not report change no state |

**Recommendation: land tasks 2–4 in front of sub-task C if the sessions can be sequenced, and behind it
otherwise.** In front is strictly better — the refusals are cheap, hash-neutral, and sub-task C is the
first caller that can trip them — and *behind* is not harmful, only later. What must not happen is the
two sessions editing `World.Unlink` and `EventWheel.Arm` in the same window, which is exactly the
re-run-not-merge conflict [`0015`](0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md)
documented against slice 10. **Task 1 is safe in any order** — it touches only comments and a message.

---

## Tasks

Four, matching [`0003`](0003-build-plan.md)'s estimate, and the match is a coincidence worth naming:
the four are not the four anybody expected.

### 1. The refusal, re-pointed — and the design it names was refuted — **done**

`Arm`'s message reads *"A longer sleep needs slice 9's overflow list, not a wrap."* **`adr/0056`
refuted the overflow list**: at 1M Households the flat list holds ~1,000,000 entries permanently, and a
staggered one *"is a coarse wheel with its buckets hidden."* So the message currently offers the
rejected design as the remedy. Three sites say it — `EventWheel.cs:131` (the message), `EventWheel.cs:65`
and `World.cs:852` (the remarks) — and all three point at the coarse wheel now, with the reason the ADR
gives for refusing rather than wrapping: **a wrap puts a Household's next event in the past and nothing
says so.**

`BinTests.cs:382` already watches the refusal fire at `Size` and `Size + 1`. Check it asserts the
*claim* rather than the message text; if it asserts text, that is the one thing in this task that can
break, and it should assert the claim.

### 2. The write-site refusal against a double-arm — **done**

`Arm` requires `Blocked == Blocking.Nothing && NextTick > now` to be **false** before it appends —
i.e. it refuses arming a row that is already armed for a future Tick, reporting
`Invariant.RuleInstanceIsArmedOrWaiting` exactly as `Subscribe` does at its own write site. Finding 3
is the derivation; the predicate needs no column and no hash change.

Two things to get right. The refusal must **not** fire for the ordinary re-arm after a success, where
the row was popped this Tick and `NextTick == now` — that is the in-flight row of finding 3's table and
it is the common path, so the comparison is strict. And it must be an `Invariants.Require` on the
`O(1)`-at-the-write-site tier rather than a throw, because `02 §10` sorts invariants by frequency and
this one is on the phase that runs every Tick.

A test that writes the violation and watches it fire, per `CLAUDE.md` — arm, then arm again without
popping.

### 3. The whole-world check, strengthened to the period rather than the bucket — **done**

`RuleInstancesAreQueuedExactlyOnce` gains `report.Tick < NextTick && NextTick < report.Tick + Size` for
every armed row it walks. Finding 4 is the derivation: the existing predicate is invariant under a whole
period and therefore cannot see the only error a modulus makes.

`InvariantRegistry.Tick` is already set, so nothing is threaded. The tier stays `EndOfRun` — the walk is
`O(Size + live rows)` and 8,192 buckets per check is not a staggered cost — and **that decision should be
recorded rather than defaulted**, because `02 §10` asks for the frequency argument and finding 4's two
reachable paths are both once-per-event rather than per-Tick.

Then the audit slice 10 task 8 ran, applied here: **is every claim the Wheel makes reported by
something?** That audit found `HouseholdHomeExists` orphaned among 26 invariants. The Wheel's claims are
the partition, the freed-row clause, the two refusals and now the period bound. Report what is orphaned;
do not renumber anything, because a crash artifact carries the id.

### 4. `Unlink`'s discarded return, and the phase-order claim underneath it — **done**

Stop discarding `IndexList.Remove`'s `bool` at `World.cs:1160`. A removal that removed nothing means the
row was not where `Blocked` said it was, which is either a bug or the in-flight window — and the caller
is the only thing that knows which. Then state the ordering that currently makes it unreachable
(demolition is Tick phase 6, the drain and apply are phases 1–3) where a reader of `Unlink` will see it,
because right now the safety is real and undocumented.

This task carries the **amendment owed to `adr/0056`** (finding 2): the partition holds at a phase
boundary, and the in-flight window is what the domain excludes. Write it as an amendment to the
consequence bullet, not a new ADR — the decision is unchanged and only its scope was missing.

---

## What building it found

Five things, and the first two are worth more than the slice.

### 1. The end-of-run tier stamped every violation `Tick 0`, in both long runs

`Simulation.CheckEndOfRun()` reports through `InvariantRegistry`, which stamps each `Violation` with the
Tick it was given. Both 100,000-Tick acceptance runs called it on a **fresh `Simulation` built over the
already-run world** — `new Simulation(world, key).CheckEndOfRun()` — purely because the helper that ran
the world did not hand its own `Simulation` back. A new `Simulation`'s `_tick` is 0, so **every
violation the whole-world tier has ever reported in the runs `CLAUDE.md` names as the ones that surface
these bugs carried a Tick of 0 on a world 100,000 Ticks old.**

**Nothing noticed because nothing read it.** No end-of-run invariant had ever been relative to *now*;
the stamp was decorative, and a decorative field agreeing with nothing is invisible. Task 3's check is
the first consumer, and it failed instantly — reading every armed row in a settled city as due a whole
run in the future. Fixed by threading the `Simulation` that ran through both helpers, which also deletes
the throwaway. **The tier where the Tick is the *only* temporal context a crash artifact carries is the
tier that had it wrong.**

### 2. `Simulation._tick` is the *next* Tick to run, and the window is half-open because of it

The first spelling of task 3 required `NextTick > Tick` and fired on a row armed for **exactly** Tick
100,000 after a 100,000-Tick run. `Simulation.Step` reads `tick = _tick` and increments **after**, so
`_tick` is the Tick about to run rather than the one just run, and a row armed for it is due next rather
than overdue. The bound is `Tick <= NextTick < Tick + WHEEL_SIZE`, and the boundary is derived rather
than nudged: mid-Tick the case cannot arise, because Phase 1 has already popped that bucket.

### 3. Three fixtures ran time backwards, and the refusal rests on time not doing that

`BinTests.Sleeper` arms at Tick 0 with a delay of 1 and pops the row for **Tick 1** — leaving
`NextTick` at 1 — and three tests then deposited at **Tick 0**. Under `IsArmed`'s `NextTick > now` a
drained row therefore read as still armed, and the new refusal reported it. **The fixtures were wrong
and the predicate was not**: a row popped for Tick 1 cannot be woken on Tick 0 in any run, because the
Tick loop only moves forward. They now act on a named `Woken` constant, and the assumption is written
where it is relied on. It is the fourth instance of this project's recurring shape — a green suite
agreeing with the code rather than with the claim — and the first found in a fixture's *clock* rather
than its data.

### 4. A state refusal wanted the registry inside `EventWheel`, which reordered `World`'s constructor

`Arm`'s refusal is a claim about the **world** and not about an argument — the delay refusal above it is
the other way round — so it reports through `InvariantRegistry` with an id a crash artifact can carry,
exactly as `World.Subscribe` does at the other half of the partition. That meant injecting the registry,
and `Invariants` was constructed *after* `Wheel`. It is built before it now, with the ordering stated;
it folds nothing, so the hash is untouched.

### 5. The slice's real shape: every check the wheel wants is relative to a *now* it does not have

Each of the three checks rests on a property nobody had written down — the double-arm refusal on **time
being monotone**, the period bound on the **caller passing a truthful Tick**, and `Unlink` on the
**phase order**. All three are properties of the World or of the Tick loop rather than of the wheel, and
the wheel reaches a `now` only through whoever calls it. **This is the same wall slice 8's `World.Adopt`
hit** when it had to take the Tick as a parameter to compute a stagger. Whether the World should hold
the Tick is not slice 9's to settle — it is `05 §7`'s save format question wearing a different hat — but
it is now the third mechanism to have paid for the answer being *no*. **Filed** to
[`0002`](0002-open-questions.md) §C under `05-technical-architecture.md` — and **closed the same day**,
into [`adr/0058`](../docs/adr/0058-the-tick-is-state-so-the-world-holds-it-and-the-hash-folds-it.md).
The World holds the Tick as a saved and hashed column, and the invariant tiers stopped taking one at
all, so the `Tick 0` bug class is **unrepresentable** rather than fixed. Two things the move found that
neither this plan nor the filed entry had: adding a table changes the hash **composition**, so the
golden baseline `README`'s own step 3 applied and the seed's version byte is bumped to `02`; and folding
the clock **kills within-run flatness**, so *"an idle Tick changes nothing"* moved to a clock-excluded
fold where it states what it always meant. `_phase` and `_inForce` stay where they were, deliberately.

Two things the filing added that this slice did not have. The Tick is in **no field declaration**, so it
is neither hashed nor saveable — and `_phase` and `_inForce`, the Ruleset in force, are in the same
position beside it, which is what makes it one question rather than three. And moving it would be
**hash-bearing by relocation**, carrying `_tick`'s *next-Tick-to-run* convention into the hash's blast
radius, which is a shape `adr/0052` has no row for.

## Acceptance

**Met.** `dotnet build` clean with 0 warnings, `dotnet test` green at **776** — four new tests, and the
three refusals each have one that writes the violation and watches it fire, plus a control for the
ordinary re-arm, which is the case a refusal written as *armed or in flight* would have broken.
**No baseline moved**, so the slice is hash-neutral as it claimed it would be.

- `dotnet build` clean, `dotnet test` green, no GPU and no Godot.
- **The three golden baselines are unchanged.** This is the slice's sharpest acceptance test and it is
  available because of what the slice is: refusals that do not fire and invariants that do not report
  change no state, so under `CLAUDE.md`'s rule — *a change is an optimisation if the State Hash is
  unchanged* — **slice 9 is an optimisation in the hash's sense while being a correctness slice in
  every other**. A moved baseline means something in tasks 2–4 changed behaviour instead of observing
  it, and that is a defect rather than a re-record.
- Each of the two refusals and each invariant has a test that writes the violation and watches it fire.
- The 100,000-Tick run is unchanged in every column, including the `--census` `Due` sum and peak.
- **Something to look at:** `--census`'s `Due` peak, named as what `02 §7`'s claim will be settled
  against. There is no new view, and there should not be: this slice makes an existing mechanism
  checkable and inventing a display for it would be the padding finding 1 warns about.

## Decisions owed, found while planning

**1. Is slice 9 a slice, or is it slice 8's tail?** Its content is four corrections at a seam another
session is writing across, and three of the four exist *because* of what slice 8 is doing this week.
**Recommendation: keep it as a slice and record that its content shrank**, because the alternative is
either padding it or folding correctness work into a slice that is already re-recording baselines for
an unrelated reason. What must not happen is the row surviving at four tasks with four *different*
tasks invented to fill it.

**2. Does `EventWheel` get generalised off `RuleInstanceTable`?** **Recommendation: no**, on
`adr/0056`'s own rule — a wheel is added when its consumer exists, and a generalisation shaped by one
consumer is that consumer's shape with a type parameter. Written down here so the per-table clause does
not read as skipped. Reopens the day a second scheduled table exists, which is Phase 2 Life Stages.

**3. The domain of the partition** (finding 2). *Arguable* under `adr/0043` — there is no number, the
code already states it correctly, and what is owed is one sentence in `adr/0056`. Settled by task 4.

**4. Does the period bound wait for invariant 6's machinery?** It is the check invariant 6 would need,
and invariant 6 does not exist. **Recommendation: slice 9 takes it**, because it is one comparison, it
needs no new plumbing, and *the other* path that reaches it — a Ruleset reload — is being written now
rather than eventually.

**No new numbers.** Nothing in this slice is hash-bearing, so `adr/0052` has nothing to ratify — which
is worth stating rather than leaving to inference, since the two numbers the Wheel does own (`WHEEL_SIZE`
and `W`) are respectively a world-creation constant and Phase 2's.

## Owed to other documents, not questions

For [`0012`](0012-corpus-audit.md), except where a task above discharges it.

- **`adr/0056`** — the partition's domain (finding 2, discharged by task 4); and the *Slice 9 builds the
  fine wheel only* bullet, whose Zone Rule justification names a mechanism that is not on the wheel
  (finding 6). The scope conclusion stands and the reason should be replaced with the stronger one.
- **[`0000`](0000-board.md) and [`0003`](0003-build-plan.md)** — both carry the same Zone Rule sentence,
  and both describe slice 9 as *finishing what slice 7 half-built*, which finding 1 contradicts.
- **`02 §7`** — session C's banner is applied and correct. What it should add is that the number it types
  *measurable* is already instrumented (`RuleCounter.Due`, peak), so the claim is waiting on a **run**
  and not on machinery. A reader today would reasonably conclude the opposite.
- **`WHEEL_SIZE` as a `const`** — slice 8 task 3 found `adr/0015`'s world-creation enumeration is
  one-quarter implemented and filed it. **Not re-filed here**; noted so the next reader of `EventWheel.cs:86`
  does not file it a second time.

## What this slice deliberately does not do

- **The coarse wheel, and the cascade.** `adr/0056` scopes them out and finding 6 strengthens the
  argument: there is one consumer, and the second Rule family is barred from being another.
- **`W`, and Life Stages.** Phase 2. `W` has a named ratifier and its refuting number reads an
  instrument that already exists.
- **A generic wheel.** Decision owed 2.
- **Invariant 6's machinery.** The save/reload equivalence test is not this slice's, even though
  finding 4 is one of the holes it would close.
- **The S0b measurement.** `02 §7`'s number is a run on a machine, not a task here.
- **Anything that moves a baseline.** If a task needs to, it has stopped being this slice.
