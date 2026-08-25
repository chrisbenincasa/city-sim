# A business takes premises by placement, and one ceiling counts both kinds of tenant

**An unpremised Business gets premises the way a Household gets a home: through **placement**, on the
same trigger, the same sample derivation and the same candidate count. ***No new number.*** And a
Building's `occupants` is **one ceiling over both kinds** — a dwelling declaring `occupants = 3` holds
three *tenants*, whether those are three families, or two families and a shop.
[`adr/0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md) decided this
and this ADR builds it: ***shops and housing compete for the same space, and no mechanism was written
to make them.***
`SOLVE THE ACTUAL PROBLEM` `NO VERDICT` `LEGIBLE CAUSE`

⚠ **This exists because the debt was real and unowned.** `PlacementEngine.cs:190` says *"nothing
tenants a Business — `World.CreateBusiness` has no production caller and the placement pass that would
is milestone **27**'s"*, and **none of milestone 27's four tasks named it**
([`plans/0041`](../../plans/0041-the-business-is-a-thing-the-city-contains.md) **G32**). It blocks task
7 by arithmetic: point a Workplace at a Business while nothing is premised and **employment is zero in
every world that ships.**

## Why placement rather than anything else

**Because the alternative is the coupling [`adr/0069`](0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md) exists to split apart.**
A Zone Rule raising a commercial Building and filling it in the same act is *construction houses
somebody*, refused there and refused again in
[`adr/0145`](0145-a-business-is-founded-by-a-household-or-arrives-through-a-gate-and-both-land-in-the-pool.md).
***The pool already exists, the sink already exists, and what was missing is the middle.***

**And the pass introduces no number, which is a decision rather than a convenience.**
`PlacementEngine.Retire` already made this exact argument for the give-up bound: *"It rides the SAME
trigger, the SAME sample derivation and the SAME bound, and every one of those is a decision not to
introduce a number… no world contains a Business to ratify one against."* ⚠ **That is still true** —
a second cadence would be hash-bearing and owed a ratifier under
[`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md), and the
world that would ratify it is the one this ADR is building. ***A shared cadence is a declared stand-in
and not a claim that the two patiences are equal.***

## Why one ceiling, and what it buys

`adr/0141`: *"`occupants` changes meaning and does not change value. It stops being how many Households
and becomes how many **tenants of any kind**."* **The consequence is the point.** A city that fills
with shops houses fewer people — **automatically, from one number, with no rule expressing it.** ⚠
***That is a simulation behaviour obtained for free, and the alternative buys nothing with a number.***

**Two lists, not one discriminated list.** `HasRoom` becomes
`Occupants.Length(b) + BuildingBusinesses.Length(b) < occupants`. Both index lists already exist, and
each threads a `next` column on **its own owner's table** — `Households.DwellingNext` and
`BusinessTable.BuildingNext`. ***A single mixed list would need a discriminated element to know which
table a slot indexes***, which is the polymorphic column
[`adr/0143`](0143-a-bin-hangs-off-its-owner-and-the-polymorphic-column-stays-unbuilt.md) deliberately
left unbuilt. **Summing two lengths costs one add and needs no new storage.**

## Eviction is kind-blind, and it needs a second purpose tag

`adr/0141` again: *"an over-capacity Building evicts, and it never asked what the overflow was."*
**So `Loser` draws across both lists and the highest draw goes**, whichever kind it is. A losing
Household is `Unplace`d into the Unplaced Pool; a losing Business is `Unpremise`d into the unpremised
one — ***and that method already exists***, shipped at milestone 25 task 5 with
[`adr/0144`](0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md).
**The eviction action was built before the thing it evicts could exist.**

🔴 **A second `purpose_tag` is REQUIRED and this is the subtle part.** `Loser` draws on the entity's
**monotonic id**, and Household ids and Business ids are **independent sequences from different
tables** — so Household 5 and Business 5 both exist, and under one tag they would draw the **identical
value**. ⚠ ***Two tenants of one Building would be perfectly correlated in a decision about which of
them loses their place***, which is exactly the invisible correlation `CLAUDE.md`'s *every distinct use
gets a distinct `purpose_tag`* rule exists to prevent. **Tag 27**, reserved to this milestone.

⚠ **This hazard is NEW and could not have existed before.** Every prior draw ranged over one table, so
one tag was one id space. ***The first mixed-population draw in the build is the first place the rule
has teeth***, and it arrived without announcing itself.

## Consequences

- **`founded.toml` stops leaking by construction.** Its header says at length that founding is a
  transfer and departing is an export, so a world that founds and never places **bleeds household
  wealth out through the Hinterland**. With placement, a founded Business can trade instead of
  emigrating. 🔴 **The leak is not gone — it is now a balance rather than a certainty**, and which
  one a world produces is downstream of whether it has premises to spare.
- **Task 7 unblocks.** Premised Businesses exist, so a Workplace can point at one and employment is not
  identically zero.
- **`EvictOverflow` gains a case and keeps its shape.** It already asks two independent ceilings per
  Building — occupancy and jobs — and this widens the first rather than adding a third.
- **A new `World.Premise(Handle<Business>, Handle<Building>)`**, the inverse of `Unpremise` and its
  mirror: refuse an already-premised Business, leave the pool, write the handle, join the Building's
  list.
- ⚠ **Hash-bearing, and not because of a column.** Placing a Business consumes a slot a Household would
  otherwise have taken, so **the housing outcome of every world with a trade in it moves.** Under
  [`adr/0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) that costs
  nothing and must not be a reason to defer.

## Rejected

**A separate `businesses` key on the building kind.** A dwelling would hold three families *and* one
shop, counted independently. ⚠ **It invents a hash-bearing number owed a ratifier**, contradicts
`adr/0141` outright, and ***asserts that shops and housing do not compete for space*** — which is a
claim about cities this design has not made and has no reason to make. **The competition is the
feature.**

**A workplace-only building kind that Businesses go into.** Cleaner in principle and refused on
[`plans/0040`](../../plans/0040-the-business-is-the-actor-and-the-building-is-premises.md) **F43**:
**no shipped Ruleset declares one**, which is precisely why `jobs` sits on `dwelling` today —
`rulesets/minimal.toml` calls *living above the shop* *"the smallest arrangement in which the
assignment pass has somewhere to send anybody."* ***A mechanism reachable only in a Ruleset nobody has
written is a mechanism with no world in it.***

**Evicting shops before families, or families before shops.** A preference reads as humane and is a
**policy claim** — that the city protects housing — which `PLAYER GOVERNS` puts on the player's side of
the line rather than the simulation's. ⚠ **It is also a mechanism admitting one outcome**, which
`NO VERDICT` refuses. ***If the city should protect housing, that is a Policy somebody enacts and not a
constant in the evictor.***

**A Business choosing premises by anything richer than room.** Rent, footfall, proximity to suppliers.
Every one of them reads a quantity that is **unbuilt** (`adr/0070`), and satisficing on the first
candidate with room is [`adr/0017`](0017-agents-satisfice-they-never-optimise.md) unchanged.

## What would trigger revisiting

- **Shops crowding housing out to the point of a stalled city.** If a world reaches a state where the
  Unplaced Pool cannot drain because trades hold the slots, the shared ceiling is producing a deadlock
  rather than a tension, and either the pass needs a housing floor or the ceiling needs splitting after
  all. ⚠ **Measure it before believing it** — a shared ceiling was chosen precisely because the
  competition is wanted.
- **A workplace kind shipping.** The moment a Ruleset declares premises that exist to be worked in, *any
  Building with room* stops being the obvious rule and *the kind that admits a trade* becomes
  arguable — which is a permission question, and `Zone` is where this corpus puts those.
- **The two placement passes needing different cadences.** The shared trigger is a stand-in. If a
  measured world wants shops to find premises at a different rate than families find homes, that is a
  second number and it arrives with a ratifier or not at all.
