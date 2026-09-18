# 0062 — Local layout storage and commit contract

Implementation design for the [redevelopment walkthrough](urban-fabric-redevelopment-walkthrough.md).
This is a recommended contract, not implemented code. It preserves the decisions in
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
site crosses a form restriction or still holds a Building. Full construction-evidence accounting
arrives in the integration slice; test proposals exercise the same site/commit contract without
pretending that housing choice has been implemented.

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

Traced at `ebab65f`. Searched direct Lot/Block zone and band access, permission setters, band
admission and all `LotsAdmitting` consumers across Core, Godot and Headless. This is a migration
map for this slice, not a new backlog. The findings below are code traces; the suggested fixes are
not implemented.

| Reader or writer | Meaning and required migration |
|---|---|
| `ZoneRuleEngine.Admits`, `World.BandAdmitting` | Future construction. Replace Lot/block summaries with complete proposed-site permission checks. Keep Zone Rule sampling independent of admission, so repainting cannot prevent an existing Building's decline assessment. |
| `PlacementEngine.TryHouse` | Standing housing search. Stop deriving eligibility from future-development paint; discover actual declared housing Buildings, then apply current capacity, abandonment, affordability and preference checks. |
| `PlacementEngine.ProspectCrosses(int, int, Ticks)` and `Compare(ArrivalProspect, ...)` | Both legacy gate admission and current prospect comparisons sample housing-painted Lots. Migrate both to the same standing-housing discovery contract; neither should advertise empty zoned ground as a home. |
| `PlacementEngine.PrefersSomewhereElse` | Moves by already-housed Households. Although excluded from the initial construction signal, this existing search must use actual housing availability rather than future land permission. |
| `PlacementEngine.Tenant`, `World.HasRoomForPremises`, `World.HasRoomForHousehold` | Existing Building use. The Business search already samples Lots and checks the standing Building's declared capability/capacity without a zone gate. Preserve that separation for both kinds of occupant. |
| `ZonedLots`, `World.RebuildDerived` and invalidation sites | Currently a cache of painted Lots, including empty ones. Keep any land-discovery cache separate from a standing-housing index. Define rebuild/invalidation for construction, destruction, abandonment, changed kind declarations and capacity changes according to what the new index stores. |
| `LotSubdivider.PaintAt`, `PaintParcelAt`, `World.ZoneBlock`, `BandBlock` | Write geographic permissions through one authority. Painting must not rewrite realised geometry or invalidate standing housing solely because paint changed. Whole-block operations and fixture setup use the same permission write contract. |
| `LotSubdivider.Preview`, `SubdivideBlock`, `RecarveBlock`, `Resubdivide` | Geometry and future permission are currently combined through one block pattern. Preview realised Lots; initial carving and local assembly query actual ground. Road edits preserve permissions where empty Lots disappear. |
| `DistrictWatershed.HeldForTrade`, `ComponentOf`, `WorldInvariants.HoldsGroundForTrade` | Actual future-land meaning: vacant trade Lots contribute to District formation near built ground and supply a road component. Migrate the operator and invariant together, preserving this deliberate vacant-trade behaviour. Do not replace it with standing Businesses alone. |
| `Evidence.AdmittedByAnyRule`, `Evidence.Lot`, `LotEvidence` | Future-construction explanations. Share the read-only permission refusal logic with construction rather than duplicate a weaker check. Describe mixed geographic permissions without implying one mask fully represents the site. |
| `Main.Zoning` overlay and brush previews, `Main.Readout` | Draw/read geographic permission independently of realised Lot and Building geometry. Current overlay falls back to block paint where no fronted Lots exist; that must become actual painted ground. Parcel selection uses current parcel bounds. Requires a driven visual check when implemented. |
| `KindDump` painted-mask aggregation and `ZoneDump` Lot output | Diagnostics of future permission, not proof that a standing kind is unusable. Keep the meaning explicit and represent mixed ground without substituting a permissive union for site admission. |
| `SyntheticCity` initial housing selection and band/zone setup | Fixture construction intent. Populate geographic permissions before creating Lots/Buildings; distinguish deliberate fixture construction from an automatic development decision. Preserve explicit setup paths and re-record deliberate behaviour changes. |
| `Simulation.RefuseService`, `RefuseGate`, `World.CreateBuilding` | Separate explicit player-placement paths use kind, vacancy, budget and gate-edge checks, not Zone Rule admission today. Do not add a generic zoning gate to `CreateBuilding` and silently change those command contracts. New form/intensity controls must define their applicability before shell integration. |

### Confirmed couplings and omissions

**Repaint can hide a standing home from search.** `PaintAt`/`PaintParcelAt` change `Lots.Zone` and
invalidate `LotsAdmitting`; `ZonedLots.Rebuild` indexes only the new painted bits. All four Household
search paths above draw from its Housing list. `Consider` would still admit a declared, affordable
home with capacity, but never sees it if the new paint excludes housing. Existing repaint tests
assert that Buildings remain and that the painted index updates; they do not assert that the
standing homes remain discoverable. This is current behaviour, not just a hypothetical migration
risk. Keep the correction within this slice and add direct placement/arrival/reassessment checks.

**Vacancy explanations use a weaker permission check.** `Evidence.AdmittedByAnyRule` checks only
`Lots.Zone & ZoneRule.Admits`; construction also checks `World.BandAdmitting`. The current evidence
test covers an unpainted use bit, not a use allowed by paint but refused by its band. Add that case
before extending the explanation to form restrictions. This identifies an omitted refusal reason,
not a claim that every band-refused Lot is currently labelled healthy or built.

**Parcel counts have a District consequence.** `HeldForTrade` increments once per vacant trade Lot,
not per area of painted land. Local merging/splitting can therefore change this signal without
changing the ground's permission. The first housing-only geometry demonstration does not settle
trade redevelopment. Preserve the current signal during the permission-storage migration and
explicitly resolve its geometric meaning before admitting trade-site assembly; do not silently
switch to Tile counts, which would change the field's units and tuning.

### Standing-housing discovery contract

Prefer a derived list of live, declared housing Buildings, independent of future paint. Keep the
existing per-candidate capacity, abandonment, balance and preference checks in one shared path.
Whether abandoned/full Buildings remain in that list affects search sampling, so define membership
and sampling order explicitly rather than claiming this index substitution preserves behaviour.
Use deterministic ordering and maintain/rebuild the index from actual Building state and Ruleset
declarations. Household placement, both arrival comparisons and relocation use it consistently.

Tests must cover a partially occupied housing Building repainted to trade and to unzoned land:
the Building and existing occupants remain, a new seeker can still consider a vacant tenancy,
while new housing construction on that ground is refused. Also cover absent, non-housing, full,
unaffordable and abandoned Buildings, Ruleset reload, demolition, and save/load. Do not turn every
occupied Lot into a housing candidate or treat losing frontage as a new permission restriction.

This change deliberately alters candidate populations and therefore may change draws and State
Hashes even without repainting: empty zoned Lots previously consumed search attempts. Existing
`PlacementTests.A_city_of_mostly_empty_lots_houses_slowly` specifically exercises that behaviour.
Review that expectation and resulting placement pace; preserve bounded individual sampling rather
than claim that all historical test expectations remain applicable. Re-record golden behaviour
deliberately after the chosen index contract is implemented.

The reader audit is complete for the direct accesses and index consumers above. The storage model
now supplies a provisional record budget and the preflight sequence. Next implement the permission
table and exact painting/query operations with Core refusal, allocator, hash and save/load checks.
The C# memory measurement and final API signatures belong to that implementation; this document
does not claim the Python prototype proves them.

Baseline verification on 2026-09-18 at `ebab65f`: 57 tests passed with
`scripts/test.sh --filter '(FullyQualifiedName~PlacementTests|FullyQualifiedName~PlacementChoiceTests|FullyQualifiedName~BandAdmissionTests|FullyQualifiedName~ZonedLotsTests)&tier!=instrument'`;
26 passed with
`scripts/test.sh --filter '(FullyQualifiedName~ZoningPaintTests|FullyQualifiedName~EvidenceTests.A_lot_the_assembler_calls_unzoned_is_a_lot_nothing_builds_on|FullyQualifiedName~DistrictWatershedTests)&tier!=instrument'`.
These exercise existing behaviour. No production code or regression tests were changed for this
audit; the repaint/search and band-explanation findings follow from the traced predicates, not a
new end-to-end reproduction. The migration checks above still need implementation.
