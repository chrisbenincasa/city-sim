# A Leg is a plan and a Traveller is a cursor

**A Trip owns its purpose and its Fate, a Leg is the *plan* for one mode-homogeneous span — mode, two Addresses, a travel time and a `next` index — and a Traveller is the *cursor* executing it, holding which Leg it is on and when it arrives.** A Leg stores a **cost, never a path**, and a Trip's Legs are created **eagerly**, all of them, at Trip creation.

Guiding concepts: `LEGIBLE CAUSE`, `UNIQUE INDIVIDUALS`, `BOUNDED KNOWLEDGE`, `SOLVE THE ACTUAL PROBLEM`.

This is an **arguable** claim under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md). The arrangements are indistinguishable in output — they produce the same Trips at the same Ticks — and differ in which invariants hold by construction and which have to be policed. Two *numbers* nearby are measurable and are deliberately untouched here: mean Legs per Trip (`plans/0002` §B-17) and whether pedestrian networks saturate (§B-16), both of whose machine is milestone **5b**.

## Why

**No document in the corpus states what a Leg is.** `CONTEXT.md` → Leg defines a Leg by mode-homogeneity and by the `walk → drive → walk` minimum and names **no fields**; [`0008`](0008-walking-is-a-simulated-leg.md) sizes a table on Legs without saying what one contains. Mode, endpoints and a cost are implied by every consumer and written down by none. That is the largest hole session **F** was booked to close, and it propagates: `03 §4` invariant 3 owes *"write down what is discarded when a Traveller leaves a Microscopic segment — anything not enumerated is a bug"*, and an enumeration cannot be written against a structure nobody has specified.

**The split is three-way, and the corpus had drawn it as two.** Trip and Leg live in `CONTEXT.md` → Movement; Traveller lives thirty entries away under Citizens and fidelity, and nothing says how they divide. Drawn out:

| | Holds | Lifetime |
|---|---|---|
| **Trip** | purpose, origin and destination Address, Leg head index, **Fate**, the failing Leg's index | until its Fate has reached the Census |
| **Leg** | mode, two Addresses, **travel time** (Q16.16 Ticks, `adr/0071`), `next`, **and — amended 2026-08-14 — a route, on a *vehicular* Leg only** | with its Trip |
| **Traveller** | the Citizen, the Trip, **which Leg it is on**, the arrival Tick of that Leg, **and — amended 2026-08-14 — how far along that Leg's route it has got** | created on demand, released on arrival |

⚠ **The Traveller row is amended (2026-08-14, 5c task 3): a cursor into the Leg list is not a cursor,
and this ADR's own title says so.** As written the Traveller holds *which Leg* and *when it arrives*,
which is enough to advance a **Statistical** journey — that tier is time-advanced, so a Traveller
between endpoints is nowhere in particular and the arrival Tick is the whole state. A **Microscopic**
one is somewhere specific: [`0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)'s
amendment requires *"a next Segment every Tick"*, so a Traveller must know **which Segment of its
route it is on**, and that is a second index against a second list. It is added here rather than to
the Leg because it is transient — released on arrival, reconstructible from the route and the clock,
and therefore exactly the kind of thing the three-way split exists to keep off the plan. **This
strengthens the claim rather than qualifying it**: the plan is still the Leg, the route is still the
route cache's, and the Traveller is still the only row that moves.

⚠ **And it is the field that discharges `03 §4` invariant 3's enumeration**, which the Consequences
below promise is *writable* against this structure. Position along the route is precisely what
demotion discards — a Statistical Traveller resumes from its arrival Tick, which the promotion never
touched. Written down now because the alternative is discovering it at the write site in milestone 6,
where an unenumerated field is the bug the invariant exists to catch.

**The reason the third row exists is an invariant, not tidiness.** `CONTEXT.md` → Promotion/Demotion states the one that carries the most weight: *"conserved quantities live on the Citizen record, never on the embodiment — **a Traveller is a view, not an owner**."* Session M has already applied it once, moving the Habit Route from the Traveller to the Citizen on exactly this ground. Making the **Leg** the plan and the **Traveller** the cursor puts every durable thing on a row that outlives the journey and every transient thing on a row that is released — so the invariant holds by construction rather than by discipline, which is the same upgrade [`0005`](0005-two-fidelity-tiers.md) achieved for reconstructibility.

**A Leg stores a cost and not a path, and this is what makes `adr/0008` affordable.** For a **walk** Leg the route is searched, `distance / speed` is taken, and the Segment list is discarded — nothing downstream reads it, because `CONTEXT.md` → Fidelity keeps pedestrians out of Stress entirely and a walk Leg therefore increments no Segment volume. For a **drive** Leg the path already has a home that is not the Leg: [`0060`](0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md) puts it in the shared route cache keyed by `(origin node, destination node, variant)`, reached through an index on the **Citizen**. Storing a path on the Leg would be a third copy of a route the design has twice decided to share, and it would multiply `adr/0008`'s *"roughly triples"* by a route length instead of by a fixed record.

> ⚠ **AMENDED 2026-08-14 by milestone 5c task 6. A *vehicular* Leg now stores its route; a walk Leg
> still stores none, which is where this paragraph was argued.** The walk half is untouched and for the
> reason given: nothing reads a pedestrian's Segments, and `03 §3.7` makes that permanent rather than
> provisional. What the drive half did not anticipate is that
> [`adr/0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) attributes volume
> **on Segment entry**, so an in-flight vehicle needs its route *every Tick, reliably* — and `adr/0060`'s
> route cache is fixed-capacity and **evicts**. An entry vanishing under a moving vehicle strands it on a
> Segment it never leaves, which is an `adr/0006`-class leak presenting as a road busy for ever with
> nothing on it. ***A shared cache is an optimisation and an executing plan is state***, so the two
> structures answer different questions and both survive: the cache answers *what is the route between
> these nodes*, this answers *what is **this** Traveller doing*. The *"third copy"* objection stands
> against a third **cache** and not against a per-journey plan that is freed when the Trip ends. Full
> argument in
> [`adr/0099`](0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md).

✅ **The route-finder exists as of 5c task 3 and this paragraph is unchanged by it** — worth stating, because `plans/0026` scoped that task as *amending* this ADR and it does not. `WalkScratch.PathTo` recovers a Segment list from the predecessors a search leaves behind, opt-in so a walk pays nothing; **where a route is stored is a different question from whether one can be produced**, and this ADR answered the first while nothing had answered the second. Producing a route on demand is what makes the route cache buildable in task 4, and it is the same decision as before: the Leg still stores a cost.

**Legs are created eagerly because a Trip that cannot be read is not reportable.** The alternative — materialise the next Leg on arrival at the previous one — contradicts `CONTEXT.md` → Trip's *"an ordered sequence of Legs"*, makes a Trip unreportable until it completes, and would make §B-17 unmeasurable, since there would be no instant at which a Trip has all its Legs to count. `CONTEXT.md` → Trip is explicit about why the object exists at all: *"Trips are first-class objects, not transient calculations, because **a failed Trip must be reportable**."* Eager creation is also what lets the Trip table be sized on a counted number rather than a guessed one — which `plans/0021` requires, because `plans/0002` §D1 already carries table-sizing ratios as a **live inconsistency** and a fourth guess must not join it.

## Consequences

**`CONTEXT.md` → Leg gains a field set, and → Trip and → Traveller gain the division of labour.** The vocabulary entries stop describing three things in three registers and start describing one mechanism.

**`03 §4` invariant 3 becomes writable.** Demotion discards the Traveller's *cursor* state — queue position, headway, a Switch Lane traversal in progress — and never the plan, because the plan is on the Leg and the Leg is not the embodiment. That is the enumeration the invariant has been owed since it was written, and milestone 6 writes it against this structure rather than inventing one.

**The Trip table needs a sink, and *"Trips are transient"* does not supply one.** [`0006`](0006-no-collection-grows-with-elapsed-time.md) is satisfied by the Fate reaching the Census **before** the row is freed — otherwise the only durable record of a failure is destroyed by the mechanism meant to bound the table. The Fate counters are *flows* rather than levels, so they follow slice 7 task 9's precedent: read as a sum and a peak over the interval, and the reading drains them.

**A Trip records the index of the Leg that failed.** One field, and it is the difference between *no route found* and *no route found on Leg 1 of 3, on foot, from here to there*. `LEGIBLE CAUSE` is the whole reason a Trip is an object, and [`0008`](0008-walking-is-a-simulated-leg.md)'s last consequence demands it by name: *"a Trip Fate of no route found for a destination 50m away must be diagnosable, not mysterious."*

**A Leg's travel time is `(saved AND hashed)`, and this is the one place the structure departs from [`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)'s pattern.** That ADR's lesson is that a Ruleset number copied onto a row at creation and never re-derived is a defect. A Leg's travel time is **not** such a number: it is the answer to a search over a graph state that no longer exists once the graph changes, and the search is not retained. It cannot be rebuilt, so it is saved — and the case where the Ruleset moves underneath it is the same case a bulldozed road creates, which the **Epoch** and the *stranded* Fate already cover.

**Mode lives on the Leg, and the Leg is mode-homogeneous by definition.** A Leg boundary *is* a mode transition, which is what makes `03 §6.4`'s promise good — *"a bus is a Leg type inserted into machinery that already handles Legs"* — without transit existing or ever being built.

**`Borough.Core` gains a namespace and milestone 5b names it.** The tables are `Trip` and `Leg` with per-field `(saved AND hashed)` / `(derived AND rebuilt)` declarations throughout, and Legs hang off the Trip as an **intrusive index list** — a head on the Trip, a `next` on the Leg, both flat arrays — per `CLAUDE.md`'s standing rule and [`0066`](0066-the-provider-list-is-an-intrusive-index-list-and-its-ruleset-length-is-a-cap-rather-than-an-allocation.md)'s precedent.

⚠ **A word collision to avoid at the write site.** A Trip has a **purpose**; the codebase has `PurposeTag`, the counter-based RNG tag policed by `BOR0801`–`BOR0803`. Two unrelated concepts one word apart, in a corpus whose first rule is exactly one meaning per term. Spell the Trip's as **Trip Purpose** and never abbreviate it.

## What would trigger revisiting

**A consumer needing a walk Leg's path after the fact.** The cost-not-path decision rests on nothing reading it: pedestrians contribute no volume, no Stress, and no Fidelity. A pedestrian **Map Layer** — footfall on a high street, which the corpus has not designed — would need the Segment list, and the answer would be to accumulate into the Layer *during* the search rather than to retain the path on the Leg.

**Mean Legs per Trip coming in far from three.** `adr/0008` sizes the Trip table on *"roughly triples"*; §B-17 has never been counted. A large miss in either direction is a fact about the generator rather than about this structure, but it is the number that decides whether Legs want their own table at all or should be a fixed stride on the Trip.

**A Traveller needing to outlive its Trip.** Everything here assumes creation on demand and release on arrival. A Traveller that persists — parked and waiting, queued at a stop — would put lifetime state somewhere with no owner, and the honest response is a new row rather than lengthening this one's life.

**Transit.** A transit Leg has a waiting time that is not a traversal, and `CONTEXT.md` → Transit already says waiting is Leg cost spent against the Commute Budget. Whether that fits in one travel-time field or needs a second is the first question to ask on the day, and it is the only foreseen addition to the Leg's fields.
