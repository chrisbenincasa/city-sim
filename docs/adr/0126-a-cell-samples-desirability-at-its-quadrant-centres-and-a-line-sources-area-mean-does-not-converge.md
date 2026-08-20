# A Cell samples desirability at its quadrant centres, and a line source's area mean does not converge

**A Cell's land value target is the mean of `MapLayers.Desirability` over four Tiles — the centres of
its quadrants, at Tile offsets 8 and 24 on each axis.** `MapLayers.DesirabilitySamplesPerAxis` is **2**,
a design constant rather than Ruleset data, and it is hash-bearing.

⚠ **The sample set *defines* the Cell's value; it does not estimate one.** The area mean of a
line-source field over a Cell does not converge as the sample order rises, because the Segments sit on
the Cell's own edges and the field is unbounded there. **Raising the order is asking a different
question, not refining the answer to this one.** What makes order 2 defensible is not accuracy — it is
that the **ordering between Cells** survives the choice, and ordering is the only thing anything reads.

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`.

**Measurable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md),
and it was measured — twice, and the first measurement **refuted the reasoning this record was opened
to write down**. See `CellDesirabilitySamplingTests`.

## Why

### The reduction exists because the composition is at a Tile and the storage is at a Cell

[`0123`](0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md)
composes desirability at a **Tile**: pollution is a Cell Layer and upsamples, but noise is a line source
whose whole gradient fits inside one Cell, and composing at the Cell would collapse it into *is there a
road here* — the outcome [`0034`](0034-fields-are-sorted-by-source-geometry.md)
sorted fields by geometry to avoid. Land value, however, is **stored per Cell**. Something has to
reduce, and this record is that something.

⚠ **The shipped geometry makes the two obvious samples the two worst ones.** A Cell is
`CellGrid.TilesPerCell` = **32** Tiles and `[roads] block_tiles` is **32**, so the lattice lines land on
Cell edges. A single centre sample is therefore *systematically the quietest Tile in the Cell*, and a
corner sample is *systematically a junction*. They are not two estimates with different errors; they
bracket the truth from opposite sides, permanently, in every Cell of every world generated at the
shipped lattice.

### The reasoning that was written first was wrong, and the measurement said so

What this record was opened to say is: *four quadrant centres is the lowest order that estimates the
Cell's mean well enough, so raise the order and watch the answer settle.* It does not settle. Measured
on a 32-Tile lattice with uniform volume, one Cell, in Q16.16:

| Order | Samples | Mean desirability | Moved from the order below |
|---|---|---|---|
| 1 | 1 (the centre) | **−252,011** | — |
| 2 | 4 | **−296,734** | 17.7% |
| 4 | 16 | **−323,982** | 9.2% |
| 8 | 64 | **−337,116** | 4.1% |

**Each step still moves, and it moves in one direction.** That is what a divergent integral looks like
sampled: a line source falls off with distance, the sources are *on the boundary*, so every refinement
puts more samples near a place where the field is large and the mean keeps climbing. There is no limit
to converge on and no order at which the answer is right.

⚠ **This is [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
working, not failing.** The claim *order 2 is enough* named a number that would refute it and a machine
that would produce it, so it was typed **measurable** and run rather than argued — and it was refuted.
Had it been settled by argument the constant would have shipped with a derivation that reads well and
is false, which is the more expensive outcome by far, because nothing would ever have gone red.

### So the property that has to survive is rank, and rank does

Land value is read by **comparison** — this Cell against that one — and never as an absolute. Nothing
in the build reads an absolute land value today, which is
[`0125`](0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md)'s
whole subject. So the question a sample order has to answer is not *how close is this to the mean* but
**does it order the Cells the way a finer sample would**.

Measured on a varied world — a 6×6 lattice with per-Segment volumes and two pollution sources of
different strength, 36 Cells, 630 pairs — order 2 against order 8:

- **615 of 630 pairs order identically.**
- **All 15 that disagree are pairs the fine sample puts within 1% of each other** — a tie broken
  differently, not a rank inverted.

That makes the order a **scale** choice rather than a design choice, and it is why 2 is enough while the
magnitudes are not converging. `CellDesirabilitySamplingTests` pins both halves: that the magnitudes do
**not** settle, so nobody re-derives convergence, and that the ordering does.

### The mean, and not the minimum or the maximum

`02 §2.5` question 3 already answered **superposes** for both terms — noise sums across sources and
pollution is a convolution — so a nearest-dominates reduction would be the wrong shape. And land value
describes what an ordinary Address in the Cell experiences rather than what its worst Tile does; a
minimum would make one junction condemn a Cell that is quiet everywhere else.

### A `const` rather than Ruleset data, and the reason is written rather than assumed

[`0015`](0015-all-tuning-data-is-hot-reloadable.md) says everything the designer would want to
change is Ruleset data, and `CLAUDE.md` calls a `const` where a Ruleset value belongs a **defect**. This
one is hash-bearing *and* not a designer's number, which is the pair `0015` has no slot for: a designer
tunes what a road costs and how loud it is, and nobody tunes the order of a quadrature rule. Exposing it
as a key would let a hot reload silently rewrite every Cell's target — and, worse, would present a
number as tunable when the table above shows that changing it changes **what the field means**, not how
strong it is.

⚠ **It is registered in `plans/0002` §D1 all the same**, because hash-bearing and unratified is exactly
what §D1 is a ledger of, and living in source rather than in TOML is not an exemption from
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).

## What this costs

**Four `Desirability` calls per resident Cell on the land value cadence**, which is one Tick in 256.
Each call is one Cell-Layer read and one line-source query, so the reduction multiplies the query cost
by four. A `plans/0013` row is filed with the multiplicand marked **guessed** — and ⚠ **it is guessed at
zero today**, because the only thing in the build that creates a Cell row is a pollution emission and
**no shipped Ruleset emits any**.

## What this refuses

**A finer Cell grid.** `02 §2.5` guard rule 2 sends a short-range field to a query rather than to a
finer grid, and this record does not reopen it — the reduction happens at the storage boundary, not in
the field.

**A stored desirability.** `02 §2.4`: a stored composite needs invalidating whenever any input changes
and drifts. Land value is the stated exception because it has *memory*; the four samples are recomputed
on every cadence Tick and nothing about them is cached.

## When to revisit

**When a term arrives that is smooth inside a Cell**, amenity being the candidate
([`0123`](0123-desirability-ships-without-its-only-positive-term-and-a-caveat-that-must-travel-gets-a-test.md),
milestone 15). A smooth term shifts weight away from the divergent one, the magnitudes may begin to
settle, and `The_area_mean_of_a_line_source_does_not_converge_with_sample_order` goes **red** — which is
the signal to reopen this, and is why that test asserts non-convergence rather than merely recording it.

**When something reads an absolute land value.** Rank stability is a sufficient property only while rank
is all that is read. Milestone 13's price surface is the first consumer that would not be satisfied by
it, and it is the same trigger `0125` names for the weights.
