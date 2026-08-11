# A shopping attempt is a Trip, and a Household tries one provider per occasion

**Step 4 of `04 §6` is a **Trip**. A Household travels to a shop on its Provider List, and finding the
shelf empty is a **transaction** outcome recorded on the Household — a consecutive-failed-occasions
count and a refusal reason — never a fifth `Trip Fate`, because the journey succeeded. **A failed
occasion costs one Trip, not `N`**: the Household goes home, its Need degrades, and it tries a
different entry at the *next* occasion. Which entry is decided by a **cursor** that advances on failure
and resets on success, so a provider that failed is skipped for exactly **one occasion** — a duration
whose value is *derived from the mechanism* rather than chosen.**

Settled by session **N**, [`plans/0018`](../../plans/0018-session-n-the-bin-the-pool-and-the-economy.md)
task 5, on `04 §6` — the chain the whole economy exists to produce, and **never grilled**. Typed under
[`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) by splitting
the claim the corpus had welded: *the failure must be a real per-Household event rather than derived
from a stock level* is **arguable** and is taken here; *a real attempt per Household is affordable at
1M* is **measurable**, is nobody's row yet, and is routed to `0002` §B.

`LEGIBLE CAUSE` `BOUNDED KNOWLEDGE` `EMERGENCE`

---

## Why

### The step the corpus said could not be typed was two claims welded together

`plans/0018`'s typing pass recorded, honestly, that it *"could not type step 4 at all"*. The reason is
that step 4 asserts two different kinds of thing at once. **That a Household must actually attempt the
purchase and actually fail** is a design commitment — no measurement decides it, and the corpus has
effectively already taken it, since [`adr/0005`](0005-two-fidelity-tiers.md) forbids cohorts and the
whole Evidence chain is what it buys. **That the city can afford one such attempt per Household per
occasion at 1M** is a measurement, and it is the one that could kill the first.

Welded, the pair is untypeable. Split, one is settled in a sitting and the other gets a machine.

### If the attempt is not a Trip, coverage-by-radius returns for Goods

[`adr/0032`](0032-services-are-delivered-by-trips-not-by-coverage.md) settled the same question one
half at a time: *a Service reaches people by someone making a journey*, with education, health and
recreation **Attended** — the Household travels. It demoted the service coverage Map Layer from
**mechanism** to **overlay** on the grounds that a decaying radius sampled by a Building is SimCity 4's
model and answers no questions.

**Goods have the stronger version of the same claim**, because a grocery's Food Bin is a physical stock
in a specific place, and `adr/0013` already makes Goods move by Shipment between Districts. A Household
that reaches across the city into a Bin without moving is coverage-by-radius arriving through the back
door for the one Resource family that unambiguously has to be *fetched* — and the Evidence chain would
then narrate *"the Households that walked in and found nothing"* about an event that never happened.
That is `LEGIBLE CAUSE` inverted: a legible story about a fiction.

**The consequence is accepted rather than avoided.** Every Household's shopping is now Trip generation,
so `04 §6` is a **load on milestone 5b** rather than an economy question with a cost of its own — and
5b's subsystem is already the one that does not fit [`0013`](../../plans/0013-tick-budget.md)'s budget.
Putting the cost where it actually falls is worth more than a smaller number in the wrong column.

### Sequential consultation makes the failure path amplify itself

`04 §6` step 5 reads *"the Household consults the rest of its short, sticky Provider List"*. Once step 4
is a Trip, the ambiguity in *consults* becomes load-bearing: if it means travelling, one shortage costs
a Household up to `N` Trips, and the city's shopping traffic is multiplied by the Provider List's length
**at exactly the moment the city is already failing**.

The loop that produces is real and runs the wrong way:

```
shortage ─▶ more shopping Trips ─▶ congestion ─▶ Trip failures
   ▲                                                  │
   └──────── abandonment ◀── Failure Pressure ◀────────┘
```

A shortage *should* hurt in more than one currency — that is `EMERGENCE` working. But an amplifier of
`N` on the failure path, where `N` is a Ruleset constant nobody has chosen, is a feedback loop tuned by
accident. **One Trip per occasion bounds it at 1 regardless of how bad the shortage is**, and moves the
bite into **time**, which is the quantity step 6 already uses: the Need degrades while the Household
works through the places it knows.

**The lag is realism and is accepted as such.** A Household with three known groceries takes three
occasions to discover its District is dry, so step 6 begins later and the player sees the consequence
later. Nobody drives to eight shops in an afternoon, and [`adr/0017`](0017-agents-satisfice-they-never-optimise.md)
is the decision that says so.

### The failure is a transaction outcome, and `Trip Fate` is about journeys

`CONTEXT.md` enumerates four Trip Fates — *completed*, *no route found*, *exceeded commute budget*,
*stranded*. Every one is a property of the **journey**, and `04 §6` insists step 4's failure is *"a
recorded failure, not a silent one"* with nowhere recorded to put it.

A fifth Fate would be a category error: the Trip **completed**. What failed is the purchase. So the
outcome lives on the Household as a **consecutive-failed-occasions count** and a **refusal reason** —
which is not new state invented here, because S0a's own Household field list already carries *"failed
attempt counter, refusal reason"*. The footprint model has been assuming this for longer than the
design has said it. The reason is a field rather than a boolean because `02 §5.9` already needs more
than *empty*: a provider can fail a Household **on price or on distance** as well as on stock.

### Deprioritisation is a duration, and the cursor is the one whose value is derived

The obvious mechanism is a *last failed at* timestamp per Provider List entry, with a decay. **It was
argued for and it is not refused on `adr/0053`'s grounds**, which is worth recording because the
opposite was claimed first in the sitting and was wrong. `adr/0053` forbids a **tally**, because
counting failure events inverts severity; `now − lastFailed` is a **duration**, it resets on success,
and nothing accrues. `adr/0053` endorses that shape.

It is refused on one narrow ground: **its window has to be picked.** The natural derivation — *skip it
for as long as the shop takes to restock* — is the provider's own Rule `rate`, and `BOUNDED KNOWLEDGE`
is precisely the rule that forbids a Household knowing it. So the decay would be an unset, hash-bearing
`adr/0052` number with no ratifier but a playtest.

**The cursor is that scheme with its duration derived instead of chosen.** Advance on failure, reset on
success: a provider that failed is skipped for exactly **one occasion** and then tried again. That is a
duration, it resets on success, and its value falls out of the mechanism rather than out of a file.
Resetting is what keeps the list **sticky** in `adr/0017`'s sense — the Household returns to its usual
shop the moment one works — where a free-running round-robin would quietly delete stickiness by
spreading custom evenly across everything it knows.

This is the fourth time the cheapest way to satisfy `adr/0052` has been to find the derivation, after
the arming stagger, `adr/0059`'s deletion and `adr/0063`'s.

---

## Consequences

**`04 §6` is owed a correction on steps 4 and 5**, filed to [`0012`](../../plans/0012-corpus-audit.md):
step 4 must say it is a Trip, and step 5 must say *at the next occasion* rather than reading as a
sequence within one. The document owns the chain and this ADR owns the mechanism, per `adr/0042`.

**`adr/0032` is extended rather than amended.** It settled the Attended/Dispatched/Networked split for
**Services** and its argument always covered Goods; this states the half it did not name. Nothing in it
changes.

**A new multiplicand exists and nobody has it: shopping occasions per Household per Day.** It is what
turns this decision into a number in `0013`, it belongs to Trip generation rather than to the economy,
and it is routed to `0002` §B with milestone **5b** as its machine. `0013` gains **no row** here,
because the cost is 5b's row and not a second one.

**The Household gains three small fields and no collection**: a cursor into its own Provider List, a
consecutive-failed-occasions count, and a refusal reason. All bounded, all `unmanaged`, none of them a
per-entity object.

**The Provider List's cap loses its last extra job.** [`adr/0066`](0066-the-provider-list-is-an-intrusive-index-list-and-its-ruleset-length-is-a-cap-rather-than-an-allocation.md)
stopped it being a memory parameter; this stops it being a traffic multiplier. What remains is a
**legibility** number — how many places a player should expect a Household to try before it gives up —
which is a feel decision selected by the first playable build, and is now a `0002` §D2 row.

**Steps 6 and 7 are unblocked but not settled here.** The counter is what Need degradation and Departure
consume, and the **give-up bound** — `02 §5.2`'s cycles-to-Departure, unnamed anywhere — is the adjacent
unset number and belongs with task 2's missing Pool sink.

### Rejected: deriving the failure from a stock level

`04 §6` already refuses it in its own words — *"a global happiness number computed from a global stock
level would produce the same aggregate and answer no questions at all"* — and it is restated here
because it is what every implementation shortcut on this path collapses into.

### Rejected: a fifth `Trip Fate`

The Trip completed. Recording a transaction failure as a journey outcome would make `Trip Fate`
un-analysable for the thing it exists for, which is whether the **network** served the city.

### Rejected: a per-provider timestamp with a decay window

Not on `adr/0053`, which endorses it, and no longer on memory, since `adr/0066` makes per-entry state
cost per entry that exists. Refused because its window must be chosen while the cursor's is derived. If
the cursor's single occasion proves too short, this is the mechanism to return to — with the number as
the thing being decided rather than as a detail.

### Rejected: a free-running round-robin

Cheaper still — no reset — and it deletes stickiness, which is `adr/0017`'s central claim about how
these actors choose. A Household with no preferred grocery is a Household that is optimising.

---

## What would trigger revisiting

- **A restock period materially longer than the shopping cadence.** Then one occasion is too short, the
  Household burns a Trip returning to a shelf that is still empty, and the wasted traffic this decision
  removed comes back. Both quantities are Ruleset numbers, so it is checkable content-side.
- **Shopping Trips dominating Trip generation.** If the measured occasions-per-Household rate makes
  shopping the majority of all Trips, the affordability half of step 4 has failed and the design must
  choose between the Evidence chain and the budget. That is the outcome this ADR's typing exists to
  make visible rather than to prevent.
- **A Household needing to know *why* a provider failed before choosing the next.** The cursor is blind
  by construction; if price-driven and stock-driven failures must be distinguished *in the choice*
  rather than only in the record, the cursor is insufficient and the timestamp returns.
- **Provider List occupancy sitting at the cap.** `adr/0066`'s reversal condition, and it reaches here
  too: at full occupancy the amplification argument matters more, not less.
