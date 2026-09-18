# 0062 — The urban fabric

Scoped 2026-09-04 against `main` at `8cbb6da`.

**The city called floor area behind an Address density while the picture asked how much of a block
had a Building on it. They are different quantities.**

## Agreed direction

Form restrictions are optional and opt-in. Ordinary zoning does not require the player to choose
a built form: the city chooses among eligible forms. A player may narrow the permitted forms
independently of the intensity permission. An unrestricted form choice still obeys the use,
intensity and physical constraints of the site; permission does not cause construction.

Changed form restrictions apply only to future construction and redevelopment. Standing Buildings
remain permitted to continue in their existing form. Changing a restriction does not itself trigger
demolition, redevelopment or a change to the existing subdivision.

Form restrictions can be painted over selected land, including individual parcels or a street
frontage. Whole-block selection is a convenience, not the minimum scope. Different parts of one
block may therefore carry different form permissions.

Automatic form selection reflects the neighbourhood's development history. Nearby built form
influences new development without requiring it to match. As development changes local conditions,
different eligible forms can become preferable; neighbourhood character is neither permanently
fixed at the first subdivision nor replaced automatically when conditions change. Its evolution
occurs through future construction and redevelopment.

Historical influence is secondary to current needs and site suitability. It must not be the
dominant weight or a requirement to repeat the prevailing form. The intended outcome is a city
that visibly evolves while retaining recognisable character through surviving Buildings and
continuity in new development. Character does not require an unchanged mix of forms.

Historical influence considers shared spatial characteristics across forms, not only matching
form categories. Start with alignment to the Street and setbacks, alongside a modest preference
for nearby forms. These are preferences subject to current needs, site suitability and permissions.
The reference is nearby standing Buildings: new construction becomes part of the context for later
choices, while surviving Buildings carry older characteristics forward. No fixed founding-era
character label is required.

Frontage rhythm, open-ground arrangement and street-facing height transitions are further ways
to carry character across forms. Revisit them when geometry and authored Building variants can
express those differences independently; they are not requirements for the initial implementation.
The first demonstration should show a change of form and capacity retaining a recognisable street
alignment or setback, as well as a case where current needs and site suitability outweigh historical
similarity. Neither should require a player-imposed form restriction.

Redevelopment can combine adjacent vacant parcels into a larger development site while occupied
neighbours remain. It does not require the whole block to become vacant. Parcel boundaries may
therefore evolve locally, allowing gradual changes in built form within one block.

These decisions concern site assembly and form selection within construction and redevelopment.
They do not introduce a developer actor or presume a property-acquisition or relocation mechanism.
The implementation design must establish the existing construction triggers and actors before
assigning responsibility for these choices.

## Decisions still needed

- Which local conditions influence form selection, how they compete with the influence of existing
  built form, and how the first development chooses where there are no neighbours. Today's
  `BlockPatterns.ForBand` selects a pattern from a density-ranked ladder; it is not an independent
  form-selection mechanism.
- How a proposed development resolves permissions across its whole site, including mixed form
  restrictions within a block. Today's saved `BlockPattern` chooses a subdivision for the whole
  block; painting finer permissions must not silently replace neighbours' permissions or assume
  that mixed built forms are already supported by that representation.
- How to assemble adjacent vacant parcels, preserve neighbouring access and permissions, and
  distinguish cleared ground from an unoccupied standing Building requiring demolition. Today's
  `LotSubdivider.RecarveBlock` requires a vacant block and permits only an increase in pattern rank;
  local redevelopment needs a replacement for both constraints and saved geometry that survives
  rebuilding derived state.
- Saved intensity and form permissions versus the saved pattern and storeys already realised;
  floor-area limits, capacity and drawing must agree.

## Current construction path

Traced against `c15d45d`; this describes current code, not an implementation of the decisions above.

1. **Zoning establishes geometry before construction.**
   [`LotSubdivider.SubdivideBlock`](../src/Borough.Core/Entities/LotSubdivider.cs) chooses and saves a
   whole-block pattern. `Carve` creates Lots with parcel bounds, footprints and storeys from
   `LotRuleset`. `ForBand` chooses the pattern; `Height` still derives height from that pattern.
2. **Placement precedes construction.** In [`Simulation`](../src/Borough.Core/Simulation.cs), Phase 6
   runs Household placement and employment before `ZoneRuleEngine.Sweep`. Construction leaves the
   new Building without Households; later placement fills it. Business founding and finding premises
   in `PlacementEngine` are distinct from constructing the premises.
3. **Zone Rules initiate automatic construction.**
   [`ZoneRuleEngine.Sweep`](../src/Borough.Core/Rules/ZoneRuleEngine.cs) runs on each Rule's interval
   and deterministically samples Lots. The Rule supplies the Building kind. `Admits` intersects Lot
   zoning, Rule permissions and band admission. A Rule not reading market need attempts each sampled
   vacant Lot and requires a nonempty Unplaced Pool; it does not test a particular Household's
   willingness to live at that site. A Rule reading market need selects the best admitted vacant
   sample by local Building count, then proximity to the District centre. `Demanded` checks that
   site's District/Good market for elapsed unserved need, a threshold and cooldown. Its claim prevents
   repeated use of the same signal within the sweep. None of these choices selects a built form.
4. **Creation consumes an already-shaped Lot.**
   [`World.CreateBuilding`](../src/Borough.Core/Entities/World.cs) requires a vacant Lot, creates the
   Building, seals its footprint, records its age/empty clock and fits declared Bins, Rules, parking
   and any associated Business. This automatic construction path charges no private construction
   budget and consumes no construction Materials. Player Service and Outside Connection commands
   have separate permission/payment checks before using the same creation method; synthetic city
   setup also calls it directly. No developer actor is needed to describe these paths.
5. **Vacancy and redevelopment are separate from low occupancy.** `LotTable.IsVacant` means no
   Building. An abandoned Building continues to hold its Lot until collapse or removal. Zone Rules
   can subsequently construct on the cleared Lot. `RecarveBlock` only changes subdivision for a
   wholly vacant block, preserves mixed parcel zoning and requires a higher pattern rank; its caller
   is `Resubdivide`, not Building destruction. There is no local parcel-assembly step.

The storage issue is more than adding a saved rectangle: `LotTable` already saves parcel bounds,
footprints, storeys and pattern. `World.RebuildParcels` nevertheless overwrites fronted Lots from the
block's pattern and current `LotRuleset`. Local assemblies and realised Building geometry need an
authoritative representation that this path preserves. `LotTable.FloorTiles` and Godot massing use
the shared `BuildingPlan`; keep that agreement when changing the representation.

Baseline verification: 69 tests passed on 2026-09-17 with
`scripts/test.sh --filter 'FullyQualifiedName~ZoneRuleCreateTests|FullyQualifiedName~ZoneRuleDemolishTests|FullyQualifiedName~ParcelTests|FullyQualifiedName~BlockPatternTests|FullyQualifiedName~BuildingPlanTests'`.
These check existing construction, vacancy, contention, geometry and rebuild behaviour; they do not
establish support for the proposed local redevelopment. This trace changes documentation only.

## Local conditions — remaining design

Separate the amount of new floor area justified at a site from the choice of form that provides it.
Otherwise a new score would recreate the density-to-pattern ladder under another name.

- **Reason to add capacity:** use unmet housing or Goods needs together with usable spare capacity
  in the relevant area. The present housing construction gate reads the citywide Unplaced Pool;
  it does not yet supply local housing pressure. Market-reading Zone Rules already have a
  District/Good signal. Defining local housing evidence and matching evidence to Building use is
  additional design, not a claim that both paths already have equivalent inputs.
- **Physical opportunity:** contiguous vacant ground, site proportions and available frontage
  determine which arrangements fit. These conditions can favour different forms at the same
  intended floor area. Candidate assembly must inspect actual geometry and permissions.
- **Development history:** nearby standing forms and their relation to the same Street influence
  choice among feasible alternatives. Its secondary role is agreed; the neighbourhood extent and
  numerical strength remain open. Current needs and site suitability carry more weight than
  historical similarity. Compare Street alignment and setbacks across forms, with a modest
  same-form preference. No neighbouring Buildings means no historical preference; an authored
  deterministic choice among eligible forms would need a defined default.

The current market-site score counts Buildings per Cell and proximity to a District centre. Neither
establishes unmet capacity or a form preference: a tower is one Building just as a house is. Avoid
using this count as a substitute for floor-area intensity. Likewise, a wider Road need not unlock
a form, and high desirability alone does not establish a reason to construct more capacity.
Accessibility can inform where capacity is useful, but its relationship to individual Household
choice and the existing construction gate still needs design.

First decision: whether unmet local need and usable spare capacity should govern the amount of new
capacity, with site geometry and historical character governing how it is arranged. Numerical
weights and any direct land-value effect remain undecided.

## Local housing need — agreed scope and remaining design

The initial construction signal uses actual Households still seeking a home after placement.
Anticipated arrivals and moves by already-housed Households are outside this initial scope; revisit
their inclusion after the seeker-driven construction loop has been demonstrated. An arrival that
has entered the city and remains in the Unplaced Pool is an actual seeker.

Checked against `c5ec430`: `PlacementEngine.TryHouse` samples housing-admitting Lots across the
world. `Consider` excludes absent/abandoned Buildings, requires room and compares the Household's
balance with the Building kind's rent. With choice enabled, `HousingUtility.Worth` scores rent and
individual centrality taste, with an Outside option where available. Centrality is distance to a
lattice origin (the nearest when several exist), not access to jobs. No home-search commute or
reachable-jobs test currently makes the proposed local housing signal meaningful on those terms.

Proposed evaluation mechanics, still to be designed: evaluate a prospective housing site against a
bounded sample of those seekers. Reuse their existing affordability
and preference semantics for the proposed Building kind and location. Compare with sampled usable
existing homes and the relevant Outside alternative rather than counting every seeker as wanting
every site. This asks where additional capacity could serve individual Households; it introduces
neither a global housing-demand meter nor synthetic future residents.

Keep reasons separate: a suitable full Building is evidence of a capacity constraint; an
unaffordable Building is not evidence for constructing more homes at the same rent; failure to
discover a usable vacancy is not proof that the city lacks capacity. Probing prospective sites is
necessary as well as observing existing searches, or land with no Buildings can never establish a
case for its first homes. These are design requirements for the signal, not current reason counters.

Sampling, the meaning of a positive preference comparison, spatial extent, and turning evidence
into a bounded floor-area choice remain open. Existing kind rents do not change with form, so a
different form alone must not be claimed to fix affordability.

## Accounting for recently added housing — recommended design

Use actual available housing as the primary brake on repeated construction. In the current engine
construction completes immediately. `PlacementEngine.Place` runs before the Zone Rule sweep when
its own cadence is due; construction can therefore see the remaining seekers and remaining capacity.
There is no construction delay requiring an additional saved reservation timer in this slice.

Within a housing-construction assessment, temporarily match sampled seekers to suitable existing
capacity before evaluating additions. Each available tenancy can cover at most one seeker and each
seeker can justify at most one addition across the participating housing Rules. These are estimates
for construction only: do not move Households, remove them from the Unplaced Pool or reserve their
actual future choice. Several Houses cannot all claim the same small group, and one empty home
cannot suppress construction for every seeker who could afford it.

After committing a Building, include its actual capacity in subsequent assessments, including later
Rules in the same Tick. On a later Tick, recompute against standing Buildings and current seekers.
Occupancy, departure, demolition, abandonment and rent or balance changes therefore affect the
answer through existing state. Temporary matching is rebuilt and bounded by the assessment's
candidate limits; it is not a new population group or a persistent Household-to-Building assignment.

Capacity uses the same units as placement. `World.HasRoomForHousehold` requires a declared housing
kind and `Tenants < occupancy`; `Tenants` includes Household and Business tenancies. A Household
occupies one tenancy regardless of its Citizen count. Estimate a proposed Building's available
housing after any associated Business tenancy rather than treating all floor-derived occupancy as
homes. Mixed-use competition must use the same capacity rules as actual placement.

The bounded search remains a substantive design issue. A failed sample does not establish that
suitable vacancies are absent. A capacity index, sampling coverage and conservative refusal when
the assessment is inconclusive need an explicit algorithm. Newly created capacity must be made
visible directly within the assessment; hoping a random sample discovers it is insufficient.
The matching policy must also avoid consuming a cheap home for a flexible seeker while counting
a less-affluent seeker as needing another cheap home. These issues prevent treating a raw vacancy
total or independent per-seeker samples as a finished solution.

Acceptance examples for the eventual algorithm:

- Two overlapping housing Rules cannot repeatedly answer the same seekers in one Tick.
- A newly built suitable Building suppresses equivalent additions across several construction
  sweeps even when placement has not run again; no timeout releases imaginary demand.
- Unaffordable, abandoned or otherwise unsuitable empty capacity does not hide an actual need.
- Several seekers sharing one suitable vacancy leave residual need; spare capacity is not counted
  once per seeker. Business tenancies and Household sizes do not distort the housing count.
- Occupying or removing homes changes the next assessment; save/load reproduces its decision
  without serialising temporary matching. A failed proposal consumes no evidence or capacity.

New housing may be justified alongside existing vacancies when repeated unsuccessful searches
establish a substantial, persistent mismatch with the available homes. This direction is agreed.
A minor preference improvement alone is insufficient, and the proposed home must be affordable.
The numerical preference margin and persistence duration remain tuning/design work. Actual
placement remains probabilistic where enabled; construction evidence does not turn its estimates
into mandatory Household choices.

### Persistence — recommended mechanics

Measure elapsed Ticks between qualifying observations, not a count of searches. The current
`UnplacedTable.Since` records entry into the Pool and `Considered` counts existing Buildings shown;
neither establishes repeated mismatch. A Household waiting to be sampled must not accumulate
evidence as though it had repeatedly rejected suitable alternatives.

A qualifying observation must distinguish capacity shortage, affordability and a substantial
preference mismatch. Start an episode on the first such observation, and require later evidence
of the same reason plus a current comparison before it can justify construction. Merely losing a
probabilistic choice draw is insufficient. A large gap without observations must not certify
continuous mismatch: define a freshness limit consistent with the sampling cadence. Proposed
duration, freshness and preference-margin tuning belongs in the Ruleset.

End or restart an episode when the relevant reason changes, when an assessment finds suitable
available capacity, or when the Household leaves the Pool. A later construction proposal still
checks current rent, balance, alternatives and its own location; old search evidence is not an
entitlement to build at any site. Keep bounded per-seeker episode state, not an ever-growing
search history. State that affects a later decision must be saved and hashed; temporary capacity
matching remains separate and rebuilt.

The initial city needs a distinct case: no usable housing capacity is a capacity shortage, not a
preference mismatch awaiting repeated refusals. Do not apply the preference-persistence delay as
a prerequisite for the first homes. Capacity shortage still needs positive evidence for the proposed
site and protection against counting undiscovered existing homes as absent.

Acceptance should compare equivalent qualifying observations at different search frequencies,
long unsampled waits, changed reasons, newly available suitable homes and save/load mid-episode.
Persistence should reject transient preference noise without creating permanent suppression after
the underlying conditions have changed.

### Size of the response — agreed direction and proposed bounds

The response must be expressed in additional Household tenancies before being translated through
the shared floor-area calculation into candidate geometry. A zoning cap is not a construction
target. Nor is one sampled seeker evidence for one seeker at every unsampled site: extrapolation
from the bounded sample requires explicit treatment of coverage and already-accounted-for capacity.
Whole floors and feasible footprints may supply more capacity than the residual need. A modest
surplus is permitted: compare forms providing roughly the justified capacity rather than requiring
one waiting Household per new tenancy. This direction is agreed; numerical limits remain open.

Recommended bound: for a positive number N of distinct, qualifying seekers not covered by existing
capacity or earlier proposals, allow up to N plus a surplus. Bound that surplus by both a Ruleset
percentage of N (rounded up to whole tenancies) and an absolute Ruleset maximum. Use the smaller
allowance. With N = 0, this signal permits no construction. The percentage keeps small responses
small; the absolute maximum prevents a large response from carrying unlimited spare capacity.
These are upper bounds, not targets to fill. Evaluate actual capacity through `BuildingPlan` and
`CapacityRuleset.Holds`, accounting for associated Business tenancy.

For the initial implementation, prefer verified distinct seekers over multiplying a small sample
by the citywide Pool size. A larger candidate can request a larger bounded assessment; its extra
capacity must earn evidence within that budget. Repeatedly observing one Household never increases
N. If the evidence budget cannot substantiate a large candidate, consider a smaller one or defer
the proposal. Report that distinction rather than claiming that no local need exists. The maximum
assessment size must be checked against the largest supported Building's housing capacity, or the
budget itself would silently prohibit that form.

Proposals sharing seekers and suitable capacity must share the accounting. Committing one removes
its matched seekers from the residual and makes its spare capacity visible to later proposals.
Recompute on subsequent assessments against standing capacity; a fresh sweep is not permission to
repeat the surplus for the same unresolved need. Avoid adding independent allowances for every Lot
or Rule that happens to sample the same site.

If a candidate's smallest feasible geometry exceeds the bound, leave it unbuilt, consider a smaller
site/form if permitted, or wait for stronger evidence. A player restriction permitting only large
forms does not itself supply the missing justification. Candidate ranking within these bounds still
needs to balance useful capacity, site fit and the agreed secondary historical influence; spare
capacity is not itself a benefit to maximise.

## Proposed integration, pending remaining design

Keep Zone Rules as the automatic construction trigger. Between sampling a candidate and committing
construction, evaluate a bounded set of sites using that vacant parcel and eligible adjacent vacant
parcels. Resolve land permissions across each whole site, then choose a feasible form and floor area
using the agreed historical influence and the local conditions still to be designed. Preserve the
existing one-Lot candidate as an option. This is simulation decision logic, not a new economic actor.

Candidate evaluation must not change parcels or consume the market claim. Once a candidate is
chosen, recheck that its parcels remain vacant and permitted, then commit the assembly, realised
geometry, construction and any market claim together. Multiple Rules or sampled Lots can discover
overlapping sites, so the existing deterministic Rule ordering needs explicit overlap handling.
Rejecting a proposal must leave the existing subdivision and the construction signal intact.

This moves automatic site/form choice ahead of `CreateBuilding`, while that method remains the
common Building-creation boundary. Household placement keeps its own decision and cadence.
Clearing a Building makes ground eligible for a later construction sweep; it need not immediately
re-plat an entire block. Ruleset tuning, saved-state compatibility, road edits and shared geometry
checks must cover the new local layout before it is usable.

## Sequence

1. Make `--morphology` report parcel, potential-footprint and standing-footprint coverage per saved
   `BlockPattern`. Reproduce `minimal.toml` and `platted.toml` at fixed populations and camera scales.
2. Separate intensity from coverage in the pattern selection. A tower may house the most people and
   still occupy little ground; neither ordering is allowed to stand in for the other.
3. Make non-Detached forms reachable in the ordinary playable city without making density-band
   admission and built form one indivisible choice.
4. Give the Tower a form of its own: a solid tower over a low podium or perimeter base. Do not let
   `BuildingPlan.Hollow` turn a tall footprint into an extruded courtyard merely because it is wide.
5. Move capacity and drawing through the same plan result. A podium, tower and courtyard must count
   exactly the floor the shell draws; this cannot be a renderer-only repair.
6. Re-run parcel invariants, replay/save equivalence and the fixed visual specimens. Done means dense
   blocks have continuous Street walls where their form promises them, Detached centres remain open,
   and a tall Building is not a hollow square unless its form explicitly says courtyard.

## First finding

Most Rulesets declare no `[[band]]`, so `BlockPatterns.ForBand` selects `Detached` everywhere. The
empty middle is then unlotted ground rather than a vacant Lot, and no placement pass can fill it.
`platted.toml` proves the other patterns can reach the interior, but also proves that floor-area
intensity can rise while Building count and ground coverage fall. The first step therefore measures
all three surfaces separately before changing one.

## Implemented so far

- `--morphology` now reports parcel, potential-footprint and standing-footprint coverage by the
  saved pattern on each Lot. At 10,000 Citizens, the old Tower measured 25.0%, 17.1% and 8.0%.
- A Tower now owns its full block. Its shared `BuildingPlan` is a two-storey, near-full-site podium
  beneath a centred solid shaft half the footprint on each axis. Capacity counts that same vertical
  partition and Godot emits it as two consecutive bodies for one Building.
- The intensity target remains floor area per block: `BlockPatterns.Storeys` solves the Tower's
  height against the shaft after paying for the podium. Giving the block centre back to the Lot
  therefore does not turn the densest form into a broad slab.
- On the same specimen the new Tower measures 100.0% parcel and 88.6% potential coverage; standing
  coverage is 46.1% because two of four Tower Lots are vacant. The driven Tick 1,026 frame shows
  solid shafts over broad bases rather than hollow-square towers.

Steps 1, 4 and 5 are implemented and verified. The Tower now demonstrates the geometric half of
step 2, but its intensity is still inferred from the selected pattern. Step 3—and the saved
intensity/form separation it requires—remains deliberately separate from this geometry change.
