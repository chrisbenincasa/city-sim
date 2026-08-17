# Stress and Sight share a volume and not an expression, and the static term belongs to Habit

**[`0046`](0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)'s requirement
that Stress and Sight *"read the same quantity"* is satisfied at the level of **volume** and nowhere
above it. One per-Segment count, incremented on entry and decremented on exit, feeds both — and
[`0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) already made it one
count, so the requirement was discharged by a decision taken for another reason and nobody came
back.** Above volume the two consumers transform it differently and must: **Stress is a *load* and
Sight is a *cost*.**

**So `03 §3.3`'s `complexity_factor` stays on Stress and does not join Sight**, and the ground is
`adr/0046`'s own layer decomposition — the factor is **static geometry**, and a term that contributes
the same amount at every crossing is a property of the network rather than of the moment, which is
**Habit**'s layer and not the live one.

**And that is where the real defect is. `complexity_factor` is not in Habit's cost basis either**, so
every Habit Route in the design as written is computed as though every junction is simple. That is the
same under-pricing `§3.6` is about, in the one layer that could act on it, permanently, for every Trip.
**It belongs there**; how much it buys is measurable and is routed rather than claimed.

Guiding concepts: `BOUNDED KNOWLEDGE`, `LEGIBLE CAUSE`, `SOLVE THE ACTUAL PROBLEM`.

## Why

`adr/0046`'s last consequence reads:

> **The Microscopic Cap gains a second consumer of the same signal.** Sight reads live `v/c` at a
> junction; Promotion reads Stress on a Segment. **They must read the *same* quantity** or the city
> will divert around a jam it never promotes, which is `01 §7`'s contradiction again.

`03 §3.3` gives `stress(segment) = volume / capacity × complexity_factor(junction)`. `adr/0046` gives
Sight bare `v/c`. **The complexity factor is in one and not the other**, it is not decorative — `§3.3`
says it *"lowers the effective threshold for junctions with many conflicting movements"* and `§3.6`
makes it the sole standing mitigation for the blind spot the audit exists to cover — and **neither
document has ever been checked against the requirement.** An ADR issuing a write to another document
that did not land: [`plans/0012`](../../plans/0012-corpus-audit.md) **Cause 2**.

### The claim is stronger than its own justification, and the weaker form is the true one

The sentence states *the same quantity* and then gives its reason: *or the city will divert around a
jam it never promotes.* **That reason is about the two consumers agreeing on where the jam is.** It
does not require numerical identity, and reading it as though it did is what made the requirement look
unpaid.

**Numerical identity is not merely unnecessary — it is unsatisfiable, and it fails hardest on exactly
the Segments the requirement is about.** On a Microscopic Segment travel time is **emergent** —
[`0016`](0016-the-lane-is-the-entity-not-the-car.md) says so in terms: *"Microscopic Segment — real
Lanes, real queues, IDM, Overlaps, Switch Lanes. **Travel time is emergent.**"* So a driver looking at
a Microscopic arc reads what the Lane is doing, not a ratio; while Stress goes on reading `v/c` there,
because that is what decides demotion. ***The cost of a Segment and the load on a Segment stop being
the same kind of thing at the promotion boundary***, and the promotion boundary is where every jam is.
A requirement that two quantities be identical, which becomes unsatisfiable precisely where it is
supposed to bite, is a requirement stated one level too high.

**One level down it is exact, and already paid.** `adr/0041` makes a Segment's volume a **single
count**, incremented when a vehicular Traveller enters and decremented when it leaves, with no
District-pair counter and no periodic smear. `03 §3.4`'s repaired form says what that buys: *experience
and contribution are one list*. So there is exactly one volume per Segment in the whole model, both
consumers read it, and **there is no second number able to disagree with the first** — which is what
`adr/0046` was protecting against. Above it, `capacity` is static and shared too; what differs is only
what each consumer does with the ratio.

***The requirement was discharged by a decision taken for another reason***, which is the third time in
this session that the answer to an open question was already in the corpus under a different heading.

### The divergence is real and it runs the opposite way from the one that was feared

`§3.3` says the factor *lowers the effective threshold*, so against a fixed `T_high` the multiplier is
**≥ 1**, equal to 1 on a simple through-road. Therefore:

```
stress(segment)  ≥  v/c        always, with equality only at a simple junction
```

**Stress is never below what Sight reads, so a complex junction promotes at a `v/c` at which a driver
sees no reason to divert.** That is *promote without divert*. `adr/0046` feared *divert without
promote* — the city avoiding a jam it never simulates — and **that failure cannot arise from this term
at all**, because the term only ever moves stress upward. The divergence exists, and the direction is
the safe one, and **the direction is arithmetic rather than a judgement**.

⚠ **It is also not `01 §7`'s contradiction.** That rule is about **overlays**: *an overlay must never be
sharper than the simulation underneath it*, and `adr/0046` glosses it as *a number must never be caught
contradicting what the player is watching*. In the promote-without-divert case the Segment **is
Microscopic**, so congestion there is *exact* rather than modelled, the overlay is honest by
construction, and the player sees a real jam with real vehicles queueing in it. What the player also
sees is **drivers continuing to drive into it** — and that is not a contradiction, it is the design.
`adr/0046` rejects the alternative in its own words: *"a player who can see a jam and a driver who
cannot is a legible failure, **but so is every driver in the city routing around a jam the instant it
forms**."* Milestone 5c task 8 measured the corpus's position on this and it is unambiguous — the
priced and free-flow runs agree **to the Citizen** on employment while their occupancies differ by
51.6%, so ***congestion is a cost paid and never a cost avoided***.

**The failure `adr/0046` named would have been the serious one and this is not it.** Recorded as a
correction to that consequence rather than as a vindication of it, because the sentence was right to
demand the check and wrong about what the check would find.

### Sight must not take the complexity factor, on `adr/0046`'s own decomposition

The obvious repair is to give Sight the factor so the two expressions match. It is refused, and the
argument is the ADR's own three-layer split rather than a cost.

`§3.3` states what the factor is: *"derived from **static geometry** — number of approaches, number of
conflicting turn paths — **computed once** and free per tick."* `adr/0046` defines the layers by what
question each answers and how often it refreshes — **Habit** is *the route I normally take*, computed
from a **slow-moving cost basis**; **Sight** is *what I can see from here*, the **live** cost of the
next few Segments.

**A static per-node constant is not something a driver sees from here.** It contributes the same amount
at every crossing of that junction, for ever, which makes it a fact about the network — and a fact
about the network belongs to the layer that reads the network slowly. Putting it in Sight is the layer
split failing: the live layer would be carrying a term with no live content, and the whole reason the
split exists is that *"the expensive layer is the one that touches the whole network and the cheap layer
is the one that touches four arcs."*

⚠ **The symmetry ceiling does not settle this and must not be cited as though it did.** Session D's
amendment fixed the Sight Horizon at 1 because a driver reads `N` live arcs of its **own** route
against **one** of each alternative's, and the asymmetry grows without bound. A **static** term added to
both sides is symmetric and survives that argument intact. So the ceiling is not the reason, and
reaching for it would be `adr/0093` run backwards — citing a nearby decision because it points the same
way. **The reason is the layer, and only the layer.**

### The real defect is that nothing carries it in the slow layer either

Follow the placement through and the finding falls out. If the factor is a property of the network that
belongs to the slow-moving basis, **is it in the slow-moving basis?** It is not. `adr/0046` names a
*"slow-moving cost basis"* and never says what is in it; nothing else in the corpus does either; and the
one basis that exists — milestone 5c's travel-time matrix — is **free flow**, which carries no junction
term of any kind.

**So a Habit Route is computed as though every junction in the city is a simple through-road.** That is
the same under-pricing `§3.6` describes, in the layer that can actually act on it, applied permanently
and to every Trip rather than to whatever a driver happens to see. `§3.3` calls the factor *"a partial
mitigation for §3.6"* while it exists in only one of the three places that could use it, and the place
it is missing from is the one that routes.

**It belongs in Habit's basis, and the case is cheap on every axis.** It is static, so it costs nothing
per Tick; it is **already computed** for Stress, so no new mechanism and no new number is introduced;
and it makes drivers structurally prefer simple junctions, which is what `§3.6` wants and what a driver
without a satnav plausibly does — you learn which junctions are horrible and you stop routing through
them. That is `adr/0017`'s sticky Provider List doing its job one level down, which is what `adr/0046`
says the whole model is.

⚠ **What is *not* settled here is whether it is a sufficient mitigation, and that is measurable.**
`§3.6` says the blind spot is *"mitigated… **not eliminated**"*, and how much a routing preference
removes is a number a machine produces. Routed below. ***A term being in the right place is a decision;
how much it buys is a measurement***, and `adr/0043` keeps them apart.

⚠ **This is not turning movements coming off [`deferred.md`](../deferred.md).** That document defers a
per-`(inbound, outbound)` **accumulator**, a junction **overlay**, and a package of lane-level **player
tools**, on `00`'s anti-goal that this is not a traffic-management game. A static geometric factor on an
existing routing cost is none of the three: it accumulates nothing, it is drawn nowhere, and it hands
the player no verb. **`§3.3` already computes it** — the only question is which consumers read it.

## What is argued here and what is routed

| Settled here | Type |
|---|---|
| The shared quantity is **volume**, and `adr/0041` already made it one count | **arguable** |
| Numerical identity above volume is unsatisfiable at the promotion boundary | **arguable** — `adr/0016`'s emergent travel time |
| The divergence direction is *promote without divert*, and it is not `01 §7`'s contradiction | **arguable**, and the direction itself is arithmetic |
| Sight does not take `complexity_factor` | **arguable** — `adr/0046`'s layer decomposition |
| Habit's cost basis does | **arguable** — same decomposition, opposite end |

| Routed, and no document may cite it as decided | The refuting number | The machine, the world and the quantity |
|---|---|---|
| **Does a complexity term in Habit's basis mitigate `§3.6` enough to matter?** | share of low-volume junction failures a driver population routes around before they fail, against a control basis without the term | milestone **23**'s audit as the detector, on a `CommandKind.Connect` world containing a low-volume turning failure |
| **The factor's own derivation and weight** | — | already `0002` §B's; untouched here, and this ADR sets no coefficient |

## Rejected

**Giving Sight the complexity factor.** Above. It is the repair the question invites and it puts a
static term in the live layer.

**Taking it off Stress.** The other way to make the expressions match, and it deletes `§3.6`'s only
standing mitigation to fix a divergence that runs in the safe direction. It would also make the audit
the *sole* cover for the blind spot, which `adr/0108` has just finished arguing is a low-rate
instrument by design.

**Reading the requirement as satisfied because both sides are monotone in `v/c`.** True and not enough.
Stress is monotone in `v/c` **for a fixed Segment**, so the two never disagree about whether one road is
getting worse — but a driver at a junction compares *different* Segments, each carrying a different
junction's factor, so monotonicity says nothing about the comparison Sight actually makes. Recorded
because it is the argument that would have closed this question wrongly and quickly.

**Amending `adr/0046`'s consequence away.** The sentence demanded a check that had never been run and
the check found something. What is corrected is its prediction, not its instinct — and the half about
`01 §7` is struck rather than softened, because a wrong reason attached to a right instruction is what
gets quoted later.

## Consequences

- **`adr/0046`'s last consequence is amended.** *The same quantity* becomes *the same volume*; the
  `01 §7` clause is withdrawn; and the divergence it predicted is recorded as running the other way.
- **`03 §3.3` says which consumers read the complexity factor**, which it has never said. The factor
  was written as a promotion term and read as though promotion were its only consumer, which is how a
  missing consumer stays invisible — ***a term stated in one expression is not thereby a term nobody
  else needed.***
- **`adr/0046`'s *slow-moving cost basis* is named as underspecified.** This ADR puts one term in it and
  does not enumerate it. **That enumeration is owed**, and it is the third unwritten enumeration this
  session has found after `03 §4` invariant 3's and `adr/0062`'s admission order — which is enough of a
  pattern to say out loud: ***this corpus writes the members it needs and never the list.***
- **Nothing here is buildable and nothing here is blocked on being built.** Habit, Sight and Temperament
  are all unbuilt (`plans/0026`: *"No Habit, Sight or Temperament"*), so this is a decision recorded
  against the milestone that will implement them, in the way `adr/0107` was recorded against
  milestone 21's Lane.
- **`03 §3.6`'s mitigation count is unchanged and its *reach* grows.** The factor was one mitigation in
  one layer; it becomes one mitigation in two. The section still says *not eliminated* and still needs
  the audit.
- **This opens no `adr/0052` number.** The factor's own derivation and weight were already open and are
  untouched; adding a consumer to an existing quantity chooses nothing.

## What would trigger revisiting

- **Turn movements acquiring their own queues.** `adr/0046`'s own last trigger already names this for
  the Sight Horizon, and it lands here harder: a per-movement cost would make the junction term *live*
  rather than static, at which point it stops belonging to Habit and the layer argument in this ADR
  inverts. **This is the trigger most likely to fire**, because milestone 21's Overlaps are what a
  crossing conflict is made of.
- **Habit's cost basis being enumerated and this term being refused a place in it.** The enumeration is
  owed to `adr/0046` rather than here, and it is entitled to disagree.
- **A measurement showing the mitigation is negligible.** The routed row above. It would not put the
  term back in Sight — the layer argument is independent of the size of the effect — but it would leave
  `§3.6` resting on the audit alone, which is a thinner position than that section currently describes.
- **Sight ceasing to read a Segment-level quantity at all.** If a driver at a junction reads the Lane's
  emergent state directly on a Microscopic arc, the *cost* side stops being derived from `v/c` even
  nominally, and the shared-volume claim needs restating over whatever the new pair of quantities is.
