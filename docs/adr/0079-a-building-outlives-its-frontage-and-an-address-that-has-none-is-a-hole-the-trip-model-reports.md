# A Building outlives its frontage and an Address that has none is a hole the Trip model reports

**A Building whose last Street is bulldozed keeps standing, keeps its Occupants and loses its Access Point, which becomes a named absence rather than a stale handle.** Its Lot is preserved because it is occupied; a **vacant** Lot that loses frontage is deleted and its land returns to unlotted. Nothing pressures the Building and nothing refuses the edit. The consequence arrives in milestone **5b**, where a Trip to or from an Address that does not exist ends *no route found* — [`0076`](0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md)'s second Fate, already closed and already named.

Guiding concepts: `LEGIBLE CAUSE`, `HONEST DEGRADATION`, `PLAYER GOVERNS`, `NO VERDICT`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md), and only just: the three candidates differ in what the player sees, not in any number a machine could produce. [`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md) decision 4 typed it and directed the slice to take it.

## Why

`02 §2.2` says re-subdivision *"must preserve existing Buildings"* and says nothing about a player bulldozing the only Street a Building fronted — a case that could not arise before [`0077`](0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md), because nothing in a running world had ever removed a Segment.

Three candidates were on the table. **Refusing the edit is out for the reason [`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md) gives and it is the right reason**: *refusing the edit is the road-derived cap [`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md) rejected, wearing a different hat.* That ADR is emphatic that a road-derived refusal *"would pre-empt the lesson the engine exists to teach"*, and a bulldoze the game declines to perform is exactly such a refusal, arriving one verb later.

### The recommended candidate does not exist

[`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md) recommends the second candidate — *"the Building starts accumulating failure pressure and declines through `adr/0053`'s existing machinery"* — on the ground that **"it needs no new mechanism"**. That ground is false, and the code says so plainly.

[`0053`](0053-failure-pressure-is-a-duration-not-a-tally.md)'s pressure is **a duration of Rule Instance starvation**. `ZoneRuleEngine.Condemn` walks a Building's Rule Instances, asks each `RuleInstances.IsStarving`, and condemns when `tick − StarvedSince ≥ CondemnAfter × Rule.Rate`. Every term in that predicate is a property of a **Rule** — a Bin that would not fill. **A Building whose Street was bulldozed starves nothing.** Its Bins are exactly as full as they were the Tick before, its Rules fire on schedule, and there is no instance for `StarvedSince` to be stamped on. Routing the case through `adr/0053` therefore requires inventing a second, road-shaped pressure source and a threshold to go with it — a new mechanism and a new hash-bearing number, which is precisely what the recommendation claimed to avoid.

**This is the [`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md) shape again, one level up.** That ADR recorded a live defect on the strength of what a loader *appeared* not to refuse; this brief recommended a mechanism on the strength of what an ADR's summary sentence appeared to cover. In both cases the sentence was about the design and the answer was in the code, and in both cases the author was competent and reading carefully. *Citing a mechanism is not checking what it is keyed on* — the sibling of `adr/0044`'s **citing an ADR is not applying it**.

### The third candidate is not a shrug, because the absence has an owner

That leaves *stranded but standing*, which reads at first like doing nothing. It is not, and [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) is what tells the two apart.

The question *given a Building can be cut off from the network, should the Zone Rule compensate?* is the exact shape `adr/0070` names as **void**: *given X does not exist, should Y compensate?* Here X is **reachability** — nothing in the simulation reads whether a Building can be got to, because there are no Trips. X is **unbuilt**, not refused, and it has a milestone: **5b**. So the answer is *build X*, and the design work this decision owes is not a pressure source but making sure 5b finds the case already expressible when it arrives.

**It is expressible, and session F closed the enumeration that expresses it hours before this question was put — without either sitting knowing about the other.** An Access Point is an Address, `(Segment, offset, side)` ([`0074`](0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md)). A Building with no frontage has **no Address**, which is a different thing from a Building with a bad one. Under [`0076`](0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md)'s closed set:

| Case | Fate | Why |
|---|---|---|
| The Street is bulldozed **mid-journey** | ***stranded*** | *"the network changed mid-journey; that is the Fate's definition"* |
| A Trip **starts or ends** at a Building with no Address | ***no route found*** | there is no route, and the Trip never leaves |

Neither needs a fifth Fate, which is the test `adr/0076` set for itself and passes here on a case it did not consider.

**And the consequence is the honest one rather than the convenient one.** A house cut off from the road network does not fall down. It becomes somewhere nobody can get to — nobody shops there, nobody commutes from it, and under [`0067`](0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md) the failures accumulate on the **Household**, where a player looks to find out why. The Building's decline then arrives through the economy that already exists, at whatever rate the Ruleset's balance makes it arrive, rather than through a timer somebody tuned. That is `LEGIBLE CAUSE` and `HONEST DEGRADATION` doing the work a pressure source would have done worse, and it is `NO VERDICT`: the game never announces that the player made a mistake.

### An Address that has none is a named absence, not a dangling handle

The one thing that must not happen is the Access Point keeping a handle to a freed Segment. A Segment row is freed on bulldoze, slots are recycled, and a stale handle would resolve to **whichever Street was laid next** — a Building silently acquiring frontage on a road across town, with every hash moving and every test passing. That is the failure `LotTable.BuildingSlot`'s plus-one encoding exists to prevent, in the same file, for the same reason.

So *no frontage* is a **value** — `Address.None` — checked at the write site and reported by name, and it is what makes the state greppable rather than inferable. It is also `CONTEXT.md` → Frontage's *"no frontage"* — **an `Evidence` answer, one of the four reasons a Lot is vacant** — expressed on the Building side rather than the Lot's, and it is the first of those four the build can actually give.

### Vacant land is not preserved, and the definition of done says otherwise

`02 §2.2`: *"re-subdivision... must preserve existing Buildings — **only vacant land re-parcels**."* So the preservation rule is keyed on **occupancy**, and a vacant Lot that loses its frontage is deleted rather than kept — it is land again, and re-parcels if a Street returns.

⚠ **This contradicts [`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md)'s own task 7**, which asks for *"Every Lot has frontage, checked whole-world"*. That invariant is false the instant decision 4 is taken any way other than *refuse the edit* — a preserved occupied Lot is a Lot without frontage by construction. The two sit in one document, decision 4 posing the question and task 7 asserting an answer incompatible with two of its three candidates, and **task 7 is the half that is wrong**: it was written against a subdivider that only ever ran forwards.

**The invariant is therefore `every vacant Lot has frontage`**, whole-world, plus a write-site check that a Lot is only ever preserved without frontage when it is occupied. Stating it as *every Lot* would be an invariant that fails on the correct behaviour, which is worse than no invariant — it is the tier that would have to be disabled to ship, and a disabled invariant is one nobody re-enables.

## Consequences

**A Building never loses its Occupants to a road edit.** Eviction is [`0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s and belongs to occupancy declarations; a bulldozed Street is not an over-capacity Building and must not borrow its mechanism.

**`Address.None` exists and is the Access Point's absent value**, distinguishable from an Access Point that was never derived. Milestone 5b's Trip generator reads it and produces *no route found* without a new Fate.

**The whole-world invariant is `every vacant Lot has frontage`**, and a second write-site check refuses a frontage-less Lot that is also vacant. `plans/0022`'s definition of done is corrected rather than quietly satisfied.

**`plans/0022` decision 4's stated recommendation is recorded as refused with its reason**, because the reason — *`adr/0053` is keyed on Rule Instance starvation and a road edit starves nothing* — is a fact about the build that the next author would otherwise have to rediscover in `ZoneRuleEngine.Condemn`.

**Nothing in this slice observes the consequence.** The acceptance test can assert that the Building stands, that its Address is `None` and that its Lot survives; it cannot assert that anybody minds, because minding is 5b. That gap is stated in the slice record rather than papered over with a test that checks the mechanism against itself.

## What would trigger revisiting

**5b measuring that stranding is invisible.** The claim here is that the economy punishes an unreachable Building at a rate the Ruleset's balance sets. If 5b runs and a cut-off Building is indistinguishable from a connected one over a long run, then reachability is not in fact feeding back and this decision's whole argument — *the consequence arrives through mechanisms that exist* — is refuted. **That is the ratifier, it is measurable, and no session may close it.**

**A player finding the state incomprehensible.** *Your house is fine but nobody can reach it* is legible only if something says so. If the inspector cannot explain a stranded Building in one sentence, the answer is a readout rather than a pressure source — but it is a real gap and it belongs to session **L** and the presentation design.

**Parking arriving.** [`0009`](0009-parking-is-modelled-supply-never-search.md)'s Parking Shed is queried around the **pedestrian** Access Point, and `CONTEXT.md` → Access Point warns that the vehicle one must never be a fallback from a failed Shed query — *a full car park must not cost less than an empty one.* A Building with no pedestrian Address is the degenerate case of that query and wants checking against F's prohibition when the Shed is built, because a `None` that reads as *no constraint* would pay the player for the shortage in exactly the way F refused.

**Anything else acquiring a road-shaped pressure source.** If a later mechanism needs *this Building has been cut off for N Ticks*, it should be built once, named, and given a threshold with a ratifier — not bolted onto [`0053`](0053-failure-pressure-is-a-duration-not-a-tally.md), whose predicate is about Bins and whose `CondemnAfter` is denominated in a **Rule's rate**. Two pressure sources sharing one threshold would make the number mean two things, which is the welding failure `05 §201` names in its own words — *"a constant welded to two decisions is governed by whichever of them is louder"* — and which `02 §2.1` cites when it splits the Cell from the Chunk.
