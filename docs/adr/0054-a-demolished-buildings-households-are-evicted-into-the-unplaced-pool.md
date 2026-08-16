# A demolished Building's Households are evicted into the Unplaced Pool

**When a Building is abandoned, its Occupants are moved to the Unplaced Pool with their Money and
Savings intact, and the Zone Rule's create predicate drains that Pool.** Eviction is the Pool's fourth
entry route and the only one the Household did not choose. Nothing is destroyed, nothing emigrates,
and the Pool needs no Departure to stay bounded — yet.
`LEGIBLE CAUSE` `HONEST DEGRADATION` `UNIQUE INDIVIDUALS`

**This decides, in a smaller scope, something `06` milestone 19 owns**, which is why it is written
down rather than left in a plan: 9a must be able to find this and generalise it instead of
contradicting it.

## Why

**Destroying the Households is a money leak, and this project has already paid for one.**
`HouseholdTable` declares `Money` and `Savings` as saved columns. Freeing the rows deletes both, which
[`adr/0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) forbids — the Outside
Connection is money's only sink. Slice 7 took the Resource family out of order specifically *"to stop
a money leak six slices old"*, and shipping demolition as a second one would be the same defect
arriving through the same door. It is also an unbounded population sink with no Departure record, in a
design where `CONTEXT` → Departure splits leaving into three channels precisely so that collapse and
choice never share a row.

**Demolishing only unoccupied Buildings ships a mechanism nothing exercises.** `SyntheticCity` places
Households in every dwelling, so in any populated world the demolish path would be unreachable — the
exact defect slice 10's own tripwire exists to catch elsewhere, and it would leave the slice unable to
discharge the row-churn assertion it inherited from slice 5 task 7.

**Eviction is free, which was the finding.** Moving a Household out of a Building touches its
`Dwelling` handle and its place in the occupant list. It does not touch `Money` or `Savings`, so
*"Households keep what they own when the city stops housing them"* required no code at all — it is
what not writing to those columns already means.

**The Pool is already bounded, and not for the reason the design gives.** `CONTEXT` → Unplaced Pool
says the Pool is bounded because Households give up and become a Departure. Departure is milestone
19's and does not exist. But **nothing creates a Household after world creation** — `CreateHousehold`
has exactly one non-test caller — so the Pool is a subset of a population fixed at that moment and
cannot grow with elapsed time whatever it does. [`adr/0006`](0006-no-collection-grows-with-elapsed-time.md)
is satisfied by a property that has nothing to do with the mechanism the design intends to rely on.

**That distinction is the load-bearing half of this ADR.** The day immigration arrives, the fixed
population evaporates and Departure becomes the only thing standing between the Pool and unbounded
growth. Whoever builds the gate owes the give-up rule *in the same milestone*, and this is the record
that says so — because the alternative is a future session observing that the Pool has always been
bounded and reasonably concluding it always will be.

**Creation drains the Pool because the design already said it should.** `CONTEXT` → Frontage lists the
four `Evidence` answers for why a Lot is vacant, and **"no Household in the Unplaced Pool that would
accept it"** is one of them — beside *no capital*, not downstream of it. So a create predicate that
consults the Pool is a documented vacancy reason rather than an approximation of `02 §5.6`'s
pro-forma, which needs prices, capital and a bid contest that this build has none of. The
decline→growth cycle it closes is also the only thing that makes Building rows churn, which is what
row recycling has never been tested against.

## Consequences

- **A Household with no dwelling becomes legal state**, which it is not today: `WorldInvariants`
  reports `HouseholdHomeExists` when `Dwelling` fails to resolve. That invariant is qualified rather
  than deleted — the claim becomes *a Household is housed or is in the Pool*, and a Household that is
  neither is still a violation. Qualifying it is the point; deleting it would remove the check that
  catches a genuinely orphaned row.
- **The Pool is a minimal list, not 9a's mechanism.** No refusal reasons, no immigration, no
  Departure, no give-up counter, no rejected-arrival taxonomy. Those are 9a's and naming them here
  would be the trespass this ADR is trying to avoid.
- **A demolition is reversible from the city's point of view and not from the Household's.** The
  rows survive; the tenancy does not. That is what makes *"where did the people from that block go"*
  a question with an answer.
- **Slice 10's long-run assertion gets a second thing to watch.** The Pool must not trend upward over
  100,000 Ticks — and today it cannot, for the fixed-population reason above, which means the
  assertion is weaker than it looks and should be read as guarding the *re-housing* path rather than
  the bound.

## What would trigger revisiting

- **Immigration arriving** (milestone 19, [`adr/0023`](0023-immigration-arrives-through-the-gate.md)).
  This is the named trigger and the one that matters: it removes the fixed-population bound and makes
  Departure load-bearing on the same day.
- **A demolition source that is not decline** — a Disaster, or player-initiated clearance. `CONTEXT`
  → Disaster already says a destroyed Building vacates its Lot, and eviction-by-catastrophe may want a
  different Departure channel from eviction-by-abandonment, since the second is a city failure and the
  first is not.
- **The re-housing path proving to be the wrong drain.** If a Household evicted from a failing
  District is immediately re-housed in the same failing District, the cycle is cosmetic. That needs
  the choice model (`02 §5.4`) to have an opinion, and until then the drain is deliberately blind.
- **`HouseholdHomeExists` proving to be load-bearing in its unqualified form** — that is, if
  qualifying it turns out to hide a class of orphaned row it used to catch.
