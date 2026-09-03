# 0057 — The Vintage

Scoped and landed 2026-09-03, against `the-ladder` at `1760e8c`. **Two debug overlays that would not
ship, and the reading one of them produced immediately.**

⚠ **A debug view is not a feature and this plan does not pretend otherwise.** It exists because
[`0056`](0056-the-ladder.md) closed with a question nothing in the picture could answer — *is the
ladder actually being crossed, or is the city one mass with a tall middle?* — and the honest way to
answer a question about a derivation is to draw the derivation rather than to read the code back to
yourself.

---

## What landed

**Two washes on the `overlay` verb, `rung` and `age`, and one saved column to feed the second.**

| | |
|---|---|
| `overlay rung` | The block pattern a Building's Lot was carved by. **Five hues, categorical** |
| `overlay age` | How long a Building has stood. **The ordinary ramp, bright is old** |
| `BuildingTable.RaisedAt` | The Tick a Building went up. Saved, hashed, `Touch.Cold` |

🔴 **They are BUILDING washes and the three that came before are GROUND washes, and the difference is
the whole implementation.** `pollution`, `value` and `sealing` mute every layer and let the ground
plane carry the reading. These two mute every layer **except** the massing, drop the ground to
`Muted`, and put the reading on the Buildings themselves through a second unshaded material that
takes the MultiMesh's per-instance colour as its albedo. ⚠ **The roof takes the same colour as the
body**, because from the shallow tilt the shell opens at, the roof is most of what a tall Building
shows, and a slate that stayed slate would put a second meaningless hue on every reading.

⚠ **The `Bands` ramp is refused for `rung` on purpose.** `BlockPatterns.Ladder` orders patterns by how
many addresses they fit, which makes a rung an **ordinal**: *slab is one place above courtyard* is
true and *slab is 25% more than courtyard* is not. A monotone lightness ramp asserts the second.
`rung` gets five hues and the legend names all five.

⚠ **`age` normalises against the Tick NOW and not against the oldest Building on screen.** A ramp
fitted to the oldest looks identical on a city of one vintage and a city of twenty, which is the exact
distinction this view exists to make. So 1.0 always means *here since Tick 0*.

**The rung is recovered and not stored** — `(storeys - 2) / storeys_per_rung`. ⚠ **It is exact except
at one value.** `LotRuleset.StoreysOn` draws `0..step` **inclusive**, so the top of rung *r*'s range is
the bottom of rung *r+1*'s and a Building sitting there is drawn as the higher of the two. That
overlap is the original design's — the draw was `0 or 1` before [`0056`](0056-the-ladder.md) widened
it, and rungs shared a height then too. ⚠ **[`0056`](0056-the-ladder.md) claims the rung is
*recoverable* from the height and that claim is one value too strong**; it is corrected there and
recorded in [`0012`](0012-corpus-audit.md).

---

## F1 — The city IS crossing its ladder, and the picture is not what the numbers said

[`0056`](0056-the-ladder.md) **Q1** asked whether the taller city reads as a ladder or as one mass.
Drawn, it is unambiguously a ladder: a green fringe of detached terraces, a blue and yellow middle
ring, a pink courtyard block and a red slab core, and the boundaries fall on streets.

***So Q1 is answered and the answer is yes.*** It is struck there.

## F2 — And the same picture says the taller Buildings are also the FATTER ones

🔴 **Footprint climbs with rung. There is no small-footprint tower in this design and there cannot
be one.** Measured off the draw list of the wash itself — `platted.toml`, 4,000 Citizens, Tick 21,514,
101 Building instances:

| rung | pattern | n | mean footprint | largest | mean height |
|---|---|---|---|---|---|
| 0 | detached | 49 | 655 m² | 1,520 m² | 9.7 m |
| 1 | perimeter | 20 | 879 m² | 1,792 m² | 20.6 m |
| 2 | back-to-back | 19 | 871 m² | 2,400 m² | 29.3 m |
| 3 | courtyard | 3 | 2,352 m² | 4,176 m² | 40.8 m |
| 4 | slab | 10 | 1,280 m² | 2,688 m² | 56.0 m |

⚠ **The courtyard row is a ring and not a slab**, so its footprint overstates its floor area by the
hole the drawing already subtracts. The trend survives without it.

**It is a derivation and not a tuning miss.** Storeys come from the rung, and the rung is the
pattern's place on a ladder sorted by **claimed ground per Address** — how much land stands behind one
front door. Measured at the shipped lattice, `block_tiles = 32`, `lots_per_segment = 5`:

| rung | pattern | claimed | Addresses | ground/door |
|---|---|---|---|---|
| 0 | detached | 624 | 8 | 78 |
| 1 | perimeter | 1,024 | 8 | 128 |
| 2 | back-to-back | 1,024 | 5 | 204 |
| 3 | courtyard | 880 | 4 | 220 |
| 4 | slab | 1,024 | 2 | 512 |

🔴 ***The densest rung is the one with the FEWEST doors and the MOST land behind each of them.*** The
quantity is a **proxy**: land behind one door stands in for *how many people are behind that door*,
and it holds across these five because every one of them houses people by going **back**. So the
patterns that rank dense are the ones taking half the block's depth per parcel
(`BlockPatterns.DepthTiles`), which is why height and footprint are positively correlated by
construction. Nothing downstream can break the correlation: the footprint is the parcel inset by a
**fixed** `[lots] setback_tiles`, and the parcel is a property of the pattern.

⚠ **This is an *unbuilt* absence and not a *refused* one** (`adr/0070`): nobody decided a city should
have no towers. The tower-shaped form was never reachable.

## F3 — A generated city is one vintage, and `overlay age` is how you see that

**At Tick 21,524 on `platted.toml` the age wash is flat across every Building in the city.**
`SyntheticCity` raises everything it lays inside one call at one Tick, and `ZoneRuleEngine.Raise` —
the one site that could produce a second vintage — had not fired on this world by Day 10.

⚠ **That is a true reading of a real absence rather than a broken overlay**, and the legend says so
on screen rather than leaving somebody to work it out. ***An instrument that reads flat has to
distinguish "nothing varies" from "I am not measuring anything", and only a legend can do it.***

## F4 — The column moved every golden hash, and the reason it is worth that

`BuildingTable.RaisedAt` is saved and hashed, so the composition changed and all three golden
artefacts moved from the first sample on. No Ruleset was touched and `World.HashSeed` is unmoved; the
precedent is milestone 24 task 8b, recorded in `tests/Borough.Tests/Golden/README.md` beside this one.

⚠ **A debug view is a thin reason for a hash move on its own.** What carries it is that *when* a
Building was raised is state the city does not hold and several things want — weathering, era-varied
massing, and anything that wants to tell a new Building from an old one.
***A view is a legitimate reason to add state; it is a bad reason to add state only a view will read.***

---

## Open

**Q1 — how does a city build UP without building OUT?** F2 says it cannot today. Three levers were
put up and **two of them do not survive F2's own table**:

1. ❌ **A setback that grows with storeys** — `[lots] setback_tiles` as a base plus a per-storey step.
   It works and it is the wrong shape: it makes footprint a *function* of height, so **every** tall
   Building is thin. That is the present monotony with its sign flipped, not variety, and it is a
   scalar knob where the ask was an emergent form.
2. ❌ **A sixth block pattern, `Tower`** — one small central parcel per block. ***It sorts to the
   BOTTOM of the ladder and comes out two storeys tall.*** A tower deliberately claims **little**
   ground; a hypothetical 8×8 parcel behind one door is 64 Tiles a door against a detached house's
   **78**, so the ladder reads it as sparser than a bungalow. 🔴 **The proxy is what fails.** Ground
   behind a door stands in for people behind a door, and it holds for the five because all five house
   people by going **back**. ***A tower houses them UP, and the metric cannot see up at all.***
3. ✅ **Invert the derivation.** A pattern declares **its own storeys** as part of its form, and the
   ladder sorts on ground per **dwelling** rather than ground per **door**. ⚠ **That is not a new
   quantity to argue about** — dwellings are floor area over `[capacity] floor_tiles_per_dwelling`,
   floor area is `claimed × storeys`, so ground per dwelling reduces to `floor_tiles_per_dwelling /
   storeys` and the ladder is sorting on **storeys**. It orders the present five in **exactly their
   current order**, and a 20-storey tower lands at the top honestly.

⚠ **Lever 3 reverses the sentence `BlockPatterns.Storeys` is built on** — *height rises with density
because it is the same quantity*. Under it, density rises with height, because height becomes the
input rather than the output. It also empties `[lots] storeys_per_rung`, which
[`0056`](0056-the-ladder.md) landed the same day. ***That is a reopening and not a refinement***, and
it wants writing down before it is written.

⚠ **And lever 3 alone still gives a RING of towers rather than a skyline.** The ladder is a total
order and `ForBand` indexes it, so one band gets one pattern for ever. Several forms sitting near one
density is what makes a choice among them available — and a draw over near-ties is the same trick
`LotRuleset.StoreysOn` already plays one level down. **Not designed. Named here so lever 3 is not
oversold.**

**Q2 — should `age` be normalised against the run or against the oldest Building?** F3 makes the case
for the run. A world loaded from a save at Tick 900,000 whose Buildings all went up in the last
thousand Ticks washes uniformly dark under the current rule, which is *true* and not *useful*.
***The choice is between an absolute scale that can read blank and a relative one that can lie***,
and it is not settled by this plan.

**Q3 — nothing asserts either wash.** The draw list carries the colour, so a test could hold
`overlay rung` to *every Building's colour is one of five* and *the colour matches
`(storeys - 2) / step`* without a display, the way F2's table was measured. Not written.
