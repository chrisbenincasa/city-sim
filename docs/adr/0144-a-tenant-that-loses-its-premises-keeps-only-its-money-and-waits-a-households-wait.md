# A tenant that loses its premises keeps only its money and waits a household's wait

**A tenant evicted from its premises keeps its **balance** and loses everything else it was holding. Its
stock Bins are freed at the moment the tenancy ends, and it searches for new premises carrying its till
and nothing else. **The rule is one rule for both Occupants** — it is what `World.UnfitOccupant` already
does to a Household, and a Business is held to it too. And **a Business waits exactly as long as a
Household waits**: it shares `[placement] gives_up_after_days` rather than stating a bound of its own,
**as a declared stand-in** rather than as a claim that the two patiences are equal.**
`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `HONEST DEGRADATION`

**This settles the two questions [`adr/0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)
and [`adr/0142`](0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md)
left between them** — `0141` gave the tenant property and `0142` gave it a lifecycle, and neither said
what happens to the property when the lifecycle reaches its gap.

## Why

### The first half is forced for a Household and chosen for a Business

**A Bin's capacity is derived rather than stored** ([`adr/0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)),
and under `0141` it is derived from **the premises' kind** — `World.RebuildCapacities` reads
`Buildings.Kind[buildingSlot]` and asks the Ruleset what that kind declares. ***So a tenant with no
premises has no kind, and a tenant with no kind has no ceiling.***

🔴 **The build already held an answer nobody had chosen.** `RebuildCapacities`'s `TryResolve` returns
`kind = 0` on a severed premises handle and `DeclaredCapacity` maps an undeclared kind to **0**, so a
rebuild would silently set a stock Bin to capacity 0 **while it holds stock** — which under `adr/0064`
is not a clamp but a **drain**. An answer arrived at by three defaults in a row is not a decision, and
it is the shape of thing that is discovered in a save file.

**For a Household there is nothing to decide.** An unhoused Household has no kind under any reading, so
its stock cannot have a ceiling, so it cannot be kept. `World.UnfitOccupant` frees every non-conserved
Bin on eviction and this ADR does not change it — it **names** it, so that the next tenant type is held
to a rule rather than to a precedent nobody wrote down.

**For a Business it is a choice, and the choice is to keep one rule.** A Business could have been given
an unbounded ceiling while unpremised, the way the treasury and a District Pool's Bins already are. It
is not. ***The word "Occupant" is only worth having if it names one thing***, and a Business whose stock
survives eviction while a Household's does not makes the shared word a coincidence between two special
cases.

### Money is not the exception; money is the rule stated exactly

The balance survives because it is **conserved, unbounded, and names no premises at all** —
`Invariant.MoneyIsConserved` holds it and no building kind declares its size. So the principle is not
*a tenant keeps its money*, it is:

> ***A tenant carries across the gap precisely what does not depend on premises for its bounds.***

Money satisfies that and stock does not, which is why the rule reads as an exception and is not one. It
is also `adr/0054`'s existing sentence about a demolished Building's Households — *it arrives with its
balance intact, because losing a dwelling is not losing what you own* — arriving one actor later.

### The second half: a number that cannot be ratified must not be written

`adr/0142` left the give-up bound open and supplied the argument against sharing: ***a shop looking for
premises and a family looking for a home are not obviously the same patience.*** **That argument is
correct and it is not what decides this.**

A second bound is **hash-bearing**, so on the day it is written it needs a named ratifier — a machine,
a **world** and a **quantity** ([`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md),
amended twice), and a category is not a name. 🔴 **No world contains a Business.** Nothing creates one:
`World.CreateBusiness` has zero `src/` callers, and milestone **27 task 8** is the first pass that would.
So no world can be named, no quantity can be measured, and ***the second bound could only be written as
a number with a ratifier field nobody can fill*** — which is the precise thing `adr/0052` exists to
refuse.

**The choice is therefore not *one bound or two*. It is *one bound, or an unratifiable number*.** Shared,
and declared a stand-in, with `0142`'s argument deferred rather than overruled.

## Consequences

**An evicted Business searches holding its till.** Its inventory is **destroyed**, not sold — there is
no buyer until [`adr/0139`](0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md)'s
market has two sides at milestone 26, and a Rule cannot trade with nobody.

🔴 **In a city with this build's churn that makes a Business's inventory nearly worthless, and the
number is recorded rather than left to be discovered.** Measured on `rulesets/minimal.toml` at 4,000
Citizens over 20,480 Ticks — **ten in-world Days** — the sweep raised 2,375 Buildings and condemned
**2,610**. ⚠ **That rate belongs to the demonstration file and not to the design**: `condemn_after = 4`
is a deliberately short fuse so that decline is observable in a run somebody will sit through, and
`minimal.toml`'s header says it is a demonstration rather than a city. ***So the figure bounds what this
decision costs today and settles nothing about how often a city ought to demolish*** — and if the churn
proves to be roughly right, then this rule is what has to move, not the churn.

**`plans/0002` §D's `gives_up_after_days` row now bounds two pools on one pool's evidence.** Its
ratifier names milestone 11's acceptance run, a world where arrivals outpace housing, and the Pool's
size at steady state — all measured on **Households**. The row records the widening on the day it
happened and names milestone 27 task 8 as a second trigger to reopen. ⚠ **This is `plans/0012` **Cause
5** reached through a decision rather than through a quotation**: the caveat still says which pool it was
measured on, and the number is now being asked about a different one.

**Nothing new is needed in the invariants.** A tenant's stock Bins are gone rather than uncapped, so
`Invariant.BinCapacitiesMatchTheirDeclarations` has nothing extra to check and needs no third case — which
is the practical argument for this half over the unbounded one.

**`World.Depart` needs a Business overload and that is mechanism, not decision** — it refuses a *housed*
Household today, and an unpremised Business is unpremised by construction. Milestone 25 task 5.

## Rejected

**Unbounded stock while unpremised**, on the treasury's and the District Pool's precedent. It gives
"unpremised" different physics from "premised", and it opens a question it does not answer: what happens
when the Business takes premises declaring **less** than it is carrying? `adr/0064` already answers —
an over-full Bin **drains rather than clamps** — so a Business moving into a smaller shop would bleed
stock for some while, which is [`adr/0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s
over-capacity eviction arriving through a back door. ***A mechanism nobody asked for, to preserve stock
the next demolition destroys anyway.***

**A saved capacity on tenant-owned Bins.** It contradicts `adr/0064` head-on, and it reinstates the exact
defect [`0040`](../../plans/0040-the-business-is-the-actor-and-the-building-is-premises.md) **F12** had
just removed: two saved facts about one Bin, free to disagree after any edit to either.

**A second give-up bound now, with the ratifier left blank.** Refused under `adr/0052` — see above. ⚠ **A
row saying *ratifier: milestone 27* is not a filled field**, it is the same blank written at greater
length.

🔴 **Voiding the first half as unreachable, which was defensible and is the one worth explaining.** A
Business owns exactly one Bin — its balance. `World.FitOccupant` and `World.CreateOccupantBin` are
**Household-only**, so a Business's stock Bins are **unbuilt**
([`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)) and `RebuildCapacities` has
nothing of a Business's to zero. Nothing can go wrong today, and closing the question *void as posed*
was available — milestone 12's decision 7 closed that way. **It is refused because `adr/0070` says an
unbuilt mechanism is not a design *constraint*; it does not say an unbuilt mechanism is not worth
*deciding*.** ***Voiding it would hand milestone 27 the question at the worst possible moment*** — with
a business kind half-built and the answer needed the same afternoon.

## What would trigger revisiting

🔴 **The strongest trigger would not revise this ADR, it would remove its reason to exist.**
`0040` **F20** records that `adr/0141`'s *What changes* table (tenant Bins declared on `[[business]]`)
and its *Why* (capacity keyed on the **building** kind, *"needs no business kind at all"*) cannot both be
whole, and milestone 27 must pick. ***If a Business's Bins are declared by the Business's own kind, that
kind travels with the Business***, an unpremised Business keeps its ceiling, and the first half's whole
premise — *no premises means no kind means no ceiling* — is false for Businesses. ⚠ **It stays true for
Households under every reading**, since a Household has no kind either way, so what would survive is a
rule for one Occupant rather than for both — and the argument for one rule is most of why this was
decided as it was.

**Milestone 27 task 8, the first pass that creates a Business.** The first day anyone can watch a shop
lose its premises and judge whether losing its stock reads honestly. It is also the day the shared
give-up bound becomes observable and `0142`'s deferred argument can be settled on evidence.

**Milestone 26's purchase.** Once a market has two sides, *destroyed* acquires an alternative — **sold
into the District Pool at whatever it will pay**. That is the first moment the stock could go somewhere
instead of nowhere, and it is the likeliest successor to this ADR's first half.

**A churn rate judged correct in a file meant as a city.** If demolition at anything like
`minimal.toml`'s rate is the intended texture rather than a demonstration's short fuse, then an inventory
wiped on every eviction is an inventory that never matters, and the rule has to move even though the
reasoning above is unchanged.
