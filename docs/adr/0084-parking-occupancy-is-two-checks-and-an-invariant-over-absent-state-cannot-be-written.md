# Parking occupancy is two checks, and an invariant over absent state cannot be written

**[`0009`](0009-parking-is-modelled-supply-never-search.md)'s *"total occupied spaces equals total
parked vehicles at every Tick"* is two invariants, not one, and neither of them runs every Tick.** A
release is checked at its **write site**, `O(1)`, in the tier `02 §10` puts write-site checks in; the
conservation **sum** is an end-of-run check, on the precedent of the two nearest invariants the project
already demoted for exactly this reason. **Neither can be written before milestone 7**, and the reason
is not scheduling: there is no parking state to sum, which is a different thing from state that is
currently empty.

`LEGIBLE CAUSE`

> ⚠ **AMENDED IN PLACE 2026-08-18 by milestone 7's opening sitting —
> [`0112`](0112-a-parking-space-is-held-by-the-citizen-and-a-household-holds-as-many-cars-as-it-has-drivers.md).**
> **The conservation sum's right-hand side is wrong, and nothing else here is.** It reads *count of
> **Travellers** currently parked*, and a Traveller is freed when the journey ends — so on this design's
> own canonical case, *"a household's car sits at home overnight"* ([`0009`](0009-parking-is-modelled-supply-never-search.md)),
> the sum reads **0 = high** every night. The right-hand side is now **the count of Citizens holding a
> Car Park**. The **tier** is right, the **split** is right, and what each half catches is right.
>
> **The error was writable only because the state did not exist**, which is this ADR's own central
> argument working against the ADR: it refused to *build* an invariant over absent state and then
> *specified* one, and a specification over absent state has no operand to check either. ***Refusing to
> write a check over state that does not exist does not license naming its operands.***
>
> **A second correction follows from the first.** *Two independent requirements landing on one field is
> corroboration* is withdrawn: `0009` put the field on the **Trip** and this ADR put it on the
> **Traveller**, and those are two objects, not one field. **Both are freed at the same moment and for
> the same reason**, so the agreement was two authors making one mistake rather than two arguments
> reaching one answer. ***Corroboration requires the two witnesses to be independent, and two objects
> with the same lifetime are one witness.***

## Why

### The obligation is at four documents and zero builds

*Parking occupancy is conserved* is specified in **`0009`**, in `02 §10`'s per-Tick tier, in `05 §60`'s
list of what the headless suite can assert, and in `06`'s milestone 7 risk — and implemented nowhere.
`0002` §E already names this shape from the last time: *"an obligation specified in three documents and
built in none is how `HouseholdHomeExists` came to be reported by nothing, and this one paid for itself
the day it existed."* **This one is at four and has been for longer.**

The corpus's own diagnosis applies without amendment, and so does its warning about the cure:
`plans/0012` *Cause 1* is that every document storing a copy of a fact drifts. **So the fix cannot be a
fifth statement of the obligation.** This ADR states the specification once; the four existing
references become citations, and what actually closes the gap is mechanical and is routed below.

### "At every Tick" is wrong on `02 §10`'s own rule, and the project has already made this correction twice

`02 §10` sorts the invariant tiers **by frequency, never by importance**. The two nearest precedents
were both written as every-Tick obligations and both landed end-of-run:

- **`SegmentVolumeIsConserved` (37).** `0041` asks for it by name, *"every Tick"*, on the ground that a
  Traveller vanishing without decrementing *"destroys the reading permanently."* Built whole-world, with
  the reason stated at the site: *"What holds every Tick is conservation **structurally**, from
  increment and decrement being paired; this is the check that the pairing was not broken."*
- **`BinCapacityMatchesItsDeclaration` (29).** Same demotion, same ground.

**That sentence is the whole argument and it transfers exactly.** A shed's occupancy is conserved every
Tick *by construction*, because an acquire and a release are a matched pair on one Bin. What can go
wrong is the **pairing**, and a pairing defect is a property of a run, not of a moment — so summing the
whole world every Tick spends `O(world)` to detect something that a single sum at the end detects just
as certainly. `0009` wrote *"at every Tick"* before either precedent existed.

### So it is two checks, and they catch different things

| | Tier | What it asserts | What it catches |
|---|---|---|---|
| **`ParkingSpaceIsReleasedOnce`** | write site, `O(1)` | A release names a ~~Bin this Traveller holds~~ **Car Park this Citizen holds**, and holds it exactly once | Double release, release of a Car Park never acquired, release after the ~~Traveller~~ **Citizen** was freed |
| **`ParkingOccupancyIsConserved`** | end of run | Σ occupied over all ~~parking Bins~~ **Car Parks** = count of ~~Travellers currently parked~~ **Citizens holding a Car Park** | The **leak** — an acquire with no matching release, which is the `0006`-class defect |

⚠ **Both right-hand sides were amended 2026-08-18 by [`0112`](0112-a-parking-space-is-held-by-the-citizen-and-a-household-holds-as-many-cars-as-it-has-drivers.md)**;
the tiers and the split are untouched. **The write-site check got *stronger* in the move**: one column on
the Citizen makes *holds it exactly once* **unrepresentable** rather than asserted, which is the
`Rule Instance` armed/waiting precedent — the corpus prefers a state it cannot express to a state it
checks.

**The first is `TripHasAFate`'s shape** — a write-site check on a condition that holds by construction,
which is exactly when it is worth writing, because construction is what a later edit breaks. **The
second is `SegmentVolumeIsConserved`'s.** Splitting them is not tidiness: the sum alone cannot say
*which* Traveller leaked and the write-site check alone cannot see a Traveller that vanished without
reaching a write site at all.

### An invariant over absent state cannot be written, and that is not the same as a vacuous one

Milestone 5b shipped `SegmentVolumeIsConserved` **deliberately vacuous** — both sides zero — and the
reasoning was good: *"The alternative is a check written by whoever adds the first vehicular Leg, at
the moment they are least able to notice they have got the pairing wrong."* The obvious move here is to
copy that and write the parking check now.

**It cannot be copied, and the reason is worth stating because the precedent does not carry its own
precondition.** `SegmentVolumeIsConserved` was writable because the *volume column existed* — 5b built
it — so both sides of the equation are defined quantities that happen to be zero. Parking has **no
Bins, no occupancy column, no parked-Traveller state and no shed** anywhere in `src/`; the only
executable shed model in the repository is the S2 spike's. **Both sides would be undefined, not zero.**

***A vacuously-satisfied invariant needs its state to exist. Zero is a value; undefined is not.*** An
assertion over absent state is a comment wearing a test's clothes, and it is worse than no test because
it reads as coverage. That distinction is the general form of slice 5 task 7's *withhold a vacuous
assertion* against 5b's *ship one on purpose*, and it is the missing third case: **withhold when the
shape is wrong, ship when the state exists and is empty, and refuse when the state does not exist.**

### What closes the gap is mechanical, and the corpus already knows the shape

The reason four documents could name an invariant nobody built is that **nothing compares the list
`02 §10` states against the `Invariant` enum**. That is the same defect as `HouseholdHomeExists` being
reported by nothing, as `0033`'s satisfiability invariant sitting specified-and-unbuilt across three
documents, and as `0002` §F silently ceasing to track new ADRs three times.

`0012`'s **mechanical check 5** is the precedent — *generate the ADR column from the directory, because
only that column is generable and a missing row makes an unassessed decision read as absent*. **The
same instrument fits here**, and it is routed to `0012` rather than built in this sitting: a check that
every invariant `02 §10` names has an enum member, and that every member is either registered or
explicitly marked unbuilt with the milestone that owes it. Under
[`0073`](0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)
the filing happens on the day and before the workaround, and *stating the specification more carefully*
is precisely the workaround this finding must not stop at.

## Consequences

- **Two invariant ids are owed at milestone 7, not one**, and `06`'s milestone-8 risk row should read
  *leaks* rather than *leaks, checked every Tick*. The next free id is **40**; ids are not reserved in
  advance, because the project's rule is that an id travels in a crash artifact and a reused id cannot
  be un-reused — reserving one for an invariant that may be specified differently when it is built
  invites exactly that.
- **`0009`'s consequence bullet is amended in place**, not superseded: the defect it names is real, the
  `0006` classification is right, and only *"at every Tick"* and *"an explicit invariant"* (singular)
  change.
- ~~**The write-site check constrains the acquire/release representation.** *A release names a Bin this
  Traveller holds* is only `O(1)` if the Traveller carries the Bin it parked in — which `0009` already
  requires for a different reason (*"A Trip must remember where it parked** in order to walk back to the
  car"*). **Two independent requirements landing on one field is corroboration**, and it is worth
  recording because the field was previously justified only by the return walk.~~

  > ⚠ **WITHDRAWN 2026-08-18 by [`0112`](0112-a-parking-space-is-held-by-the-citizen-and-a-household-holds-as-many-cars-as-it-has-drivers.md).**
  > The constraint survives and the corroboration does not. **A release is `O(1)` only if the holder
  > carries the Car Park it parked in** — that is true and is why the column exists. But `0009` put the
  > field on the **Trip** and this ADR put it on the **Traveller**, which are *two objects*, not one
  > field, and **both are freed at the end of the journey for the same reason**. ***Corroboration
  > requires the two witnesses to be independent, and two objects with the same lifetime are one
  > witness.*** The holder is the **Citizen**, and the return walk is no longer the field's only
  > justification — the overnight case is, and it is the case both original candidates could not express.
- **Nothing is built in this sitting and no State Hash moves.** What milestone 7 inherits is a
  specification it cannot get wrong by reading, rather than four documents it must reconcile.

## What would trigger revisiting

- **The conservation sum being too coarse to diagnose a leak in practice.** If a 100,000-Tick run
  reports a mismatch and the write-site check has caught nothing, the pairing broke somewhere neither
  tier watches — a Traveller freed by a path that bypasses release entirely. The repair is a staggered
  `O(n)` middle tier, which is the one tier this decision does not use.
- **Parking acquiring a second mutation site.** The two-check split assumes acquire and release are one
  matched pair. Illegal parking as an overflow tier (`deferred.md`), or a car displaced by a bulldozed
  garage, would each add a way for occupancy to change, and the write-site check's predicate would need
  restating before either ships.
- **The mechanical check finding more than one unbuilt-but-specified invariant.** If it does, the
  problem is not parking's and this ADR is a symptom — the general fix moves ahead of the specific one.
