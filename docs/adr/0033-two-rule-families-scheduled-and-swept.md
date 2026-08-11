# Two Rule families — scheduled, and swept

**The Rule engine has two execution models, and which one a mechanism uses is a property of the mechanism, fixed at design time.** A **Bin Rule** is attached to one Building, is dispatched by the Event Wheel, and on failure **subscribes** to the Bin that was short instead of retrying on a timer. A **Sweep Rule** is attached to the city or a District, fires on a **time trigger**, walks a population, and never waits on anything. Zone Rules and Policies are the two instances of the second family.

The test that sorts a mechanism into one or the other:

> **Subscribe when waiting on a specific named thing. Poll when sweeping a population.**

## Why

### The two documents contradicted each other

`02 §4.1` said `rate = 10` means *"evaluated every tenth tick"* — a poll. [`05 §9`](../05-technical-architecture.md) said *"entities do not poll, mutators wake observers. A Rule that fails on flour registers interest in flour arriving in the Pool rather than retrying on a timer."* Both could not be true, and the discipline in `05 §9` is what [`0006`](0006-no-collection-grows-with-elapsed-time.md) and the Event Wheel exist to enforce.

The cost asymmetry decides it, and it is entirely in **failure**. A Rule that succeeds costs the same either way. A Rule that fails, under polling, pays its whole `on_fail` chain every `rate` Ticks for as long as the shortage lasts. So **polling makes the simulator most expensive precisely when the city is most broken** — a city-wide Materials shortage means every consuming Rule walking a four-deep chain forever, at the moment the Microscopic Cap is already saturated by the congestion that caused it. That is a coupling between gameplay state and simulation cost, which is the defect [`03 §3.9`](../03-agent-architecture.md) severed when it removed gridlock as a failure mode.

Polling also introduces a lag nobody authored. Each level of a fallback chain carries its own rate and its own phase offset — whichever Tick that Building happened to be built on — so an arriving Shipment reaches the thing that needed it after a delay determined by **construction order**. Deterministic, but unreadable, and the project's standing rule is that a constant is acceptable only when it is the same thing the player is shown.

### But subscription is wrong for a population

Applying the wheel everywhere was the obvious next move and it is wrong, for three reasons that were only visible once the Policy case was worked:

- **The wheel does not help.** An entity cannot know whether it matches a Policy's predicate without being evaluated, so every Household must wake to discover it does not qualify. The same evaluations happen, plus a wheel entry and a subscription apiece.
- **It distributes state that is conceptually singular.** Enacting a Policy would mutate every Household; repealing it would mutate them again; every immigrating Household would need arming with every active Policy at spawn. That last is a silent invariant — miss the hook and a Household is quietly exempt from a Policy the player enacted.
- **It is worse at the thing that motivated the commitment.** `CONTEXT` → Policy asserts *"a Policy is a Rule"* on the strength of the `Evidence` expansion test. A centralised sweep **is** that expansion. A distributed one can report only who fired, never who qualifies, without running the scan it was trying to avoid.

And the scan is cheap: eight Policies over ten thousand Households is ~10 integer comparisons per Tick amortised, and it stays affordable an order of magnitude up. The *"O(population) query"* objection was simply wrong at this scale.

### Zone Rules were the second model all along

`CONTEXT` had described Zone Rules as *"fire on a time trigger, sample a small random set of Lots, and test real simulation state"* and treated them as an anomaly the Rule engine tolerated. That is exactly the shape a Policy needs. Naming the family turns two exceptions into one model with two instances — the same move [`0031`](0031-one-resource-abstraction-and-depth-not-count.md) made when four exceptions to *"it's a Good"* turned out to be one axis.

## Consequences

- **Every Bin carries a wait list, and a subscription records which Bin stopped the Rule and in which direction.** The Bin drains from the head only while **the Bin's own level — or headroom — covers the head's requirement, derived at the drain** ([`0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)); a Rule that fires goes to the back. **The requirement is load-bearing, not an optimisation.** Waking every subscriber would push them all into Phase 3, where **small waiters would beat large ones on quantity**. Draining by requirement keeps contention out of Phase 3 in the chronic case, which is what turns shortage into a gradient rather than permanent starvation of a fixed set of Buildings. Polling had this defect too and hid it.

  > **⚠ AMENDED BY [`0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md), AND THIS BULLET WAS WRONG IN TWO INDEPENDENT PLACES.** Its original wording is worth keeping because both errors were copied *from* it: *"a subscription records the amount that was missing. The Bin drains from the head only while the arriving quantity covers the recorded shortfalls … Waking every subscriber would push them all into Phase 3, where `02 §1.1`'s sorted-key settle order picks the winner — so the head of the queue would lose every time and the list would be decorative."*
  >
  > **The budget was wrong.** *The arriving quantity* is one write's delta, so a requirement coarser than the granularity of supply is never reached however much accumulates — a consumer short of three, fed by arrivals of one, sleeps for ever beside a Bin filling to its ceiling. That is this ADR's *own* next bullet violated, and the check that would have caught it is this ADR's *own* second mitigation, which nothing implemented until `0063`.
  >
  > **The justification was a mis-citation.** There is no *sorted-key settle order*. `02 §1.1` says contention is resolved *"by a counter-based random shuffle"* and warns in its own voice that a sorted key applied to chronic shortage *"produces permanent starvation rather than a gradient"*; the code draws over `(seed, instance, tick, purpose)`, so a permanent winner is not possible and never was. The worry underneath it is real and survives as **size** bias rather than **identity** bias, which the level budget's spend-down bounds.
  >
  > **And the amount was never an entitlement.** Nothing downstream read it; it was a filter deciding whether to re-arm, and a woken Rule recomputes everything in `Check` regardless. So `0063` **deletes** `RuleInstance.shortfall` rather than re-deriving into it — the second time in this corpus that satisfying [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) has meant removing a number rather than ratifying one.
- **The failure mode inverts, and the new one is silent.** Polling is self-healing; subscription is not. A Bin written without draining its wait list leaves a Building asleep forever, with no error. Two mitigations, both required: **Bins are not public fields** — one write function, so *"every mutation site knows its observers"* is satisfied by there being exactly one — and a sweep invariant in `Borough.Tests`, *no Rule is asleep with all inputs satisfiable*, unaffordable per Tick and trivial at the end of a headless run.

  > **The second mitigation was specified here, restated in `02 §10` and [`plans/0008`](../../plans/0008-tick-and-replay.md), and built in none of them for the life of Phase 1.** It is now `Invariant.WaiterIsBlockedByTheBinItNames`, in the end-of-run tier, and it **fired on the committed golden session within minutes of being registered** — the drain defect [`0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md) corrects was live in shipped content, not waiting for `pool`. It is deliberately **narrower** than the sentence above in what it inspects and **stronger** in what it catches: it asks whether the Bin a waiter *named* still blocks it, which also catches a waiter resubscribed nowhere useful — a livelock *would this Rule fire* is blind to.
  >
  > **An obligation specified in three documents and built in none is how `HouseholdHomeExists` came to be reported by nothing.** This one paid for itself the day it existed, which is the argument for building the rest of them.
- **Wait lists are rebuilt, never saved.** On load, and on Ruleset hot reload, all wait lists are dropped and every Rule is woken with a stagger. Same reasoning as the travel-time matrix: derived state is a cache. It also means a wait list is never cross-version state.

  > **Amended by session C, and the bullet was half wrong.** It conflates two events that need opposite answers, and the code shipped the opposite of what it says: `RuleInstanceTable`'s `waiting_on`, `blocked`, `shortfall` and `queue_next` are all **`Saved`**. *(`shortfall` has since been deleted by [`0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md); the other three stand, and so does the argument, which was about the wait list being **history** rather than about any one column.)*
  >
  > **A load from a save restores wait lists and drain order exactly.** Dropping them is not a cache miss — this ADR's own third bullet says the requirement is *"load-bearing, not an optimisation"*, and drain order decides who eats next under sustained shortage. Re-arming everything with a stagger makes `run(N) → save → reload → run(M)` diverge from `run(N+M)`, which is **architecture invariant 6, the Factorio test**. The reason nobody caught it is that invariant 6's machinery does not exist yet, so the one check that would have fired has never run.
  >
  > **A Ruleset hot reload still drops and re-arms with the stagger**, and that half stands: the Rules themselves moved, so a subscription may name a Rule that no longer exists. *(The narrower hazard it also named — a recorded shortfall outliving the numbers it was computed under — is gone with the column, [`0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md).)* It is a deliberate, logged change to the world rather than lost state, and slice 8 carries it as one of its three degradations.
  >
  > The general shape is worth keeping: *derived state is a cache* is a claim about **provenance**, and it fails wherever the structure also carries **history** nothing else records. A wait list is a schedule and a fairness ledger at once.
- **Money moves to `local` scope.** `02 §4.2`'s example Rule drew money at `global`, which predates [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md). Left as-is, every money-consuming Rule in the city would subscribe to one Bin and every tax collection would wake ten thousand of them.
- **There is no proximity scope, and four scopes are final.** `local`, `pool`, `global`, and `map` — the last write-only. *"Bins in nearby Buildings"* was a category error: nearest-first selection always belongs to something that moves. See the `0009` note below.
- **A third category of quantity: the Readout.** Bins are what a Rule *spends*; Readouts are named read-only scalars it *consults* — never consumed, never conserved, never subscribable. The readable set is **declared simulation-side** and `Evidence` reads it, so **a Rule may read anything the player can see, and nothing else.**

  > **Amended.** This bullet originally bound the set to `Evidence` in the opposite direction, which `02 §4.1` inverted and calls an error rather than a ratchet. It also listed `income, experience, occupancy, composed fertility`; the declared set has **one** member, `occupancy`, and `experience` is struck entirely. That is a `LEGIBLE CAUSE` guarantee rather than a convenience: a Policy predicate reading a hidden internal would be one no player could explain being subject to. It also gives the Ruleset a stable named interface that fails loudly on an unknown Readout, where field paths would fail silently on a rename.
- **Predicates belong to Sweep Rules only.** A Bin Rule predicate would fail against an unsubscribable Readout and therefore poll, and a false predicate produces no `on_fail` chain — the sole way in the design for a Building to do nothing without saying why. Cases that appear to need one resolve into a labour input Bin, a different Building `kind`, or the scheduler. Corollary: **a derived apply count of zero is a success**, since nothing is missing and nothing is waited on.
- **Proportion is a derived apply count.** `amount` stays a fixed integer and `apply` may be computed from a quantity in scope, so *"15% of gross income"* needs no expression language, no parser, and no floats.
- **A mechanism never migrates between families for performance reasons.** The two differ in observable behaviour, not only in cost. [`05 §4`](../05-technical-architecture.md) states the general form: *a change is an optimisation if the State Hash is unchanged, and a design change otherwise.*

- **Phase 3 settle order changed with it.** Working the wait list against the phase table exposed that `02 §8` rule 5's *"stable key — entity id"* was both biased (the same Building wins every contested draw for the life of the city) and unstable (`05 §3` recycles row indices, so an unrelated demolition could change who wins). It is now a counter-based random shuffle, which the determinism rules already mandated the machinery for. A **tiebreak, not a priority** — sustained-shortage fairness stays in the wait list.

## What this cost

It argues directly against [`0018`](0018-prefer-off-the-shelf-infrastructure.md)'s standing bias, because a per-Bin wait list with deterministic drain order is bespoke infrastructure. The bias is overridden on the strength of the structure earning its keep twice: it is a scheduler **and** a diagnostic index. Under polling, *"why is this bakery not producing?"* is re-derived by re-running the chain; under subscription the Building is parked on a named Bin and the answer is a read. `02 §9` Evidence gains a data source it would otherwise have had to compute.

## What would trigger revisiting

- **The sweep invariant failing in the wild rather than in CI.** If asleep-with-satisfiable-inputs bugs survive to a build, the single-mutation-site constraint is not actually holding and needs enforcement by a lint rather than by structure.
- **The `global` treasury wait list showing up in a profile.** Public-sector salary Rules are a genuine global draw and all park on one list. Round-robin bounds the *wake* count by available money rather than by list length, which should be sufficient; if it is not, the answer is to give Districts their own budget Bins, not to reintroduce polling.
- **A Sweep Rule interval that has to shorten for gameplay reasons until the sweep is no longer cheap.** The fix is a finer stagger or a spatial partition, never re-homing the mechanism as a Bin Rule.
