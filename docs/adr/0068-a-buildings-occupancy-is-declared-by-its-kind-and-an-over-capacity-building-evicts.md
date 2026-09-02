# A Building's occupancy is declared by its kind, and an over-capacity Building evicts

> 🔴 **THE TITLE'S FIRST HALF IS FALSIFIED AS OF 2026-09-02, AND ITS SECOND HALF IS THE REASON THIS
> DOCUMENT STAYS.** [`plans/0053`](../../plans/0053-the-block.md) step 3 makes a Building's occupancy
> **derived from the ground it stands on** — its floor area over `[capacity] floor_tiles_per_occupant`
> — so `[[building]] occupants` is retired and no kind declares a count any more. ⚠ **What a kind
> declares now is WHETHER it takes tenants at all**, `[[building]] tenanted`, which is behaviour
> rather than capacity.
>
> ⚠ **Every consequence below survives the change and one of them is now load-bearing rather than
> hypothetical.** *An over-capacity Building evicts the overflow into the Unplaced Pool* was written
> against a designer lowering a number in a Ruleset, which nobody had ever done; it now fires whenever
> the **rate** moves, and it fired during step 3's own build — a Building whose derived ceiling came
> out at 1 took a Household beside its Business and `EvictOverflow` removed it the same Tick, because
> `adr/0147` has one ceiling count both tenants. ***A mechanism written for an occasion that never
> arrived was the thing that reported the defect.***
>
> ⚠ **And the amendment above still holds, for its own reason rather than by inheritance.** There is
> no occupancy column: the ceiling is computed at the guard that needs it, from the Lot's footprint
> and storeys and the Ruleset in force. The `adr/0064` argument that put it there is unchanged — what
> moved is which fact it reads, not where the fact lives. **No new ADR, per
> [`plans/0045`](../../plans/0045-amnesty.md); this banner is the record, and
> [`plans/0012`](../../plans/0012-corpus-audit.md) carries the correction owed to the title.**


> **⚠ AMENDED BY BUILDING IT, 2026-08-10. This ADR said `derived AND rebuilt`, by analogy with a Bin's
> ceiling, and there is no column at all.** The decision is unchanged and the implementation is *more*
> of what the decision says, not less: occupancy is read straight off the kind at the one guard that
> needs it. The analogy does not survive contact, and the property that breaks it is worth keeping —
> a Bin's ceiling earned a column because `HeadroomAt` is on the hot path and would otherwise resolve
> an owner and walk a declaration list **on every check**, where this is read at a guard that runs
> **once per placement** and the Building already carries its `Kind`. A column here would have been a
> second copy of a fact one field away, which is the thing `adr/0064` was about. **Two consequences
> below are struck with it**: there is no rebuild to check at end of run, so the `Invariant`
> mirroring id 29 is not owed — this section **loses an obligation rather than gaining one**, which
> is `adr/0059`'s shape a third time.

**How many Occupants a Building may hold is declared per `[[kind]]` in the Ruleset and is
~~`derived AND rebuilt` from~~ **read directly from** the Ruleset in force, exactly as a Bin's capacity is
([`adr/0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)).
A density band expresses itself as **which kinds a Lot permits**, never as a second capacity mechanism.
A Building standing over a newly lowered ceiling **evicts the overflow into the Unplaced Pool**, chosen
by a draw under its own `purpose_tag`. The number itself is not chosen here.**
`LEGIBLE CAUSE` `HONEST DEGRADATION` `UNIQUE INDIVIDUALS`

## Why

**The corpus had already decided this, and two documents recorded its absence.**
[`adr/0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md) has a section headed
*Capacity, not quality* whose first sentence is **"Density says how many Occupants a Lot may carry"**,
and a consequence reading *"A Building holds many Occupants"*. Both
[`plans/0002`](../../plans/0002-open-questions.md) §C and
[`plans/0018`](../../plans/0018-session-n-the-bin-the-pool-and-the-economy.md) task 2 disqualify that ADR
as *"adjacent and does not cover it"*, on a distinction — *what may be built* against *how many fit in
what was built* — that `adr/0025` does not draw and its own heading refuses. This is
[`plans/0012`](../../plans/0012-corpus-audit.md) *Cause 1* in the form tasks 3 and 4 met it: **not a
second copy of a fact drifting from the first, but a decision re-derived as absent because nobody looked
in the ADR that made it.** Third sighting in one session.

**`adr/0064` applies verbatim, and this is its second instance.** That decision's test was *can you name
a property distinguishing this column from one already agreed to be derived?* Applied here: a Bin's
ceiling and a Building's occupancy are both authored numbers keyed on the kind, both read on a write
path, and neither is pointed at by live state. There is no distinguishing property, so there is no saved
column — the same move made on `shortfall` and then on `capacity`, arriving a third time. **A rule that
generalises on its second and third application is the evidence `adr/0064` could not supply for itself.**

**Density needs no second mechanism, because the permission set is already the band.**
[`adr/0055`](0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md) scopes a Zone Rule to what it
*builds*; a band is therefore a permitted kind set, and the kind carries the number. `adr/0025`'s two
routes survive intact and land in different mechanisms, which is what says the reading is right rather
than convenient: **Subdivide** is Lot subdivision (`02 §2.2`), many small Buildings on narrow Lots;
**Stack** is one kind with a larger declared occupancy and one shared Access Point. And *"Buildings do
not shrink"* survives, because a retuned kind is a **Ruleset** change and not a Building getting
shorter — which is `adr/0064`'s patch argument, unchanged.

**The transplant from `adr/0064` stops at the over-capacity case, and the reason is nameable.** An
over-full Bin is left to drain because *something consumes it*. Occupancy has **no consumer at all**:
`World.Place` has exactly one caller, and nothing removes a single Household from a standing Building —
there is no housed departure and no moving. So *refuse admission and let it drain* has no attrition to
run on, and it is the failure this session already recorded once, in
[`plans/0018`](../../plans/0018-session-n-the-bin-the-pool-and-the-economy.md)'s tasks 3 and 4
implementation record, finding 3: *an over-full Bin that nothing consumes stays over-full for ever and
cannot distinguish draining from clamping.* **The same fixture problem, arriving as a design condition
rather than as a test difficulty.**

**Left alone it is the split city `adr/0064` refused.** `occupancy` is the **one declared Readout**, and
`rulesets/minimal.toml` bands a dwelling's drawdown on it — `apply = { derived = "occupancy" }`. A
Building permanently holding three where its kind says one therefore *consumes* three where the Ruleset
says one, for the life of the city, with no edit able to reach it. That is precisely the condition
`adr/0064` called its deciding argument, and it is worse here than there: a Bin's over-fullness is a
level that spends itself, and this one never would.

**Eviction costs nothing new.**
[`adr/0054`](0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md) opened this
channel and found the act free — it touches a dwelling handle and a place in an occupant list and
nothing else. Money and Savings survive, so
[`adr/0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) is untouched; the Pool
*is* the demand signal, so a designer who halves a dwelling's occupancy watches two-thirds of the city's
families appear in the readout that means *the city cannot house these people* — **which is a true
sentence about what they just did**, and [`adr/0015`](0015-all-tuning-data-is-hot-reloadable.md)'s acceptance test
landing on a third number.

**The draw needs its own `purpose_tag` and this is not a formality.** Reusing `PoolDraw` would correlate
two decisions invisibly, which `05 §4` bans outright. Evicting in occupant-list order would remove the
same families on every patch, which is `02 §8` rule 5's argument — the same one that made the Pool drain
a draw rather than a queue in the first place.

## Consequences

- **A `[[kind]]` gains an occupancy declaration, and its absence means zero.** Most kinds house nobody;
  a factory declaring nothing is a factory that holds no Occupants, not one with an unset capacity.
- **`SyntheticCity.HouseholdsPerBuilding = 3` stops being a `const`**, which closes a live defect against
  `CLAUDE.md`'s *no tuning number is a `const` in simulation source*. It is also half of `0002` §D's
  *Table sizing ratios* row, whose inconsistency is downstream of this constant.
- **The cap is a write-site guard at `Place`, never a standing whole-world check**, which is `adr/0064`'s
  id-14 finding transplanted: the guard belongs where the write is. It is
  `Invariant.BuildingHasRoomForTheHousehold`, id **30**, and `World.HasRoom` is the *predicate* beside
  it — a full Building is an ordinary answer to placement, so the query is what callers ask and the
  guard is what catches one that did not. ~~What is owed as an end-of-run check is that the **rebuild
  ran**, mirroring `Invariant.BinCapacityMatchesItsDeclaration` (id 29).~~ **Struck: there is no
  rebuild.** See the amendment at the head.
- **The two negative cases are different and the code keeps them apart.** A declared `occupants = 0`
  houses nobody, which a factory means; a kind the Ruleset does not declare at all is **derelict**, has
  no ceiling, keeps its Occupants and admits nobody. Collapsing them would make a designer deleting a
  `[[building]]` paragraph evict a District, against `CONTEXT` → Derelict Building's own sentence.
- **It moved no State Hash.** The only committed line that changed is the recorded Ruleset content
  hash, because `occupants = 3` restates what `SyntheticCity` was already doing and a Zone Rule's
  single placement fits under it. **That is the decision being a no-op on today's content and not a
  no-op on tomorrow's**, which is the same position `adr/0064` shipped in.
- **`Readout.Occupancy` is unchanged** and stays a walk of the occupant list. Declaring a capacity does
  not make a counter worth keeping — `adr/0006`'s question of what keeps a counter true still has no
  answer that a walk needs.
- **This does not settle the five-sixths equilibrium**, and nothing in it claims to. That is
  [`adr/0069`](0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)'s. **⚠ And
  `adr/0069` did not settle it either, because nothing could**: homelessness in the shipped fixture is
  `1 − capacity ÷ population` over a Ruleset that condemns every dwelling on purpose, so it is that
  file's arithmetic and not a mechanism's. **The quantity both ADRs were really about is the vacancy** —
  45% of declared places standing empty, now 10%.
- ~~**The number is unset**, with a named ratifier, per~~ **SET at 3 on 2026-08-11 in both shipped
  Rulesets and moved to `0002` §D1. The named ratifier has run** — the first long run in which placement
  and eviction both do — and did not refute. Per
  [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md). See `0002` §D.

## What would trigger revisiting

- **A density band that cannot be expressed as a kind set.** If bands are wanted continuous rather than
  as discrete buildable forms, the band stops being a permission and the number stops being per-kind.
- **A mechanism that removes one Household from a standing Building** — a housed departure, or a
  Household deciding to move (`02 §5.2` step 1). That gives occupancy a consumer for the first time, at
  which point *leave it to drain* becomes available and eviction should be re-argued rather than
  inherited. **This is the trigger that matters**, and it arrives with milestone 9a.
- **Occupancy needing to differ between two Buildings of the same kind** — which is what happens if
  construction picks a form *within* a band rather than a band selecting a kind. That is the *as built*
  position, and it returns with a saved column and `adr/0064`'s reasoning to answer.
- **An eviction on reload proving intolerable in play** rather than merely severe. The fallback is not
  clamping and not grandfathering; it is refusing the reload, which `adr/0015` already has a category
  for.
