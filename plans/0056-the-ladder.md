# 0056 — The Ladder

Scoped and landed 2026-09-03, against `the-middle` at `de33571`. **The distance between two rungs is a
number somebody chooses now, and it was 1 because nobody had.**

⚠ **This is the second half of [`0049`](0049-visuals.md) row 4 and it only became visible when the
first half landed.** [`0055`](0055-the-middle.md) made the city cross its density ladder for the first
time; what that showed is that the ladder is short.

---

## The finding

**The tallest Building this design could produce was seven storeys — 24.5 m — on every world, at every
population, and no document in the corpus states that number.**

It is not a cap anybody wrote. It is arithmetic:

```
BlockPattern.Count       = 5      five ways to subdivide a block
BlockPatterns.Storeys    = Rung + 2      rungs 0..4  →  2..6 storeys
LotRuleset.StoreysOn     + a draw of 0 or 1          →  2..7 storeys
Main.StoreyMetres        × 3.5 m                     →  7.0 .. 24.5 m
```

🔴 ***The height of the tallest thing in the city was set by how many ways there are to subdivide a
block***, which is a geometry decision that was never about height.

⚠ **Two of the three terms carry an argument and the third does not.** `BlockPatterns.Storeys`'
remarks defend the **floor** — *two is the floor, a building with no upper floor is a shed* — and
`Rung` is a derivation from ground-per-Address. **The distance between rungs carries neither.** Nothing
in `docs/07-the-drawing.md`, `adr/0025`, [`0002`](0002-open-questions.md) or
[`0053`](0053-the-block.md) states an intended maximum height, and no open question named one.

⚠ **A `const` where a Ruleset value belongs is a defect and not a shortcut** (`CLAUDE.md`,
[`adr/0015`](../docs/adr/0015-all-tuning-data-is-hot-reloadable.md)).
The floor of two survives that test because it has a derivation; the step never did.

---

## What shipped

**One optional Ruleset key.** `[lots] storeys_per_rung`, refused outside `[1, 50]`, and **absent means
1** — so no file that does not state it carves anything different.

| | |
|---|---|
| `BlockPatterns.Storeys` | `(Rung × step) + 2`. ⚠ **The floor does not scale** — a shed is a shed whatever the step is, and multiplying the floor would raise the suburb rather than lengthen the ladder |
| `LotRuleset.StoreysOn` | The jitter is **one rung wide**: a draw over `0 .. step` inclusive |
| The refusals | Floor 1, because *a flat ladder is a city with one `[[band]]`* — a step of zero is a second spelling of a city that is already writable. Ceiling 50, because `LotTable.Storeys` is a **byte** and 50 gives `(4 × 50) + 2 + 50 = 252` |

### The jitter's own argument discharged itself

`StoreysOn` read: ***one storey of variation and no more, because two storeys of jitter would blur the
ladder, which is the thing the height is supposed to say.*** That is true of a ladder whose step is 1,
and only of one — a jitter of two then spans three rungs and the height stops naming a density.

**With the step a variable, the jitter's width is derived rather than chosen: one step.** A jitter
narrower than the step cannot blur the ladder; a jitter exactly one step wide is the widest that
leaves the rungs merely *touching* rather than overlapping. ***The argument was never about the
number two; it was about the jitter against the step.***

⚠ **CORRECTED 2026-09-03 by [`0057`](0057-the-vintage.md).** This paragraph said the jitter leaves a
Building's rung *recoverable* from its height, as `(storeys - 2) / step`. **It is one value too
strong.** The draw spans `0..step` **inclusive**, so rung *r*'s top height is rung *r+1*'s bottom and
a Building sitting there is indistinguishable. ⚠ **The overlap is not new** — the original `0 or 1`
mask gave rung *r* `{r+2, r+3}` and rung *r+1* `{r+3, r+4}`, sharing a value in exactly the same way —
so the widening cost nothing that was there before. Making the draw exclusive would close it and
would also delete the jitter entirely at `step = 1`, which is the bit-identity the section below
turns on. Filed in [`0012`](0012-corpus-audit.md).

⚠ **At `storeys_per_rung = 1` the new draw is bit-identical to the mask it replaced** —
`((draw >> 32) & 0xFF) % 2` is `(draw >> 32) & 1` — so **no world that does not state the key moves a
State Hash**, and neither golden trace was re-recorded.

---

## What it looks like

`rulesets/platted.toml` is the only shipped file that states the key, because it is the file whose
whole job is to draw the ladder. **It states 3**, which is chosen against a band and not derived: the
low rung has to stay a house and the high one has to read as a different *kind* of building from
across the city.

Measured at 10,000 Citizens, off the shell's own `draw` dump:

| | before | after |
|---|---|---|
| Distinct drawn heights | **6** — 7.0 to 24.5 m | **16** — 7.0 to 59.5 m |
| Mean height, 24-Tile rings out from the middle | — | **53.8 → 35.4 → 21.2 → 12.4 → 11.5 m** |
| Buildings | 373 | **181** |
| Built extent | 251 Tiles square | **187** |

🔴 **THE CITY GOT SMALLER AND THAT IS THE KEY WORKING.** Capacity is floor area
([`0053`](0053-the-block.md)), so a taller Building houses more and 10,000 Citizens need fewer of them
on less ground. ***A density ladder that did not shrink the city would not be a density ladder.***

✅ **`Main.PitchCeilingMetres` stays at 24 m and turns from an accident into a rule.** *Tall things are
not gabled* is right; what was wrong is that the ladder used to trip it with its top rung alone and by
**half a metre**, so it never separated anything. At a step of 3 it is a real low-rise/mid-rise split
and the core reads flat-roofed against a pitched rim. ⚠ **This was very nearly raised instead**, on
the reasoning that the top half of the city would go flat — which is the correct behaviour stated as a
complaint.

---

## What is open

| # | Question | Where it goes |
|---|---|---|
| **Q1** | 🔴 **Height is one axis and the city now reads as ONE MASS at the top of it.** The tall Buildings are all the same pale wall against the same flat roof, so a dense core is a lump rather than a row of distinct buildings. ***Variation in height without variation in anything else buys a silhouette and not a city.*** | **The next sitting**, and it is the reason this one was asked for |
| **Q2** | **3 is chosen, unratified, and hash-bearing on one file.** `adr/0052` is suspended under [`0045`](0045-amnesty.md), so it is stamped provisional rather than given a ratifier. **What would move it is a picture and not a census figure** | [`0002`](0002-open-questions.md) §D, when the amnesty lifts |
| **Q3** | **Only one shipped file states the key.** Every other world still tops out at seven storeys, which is correct — they demonstrate other things — but it means ***the ladder is exercised by one fixture*** | [`0050`](0050-the-ruleset-sweep.md) |
