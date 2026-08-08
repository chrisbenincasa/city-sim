# 0011 — Slice 7: the Rule engine, Bins and Bin Rules

> Slice 7 of [`0003-build-plan.md`](0003-build-plan.md). Roadmap **milestone 3a**.
> Governed by [`02 §4`](../docs/02-simulation-model.md),
> [`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md),
> [`adr/0045`](../docs/adr/0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md),
> [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md),
> [`adr/0015`](../docs/adr/0015-all-tuning-data-is-hot-reloadable.md).

**A Bin Rule is an atomic transformation over Bins, declared in the Ruleset and interpreted by the
binary.** This slice builds Bins and their wait lists, the Ruleset loader and its refusals,
Rule evaluation with atomicity and apply count, and `on_fail` chains. It is the first slice in which
the simulation *does* something — until now the Tick has been a skeleton with a hash on the end.

**The risk it retires:** that the Rule engine is a design nobody has tried to write down as code, and
that `02 §4`'s two execution models are one model with an exception bolted on.

---

## Status

**Tasks 1 and 2 are done**, plus the minimal Event Wheel that decision owed 1 branch (c) calls for.
The gate cleared on 2026-08-07 (session A → `adr/0048`).

`Borough.Core.Rules` holds the Bin with **no public level column**, the Rule Instance with its **two
mutually exclusive states sharing one link**, and a bucket-per-Tick Wheel. `World.Deposit` /
`World.Withdraw` are the only doors onto a Bin's level and they drain the wait list in the same call.
473 tests green; the golden baselines were re-recorded once, for three tables entering the hash
composition and nothing else — the same shape as slice 6, so `World.HashSeed` is untouched.

**Task 3 is done.** `Borough.Formats.RulesetLoader` walks Tomlyn's **syntax tree** rather than its
model, so every refusal carries a file, a line and a rule name; `Borough.Core.Rules.Ruleset` is the
string-free table of ids and integers the core receives. All three fire under test, the
previous Ruleset stays live through `RulesetInForce`, and `BoundaryTests` now asserts mechanically
that the core cannot name the parser — which is the sentence `adr/0048`'s *the exception is not
owed* actually rests on. 495 tests green.

**Task 4 is done.** `Borough.Formats.QuotedDecimal` reads a quoted decimal digit by digit into a
`Ratio` — no format provider, no `decimal`, nothing a locale can move — and the culture test
`adr/0048` asks for **could not be written the way it is written anywhere else**: `InvariantGlobalization`
makes `new CultureInfo("de-DE")` throw, so the rig is a hand-built `NumberFormatInfo` and it bites
harder than `de-DE` would have. 510 tests green.

**Task 5 is done.** `Borough.Core.Rules.RuleEngine` fills Phases 1, 2 and 3, which were empty
skeletons until now: the Wheel bucket is collected, every due Rule is evaluated read-only, and the
intents are applied in a **counter-based shuffle order** with atomicity re-checked as each is reached.
Atomicity is over **net deltas per Bin**. `local` and `map` are real; `pool` and `global` are named
holes. Two claims were verified by mutation rather than by argument — deleting the net merge and
replacing the shuffle with the slot index each fail exactly one test. 530 tests green, and the golden
baselines did not move, because the Ruleset is not folded into the State Hash and the golden session
declares no Rules.

**Task 6 is done.** A Rule is now evaluated at the largest count its Bins allow within its band, so
`apply = { min = 1, max = 4 }` is executed rather than merely parsed — until now a greedy band loaded
clean and behaved as `{ 1, 1 }`. The raise is **one arithmetic and not a search** (finding 19), the
failure shortfall stays the **floor's** rather than the ceiling's, and Phase 3's re-check gained a
direction it did not have when every count was one: it **may serve a greedy Rule short and may never
serve it more** (finding 18, and it wants ratifying). 538 tests green; the golden baselines did not
move, because the golden session declares no Rules. `derived` still throws — it needs task 7's
Readouts.

**Task 7 is done.** `Borough.Core.Rules.Readout` declares the readable set — **one member,
`occupancy`** — and `Readouts.Read` is the only way to obtain a value, which is what makes *a Rule
cannot act on what a player cannot inspect* structural rather than a promise. `derived` apply counts
work, carrying `02 §4.1`'s percentage; an undeclared name is refused at load quoting the declared set,
and an undeclared **id** throws in the interpreter, which is `adr/0048`'s two-sided drift answer with
no second validator. **A latent defect surfaced**: `Fires` meant `Applications > 0`, so a derived zero
— which `02 §4.1` calls a *success* — would have subscribed to `Rows.NoSlot`. See finding 21. 549
tests green; the golden baselines did not move.

**Task 8 is done.** `RuleEngine.Walk`/`Descend` evaluate a failed Rule's `on_fail` chain. A link
**refills the Bin the head failed on** rather than doing the head's work by another route, so a
rescued Building runs the link's terms and bakes nothing that Tick — the link's deposit wakes the
head through the Bin's wait list, which is `02 §7`'s *mutators wake observers* rather than any retry
logic. A failed chain returns **the head's** verdict, so the single subscription lands on the head's
Bin and not on whichever Bin the walk actually stopped on. **The terminal is never evaluated**: it
has no term that could be short, so ordinary Rule semantics would fire it and re-arm the head on
`rate`, and deleting that guard fails four tests by name. A fifth refusal,
`RefuseUnterminatedChains`, makes the terminal a load-time law rather than a convention. **Two
findings**, 22 and 23 below. 564 tests green; the golden baselines did not move, because the golden
session declares no Rules.

**The Resource family was taken out of order, between tasks 7 and 8, because the money leak had no
smaller fix.** A `[[resource]]` declared a name and nothing else — not its family, and neither of
`CONTEXT` → Resource's two parameters — so `money` was a Good with a warehouse ceiling and
`02 §4.3`'s own bakery destroyed one money per baking. `family` is now required with no default, a money Bin is
**unbounded** and refuses an authored ceiling, **refusal 4** refuses any Rule whose money terms do not
sum to zero in either direction, and `storage` is a **named hole that throws** rather than a key taken
and dropped. `02 §4.3`'s example is corrected. Decision owed 6 is settled: a transfer and a purchase
are different shapes, so both mechanisms stand and `adr/0050` survives re-reading. See findings 24–28
— **27 is the one to read**, since the leak reached six slices only because the transcription into the
loader's own fixture had silently dropped the money line. 556 tests green; the golden baselines did
not move.

**Tasks 8–10 are not started.**

---

## Gate

**Cleared.** Session A settled everything slice 7 was waiting on:

- the parser is **Tomlyn**, in `Borough.Formats`
- the **validator lives with the parser**, and the core receives ids and integers, never a string
- **three refusals** in one load-time walk: the `on_fail` cycle check, the `fills` check, and an
  unquoted decimal
- a tuning number is a bare TOML integer or a **quoted decimal string** converted by our own routine

`adr/0003`'s owed dependency exception turned out never to be owed — there is no core dependency.

---

## Prerequisites

Slices 4, 5 and 6, all closed. Specifically: typed tables with the per-field declaration, the eight
Tick phases, the Input Log and replay, the invariant tiers, and the Census.

---

## The dependency this plan found, and it is not small

**`02 §4.1` says a Rule that fires successfully re-arms on the Event Wheel, and the Event Wheel is
slice 9.** The sentence is not incidental — it is the mechanism: *"nothing ever walks the Building
list looking for work."* Slice 5's `Wake` phase is empty and says so.

**Most of slice 7 does not need it.** Bins, wait lists, subscription, atomicity, apply count,
`on_fail` chains, the loader and its refusals are all reachable with an empty `Wake`, because a
subscription is woken by *the mutator that writes the Bin*, not by a timer. What needs the Wheel is
exactly one thing: **scheduled dispatch — the `rate` re-arm after a success, and the first firing of
any Rule at all.**

This is recorded as **decision owed 1** below rather than settled here, because it is a real fork and
one of its branches re-blocks the code column.

---

## Tasks

### 1. The Bin

A typed container of one Good on one Building. A `[Table]` in `Borough.Core`, per the field
declaration: level, capacity, and the wait-list head.

Bins are **not public fields** — `05 §9` is explicit that every write goes through one function that
drains the wait list, because *"a Bin written without draining its wait list leaves that Building
asleep forever, with no error and no timer to rescue it."* That is structural, not disciplinary: make
the column private and the mutator the only door.

### 2. The wait list, and the recorded shortfall

An intrusive index list per Bin, per the core's collection rule. A subscription records **the amount
that was missing**, which the waiter already computed when it failed.

On a write the Bin drains **from the head, only while the arriving quantity covers the recorded
shortfalls**. `02 §4.1` is emphatic about why: waking every subscriber instead would push them all
into Phase 3 and let the sorted settle order pick a permanent winner, so the head would lose every
time and the list would be decorative. **Six flour arriving wakes exactly the one bakery that needs
six.**

A Rule that fires goes to the **back** of the queue. Round-robin is a balance decision as much as a
determinism one — strict FIFO would satisfy `adr/0003` equally and produce a starvation wall.

A recorded shortfall may be stale if the waiter's own Bins changed while it slept. Nothing special is
needed: it re-checks atomicity in Phase 2, fails, and resubscribes.

### 3. The Ruleset loader and its refusals

In `Borough.Formats`, per `adr/0048`. Tomlyn in, ids and integers out.

Refusals, one walk at load, each naming a file, a line and a rule name:

1. **`on_fail` cycle** — the graph has out-degree 1, so chains are paths and a cycle is a malformed
   Ruleset rather than a runtime hazard.
2. **`fills`** — every link relieves the same Bin the head failed on; a link whose rescue arrives
   later declares what it fills. A chain that cannot satisfy this is refused.
3. **Unquoted decimal** — refused by name, never coerced, because a library `double` must not reach a
   tuning number.

A fourth arrived later, with the Resource family, and it is a different kind of refusal from these
three — it is about **what a Rule means** rather than how it is spelled:

4. **Money that does not balance** — a Rule whose money terms do not sum to zero across its inputs and
   outputs is refused in either direction. Destroying money and creating it are the same defect with
   the sign flipped, and `adr/0024` allows neither inside the city. This is what an explicit money
   term being a **transfer** amounts to mechanically: it names both ends. A *purchase* has no syntax
   at all and is untouched by this (`adr/0050`).

The **previous Ruleset stays live** on a refusal. That is `adr/0015`'s error surface and it is part of
the deliverable, not polish.

### 4. Quoted decimal → Q16.16

Our own routine, in `Borough.Formats`. `"0.15"` → `Ratio`. Exact, integer-only, invariant-culture,
with a test that sets a non-invariant culture and proves the result unmoved.

Under `adr/0043` the claim *"Tomlyn's integer parsing is culture-insensitive"* is **measurable**, and
the machine is a unit test. Write it rather than assume it.

**Done.** `Borough.Formats.QuotedDecimal.TryParse` reads the digits by hand — no `decimal.Parse`, no
format provider, nothing a locale could move — and floors to Q16.16, so `"0.15"` and
`Ratio.FromFraction(15, 100)` are the same number by assertion rather than by coincidence. Both
claims are measured, and the rig had to be built rather than named: see findings 10 and 11.

### 5. Rule evaluation and atomicity

A Rule declares inputs and outputs against the four scopes — `local`, `pool`, `global`, `map`. There
is **no proximity scope**: movers choose, Rules transform.

**Atomicity is the core semantic.** If any input is insufficient or any output would exceed capacity,
nothing happens and the Rule fails. No partial application, ever — that is what makes the economy
conserved and failure reportable.

`map` is **write-only** and therefore outside the subscription question entirely: a `map` output
cannot fail and no Rule ever waits on one.

Evaluation happens in **Phase 2 (Decide) and writes nothing**; application is Phase 3 (Settle). The
existing `VerifyDecideWritesNothing` guard is what proves it — and note it is `O(world)` per Tick, so
the 1M runs turn it off with `--no-decide-guard`.

**Done.** `Borough.Core.Rules.RuleEngine` collects in Phase 1, evaluates in Phase 2 and applies in
Phase 3, and `Simulation`'s three empty phases now call it. Atomicity is checked over **net deltas per
Bin** rather than term by term, which is not a refinement — see finding 13. `local` and `map` are
real; `pool` and `global` throw as named holes, and one of them **could not** be a load refusal
(finding 14). Phase 3's order is the project's **first `PurposeTag`**, and the claim it exists to
discharge was measured rather than asserted (finding 16). Task 5 runs the apply *floor*, so the fixed
case (`min = max`) was already correct and a greedy Rule already conservative; task 6 raised it, and
that the shortfall arithmetic needed no change is the evidence the floor was the right thing to run
first.

### 6. Apply count — `{min, max}` and `derived`

A Rule declares **either** `{min, max}` **or** `derived`, never both.

- `{min, max}` applies as many times as inputs allow within the band, and **fails below `min`**,
  subscribing with a shortfall of `(min × amount) − available`.
- `min = max` is the fixed case. One form expresses both; there is no third spelling.
- `derived` computes `n` from a **Readout** — integer arithmetic, no floats, no expression language.
  A derived count of **zero is a success**, re-arms normally, and waits on nothing, because a Readout
  is not subscribable.

Greedy versus fixed is a **modelling decision fixed at design time**, never a performance one: *greedy
when the actor works through its stock, fixed when the actor owes a quantum.*

**Done.** `RuleEngine.Check` takes a ceiling, accumulates the net deltas at **one** application, and
lets each Bin state the count it can carry — `level ÷ −delta` for a draw, `headroom ÷ delta` for a
deposit — taking the least of those and the Rule's `max`. Failure is unchanged and still tested
against the **floor**: a Bin affording fewer than `min` blames itself and subscribes with
`(min × amount) − available`. `min = max` needs no branch of its own, which is what makes the two
spellings one form in the code and not only in the prose. Two findings, 18 and 19, and **18 is a
semantic the corpus had never had to state** because it cannot arise while every count is one.

### 7. The Readout set, declared simulation-side

A named read-only scalar an entity exposes. **The readable set is declared in the simulation**, and
every declared Readout is inspectable — so no Rule can act on a quantity the player cannot inspect,
by construction rather than by reference.

The converse does not hold: the inspectable surface is much larger. The test for admitting one is
*does a Rule read it, or is it only displayed?* Display-only is a display, never a Readout.

**Done.** `Borough.Core.Rules.Readout` is the declared set and `Readouts.Read` is the only way to
obtain a value, so a panel calls what a Rule calls and there is no second path to drift from. **The
set has one member** — `occupancy`, the Households in a Building — because the admission test is
narrow and nothing else in the world today is read by a Rule. Gross income, experience, time
unemployed and composed fertility are all named as Readouts by `CONTEXT` and **none is declared**; a
name the loader cannot resolve is refused, quoting the set that does exist. `derived` also carries
`02 §4.1`'s **percentage** — `readout × percent / 100`, floored — which is the corpus's own spelling
of *"15% of gross income"* and the reason `CONTEXT` → Policy prefers percentages to flat amounts.
Three findings, 21–23, and **21 was a latent defect that a derived count of zero would have tripped
the first time anyone wrote one.**

### 8. `on_fail` chains

A source ladder over one Bin, per `adr/0045`: local Bin → District Pool → Shipment → import →
terminal.

- **A failed chain subscribes once, at its head.** Chain depth costs no subscriptions.
- **The last link is a reporting terminal**: it records a condition and leaves the chain *failed*. A
  terminal that succeeded would re-arm the head on `rate` and walk the chain for ever — which is the
  polling defect the subscription model exists to remove, and which the corpus's own worked example
  contained.
- **No depth cap.** The ladder bounds depth structurally, and any number published today would be an
  argument wearing a measurement's clothes.

### 9. The two counters

`02 §4` names them and calls them slice 7's: **Rule evaluations per Tick**, and **walked chain depth**.

State the tripwire over **measured evaluation cost against the Tick budget** — *chain walking fits
while fewer than N evaluations occur per Tick* — and never over a depth. Publish the break-even, not
a multiple over a guessed denominator.

These feed the Census, which already has the ring and the `series(metric, window)` API.

### 10. A Ruleset with something in it

Enough content to prove the engine, and no more: a production chain over two or three of `04 §1`'s
five Goods, one greedy Rule, one fixed Rule, one derived Rule, and one `on_fail` chain that
terminates in a report.

This is the slice's *something to look at* — `--census` showing Rules firing, Bins filling and
draining, and a chain walking on entry into shortage and then going quiet.

---

## Acceptance

- `dotnet build` and `dotnet test` green with no GPU and no Godot.
- A Ruleset with a cycle, a broken `fills`, or an unquoted decimal is **refused with a file, a line
  and a rule name**, and the previous Ruleset stays live.
- Replay equivalence holds over a session in which Rules fire: two runs, identical hash traces.
- A 100,000-Tick run at a real population where **no collection and no magnitude trends upward** —
  wait lists especially, since a wait list that only grows is `adr/0006` arriving through the Rule
  engine.
- The State Hash moved, deliberately, and the golden baselines were re-recorded.
- Every unratified number this slice chose is in [`0002`](0002-open-questions.md) before it closes.

---

## What building it found

Recorded as it happens, per slice 6's practice. Seventeen so far.

**1. `adr/0045`'s *blocking* forces a Bin to have two wait lists, not one.** The plan's task 2 describes
one queue and one recorded shortfall, which is right for an input Bin that was short. But the ADR
generalises over both failure modes — *refill if the Bin was short, drain if it was a full output* —
and a single queue holding both **deadlocks in one direction**. The drain's fairness rule is that it
*stops* at the first waiter it cannot cover rather than skipping past it; a deposit can never cover a
waiter that needs headroom, so one such waiter at the head stops the queue for ever. Skipping instead
is the other defect, the one that starves a large waiter behind small ones. Two lists remove the
choice, and the discriminator is a `Blocking` column that is `adr/0045`'s own word made checkable.

**2. The armed and waiting states are one row with one link, which was not planned and is better.**
`02 §4.1` says a Rule that fires re-arms and a Rule that fails subscribes *instead of* re-arming — so
the two are exclusive by the design's own account, and a row that could be both would be a Rule that
polls **and** subscribes. Sharing the link makes that unrepresentable rather than checked. It also
removes all row churn: a Rule Instance's life is its Building's, so shortage *churn* — which `02 §4.1`
names as the real cost driver ahead of chain depth — allocates and frees nothing at all. New term in
`CONTEXT.md` → **Rule Instance**.

**3. The invariant caught a real defect in its first ten minutes, and it was in the test helper.** The
first `Sleeper` helper armed a Rule Instance and subscribed it without popping it off the Wheel —
leaving it on two queues, which is exactly the state the shared link was supposed to make impossible
and which the shared link *does* make impossible for the row while saying nothing about the two lists.
`Subscribe` cannot see it: from the write site the only evidence is a link in a bucket nobody has
walked, so the `O(1)` check at the write site catches a double subscribe and nothing else. The
end-of-run counter is what noticed. **Neither tier covers the invariant alone**, which is `02 §10`
working rather than a gap — and it is the third instance in the corpus of a check being split across
tiers for that reason.

**4. A derived list and a saved list one indirection apart.** A Building's Bins are `Derived` —
membership is a pure function of the Bins' own `owner` column and the order carries no meaning,
because a lookup by Resource is a search either way. The wait lists threaded through those same Bins
are `Saved`, because arrival order is what makes the round-robin drain fair and it is recoverable from
nothing. `IndexList.InsertOrdered`'s docstring called this in slice 4 and it landed exactly as
written; what was not anticipated is that the two would sit this close together, which is worth a
comment at both ends rather than an inference.

**5. The footprint report reads live rows, so it did not notice 30 MB.** At 1M Citizens the table
report moves only by the Wheel's 192 KiB, because `bin` and `rule_instance` have no live rows until a
Ruleset creates any. **Resident memory is a different number**: both tables pre-allocate to capacity
like every other table, so ~30 MB is committed for rows that do not exist yet. That does not change
any conclusion — S0a's own resident figure was already ~8 MiB above its table total — but **S0a's
~94 MiB owes a re-take once task 10's Ruleset actually fills them**, and the two multipliers behind it
(450 Bins and 450 Rule Instances per 1,000 Citizens) are unratified and go to `0002` before this slice
closes.

**6. `map` is not a scope of the Bin term type, and making it one would have cost three checks.**
`02 §4.1` says a Map Layer write is write-only, has no capacity, cannot fail, and can never be
subscribed to. Spelling it as a `Term` with `Scope.Map` would have made all three of those *runtime*
checks over a representable state; giving it its own `MapEmission` type makes them unrepresentable.
The loader splits one authored `outputs` array into the two, so the designer's file is unchanged —
`02 §4.3`'s worked example parses verbatim, which was the acceptance test used.

**7. The `fills` refusal is set arithmetic over the chain, and it needed no new syntax.** `02 §4.1`
says *every link in a well-formed chain rescues by relieving the same Bin the head failed on*, which
is computable: start with the head's own Bins — inputs **and** outputs, because `adr/0045`'s
*blocking* covers both — and intersect with what each link relieves, where relieving is outputting
to it, drawing from it, or declaring `fills`. Empty intersection, refused. **The head is the only
rule whose Bins seed the set**, per `adr/0045`'s *a failed chain subscribes once, at its head*, so
the check runs from chain heads rather than from every rule with an `on_fail` — mid-chain the
question is not asked, because the blocking Bin was never that link's.

**8. A reporting terminal has to be exempt from the `fills` check, and that is what makes `reports`
a field rather than a flag.** A terminal rescues nothing by design, so requiring it to relieve the
head's Bin would refuse the corpus's own worked example. Carrying a `ConditionId` rather than a
boolean means the exemption and the diagnostic are the same declaration.

**9. Tomlyn 2.10 has no `Toml` facade**, which the ADR's own example call used. The syntax parser is
`Tomlyn.Parsing.SyntaxParser.Parse(text, sourceName, validate)`. Cost: one line. Recorded only
because `adr/0048` quotes the older spelling and a reader following it will not find it.

### What task 3 did *not* settle, deliberately

- **A chain is not required to end in a terminal.** `02 §4.1` and `adr/0045` both describe one as the
  last link, but neither states it as a load-time law, and a chain that simply ends leaves the
  Building failed with nothing recorded — which is the *silent non-event* the same section bans
  predicates for. **Adding the refusal would be a design claim made by a loader**, so it is not
  added. It belongs to task 8 or to a session.
- **`derived` refuses, and correctly.** The readable set is declared simulation-side and task 7
  populates it; this build declares none, so no name can resolve. The refusal is the honest answer
  rather than a placeholder, and it starts passing the day task 7 lands.
- **No Ruleset is loaded by anything yet.** Nothing calls `RulesetLoader` outside its tests: the
  runner still has no `--ruleset` content path and `World` still creates no Bins. Wiring is task 10's,
  and doing it now would have meant choosing task 4's content before the engine could run it.

### Task 4's findings

**10. The culture test `adr/0048` asks for cannot be written the way it is written anywhere else, and
that nearly went unnoticed.** `Directory.Build.props` sets `InvariantGlobalization` for *every*
project, tests included, and under it `new CultureInfo("de-DE")` throws `CultureNotFoundException` —
measured, not assumed. Relaxing `PredefinedCulturesOnly` would not fix it; it would hand back a
culture carrying invariant data, so the test would pass against a routine that called `double.Parse`
with no provider at all. **That is the shape `adr/0043` exists to catch**: a green test standing in
for a measurement nobody took, and here the *build configuration itself* is what would have made it
vacuous. The rig is instead a cloned `NumberFormatInfo` with the separators set by hand — no ICU
needed, and nothing a `.props` file can neutralise.

**11. The hand-built rig is harsher than `de-DE`, and the failure mode it exposes is the expensive
one.** Under it, with `InvariantGlobalization` on: `double.Parse("1,5")` → **1.5**,
`double.Parse("1.5")` → **15**, and `double.TryParse("-1")` → **false**. The middle one is the reason
this task exists — a designer writes `"1.5"` and the simulation runs at **ten times** the number, with
no exception, no refusal and no hash that could disagree, because both runs are wrong identically.
That is precisely the class `adr/0003`'s narrowing-cast note names as the one the State Hash cannot
find. Reading the digits by hand removes the format provider, and with it the argument anyone could
forget to pass.

**12. Refusal 3 was half a policy for one task.** The loader told authors to write
`decline_rate = "0.15"` while nothing in the project could read that spelling. It is now whole, but
the routine still has **no caller**: every tuning number the Ruleset currently carries — `rate`,
`amount`, `capacity`, the apply band — is a whole number, so the first Ratio-valued key arrives with
task 7's Readouts. Recorded so the gap is a decision rather than an oversight.

### Task 5's findings

**13. Checking atomicity term by term is a deadlock, not a conservatism — and it took a mutation to
see which.** A Rule naming one Bin on *both* sides checks its output against a Bin the withdrawal was
about to make room in, so it is refused headroom, subscribes on `Blocking.Headroom`, and waits for a
drain that only it would ever have performed. The first reading of this was *"conservative, may fail
where it could succeed"*, which is wrong in the way that matters: **nothing wakes it.** Checking the
**net delta per Bin** removes the case rather than documenting it, and it is load-bearing twice over —
`World.Deposit` asserts against headroom, so applying such a Rule term by term would trip an invariant
the check had just passed. Verified by mutation: deleting the merge in `Touch` fails
`A_rule_naming_one_bin_on_both_sides_is_checked_net` and nothing else.

**14. `adr/0015`'s error surface loses to `02 §4.3`, in exactly one place, and the inversion is
right.** The obvious home for a named hole in data-driven work is a load-time refusal — a designer
gets a file, a line and a rule name instead of a panic mid-run. But **the corpus's own worked example
rescues its bakery from the District Pool**, and task 3's whole standard was that *a loader that
refuses the corpus's example is not a loader*. So a Ruleset naming `pool` loads, runs, and fails the
first time a Rule actually reaches for it. Recorded because the general rule and this instance point
opposite ways, and a later reader will otherwise "fix" it.

**15. `map` has one emittable Layer, not three.** The loader accepts `pollution`, `land-value` and
`sealing` because those are the Layers; only pollution is a quantity a Rule *adds*. Land value is
chased towards a target by the momentum operator — `SetLandValueTarget` is a different verb from *add
this much* — and Sealing is a property of a footprint rather than an output of an application.
Accepting either would have meant inventing a semantic for it inside a `switch`, so both throw.

**16. The project's first `PurposeTag` is a settle order, and its justification is measurable.**
`02 §8` rule 5 says a contested outcome must be shuffled because ordering by entity id is *biased* —
"the same Building would win every contested draw for the life of the city". Under `adr/0043` that is
a claim a number settles, and the number is a win count: thirty-two worlds identical but for the Tick,
two Rule Instances contesting six flour. Replacing the draw with the slot index makes it **32–0**, and
the test says so by name. The draw happens in Phase 2 rather than Phase 3, which costs nothing —
`Randomness.Draw` is a pure function of its coordinates, so it needs no stream and no ordering.

**17. A local term naming a Bin its kind does not declare is a load-time refusal nobody has written.**
Both facts sit in one file — the kind's `bins` and the Rules attached to that kind — so this is a
consistency check within a Ruleset rather than a design claim, which makes it the *safest* kind of
refusal to add and the one most clearly owed. It is not added here because it is a task 3 change, and
the engine throws with a message that names the gap. Owed before slice 7 closes.

### Task 6's findings

**18. A greedy count gives Phase 3's re-check a direction, and nothing in the corpus states one.**
While every apply count was one, the re-check had two outcomes — the intent still fits, or it does not
— so *which way it may move* was not a question anybody had to answer. A greedy count makes it one:
by the time an intent is reached, an earlier intent may have **deposited** into the Bin it draws from,
so the Rule could now afford more than it decided on. Serving that would make a Rule's consumption
depend on the shuffle in a way Phase 2 never saw — the same city eating twenty-four flour or twelve
depending on a draw. **Settled as: the re-check may lower a count and may never raise it**, by passing
the Phase 2 count down as a ceiling. The reasoning is `adr/0037`'s — Phase 2 decides against the Past,
and lowering is *forced* by conservation while raising is merely available — and the surplus is simply
there next time the Rule is due. Verified by mutation: re-checking unbounded leaves 12 flour where the
contract leaves 24, and fails exactly one test.

**Settled in [`adr/0049`](../docs/adr/0049-a-rules-apply-count-is-decided-against-the-past-and-settle-may-only-reduce-it.md)**,
after a grilling session that made the debt worth having. Three things came out of it that this
finding did not contain. **The alternative was a real one**: a *natural* settle order — producers
before consumers — has corpus support behind it (`04 §1`'s depth-three DAG makes a topological sort
feasible) and dies on `02 §5`'s **unauthored lag**, because whether a chain collapses inside one Tick
would depend on whether its Rules are due together, which depends on phase offsets set by
construction order. **The stricter option was also live** — Phase 3 honours the decided count or
fails — and it is wrong for greed and *already spelled `min = max`*, so making it global would delete
greedy Rules to obtain something a designer can ask for one Rule at a time. And **the shuffle's remit
shrank to one quotable sentence**: it decides who goes short when there is not enough, never how much
anyone takes when there is.

**19. The raise is one arithmetic rather than a search, and that is a property of linearity.** A
Rule's net delta per Bin is linear in the apply count, so every Bin can *state* the count it affords
by a single division, and the answer is the least of those. The obvious spellings — step down from
`max`, or bisect — are a loop per Rule per Tick, against a Tick that `02 §4`'s own counter is there to
watch. Worth recording because the property is not guaranteed to survive: anything that makes a term's
amount depend on the count would restore the search, and task 7's `derived` count avoids this only
because it *computes* `n` rather than bounding it.

**20. The shortfall a greedy Rule subscribes with is the floor's, not the ceiling's.** A Rule that
cannot fire at `min` waits for the least that would let it fire *at all* — subscribing for what it
wanted would sleep through a delivery it could have used. This needed no work because task 5 ran the
floor, which is the argument that task's note made for running it; recorded because it is invisible in
the diff and a later reader optimising the wait list could easily "correct" it.

### Task 7's findings

**21. `Fires` and `succeeds` were the same predicate, and a derived zero is what separates them.**
Task 5 defined a verdict as firing when `Applications > 0`, which is exactly right while every count
is at least one. `02 §4.1` says **a derived count of zero is a success** — it re-arms on its rate and
waits on nothing, because a Readout is not subscribable and there is no Bin that could ever wake it.
Under the old predicate a zero-count Rule took the failure branch and **subscribed to
`Rows.NoSlot`**: a Rule asleep on a Bin that does not exist, which nothing would ever drain and no
invariant names. Success is therefore *no Bin stopped this*, not *something moved*. **The defect was
latent rather than theoretical** — it was unreachable only because `derived` threw, and it would have
fired on the first zero-occupancy dwelling in task 10's proving Ruleset. Verified by mutation:
restoring `Applications > 0` fails exactly one test.

**22. A derived count is a band of one, which is the fixed case reached from the other side.** A
Readout *states* how many times a Rule applies rather than bounding it, so floor and ceiling are the
same number and a derived Rule **cannot be served short** — it applies `n` times or it fails and
subscribes. That falls out of task 6's structure with no branch of its own, and it is the code half of
`CONTEXT`'s *greed handles what is consumed, derived handles what is consulted*. `adr/0049` governs it
too: Phase 3 re-reads the Readout and then clamps to the Phase 2 count, so a Readout that grew
mid-Tick cannot enlarge what the Rule decided, for the same reason a Bin that grew cannot.

**23. The name of a Readout cannot live where the Readout is declared.** The set must be in `Core`,
because `Core` is what reads it; the *name* cannot be, because `adr/0002` forbids `Core` a string a
human reads. So `Borough.Core.Rules.Readout` is the declaration and `Borough.Formats.ReadoutNames` is
its vocabulary, joined at load exactly as `adr/0048` joins everything else — **and nothing at runtime
checks that the two describe the same set**, because `adr/0048` refuses a second validator. That check
is a test instead, and it is worth having for the direction it fails in: a declared Readout with no
name is not a crash, it is a feature that silently does not work with nothing to grep for.

### Task 8's findings

**22. Every link was being armed as its own Rule Instance, and a reporting terminal armed that way
polls for ever.** `ReadKinds` built each kind's Rule list from *every* `[[rule]]` naming that kind,
heads and links alike, and task 10 would have created an instance per entry. `mark_input_starved` has
its own `rate`, so it would have been collected from the Wheel every ten Ticks and reported — with no
shortage, no head, and no walk — which is verbatim the polling defect `adr/0045` exists to remove,
arriving through the Rule Instance table rather than through the chain. **A link is now excluded from
`kindRules`**: a Rule that is some other Rule's `on_fail` is reached by walking a chain that failed
and is never armed on its own rate. The tell was in a green test — `RulesOf(1)` asserted **4** for the
corpus's four-deep chain, which reads as *the bakery has four Rules* and means *the bakery will run
four Rules independently*. The assertion is now the head alone, spelled as the id rather than the
count, because a count is what let the wrong answer look right.

**23. `adr/0049` did not need amending, and working out why moved the design.** `02 §4.1` requires a
Phase 3 atomicity loser to take its fallback, so a link can be reached having never been evaluated
against the Past — which looked like a direct contradiction of an ADR written one task earlier. It is
not, and the reason is in `02 §4.1`'s own worked case: the two contending bakeries contend over *"six
flour in the **Pool**"*. Contention needs a **shared** Bin, and a `local` Bin belongs to one Building,
so the Phase 3 loser is normally a **link** rather than a head — and the Bin it lost on is *genuinely*
short, because a peer really did drain it. Escalating down the ladder is the ladder working. What
`adr/0049` forbids is a **producer's throughput** moving with shuffle position; who wins a scarce Pool
is what the shuffle is *for*, in that ADR's own words. So it gained a scoping consequence and no
amendment. **The candidate that had to be rejected is worth recording**: serving a Phase 3 link at its
authored floor would have honoured `adr/0049` by construction, and it was machinery for a case that
may be empty — down-ladder rungs mostly have no contendable terms at all, since `request_shipment`
declares `fills` and carries no inputs or outputs, and the terminal carries none either.

### The Resource family, taken out of order

**24. Money was disappearing because a Resource carried a name and nothing else.** `CONTEXT` →
Resource names three things that distinguish every member — the **family**, the **capacity** and the
**storage** — and the Ruleset declared none of them. So `money` was a Good with a warehouse ceiling,
and `02 §4.3`'s own bakery drew one money per baking and returned none: a leak in the corpus's
flagship example, in direct contradiction of `adr/0024`, in the document for six slices. The fix is
not a refusal bolted onto a name, it is the missing field. `[[resource]]` now declares
`family = "good" | "utility" | "money"`, **required, with no default** — `good` is the tempting
default and it is wrong in the direction that does not announce itself, since a Resource silently
filed as a Good is conserved by nothing, ceilinged like a warehouse and shipped by Vehicle.

**25. The family is what makes the conservation check writable at all.** Refusal 4 sums a Rule's money
terms across inputs and outputs and refuses a non-zero total, in either direction — *destroys 1 money*
and *creates 5 money* are the same defect with the sign flipped, and the second reads as inflation
rather than as a bug. This is `adr/0024` made mechanical rather than remembered, and it is the first
refusal in the loader that is about **what a Rule means** rather than what it is spelled like. An
explicit money term is therefore only ever a **transfer** — it names both ends — while a *purchase*
has no syntax at all, which is `adr/0050`.

**26. A money Bin is unbounded, and authoring a ceiling on one is refused rather than ignored.** A
finite ceiling on money means an actor too full of money to be paid and a sale failing on headroom
because the seller is rich, which is not a game mechanic anybody asked for. `BinCapacity` carries an
explicit `IsUnbounded` rather than a large sentinel a reader must recognise — `CONTEXT` asks for
exactly that — and `int.MaxValue` sits underneath it only so the arithmetic stays uniform. That works
solely because `HeadroomAt` is `capacity − level`: the form `CONTEXT` prescribes, whose reason is this
Bin, and the reason is now written at the method rather than left to be rediscovered.

**27. The example that leaked was never run, and that is the finding worth keeping.** Whoever
transcribed `02 §4.3`'s bakery into the loader's own test fixture **silently dropped the money line**.
So the corpus had a worked example nothing executed, and a test suite whose fixture agreed with the
code instead of with the document. A transcription that quietly drops the inconvenient term is the
failure mode a green suite cannot see — the same shape as `adr/0043`'s two claims that sat in
documents `0002` marks fully argued.

**28. Storage is filed as a named hole that throws, not as a field taken and dropped.** The third
parameter — whether a Bin carries over between periods, zero for Power, filling for Waste — is
per-Tick Bin behaviour and none of it exists. An accepted-and-ignored `storage = 0` would give a
designer a Power Resource that warehouses electricity for ever, which they would debug as balance;
the loader refuses the key by name and says why. Slice 6's holes set the form.

---

## Decisions owed, found while planning

**1. Scheduled dispatch needs the Event Wheel, and the Event Wheel is slice 9.** Three branches:

- **(a) Swap slices 7 and 9.** Honest, and it re-blocks the code column immediately: slice 9's gate is
  session **C** (`02 §7` + `adr/0006`), which is not cleared.
- **(b) Slice 7 scans the Building list until the Wheel lands.** Cheapest to write and the thing
  `02 §4.1` explicitly forbids — *"nothing ever walks the Building list looking for work"* — and
  `adr/0015`'s own argument says a retrofit competes with real work and loses.
- **(c) Slice 7 builds the minimal bucket it needs; slice 9 generalises it.** The columns already
  exist — `next_event_tick` and `wheel_next` were declared in slice 4 and `Simulation.Wake` says so —
  so what is missing is a bucket array and a drain, not a design.

**Recommendation: (c)**, with the reservation that it must not quietly decide what session C owns.
Session C's subject is the Wheel's *semantics* — sinks, `adr/0006`, what `02 §7` promises — not
whether a bucket array exists. If writing the bucket turns out to require answering one of those,
that is the signal to stop and take (a).

**SETTLED: (c), and the reservation held.** `Borough.Core.Rules.EventWheel` is a bucket per Tick and
a drain, and nothing more. It is a `[Table]` rather than a bare array because a bare array beside the
columns is what `BOR0901` is an error for. **The one thing it decides, it refuses rather than
decides**: an arming of zero Ticks or of a whole period or more throws, because both land the row back
in the bucket being drained. Carrying a longer sleep is an overflow list, which is slice 9's, and
clamping here would have answered that question quietly. Nothing about sinks, `adr/0006` or `02 §7`
came up.

**2. How many Bins does a Building have, and are they declared per Building kind?** `02 §4` does not
say. It bears on the table layout, which is on the *expensive to retrofit* list.

**SETTLED: a Bin is a row, listed intrusively off its Building, and the set is declared per Building
kind in the Ruleset with each Bin's capacity.** So *how many* has a structural answer rather than a
guess, and a capacity is ordinary tuning data under `adr/0015` rather than a number in code. The two
alternatives were a contiguous block per Building — which buys `ResourceMap`'s binary search back at
the price of a second allocator and a fragmentation sink under `adr/0006` — and fixed Bin slots on the
Building row, which pins a maximum at world creation and charges every Building for the widest kind.
The list is a handful of entries, so the search the block would have bought back is a few sequential
comparisons. Recorded in `CONTEXT.md` → Bin.

**3. The District Pool does not exist.** The `pool` scope *"requires road connectivity"*, and there
are no roads, no Districts and no connectivity until Phase 2. Slice 7 probably ships `local`,
`global` and `map`, and declares `pool` as a **named hole that throws** — slice 6's pattern, which
exists precisely because a placeholder returning zero is a value somebody will read and tune around.

**SETTLED: `local` and `map` ship; `pool` and `global` are both named holes, and the guess about
`global` was wrong.** `pool` is blocked on machinery — roads, Districts, connectivity — exactly as
predicted. `global` turned out to be blocked on something the plan did not see: a city-wide Bin is a
Bin **no Building owns**, and where such a row lives is an entity decision. The only content ever
named for it is the treasury, and Money is on this slice's *deliberately does not do* list, so
inventing an entity to host a scope with nothing to put in it would be a design change made to avoid
writing `throw`. Both throw with distinct messages naming what they are waiting for. Neither is
refused at load — see finding 14, which is the one place `adr/0015`'s error surface loses.

**4. `04 §1`'s Goods are five and the Resources are nine.** Which of them slice 7's proving Ruleset
uses is a content decision, and content is meant to follow the Ruleset work rather than lead it.

**5. A Bin Rule term is a Ruleset constant, and a price is not — so a purchase is not expressible as
a Bin Rule.** Found while grilling task 6, and **not settled here deliberately**. A term is
`new Term(BinRef, 6)`: an integer fixed at load, hot-reloadable, the same everywhere in the city.
`04 §4` makes a price the opposite of each of those — **per-District**, recomputed **each Day** from
Pool level against recent consumption, and *"not set by the player and not authored in the Ruleset"*.
The two cannot both be true of the same mechanism.

**Worth being precise about what is and is not missing.** Money is not the gap: `6 flour → 4 bread`
**already is** an exchange at a constant ratio, and a Ruleset of such terms is a working barter
economy. The gap is that a term amount cannot **vary**, which is a statement about the Rule engine
rather than about the economy — so this is a slice 7 question wearing an economy question's clothes,
and it is the reason it is recorded here.

Three shapes it could take, none of them argued: a purchase is **not a Bin Rule** and prices live in a
separate mechanism; or `derived` carries it, which would mean task 7's *compute `n` from a Readout* is
specified far too narrowly and would have to price a **term** rather than a count; or **terms gain a
variable form**, which is the largest of the three and reaches `adr/0015`'s no-`const` rule from an
unexpected side.

**It does not block slice 7.** Greedy apply stands on the labour Bin regardless (`02 §4`), `global`
and `pool` already throw as named holes naming the treasury, and conserved Money is Phase 2 of the
roadmap. **But it should be grilled before task 7**, because task 7 is `derived`, and one of the three
shapes above lands squarely on it. Filed to `04 §8` as open question 2.

**SETTLED, before task 7, in
[`adr/0050`](../docs/adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)
— and none of the three shapes won.** The fourth was that **a purchase was never a transformation**. A
scope answers *whose is it*, not *where do I look*: a `local` term is free because the Bin is already
the Building's, and a term crossing an **ownership boundary** is a **trade**, with the Good moving one
way and money the other at the prevailing price. **There is no Ruleset syntax for the payment at all**,
because nothing is left for a designer to write — so `amount` stays a fixed integer permanently and
the expression language never arrives.

**Four things came out of the session that the debt did not contain.**

**The impossibility is arithmetic, not aesthetic.** `n × (−1 money, +1 Food)` is `n` money for `n`
Food — **the count cancels out of the ratio**, so a derived apply count expresses a *variable quantity
at a fixed rate* and can never express a *variable rate*. That also explains why taxes, wages and
Policy percentages sail through: they are **one-sided**, so there is no ratio to vary. **Task 7 is
therefore confirmed as specified rather than widened**, which is what this grilling was for.

**The first proposed split was wrong, and the objection that killed it is worth keeping.** The draft
answer was *Household purchases are not Rules, Business Pool draws are* — discriminating on who chooses
the source. It hollows out the Business: inputs free down an authored ladder and outputs free into the
Pool leaves **no margin, no bankruptcy, and nothing for `04 §4`'s satisficing Business to satisfice
over**. The right line is not *who chooses* but *whose is it*.

**`adr/0045`'s ladder is a price ladder.** local → Pool → Shipment → import is **monotone increasing in
cost**, bounded above by `04 §4`'s import ceiling — which is *why* the rungs are in that order, and
which nothing in the corpus had said.

**And the anchor had a hole in it.** `adr/0026` claims every price system anchors to *"the same
authored object"* while `adr/0023` enumerated a Hinterland as population, rent and wage — **no Goods
prices**, so the claim was false by inspection with a second anchor implied and homeless. `adr/0023`
now carries a **price per Good**, which also extends its own *"a port on the far edge buys a different
economy"* from immigration to trade.

**6. ~~`02 §4.3`'s worked example destroys money.~~ Settled — both mechanisms exist, and they are not
the same mechanism.** Its bakery drew `{ scope = "local", resource = "money", amount = 1 }` and output
no money anywhere, so one money per baking ceased to exist, which `adr/0024` forbids. The open
question was whether an authored money *cost* and `adr/0050`'s *implicit trade payment* could both
stand. **They can, because they are different shapes rather than two spellings of one.** An explicit
money term is a **transfer** — a tax, a wage, a subsidy — which names both ends and therefore balances
inside its own atomic Rule; `adr/0050`'s payment is a **purchase**, whose price is emergent and whose
counterparty is implied by the scope, so it has no syntax to conflict with. `adr/0050`'s *no syntax
for payment* survives re-reading: it was always a claim about purchases and never about money terms.

The example itself was wrong on a third count that closes it outright: **baking crosses no ownership
boundary**, every term being `local`, so there is nobody to pay and the money term should never have
been there. Removed. The loader now enforces the general rule as **refusal 4** — a Rule whose money
terms do not sum to zero is refused in either direction — so the document and the interpreter cannot
disagree about this again. See findings 24–27; the enforcement arrived with the Resource family,
which is what made it writable.

---

## What this slice deliberately does not do

- **Sweep Rules.** Slice 10, and gated on this one. A Zone Rule is a Sweep Rule.
- **Hot reload.** Slice 8. This slice loads a Ruleset once, at world creation. Slice 8 makes it
  swappable at a phase boundary, logs the transition, and is not done until the **Layer cadence and
  rates** load from a file — the concrete obligation that replaced `06`'s retired ordering claim.
- **Policies.** They are Sweep Rules.
- **The economy.** Money is conserved in Phase 2 of the roadmap and has no milestone yet.
- **A DSL.** Parked in `deferred.md` with a trigger.
