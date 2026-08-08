# 0011 — Slice 7: the Rule engine, Bins and Bin Rules

> Slice 7 of [`0003-build-plan.md`](0003-build-plan.md). Roadmap **milestone 3a**.
> Governed by [`02 §4`](../docs/02-simulation-model.md),
> [`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md),
> [`adr/0045`](../docs/adr/0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md),
> [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md),
> [`adr/0015`](../docs/adr/0015-all-tuning-data-is-hot-reloadable.md).

**A Bin Rule is an atomic transformation over Bins, declared in the Ruleset and interpreted by the
binary.** This slice builds Bins and their wait lists, the Ruleset loader and its three refusals,
Rule evaluation with atomicity and apply count, and `on_fail` chains. It is the first slice in which
the simulation *does* something — until now the Tick has been a skeleton with a hash on the end.

**The risk it retires:** that the Rule engine is a design nobody has tried to write down as code, and
that `02 §4`'s two execution models are one model with an exception bolted on.

---

## Status

**Not started.** The gate cleared on 2026-08-07 (session A → `adr/0048`).

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
`on_fail` chains, the loader and all three refusals are all reachable with an empty `Wake`, because a
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

### 3. The Ruleset loader and the three refusals

In `Borough.Formats`, per `adr/0048`. Tomlyn in, ids and integers out.

Refusals, one walk at load, each naming a file, a line and a rule name:

1. **`on_fail` cycle** — the graph has out-degree 1, so chains are paths and a cycle is a malformed
   Ruleset rather than a runtime hazard.
2. **`fills`** — every link relieves the same Bin the head failed on; a link whose rescue arrives
   later declares what it fills. A chain that cannot satisfy this is refused.
3. **Unquoted decimal** — refused by name, never coerced, because a library `double` must not reach a
   tuning number.

The **previous Ruleset stays live** on a refusal. That is `adr/0015`'s error surface and it is part of
the deliverable, not polish.

### 4. Quoted decimal → Q16.16

Our own routine, in `Borough.Formats`. `"0.15"` → `Ratio`. Exact, integer-only, invariant-culture,
with a test that sets a non-invariant culture and proves the result unmoved.

Under `adr/0043` the claim *"Tomlyn's integer parsing is culture-insensitive"* is **measurable**, and
the machine is a unit test. Write it rather than assume it.

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

### 7. The Readout set, declared simulation-side

A named read-only scalar an entity exposes. **The readable set is declared in the simulation**, and
every declared Readout is inspectable — so no Rule can act on a quantity the player cannot inspect,
by construction rather than by reference.

The converse does not hold: the inspectable surface is much larger. The test for admitting one is
*does a Rule read it, or is it only displayed?* Display-only is a display, never a Readout.

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

**2. How many Bins does a Building have, and are they declared per Building kind?** `02 §4` does not
say. It bears on the table layout, which is on the *expensive to retrofit* list.

**3. The District Pool does not exist.** The `pool` scope *"requires road connectivity"*, and there
are no roads, no Districts and no connectivity until Phase 2. Slice 7 probably ships `local`,
`global` and `map`, and declares `pool` as a **named hole that throws** — slice 6's pattern, which
exists precisely because a placeholder returning zero is a value somebody will read and tune around.

**4. `04 §1`'s Goods are five and the Resources are nine.** Which of them slice 7's proving Ruleset
uses is a content decision, and content is meant to follow the Ruleset work rather than lead it.

---

## What this slice deliberately does not do

- **Sweep Rules.** Slice 10, and gated on this one. A Zone Rule is a Sweep Rule.
- **Hot reload.** Slice 8. This slice loads a Ruleset once, at world creation. Slice 8 makes it
  swappable at a phase boundary, logs the transition, and is not done until the **Layer cadence and
  rates** load from a file — the concrete obligation that replaced `06`'s retired ordering claim.
- **Policies.** They are Sweep Rules.
- **The economy.** Money is conserved in Phase 2 of the roadmap and has no milestone yet.
- **A DSL.** Parked in `deferred.md` with a trigger.
