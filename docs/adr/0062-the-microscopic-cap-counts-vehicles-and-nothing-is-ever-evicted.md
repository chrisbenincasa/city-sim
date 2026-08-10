# The Microscopic Cap counts Vehicles, and nothing is ever evicted

**The Cap is a ceiling on simulated *Vehicles*, not on Segments.** A Lane's per-Tick cost is one pass over its queue, so the bill scales with what a Segment holds and not with how many Segments there are; a six-lane arterial and a residential street are not one slot each.

**Nothing is ever evicted to make room.** A Segment with a non-empty queue does not demote — `03 §4` invariant 3, unchanged — so a full Cap **refuses** rather than displaces. Admission is ordered: **force-promotion outranks stress-promotion**, ties break on Stress and then on Segment id. When even a force-promotion cannot be admitted, the boundary **refuses entry** to arriving vehicles, which is what `03 §3.9` already specified.

**The Cap's *value* is not set here and cannot be.** This decision is about what the number counts and what happens when it binds.

`HONEST DEGRADATION` `SOLVE THE ACTUAL PROBLEM` `PLAYER GOVERNS`

## Why

### Denominated in Segments, the Cap is a proxy for a cost whose variance it cannot see

`03 §3.9` writes the Cap as *"a ceiling on how many **segments** may be Microscopic at once"*, derived from Segment count or map size. But the thing it protects is a per-Tick cost, and under [`adr/0016`](0016-the-lane-is-the-entity-not-the-car.md) that cost is a linear pass over each Lane's queue. **Occupancy varies by more than an order of magnitude between Segments, and it varies most in exactly the direction that binds** — a Segment holds the most Vehicles when it is jammed, and it is promoted because it is jammed. A Segment-denominated Cap is therefore least accurate precisely when it is doing its job.

This is the family [`adr/0053`](0053-failure-pressure-is-a-duration-not-a-tally.md) and [`adr/0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md) already named: **state the quantity in the unit the thing actually paces in.** S0b found it for a Zone Rule's `sample`, and the lesson recorded there is the one that applies here — *a ratifier that runs at one city size cannot catch a number that is absolute*. The Cap has never been ratified at all, so nothing has had the opportunity to miss it. Repairing the unit now costs a sentence; repairing it after a balance pass costs the balance pass.

**Nothing in `§3.9`'s argument depends on the unit.** That argument is about *who sets the number* — it must be world configuration, identical on every machine, never host-tunable, or the same Input Log produces two different cities. Counting Vehicles instead of Segments leaves every word of it standing.

### Eviction is refused on information loss, and not on any claim about the VDF

A tempting argument says a full Cap should shed the *newest* stress because the VDF is least wrong at the onset of congestion and worst on an established queue. **That argument was made in the sitting and withdrawn.** `adr/0007`'s claim is about **saturation** — the VDF diverges above capacity — and the version above is about **age**, which is a different axis, is supported by nothing, and is measurable: divergence against Ticks since the threshold crossing, on `03 §5.1`'s queueing scenario. Under [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) it may not settle this, and it does not.

The argument that does is structural. **A Segment that is already Microscopic holds state that cannot be reconstructed** — queue position, headway, an in-progress Switch Lane traversal, which `adr/0016` names as exactly what is at risk on demotion. **A Segment newly crossing the threshold holds none**, because it had no queue a Tick ago. Refusing the second destroys nothing; evicting the first destroys something no rule can rebuild, and destroys it at the moment it would have been observed — `03 §4` invariant 3 already says a queued Segment does not demote *because hysteresis is deleted at exactly that instant*, and eviction is that deletion with a resource limit as its motive.

So the Cap never displaces. It refuses.

### Force-promotion outranks stress-promotion because they are different kinds of claim

Stress-promotion buys **accuracy**: a Segment gets travel times from simulation rather than from a curve. Force-promotion buys **correctness**: a Statistical Segment computes arrival as `distance / speed` and is structurally blind to a full downstream neighbour, so it delivers Vehicles into a road with no room — and **spillback is one of `03 §5.1`'s three acceptance criteria**, the tier's own definition of doing its job.

A resource limit may cost the simulation accuracy. It must not be allowed to cost it a phenomenon the tier exists to produce. That ordering is arguable, it is taken here, and it is **conditional**: whether force-promotion is needed at all is measurable and is routed below.

### Refusal at the boundary is not a new mechanism

When a force-promotion cannot be admitted, `03 §3.9` already says what happens: *"the fallback is a virtual queue held at the boundary — vehicles are refused entry rather than admitted into a full segment. Refusing is a smaller lie than over-filling."* A full Segment refusing entry is what a full Segment does; the upstream Segment is then itself blocked, which is spillback expressed as refusal rather than as simulation.

## What is argued here and what is routed

`03 §5.1`'s acceptance suite is the arbiter of everything in the second table — *three small deterministic scenarios, headless and fast*, already specified and not yet built. **This is milestone 6's work and not a spike**, and the suite is being used to admit or refuse a **representation** rather than a scenario, which is a new use of `§5.1`'s own admission rule.

| Settled here | Type |
|---|---|
| The Cap counts Vehicles | **arguable** |
| Nothing is evicted; a full Cap refuses | **arguable** — on unreconstructible state |
| Force-promotion outranks stress-promotion | **arguable** — correctness outranks accuracy |
| At the Cap, the boundary refuses entry | **arguable** — `§3.9`'s own fallback |
| A third representation is refused | **arguable** — `adr/0016`'s standing warning |

| Routed, and no document may cite it as decided | The refuting number | The machine |
|---|---|---|
| **Is force-promotion needed at all?** `03 §3.3`'s open decision | does the upstream Segment block with the mechanism **disabled**? | `§5.1` **spillback** scenario |
| Is a lossy demotion under Cap pressure safe? | does the jam persist after the volume that caused it is gone? | `§5.1` **hysteresis** scenario |
| Does a virtual queue qualify as a representation? | does it pass **all three**? | the whole suite |
| Does VDF error grow with queue **age**? | divergence against Ticks since threshold crossing | `§5.1` **queueing** scenario |
| **The Cap's value** | Vehicles affordable in 15.6 ms, against Vehicles a real city stresses | **S5**, and `06` **5b** |

## Rejected

**The virtual queue as a third representation.** A Segment demoted under Cap pressure could keep a Vehicle count without Lane positions — the jam survives, the slot frees, and eviction becomes possible without deleting hysteresis. It is genuinely attractive and it is refused **and named**, so that reaching for it later is a decision rather than a drift. [`adr/0016`](0016-the-lane-is-the-entity-not-the-car.md)'s revisit trigger says it in terms: if promotion cost dominates, *"the answer is wider hysteresis in `0007`, not a third representation."* It also creates a second demotion path whose losses would have to be enumerated under invariant 3, and the corpus has one lossy path and one enumeration today.

**Evicting the least-stressed Microscopic Segment.** Keeps the simulated set matched to where congestion is, and to do it you must demote a non-empty queue. That is invariant 3 rewritten to accommodate a resource limit, which is the coupling `03 §3.9` spent its second half severing.

**Keeping the Segment denomination and setting the Cap conservatively.** Sizing for the worst Segment under-uses the budget everywhere else, and the amount of under-use is the occupancy variance — the same quantity the correct unit would have measured directly.

## Consequences

- **`03 §3.9` and `CONTEXT.md` → Microscopic Cap and → Segment all state the Segment denomination** and are corrected. `CONTEXT.md` → Segment carries it in a list of things counted per Segment, which is precisely how a unit propagates unnoticed.
- **The admission test needs a Segment's occupancy before it is promoted**, which is available: the in-flight Trips on that Segment are what promotion materialises from, so the count exists in the structure promotion already reads.
- **A promotion can now be refused for a reason other than the Cap being full of Segments** — a single arterial may consume a large share of the Cap alone. That is the correct behaviour and it makes `03 §6`'s open question 2 sharper rather than answering it: *a single arterial jam could cascade promotions along its whole length and consume the Cap by itself* is now a statement about Vehicles, and the cascade is bounded by what the arterial holds.
- **The Cap's value is one side of a ratio and S5 is the other.** [`0002`](../../plans/0002-open-questions.md) §B holds both: Vehicles affordable in a Tick (S5), against Vehicles a real city stresses at once (`06` 5b). Neither existed when `§3.9` was written, which is why it could only say *unknowable before there is a build*.
- **`HONEST DEGRADATION` is satisfied exactly as `§3.9` already arranged** — reaching the Cap means more of the traffic overlay reads *modelled* rather than *exact*, and nothing is announced. This decision adds no event and no Trip Fate.
- **`0002` §C's typing of force-promotion is corrected.** It was filed as an open **decision**; it is **measurable**, with a named scenario and a pass/fail. That is the second thing task 0's method caught after the pass itself had finished.

## What would trigger revisiting

- **The spillback scenario passing with force-promotion disabled.** The mechanism then goes, and the admission ordering it heads goes with it — the priority rule is written as *conditional on force-promotion surviving* for exactly this reason.
- **Occupancy turning out to be nearly uniform across stressed Segments.** The denomination would then be a distinction without a difference, and the simpler unit should win. S5 and 5b both report it.
- **Promotion cost dominating the traffic budget** — `adr/0016`'s own trigger. The answer is wider hysteresis, not the third representation refused above, and the order in which those two are reached for is what this ADR exists to fix in advance.
- **A city in which the Cap binds during ordinary play.** `§3.9` rests on it binding only in cities already congested past the ceiling; if it is common in healthy play, *reaching the Cap is not a failure mode* is a claim about the rare case being applied to the common one, and the exposure is wider than anything priced here.
