# A wait list wakes on the Bin's state, and a shortfall is derived rather than stored

**A Bin's wait list wakes a waiter when the Bin's *current* state can complete it, against a requirement
*derived at the moment of the drain*. The budget is the Bin's `level` — or its `headroom` on the other
list — and never the arriving delta. `RuleInstance.shortfall` stops being a stored column. Servings stay
**atomic**: a waiter is woken only when it can be completed, and is then served completely, in queue
order. Partial acquisition — taking what is available and accumulating toward a threshold — is a
**content pattern authored as two Rules over the consumer's own Bin**, never an engine behaviour.**

> 🔴 ⚠ **AMENDED 2026-08-26 by milestone 17 task 3 ([`plans/0045`](../../plans/0045-decline-demolish-and-cleared-land.md) **F10**), and the amendment is about WHAT ELSE CAN CHANGE A VERDICT rather than about what a waiter is promised.** Everything argued below survives untouched: the budget is the Bin's `level` and never the arriving delta, the requirement is derived at the moment of the drain, `shortfall` stays uncolumned, and servings stay atomic and in queue order. **What does not survive is the title read as an exhaustive list.** *A wait list wakes on the Bin's state* is true of the wait list and was taken to mean the Bin's state is the only thing that can change a Rule's verdict — and it is not. ***A Rule whose `apply` count is `{ derived = ... }` also depends on a Readout***, the count is recomputed only when the Rule is evaluated, and a starving Rule subscribes and sleeps rather than re-arming on its rate (`02 §4.1`'s *"subscribes to the specific Bin that was short"*, and [`0045`](0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md) for what a chain does) — so **nothing anywhere was watching the Readout**, and a Rule could sleep for ever waiting on supply it no longer needed.
>
> 🔴 **It was not a latent tidiness defect; it made a shipped mechanism silently inert.** Milestone 17's first decline threshold sheds an Occupant to lower a derived Rule's demand — to *nothing* at zero occupancy, which is the entire point of the rung — and the Building was condemned anyway, because `upkeep` never woke to notice. ***The mechanism was correct, complete and unobservable***, which is [`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s failure mode: the code said what it did and the city did not do it. **A test found it. No amount of reading this record would have.**
>
> ✅ **`World.WakeDerivedApply` is the second wake trigger**, and it is deliberately narrow. ⚠ **Only a SLEEPING instance needs it, and that is what makes the repair complete rather than partial**: a Rule that is not starving is armed on the Event Wheel and re-evaluates on its rate, picking up the new Readout by itself — so a **rise** in the Readout needs no wake at all, and only a **fall**, on a Rule already asleep, is unreachable by any other path. ⚠ **The guard is `Blocked != Nothing` and not `IsStarving`**: `StarvedSince` stays stamped until the Rule actually fires again, so an already-woken instance still reads as starving while being armed, and `EventWheel.Arm` refuses that by invariant. ***The two spellings of "asleep" are not the same set***, and the one that matters names the structure the instance is in.
>
> ⚠ **What is still owed is the general form.** Occupancy is the only Readout an `apply` derives from today and the only one anything writes a wake for; a second one would need the same treatment, and nothing enumerates them. **Read this record as deciding the Bin half in full and the Readout half not at all.**

Settled by session **N1**, [`plans/0018`](../../plans/0018-session-n-the-bin-the-pool-and-the-economy.md)
task 1. Typed **arguable** under [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md):
no measurement decides what a waiter is promised. **The cost half is measurable, is not settled here,
and is routed** — see *Consequences*.

`HONEST DEGRADATION` `SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE`

---

## Why

### The predicate was wrong on both sides, and the corpus filed it as one question

The drain compared `Shortfall[head] > remaining`, with `remaining` initialised to the arriving delta.
Each side carried an independent defect:

| | Side | What was wrong | Where it showed |
|---|---|---|---|
| **1** | the **budget** | `remaining = arriving` — *this write's delta*, not what the Bin holds. A requirement larger than the granularity of supply is never reached, however much accumulates | A consumer short of three, fed by arrivals of one, sleeps for ever while the Bin fills to its ceiling behind it |
| **2** | the **requirement** | the stored shortfall was computed under the Ruleset in force *when the waiter failed*, and nothing ever re-derived it | Halve a Rule's input on a hot reload and the starving Building is the one Building the edit never reaches |

[`0002`](../../plans/0002-open-questions.md) §C held these as **one** entry — *does a wait list wake on
the arrival or on the state* — with slice 8's reload face recorded as a *second face of the same
question*. They are two bugs on two axes, and the framing hid the consequence: **re-deriving the
requirement while still comparing it against a single arrival fixes nothing about the trickle**, because
`3 > 1` holds however the 3 was obtained. The candidate the ledger favoured was incomplete, and the
ledger could not see it.

### The state it produced was already illegal, and the instrument that would have caught it was never built

A consumer asleep beside a Bin that holds what it needs is exactly
[`adr/0033`](0033-two-rule-families-scheduled-and-swept.md)'s *no Rule is asleep with all inputs
satisfiable* — which that ADR names as one of **two mitigations, both required**, for the silent failure
mode subscription introduces. `02 §10` lists it in the end-of-run tier and
[`plans/0008`](../../plans/0008-tick-and-replay.md) repeats it. **Nothing implements it.** A sweep for
`satisfiab` over the tree returns those three statements of intent and one unrelated doc-comment.

So the defect and the missing check are one omission seen from two ends, which is how this survived two
100,000-Tick acceptance runs. **The decision is therefore not a choice between two behaviours; it is the
correction of a state the corpus had already declared inadmissible.**

### The stored shortfall never authorised anything, so it was never a claim

`World.Wake` zeroes `Shortfall`, clears `WaitingOn` and `Blocked`, and arms the row for `tick + 1` —
*the next* Tick, because a Bin is written in Phase 3 and Rules evaluate in Phase 2, which has already
run. The woken Rule then recomputes everything in `Check`: `Band`, the term walk, the `FloorDiv`. It may
fail again and resubscribe, which `02 §4.1` states in its own words.

**Nothing downstream ever reads the stored number as an entitlement.** It is a filter deciding whether
to re-arm. A saved, hash-bearing column existed to answer a question that is cheaper to ask of the Bin,
and deriving it **deletes the column** — which is the shape
[`adr/0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md) rewarded
and the second time in this corpus that satisfying
[`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) has meant
removing a number rather than ratifying one.

### Atomic servings are not a conservatism — they are what preserves throughput

The alternative considered was **partial acquisition**: let the head take what is available, accumulate,
and re-queue for the remainder. It is the intuitive answer, it is what happens in a real market, and
**with an indivisible threshold it is strictly worse than queueing.**

Three consumers each needing 6 to fire, a Pool supplying 12 in a period:

| Scheme | Each consumer holds | Firings | Goods immobilised |
|---|---|---|---|
| Partial acquisition — divide each arrival | 4, 4, 4 | **0** | **12** |
| Atomic + level budget — serve the head completely, rotate | 6 → fires, 6 → fires, 0 | **2** | **0** |

Twelve units of flour and no bread. This is arithmetic rather than a balance judgement, and it settles
the *fairness* question by argument rather than by appeal to the fact that nothing has exercised it:
**even division of an indivisible threshold yields zero output where rotation yields proportional
output.** Starvation at least leaves the early Buildings working.

**`02 §4.1`'s gradient claim survives, and this is why it survives.** *"Under half supply every bakery
bakes half as often, rather than half the bakeries running normally while the rest starve"* is true
**because servings are complete and the queue rotates** — each bakery's turn comes half as often. It is
a statement about evenness **over time**, not about dividing each arrival. The two readings of *degrades
evenly* are not equivalent and one of them is dominated; the section did not distinguish them and this
ADR now does.

### One of the two justifications for the old budget was a mis-citation

`02 §4.1` justified draining by shortfall on the grounds that waking every subscriber would *"let §1.1's
**sorted-key** settle order pick the winner — so the head of the queue would lose every time, forever."*

**`02 §1.1` says the opposite, four hundred lines earlier**: contention is resolved *"by a **counter-based
random shuffle**"*, and `§1.1` goes on to warn in its own voice that *"a sorted key applied to chronic
shortage produces permanent starvation rather than a gradient."* The code agrees with `§1.1`:

```csharp
_order[i] = Randomness.Draw(_key, IdAt(instance), tick, PurposeTag.RuleSettleOrder);
```

`tick` is in the draw, so the order is re-randomised every Tick and **a permanent winner is not
possible.** `CONTEXT.md` → Bin and `World.Drain`'s own remarks both inherited the mis-citation from
`§4.1`. **One error, three homes, and the original is a document mis-citing a section of itself that
says the reverse** — the same shape as the four *"quotation copied forward instead of checked"* findings
already on [`0000`](../../plans/0000-board.md)'s *Owed* list.

The worry underneath it is real and survives in a different form: waking a wide prefix lets **small
waiters beat large ones on quantity**, not on identity. That is size bias, and the level budget's
spend-down is what bounds it — see *Consequences*.

---

## Consequences

**The State Hash moves, so this is a design change and not an optimisation.** By the project's own test
a change that leaves the hash unchanged is an optimisation and anything else is a design change; deleting
a saved column is the latter twice over.

> **⚠ AMENDED BY DOING IT: one baseline re-recorded, not three.** This paragraph said *all three golden
> baselines re-record*. **Only `session-trace.txt` moved.** `session.borough` is an Input Log rather than
> a hash and was never going to; `world-hash.txt` is unchanged because `GoldenFixtures.Build()` raises
> Buildings through `Buildings.Create` rather than `World.CreateBuilding`, so it holds **no Rule Instance
> rows** and the deleted column was under no committed hash in it. **The coverage observation is the part
> worth keeping**: the `rule_instance` table's saved columns are covered by the session trace *alone*, and
> the artefact that exists precisely to cover what a session cannot reach does not reach them. Filed to
> [`0003`](../../plans/0003-build-plan.md) → *The hash-moving queue*, beside the note that lint 6 does not
> exist — the two together say the save format's only witness here is one file.

**`Shortfall` leaves `RuleInstanceTable`.** It was the one wait-list column carrying no `Touch.PerTick`,
and it was `Saved<int>`, so it folded into the hash. `WaitingOn`, `Blocked` and `QueueNext` all stay —
*which Bin* and *why* are still facts about the row, and only *how much* was ever recomputable. The
consequence for `adr/0003`'s per-field declaration is a column removed rather than retyped: nothing
becomes `Derived`, because nothing is stored.

**The drain's cost changes shape, and the number is owed.** Today it reads one field per waiter examined;
under this decision it derives a requirement, which is a partial `Check` — `Band` plus the term walk for
the one blocking Bin. The drain stops at the first uncovered waiter, so the common case is **one
derivation per Bin write**. Against a measured **82.84 ns** an evaluation synthetic and **552 ns** in a
real 1M world, that is a real cost on the hot path of every deposit and withdrawal, in the subsystem that
is already the largest priced row in [`0013`](../../plans/0013-tick-budget.md). **This ADR does not
declare it affordable.** The claim *the derived requirement costs less than a Bin write already costs* is
**measurable**, the machine exists (slice 7's counters plus an S0b-class capture), and under `adr/0043` no
document may cite it as settled until the number exists. Routed to `0002` §B.

**The wake set widens only where the Bin can genuinely serve more than one waiter.** The spend-down is
retained: `remaining` starts at `level` and each woken waiter's derived requirement is deducted, so a
budget of 6 against waiters needing 6, 4 and 2 wakes **only the first**. Every additional wake this
decision produces is a waiter that was asleep beside satisfiable stock, which is the illegal state being
corrected rather than a cost being incurred.

**An unbounded Bin never blocks on headroom, and now says so.** `HeadroomAt` is `Capacity − level` with
`int.MaxValue` underneath for a Money Bin (`adr/0031`), so a headroom budget of `int.MaxValue` wakes any
headroom waiter immediately. That is correct and was previously true by accident of the arriving delta
usually being small. It does **not** settle what *full* means for an unbounded Bin, which is `adr/0031`'s
unresolved comparison half and remains open.

**Hot reload's shortage path is fixed as a side effect.** [`plans/0015`](../../plans/0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md)
finding 3 — *a tuning reload never re-evaluates a sleeping Rule, so a shortfall recorded under the old
numbers outlives the change* — is discharged by derivation, without the whole-world wake pass on a
keystroke that the finding rejected. `adr/0015`'s acceptance test now holds on the shortage path.

**`adr/0033`'s satisfiability invariant must be built, and this decision is unguarded without it.**
Nothing in the suite would notice this regressing. The invariant is `O(n)` over waiters at the end of a
run, which `adr/0033` itself calls *"trivial at the end of a headless run"*. **It is owed slice work and
there is no open slice to owe it to** — see `plans/0018` decision owed 2, and
[`0003`](../../plans/0003-build-plan.md) → *The hash-moving queue* for where it lands.

> **⚠ AMENDED BY MEASUREMENT, TWICE, AND THE SECOND TIME REVERSED THE CONCLUSION.** This paragraph first
> claimed the invariant would demonstrate the fix. It was then narrowed to the opposite — a standing
> guard that *cannot* fire on today's content, because a violation needs a trickle and a trickle needs
> `pool`. **The invariant was then built, and it fired on the committed golden session immediately.**
>
> The reasoning was sound and the conclusion wrong. `rulesets/minimal.toml` is safe because `restock`'s
> headroom deficit is **1**, the smallest quantity the engine can express, so any withdrawal covers it —
> but **the golden session reloads into `rulesets/minimal-tuned.toml` at Tick 128, and the one number
> that file changes is `restock`'s output amount, 1 → 2.** A producer whose deficit is 2, drawn down one
> unit at a time by the occupancy-1 Buildings a Zone Rule creates, is never woken. At Tick 256 the
> committed trace holds a `restock` asleep on headroom **3** against a recorded shortfall of **2**.
>
> **So this ADR describes a live defect in shipped content rather than a hazard awaiting `pool`**, and
> the sequencing consequence is hard: the invariant **cannot be committed green without this fix**, so
> the two are one commit. See [`0003`](../../plans/0003-build-plan.md) → *The hash-moving queue*, which
> was written on the opposite assumption and is corrected.
>
> **The acceptance criterion is still the fixture rather than the invariant**: three `World.Deposit`
> calls of 1 against a waiter requiring 3, plus the `Withdraw` mirror, both in `BinWaitListTests` and
> both independent of `pool`. What changed is that the invariant is *also* demonstrative, on content that
> already exists, which is a stronger position than either reading predicted.

**Partial acquisition is demoted from mechanism to content, with a named case.** A designer who wants
stock to accumulate toward a threshold authors **two Rules**: an acquisition Rule at `min = 1` moving
`pool → local`, and a consumption Rule drawing from that `local` Bin. The accumulator is the consumer's
own Bin, which is `CONTEXT.md` → Building's own sentence — *a Building may hold Bins its Occupants draw
from* — so this needs no new state and no new concept. **`04 §8.5`, *how does construction consume
Materials, over Days or in a single transaction*, is exactly this question and is the pattern's first
real case.**

**Its failure mode is named rather than prevented.** Partial acquisition authored against a Pool that
cannot supply every consumer stalls with each holding part of a threshold and none firing. That failure
is **honest**: the Goods are in Bins, the levels are readable, the satisfiability invariant is *not*
violated because the inputs genuinely are unsatisfiable, and the diagnosis is a sentence — *three
bakeries hold four of the six flour they need*. The designed escape is `adr/0045`'s ladder to the import
rung at the Hinterland price, which a poor city cannot afford. **Legible is not the same as acceptable**,
and a Ruleset that authors partial acquisition without a chain behind it has authored a stall.

**Rejected: reservations.** Holding a committed quantity per waiter — whether inside the Bin or on the
Rule Instance — reproduces the ~200-Tick deadlock *through the fairness mechanism*, and worse than the
original: there, every Rule visibly failed on headroom, whereas reserved stock is neither available nor
spent. On the Rule Instance it is also outside every Bin, which breaks `02 §3.1`'s *Goods live on
Buildings, District Pools and Outside Connections* and `04 §2`'s conservation audit, and makes a Rule
persistently half-applied — the one thing atomicity exists to prevent.

**Unchanged: a greedy waiter can beat a woken atomic one.** Woken alongside each other, a greedy Rule
takes above its floor and the atomic one re-checks, fails and resubscribes. This is
[`adr/0037`](0037-the-world-is-single-buffered-and-hazards-are-per-table.md)'s stated design — Phase 2
decides how much it wants, Phase 3 may refuse it or serve it short — and the per-Tick shuffle makes it
fair in expectation. Not introduced here and not made worse here.

> ### Amended 2026-08-22: the invariant asks the head of the list, because the head is the whole of what the drain promises
>
> **The check this ADR owes was built stronger than the drain it checks, and the two contradicted each
> other in a committed test.** `WorldInvariants.CheckQueueStillBlocks` walked **every** waiter on a Bin's
> list and asked whether the Bin's level covered it. But the drain **stops rather than skips** — that is
> this ADR's *in queue order*, and the paragraph above defends it: skipping an uncovered waiter to reach a
> smaller one behind it starves every large waiter for the life of the city. **So a covered waiter queued
> behind an uncovered one is parked correctly**, and the walk called it a violation.
> [`plans/0003`](../../plans/0003-build-plan.md) hash-moving queue item 14.
>
> **It is settled by narrowing the check and not by changing the drain**, and the argument is this ADR's
> own rather than a preference for the cheaper repair. The two candidates are different cities: making the
> drain skip is a change to *who gets served*, and it is the behaviour the *Atomic servings* section
> refuses by name. ***The drain was right and the sentence describing it was too strong***, which is
> [`adr/0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s rule
> arriving inside an invariant: a check is a description of the build, and a description can overstate.
> `WorldInvariants.HeadThatShouldHaveWoken` is the narrowed predicate. **It moves no State Hash** — the
> answer the city computes is untouched and only the question asked afterwards changed.
>
> ⚠ **The narrowing exposed a second half that no reasoning about queue order reaches, and it is the
> half worth reading.** Three waiters needing three each against a deposit of six: two wake, the third
> parks, and the third **is the head**. The Bin's whole level still reads six, because `World.Wake` only
> clears `Blocked` and arms for `tick + 1` — **nothing is drawn until those rows run**. So the drain's
> guarantee is true *of an instant*, and a check that compares the head against the whole level is asking
> after the budget has gone. `RuleEngine.AccumulateClaims` derives what every armed Rule Instance will
> draw and subtracts it. ***A woken waiter records no claim anywhere***, and the alternative — a reserved
> column on the Bin, incremented by the drain and released on apply — is the reservation this ADR already
> refused, arriving as bookkeeping. Deriving it costs one pass over the Rule Instances at end of run,
> where `02 §10` already puts a whole-world walk, and adds no saved field to drift.
>
> ⚠ **Neither half is reached by the other's repair.** Nobody was skipped in the spent-down run, and the
> parked waiter is the head; nobody was over-claimed in the starvation run, and the head is genuinely
> uncovered. **Two tests, one for each, and both are the probe that found the defect kept rather than
> reverted.**
>
> **`RuleEngine.BinStillBlocks` is gone rather than left standing.** Its only production caller was the
> walk, and keeping it for the two tests that reached through it would leave a second spelling of *does
> this Bin block this waiter* that nothing runs — and the one nothing runs is the one that drifts. Both
> tests now assert against `RuleEngine.Requirement`, which is what they were ever about.

---

## What would trigger revisiting

- **The derived requirement's cost at the write site.** If a capture shows the derivation is a material
  share of the Tick — the threshold worth stating is *more than the Bin write it accompanies* — then the
  requirement wants caching, and the honest cache is the stored column returning with an explicit
  invalidation on reload. That would be this decision reversed on a number, which is the correct way to
  reverse it.
- **`pool` arriving and the gradient failing to appear.** This ADR rests on rotation producing *every
  bakery bakes half as often*. Nothing has ever had two waiters on one Bin, because `local` is the only
  scope with a Bin behind it. **The first Ruleset in which two Buildings contend for one Pool is the
  first test of the central claim**, and if the observed behaviour is persistent starvation of the large
  waiters rather than a gradient, the queue discipline is wrong and not merely the predicate.
- **A mechanism that needs partial acquisition as the *default*.** If more than one or two Rule families
  want accumulation, it stops being a content pattern and wants engine support — at which point the
  reservation objections have to be answered rather than avoided.
- **`adr/0033`'s invariant still unbuilt when the next slice closes.** An unguarded correctness decision
  is a decision that will regress silently. If the invariant has not been built by then, that is
  evidence this ADR shipped without its check and the sequencing was wrong.
