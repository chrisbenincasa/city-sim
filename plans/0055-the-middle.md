# 0055 — The Middle

Scoped and landed 2026-09-03, against `the-block` at `6e7d0f1`. **The city grows outward from its own
middle, and it never did before.**

⚠ **This is the generator half of [`0049`](0049-visuals.md) row 4.** The drawing half — a Building's
height being scrambled after the city had decided it — landed separately at `6e7d0f1` and is
[`0049`](0049-visuals.md) **F42**. Read that row for the picture; this document owns the city under it.

---

## The finding

**`SyntheticCity.Subdivide` raster-scanned the block lattice from its south-west corner and stopped
when it had housed everybody, so a city was a filled rectangle with one ragged row along its top.**
Two things followed from that and neither was ever decided.

| What it produced | Why it is wrong |
|---|---|
| **A city with no middle** | [`0053`](0053-the-block.md) gave the block a **density band** and `BandAt` painted the bands by Chebyshev reach from the box's centre — a geometric ring. But the walk filled from a *corner*, so the dense rings were the ones the walk reached **last**, and a city that stopped early never carved them at all |
| **A city with a seam** | The row the walk stopped in is half filled, and a half-filled row of blocks carries density maxima of its own. ***That seam has been reported as a finding about the city three times*** — see **F3** |

🔴 **The band half is the one that matters, because [`0053`](0053-the-block.md) shipped five rungs and
no shipped world ever crossed more than three of them.** `rulesets/platted.toml` declares one band per
rung and its header describes a five-ring city; no run of it since 2026-09-02 has produced one. That is
[`0049`](0049-visuals.md) **F43** and **F44**, and it is filed as a correction owed in
[`0012`](0012-corpus-audit.md).

---

## What shipped

Three changes, all in `SyntheticCity`, all hash-bearing.

| # | Change | What it is |
|---|---|---|
| **1** | **`Subdivide` walks Chebyshev shells outward from the middle** | `Shell(centre, ring, step)` numbers each shell from its south-west corner and the walk takes them in order. The stopping rule is unchanged — `room >= wanted` — so what moved is *which* blocks a city that stops early has, and not how many |
| **2** | **`BandAt` reads PROGRESS and not geometry** | It was `ring = reach × bands / (half + 1)` — a fraction of the *box*. It is now `crossed = room × bands / wanted` — a fraction of the *population housed*. ⚠ **So each band houses an equal share of the people and the rings are UNEQUAL**, which is the correct way round: a dense band holds more people per block, so it is a smaller ring |
| **3** | **`IsTradeBlock` counts round the ring** | It was an anti-diagonal keyed to the map's origin. It is now `Step(Δeast, Δnorth) % TradeBlockStride == 0`, where `Step` is `Shell` read backwards — so a shell of `8r` blocks yields exactly `r` to trade, at every radius |

⚠ **`Step` and `Shell` are a pair with no test between them.** What checks them is that the trade share
comes out at the stride, which is asserted on a real lattice rather than on the arithmetic.

---

## The findings

### F1 — The bands were painted on ground the walk never reached

**`banded.toml` at 4,000 Citizens: the densest band is 6 blocks of 33, and the trade diagonal missed
every one of them.** 24 trade Lots, all of them suburban. The geometric `BandAt` put the dense rings in
the middle of the *box*; the corner walk filled from the outside of it. ***Two mechanisms both derived
from the same lattice and neither knew the other's orientation.***

### F2 — A land-use split that changes with the city's size is not a split

Phase-locking the trade diagonal to the new middle fixed **F1** and broke the share. ***A straight line
crosses a small square ring far more often, per block of its perimeter, than a large one*** — ring 1
gave **2 blocks of 8** where the stride says one in eight. Measured on `provisioned.toml` at 2,000
Citizens, **16 trade Lots before and 72 after**, on a city of 42 blocks where the stride promises about
five.

⚠ **A Manhattan radius was tried first and is the wrong shape entirely.** It turns the stride into
concentric rings of shops, so a city whose radius is under the stride gets exactly **one** trade block:
`banded.toml` at 4,000 Citizens — 36 blocks, radius 3, stride 8 — came out at **2 trade Lots in the
whole city.**

**Counting round the ring is exact at every radius.** Measured after: `provisioned.toml` 64 trade Lots
of 40 blocks, `minimal.toml` 48 of 37, `banded.toml` 36 of 36, `platted.toml` **61 of 68 blocks — 13%
against the stride's 12.5%.** ⚠ **The small-city excess is one block and it is the middle one**, which
is trade by construction because it is step 0 of ring 0.

### F3 — The ragged band was the scan order showing through the world

🔴 **`twinned.toml` had a documented, twice-re-swept, non-monotonic band in which it measured more
concentrations than it has: 200 → 2, 300 → 3, 400 → 4, 500 → 3, 600 → 3, 800 → 3, 1,000 → 2.** Its
header carried a paragraph about it, `BuildingDensityFieldTests` carried a fact pinning it so that
nobody would mistake it for a defect, and the band's lower edge had already moved once at
[`0053`](0053-the-block.md) when the Lots per block changed.

**Re-swept 2026-09-03 across 31 sizes from 100 to 64,000 Citizens: it is 2 at every single one.**

***It was never a property of that world.*** A lattice that has not filled its ground is now a smaller
filled lattice rather than a ragged one, so it has one candidate centre and not several. The fact is
kept as its inverse — it now asserts **two at every rung of the ladder**, which is the property the
world exists to have and is what would go red if a future walk left ragged edges again.

⚠ **The old fact's own instruction was *if this ever reads 2, re-sweep it and update this and the
file's header rather than deleting this*.** Both were done. **It also replaced a `[Theory]` at 200 /
4,000 / 16,000 / 64,000 rather than joining it** — every rung of that fact is on this ladder, and two
statements of one claim is [`0012`](0012-corpus-audit.md) **Cause 1** by construction.

### F4 — A watershed test had never once asserted the property it named

🔴 **`DistrictWatershedTests.A_districts_centre_is_its_densest_cell` read `BuildingsInCells.Density`.
The flood climbs `Buildings + vacant trade land`.** Two different surfaces, and they ordered the Cells
the same way for exactly as long as trade land was scarce enough not to matter. **F2** quadrupled it and
both of `twinned.toml`'s Districts came back with a centre of Building-density **7** holding a Cell of
**8**, one Cell east.

**The flood was right in both.** ***A test comparing an answer against a field the answer was not
computed from is not a check, it is a coincidence with a good record.*** The repair is one definition
rather than one fix: `DistrictWatershed.Field` is now public and `Collect` is its own first caller, so
the test and the flood cannot disagree about what they are looking at.

⚠ **The new assertion is a NEW fact and not a repaired one**, and what it guards is the watershed's
central post-condition: a Cell drains uphill, so the seat it drains to cannot be lower than it is.

### F5 — A conservation leak that could not fire while the city sat in a corner

🔴 **`ParkingOccupancyIsConserved` failed, and the cause was a Citizen on two journeys at once.** An
immigrating Citizen is never marked as travelling, so `CommuteEngine.Travel`'s `Activity` guard — whose
own comment says it exists to stop *a second Trip under the first* — let the commute roster start a
Trip under an in-flight Immigration Trip. The two journeys' parking releases and takes interleave and
one space is orphaned.

⚠ **It could not happen before this plan** because the city sat in the map's origin corner, two Gates
were zero minutes away, and an Immigration Trip completed on the Tick it started. ***A defect that
needs a journey to take longer than a Tick is invisible in a city with no distances in it.***

The repair is a fifth `CitizenActivity` — **`Travelling`**, for a journey that is not the commute — set
by `TripEngine.Start` for anything that is not already a commute leg, and cleared by
`World.RecordTripFate`. `Borough.Headless --arrivals` counts it.

⚠ **Nothing structurally refuses a second Trip.** Only `CommuteEngine` asks the `Activity` column, so
Shopping and School remain ***unbuilt*** rather than ***refused*** under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md). That absence is named
in `CitizenActivity`'s own remarks and not here.

### F6 — Four tests were resting on the scan order, and each of them said so differently

**None of these is a fixture that drifted. Each is a test whose subject was the raster walk.**

| Test | What it was actually testing | What it tests now |
|---|---|---|
| `MarketLongRunTests.The_shop_count_is_bounded_by_the_land` | Its non-vacuity guard read *shops × 4 ≥ trade Lots*, which held only because `provisioned.toml` happened to carve about eighteen trade Lots. **F2** made it 64 and the guard failed with nothing about the market changed | **The count CLIMBED** — 2 shops at the first reading and 13 at the last, still rising. And the ceiling is shown reachable **across the arms**: `oversupplied.toml` closes at **64 shops on 64 trade Lots**, exactly pinned |
| `TripDumpTests.A_severed_city_reports_pairs_with_no_route` | One population inside a band that was chosen as *the middle, so a test here is not one small change away from an edge*. **There is no middle.** Re-swept: 1,000 → 0, 2,000 → 16, 3,000 → 20, 4,000 → **0**, 6,000 → 111, 8,000 → 153, 12,000 → **0**, 16,000 → 951 | **A ladder of eight sizes**, asserting the file can sever and checking every rung for a disagreeing verdict |
| `ServiceCapacityTests.The_place_goes_to_the_nearest_family_and_not_the_oldest_household` | Its guard needs distance order and slot order to disagree, and got that as a **side effect** of the generator laying Households down a raster. Both ends of the widest axis are now equally old, and Household 0 came back as both the nearest applicant and the lowest-slot one | **The site is named after what it is for** — `OnTheNewestFamilysDoorstep`, the vacant Lot nearest the highest-slot housed Household |
| `CommuteDumpTests.The_before_is_a_city_with_no_jobs_and_the_after_is_not` | `DoesNotContain("0 are within a")`, which reads a zero out of the middle of **"10 are within a"**. It held while the balanced count never ended in a zero | The comma. ***A substring is not a field.*** |

⚠ **`GatePlacementTests` and `DistrictReevaluationTests` moved for the ordinary reason** — a compass
point and a fixed Cell that no longer name what they used to — and are re-sited on the mechanism rather
than on a coordinate. `GatePlacementTests` now counts admitted against refused on the sieve: west 39.7,
south 39.7, north 48.9, east 52.9 clock minutes, where it read west/south 0 and east 62, north 73.

### F7 — There is nowhere far away to put a golden command, and that is the finding

🔴 **The golden session's thirteen Zone commands carved 36 Lots where 93 were expected**, because every
one of them named a block inside the square the populator now fills. **The first repair translated the
whole set by `(+16, +14)` and carved ZERO.**

**`SyntheticCity.PavedTiles` sizes the lattice to what the population needs and no more.** At the
session's 4,000 Citizens the paved lattice is **11 blocks a side and the populator holds 9 of them**.
***The margin those commands need does not exist on the map; it exists in the ring order.***

So they sit in the **outermost ring** — column 0 and row 0, Chebyshev distance 6 from the middle —
which is the last ground the walk would reach, and the only ground whose safety is a property of the
algorithm rather than of a measurement. **Both road-edit blocks are on row 0 on purpose**: the south
face of a row-0 block is the map's own boundary street, so bulldozing it takes no neighbour with it —
which is the hazard `GoldenSessionCoverageTests` was written about, and this is the one siting that
cannot suffer it.

⚠ **Three previous occasions moved a command because the populator *reached further*.** This one moved
because the populator ***moved***, and the answer was never a bigger margin until the frontier stopped
creeping in one direction and started growing in all four.

---

## What it cost

| | |
|---|---|
| **Hash movement** | Both golden traces, re-recorded. `session.borough` rewritten from the fixture. ⚠ **No Ruleset content hash moved** — `declining.toml`, `declining-tuned.toml` and `congested.toml` were not touched |
| **Ruleset headers corrected** | `banded.toml` and `platted.toml` (the ring width no longer falls out of the lattice's half-span; each band houses an equal share, so the rings are unequal), and `twinned.toml` (**F3**) |
| **Tests re-sited** | Nine, all named in **F3**, **F4**, **F6** and **F7**. ⚠ **One `[Theory]`'s four rungs were folded into a ladder**, so the suite is four tests smaller and reads more ground |
| **The lane** | `scripts/test.sh` green at **2,733** |

---

## Watching it happen

[`0045`](0045-amnesty.md)'s amendment to the Definition of done asks for this and not for a column of
hexadecimal. Driven on `platted.toml` at 10,000 Citizens, Tick 1,026, and photographed from the air.

✅ **THE LADDER IS CROSSED IN FULL, WHICH ANSWERS Q1 ON THE DAY IT WAS ASKED.** Off the `draw` dump —
**six distinct drawn heights, 7.0, 10.5, 14.0, 17.5, 21.0 and 24.5 m**, every one an exact multiple of
the storey, over 373 Buildings. That is rungs 2 through 7: the five block patterns plus
`LotRuleset.StoreysOn`'s draw of one. [`0049`](0049-visuals.md) **F42** measured **four** on the same
world and same population three hours earlier, and [`0049`](0049-visuals.md) **F43** measured
**three** before that.

✅ **AND IT IS A GRADIENT AND NOT A PATCHWORK.** Mean drawn height against Chebyshev distance from the
city's middle, in 32-Tile rings: **22.8 m at 0–31, 18.4 at 32–63, 12.1 at 64–95, 8.7 at 96–127.**
Monotonic, and the fall is by nearly two thirds.

🔴 **WHAT SURPRISED ME IS THE BUILDING COUNTS BESIDE THOSE HEIGHTS: 24, 77, 175, 199.** ***The outer
ring holds eight times the Buildings of the core and houses about the same number of people.*** That
is change 2 stated as a picture — an equal share of the population per band means the dense band is a
small ring — and I had read the code, written the sentence and not expected the ratio to be anything
like that large. ⚠ **It is visible in the readout too**: a Lot near the middle reads *dwelling — 48 of
49 occupied*, one out at the rim reads *8 of 9*. **Occupancy comes from floor area alone**
([`0053`](0053-the-block.md)), so nothing chose that spread.

⚠ **The second surprise is that the shape is legible from the air and invisible from the street.** At
50° the middle reads as a different fabric outright — grey flat-roofed slabs and courtyards against
red pitched terraces, with a hard frontier where the built ground stops. At 24° it is a wall of roofs
and the rings are gone. ***A structure the city genuinely has can still be a structure the player
never sees***, which is a question for [`0049`](0049-visuals.md) and not for this document.

---

## What is open

| # | Question | Where it goes |
|---|---|---|
| ~~**Q1**~~ | ✅ **ANSWERED THE SAME DAY, ABOVE.** All six rungs are drawn on `platted.toml` at 10,000 Citizens, and the mean height falls monotonically with distance from the middle | — |
| **Q2** | **`twinned.toml` says its field is FLAT and cannot ratify `prominence_percent`.** That is still true at 16,000 Citizens — 226 of 322 Cells at height 8 — so [`0002`](0002-open-questions.md) §D1's row stands and milestone 15 is still what would ratify it. ⚠ **But the small-city reading is no longer flat**, and this document has not established what that buys | [`0002`](0002-open-questions.md) §D1 |
| **Q3** | **A second Trip is refused by one engine's guard and by nothing else** — **F5**. *Unbuilt*, not *refused* | `CitizenActivity`'s remarks; [`0003`](0003-build-plan.md) when Shopping lands |
