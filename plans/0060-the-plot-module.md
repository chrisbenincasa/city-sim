# 0060 — The plot module, and the ruler that chose it

Scoped and landed 2026-09-04, against `main` at `2620f50`. **[`0045`](0045-amnesty.md) queue row 24.**

**A plot width is a multiple of its own block's module, and it used to be a fifth of every Segment on
the map.**

---

## The two numbers `lots_per_segment` was carrying

`[lots] lots_per_segment = 5` is one number for the whole world, and it sized two different things.

| What it sized | Whose number that is |
|---|---|
| **How many Addresses a Segment holds** | The **routing graph's**. [`adr/0078`](../docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md) says *there was no number to choose* because five is already `CONTEXT.md` → Address's — and **read what that sentence is for**: five a Segment is what holds the graph near **30,000** Segments rather than 150,000–300,000 |
| **How wide the ground behind one of them is** | **Nobody's.** The routing argument was never a claim about how wide a building is |

***The five was chosen to size the graph and was being used to size plots.***

⚠ **The routing argument survives untouched and nothing here reopens it.** Five *on average* is
compatible with widths that vary, and nothing in the 30,000 bound needs them equal. What changed is
how the ground behind a face divides, not how many doors are on it.

---

## F1 — The file's one shape claim was false by 1.8×, and it was false before any width varied

`BlockPatterns.StripTiles` is the depth of a shallow strip, and its own remark carries the only shape
claim in that file: ***a plot is about as deep as it is wide***. Its first term was
`blockTiles ÷ lotsPerSegment`.

🔴 **That is the ADDRESS SPACING and not a plot's frontage.** A Segment's Addresses split between the
two blocks that share it by parity (`Frontage.SideOf`), so a face carries about **half** of them and
each parcel spans about **twice** the spacing.

**Measured off the draw list**, `minimal.toml` at 1,000 Citizens, before the change:

| | |
|---|---|
| Parcels on a south face | **40 × 24 m** and **44 × 24 m** — 10 and 11 Tiles wide, **6** deep |
| The claim | a plot about as deep as it is wide |
| The reading | **1.8 : 1** |

The term is `2 × blockTiles ÷ lotsPerSegment` now. ⚠ **At the shipped lattice the quarter cap is what
binds after the correction** — 12 capped to 8 — so the depth goes **6 → 8** and the reading goes
**1.8 → 1.6**. A test covers both halves, because one that checked only the term would pass on a
lattice where the cap never bit and say nothing about the one that ships.

🔴 **The row predicted this would break when widths varied. It was already broken.** ***A derivation
can be wrong about the quantity it names without ever being wrong about the number it returns***, and
nothing noticed for as long as there was only one width to be wrong about.

---

## F2 — Nothing could put a ruler on a picture, so *what reads* had never been asked

Row 24 says the step is settled by *what reads at the camera distance*, and that **the `draw` list and
a screenshot can now measure it**. They could not. **The draw list is in metres and a screenshot is in
pixels, and nothing joined the two** — so every judgement about what the drawing reads was an eyeball,
which is why the question had sat.

✅ **`draw` gained a `scale` row**, from `Main.Ruler`: three ground points a Tile apart through
`Camera3D.UnprojectPosition`, which is the same projection the frame was drawn with — so perspective,
tilt and the viewport's own size are in the answer rather than in an approximation of it.

```
scale	<px per Tile east>	<px per Tile north>	<eye distance m>	<viewport w>	<viewport h>
```

**Measured** — `minimal.toml`, 1,000 Citizens, tilt 40°, viewport 3,024 × 1,834:

| Eye distance | px per Tile | What it is |
|---|---|---|
| 100 m | **54.6** | a street |
| 200 m | **27.0** | a block |
| 400 m | **13.4** | **where a player edits** |
| 1,000 m | **5.35** | **the whole city in frame** |
| 2,000 m | **2.67** | past useful |

⚠ **It is a reading of the CAMERA and not of the city**, so it moves with a zoom and a tilt and never
with a Tick. ⚠ **And it is a property of the reading**, so the viewport is in the row: the same
distance on a smaller window is fewer pixels a Tile.

---

## The decision the two measurements make

Row 24 asks two questions and says a survey cannot answer either.

**1 — What reads at the camera distance?** One Tile of frontage is **5.35 px** at the distance the
whole city is read from and **13.4 px** where a player edits. So **the grid's own step is already at
the threshold at the far end**, and there is no case for anything finer — which the grid could not
express anyway. The row's own worry — *a quarter of a 6-Tile unit is 4 metres of frontage seen from a
kilometre up* — now has a number against it: **5 pixels, visible, and only just.**

**2 — How many distinguishable width classes does a block need?** A face carries **two or three**
parcels at the shipped lattice, because the Addresses split by parity. ***A set of six classes is a
distribution nobody can see in a row of three.*** **Two.**

**So the module is a power-of-two fraction of the block, drawn between two adjacent ones**, and a face
shows two widths.

| | |
|---|---|
| `UnitTiles` | `blockTiles ÷ 8` or `blockTiles ÷ 16`, drawn per block — **4 or 2 Tiles** at the shipped lattice |
| The spread between blocks | **2×** |
| `Widths` | each parcel takes `units ÷ groups` modules; the spare ones go to a **contiguous run**, drawn per face |
| Widths a face | **two** |

⚠ **Tait's 49 Scottish blocks (2008) supply the STRUCTURE and not the step** — widths quantised rather
than continuous, the module block-specific and varying about 2.2× between blocks, regular within a
block and varying between them. 🔴 **The step is the grid's**: a block is a power of two Tiles, so
halves and quarters are exact where thirds are not, and **2× is the nearest thing the grid can express
to 2.2 without borrowing Scotland's number**. ***Importing ¾/1/1¼/1½/1¾/2 would make a Borough plot
width a claim about Scotland***, which is [`0012`](0012-corpus-audit.md) **Cause 5** committed on
purpose.

⚠ **Two new `purpose_tag`s and not one.** `PlotUnit` is drawn on the block's coordinates —
`BlockPattern`'s reason exactly, a block cleared and zoned again is re-platted the same way — and
`PlotWidths` is drawn on **the block and the face**, because four faces keyed alike would take their
spare modules at one position and a block would read as four copies of one terrace.

---

## What the face still guarantees

🔴 **The widths tile the reach exactly, and that is not a rounding convenience.** `Exhaustive`
patterns claim to leave no ground over, and a partition that quantised and then stopped would leave a
sliver at the end of every face of every one of them. ***The last parcel absorbs `reach mod unit`***,
which is under one module and is what the end of a real terrace does.

⚠ **A module too coarse for a face falls back to the even split** — fewer modules than parcels —
rather than refusing or dropping a parcel. That is the coarse-Ruleset case and it reproduces exactly
what the carve did before a module existed.

⚠ **`ClaimedTiles` and `AddressCount` pass a default key and are unaffected**, because the widths sum
to the same reach whatever the module: the ground a pattern claims and the doors it lays are both
invariant under this draw. ***A ladder that moved with the world seed would be a ladder that is not a
property of the lattice.***

---

## What it looks like

**Measured on the draw list**, `minimal.toml` at 1,000 Citizens:

| | Before | After |
|---|---|---|
| Parcel sizes, in metres | 40×24, 44×24, 64×24, 24×40, 24×80 | 32×32, 40×32, 48×32, 64×32, 32×64 |
| Widths, in Tiles | 10, 11, 16, 20 | 8, 10, 12, 16 |
| Depth, in Tiles | 6 | **8** |

**And watched.** At 1,000 m the terraces used to read as long uniform bars; they break into pieces of
unequal length now, so a block has a texture where it had a stripe. ⚠ **The depth correction is doing
half of that work** and is not separable from it in a picture — which is why the numbers above are the
record and the screenshot is the spot check ([`0048`](0048-driving-the-shell.md) §7).

---

## F3 — Two tests moved, and one of them had been passing on a margin of one job

The carve is hash-bearing, so the three golden artefacts were expected. **Two behavioural tests were
not**, and re-measuring both found the same defect underneath: ***a sign test on a saturating
quantity reports that the mechanism has a direction and never that it still has a size.***

### `CarOwnershipTests` — the employment total is the wrong reading

The class asserted that a city of drivers **employs more people** than a city of walkers, sited at
16,000 Citizens, on a sweep recorded 2026-09-01 reading **−1, +41, +1,070** at 4,000, 16,000 and
64,000.

🔴 **Re-measured on `2620f50` — before anything here — the same three populations give +1, +1, +19.**
The margin had fallen by two orders of magnitude at the top rung and nothing said so. This row moved
16,000 from **+1 to −3** and the test failed; ***it tipped it and did not cause it.***

⚠ **The module and the depth were separated to find that out.** With the module drawn and the depth
left alone the readings are **0, −1, +17**; with both, **−3, +5, +23**. The depth correction is worth
about **+4** and the module about **−1**, against a quantity whose whole range is under 25 — which is
another way of saying neither of them is what this test was measuring.

✅ **The reading is `EmploymentActivity.Beyond`** — candidate vacancies refused for exceeding the
ceiling, which is the Commute Budget binding, stated directly rather than through its effect on a
total. Swept on `minimal.toml` over 2,048 Ticks:

| Citizens | walker `Beyond` | driver `Beyond` | walker fast / moderate / unsavoury |
|---|---|---|---|
| 16,000 | **0** | 0 | 10,509 / 624 / 0 |
| 24,000 | **0** | 0 | 14,330 / 2,096 / 18 |
| 32,000 | 36 | 0 | 17,661 / 4,351 / 144 |
| 48,000 | 1,124 | 0 | 22,441 / 9,853 / 705 |
| 64,000 | **4,920** | 0 | 26,307 / 15,991 / 1,890 |

🔴 **At 16,000 the Budget refuses nobody at all**, in either mode — not one walker even reaches the
unsavoury rung. ***The class moved off 4,000 on 2026-09-01 because the mechanism was inert there, and
landed on another population where it is inert.*** The escape from an inert fixture was itself inert,
and it survived three days because the reading it escaped to saturates: **a walker the ceiling
refuses does not go unemployed, they take a nearer job**, so the total is bounded by the vacancies
the city holds and the whole effect lands in *which* job rather than in *how many*.

**Re-sited at 64,000**, asserting on the refusal and on the rung split, at a cost of about 6 s.
⚠ **A driver's `Beyond` is 0 at every population measured and that is a finding rather than a
tautology** — 50 clock minutes at 90 km/h reaches across more city than this lattice holds.

### `TasteTests` — seed 0 was lucky twice

`A_household_that_wants_the_centre_ends_up_nearer_it` was moved from 2,000 to 12,000 on **the same
day and for the same reason**: at 2,000 the gap between the two groups swept **−11, +2, +10, −5, −1**
over five seeds and the committed −11 was one draw from it. Re-swept at 20,480 Ticks on this build,
`roomMean − centreMean` with its own sham gap beside it:

| Citizens | five seeds | sham |
|---|---|---|
| 12,000 | **−6**, +3, +11, +3, +8 | 2, 4, 6, 11, 10 |
| 20,000 | +3, +1, +13, +7, +13 | 11, 3, 1, 4, 4 |
| 32,000 | **+13, +14, +16, +15, +12** | 6, 6, 2, 8, 6 |

**Seed 0 is the single negative reading in the 12,000 row.** ⚠ At 20,000 the sign is right on all
five and two of them sit inside their own sham, so that is not the rung either. **Re-sited at
32,000**, where every seed is positive and clear of its sham, at a cost of about 5 s.

⚠ **`world-hash.txt` did not move and could not.** `GoldenFixtures.Build()` hand-places its six Lots
and its own comment says it avoids depending on how `SubdivideAt` floors — so the hand-built world
never reaches this carve. Both session traces moved; that file did not.

---

## What this does not do

⚠ **It does not vary the module WITHIN a block**, which Tait's structure does not ask for — regularity
within a block is half of what the survey found.

⚠ **It does not change how many Addresses a Segment holds**, and the day something wants that,
`adr/0078` is the document and the 30,000-Segment bound is the argument.

⚠ **It states no Ruleset key.** The module is derived from `block_tiles` and drawn; `adr/0078`'s
refusal of an authored width stands exactly as its refusal of an authored depth does.

🔴 **And it is the twin of [`0045`](0045-amnesty.md) row 25 at the parcel grain.** That row asks
whether the **lattice** should be uniform; this one asked whether the **plots on it** should be. They
share `LotSubdivider` and `BlockPattern` and they sequence rather than parallelise — this one first,
because a module that varies on a lattice that does not is one variable and not two.
