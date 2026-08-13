# A reach failure is counted on the Citizen, and a stock failure is not remembered at all

**A candidate refused because the Road Graph cannot deliver it inside the Commute Budget increments a
saved count on the Citizen; the count resets on success and is read by nothing yet.** A candidate
refused because it is *full* leaves no memory, because detecting that costs one array read and
remembering it costs more than repeating it. **The two failures are different mechanisms and the corpus
has been treating them as one.**

`LEGIBLE CAUSE` `BOUNDED KNOWLEDGE` `SOLVE THE ACTUAL PROBLEM`

Settled by a sitting on 2026-08-13, run beside [`plans/0003`](../../plans/0003-build-plan.md) queue item 6,
against the rule [`adr/0047`](0047-routing-never-keys-on-the-district.md) filed and
[`adr/0017`](0017-agents-satisfice-they-never-optimise.md) recorded and neither settled: *a failed Trip
must demote the option that produced it.*

## Why

### The debt was one sentence covering two failures, and half of it was discharged three days later

`adr/0047` names the defect: `adr/0017` re-evaluates *"immediately on a failed Trip"* against the same
information, which still says the same wrong thing, so a Household can choose, fail, re-evaluate and
**choose the same unreachable option for ever**. It files the repair *"owed to `adr/0017`"*, and
`adr/0017:44` records it faithfully — *"what that memory **is** — a per-Household demotion, a cooldown, or
Habit's own weight moving — is unsettled… **Recorded here rather than invented**."*

**[`adr/0067`](0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md)
then answered half of it on 2026-08-10, three days after the debt was filed, and nothing connected the
two.** Its subject is *"finding the shelf **empty**"* — a **stock** failure — and its answer is a
consecutive-failed-occasions count plus a **cursor** that advances on failure and resets on success, so a
failed provider is skipped for exactly one occasion, *"a duration whose value is derived from the
mechanism rather than chosen."*

**The tell that the debt was always about the other failure is in its own candidate list.** `adr/0017`
offers *a demotion, a cooldown, or Habit's weight moving* and does **not** offer a cursor — because
whoever wrote it was thinking about an **unreachable** option, correctly, and `adr/0067` was written about
an **empty** one. Two ADRs, three days apart, each right, neither aware the other existed.

***An answer that arrives after a debt is filed does not find its way to the ledger*** — which is
[`plans/0012`](../../plans/0012-corpus-audit.md) **Cause 2** running in the direction the sweep does not
look: the usual failure is a decision that never propagates, and this is a **discharge** that never
propagated.

### The split is not a proposal — the build already has it, in one method

`EmploymentEngine.TryEmploy` rejects a candidate two ways and treats them differently already:

| | Detected by | Costs | Counted |
|---|---|---|---|
| **Stock** — no free slot | `!_world.HasJob(building)` | one array read | no, `continue`s silently |
| **Reach** — beyond the Budget | `WalkRouting.Cost(...)` then `!trips.WithinBudget(cost)` | **a full Dijkstra**, ~32.5 µs in a real world | yes, `_tickBeyond++` |

**A memory buys a skipped search.** That is worth having where detection costs 32.5 µs and worth nothing
where it costs a load, so the asymmetry in the table is the whole of the decision about which failure gets
remembered. *The code reached this split before any document argued it*, which is
[`adr/0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) paying out in the
useful direction for once: opening the mechanism settled a question three documents had left open.

### `adr/0047`'s loop is not reachable today, and the reason is one term in a hash key

The candidate draw is keyed on `(citizen id, look ordinal, **tick**, PurposeTag.JobCandidate)`. The tick is
in the key, so **every occasion draws a different candidate set** and the literal *choose the same
unreachable option for ever* cannot happen. What recurs is not the choice but the **work**: at 5b-bis's
100,000-Tick run, `jobs beyond budget` never reaches zero and **4,561 of 10,000 hold a job against 9,608
posts declared**, so roughly 5,400 Citizens re-run up to three Dijkstras every time they are sampled, for
ever, to be told again what the graph has not changed its mind about.

**So `adr/0047` diagnosed the mechanism correctly and named the symptom that arrives last.** Its loop needs
a **deterministic** re-evaluation against a travel-time matrix, which is milestone 5c and unbuilt. What is
live is the same defect wearing a different cost: a bill rather than a loop. The ADR could not have made
this distinction, because `EmploymentEngine` did not exist when it was written.

### Why a count, and not the three mechanisms the debt proposed

**A cooldown is refused on *shape*, not on its number.** A duration models *wait and it will get better*,
and nothing here gets better with elapsed time — a shelf refills on its own, **a road does not move on its
own**. What could change a reach verdict is a road edit, a new Building, or the Citizen moving house, and
not one of those is a clock. This is the same objection that makes `adr/0067`'s *skipped for exactly one
occasion* right for stock and wrong here.

**Widening the search box is refused on geometry, and it is provably useless.** 5b-bis derives the box from
the Commute Budget precisely so there is no second number that can contradict the first, and the box is a
**straight-line over-approximation** of a walk on the Road Graph. Everything acceptable is already inside
it; everything a wider box adds is further away than things already refused. *A wider box returns strictly
more candidates, all of which `WithinBudget` refuses.*

**Habit's weight moving is refused as a category error.** A Habit is a route between two places
([`adr/0060`](0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md)); a
job search has no incumbent route and no variants to switch between, which is precisely
[`adr/0061`](0061-a-diversion-rejoins-by-local-descent-and-a-rejoin-is-never-a-search.md)'s mechanism and
precisely why it does not transplant.

What is left is the count, and `adr/0061` is its direct precedent: a failed rejoin is **counted rather than
corrected**, and session D deleted an elaborate strand-versus-congestion classification the moment the
counter made it unnecessary. `adr/0059`'s direction, and this is its fifth sighting.

### The count is history, and history is the one thing recomputation cannot produce

The obvious objection is that the count is a diagnostic and this corpus has just refused to store one:
session P settled that **a vacant Lot's reason is recomputed on the click and never recorded**, and
*"everything I can reach is too far"* is equally recomputable on a cold path
([`adr/0036`](0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md)).

**That objection is correct and it kills the diagnostic justification, not the decision.** Recomputation
answers *why is this Citizen jobless now*. It cannot answer *has this Citizen been failing persistently*,
and persistence is the entire signal — it is what separates a Citizen who was unlucky this occasion from
one who is **structurally excluded**, which is a different fact about the city and the one the player needs.

⚠ **`02 §9` is already violated and this is the narrowest thing that repairs it.** Its general rule is that
*"every aggregate figure must be able to name its constituents"*, and `jobs beyond budget` is an aggregate
with no entity reference anywhere behind it. 5b-bis could report *distance rather than supply is what
separates them* **only in aggregate**, and could not name one Citizen it was true of.

## What this decision does not do

- **It does not stop the search recurring.** The count is a memory and nothing reads it, so the pass
  costs what it cost. Saying so plainly is the point — *a byte on a row does not repair a bill*, and
  pretending otherwise is how a cost stops being anybody's row.
  ⚠ **The bill was already filed and this ADR sharpens it rather than routing it**, which is worth
  recording because the sitting set out to route it: [`plans/0013`](../../plans/0013-tick-budget.md)
  has held the job-assignment pass since 5b-bis task 4 — **7.0 ms a pass at 100,000 Citizens in steady
  state, ~70 ms in the pass Tick at 1M against a 15.6 ms budget, 4.5×**, and 31× at cold start. What
  this decision changes is **one sentence** in it: *"a pass whose cost falls as the city settles"* is
  right about the peak and wrong about the floor, because 5b-bis task 8's 100,000-Tick run has
  `jobs beyond budget` **never reaching zero**. ***A cost that decays and a cost that decays to a floor
  are different rows***, and the floor is exactly the population this ADR makes countable. Amended
  there under [`adr/0073`](0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md), with a re-take owed at
  ≥100,000 Ticks and at each of `adr/0095`'s three rungs.
- **It does not settle the provider case**, which is **void as posed** under
  [`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md): the Provider List is unbuilt with
  no milestone, the market term `Scope.Pool` is a named hole that throws, no Ruleset declares a shop, and
  the matrix is 5c's. `adr/0017`'s note is narrowed rather than discharged.
- **It does not choose what the count drives.** Its consumer is **Departure**, `06` milestone **9a**, whose
  own stated risk is *"growth is driven by a global demand scalar — the Pool with per-Household reasons
  **is** the demand signal"*. Designing the threshold now would be choosing a parameter for a consumer that
  does not exist, which is `adr/0070`'s forbidden move, and it is why this ADR opens **no
  [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) number**.

## Rejected

**Storing which option failed, stamped with the Road Graph Epoch.** Genuinely attractive: a reach failure
stays true until the road changes, so the memory is an **invalidation** rather than a clock, on
[`adr/0012`](0012-routing-intent-lives-in-the-agent.md)'s contract and
[`adr/0083`](0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md)'s
checked-at-use, both already built on 5a's per-Segment Epoch — and it would actually cut the bill, which
the count does not. **It is refused for now on scope rather than on merit**: at fixed width it remembers
exactly *one* option, and a set needs a bound under
[`adr/0006`](0006-no-collection-grows-with-elapsed-time.md); and the candidate draw is randomised per Tick,
so the one option remembered is unlikely to be the one drawn next. **It becomes the right answer the moment
choice becomes deterministic**, which is 5c, and it is recorded here so that arriving at it later is a
decision rather than a rediscovery.

**A shared table of unreachable pairs.** `adr/0017` rejects a shared global ranking as
[`adr/0005`](0005-two-fidelity-tiers.md) arriving through the back door, and this would be that. **But the
distinction worth writing down is finer than the prohibition**: `adr/0017` forbids sharing a **choice**, not
a **measurement** — the route cache is a shared reachability fact and `adr/0012` permits it, keyed on the
pair. So a shared *cost* is legitimate and is 5c's; a shared *memory of having tried* is not, because who
has tried what is the history that makes two identical Citizens differ.

**Remembering the stock failure too, for symmetry.** Symmetry is not a reason. Detection costs one load, so
the memory would cost more than the thing it saves, and `adr/0067` has already given the stock case the
mechanism it wants in the setting where it matters.

## Consequences

- **`CitizenTable` gains one saved column** — a reach-failure count, `(saved AND hashed)` under
  `adr/0003`'s per-field declaration. It **moves the State Hash and re-records all three golden
  baselines**, so it is a commit of its own and must not land beside `0003` queue items 6 or 7.
- **It resets on employment and on nothing else.** In particular it does **not** reset on a Ruleset
  reload: the count is the Citizen's history, not a cache of a Ruleset value, which is the distinction
  `adr/0064` and `adr/0065` were both written around.
- **A width is owed and is deliberately not chosen here.** It must saturate rather than wrap —
  `adr/0003`'s no-unbounded-magnitude rule applies to a counter as much as to a quantity — and the width
  follows from 9a's threshold, which does not exist.
- **`adr/0017`'s owed-rule note is narrowed to reachability**, and records that `adr/0067` settled stock.
- **`adr/0067` gains a consequence**: its cursor covers an **empty** shelf and not an **unreachable** one,
  and *skipped for exactly one occasion* is derived from a shelf refilling on its own.
- **`02 §9`'s Citizen row acquires its first honest constituent.** The aggregate can now name somebody.
- **Milestone 9a inherits a signal it did not know was coming**, with the reason already accumulated by
  the time Departure is built rather than needing a migration.

## What would trigger revisiting

- **5c making the choice deterministic.** The moment a matrix replaces the per-Tick randomised draw,
  `adr/0047`'s loop becomes reachable for real and a count that drives nothing stops being sufficient. The
  Epoch-stamped option in *Rejected* is the successor and is written down for that day.
- **The recurring search bill turning out to bind.** If `plans/0013`'s row makes the repeated Dijkstra
  material, the repair is the Epoch stamp — which cuts work — and not a bigger count, which does not.
- **A count that never leaves zero, or never stops climbing.** Both refute it, in opposite directions:
  the first means the Budget is not binding and 5b-bis's reading was a fixture artefact; the second means
  nothing consumes the signal and 9a has not arrived.
- **Departure being designed against something else.** If 9a reads a different signal, this column has no
  consumer and should be deleted rather than kept for tidiness — *a column whose only reader was a
  prediction is a collection of one*.
