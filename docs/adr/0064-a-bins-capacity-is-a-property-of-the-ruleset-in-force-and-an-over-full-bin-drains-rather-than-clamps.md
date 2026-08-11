# A Bin's capacity is a property of the Ruleset in force, and an over-full Bin drains rather than clamps

**A Bin's capacity is *derived and rebuilt* from the Ruleset in force, never *saved and hashed*. It is
declared by `(Building kind, Resource)` and rebuilt at world load and at every Ruleset swap, so a
retuned ceiling reaches every Building standing rather than only the next one raised. A Bin found
holding more than its new ceiling is **left alone**: it is not clamped, the reload is not refused, and
nothing is destroyed. Negative headroom stops every producer on headroom and leaves every consumer
untouched, so the Bin bleeds down and heals itself. `(kind, Resource)` becomes a **key**, refused at
load, because the derivation needs it to be a function.**

Settled by session **N**, [`plans/0018`](../../plans/0018-session-n-the-bin-the-pool-and-the-economy.md)
task 3, on [`plans/0015`](../../plans/0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md)
finding 4. Typed **arguable** under [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md):
no measurement decides whether a ceiling is state or a function of the Ruleset. **It sets no number and
retires one column**, so it opens no `adr/0052` row — the second time a decision here has *removed*
state rather than chosen a value, after [`adr/0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md).

`FAST ITERATION` `LEGIBLE CAUSE` `HONEST DEGRADATION`

---

## Why

### The field was typed as tuning and implemented as world-creation state

`BinTable.Create` copies `capacity.Units` onto the row, and `World.Fit` creates only the Bins it does
not already find — so a Bin carries the ceiling in force **at the moment it was built**, for ever.
That is a coherent position, and it is not the position the corpus records. `RulesetShape.Compare`
files capacity as a **number** — tunable, accepted on a reload with no migration — on the test *what
live state points at it*: a Bin row holds a Resource id, so the Resource is structure; a capacity is
only ever read *through* a row, so it is a number.

**That test asks whether live rows stay interpretable, not whether the edit reaches anything.** It is
the right test for the question it was built for, and it was silently reused for a second question it
does not answer. The result is a field the Ruleset accepts an edit to, the reload reports as applied,
and no standing Building observes — which is `adr/0015`'s own polarity failing, since that ADR names
**silently ignoring** as its failure mode.

`plans/0015` finding 4 called this a gap left by a merged task. It is not a gap. It is a **disagreement
between two documents about what kind of thing a capacity is**, and the fix has to settle that rather
than write the missing assignment.

### It is the same defect [`adr/0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md) removed, in a different column

`RuleInstance.shortfall` was a Ruleset-derived quantity computed once, written onto a row, and never
re-derived — so halving a Rule's input on a reload left the starving Building as the one Building the
edit never reached. `Bin.capacity` is that shape exactly: authored in the Ruleset, copied to a row at
creation, read through the row for ever after.

**The session looked for a property distinguishing the two and did not find one.** Neither is pointed
at by live state; both are authored numbers; both are read on the hot path. Absent such a property, two
columns of one kind settled two different ways is the corpus disagreeing with itself, and the
disagreement would sit in the one subsystem whose acceptance test is *change a number and see it*.

### The sweep is the incoherent middle, not the cheap compromise

Writing capacity onto every Bin during the reload — call it the sweep — looks like the frugal answer:
it pays only when the rare event happens, where a derivation pays continuously.

**That reading does not survive contact with what the two actually do.** A rebuilt column *is* the
sweep: both walk every Bin, both write the Ruleset's number, both run at the same moments, and neither
touches the Tick. `RuleEngine.Check` still reads `Capacity[slot]` — the same array access it makes
today. The difference is not cost. It is that under the sweep, capacity remains **saved and hashed**
state that a reload periodically overwrites — *a cache of a derived fact* — so *the stored ceiling
disagrees with the Ruleset* is a representable state that merely happens to be false. Under a rebuilt
column it is not representable at all.

**That distinction is the whole purpose of the per-field declaration** (`adr/0003`), which `BOR0901`
enforces by making an undeclared field a build error. A saved cache of a derived value is precisely
what the declaration exists to stop somebody writing.

Two smaller consequences fall out and neither is the argument: the State Hash folds one fewer column,
which is a small win against a hash costing **32.47 ms**; and the save stops round-tripping a number it
can recompute. The save was already meaningless without its Ruleset — every Rule Instance holds a
`RuleId` — so nothing new is depended on.

### The patch, not the reload, is where *as built* bites

The strongest objection to deriving is that reload is a **design-time** event: rare, and performed by
the person holding the file. If the only beneficiary were the designer's loop, the case would be thin.

It is not the only beneficiary. **A shipped patch changes a Ruleset under a live city**, which is what
`05 §7`'s provenance trail exists for and why its retention cap is world-creation-fixed. Under *as
built*, a patch that retunes a capacity leaves the city permanently divided into Buildings raised
before and after it, identical in kind, identical in every declared respect, and different in their
ceilings — with **nothing in the world able to explain the difference**. A player clicking two bakeries
gets two numbers and no cause, and the cause is not recoverable from any state the world holds: it is
the order in which construction and a patch happened to interleave months ago. That fails
`LEGIBLE CAUSE` in the specific way the project is built to avoid, and no Evidence panel can rescue it
because the fact needed is not there to find.

**And the corpus already treats capacity as the canonical *which Ruleset is in force* observable.**
Slice 8 chose it, by name, to distinguish the two swap orderings —
`The_commands_in_the_reloading_tick_run_under_the_new_rules`, on the grounds that it is *"the one
Ruleset number a command's effect reads immediately"*. Under *as built* that is true only of Bins
created in that very Tick. Under this decision it is true of the whole world, which is what the test
was reaching for.

### Over-full is already reachable, already self-healing, and the assertion is already out of reach

The second-order question — *what happens to a Bin holding more than its new ceiling* — was briefed as
a choice between destroying Goods and violating an invariant. **Both horns are wrong**, and the second
is wrong as a matter of fact.

`Invariant.BinLevelIsWithinCapacity` (id 14) exists at exactly two sites, both **write-site guards** in
`World.Deposit` and `World.Withdraw`. There is no standing whole-world check that a level is under its
ceiling. A Bin with `level > capacity` therefore has negative headroom, which stops every deposit at
the engine's own affordability test, leaves every withdrawal untouched, and **drains back under its
ceiling on its own**. Nothing is destroyed, `04 §2`'s *a hundred units that entered must be accounted
for* holds, and the visible symptom is a producer asleep on headroom — a state the Evidence chain
already explains, and the regime the shipped Ruleset has run in since it was written.

The argument depends on that assertion being unreachable from the engine, which was verified rather
than assumed. **Four independent things close it**, and they are recorded because a future change need
only break one:

1. **`RuleEngine.Fire` is the only production caller** of `Deposit` and `Withdraw`. Every other call
   site in the repository is a test.
2. **`Fire` applies net deltas, never terms**, for a reason its own remark already states: a
   term-by-term application of a Rule holding one Bin on both sides could trip the invariant on a Rule
   the check had just passed.
3. **`Check` refuses on negative headroom because of the rounding convention.** It computes
   `affordable = IntegerMath.FloorDiv(headroom, delta)`, and `FloorDiv` rounds toward **negative
   infinity**, so any negative headroom yields `affordable ≤ −1 < floor` — including a **derived floor
   of 0**, which is the case that would escape under C#'s truncating `/`, where `FloorDiv(−1, 2)` would
   have been `0`, passed the guard and reached `Deposit`. **`BOR0203`, the lint banning raw division,
   is what makes this safe**, in a place nobody aimed it.
4. **A net-zero delta is skipped on both sides** — `continue` in `Check`, neither branch in `Fire` — so
   a Bin drawn from and returned to in equal measure is never written.

There is no mid-Tick window either: the swap runs in **Phase 0** and a Tick has exactly one Ruleset, so
the rebuild lands before any Rule evaluates and `Check` and `Fire` cannot disagree about a ceiling.

**Waking is not authorisation**, which is what makes the whole shape safe rather than delicately
balanced. A woken waiter re-arms and re-checks in Phase 2; it never writes on the strength of having
been woken. `adr/0063`'s drain additionally declines to wake anything while headroom is negative, since
its budget is the Bin's own headroom — so the over-full case needs no special handling in the wait list
at all, and the two decisions compose without either knowing about the other.

---

## Consequences

**`(kind, Resource)` becomes a key, and the refusal closes a defect that is already live.** The
derivation requires one declaration per pair. `RulesetLoader` refuses nothing of the sort today, and
`World.FindBin` returns the **first** Bin matching a Resource — so a kind declaring two Bins of one
Resource creates a second Bin that is unreachable, unwritable, dead storage. Worse and independent of
this decision: `Fit` creates a Bin only when `FindBin` returns `NoSlot`, so **a refit builds one Bin
where construction built two**, and the same kind ends up with different Bin counts depending on how
the Building came to exist. The loader must refuse the duplicate declaration by name.

**A derived column that nobody rebuilt is stale, and stale is silent.** This is the class of failure
`adr/0063` had to build an invariant to notice, and it is the honest price of the position rather than
an extra. An **end-of-run** check that every live Bin's capacity equals its declaration goes in beside
`NoWaiterSleepsOnANonBlockingBin` — whole-world, once per run, per `02 §10`'s frequency tiering.

**Nothing changes on the Tick path, so [`0013`](../../plans/0013-tick-budget.md) gains no row.** The
read is the same array access; the rebuild is `O(bins)` at load and at reload, both off the Tick. The
only measurable claim this decision makes is that a rebuild at 1M Bins is not a visible pause on a
keystroke, and it is not routed to a spike because the same walk was the alternative's cost too.

**It does not settle task 4, and it makes task 4 cheaper.** The row still holds an `int`, so *unbounded*
still arrives as `int.MaxValue` and is still indistinguishable from a ceiling authored at
`int.MaxValue`, which the loader permits. What changes is that the **declaration is consulted on every
rebuild**, so an explicit unbounded marker can be carried as a second *derived* column at no cost in
save size and none in the hash — where under *as built* it would have been another saved field. The
semantic question — whether *full* is a state an unbounded Bin can be in — is untouched and remains
task 4's.

**Structure is unaffected.** A Bin's Resource stays structure, a removed declaration still requires the
migration and its degradations, and a reload that changes only capacity still runs the number path with
no refit. This decision moves one field from one side of `RulesetShape`'s classification to the other
and changes the classification of nothing else.

### Rejected: capacity as a property of the Bin as built

Coherent, and it is what the code does today. It was rejected on the patch argument above — a permanent
split population of identical Buildings with an unexplainable difference — and on the absence of any
property distinguishing capacity from the shortfall deleted the same week. **Had it been chosen it would
have had to be written down**, in `RulesetShape` and in `0002`, as *a capacity edit is world-creation
scoped per Bin and a reload does not reach standing Buildings*. The unacceptable outcome was not either
position; it was the status quo, in which the code holds one and the documents record the other.

### Rejected: writing capacity in the refit alone

The narrow patch to finding 4, and it fails on consistency rather than on cost: the refit runs only when
**structure** moved, so one edit would have two behaviours depending on whether some *unrelated*
declaration also changed. `plans/0015` reached this conclusion while filing the finding — *the
consistent fixes are both or neither* — and it is restated here so nobody rediscovers it as an
optimisation.

### Rejected: clamping a Bin to its new ceiling

It destroys Goods to make a display number tidy, against `04 §2`'s conservation audit, and it has **no
defined meaning for a money Bin**, whose row cannot say whether `int.MaxValue` is a ceiling or the
absence of one. A clamp is also the least legible of the options: the Goods leave with no event, no
Evidence and no counterparty, which is the shape `00-vision` names as CS1's failure — *silent cap
exhaustion with no explanation*.

### Rejected: refusing a reload that would leave any Bin over its ceiling

Superficially the safest, and it makes the designer's loop hostage to the state of a running city: the
same edit succeeds or is refused depending on how full one warehouse happens to be at the instant of the
keystroke. That is `FAST ITERATION` traded for a condition that resolves itself within a few Ticks, and
it converts a self-healing transient into a hard stop.

---

## What would trigger revisiting

- **A capacity that depends on live state rather than on the declaration.** If a Building upgrade, a
  Readout-derived ceiling, or a per-Building modifier ever makes capacity a function of something other
  than `(kind, Resource)`, the derivation's key is wrong. Note the shape already exists —
  `ApplyCount.IsDerived` reads a Readout — so this is a plausible future rather than a hypothetical.
  It would **not** return capacity to saved state; it would widen what the rebuild reads.
- **A Ruleset that genuinely wants two Bins of one Resource on one kind** — separate silos, or a raw and
  a finished store of the same Good. Then the key is wrong, and a Bin needs an identity beyond its
  Resource, which is a larger change than this decision and touches `World.FindBin` and every `BinRef`.
- **A provenance requirement to record what a Bin's ceiling *was*.** If diagnosing a cross-patch defect
  ever needs the historical ceiling, history matters and the *as built* column returns — as history
  alongside the derivation, not instead of it.
- **The end-of-run derivation check firing.** It should be unfirable: a live Bin whose capacity differs
  from its declaration means a rebuild was not run somewhere. If it fires, the fault is the rebuild's
  **placement**, and the decision to look at is where a Ruleset can reach a `World` without passing
  through the swap.
- **A visible pause on reload at target scale.** The rebuild is `O(bins)` and 1M Citizens implies of the
  order of 1M Bins. If a designer's keystroke stops feeling immediate, the answer is an incremental
  rebuild keyed on which declarations moved — not a return to stored state.
