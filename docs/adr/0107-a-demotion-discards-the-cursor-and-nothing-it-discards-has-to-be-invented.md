# A demotion discards the cursor, and nothing it discards has to be invented

**When a Traveller leaves a Microscopic Segment, exactly four fields are discarded — its *position
along the Lane*, its *velocity*, its *Lane assignment*, and a *Switch Lane traversal in progress*.
Nothing else is discarded, because everything else durable is on the Leg and the Leg is not the
embodiment. Each of the four is recovered on promotion from state the world still holds**, so
[`03 §4`](../03-agent-architecture.md) invariant 2 — *promotion is reconstructible* — holds across a
full demote/promote cycle by construction rather than by discipline.

Guiding concepts: `HONEST DEGRADATION`, `SOLVE THE ACTUAL PROBLEM`.

This is an **arguable** claim under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
It fixes no number and it is settled by reading what a Lane holds against what a Statistical Segment
does. The two numbers nearby — `T_high` and `T_low` — are **measurable** and are deliberately
untouched, per [`03 §3.3`](../03-agent-architecture.md)'s own *"the two thresholds are measured, not
chosen"*.

## Why

`03 §4` invariant 3 reads *"Demotion is lossy only in enumerated ways. Write down what is discarded
when a Traveller leaves a Microscopic segment. Anything not enumerated is a bug."* It has read that
way since it was written. **The list it owes has to exist somewhere, because the invariant's whole
force is the last clause** — an unenumerated field is not merely undocumented, it is defined to be a
defect, and a rule of that shape is inert until the enumeration exists to be checked against.

### The enumeration was written, in the wrong document, and the write did not land

[`0075`](0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) supplies it twice — once in its
2026-08-14 amendment (*"it is the field that discharges `03 §4` invariant 3's enumeration"*) and once
in its consequences: *"Demotion discards the Traveller's cursor state — queue position, headway, a
Switch Lane traversal in progress — and never the plan."* `03 §4` was never edited, so the invariant
still reads as owing what another ADR believes it has been given. That is
[`plans/0012`](../../plans/0012-corpus-audit.md) **Cause 2**, an ADR issuing a write to another
document that did not arrive — and it is the third instance inside this decision's own material,
after `adr/0007`'s invariant-6 inversion and its `in_flight` bullet.

**That is the smaller half.** Relocating a correct list would be bookkeeping. The list is not correct.

### Two of the three named fields are one field and one derived quantity

[`03 §5`](../03-agent-architecture.md) states the structure plainly: *"A Lane holds its vehicles as a
sorted 1-D queue and updates all of them in one pass. **Vehicles do not hold references to each other
and are not independently scheduled.**"*

**Headway is therefore not a field.** It is a relation between a vehicle and the one ahead, computed
inside the Lane's pass from two positions. A quantity recomputed every Tick from other state cannot
be *discarded* by a demotion, because it was never held across one.

**And *queue position* is ambiguous in the way that decides the rest.** A sorted 1-D queue gives a
vehicle a metric offset along the Lane; its index in the queue is derived from that offset by the
sort. If the field meant is the index, headway is genuinely unrecoverable and belongs on the list. If
it is the offset, headway follows from it and so does the index. **The offset is the real state** —
it is what car-following integrates — so the precise field is **position along the Lane**, and *queue
position* and *headway* both resolve into it.

***A list that names a derived quantity as state cannot be checked against a structure***, and it
fails in the direction nobody guards against. The failure everybody anticipates is an enumeration
that is too short. This one was too **long**, which reads as more careful rather than less, and would
have survived exactly as long as nobody held it against `§5`.

### Two fields were genuinely absent

**Velocity.** Car-following is the Intelligent Driver Model (`03 §5`), which integrates a vehicle's
own speed; it cannot run without one. Velocity is distinct from headway — headway is the gap to the
leader, velocity is the vehicle's own — which is part of why collapsing the two under one word hid
it.

**Lane assignment.** A Segment holds more than one Lane, and which Lane a vehicle occupies is state
the Lane structure is built around. It is also the field that a Switch Lane traversal transitions
*between*, so naming the traversal while omitting the assignment names a change to a quantity that is
not on the list.

⚠ **This list is written against `03 §5`'s *specification*, and milestone 21 has not built a Lane.**
That is designing ahead of the build, which is what an acceptance criterion is for — but it means the
first implementation is the first thing that can contradict it, and a contradiction is evidence rather
than a mismatch to be smoothed over. See *What would trigger revisiting*.

## Nothing on the list has to be invented

This is the half worth arguing rather than asserting, because it is what makes the enumeration a
guarantee instead of a disclosure. **Each field is recovered from state the world still holds at the
moment of promotion** — not from a plausible default.

- **Position along the Lane is the fraction of the Segment's dwell already elapsed**, times its length.
  [`0099`](0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md) prices
  the dwell on entry and the Traveller holds the Tick the Segment completes, so the elapsed fraction is
  arithmetic over state the world still has. **It is not an approximation of where the Vehicle was —
  it is exactly where the Statistical model said it was**, because a Statistical Segment has one dwell
  and no within-Segment dynamics, so there is no other answer to disagree with.

  > ⚠ **AMENDED 2026-08-16, hours after this ADR was written, by session E's Q2** — and the correction
  > is this ADR's own, committed in the bullet next door. The first version read *"the Segment's entry
  > point. A promoted Traveller enters a Segment; it does not materialise in the middle of one"*, citing
  > [`0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)'s attribution on entry.
  > **That is a plausible default wearing a derivation** — the exact failure the velocity bullet below
  > was rewritten to avoid, one bullet away, in the same sitting. `adr/0041` says volume is *attributed*
  > on entry; it does not say a promotion *happens* on entry, and ***an ADR about when a counter moves is
  > not an ADR about when a Traveller is promoted***. A Segment crosses `T_high` on whatever Tick its
  > volume crosses it, and every Traveller already on it is partway through.
  >
  > **Position and velocity are therefore one derivation read once**, both out of `adr/0099`'s dwell,
  > which is tighter than two rules that happened to agree. *(The build corroborates and is not the
  > ground: `TravellerTable.ArrivesAt` is "the Tick at which the current Leg — or the current Segment —
  > completes", and `Carry` holds the sub-Tick remainder, so the elapsed fraction is available without a
  > new column.)*
  >
  > ⚠ **It also makes the guarantee independent of an ambiguity in `03 §4`'s queue guard.** *"A segment
  > with a non-empty queue does not demote"* can be read against `§5`'s **sorted 1-D queue** — the data
  > structure, so any occupied Segment is barred from demoting — or against a **jam**, which is what that
  > note's own *"forty queued vehicles"* and its hysteresis argument mean. The second is the intended
  > one, since the first would forbid demoting a Segment whose rush hour has ended while cars still flow
  > over it, which is the ordinary case the ladder exists for. **Under the entry-point rule the two
  > readings gave different answers**; under the dwell-fraction rule they give the same one, so the
  > ambiguity stops being load-bearing and is recorded rather than resolved here.
- **Velocity is the speed implied by the dwell of the Segment just left.**
  [`0099`](0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md) prices
  a Statistical Segment's dwell on entry from that Segment's volume at that instant, so dwell over
  length **is** the speed the Traveller was travelling at, already computed and still in the world.
  ⚠ **It is deliberately not *free flow*, and that correction is the reason this section is worded as
  it is.** `adr/0007` and `03 §3.2` call travel time on an unstressed Segment *"not an approximation
  but an exact answer"*, and since 5c that is an **idealisation** rather than a fact: the VDF runs on
  Statistical Segments too, so at the shipped `α = 15%`, `β = 4` a Segment at `v/c` 0.8 is **6.1%**
  slower than free flow and at 1.0 is **15%** slower. ***The idealisation is worst exactly at the
  promotion boundary***, because just below `T_high` is where promotions happen — so *free flow* would
  have been a plausible default dressed as a derivation, which is the failure this section exists to
  avoid. `adr/0007` is amended.
- **Lane assignment follows from the next turn.** `03 §5` records that Citybound's lane changing *"is
  triggered purely by proximity to the end of a switchable stretch, with no incentive or politeness
  criterion"* — so under the model as specified, lane is a function of where the vehicle is going and
  is derivable from the route the Leg already carries.
- **A Switch Lane traversal cannot be in progress at entry.** A Switch Lane spans the Overlap between
  two parallel Lanes *within* a stretch, so a Traveller that has just entered a Segment has not begun
  one. This field is discarded and its recovered value is always *none*, which is the strongest form
  the guarantee takes.

**Overlap projections are excluded, and the reason is the same one that excludes headway.** `03 §5`
has Lanes *"exchange their vehicles' projected positions each tick as obstacles"* — computed per
Tick, held across none.

### The force-promotion case is the one that tests the argument

`03 §3.3`'s second trigger force-promotes a Segment because its **downstream neighbour is full** —
precisely the case where a free-flow entry sounds wrong, since the vehicles are about to hit a queue.
Under the dwell-implied rule it is not a special case at all: the Segment was Statistical until that
Tick and its dwell already carried whatever volume it had, so the Traveller enters at the speed it was
actually making and meets the queue immediately. **That is the picture spillback is supposed to
produce**, and it arrives without the rule needing an exception.

### What this buys, and why it is the same upgrade `adr/0005` made

[`0005`](0005-two-fidelity-tiers.md) turned reconstructibility *"from an invariant to police into a
property of the design"* by refusing to discard records. This does the same for the fidelity cycle one
level down: with the four fields enumerated and each one recovered on the way back, a demote/promote
round trip cannot silently lose anything, and `03 §4` invariant 2 stops depending on an implementer
remembering it.

## The route cursor is not on the list, and that is what keeps the list short

**A Traveller holds two cursors one word apart, and only one of them is lost.** The **route** cursor
is which Segment of the Leg's route it is on; the **Lane** position is where it is inside that
Segment. A demotion discards the second and **must not** discard the first.

**It must not, on `adr/0006` grounds rather than on convenience.** `adr/0041` attributes volume by
entering and leaving a Segment. A Traveller that loses its route cursor has no next Segment, so
nothing ever decrements and a `+1` stands on that Segment for ever: **a road busy for ever with
nothing on it.** [`0075`](0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md)'s own task-6 amendment
names that leak, in those words, as the reason a vehicular Leg stores its route at all — so a
demotion that discarded the cursor would be doing on purpose what that amendment forbids happening by
accident. *(The build agrees — `TripEngine.AdvanceTravellers` walks `TravellerTable.CurrentHop` and
calls `Leave` and `Enter` on every crossing — but the argument is `adr/0041`'s and does not rest on
it.)*

⚠ **`adr/0075`'s task-3 amendment says the opposite, and what is wrong with it is a conflict between
two decisions rather than a disagreement with the build.** It reads *"position along the route is
precisely what demotion discards — a Statistical Traveller resumes from its arrival Tick"*. That
matches `03 §3.1`'s table, which calls a Statistical Traveller **time-advanced**. It does not match
`adr/0041`, which requires *"a next Segment every Tick"*, or `adr/0099`, which prices a drive **Segment
by Segment as it is met**. **Both sides are design.** The later and more specific pair wins, and `03
§3.1`'s summary row is the loose one — the build merely shows which reading was implemented, and is
cited here as corroboration and never as the ground.

**The offered reconstruction does not exist, and it fails hardest where it is needed.**
*Reconstructible from the route and the clock* held while a Segment's cost was a function of the plan.
Under `adr/0099` a Segment is priced from its volume at the instant of entry, so how far along a
Traveller has got is a function of a volume history the world no longer holds — and the journey whose
history is least recoverable is the congested one, which is the only journey ever promoted.

**This is why four fields is the whole list.** Everything durable is on the Leg, the route cursor
survives on the Traveller, and what is left is the state that exists only because a Lane exists. A
demotion is cheap because it is *shallow*, and it is shallow because the three-way split put the plan
somewhere the embodiment cannot take with it.

## Consequences

- **`03 §4` invariant 3 carries the list** and stops reading as an unpaid promise.
- **[`0007`](0007-stress-driven-simulation-detail.md) is amended.** Its *"not an approximation but an
  exact answer"* is an idealisation since 5c put the VDF on Statistical Segments, and it was the stated
  ground for a reconstruction rule in this ADR's first draft. The tier split survives untouched — the
  VDF is still replaced exactly where it is weak — but the exactness clause reads as a guarantee and
  is not one.
- **The artefact belongs to milestone 22, not milestone 21.** `adr/0075` routes it to 21 — *"milestone
  21 writes it against this structure"* — but invariant 3 is 22's, and 21 is gated on session **G**
  while 22 is not gated at all. Writing the enumeration behind a gate it does not need was how it
  stayed unwritten.
- **`adr/0075`'s consequence bullet is superseded in its list and upheld in its principle.** *The
  cursor, never the plan* is exactly right and is the sentence that makes this enumeration short; the
  three fields it named are replaced by the four above, and its task-3 amendment is withdrawn there.
- **[`0062`](0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md)'s count now has a
  referent.** That ADR says *"the corpus has one lossy path and one enumeration today"* — this is the
  enumeration. Its refused **virtual queue** would create a second lossy path, and by that ADR's own
  wording it would owe a second enumeration written the same way.

  > ⚠ **AMENDED 2026-08-16 by session E's Q2, and the list had a fourth copy this ADR did not find —
  > the *source*.** [`0016`](0016-the-lane-is-the-entity-not-the-car.md) is where *queue position,
  > headway, and any in-progress Switch Lane traversal* was written; `adr/0075` and `adr/0062` both
  > quote it, the latter **by name**, and `plans/0017` and `plans/0019` carry it too. All are amended
  > or noted. ***Tracing a wrong sentence to the document that quoted it is not tracing it to the
  > document that wrote it.***
  >
  > ⚠ **Two live consequences fall out, and neither is bookkeeping.** `adr/0062`'s eviction refusal
  > rests on *"a Segment that is already Microscopic holds state that cannot be reconstructed"* — which
  > **this ADR's second half contradicts directly**, since each of the four is recovered. That decision
  > survives on its other leg, stated two lines below in its own text: eviction destroys the **queue**,
  > and with it hysteresis at the moment it would have been observed. ***This ADR knocked a leg out from
  > under a decision it cited and did not check.***
  >
  > And **`adr/0016`'s S5 amendment claims a fifth field — the arrival Tick — and it is not one.** An
  > arrival Tick is not lost on demotion; it is a conversion that cannot be *performed*, and the
  > demotion that fails to perform it is one `03 §4`'s queue guard has forbidden since the repository's
  > first commit. The number belongs to `adr/0062`'s **refused second lossy path**, which it prices at
  > 80.9%. Corrected there.
- **A fifth field discovered at the write site is a bug**, by the invariant's own terms. That is the
  artefact working rather than failing.
- **Nothing here sets `T_high` or `T_low`**, and nothing here decides whether force-promotion is
  needed — both are measurable and routed
  ([`plans/0002`](../../plans/0002-open-questions.md) §B).

## What would trigger revisiting

- **Milestone 21 finding a fifth field a Lane holds.** The list is written against `03 §5`'s
  specification and no Lane exists yet
  ([`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) — a
  specification tells you which symbol to read and never what is in it), so the first implementation
  is the first thing that can contradict it.
- **MOBIL, or any discretionary lane changing.** `03 §5` records that Citybound has none and that
  *"MOBIL is the missing half"*. With an incentive criterion, a Lane assignment carries a driver's
  **intent** rather than falling out of the next turn — so it stops being recoverable and becomes
  genuinely lost, which would make it the first field on this list that a promotion has to invent.
  **This is the trigger most likely to fire.**
- **Turn movements acquiring their own queues.**
  [`0046`](0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)'s last
  revisit trigger already names this for the Sight Horizon; here it would turn *Lane assignment* into
  a turn assignment over a different graph, and the derivation from the next turn becomes circular.
- **A Statistical Segment ceasing to carry a priced dwell.** The velocity rule reads `adr/0099`'s
  dwell. If a Statistical Segment ever reverts to an unpriced `distance / speed`, the rule reverts
  with it and the idealisation becomes true again — which would be a simplification rather than a
  problem, but it must be noticed rather than assumed.
- **A second lossy path**, whether the virtual queue `adr/0062` refuses by name or something else.
  One enumeration per lossy path, and a path admitted without one defeats the invariant.
- **A Traveller outliving its Trip** — `adr/0075`'s own trigger. Everything here assumes creation on
  demand and release on arrival; a parked or queued Traveller holds cursor state across a boundary
  this list does not model.
