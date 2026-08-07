# A fallback chain is a source ladder over one Bin

**Every link in an `on_fail` chain relieves the same Bin the head failed on** — refilling it if it was short, draining it if it was a full output. A link whose rescue arrives later than this Tick declares the Bin it `fills`. A chain that cannot satisfy this is a **malformed Ruleset and is refused at load**, by the same static walk that refuses a cycle.

Two things follow, and they are the reason this is a decision rather than a restatement. **A failed chain subscribes once, at its head**, so chain depth costs no subscriptions at all. And **the last link is a reporting terminal** — it records a condition and leaves the chain *failed*, because a terminal that succeeded would re-arm the head on `rate` and walk the chain for ever. `LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM` `FAST ITERATION`

## Why

### The substitution this economy has is *source* substitution

[`CONTEXT`](../../CONTEXT.md) → Rule states the case and states it narrowly: *"Fallback chaining is how supply-chain substitution works: **can't source locally → import**."* Same Good, different source. [`04 §1`](../04-economy-and-goods.md)'s Goods table is strictly linear —

| Good | From |
|---|---|
| Produce | Fertility |
| Food | Produce |
| Timber | Woodland |
| Materials | Timber |
| Consumer Goods | Materials |

— five Goods, **one input each**. There is no Good in this economy with two possible inputs, so *input* substitution (bake with rye when wheat runs out) has **no instance**. It was assumed into the design from the genre rather than derived from it, and the generality it demanded is what left the chain's subscription semantics undefined.

The ladder over one Bin is short and enumerable: **local Bin → District Pool → Shipment → import → terminal**, with `04 §1`'s *"importing is always the expensive fallback"* fixing the tail.

### Which makes one subscription correct, not merely cheap

[`02 §4.1`](../02-simulation-model.md) says a failed Rule *"subscribes to the specific Bin that was short"* — singular, written for a single Rule, and **nothing in the corpus said what a chain does**. The two candidate answers were one subscription at the head, or one per link with cancellation on wake.

Under the ladder the second is redundant rather than safer: every link terminates by relieving the head's Bin, so a single subscription already wakes on every rescue path. The machinery avoided is real — removal from a singly-linked intrusive list, which is what the core's collections are, or tolerated stale entries, which `02 §4.1` permits for a different reason and which would then be load-bearing rather than incidental. Neither is fatal. Both would have been bought for a case the Goods table does not contain.

### `fills` is what makes an asynchronous rescue honest

Apply the well-formedness rule to the corpus's own chain and the third link fails it. **`request_shipment` outputs nothing this Tick** — a Shipment is a movement, and it delivers into local flour some Ticks later — so a static check has nothing to match, and the link is indistinguishable from one that rescues nothing at all.

`fills = { scope, resource }` is the link asserting what its Shipment is *for*. One Ruleset field, still a pure load-time walk, still one subscription. It also draws the sync/async boundary explicitly rather than leaving it to be inferred from an empty output list.

### Refuse, do not warn

The `on_fail` graph is **static**: `on_fail` is a single Rule name in the Ruleset, so the graph has out-degree 1 and chains are paths through it. A cycle is therefore not a runtime hazard to guard against but a malformed Ruleset, and so is an unfilled link. Both are one walk at load.

That walk belongs to [`adr/0015`](0015-all-tuning-data-is-hot-reloadable.md), which already specifies the surface it reports on: *"A malformed Rule reports a file, a line, and a rule name, and the previous Ruleset stays live rather than the game dying."* A validator that warns is a validator whose refusals are advisory, and a Ruleset that loads with a broken chain produces a Building that fails silently — the outcome this whole section exists to prevent.

### The reporting terminal, and the defect the worked example carried

`02 §4.1`'s chain ended:

```
          on_fail → mark_input_starved   (records a reportable condition)
```

Recording a condition has no input that can be short, so under ordinary Rule semantics `mark_input_starved` **succeeds** — and *"a Rule that fires successfully re-arms on the Event Wheel at `+rate`."* A chronically starved bakery therefore walked all four links every `rate` Ticks for as long as the shortage lasted.

That is verbatim the cost [`adr/0033`](0033-two-rule-families-scheduled-and-swept.md) names as the reason subscription exists: *"a city-wide Materials shortage means every consuming Rule walking a four-deep chain forever, at the moment the Microscopic Cap is already saturated."* **The subscription model's own worked example reproduced the polling model's defect**, through its last link, in the one place nobody looks because the last link is the one that always works.

A reporting terminal is therefore a distinct kind: it records, and the chain stays failed, so the Building sleeps on its subscription.

### What this makes of chain depth

[`0003`](../../plans/0003-build-plan.md)'s gate board and [`0002`](../../plans/0002-open-questions.md) both named unbounded depth as slice 7's first blocker, citing two multipliers. **The ladder removes both.** A Policy cannot lengthen a chain — Policies are Sweep Rules and a Sweep Rule has no `on_fail`. Nine Resources cannot lengthen one either, because *"no power → run the backup generator on fuel"* is input substitution across Resources, which this decision refuses at load. And depth costs no subscriptions.

So depth is a `LEGIBLE CAUSE` question and nothing else, and under [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) it is typed **measurable**: real chain depths do not exist until a real Ruleset runs. `02 §9` already requires *"which fallback chain it walked and where it terminated"* to be reportable, so **the instrument is specified before the number is needed**.

**A draft of this decision published a cap of 5**, derived from the length of the source ladder above. That is R3's tripwire defect exactly — *a wire whose denominator is a guess fires on the guess* — and it is recorded because the rule that catches it was already written down in [`plans/0010`](../../plans/0010-s2-routing.md) and was not applied until the number was challenged. **Citing a rule is not the same as running it**, which is `adr/0044`'s closing finding arriving again one ADR later.

## Rejected

**One subscription per link, with cancellation on wake.** Correct under input substitution and redundant without it. Rejected on the Goods table rather than on cost, so that it reopens cleanly if the table gains a second input to any Good.

**A hard depth cap now.** No number is available. The structural bound is not a number — a chain cannot exceed the count of distinct sources for one Bin — and any figure published today would be an argument wearing a measurement's clothes.

**Warning rather than refusing.** A warned-about broken chain still loads, and the Building it belongs to then fails without a diagnostic, which is the failure mode `02 §4.1` bans predicates to avoid.

## Consequences

- **`02 §4.1` gains the chain semantics it never had**, and its worked example is corrected on two links: `request_shipment` declares what it fills, and `mark_input_starved` is marked a reporting terminal.
- **`CONTEXT` → Bin Rule gains the ladder, the single subscription and the terminal.** The vocabulary already said *can't source locally → import*; it now says that is the **only** shape.
- **The cost driver under subscription is shortage *churn*, not chain depth and not how broken the city is.** A chain is walked once on entry into shortage and a chronically starved District walks nothing. This sharpens `adr/0033`: under polling the simulator is most expensive when the city is most **broken**, and under subscription when it is most **unstable**.
- **`adr/0015`'s session inherits two named refusals** — the cycle check and the `fills` check — so it is no longer *"never grilled at all"* but a session with a concrete first item. **Slice 7 is therefore gated on `adr/0015` as well as on `02 §4`**, which is the opposite of what the board assumed.
- **Slice 7 ships two counters**: Rule evaluations per Tick, and walked chain depth per `02 §9`. The tripwire is stated over measured evaluation cost against the Tick budget — *chain walking fits while fewer than N evaluations occur per Tick* — and never over a depth.
- **`fills` is Ruleset data**, so the balance surface stays enumerable in `adr/0015`'s sense: what a chain rescues is a file listing rather than an inference from an empty output list.

## What would trigger revisiting

- **A Good with two possible inputs.** Recycled Consumer Goods into Materials is the likely first and is a Phase 2+ mechanism. The refill law refuses it at load, which is the **right** failure — a named error rather than a silent miscount — and the retrofit is additive, since every Ruleset written under the law stays valid under a looser one. This is the trigger this ADR most expects to fire.
- **A rescue that legitimately relieves a *different* Bin.** No instance could be constructed from the current corpus, but a Service mechanic that substitutes one provision for another would be one. Note the escape that already exists: a Sweep Rule has no `on_fail` and no such constraint.
- **A p99 walked chain depth that makes the Building diagnostic unreadable.** Measurable from slice 7, with the instrument already required by `02 §9`.
- **Rule evaluations per Tick exceeding the routing-style budget share.** Same inversion, same discipline: publish the break-even, never the multiple over a guessed denominator.
- **A terminal that genuinely needs to succeed** — a fallback whose last resort is a real transformation rather than a report. It would re-open the polling exposure and needs the re-arm cost priced, not assumed away.
