# The coarse Day wheel — `06` milestone 18's first half

**Milestone 18 is *Needs and the coarse Day wheel*. This document scopes the wheel half only**, which
`06`'s roots table lists as a **dependency root** — *"the fine wheel shipped in milestone 4"* — while the
Needs half sits downstream of the **Provider List at 14**. Scoping the two together would be scoping a
root and a leaf as one thing.

## Status

🟡 **SCOPED 2026-08-21.** ✅ **ASSESSED the same day: no document names a gate on milestone 18.**
`plans/0002` §A has held no open row since 2026-08-14, `plans/0000`'s per-milestone table has no row for
18 at all, and `plans/0003`'s gate board holds no Phase 2 row. What the corpus says instead is the
opposite of a gate: [`06`](../docs/06-roadmap.md):241 records milestone **20** as *"gated on `adr/0011`
and repaired by **18**"*. ⚠ **This milestone is the repair.**

⚠ **It is being taken out of `06`'s order, in parallel with milestone 11**, which is the whole reason
decision **1** below exists and must be settled before any task starts.

**Numbering**: this document is `0036`; ADRs from this track take **0140–0149**, leaving 0132–0139 to
milestone 11. ***A number claimed by reading what other branches have already written is claimed against
a set that is still growing*** — the `0112`/`0113` collision of 2026-08-19, which cost a day and is
recorded in `plans/0000`'s milestone 7 row.

## Why this milestone exists, in one paragraph

[`adr/0056`](../docs/adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md) says the Event Wheel is
**two levels** — a fine wheel of one bucket per Tick and a coarse wheel of one bucket per Day, the coarse
cascading into the fine at each Day boundary. **Only the fine wheel is built.** Slice 9 shipped it and
said so in writing, in the code: `EventWheel.cs:66-78` — *"This is still the fine wheel only, and slice 9
did not generalise it."* The consequence is that **`EventWheel.Arm` refuses any delay of a whole period
or more** (`EventWheel.cs:164`), so every Day countdown the design specifies — Life Stages, Need decay,
the housed re-evaluation — is **unrepresentable on the wheel it was specified to run on**. That is
`adr/0011`'s ⚫ in [`plans/0002`](0002-open-questions.md) §F2, and it is a live defect in the ADR
milestone 20 builds from.

## The named risk

**That every Day countdown in the design runs on a wheel whose period is exactly one Day** — `06`'s own
row 93. ⚠ **The risk is retired by the *build*, not by the amendment**, and decision 1 is about which of
those two this milestone is allowed to deliver.

## What the build already holds — surveyed 2026-08-21

**The fine wheel is complete and correct, and it is bound to one table by name.**

| Symbol | Where | What it is |
|---|---|---|
| `EventWheel.Size` | `EventWheel.cs:99` | **2048**, a world-creation `const`, folded into the save header (`SaveHeader.cs:179`) and checked on load (`:264`) |
| `Ticks.PerDay` | `Quantities/Ticks.cs:54` | **2048**. The fine wheel's period is therefore **exactly one Day**, which is the coincidence every comment leans on |
| `WheelBucketTable` | `EventWheel.cs:17` | `Head` and `Tail`, both **Saved**, `Touch.PerTick`. All `size` rows allocated up front, never freed |
| `EventWheel.BucketOf` | `EventWheel.cs:134` | `tick.Raw % Size` — **a bare modulus**, no epoch and no revolution counter |
| `EventWheel.Arm` | `EventWheel.cs:164` | Throws on `delay == 0 \|\| delay >= Size`; refuses a double arming; writes `NextTick` and appends to the bucket |
| `EventWheel.PopDue` | `EventWheel.cs:215` | One slot at a time — a returned collection would be a per-Tick allocation |
| `EventWheel.IsArmed` | `EventWheel.cs:202` | `Blocked == Nothing && NextTick > now`. **The strict `>` is only sound because `Arm` refuses `delay == 0`** |

**Who arms, and where in the Tick.** The phase order is `Simulation.cs:240-249` — Input(0), Wake(1),
Decide(2), Settle(3), Move(4), Layers(5), Growth(6), Commit(7), then `World.Advance()`.

- **Phase 1 `Wake` is the only phase that takes rows off the wheel** (`RuleEngine.CollectDue`,
  `RuleEngine.cs:232-243`), and its doc-comment says so.
- **Phase 3 `Settle` re-arms** (`RuleEngine.Fire`, `:575`) or subscribes (`Stop`, `:608`).
- **Phase 6 `Growth` arms new rows** via `World.CreateRuleInstance` (`World.cs:2957`), fed from
  `World.cs:1803` with `ArmingStagger` — uniform over `[1, rate]`, `PurposeTag.RuleArmingStagger`,
  defined at `World.cs:1825`.
- **Phase 0 `Input` arms on a Ruleset refit**, and `World.Unlink` (`World.cs:3344`) removes.

**There is exactly one scheduled table.** `RuleInstanceTable` (`RuleInstanceTable.cs:47-56`) carries
`next_tick` (**Saved**, absolute Ticks — *not* a bucket index), `blocked` as the queue discriminator, and
**one** intrusive link column, `queue_next` (`:54`), shared between a Bin wait list and a wheel bucket.
⚠ **One link column means *on two lists at once* is unrepresentable by construction** — which is what a
cascade has to work within, and is a help rather than an obstacle.

**Nothing else is on a wheel, and two things look like they are.**
`CitizenTable` carries `next_event_tick` (`CitizenTable.cs:98-112`) declared in slice 4 as a future
bucket key, and `:111` states it is *"**not** a Wheel link now"* — no Citizen is ever armed.
`CommuteRoster` (`CommuteRoster.cs:56-70`) holds four `int[Ticks.PerDay]` arrays and is the closest thing
in the build to a second ring, but it is **derived-and-rebuilt**, indexed by Tick-of-Day rather than by
absolute Tick, and nothing fires to move an entry. ⚠ **Its own header remark (`:17-33`) is worth reading
before this milestone starts**: it explains why the Wheel was not used for commutes and then says *"The
Wheel is still not wanted, and it is now **closer** to being wanted than it was."*

**The bound is enforced twice.** `EventWheel.Arm` throws, and `RulesetLoader.cs:514` refuses a
`rate < 1 || rate >= EventWheel.Size` at load. ⚠ **So a Ruleset cannot today author a Rule that fires
less often than once a Day**, and that is a consequence nobody chose — see decision **2**.

---

## Open decisions this milestone owes, before the task that needs them

### 1. Does the wheel get **built** here, or only **argued** here? Typed *arguable*

🔴 ⚠ **This is the decision the milestone turns on, and it exists only because 18 is being taken out of
order.** [`adr/0056`](../docs/adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md)'s own rule is
***a wheel is added when its consumer exists***, and it names the trigger: *"the coarse wheel has no
consumer until Life Stages arrive in Phase 2."*

**In `06`'s order the consumers are there when 18 runs.** Need decay and the housed re-evaluation are
milestone 18's own, and Life Stages are 20's. **Taken now, they are not**: the Needs half depends on the
**Provider List at 14**, which is unbuilt, and Life Stages are two milestones further still.

⚠ **Building it now with no consumer is a shape this project has already shipped twice and paid for
both times.** Milestone 9 shipped a land value producer that was *"correct and unobservable"* in every
world that existed (`plans/0034` F17), and milestone 11's own risk row records the same thing one
milestone later — *the anchor ships unread*. **A third instance would be a pattern rather than an
accident.**

**Three answers, and only the first two are defensible:**

- **(a) Argue only.** Settle decisions 3 and 4, amend `adr/0056` and `adr/0011`, correct the coverage
  map, and leave the build to 18 proper. Zero code, zero conflict with milestone 11.
  - 🔴 ⚠ **THIS BULLET CLAIMED IT DISCHARGES MILESTONE 20's GATE, AND THAT WAS WRONG — struck by task 2
    on the day it was written.** The claim rested on *a ⚫ is a defect in a document*, and
    [`0002`](0002-open-questions.md) §F2's legend says otherwise: ⚫ is **a supporting claim measured
    false**, and the row survives the correction. **Milestone 20 is blocked on the coarse wheel not
    being built**, which argument cannot reach. ***Option (a) makes 20's gate legible and does not
    discharge it***, and the two were conflated because the board's own milestone 20 row calls the gate
    *"a live defect in the ADR."*
- **(b) Argue and build, against a consumer that exists today** — decision **2**'s multi-Day Rule rate,
  which is authorable in TOML the moment the loader's bound is lifted, and which makes the tier
  **observable in a shipped Ruleset** rather than in a test fixture only.
- **(c) Argue and build with no consumer.** ⚠ **Refused by `adr/0056`'s own rule**, and recorded here so
  it does not get taken by default.

### 2. Is a **multi-Day Rule rate** the coarse wheel's first consumer? Typed *arguable*

`RulesetLoader.cs:514` refuses `rate >= EventWheel.Size` and `EventWheel.Arm` throws on the same bound.
**Together they make a Rule that fires less often than once a Day unauthorable.** Nothing decided that —
it falls out of the fine wheel being the only wheel.

⚠ **If this is a real capability a designer would want, it is a consumer that needs neither the Provider
List nor Life Stages**, and it turns decision 1 from *(a) or (c)* into *(a) or (b)*. **Type it first**
under [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md):
*is there a Rule in `04`'s chains, or in any shipped Ruleset, that wants a rate of Days?* If the honest
answer is that nobody has wanted one, this is **invented demand** and decision 1 collapses back to (a).

### 3. How many Day buckets? ✅ **SETTLED 2026-08-21 by task 1 — the count is WITHDRAWN, not rechosen.** Typed *arguable*

🔴 ⚠ **`adr/0056` was argued at `TICKS_PER_DAY = 8192` and the constant is now 2048**, in four places
including its **headline**. ⚠ **The headline describes shipped code wrongly**, not just the unbuilt half:
*"a fine wheel of 8192 buckets of one Tick"* — the fine wheel is built, at 2048.

⚠ **The sweep that should have caught this ran, and it covered the design documents and not the decision
records.** [`02 §7`](../docs/02-simulation-model.md):112 carries `~~8192~~ **2048**` with a note reading
**CORRECTED 2026-08-19**, and `05`:454 the same. `adr/0056` and `adr/0011` were not touched.
***A constant sweep that reaches the documents describing a mechanism and not the record deciding it
leaves the argument standing on the old number***, and the argument is the part somebody reasons from.

**The method is not a free choice, and `adr/0056` reached for the wrong one.**
[`adr/0019`](../docs/adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md)'s consequences
say it outright: ***"`WHEEL_SIZE` is set by the longest common event horizon, not by the Day. These are
independent questions that happen to share an answer... That equality is a coincidence worth stating."***
`02`:112 says the same in its correction note — *"it moved with the Day, for the reason stated —
**independently**, and to the same number."*

⚠ **`adr/0056` sized the coarse wheel *"symmetric with the fine wheel"* and defended it with a
**structural end-stop** — neither of which is `adr/0019`'s rule.** Applied properly, the coarse wheel is
sized by **the longest common *Day-denominated* event horizon**, which is the longest Life Stage arc.
That makes the number **derived rather than chosen** — the same standing as `lots_per_segment` and a
`[[kind]]`'s employment — and under
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
***a derived number owes a derivation and not a ratifier.***

**What that costs is a dependency this milestone may not be able to pay.** The arc lives in `adr/0011`'s
stage table, whose arithmetic is the very thing task 2 is repairing, and `plans/0002`:1140 files *"A
Household's life in Days"* as a §D2 gap with **playtest** as its only named ratifier. ⚠ **So the honest
outcome may be that the coarse wheel's size is derivable in principle and unfixable here**, and the
milestone writes the derivation down rather than the number.

**The two numbers `adr/0056`'s own reasoning reaches, recorded so the arithmetic is not redone:**

| Size | End-stop | Memory per consumer | Standing |
|---|---|---|---|
| **2048** | ~5.6 game-years, ≈ 2 Household lives | 16 KiB | Symmetry — ⚠ **not a rule this corpus has** |
| **8192** | ~22.4 game-years | 64 KiB | The end-stop `adr/0056` argued for, at the old Day length |

⚠ **Neither is ratifiable here.** A world in which either end-stop binds requires Life Stages, which is
milestone **20** — and `adr/0052` as amended requires the world be **checked for whether it can occur**.

### 4. Where does the cascade run, and what happens to `AnArmedRowIsDueWithinOnePeriod`? Typed *arguable*

`WorldInvariants.cs:1023` asserts `due >= tick && due < tick + EventWheel.Size` under
`Invariant.AnArmedRowIsDueWithinOnePeriod` (`Invariant.cs:315-332`), and its comment explains that a
bucket-agreement check *"cannot be caught by a check written modulo the same number"*.

⚠ **A row on the coarse wheel violates that invariant by construction.** So the invariant moves, exactly
as `OnlyAHousedHouseholdIsUnplaced` moved for milestone 11 task 4 — and ***a mechanism the design
describes and an invariant refuses is a disagreement, not a defect***. What replaces it has to say *due
within one **coarse** period, and within one fine period once cascaded*, which is two claims where there
was one.

**And the cascade needs a home in the phase order.** Phase 1 `Wake` is *"the only phase that takes rows
off the Wheel"*, so the Day-boundary step belongs there or immediately before it — at the cost of one
phase in every 2048 doing `O(entities waking that Day)` extra work. ⚠ **That is `adr/0056`'s claim
already** (*"a Day-boundary step costing `O(entities waking that Day)`, not `O(entities asleep)`"*), and
it needs a place in `plans/0013` rather than only in an ADR.

⚠ **The third state arrives one level up.** `adr/0056`'s amendment names the window in which a due row
is on **neither** queue — between Phase 1's `CollectDue` and the end of Phase 3. A cascade moving a row
from the coarse wheel to the fine one, through **one** `queue_next` column, opens a second such window.
***A partition with a named exception acquires a second exception the moment a second mover exists***,
and it should be named on the day rather than found by a consumer.

### 5. Does `EventWheel` get generalised off `RuleInstanceTable`? Typed *arguable* — **and this decision has already fired**

[`plans/0016`](0016-the-event-wheel.md) **decision owed 2** recommended **no**, on `adr/0056`'s rule, and
recorded the trigger in writing: ***"Reopens the day a second scheduled table exists, which is Phase 2
Life Stages."*** ⚠ **Whether that day is today is decision 1's answer** — under (a) it has not fired,
under (b) the second scheduled table is still `RuleInstanceTable` and it has *still* not fired, and it
fires properly only at 20.

⚠ **So the honest reading is that the reopen trigger names Life Stages and not this milestone**, and the
generalisation should be *declined again with the trigger restated* rather than taken here. Recorded so
the per-table clause does not read as skipped for a second time.

### 6. Does `W`, the Life Stage spread window, ship here? Typed *arguable* — **recommendation: no**

`adr/0056` couples `W` to the coarse wheel and [`plans/0002`](0002-open-questions.md):1155 files it in
**§D2** as a *gap* — one `W` per `[[life_stage]]`, hash-bearing, Phase 2. ⚠ **A `[[life_stage]]` table
does not exist**, so `W` has nothing to sit on: it is milestone **20**'s, and it stays a gap. Recorded
because `adr/0056` states the two together and a reader will expect them to travel together.

---

## Tasks — ⚠ **PROVISIONAL: the list below assumes decision 1 answers (b)**

Under **(a)** only tasks 1 to 3 survive, and they are the whole milestone.

1. **Re-derive `adr/0056` at 2048, and against `adr/0019`'s sizing rule** — ✅ **DONE 2026-08-21.**
   A correction banner on the exemplar's form ([`adr/0005`](../docs/adr/0005-two-fidelity-tiers.md), the
   one `PROCESS.md` names), plus strikethrough at all four sites. **The decision stands and its
   arithmetic and *method* do not**: the coarse wheel's bucket count is **withdrawn rather than
   restated**, the *"a third level can never be needed"* end-stop is **withdrawn with it** — it was
   arithmetic on 8192 Days, not an argument — and the 64 KiB figure goes too. ⚠ **The ratio claim is
   kept**: the fine wheel's period is still exactly one Day. **A fifth revisit trigger was added** —
   *`TICKS_PER_DAY` moving again* — because it has moved once and the sweep that followed did not reach
   this document. ⚠ **Two sites outside the task's scope were routed rather than left**
   ([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)),
   see **F9**.
2. **Amend `adr/0011`** — ✅ **DONE 2026-08-21.** Its session C amendment quotes `WHEEL_SIZE = 8192` and `TICKS_PER_DAY = 8192`.
   The defect it records is real and its arithmetic is stale, which is the worst combination to leave in
   a document a milestone builds from.
3. ~~**Strike the stale ⚫ on `adr/0056`**~~ **CORRECT THE COVERAGE MAP** in `plans/0002` §F2 — ✅ **DONE 2026-08-21, and the task was mis-scoped; see its record.** ⚠ **It is discharged and was never
   struck** — slice 9 task 4 wrote the third state into `adr/0056` in full, and the survey confirms the
   code said it first (`RuleEngine.cs:225-229`, `World.cs:3316-3327`). ***A gate is discharged by the
   work and struck by somebody, and only the first happens on its own.*** ⚠ **`adr/0011`'s ⚫ stays**
   until task 2 lands.
4. **Lift the two bounds against a real consumer** — `RulesetLoader.cs:514` and `EventWheel.Arm`'s
   refusal, decision **2**.
5. **The coarse wheel and the cascade** — a second `WheelBucketTable` at the decided size, the
   Day-boundary step, and the move through `queue_next`.
6. **The invariant** — `AnArmedRowIsDueWithinOnePeriod` rewritten as two claims, decision **4**, plus the
   second on-no-queue window named.
7. **A shipped Ruleset with a multi-Day rate in it**, so the tier is observable in a world rather than in
   a fixture. ⚠ **This is the task that stops it being milestone 9's shape a third time.**
8. **A `plans/0013` row** for the Day-boundary cascade, with its multiplicand marked **guessed** until
   something measures it.

### Task 1 — `adr/0056` re-derived — ✅ **DONE 2026-08-21**

**What ships**: a correction block at the head of
[`adr/0056`](../docs/adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md) on
[`PROCESS.md`](../PROCESS.md)'s exemplar form, plus four in-place corrections. **The decision is
untouched and the banner says so in a list** — two levels and no more, the coarse bucket being one Day,
the refusal of both the wrap and the flat overflow list, one wheel per scheduled table, and the
{armed, waiting} partition. ⚠ **What moved is the arithmetic and the *sizing method*, and the second is
the one that mattered.**

- **The headline no longer states a bucket count at all.** It read *"a fine wheel of 8192 buckets of one
  Tick"* — ⚠ **describing shipped code wrongly**, since the fine wheel is built and `EventWheel.Size` is
  2048. It now reads *one bucket per Tick, spanning exactly one Day*, which is the claim the ADR was
  always making. ***A structural decision that states a count in its headline invites the count to be
  the thing that is quoted.***
- **The coarse wheel's count is WITHDRAWN rather than rechosen** — decision **3**. `adr/0019` sizes a
  wheel by **the longest common event horizon**; `adr/0056` sized this one by **symmetry with the fine
  wheel**, which is not a rule this corpus has. The coarse horizon is the longest Life Stage arc, so the
  count is **derived rather than chosen**, and under `adr/0052` a derived number owes a **derivation**
  and not a ratifier. ⚠ **The derivation cannot be completed here** — the arc is a `plans/0002` §D2 gap
  ratified only by playtest and there is no `[[life_stage]]` table — so the coarse wheel now has a
  *shape* and no *size*, and the ADR says nothing may quote one.
- 🔴 **The end-stop went with it.** *"A third level can never be needed"* was **arithmetic on 8192
  Days**, not an argument. At the corrected constant, symmetry would give 2048 Days — ~5.6 game-years,
  about **two Household lives** against `adr/0011`'s *"on the order of a thousand Days"* — which is
  reachable, and therefore not an end-stop. ***Whether a third level can be ruled out is a property of
  the derived size and not a claim the ADR is entitled to make in advance.***
- **A fifth revisit trigger is added**, and it is the lesson rather than a mechanism: ***`TICKS_PER_DAY`
  moving again.*** Every number in the ADR is denominated in Ticks or Days, and the two kinds fail
  differently — a **ratio** claim survives such a move and a **derived** one does not. It is the derived
  ones that read as settled while stale, *because they carry no constant a search would find*: an
  end-stop in **years**, a memory figure in **KiB**.

⚠ **Two sites outside the task's scope were repaired rather than filed**, on
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md):

- 🔴 **[`adr/0019`](../docs/adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md) carried
  the same stale constant, and it is the ADR task 1 corrected `adr/0056` *against*.** It states
  `WHEEL_SIZE = 8192` and *"64 KB of bucket heads"* derived from it. ***The authority for a correction
  standing on the error it corrects is not a coincidence*** — it is the same sweep missing the same
  class of document — and leaving it would have pointed every future reader of the new banner at a stale
  source. Both figures struck, the rule in bold left untouched, and the bullet now says outright that it
  was the authority while carrying the defect.
- **[`0016`](0016-the-event-wheel.md):64** states the fine wheel's hashed bucket memory as **64 KiB**; at
  2048 buckets × a head and a tail `int` it is **16 KiB**. Corrected in place.

**The corpus checks are green — 24 of 24.** ⚠ **The assertion tier was NOT run and deliberately so**:
milestone 11's session has ~353 uncommitted lines across six code files in this same working tree, so a
suite run here would grade *their* work in progress and a red would say nothing about this task's
markdown. ***A gate run over somebody else's uncommitted work is not this change's gate.***

### Tasks 2 and 3 — `adr/0011` amended and the coverage map corrected — ✅ **DONE 2026-08-21**

**Task 2** struck the two 8192s in session C's amendment block and added a correction note. ⚠ **The
defect it records is untouched and so is every conclusion drawn from it** — this block is a **ratio**
claim (*the wheel's period is exactly one Day, so a two-Day countdown is unrepresentable*), and both
constants moved together, so it survives. That is the clean contrast with `adr/0056`, whose **derived**
claims did not.

🔴 **F9 — the character of `adr/0011`'s defect has changed, and no document had noticed.** Session C's
amendment **is** the *"arithmetic was never checked"* that `0002` §F2 marks it ⚫ for — written into the
ADR, with the repair named. ***So the document is no longer the thing that is wrong.*** What is still
wrong is that the **coarse wheel is not built**, which [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)
types ***unbuilt* rather than *refused***. ⚠ **The ADR is correct and unbuildable**, which is a
different obligation from a wrong one. **Owed to [`0000`](0000-board.md)**: its milestone 20 row calls
the gate *"a live defect in the ADR this milestone builds from"* and that now describes the wrong
obligation. ⚠ **Not edited from this branch** — the board is milestone 11's most likely merge conflict,
and *a view is corrected where it is read, not from a side branch.*

🔴 **F10 — task 3 was mis-scoped by this document, and the legend says why.** It was written as *strike
the stale ⚫*. **A ⚫ is never struck.** `0002` §F2's legend defines it as ***a supporting claim has been
measured false** (the decision may still stand)* — a permanent property of the ADR, not an open debt —
and `adr/0046`'s row is the exemplar: its two qualifications are struck and its ⚫ remains. What is
struck is the **annotation**, never the mark. ***A marker that records history reads like a to-do to
anybody who meets it as one***, and this document met it as one.

**What actually shipped**: three rows corrected rather than one struck.

- **`0056`** — the partition annotation struck as *open* (slice 9 named the third state; the build said
  it first in two doc-comments), and 🔴 **a SECOND ⚫ added** for the end-stop, which was arithmetic on
  8192 Days that `adr/0094` moved out from under. The row now carries **⚫ ⚫**.
- **`0011`** — retyped, per F9.
- **`0019`** — its own `WHEEL_SIZE = 8192` and *"64 KB of bucket heads"* struck, and its surviving rule
  marked **load-bearing**, since it is what `0056` was corrected against.

**Corpus checks green — 24 of 24. The assertion tier is green at 1,869**, run in this worktree rather
than in the shared tree.

## What this milestone must not do

- **Do not build Needs.** They are downstream of the **Provider List at 14**, which is unbuilt. Half of
  milestone 18 is not this document's.
- **Do not build Life Stages, or a `[[life_stage]]` table, or `W`.** Milestone 20.
- **Do not generalise `EventWheel` off `RuleInstanceTable`** — decision **5**; the reopen trigger names
  Life Stages.
- **Do not wrap.** `adr/0056`: a wrap puts a Household's next event in the past and nothing says so.
- **Do not add a third level.** `adr/0056`'s revisit trigger is a *consumer whose sleep exceeds the
  end-stop*, and the remedy it names is a wider `W` or a finer coarse bucket, never rescanning a list.
- **Do not touch milestone 11's surfaces** — `World.Arrive`, `MoneySupplyTable`, `TripEngine`,
  `UnplacedTable`, `rulesets/bordered.toml`. The two tracks are file-disjoint and should stay so.

## Definition of done

[`CLAUDE.md`](../CLAUDE.md)'s list, refined per [`0003`](0003-build-plan.md) → *Definition of done*.
⚠ **Under decision 1(a) only the first two rows apply.**

- `dotnet build` green with no GPU and no Godot; **the whole unfiltered `dotnet test` green**.
- **No document states a wheel size that the build does not hold**, and `adr/0011`'s ⚫ is struck with
  the amendment that discharges it.
- **A shipped Ruleset authors a rate longer than one Day**, and a run fires it — the tier is observable
  in a world.
- **`Invariant.RuleInstancesAreQueuedExactlyOnce` still holds** across a cascade, and the second
  on-no-queue window is named where `adr/0056` names the first.
- **The wheel still cannot grow with elapsed time** — `adr/0006`. Membership stays a *partition* of the
  live rows across both levels, not an accumulation.
- Every number that reached a Ruleset or a `const` has a row in `plans/0002` §D with a machine, **a
  world** and **a quantity**, and ⚠ **the world is checked for whether it can occur** — decision 3 says
  it may not be, here.

---

## What scoping found

### F1 — the assessment: no document names a gate on milestone 18, and one names it as the repair

`plans/0002` §A empty since 2026-08-14; no row for 18 in `plans/0000`'s per-milestone table; no Phase 2
row in `plans/0003`'s gate board. [`06`](../docs/06-roadmap.md):241 records 20 as *"gated on `adr/0011`
and repaired by **18**"*. ⚠ **`06`'s roots table (`:147`) makes the wheel half a root** — *"the fine
wheel shipped in milestone 4"* — while `:343`'s inventory row for the same mechanism carries **no ✅
Placed marker** at all. *One document, two rows, one placed and one not.* Filed to
[`0012`](0012-corpus-audit.md) as **Cause 1**, minor.

### F2 — the constant sweep reached the design documents and not the decision records

🔴 **`adr/0094` moved `TICKS_PER_DAY` and `WHEEL_SIZE` from 8192 to 2048, and a sweep on 2026-08-19
corrected [`02`](../docs/02-simulation-model.md):112 and [`05`](../docs/05-technical-architecture.md):454
in place, with strikethrough and a dated note.** It did not reach `adr/0056` (**four** sites, including
the headline), `adr/0011` (one, inside session C's amendment),
[`adr/0019`](../docs/adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md) (two — the
constant **and** the bucket-head memory derived from it) or [`0016`](0016-the-event-wheel.md):64 (the
fine wheel's hashed bucket memory, stated at the old count). ⚠ **Four documents, and the sweep found
none of them**, while correcting the two that state the constant outright.

⚠ **`adr/0094` is not at fault and that is what makes this interesting.** It *did* revisit the wheel: its
consequence at `:202-205` checks the claim that breaks loudly — the fine period — confirms it, and writes
it down. What no sweep caught is a claim in **another ADR** that was **derived** from the constant rather
than restating it: `adr/0056`'s *"~22 game-years… which means a third level can never be needed."*
***A restatement is findable by search and a conclusion is not***, so a constant that moves takes its
restatements with it and leaves its conclusions behind. That is [`0012`](0012-corpus-audit.md) **Cause 5**
one level up — not a digit copied without its clause, but a **conclusion computed from a digit** that
nobody recomputed.

### F3 — one of the two ⚫ defects is discharged and was never struck

`plans/0002` §F2:1481 marks `adr/0056` ⚫ for *"a third state it does not name"*. **It names it**, in the
slice 9 amendment, in full and with the reasoning — and the code named it first, in two doc-comments.
`plans/0016` decision 3 says outright that it was *"Settled by task 4."* ⚠ **So this milestone inherits
one live ADR defect, not two**, and the coverage map overstates the debt.

### F4 — the loader forbids a Rule slower than a Day, and nobody decided that

`RulesetLoader.cs:514` and `EventWheel.Arm` both refuse `rate >= EventWheel.Size`. **It is a consequence
of the fine wheel being the only wheel**, not a design choice — no ADR argues for it and no document
records it as a limitation. Decision **2** is whether lifting it is a capability or invented demand.
⚠ **Under `adr/0070` this is an *unbuilt* mechanism and not a *refused* one**, so it is not evidence of
anything, and *the simulation does not do X* means nobody built X.

### F5 — the cascade opens a second on-no-queue window, one level up

`RuleInstanceTable` has **one** link column, so a cascade is a remove-then-append and the row is briefly
on neither list — the same third state `adr/0056`'s amendment already names between Phase 1 and Phase 3,
arriving again for a different mover. ⚠ **Name it on the day.** The first one cost a Phase 2 author *"a
claim they will find false the first time they check it mid-Tick, with nothing to tell them whether they
have a bug or a bad invariant."*

### F6 — `CommuteRoster` says in its own header that it is getting closer to wanting the Wheel

`CommuteRoster.cs:17-33` explains why commutes are a Day-long ring of per-Tick buckets rather than wheel
armings, and ends *"The Wheel is still not wanted, and it is now **closer** to being wanted than it was."*
⚠ **Not this milestone's**, and recorded because a second Day-periodic structure existing while a Day
wheel is being built is exactly the adjacency somebody will want to collapse. **The reasons it is not a
wheel tier are in that header and they are good ones** — it is derived-and-rebuilt, keyed on Tick-of-Day,
and nothing fires to move an entry.

### F7 — `CitizenTable.next_event_tick` is a declared column with no wheel behind it

`CitizenTable.cs:98-112`, declared in slice 4 as the future bucket key; `:111` says it is *"**not** a
Wheel link now"*. ⚠ **This is the shape `CLAUDE.md` warns about** — *declaring a column `Derived`
allocates it; it does not make anything rebuild it* — arriving for a **Saved** column instead: the column
exists, is hashed, and no wheel reads it. Check what `DerivedRebuildAuditTests` and the save round-trip
actually do with it before task 5 adds a second wheel that might look like its owner.

### F9 — the ADR that supplies the sizing rule carries the stale constant it is cited to correct

🔴 **Task 1 corrected `adr/0056` *against* [`adr/0019`](../docs/adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md), and `adr/0019`'s own consequence bullet still read `WHEEL_SIZE = 8192` and *"64 KB of bucket heads"*.** The rule in its bold lead — *a wheel is sized by the longest common event horizon, not by the Day* — is untouched and is the load-bearing half; what was stale is the worked number beneath it. ⚠ **Leaving it would have pointed a fresh correction at a stale source**, so it was amended on the day rather than filed.

**And a third site**: [`plans/0016`](0016-the-event-wheel.md):64's survey table gave the fine wheel's hashed bucket memory as **64 KiB**, which is 8192 buckets × a head and a tail `int`; at 2048 it is **16 KiB**. Corrected in place.

⚠ **Three of the four sites were found only by grepping for the *derived* figures** — `22 game-years`, `64 KiB`, `64 KB` — and not for the constant, which is F2's point arriving as a method: ***to find what a moved constant broke, search for what was computed from it, not for it.*** ⚠ **And that search is noisy in the way Cause 5 predicts**: `64 KB` also names Chunk save-record overhead in `adr/0034`, `05`:259 and `plans/0002`:2025, which have nothing to do with a wheel. **A number of the right shape is not the number.**

### F8 — a recorded spike figure states a control the build no longer has, twice over

[`spike-results`](../docs/spike-results.md):1179, **K5 — wheel bucket drain and reschedule**: *"Across
**8,192 buckets**, matching `WHEEL_SIZE`, with **1,560,000 scheduled entities** — every table in the
schema carrying a `wheel_next` column, which is **Citizens, Households, Buildings and Businesses**."*
It concludes **~18.7 MB against a 12 MB L3** — *"It does not fit."*

⚠ **Both halves of its stated basis are now false.** `WHEEL_SIZE` is **2048**, and `adr/0056` decided
**one wheel per scheduled table** with the Rule Instance as the only consumer — `02 §7`'s own correction
says *"A Building has no event of its own at all"*, and `CitizenTable.next_event_tick` is documented as
**not** a Wheel link (`CitizenTable.cs:111`). ***K5 priced an architecture the corpus went on to
refuse.*** ⚠ **Routed to [`0012`](0012-corpus-audit.md) rather than repaired here**
([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)):
the figure is not this milestone's to re-measure, and the candidate action is the **disqualifier
registry** — a figure a test refuses to let any document quote bare — rather than a deletion, because
the measurement was honest about a world that existed when it was taken.

## Where this sits

**Milestone 11 is in flight in parallel and the two are file-disjoint.** 11 touches `World.Arrive`,
`MoneySupplyTable`, `WorldInvariants.MoneyIsConserved`, `TripEngine`, `UnplacedTable` and
`rulesets/bordered.toml`; this touches `EventWheel.cs`, `WheelBucketTable`, `RuleInstanceTable` and
`RulesetLoader.cs:514`. ⚠ **The shared surfaces are prose, not code** — `plans/0000`, `plans/0002` and
`WorldInvariants.cs` — and the ADR block reservation above is what keeps the numbering apart.
