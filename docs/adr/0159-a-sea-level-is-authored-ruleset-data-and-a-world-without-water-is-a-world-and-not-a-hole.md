# A sea level is authored Ruleset data, and a world without `[water]` is a world and not a hole

**Where the sea stands is the one thing a Ruleset says about water, and everything else about a Water
Body is derived from it and the `WorldKey`.** `[water] sea_level_percent` is a level, stated as a
percent of the height range *that world realised*. Which Cells are wet, how many bodies there are, how
big each is and which one each drains into all fall out of the field — there is no water-coverage key,
no body count, no outflow-direction key and no shoreline key.

**A Ruleset that omits `[water]` has no water at all**, reached by omitting the table rather than by a
defaulted key. That is an inland city, and it is a legitimate world rather than a degraded one: ten of
the eleven shipped Rulesets are one.

Guiding concepts: `EMERGENCE`, `PLAYER GOVERNS`, `HONEST DEGRADATION`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
Whether water should be authored or derived is a design question; the measurements below corroborate
and do not decide. 🔴 **The value itself is unratified and `plans/0002` §D1 carries its row.**

## Why

### The number is authored because the alternative was measured and it is worse

Two candidates were live, and the second was the recommendation until it was priced.

**Basins only, authoring nothing.** Water fills depressions: priority-flood the height field, water
exists where the filled level exceeds the ground, and a body's outflow is where it spills. This authors
**zero** numbers, so it opens no §D row and needs no ratifier — which under
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) is a real
advantage and not a stylistic one.

**A sea level, authored.** Water is everything below a stated level.

***The basins-only world has no sea, no bay and no coast in it***, and `CONTEXT.md` → Water Body says a
Water Body is *"a pond, lake, river, bay or stretch of coast."* A generator producing only the first
two is a strict subset of the design, which is *unbuilt* under
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) and nameable as such — but the thing
being given up is the largest water feature the design names, on a map
[`0089`](0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) sizes at 65.5 km a side. **A city
on a coast is the reference case for half the genre.** The number is taken.

### A level rather than a coverage, and the two get confused the moment somebody quotes a share

The key says how high the sea stands, not how much of the map is wet. How much falls out of the key's
own field as well, and the spread is not small — measured on `rulesets/coastal.toml` at
`sea_level_percent = 25`, over five keys, on a 512×512-Cell map:

| Seed | Wet Cells | Share of map | Bodies |
|---|---|---|---|
| 1 | 5,569 | 2% | 14 |
| 24006 | 40,739 | **15%** | 34 |
| 770413 | 7,881 | 3% | 26 |
| 8675309 | 24,131 | 9% | 64 |
| 18446744073709551615 | 6,672 | **2%** | 20 |

***One level, and a 7.5× spread in coverage across five keys.*** That is the generator being a
generator, and it is why no coverage key exists: authoring a share would make the pass **solve for a
number** instead of laying a world. ⚠ **A water share is a fact about the world it was measured on**,
and a document quoting one names the file, the level and the key — `plans/0012` **Cause 5**.

### Against the realised range, so that every key has a coast

The level is a percent of the range the key produced, not of the range the noise could produce. This is
[`0157`](0157-terrain-is-five-types-and-base-fertility-varies-across-them-because-a-category-exclusion-is-not-an-overlay.md)'s
self-normalising reading and it is taken for that ADR's reason: the octave sum is bell-shaped, so a
level fixed against the theoretical ceiling would drown one key and leave the next dry. `WaterTests`
asserts the property this buys — **every key has a coast** — rather than the amount it produces.

### One height field, three readings, and one `PurposeTag`

[`0156`](0156-height-does-not-ship-until-terraforming-does-because-terrain-without-a-price-is-a-wall.md)
says the generator uses height *"to decide where water sits, which ground floods, and what terrain type
a Cell is."* So `WaterGenerator` reads the **same** field `TerrainGenerator` reads, on the same
`PurposeTag.TerrainType`, and **no new tag is allocated**.

⚠ **That is not the tag reuse `CLAUDE.md` forbids.** A tag is owed to a distinct *decision*, and
*where the ground is low* is one decision. Drawing water from a second tag would put the sea somewhere
unrelated to the low ground of the terrain it sits in — not independence, but nonsense. **Height is read
and stored nowhere**, as `0156` requires.

### The graph is real, and the thing that keeps it acyclic had to be found by testing

A body that reaches the map's edge drains off-map — `CONTEXT.md` → Water Body's *"terminating in a
Hinterland off the map edge"* — and spells that as an unset handle. A landlocked body drains over its
**lowest rim** and the outflow goes wherever water leaving there would go, found by walking downhill.
There is no outflow-direction key because the field already knows.

🔴 **Two things about that walk were wrong before they were measured, and both were caught rather than
argued.**

1. **The walk fell back into the body it came from.** A rim Cell is by construction the lowest *dry*
   ground touching the body, so its lowest neighbour is nearly always the body itself. Excluding the
   source body from the walk roughly **doubled** the number of bodies with a downstream edge.
2. **The graph had cycles in it.** Two basins spill into each other across a ridge, and each walk is
   individually correct. `WaterTests.Every_outflow_reaches_the_map_edge` found it. The fix is a strict
   order — a basin drains to one whose **spill elevation** is lower, ties broken by Cell index — and
   ***a strict total order cannot contain a cycle***, so nothing downstream has to check for one.

⚠ **A body refused an edge by that order becomes endorheic rather than being rerouted**, and after both
fixes that is still **35–60%** of bodies on the keys above. **That is a real coarseness and it is stated
rather than buried**: a spill can descend into a *dry* hollow, and where the water goes from there needs
to know how full the hollow gets, which is a volume, which is a Bin — task **6b**.

### What this deliberately does not decide

**The Bin is not here.** [`0034`](0034-fields-are-sorted-by-source-geometry.md) gives a Water Body a
**capacity** and an **outflow rate**, and both are parameters of a Bin. That Bin is blocked on **what
family Waste is**, which `CONTEXT.md` answers two ways two entries apart, `04 §1` answers a third way
and `0031`'s own table implies a fourth — surveyed in [`docs/references.md`](../references.md) **§10**,
whose finding is that the question is malformed and the genre has known it for twenty-five years.

⚠ **So the `− w₅·shoreline` term stays ABSENT from desirability rather than present and zero**, which is
[`0123`](0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md)
holding: a working mechanism that says something false is worse than a named hole.

## Consequences

- **`[water]` is one key, optional, and refused at both ends of its range.** 0 is refused because it is
  a *second spelling of omitting the table*, not because it is out of range; 100 because a world that is
  entirely water is not a city. `adr/0048`'s count of record moves 145 → **147**.
- **`rulesets/coastal.toml` ships** — `minimal.toml` with `[water]` — and is the only shipped file in
  which the map contains water. Its header carries the measurements above and the caveats.
- **Two new tables, both `(saved AND hashed)`**: `water_body` with one `downstream` handle into itself,
  and `water_cell`, which is **sparse** on `DistrictCellTable`'s reasoning — dry ground is in no Water
  Body by definition. 🔴 **Every State Hash moves.**
- **Both tables are excluded from `TablesAPhaseCanWrite`**, on terrain's test rather than on a cost: no
  Tick phase can write them. ⚠ **Task 6b must take them back out of that exclusion**, because a Bin's
  level is a write.
- **A seventh `FactorioTests` fixture**, because the water columns are gated on a Ruleset key and no
  other shipped file states one. ***A table with no production writer needs a fixture named for it***,
  which is that test's own corollary arriving for the fourth time.
- 🔴 **Roads still do not avoid water.** `adr/0021` makes a water crossing *"a buildability exception
  plus a rendering variant, not a system"*, and neither exists — so a lattice runs straight across a
  lake and nothing refuses it. **Stated in `coastal.toml`'s header**, because it is the first thing
  somebody will see and read as a defect.

## What would trigger revisiting

- **Task 6b ships the Bin**, and a Water Body gets a level. That is when the sea level acquires its
  first consumer and §D1's row becomes ratifiable at all.
- **Somebody measures a water share and quotes it without the level and the key.** The spread above is
  7.5× at one level; the row is `plans/0012` **Cause 5** waiting to happen, and the disqualifier
  registry is where it would go.
- **A bridge, a buildability exception or a rendering variant is argued.** Any of the three makes water
  something the Road Graph has to know about, and the *order against `LayLand`* becomes load-bearing
  where today it is only anticipatory.
- **The endorheic share becomes a problem somebody can see.** It is 35–60% today and invisible, because
  nothing reads a Water Body. A depression-filling pass would fix it and needs a volume, so it arrives
  with the Bin or not at all.
