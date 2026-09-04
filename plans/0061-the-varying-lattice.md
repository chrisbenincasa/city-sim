# 0061 — The varying lattice, and the two functions one integer was doing

Scoped and landed 2026-09-04, against `main` at `22f47be`. **[`0045`](0045-amnesty.md) queue row 25.**

**`[roads] block_tiles = 32` was one number for the whole map, and 128 m was a good block size that
was also the only one.**

Three commits, and the split is deliberate. Two of them are **hash-neutral by construction** and the
third is opt-in, so nothing in the shipped corpus moves.

| | What it did | Hash |
|---|---|---|
| `bd82e45` | `BlockLattice` names the two questions and every site asks it | unmoved |
| `eb95dda` | `BlockGround` gives a block a width **and** a depth | unmoved |
| this one | `[roads] block_spread_tiles` makes the lattice vary, and `--morphology` can read it | unmoved on every shipped file |

---

## F1 — The survey's answer is sharper than the row expected, and it is not about a number

The row says ***the work is not changing a number, it is finding out how many places believe there is
one***, and names **71 sites across 11 files**. The survey confirms the count and disagrees about what
they believe.

**Every one of the seventy-odd sites is one of exactly two expressions.**

```
line * block_tiles            -- where does this line stand
FloorDiv(tile, block_tiles)   -- which block is this Tile in
```

🔴 **So *uniform* is not a value anywhere in this build.** No site holds it, no site checks it, and no
site could be found by searching for it. It is the **shape of the two expressions**, and the reason
one integer answered both questions is that on an even lattice they are inverses of one multiplication.

`BlockLattice` names them — `EdgeOf`, `LineAt`, and `WidthOf`, ***which is the difference the second
expression could not express at all***: the arithmetic can say which block a Tile is in and has no way
to ask how wide that block is, because the answer was the divisor.

⚠ **`Even(blockTiles)` is the identity case, and it is what makes the routing provable rather than
argued.** `BlockLatticeTests` states the properties over the whole **16,384**-Tile map rather than a
sample — a transcription error at one line is exactly the defect a sample misses — and the golden
baselines are the second half of the proof. Nothing in `bd82e45` is a design change under `05 §4`
(`adr/0100`).

---

## F2 — Four things a mechanical rewrite would have got wrong

This is why the row's own framing — a survey rather than a `sed` — was right.

1. **`LineSourceQueries` sizes its window as `CeilDiv(range, block)`** on the stated ground that *a
   block IS the lattice pitch*. On a lattice whose lines are not evenly spaced **the pitch is a
   range**, and a window sized by the mean covers less ground than it believes. ***That is a quiet
   wrong answer rather than a loud one*** — a source silently outside the window is a source that
   does not exist. Sized by `BlockLattice.Narrowest` now, and so is `TrafficPresence`'s dilation.
2. **`RoadGenerator.Link` stepped between two lattices in fixed block-sized jumps.** A fixed step
   walks off a varying grid and invents a Node between two intersections. It steps line to line now.
3. **A Street's length was hoisted out of both generator loops as one `var length`** for the whole
   lattice. ⚠ **A Segment's length is GROUND and not a nominal**, and the hoist is invisible while
   the two agree.
4. **`Nominal` against `WidthOf` is the distinction the type exists to make.** A reach, an overlap
   margin and a search radius are legitimately uniform; a Segment's length, a Lot's frontage and a
   parcel's carve are not. ***Both are on the type so each site says which it meant***, and neither
   was mechanically rewritten into the other.

---

## F3 — A block was square by construction, and the carve could not have said otherwise

`BlockPatterns.Carve` took a single `blockTiles` and produced four faces from it. The lattice answers
`WidthOf(column)` and `WidthOf(row)` separately from the day it exists — **and `Carve` had nowhere to
put the second answer**, so passing the column's width would have looked done and silently used a
block's width as its depth.

⚠ **It was marked rather than half-fixed** in `bd82e45` and closed in `eb95dda` by `BlockGround`,
which carries `Column, Row, East, North, Wide, Deep` and answers `Along(face)` and `Across(face)`.
***A face's strip runs along one of the two and reaches across the other***, and which is which is a
property of the face rather than of the block — that is the whole content of the type.

The square overloads stay, because the pattern **ladder** genuinely compares forms at one size and
`BlockGround.Square(blockTiles)` is how it says so out loud.

---

## F4 — The named instrument was blind to what the row changes

The row names `--morphology` as ***the instrument that will read it***. It could not.

🔴 **Orientation entropy, φ, node degree and circuity are all properties of which way an edge runs and
how many meet.** Moving the spacing while holding the mean moves none of them, because every Street
still runs due east or due north. So the mode gained a **`## Blocks`** section: the Street-length
distribution, printed as distinct lengths with their counts.

⚠ **Streets only.** An Arterial is a spline and a foot path is a block diagonal, so neither length is
a lattice spacing — including them would put the diagonal's 181 m in a histogram of block sizes and
read as variation that is not there.

**Measured, `minimal.toml` against `gridded.toml`, one seed, at `--morphology`'s own default of
10,000 Citizens:**

| Reading | `minimal` | `gridded` |
|---|---|---|
| Segments | 627 | **627** |
| Nodes | 324 | **324** |
| Intersections | 320 | **320** |
| Four-way share | 72.50% | **72.50%** |
| The whole degree histogram | — | **byte-identical** |
| Distinct Street lengths | **1** | **3** — 24/32/40 Tiles at 29/47/24% |
| φ | 0.9981 | 0.9969 |
| Occupied bearing bins | 6 of 36 | 12 of 36 |
| Paved extent | 4.73 km² | 4.60 km² |
| Intersections per sq mile | 175 | 180 |

⚠ **Two readings do move and neither is the grain.** The four cardinal bearing bins hold at **306
each**, so ***every bit of φ's movement is the foot-path diagonals***, which stop sitting at 45° once
the block they cross is oblong. And the density moved on an **unchanged Node count** because the
paved extent shrank — ***a density that moved because the denominator did.***

**So the change did what the row demanded**: the row warned that ***a change that moves the grain
rather than the uniformity would be the wrong change made confidently***, and the grain readings the
warning is about are byte-identical across the pair.

---

## F5 — The mean is held over the MAP, and a city is a sample of it

`BlockLattice.Varied` holds the mean **exactly** over every run of four lines: one wide spacing, one
narrow, the nominal everywhere else, so a period measures `4 × block_tiles` whatever the draw did.

🔴 **The printed mean is still not the nominal, and the first version of that line claimed it was.**
A city paves the blocks it needs and **stops mid-period**, so the sample's mean is the map's only
when the truncation happens to be even: **128.0 m at 2,000 Citizens and 126.1 m at 10,000**, on one
lattice whose every period sums exactly.

***A quantity held by construction over the world is not held over an arbitrary window into it.***
The caption says so now rather than asserting the stronger claim.

---

## F6 — A refusal became unstateable in a Ruleset, which is `adr/0070` evidence

`RulesetLoader` refuses a `[[lattice]]` origin that is **not a multiple of `block_tiles`**, and *a
multiple of `block_tiles`* is the same sentence as *on a line* **only while the lines are evenly
spaced**.

Once a spread is stated the line positions are drawn on the **world seed**, so whether an origin
stands on a line is a property of the **world** and not of the file — ***and a Ruleset is loaded
before any world exists.*** A loader has no world to ask.

⚠ **So the two are refused together, by name.** Under `adr/0070` that is the one classification which
*is* evidence: a second lattice on a varying grid is **refused** and not merely unbuilt, and the day
somebody wants one the question is *where the check lives* rather than *how to compute it*. Line 0 is
always at Tile 0, so an origin of **0** is the one thing a file can still promise.

⚠ **Without the refusal the file would load clean**, the origin would land mid-block, and the symptom
would be a second lattice off its own grid in a world nobody could reproduce from the file alone.
`adr/0048` carries all three of this row's refusals; the count of record moved 229 → **232**.

---

## F7 — Absent means uniform, and that polarity is what keeps the corpus still

`[roads] block_spread_tiles` is **optional**, and its absence means the lattice is even.

⚠ **A derived spread with no key would have replaced one absolute with another**, which is the row's
own complaint arriving one level up: 128 m everywhere is bad because it is the only answer, and *some
spread everywhere* would be exactly as unavoidable. A file that wants a hierarchy of streets states
it.

**The consequence is that every shipped Ruleset is untouched and not one golden baseline moved** —
2,806 working-lane tests green, both session traces and `world-hash.txt` unmoved. `gridded.toml` is a
new file, so the demonstration costs nothing that already existed.

⚠ **The step is the grid's own.** 8 Tiles is a quarter of `block_tiles = 32`, on the footing
[`0060`](0060-the-plot-module.md) put the plot module on, and it is **not imported from a survey of
anywhere** — `plans/0012` **Cause 5** is what importing Tait's ratio would have been.

---

## F8 — Watched in the shell, and the surprise is a Lot count nobody was measuring

The amnesty's Definition of done is ***a milestone is done when you have watched it happen and
something surprised you***, so both worlds were driven, photographed and read.

**Watched**, `minimal.toml` against `gridded.toml`, 2,000 Citizens, Tick 1,032, Day 0 17:05, camera
over Tile (145, 145) at 1,150 m and tilt 78°: the plan view reads as a gridiron in both, and in
`gridded.toml` the block lines are **visibly unequal** — narrow strips against wide ones, in runs,
with the wide ones not adjacent. It reads as a street hierarchy rather than as noise, which is what
`Varied`'s period is for.

🔴 **The surprise is that the Lot count moved, and nothing in the row predicted it.** Measured
headless, `--zones --citizens 2000 --ticks 1`, one seed:

| `block_spread_tiles` | Lots | Buildings |
|---|---|---|
| absent (uniform) | **192** | 160 |
| 2 | **200** | 167 |
| 4 | **200** | 163 |
| 8 | **200** | 167 |
| 12 | **200** | 163 |
| 15 | **216** | 168 |

⚠ **The lattice's own extent is identical across every one of those rows** — 149 Segments and 81
Nodes, confirmed by `--morphology` — so this is not a bigger city. **The same ground carves into more
parcels once the blocks are unequal.**

🔴 **And the shape of the table is the interesting half: it is a STEP and not a slope.** A spread of
2 Tiles buys the whole +8, and 4, 8 and 12 buy nothing more. That is a rounding threshold somewhere in
the carve rather than a proportional effect — ***a quantity that jumps at the smallest non-zero input
and then ignores its own magnitude is not responding to the magnitude at all.***

⚠ **The mechanism has NOT been established and no document may state one.** `adr/0043`: the plausible
candidates are `ReachTiles` admitting a third Address on a longer face, `UnitTiles` reading
`BlockGround.Least`, and `Widths`' remainder — and *plausible* is what that ADR exists to refuse.
**The machine that would separate them is a per-block parcel census across those three sites, one
seed, at spreads 0, 1, 2 and 15.** No milestone owns it.

🔴 **It is owed to [`0002`](0002-open-questions.md) §B and IT COULD NOT BE FILED THERE, which is a
finding about the freeze rather than about the lattice.** `adr/0073` routes a question to `0002`;
`CorpusBudgetTests.The_open_questions_do_not_grow` holds that file at **153,786 words** for the
duration of the amnesty and it is **at** the ceiling with zero headroom, so a new entry has to be paid
for by deleting an existing one. ⚠ **Neither half of that is wrong**: the freeze is `adr/0006`'s
missing sink arriving at the one file that had none, and the routing rule is what stops findings dying
in the plan that found them. ***What has no answer is what a question found by new simulation work is
supposed to do*** — the amnesty's own rule is that prose beside new simulation is free, and this test
does not have that exception in it. **The row is written and is held here**; filing it is the player's
call, because it costs somebody else's entry.

⚠ **The Buildings column is placement order and not supply** — 163 and 167 alternate with no relation
to the spread — so it is printed to be honest about what was read and is **not** a reading of anything.

---

## What the shape of the variation is, and what it is not

**A period that sums to the mean, with the wide line's position drawn and the fact that one exists
fixed.**

- **A fixed repeating pattern** would put a wide street every fourth line across the whole map, which
  is a wallpaper rather than a city.
- **An independent draw per line** would be noise, and no gridiron anybody has built is noise.
- ***What a real gridiron has is a hierarchy*** — most streets at the common spacing and an
  occasional wider one — so the structure is fixed and its position is drawn.

⚠ **One spacing serves both axes and the two read it at different offsets**, so block `(c, r)` is
`WidthOf(c)` by `WidthOf(r)` and is oblong wherever they differ. **A second independent spacing** —
Manhattan's avenues against its streets — is a different decision and nothing here makes a claim
about it.

⚠ **`Period = 4` is doing one job: it is how often a wider street happens.** At four, a quarter of
the lines are wide and a quarter narrow, which is dense enough to read as a hierarchy rather than as
an occasional oddity — and the mean is held over four lines rather than over the map, so no stretch
of the city drifts wide or narrow.

**`PurposeTag.BlockSpacing` is new**, drawn once per **period** rather than per line, which is what
makes the two positions within a period a single decision rather than two that can collide.

---

## Residuals

| | |
|---|---|
| **`RoadGraph.ExpectedNodes`** sizes its tables by the same division `LineAt` replaced. It is a **capacity** rather than a position, so it is legitimately nominal — but it is the one site the survey passed over rather than routed, and it is named here so the next reader does not have to find it twice |
| **A second independent spacing per axis** is undesigned, not refused. `Varied` takes one spread and both axes read it |
| **The player's snap now follows the lattice** (`Simulation.ApplyConnect` calls `LineAt`), and **nobody has watched a player draw a street on a varying lattice**. Row 22's ghost is what would make it judgeable |
