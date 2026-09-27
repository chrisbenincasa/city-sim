# Commercial Lot types and trade forms

Board row: Commercial Lot types and trade forms. Evidence: [#51](https://github.com/chrisbenincasa/city-sim/issues/51), PR #62.

## Outcome

Trade blocks carry real commercial forms instead of housing parcels. Each form holds several
Units of different sizes, employs up to what its Units hold, parks according to its ground, and
is raised at world creation. The drawing receives the facts it needs to tell a strip mall from a
parade, an arcade or a department store.

## Current behaviour (checked 2026-09-26 at `16b3369b`)

- A trade block uses the housing carve. `BlockPatterns.ForBand` picks the pattern from the density
  band and ignores the zone. In `pictured.toml` a trade Lot is a 4 × 6 Tile parcel with a 2 × 3
  footprint on 2 storeys.
- Every tenancy is `floor_tiles_per_occupant` of floor (25 Tiles, 400 m², in `pictured.toml`).
  A Business employs its tenancy share over `floor_tiles_per_job` (`World.cs:7660`). That gives
  about 8 jobs per Business at any building size.
- The populator raises dwellings only (`SyntheticCity.RaiseDwellings`). The trade Zone Rule builds
  a shop only when Households starve on a District market, so `pictured.toml` has 8 posts for
  2,000 Citizens.
- A Car Park is a Building's parking at one Address. Its capacity is floor over
  `floor_tiles_per_parking_space`, and nothing draws it.
- The Lot table saves the parcel and footprint as separate rectangles (`LotTable.cs:75-82`). The
  carve centres the footprint with a symmetric setback.
- `BuildingFacts` gives the drawing frontage, depth, storeys, pattern, zone, side, face and raised
  day. It gives no parcel, unit or parking facts.
- Issue #61: a non-housing kind with one tenancy never starts its trade (`World.cs:5076`).

A Tile is 4 m, so one Tile of floor is 16 m².

## Survey

Sizes are typical ranges from general knowledge, not measurements.

| Type | Band | Storeys | Units × size | Parking | Catchment | Place in the city | Decision |
|---|---|---|---|---|---|---|---|
| Corner shop | Low–mid | 1–2, flat above | 1 × 50–150 m² | None | Walk-in | Residential corner | Shop-house parade |
| High-street terrace | Mid | 2–4, flats or offices above | 5–20 × 50–200 m² | Street and shed | Walk-in | High street | Shop-house parade |
| Parade with service road | Low–mid | 2–3 | 6–12 × 80–200 m² | Front strip | Walk and drive | Suburban main road | Family of car-park centre |
| Strip mall | Low | 1 | 5–15 × 100–300 m² | Surface, in front | Drive-to, local | Arterial side | Car-park centre |
| Neighbourhood centre | Low | 1 | Anchor 2,500–4,500 m² + 10–20 small | Surface | District | Suburban junction | Car-park centre |
| Big box | Low | 1 | 1 × 5,000–15,000 m² | Surface | Regional | Edge | Car-park centre |
| Power centre | Low | 1 | 3–8 × 1,000–5,000 m² | Shared surface | Regional | Edge | Car-park centre |
| Outlet centre | Low | 1 | 20–80 × 100–400 m² | Surface | Regional | Edge | Family of car-park centre |
| Sales-yard trade | Low | 1 | 1 × 500–5,000 m² + yard | Surface + display yard | Drive-to | Arterial side, edge | Sales-yard trade |
| Petrol station | Low–mid | 1 | 1 kiosk | Forecourt | Passing traffic | Main road | Pad site |
| Drive-through | Low | 1 | 1 × 200–400 m² | Drive lane | Passing traffic | Front of a centre | Pad site |
| Town supermarket | Mid | 1–2 | 1 × 2,000–5,000 m² | Deck or roof | District | Town centre | Town supermarket |
| Precinct / arcade | Mid | 1–3 | 20–60 × 30–200 m² | Deck behind | District | Town centre | Precinct / arcade |
| Market hall | Mid–high | 1 | 50–200 stalls × 5–15 m² | None | Walk-in | Town centre | Market hall |
| Open-air market | Any | 0 | Stalls on open ground | None | Walk-in | Square | Later |
| Department store | High | 3–6 | 1 × 5,000–30,000 m² | Shed | Regional | High street | High-street block |
| Enclosed mall | Low–mid | 1–3 | 50–200 + 2–4 anchors | Surface + decks | Regional | Edge | Later |
| Mixed-use podium | High | 1–3 + tower | 5–30 + flats or offices above | Underground | Walk-in | Centre | Later |
| Office park | Low | 2–4 | Floors × 500–2,000 m² | Surface | Commute | Edge | Office row |
| Office block / tower | Mid–high | 4–40 | Floors × 500–2,000 m² | Deck or underground | Commute | Centre | Office row |
| Workshop estate | Low | 1–2 | 10–30 × 100–500 m² + yards | Yard | Drive-to | Edge | Workshop row |
| Warehouse | Edge | 1 | 1 × 10,000 m²+ | Lorry yard | Freight | Edge | Freight row |
| Hotel / motel | Any | 2–20 | Rooms | Surface to underground | Visitors | Any | Later |

Housing types differ mainly by density. Commercial types differ along four axes: density,
catchment, parking arrangement and ground use (parking, sales yard, market square).

## Decisions

### Kept types

A type is kept when it changes jobs, trips, parking, catchment, land take or street character.
Types that differ only in looks are Appearance Families of a kept type.

| Band | Retail types (this row) | Workplace types (other rows) |
|---|---|---|
| Low | Car-park centre, pad site, sales-yard trade | Office park, workshop estate |
| Middle | Shop-house parade, town supermarket, precinct / arcade, market hall | Office block |
| High | High-street block | Office tower |
| Later | Enclosed mall, mixed-use podium, open-air market, hotel | Warehouse |

- A strip mall, neighbourhood centre, big box and power centre are one type. Their unit mix
  differs, and the drawing reads it.
- Pad sites are ordinary Lots with their own frontage.

### Units

- A **Unit** is one tenancy's space inside a Building. It has its own rectangle within the
  footprint, a first storey, a storey span, a facing side and an anchor flag.
- A Building of a commercial form holds a list of Units. A Business's premises name a Building and
  a Unit.
- Units are stored as an intrusive list per Building in flat arrays.
- Placement and eviction match a Business to a Unit, because Units stop being interchangeable.
- A Building of an existing kind, such as a terraced `dwelling`, `shopfront` or `officeblock`,
  holds one Unit per tenancy. Each Unit is an equal slice of its floor, and Households and
  Businesses still compete for the same tenancies. The slice's storey and side carry no meaning
  until the shop-house form replaces trade in terraces.
- A shop-house has one ground-floor Unit. Its upper storeys house Households at
  `floor_tiles_per_occupant`.
- CONTEXT.md's Building entry notes that GlassBox used "Unit" to mean a Building. The new entry
  must say that Unit here means a tenancy's space.

### Jobs

- A Business's posts are its Unit's floor over `[capacity] floor_tiles_per_job`.
- The real density is one worker per floor Tile (16 m²). The UK Employment Density Guide (2015)
  gives high-street retail about 15–20 m² per worker.
- `pictured.toml` states `floor_tiles_per_job = 1`. Its shopfronts stand on setback parcels of
  real size.
- Every other Ruleset keeps 3, and the key stays for now.
  - Their floors are about 4× a real building's (`minimal.toml`'s header), and 3 compensates.
  - At 1, those worlds reached full employment. Checked 2026-09-26 on `insolvent.toml` at 2,000
    Citizens over 20 Days: 2,000 employed against 1,824, and 0 short paydays against 27. Workers
    filled the till-less dwelling shops, so no grocer paid wages or went bankrupt.
  - That broke the insolvency, business-pool (`levied.toml`) and evidence (`diagnosed.toml`) tests.
- Retire the key once floor areas are realistic. That is the carve defect in `plans/0053`.
- The founder is still the first worker (adr/0146).
- Posts that follow a Business's daily margin belong to the Employment feedback row. When that
  lands, the ceiling here becomes the most a Unit can hold.
- In `pictured.toml` the rate has nothing to act on until phase 2. Over 20 Days, 1,023 of its
  1,025 Businesses were founded with no premises, each employed only its founder, and the job
  pass assigned nobody.

### Type choice

- The density band gives the set of types a block allows.
- A seeded draw per block picks one type from that set, using the world seed, the block and a
  `purpose_tag`.
- A block beside an Arterial favours pad sites and car-park centres.
- A District with no seller of a Good favours a market hall or a town supermarket.
- A zone may forbid a type, but a zone never places one.
- No Ruleset key sets these weights.
- `pictured.toml` declares no bands, so every trade block becomes a car-park centre.

### World creation

The populator raises each trade block's form and fills its Units with Businesses, as it raises
dwellings now.

### Storeys and parking

- Storeys follow the band ladder, as housing does through `storeys_per_rung`.
- Parking builds up first and down only when necessary.
  - Low band: surface parking.
  - Middle band: a deck, with one level per band rung.
  - High band: underground, which is deferred.
- Capacity is the number of stalls in a derived layout: 2.5 × 5 m stalls, 6 m aisles, double rows
  perpendicular to the Unit row. The drawing paints the same stalls.
- There is one Car Park row per Building.
- Housing keeps `floor_tiles_per_parking_space`, which is a parking minimum.
- A commercial parking minimum would be a later Policy. It needs its own number, because CONTEXT.md
  says commercial and residential parking are balanced separately.

## Geometry contract

| Type | Carve | Footprint | Units | Ground uses | Exists today |
|---|---|---|---|---|---|
| Car-park centre | New `BlockPattern` member: one Lot per block fronting one face; the other faces carry no Address | Against the rear of the parcel | Row split by `BlockPatterns.Widths` on an 8 m (2-Tile) bay; the anchor takes half the row; a big box takes all of it | Surface car park between the footprint and the street | Parcel and footprint storage, `Widths`, Car Park rows |
| Pad site | Corner Lots, about 8 × 8 Tiles, carved from the front of a centre block | Centred | 1 | Forecourt or drive lane | Ordinary Lots |
| Sales-yard trade | One Lot fronting one face | At one side of the parcel | 1 | Yard, typed by the Building kind | No |
| Shop-house parade | Existing perimeter and back-to-back carves | Existing | 1 per ground floor, 1 or 2 Tiles wide | None | Carve and kinds |
| Town supermarket | Half-block or whole-block Lot | Front of the parcel | 1 anchor + optional small Units | Deck | No |
| Precinct / arcade | Whole-block Lot | Two rows either side of an internal walkway | Units face the walkway or the street | Walkway, deck behind | No |
| Market hall | Half-block Lot | Hall shell | Grid of 1-Tile stalls facing inward | None | No |
| High-street block | Perimeter carve | Existing | Anchor spanning several storeys, small Units around it | None | Carve |

Rules shared by every form:

- Row depth is 6 Tiles (24 m) for small Units and 10 Tiles (40 m) for an anchor or big box. The
  footprint stays one rectangle, and a deeper anchor waits for a second rectangle.
- Unit mix and front face come from seeded draws per block. No `[[trade_form]]` table is added.
- Every form has one Address and one Access Point. Customers who drive park in the Building's own
  Car Park, which the Parking Shed finds first.
- Ground-use rectangles are derived from the parcel and footprint. They are never saved.

## Appearance Family contract

The graphics session builds against these facts. Only facts fixed when a Building is raised pick
a family (adr/0173).

| Fact | Content | Source |
|---|---|---|
| Form | One of the eight types | New |
| Parcel | Rectangle, plus the footprint's offset within it | `LotTable.Parcel*` |
| Units | Rectangle within the footprint, first storey, storey span, facing side, anchor flag | New unit table |
| Ground uses | Car park with stall layout, deck with levels, yard with its use, walkway | Derived |
| Corner | The footprint meets two street faces | Derived |
| Storeys, Side, Face, RaisedDay, Kind | As today | Existing |

The live facts are these:

- A Unit is let or vacant.
- A Unit's Business is open or closed.

Live facts drive dressing, such as shutters, lit signs and dark windows. They never pick a family.

The new `preset.toml` filters are `forms`, `units` (a count range), `anchored`, `corner` and
`parking` (surface or deck).

| Type | Families | Modules |
|---|---|---|
| Car-park centre | Plain flat-roof strip, covered-walkway strip, themed gable strip, tilt-up big box, outlet village | 8 m storefront bay, anchor front, end caps, parapet, canopy, stall paint, islands, lamps |
| Pad site | Petrol forecourt, fast-food pavilion, bank drive-up | Canopy and pumps, kiosk, drive lane, menu board |
| Sales-yard trade | Car showroom, garden centre, builders' merchant | Glass showroom, glasshouse, racked shed, yard kit per use |
| Shop-house parade | Victorian, interwar, 1960s flat-roof, contemporary infill | 4 m and 8 m shopfronts, corner entrance, upper storeys from the dwelling families |
| Town supermarket | 1980s brick shed with deck, modern glazed with rooftop parking | Supermarket front, deck level, ramp |
| Precinct / arcade | Glazed Victorian arcade, 1960s open precinct, modern galleria | Glass passage roof, upper walkway, arcade shopfront |
| Market hall | Iron-and-glass hall, 1960s concrete hall, stall shed | Hall shell, stall kit |
| High-street block | Edwardian stone store with corner dome, interwar moderne, 1960s curtain wall | Department-store bays per storey, corner feature |

Models support these dimensions:

- Bay widths in multiples of 4 m.
- Row depths of 24 m and 40 m.
- 1 to N storeys at the dwelling storey height.
- Deck levels at a fixed floor-to-floor height.
- Rows up to the block's net frontage, about 120 m in `pictured.toml`.

Each Unit's door is drawn at the centre of its facing side. Doors are drawn only, and the
simulation keeps one Access Point per Building. Models are authored in Blender first, following
[the authoring procedure](../docs/07-the-drawing.md#building-authoring-procedure). Simulation
geometry owns footprint and access.

## Phases

| Phase | Work | Acceptance |
|---|---|---|
| 1. Units and the jobs ceiling | Unit table, premises name a Unit, posts from the Unit's floor, shop-house ground-floor Unit, CONTEXT.md Unit entry. `pictured.toml` at one worker per Tile is done. | Replay, save/reload and thread-count equivalence hold. `DerivedRebuildAuditTests` passes. Goldens are re-recorded deliberately. |
| 2. Car-park centre | New block pattern, rear footprint, Unit row, stall layout and capacity, world-creation raising, a new meaning for `DistrictWatershed.HeldForTrade` | `pictured.toml` at 2,000 Citizens employs a large share of working-age Citizens at world creation. A driven screenshot shows centres with their car parks. |
| 3. Facts for the drawing | Form, parcel, Units, ground uses, corner and live Unit facts in `BuildingFacts`, and the new preset filters | The graphics session can draw a test preset of centres. Drawn stalls equal capacity. |
| 4. Type choice | Band set, seeded draw, Arterial and missing-seller nudges, zone exclusion; shop-house parade, town supermarket with deck, high-street block | A banded Ruleset shows every kept type in its bands. |
| 5. Remaining types | Pad sites, sales-yard trades, precinct / arcade, market hall | Each type appears in a fixture world and draws with a family. |

Deferred: enclosed mall, mixed-use podium and underground parking with a Building above, open-air
market, hotel, and a commercial parking minimum Policy.

## Interfaces

| With | Interface |
|---|---|
| Employment feedback row | That row owns posts that follow margin, the opening staff, hire and cut thresholds, anchor catchment and re-letting vacant Units. This row supplies the Unit and its ceiling. |
| Office and agglomeration row | Office types reuse Units, the ceiling and band storeys. Margin staffing needs office income from that row. |
| Building entrances and usable open ground | Ground uses are the first typed use of leftover lot ground. Doors per Unit are drawn only, so door count stays separate from how many people a Building handles. |
| Issue #61 | Commercial forms always have more than one Unit, so they avoid the one-tenancy bug. The general fix in `World.CreateBuilding` still lands on its own. |
| Commute Budget failures ([#63](https://github.com/chrisbenincasa/city-sim/issues/63)) | The two prototypes that filled trade blocks logged 7,416 and 12,781 commutes over the Commute Budget in 20 days. The cause is undiagnosed. Phase 2's acceptance run must diagnose it first. |
| Urban fabric layout contract | Merging trade Lots changes `DistrictWatershed.HeldForTrade`. Phase 2 settles its meaning first. |

