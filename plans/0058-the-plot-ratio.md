# 0058 — The Plot Ratio

Scoped and landed 2026-09-03, against `the-vintage` at `6d3edf2`. **A rung named a height, and a
height applied to whatever footprint the pattern happened to carve is why the city could only build
out.**

⚠ **This is [`0057`](0057-the-vintage.md) **Q1**, and it is the third lever of the three that plan put
up.** The other two are refused there with the measurement that refuses them; do not re-propose them
from this document without reading that section.

---

## The change, in one line

***The rung names a plot ratio and the storeys fall out of the ground the form declined to take.***

```
ratio    = rung × storeys_per_rung + 2          floor area per unit of BLOCK
storeys  = ratio × blockTiles² ÷ claimedTiles   what it takes to deliver that
```

Before, `BlockPatterns.Storeys` returned `rung × step + 2` **as a storey count**. It was applied to
whatever footprint the pattern carved, so height and footprint were one decision and the city had no
way to put people up rather than back.

⚠ **The ladder's own quantity is unchanged and was never the defect.** `BlockPatterns.Ladder` still
sorts on ground behind one front door, which is a fine ordering of five forms that all house people by
going back. **What was wrong was reading its index as a height.**

---

## F1 — The floor of two stopped being a clamp and became a consequence

The plot ratio's floor is 2 and **no form claims more than its whole block**, so `ratio × block ÷
claimed` is at least 2 for every pattern that claims anything. The clamp survives in the code for the
degenerate lattice, not for the design.

⚠ **The argument it carried is untouched** — *a building with no upper floor is a shed*. What changed
is that nothing has to enforce it.

## F2 — Only the two forms that leave ground open moved, and they moved up

At the shipped lattice, `block_tiles = 32`, `lots_per_segment = 5`, `storeys_per_rung = 3`:

| rung | pattern | claims | was | is |
|---|---|---|---|---|
| 0 | detached | 624 / 1,024 | 2 | **3** |
| 1 | perimeter | 1,024 / 1,024 | 5 | 5 |
| 2 | back-to-back | 1,024 / 1,024 | 8 | 8 |
| 3 | courtyard | 880 / 1,024 | 11 | **12** |
| 4 | slab | 1,024 / 1,024 | 14 | 14 |

***Three of the five are bit-identical because they cover their block***, and a ratio applied to the
whole block is a storey count. The two that stand back off their ground are the two that changed, and
upward is the direction this exists to enable.

## F3 — 🔴 Every generated city got smaller, by up to a third, and no test named that as its subject

`SyntheticCity.Subdivide` stops when the Lots it has laid can hold the population, and occupancy is
floor area. **More storeys per Lot is fewer Lots.** At 4,000 Citizens:

| Ruleset | Buildings before | after |
|---|---|---|
| `minimal.toml` | 488 | **304** |
| `twinned.toml` | 478 | 319 |
| `platted.toml` | 74 | **46** |

⚠ **`minimal.toml` is the worst case and the reason is that it declares no `[[band]]`**, so every
block is `Detached` — the one form whose height moved most. A file with bands spreads the change
across five forms and loses less.

⚠ **It is the mechanism working.** A denser city houses the same people on less ground; that is what
density *is*. But ***nothing in the suite asserts a generated city's extent***, so a 38% contraction
arrived as three unrelated-looking failures in three unrelated files, and each of them had to be
diagnosed separately before the common cause was visible. **F4.**

## F4 — Three tests failed, one cause, and not one of them mentions the generator

| Test | What it said | What it was |
|---|---|---|
| `TripDumpTests.No_band_reports_a_walk_of_no_time_at_all` | exit code 3 | `severance.toml` at 400 Citizens is now **one Building**, and one Building has no pair to walk between |
| `CommuteLongRunTests.The_Day_has_two_peaks…` | *"one quarter-hour holds 81 of 646 journeys… the draws behind it are quantised"* | fewer Buildings is fewer Workplaces is fewer distinct Shift starts |
| `TasteTests.A_household_that_wants_the_centre_ends_up_nearer_it` | *"the preference is not reaching placement"* | the city no longer spans enough ground for the preference to have anywhere to express itself |

**Each was confirmed by restoring the world rather than by reasoning about it**: `severance.toml` at
2,000, `PeakPopulation` 4,000 → 6,000, `TasteIsMeasurable` 8,000 → 12,000. ⚠ **That is re-siting a
fixture and it is the thing this repository warns against**, so it is named here rather than done
quietly: the diagnosis is *the world these tests were written against no longer exists at that
population*, and the check on the diagnosis is that the property returns when the world does.

🔴 **`TasteTests` carries a comment forbidding exactly this move — *"Do NOT re-site this at a smaller
city to make it pass"* — and this is the opposite direction, which is why it was allowed.** ***A rule
against weakening a fixture is not a rule against re-sizing one, and the two are one edit apart.***

## F5 — 🔴 The change made density and form ORTHOGONAL, and nobody designed that

Floor area is `claimed × storeys`, and `storeys = ratio × block ÷ claimed`. **The `claimed` cancels.**

```
floor area on a block  =  ratio × blockTiles²
```

***Every pattern at the same rung houses exactly the same number of people, whatever shape it is.***
A slab, a courtyard block and a hypothetical tower at rung 4 are one density in three shapes. **That
is the property this change was reaching for and it arrived as an identity rather than as a design** —
it was not noticed until the heights were measured back off the drawing.

⚠ **And it retires the ladder's purpose without retiring the ladder.** `BlockPatterns.Ladder` orders
forms by ground behind one door, which was an ordering by *intensity* while a rung was a height. A
rung is a plot ratio now and intensity is the rung, so ***the ladder is ordering shapes on a quantity
that no longer measures what it is being used to pick.*** It still works, because the five forms
happen to run suburb-to-slab in that order; nothing says the sixth will.

## F6 — And the visible payoff is small, because four of the five forms already cover their block

⚠ **Stated because the commit could be read as delivering more than it does.** The claimed shares are
61%, 100%, 100%, 86%, 100%. **The correction is a no-op for three forms and one storey for the other
two**, so the drawing after this change is the drawing before it with the bands mixed — and the mixing
is `pattern_spread`'s doing, not the plot ratio's.

Measured on the wash's own draw list, `platted.toml`, 20,000 Citizens, Tick 21,514, 240 Building
instances: **15 distinct heights from 10.5 m to 59.5 m**, and mean footprint still climbing with rung
— 704, 866, 998, 2,610, 1,149 m². ***Height and footprint are still correlated, because within a rung
the pattern is fixed and the pattern is the footprint.***

🔴 **The plot ratio is the mechanism a small-footprint form needs and it is not itself that form.**
Nothing in this change puts a tower in the city; it makes one expressible. Q2.

---

## The near-tie draw

**`[lots] pattern_spread` — how many rungs either side of its band's own rung a block's form is drawn,
symmetrically.** `PurposeTag.BlockPattern`, drawn on the block's coordinates, `Ticks.Zero`.

🔴 **A density was a value and is now a distribution.** Without it `ForBand` indexes a total order, so
one band gets one form for ever — which is precisely what [`0057`](0057-the-vintage.md) **F1**
photographed: five clean rings with the boundaries falling on streets, because streets are the only
thing there is to fall on. ***A city is not one form per density; it is a mix around one.***

⚠ **Symmetric, and a one-sided draw was refused.** Drawing only upward never makes a dense band
sparser, which sounds safer and quietly biases every world denser than its Ruleset says. The band's
rung stays the **centre** of what the band means. ⚠ **The clamp at each end is not symmetric and
cannot be** — a band at rung 0 has nothing below it — so the bottom band skews up and the top skews
down. That is a bounded ladder, not this draw.

🔴 **It draws on the BLOCK and never on the BAND, and the re-carve ratchet is the whole reason.**
`LotSubdivider.RecarveBlock` refuses a re-plat that would move a block *down* the ladder, so selection
has to be monotone in the band **for one piece of ground**. A fixed offset added to a rung that rises
with the band still rises with it, and a clamp is monotone too. ***A draw that saw the band would make
upzoning a block and watching it get sparser writeable.*** `A_denser_band_never_gets_a_less_intense_pattern`
now sweeps every spread and 32 blocks for that reason, rather than asserting the property once at
spread 0 and calling the ratchet safe.

⚠ **Absent means 0 and 0 is the old selection exactly.** `rulesets/platted.toml` is the only file that
states it, at **2**. **Unratified**; `adr/0052` is suspended.

---

## Open

**Q1 — is the sparsest rung's plot ratio 2?** F2 turns entirely on it, and 2 was inherited from a
number that used to mean storeys. ⚠ **The ladder is a relative intensity and not a real Floor Area
Ratio** — a real detached suburb is 0.3 to 0.6, and every rung here is above 2 — so the bottom rung is
where the calibration question actually lives. **It is what decides how big every generated city is**
(F3), which is a larger consequence than the height it was chosen for. `plans/0002` §D.

**Q2 — where does a sixth form that houses people UP sort?** ⚠ **The answer moved while this was being
written and the earlier one was wrong.** [`0057`](0057-the-vintage.md) **Q1** priced a tower at an 8×8
parcel — 64 Tiles behind one door, below a bungalow's 78 — and concluded it would be selected in the
suburbs. **At a plausible 12×12 it is 144 a door, which sorts between perimeter (128) and back-to-back
(204): the middle of the city.** At `storeys_per_rung = 3` that rung's ratio is 8, so it would stand
`8 × 1024 ÷ 144` = **56 storeys**. ***A tower is writeable today and would land in a sensible place.***

⚠ **What is genuinely open is F5's half**: the ladder orders forms on a quantity that no longer
measures intensity, so *where* a new form lands is now an accident of its plan rather than a statement
about its density. The five in hand happen to sort suburb-to-slab. **A sixth is where that stops being
lucky**, and the ordering wants an argument before it gets one rather than after.

**ANSWERED 2026-09-03 by [`0059`](0059-the-tower.md), and both estimates above were wrong.** Built and
measured, a tower on a third of the block each way sorts to **rung 1** — not the middle of the city
and not the suburbs, but standing 51 storeys in the second-sparsest band. ***The ordering did want an
argument and it got one on the day***: ground behind a door was a proxy for people behind a door, and
`0058` **F5** is exactly what makes the real quantity computable, so the ladder now sorts on the
Address count and the five keep the order they had.

**Q3 — nothing asserts the extent of a generated city.** F3 is the argument for it: a change to
occupancy silently resizes every world, and the suite finds out through whichever fixture happens to
sit nearest an edge. One instrument printing Tiles paved and Buildings raised per shipped Ruleset
would have turned three diagnoses into one line of a diff. Not written.
