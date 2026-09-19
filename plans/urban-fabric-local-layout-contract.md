# 0062 — Local layout storage and commit contract

Implementation design for the [redevelopment walkthrough](urban-fabric-redevelopment-walkthrough.md).
Geographic storage, local assembly and reader migration are implemented. The current integration
adds bounded capacity-shortage housing construction. This contract preserves the decisions in
[0062](0062-the-urban-fabric.md) without adding a developer actor.

## Sources of truth

| Information | Authoritative state | Rebuilt information |
|---|---|---|
| What future development is permitted | Saved geographic land permissions | Spatial lookup and summaries for candidate discovery |
| The current parcel arrangement | Existing saved Lot rectangles and positions | Adjacency, spatial lookup and frontage |
| What actually stands | Existing Building-to-Lot handle and saved footprint, storeys and form | Capacity from the shared Building plan; drawing |

Keep permissions independent of Lot lifetime. `LotSubdivider.Resubdivide` currently frees empty
Lots that lose frontage, while occupied Lots survive. A lost Street must not thereby erase painted
permission. Conversely, repainting land beneath a standing Building changes future permission,
not that Building's realised geometry.

The present Lot `Pattern` field supplies Building shape but also names a whole-block subdivision.
Separate these meanings in readers: an initial block subdivision can propose Lots, but cannot
reconstruct or override a committed local layout. Choose final field names with the implementation;
do not create a duplicate saved footprint merely to rename its role.

## Geographic permission representation

Use sparse, non-overlapping integer-Tile rectangles recording painted permissions, independent of
the Lot rectangles. Partition their storage into fixed 32 x 32 Tile Cell pages; this is an indexing
boundary, not a restriction on paint or Building extent. Each rectangle stays within one page and
cross-page queries inspect all covered ground. Unrecorded land is unzoned. A record carries bounds, use permissions, intensity
permission and an explicit optional form restriction. An absent form restriction admits any
otherwise eligible form; an explicitly empty set admits none. Do not silently conflate the two.
The first slice can retain existing band identity as the intensity permission, requiring a uniform
intensity permission across an assembled site until numeric cap enforcement is defined.

Store records in an unmanaged table through `Rows.Saved`; any table ownership reference uses
`Rows.SavedHandle`. Group them spatially using derived indices/intrusive lists, not per-record
managed collections. No record's lifetime or bounds are owned by a Lot handle. Allocation and
iteration order must be deterministic. Final table fields and indices are implementation details,
but the three sources of truth above are the contract.

Painting replaces permissions on the selected ground. Decode affected pages into bounded scratch,
apply the paint, then encode equal-permission horizontal runs, extending identical runs vertically.
This provides an exact deterministic representation without needing a minimum-rectangle solver.
If a paint operation
changes only form restrictions, preserve use and intensity on each affected piece. Whole-block
and parcel painting are ways to select ground, not different permission authorities.

There must be one canonical permission source: convert current `Blocks.Zone`, `Blocks.Band` and
`Lots.Zone` readers into ground lookups or explicitly derived summaries as appropriate, rather than
allowing three independently writable versions. A coarse search summary may find candidates but
must never authorise development without checking the ground. Preserve existing use semantics.

The [storage-sizing model](evidence/urban-fabric/permission-storage.md) checks 20,385 paint operations
and bounds page fragmentation at 1,024 records. Repaint replaces/reclaims rows rather than appending
history. It recommends a provisional world-wide limit of 1,048,576 records: 48 MiB of allocated
columns under the proposed schema, plus a 1 MiB derived page directory. Staging/array overhead is
additional. This is a projected engineering budget, not a measured Core memory or timing result.
Keep the behaviour-affecting limit explicit in the Ruleset and validate against saved slot counts;
verify the budget against the implemented table. A paint exceeding the final record limit fails
before changing permissions or saved allocator state. Do not drop restrictions or partly apply it.

Use a complete count pass before staging a multi-page result; one page's growth may be offset by
another's compaction. Prepare column capacity, then retire affected rows before allocating their
replacements in stable order. A no-op does not retire/reallocate rows. The sizing report specifies
the scratch bounds and the distinction between expected refusal and process-level allocation failure.

## Site checks

A first-slice site is a rectangle formed by whole adjacent vacant Lots of compatible depth along
one Street frontage. Vacancy means no Building, including no abandoned shell. Require complete
coverage by permitted ground; unzoned gaps are not available merely because the neighbours allow
housing. Check use and form on every intersected permission rectangle and require uniform intensity
permission for this bounded slice. Refuse unsupported mixed-intensity assembly explicitly, without
claiming the full intensity feature is finished.

Keep all occupied Lots, their access and geometry intact. Require a valid Address for the resulting
Lot, disjointness from other parcels and a valid footprint/floor plan inside the selected site.
This slice merges whole vacant rectangles and replaces one Building's site; it does not cut occupied
Lots, cross a Street or decide arbitrary corner/polygon subdivision.

## Evaluate, validate, commit

1. Evaluate candidates without mutation. A proposal identifies exact source Lot handles and their
   expected geometry, the selected Building kind, resulting Address/parcel/footprint/form/storeys,
   and calculated capacity. A wider arrangement is scratch work, not a saved build queue.
2. Immediately before commitment, resolve every handle and recheck vacancy, geometry, current
   permissions, Street access, overlap, kind and capacity. Resolve current housing evidence at the
   integration boundary. Proposals are synchronous within a sweep in this slice; no saved pending
   proposal or global revision counter is required. Never keep recyclable slot numbers as identity.
3. Preflight storage and expected refusals before mutation, including Lot/Building and fitted-state
   allocation requirements. Ordinary failure leaves the State Hash, permissions, evidence and
   allocator identities unchanged. Reuse existing invariant-failure/crash behaviour for unexpected
   programming errors; this contract does not promise rollback from process failure.
4. Retire only the consumed vacant Lots in stable identity order. Allocate the resulting Lot,
   write its realised geometry and create exactly one Building through `World.CreateBuilding`.
   Permission rectangles do not change during assembly. Unaffected Lot and Building handles stay
   stable. Stale proposals referring to consumed Lots fail validation rather than targeting reused
   slots. Address uniqueness and any other saved references to consumed Lots must be audited first.
5. Update derived frontage, adjacency, spatial and admission indices before another candidate can
   read them. Expose only the committed Building's real capacity to later housing assessments and
   consume only its supported evidence. Put the entire successful transition inside the simulation
   commit path; no shell can publish half a subdivision or bypass permission checks.

Core returns structured refusal codes and ids/numbers. The shell explains, for example, that a
site crosses a form restriction or still holds a Building. Capacity-shortage evidence is now checked in the housing integration path. Persistent preference
mismatch uses saved search episodes; the lower-level assembly API is also used by geometry tests.

## Readers, rebuilds and persistence

- `LotSubdivider.Preview` must distinguish a proposed initial subdivision on unplatted ground from
  the realised layout. On a locally developed block, preview and parcel hit-testing use saved Lots.
- `PaintParcelAt` selects the current parcel's geographic bounds and paints those bounds. It must
  not regenerate the former pattern to decide whether a click hits a parcel.
- Road edits can remove frontage and remove vacant Lots under the existing contract, but keep
  geographic permissions. New subdivision must examine genuinely free ground and cannot overlap
  saved occupied Lots. Restoring a Street must not restore old Lots on top of a merged Building.
- Normal `SaveFile.Read` already restores saved parcel geometry and rebuilds derived state without
  invoking `RebuildParcels`. Preserve that behaviour. Retire or constrain the explicit whole-block
  reconstruction method so no supported reader can overwrite a local layout.
- Include the permission table and any revised saved fields in hashing and saving. Coordinate the
  next save-format revision with concurrent Ruleset work; do not preallocate a version number here.
  Old-save migration must be deliberate or refused clearly. A possible conversion uses old block
  permissions with saved Lot overrides, but it needs tests and must not infer form restrictions
  from the form of existing Buildings. Golden re-recording follows the repository procedure.

## Implementation order and checks

1. Add geographic permission storage, painting and queries with split/coalesce, no-op and capacity
   refusal checks. Verify repeated repainting has a sink and preserves unaffected ground.
2. Add read-only site evaluation and the local commit path. Exercise both walkthrough branches,
   stale/overlapping proposals, occupied/abandoned Lots, access and mixed restrictions. Refusals
   preserve the complete state; successful assembly preserves unaffected handles.
3. Adapt layout readers and road-edit subdivision. Test Street removal/restoration around the
   merged site, derived rebuild equivalence and save/load continuation. Verify capacity from the
   same plan used for geometry, including mixed-use tenancy subtraction.
4. Complete save-format, hashing and golden checks with the required working lane. Then integrate
   shell painting/preview and observe with the drive skill. This first slice is a local-layout
   foundation, not completion of automatic urban development.

The saved-reference audit found `BuildingTable.Lot` and `CondemnationTrailTable.Lot`. The former
must have no surviving reference to a consumed vacant Lot; the latter is explicitly severable because
Lots can already disappear after road changes. Test this history through assembly and save/load;
do not retarget an old condemnation to the new Lot. `Rows.Free` invalidates the old generation.

Permission-reader migration includes `ZoneRuleEngine.Admits`, `World.BandAdmitting`, `ZonedLots`,
whole-block/parcel paint and `Resubdivide`. Keep housing placement's lookup of standing homes
distinct from future construction permission: repainting beneath a standing home must not newly
make it invisible to seekers. Audit existing coupling rather than propagating the new restrictions
through a blanket replacement of every zone lookup.

## Permission-reader migration audit

The migration below is implemented. Geographic permissions are authoritative; Lot and Block masks
are derived summaries. Saved realised Lot geometry remains authoritative after local assembly.

| Reader or writer | Contract |
|---|---|
| `ZoneRuleEngine.Admits`, `World.BandAdmitting` | Future construction. Uses complete-site permission checks. Keep Zone Rule sampling independent of admission, so repainting cannot prevent an existing Building's decline assessment. |
| `PlacementEngine.TryHouse` | Standing housing search. Discovers actual declared housing Buildings, then applies current capacity, abandonment, affordability and preference checks. |
| `PlacementEngine.ProspectCrosses(int, int, Ticks)` and `Compare(ArrivalProspect, ...)` | Both paths use standing-housing discovery; neither advertises empty zoned ground as a home. |
| `PlacementEngine.PrefersSomewhereElse` | Moves by already-housed Households. Although excluded from the initial construction signal, this existing search must use actual housing availability rather than future land permission. |
| `PlacementEngine.Tenant`, `World.HasRoomForPremises`, `World.HasRoomForHousehold` | Existing Building use. The Business search already samples Lots and checks the standing Building's declared capability/capacity without a zone gate. Preserve that separation for both kinds of occupant. |
| `ZonedLots`, `World.RebuildDerived` and invalidation sites | A cache of derived common-use summaries, including vacant Lots. Keep any land-discovery cache separate from a standing-housing index. Define rebuild/invalidation for construction, destruction, abandonment, changed kind declarations and capacity changes according to what the new index stores. |
| `LotSubdivider.PaintAt`, `PaintParcelAt`, `World.ZoneBlock`, `BandBlock` | Write geographic permissions through one authority. Painting must not rewrite realised geometry or invalidate standing housing solely because paint changed. Whole-block operations and fixture setup use the same permission write contract. |
| `LotSubdivider.Preview`, `SubdivideBlock`, `RecarveBlock`, `Resubdivide` | Geometry and future permission are currently combined through one block pattern. Preview realised Lots; initial carving and local assembly query actual ground. Road edits preserve permissions where empty Lots disappear. |
| `DistrictWatershed.HeldForTrade`, `ComponentOf`, `WorldInvariants.HoldsGroundForTrade` | Actual future-land meaning: vacant trade Lots contribute to District formation near built ground and supply a road component. Migrate the operator and invariant together, preserving this deliberate vacant-trade behaviour. Do not replace it with standing Businesses alone. |
| `Evidence.AdmittedByAnyRule`, `Evidence.Lot`, `LotEvidence` | Future-construction explanations. Share the read-only permission refusal logic with construction rather than duplicate a weaker check. Describe mixed geographic permissions without implying one mask fully represents the site. |
| `Main.Zoning` overlay and brush previews, `Main.Readout` | Draw/read geographic permission independently of realised Lot and Building geometry. The overlay draws actual geographic permission rectangles. Parcel selection uses current parcel bounds. Verified with the driven demonstration below. |
| `KindDump` painted-mask aggregation and `ZoneDump` Lot output | Diagnostics of future permission, not proof that a standing kind is unusable. Keep the meaning explicit and represent mixed ground without substituting a permissive union for site admission. |
| `SyntheticCity` initial housing selection and band/zone setup | Fixture construction intent. Populate geographic permissions before creating Lots/Buildings; distinguish deliberate fixture construction from an automatic development decision. Preserve explicit setup paths and re-record deliberate behaviour changes. |
| `Simulation.RefuseService`, `RefuseGate`, `World.CreateBuilding` | Separate explicit player-placement paths use kind, vacancy, budget and gate-edge checks, not Zone Rule admission today. Do not add a generic zoning gate to `CreateBuilding` and silently change those command contracts. New form/intensity controls must define their applicability before shell integration. |

### District land accounting

`HeldForTrade` still increments once per vacant trade Lot, using its derived common-use mask.
Operator and invariant retain the same units. Housing-only assembly does not settle trade
redevelopment: resolve how trade merging/splitting should affect that field before enabling it.

### Standing-housing discovery contract

`StandingHousing` indexes live declared housing Buildings in ascending monotonic Building-id order.
Full and abandoned Buildings remain candidates; the existing shared capacity, abandonment, balance
and preference checks decide availability. Creation, destruction and Ruleset adoption invalidate the
index; save/load rebuilds it. Repaint does not change membership. Household placement, both arrival
comparisons and relocation all use this index. Vacant zoned Lots no longer consume housing attempts.
This deliberately changes individual draws and replay hashes even without repainting.

Tests cover repaint to trade and unzoned ground with an existing resident and a remaining tenancy,
both arrival comparisons, identical relocation choices, demolition, shared form/intensity refusal,
save/load and derived rebuild.
Existing capacity, abandonment, affordability and reassessment tests remain active.

## Implemented geographic storage foundation

`World.PaintPermissions` and `World.PaintFormPermissions` select exact `LandRectangle` bounds.
`World.LandPermissions.At` reads one Tile; `Check` examines complete rectangular ground for use,
a single proposed form bit and uniform intensity, returning `PermissionRefusal` and a band.
Use admission keeps the existing any-matching-use-bit semantics. Form restrictions are explicitly
optional: an absent restriction admits any form, a present empty mask admits none. Form-only paint
preserves each Tile's use and band, including restrictions on otherwise unzoned ground.

`LandPermissionTable` owns the saved rectangles and packed permissions. It has no Lot handles.
The Cell directory and ascending-slot intrusive page links are rebuilt, including after save/load.
Painting counts every changed page before staging, caps column growth (including non-power-of-two
limits), retires all replaced pages before allocating replacements, and leaves saved allocator
identity unchanged for no-ops and refusals. An end-of-run invariant checks geometry, disjointness,
index coverage and the saved slot bound. Repeated clearing/repainting reuses the high-water slots.

`[land_permissions] max_records` defaults to the provisional 1,048,576, is retained when Rulesets
are copied, and refuses reload/load below the saved slot high-water mark. Physical column capacity
is allocation headroom, not saved state; existing high-water capacity is retained after clearing.
The [C# measurement](evidence/urban-fabric/permission-storage-csharp.md) records actual allocated
memory and the additional staging/growth costs.

Core save format is now **8** (Lot/Block permission summaries are derived); earlier versions are explicitly refused, with no inferred migration
from block/Lot paint. The Formats CitySave envelope stays unchanged. Appending the permission table
intentionally changes State Hash composition; the three golden outputs are re-recorded under the
existing procedure. The hash algorithm/seed and baseline Ruleset content hashes are unchanged.

Existing gameplay paint now uses this authority. `PaintUsePermissions` and `PaintBandPermissions`
preserve the other attributes; block painting preflights before allocating a Block. Command refusal
and application share the same read-only preflight. Abstract worlds without a Street lattice retain
their existing no-op block-paint behavior.

## Implemented read-only evaluation and local assembly

`LocalLayout.Evaluate` accepts explicit whole-Lot handles and a `LocalBuildingPlan`. It checks a
rectangular contiguous site along one Street face, vacancy (including the authoritative Building
references), depth, access, overlap and Address uniqueness. The entire site must admit housing and
the requested form under one intensity band. Floor area uses `BuildingPlan.TryFloorTiles`; housing
capacity subtracts the kind's own Business tenancy when one will actually be fitted. This initial
operation supports housing kinds only. Trade-site assembly remains deferred until its District land-accounting meaning is resolved.

The immutable proposal captures source identities and geometry, Street identity/epoch, Ruleset,
Tick, realised plan and capacity. `Simulation.CommitLocalLayout` is an internal Commit-phase
integration boundary, exercised directly by Core tests. It re-evaluates current state before any
mutation. It has no Input command or shell caller, no pending queue, and no evidence reservation.
Automatic construction will need to supply current housing evidence at this boundary.

Preflight checks allocator slot requirements and remaining monotonic ids for the Lot, Building,
Bins, Business, Rule Instances, Car Park and affected sparse Layer Cells before reserving capacity.
Expected refusal leaves saved and derived table state and allocator capacities unchanged; process
allocation failure still has the repository's crash semantics. Source Lots retire in monotonic-id
order. The new Lot retains the oldest source's Address, receives saved parcel/footprint/form/storeys,
and enters `World.CreateBuilding`. Frontage is rebuilt and admission invalidated; the common creation
path maintains Building residency, fitted contents, access and sealing. Ground permissions are never
collapsed into the merged Lot. Its Zone is the derived intersection of geographic uses, never standing-housing eligibility.

The existing severable condemnation-history reference stays stale after retirement, even if its slot
is reused. Normal save/load preserves realised geometry. Preview, parcel selection and painting use those
saved bounds. Street edits remove vacant unfronted Lots, preserve occupied Lots and their geometry,
and carve only genuinely free painted ground. Restoring a Street never repaints land or overlaps
standing merged sites. `RebuildParcels` now rebuilds frontage only. Assembly remains an internal API.

`LocalLayoutTests` makes both walkthrough branches executable. On a 64-Tile Street, five adjacent
12-by-16 parcels leave W/E standing. Two storeys give a terrace (`BackToBack`) 320 floor Tiles;
a courtyard on A+B gives 512. At 128 floor Tiles per tenancy these supply two and four tenancies.
The mixed-use version supplies three housing tenancies plus its own Business. Tests cover all four
Street faces, changed permissions, gaps, occupied/abandoned sites, stale geometry/handles/Street/
Ruleset/Tick, allocator exhaustion, immutable neighbours, severed history, save/load continuation,
derived rebuild, and twelve construction/removal cycles without slot growth. The continuation runs
with one versus two route workers and the Decide write guard enabled. These are explicit Core
proposals, not a demonstration of automatic housing choice or a visible shell capability.

Validation on 2026-09-18: `scripts/test.sh -- -m:1 --no-restore` passed 3,801 tests (35 new local-layout
cases), log `/tmp/borough-test-20260918-200244.log`. The working lane includes persistence, replay,
golden and route-worker equivalence coverage; no golden outputs changed. Formatting passed with
`scripts/format.sh --check -- --no-restore`. The instrument tier was not run for this slice.


## Implemented permission and realised-geometry readers

Construction and `Evidence.OfLot` share `World.ConstructionPermission`: the full saved parcel must
admit the use and realised form under a uniform admitting band. `LandPermissionSummary` separates
common uses from the diagnostic union and identifies mixed permissions/intensity. Headless CSV
appends those distinctions; the existing Godot zoning overlay draws actual permission rectangles.
The existing parcel brush selects saved merged sites. The permission-table allocation layout is
unchanged from the C# memory measurement; the separate standing-home index is bounded by the live
Building high-water mark. No new intensity/form controls or automatic
seeker-driven assembly are included.

The standing-home population change intentionally re-records both golden replay traces. The golden
world fixture now paints geographic ground explicitly. Save format 8 removes saved Lot/Block masks;
old-format saves are refused rather than inventing geographic permission from realised geometry.

Long-run findings: the 524,288-Tick market fixture now reaches a later Building high-water mark
(187 to 203) and a late rise in unpremised Businesses (tail-half means 0.1 to 15.9). It drains cash
into the treasury and does not establish equilibrium. Its checks now bound Building storage by Lot
storage and unpremised storage by Business storage, retaining per-reading invariants, conservation,
Bin/Rule peak checks and market-row tail checks. The evidence fixture's former 33-Day tail showed a
2.2 rise in worst reach-failure history against a 2.1 three-sigma band. Extending observation to 129
Days passed with the same 32-Day settling period and three-sigma threshold; this is behavioral
verification, not a quiet-machine performance claim.

Driven demonstration: `plans/evidence/urban-fabric/readers.drive` on `minimal.toml`, empty world,
1,000 fixture Citizens, start Tick 256, Debug Godot with a real display. Paint one frontage parcel,
populate, erase future paint, remove its Street, then restore it. Draw lists show one permission
rectangle becoming zero while the same Building id, transform and colour remain identical; Street count goes 1 → 0 → 1. The first
attempt at Tile (8, 8) selected open interior ground and correctly refused; Tile (8, 2) is on a
frontage parcel. Captures and readouts are in `/tmp/urban-reader-demo`. The dawn capture was too dark,
so the retained script uses daytime. Core tests separately exercise merged-site geometry and repeated
road restoration without overlaps or allocator growth.

Next: connect bounded individual housing evidence to candidate site/form evaluation and atomic
assembly. Keep explicit full-site permissions and occupied-neighbor preservation; trade assembly
and new shell intensity/form controls remain separate follow-ups.


Reader-migration validation on 2026-09-18: all **3,814 working-lane tests passed** with
`scripts/test.sh -- -m:1 --no-restore --no-build --logger 'console;verbosity=normal'`, log
`/tmp/borough-test-20260918-214702.log`. This includes replay/golden, save/load, derived rebuild,
route-worker equivalence, invariants and long-run assertions. Debug Godot built without warnings;
repository formatting and the final relocation-test whitespace check passed. The broader instrument
run exposed `ParkingScarcityTests` expecting the obsolete literal `parking = 8` in its unchanged
fixture, before it starts a simulation. That separate fixture repair is recorded on the backlog.

The pre-fix unfiltered run was stopped after 3,843 passes and five failures: four fixture/contract
failures subsequently covered by the clean working lane, plus the unrelated parking fixture above.
It is not a completed or green full-instrument result. The two relevant permission-allocation
instruments were then run on the committed code and both passed, reproducing **47,188,128 bytes**
at the record budget and **1,048,600 bytes** for oversized refusal without mutation; log
`/tmp/borough-test-20260918-215645.log`.

## Capacity-shortage integration

`[housing_construction]` opts housing Zone Rules into local construction. Omitted tables preserve
existing fixture behaviour; market-reading Rules retain District trade-Lot accounting. This is a
Core integration slice, with no new shell controls. `rulesets/urban-housing.toml` is an executable
mechanics fixture, not balanced content.

- Assess a bounded, distinct circular sample of current Unplaced Pool members, using its own counter
  hash purpose tag. Do not extrapolate the sample. Compare actual affordability and the shared
  placement utility against each Household's origin/saved Outside. A prospective tie with Outside
  supplies no positive evidence; an existing tie still covers demand.
- Completely inspect standing Building slots within authored Building and Lot high-water budgets.
  If either budget is exceeded, refuse before allocation. Missing a sampled vacancy never proves
  absence. Match site-qualifying seekers to usable existing tenancies with augmenting paths, so a
  flexible seeker cannot consume the only affordable home as evidence for another Building. Actual
  Business and Household occupancies share the same ceiling. Matching places and reserves nobody.
- Each form authors a frontage/depth envelope, storeys, symmetric setback and relative weight,
  independently of intensity. Its maximum floor-derived capacity must be earnable within the seeker
  limit and bounded surplus. Assembly still requires uniform geographic intensity and full-site
  permissions. Numeric intensity caps remain deferred.
- From the sampled Lot, extend a bounded window through consecutive vacant whole Lots in increasing
  Street coordinate. Generate singleton and contiguous merged sites within a capped number of
  form/site attempts, counting failed attempts too. Compare one- and two-Building arrangements,
  deduplicating identical realised alternatives. This bounded search is not an exhaustive block
  optimiser; longer arrangements and searching on both sides are later extensions.
- Maximise useful distinct seekers served; surplus earns no utility. Compare equal-usefulness
  alternatives with an authored weighted counter draw. Immediate standing neighbours add a Street
  wall alignment bonus and a no-larger same-form bonus; no founding identity is stored. These are
  provisional mechanics weights. Each possible first Building gets one draw entry, regardless of
  the number of hypothetical companions it could have. Reuse matching scratch across comparisons.
- Commit only the first Building. Revalidate exact source identities, geometry, authored form,
  permissions and current individual evidence before allocation or retirement. Subsequent samples,
  Rules and Ticks read its real capacity. No queue, cooldown or persistent reservation can resurrect
  already-covered demand. Temporary matching is rebuilt; preference episodes are saved separately.

Capacity and affordability shortages have no persistence delay, including first homes. Affordable
vacancies cover seekers in the scratch matching even below Outside until a substantial mismatch
has qualified. A current affordable home within the preference margin of Outside prevents that
seeker supplying mismatch evidence; a random choice loss is never enough. Selection currently
returns no proposal on refusal; `HousingNeedAssessment` exposes the structured
coverage/no-need/excess-capacity distinction for a specific proposal. A shell explanation for the
entire search is later integration work.

## Persistent preference mismatch

After a sampled Household fails an actual placement search, `HousingSearchEvidence` inspects all
standing Building slots within the existing coverage bounds. At most `max_seekers` drawn positions
per placement pass can contribute observations. Each affordable available home's utility is compared
with that Household's origin/saved Outside using the same rent, centrality and Life Stage terms as
placement. All must lose by at least `preference_margin_percent` hundredths of a utility unit.
No Outside or no choice model means no preference episode. A smaller gap is conservatively treated
as usable capacity; neither its probability of rejection nor a slightly better proposal establishes
persistent mismatch.

The Pool saves the last search reason and two unsigned Tick values: the first and latest qualifying
observation. Construction requires elapsed time **between those observations** to reach
`preference_persistence_ticks` and the latest to be at most `preference_freshness_ticks` old. A gap
larger than freshness restarts the episode. Same-Tick observations add no time. A different reason
clears the episode; Pool swap-removal carries every field with its Household, reentry starts clean,
and Ruleset adoption clears evidence gathered under the old comparison. An end-of-run invariant
checks reason and clock consistency. The save declaration advances Core format 8 → 9; old formats
are refused before reading their body.

Evaluation and selection remain read-only. They require a current complete comparison as well as
the saved episode, so a newly suitable home suppresses construction even before the next placement
observation. The proposed home must be affordable and strictly better than Outside. Real capacity
from an earlier commit covers later proposals immediately; there is no reserved future capacity.
Placement remains probabilistic and may end the episode by housing the Household at any search.

Tuning is provisional: the shipped mechanics fixture states persistence 1,024 Ticks, freshness
2,048 Ticks and margin 0.25 utility units. Tune freshness with placement's interval, revisit period,
Pool sampling and the observation budget: an infrequently sampled Household must restart after a
stale gap. The focused demonstration compresses these to 8, 4 and 0.10 to exercise boundaries.

The [construction validation](evidence/urban-fabric/housing-construction.md) records capacity-shortage
coverage. The [preference validation](evidence/urban-fabric/preference-mismatch.md) covers the new
saved episodes and continued automatic construction. The [playable neighbourhood](../examples/UrbanNeighbourhood/README.md)
exposes current comparisons and saved episodes in City Evidence and individual Household inspection.
Numeric intensity caps, larger arrangements, District trade assembly and intensity/form controls
remain separate follow-ups.
