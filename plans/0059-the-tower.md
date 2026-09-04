# 0059 — The Tower

Scoped and landed 2026-09-03, against `the-plot-ratio` at `710eabd`. **A sixth block pattern, and the
proxy it broke on the way in.**

⚠ **This is [`0058`](0058-the-plot-ratio.md) **Q2**, and it is only writeable because `0058` landed.**
Under the old reading — a rung being a storey count — a form on a quarter of its block was **two
storeys on a small parcel**, which is a shed. The plot ratio is what turns a small footprint into a
tall building instead of a short one.

---

## The form

**`BlockPattern.Tower` — one Address, on one face, on half the block each way.**

| | |
|---|---|
| `Carries` | **South only.** One face and not two, which is what makes it one Building rather than a pair meeting along the centre line |
| `ParcelsPerFace` | **1**, as `Courtyard` and `Slab` already are |
| `DepthTiles` | **half the block**, so a quarter of its ground and **three quarters left open** |
| `Narrow` | **new** — shrinks the parcel inside its own slice and centres what is left |

🔴 **`Narrow` is the one thing a tower needs that no other form does.** Every pattern above it says how
**deep** a parcel runs and lets the face's own division say how **wide**, which is right for a row of
plots and wrong for a form whose whole claim is that it does not use the frontage it was given.
***A tower with `ParcelsPerFace` of one and no narrowing is a slab***, because one parcel over one
face is the face.

⚠ **The fraction is priced by the top of the ladder and was measured before it was chosen.** A rung
names a plot ratio, so a form on a **ninth** of its block — a third each way, which is `Courtyard`'s
own fraction and was the first thing tried — has to stand nine times the ratio: **174 storeys at the
top rung of `storeys_per_rung = 3`, which is 609 m and absurd.** Half each way is a quarter of the
ground and **68 storeys**. ***The footprint of the tallest form is what prices the top of the
ladder***, which is not a relationship anybody would look for before building one.

---

## F1 — Ground behind a door was a proxy, and the tower is what it was wrong about

**`BlockPatterns.Ladder` sorted on claimed ground per Address.** With the tower added, at the shipped
lattice, that put it at **rung 1** — between a suburb and a terrace, standing 51 storeys in the
second-sparsest band. At `lots_per_segment = 4` it sorted to **rung 0**.

🔴 **The quantity was standing in for something else.** *Land behind one door* was a proxy for *people
behind one door*, and it holds for five forms that all house people by going **back**. A tower houses
them **up**. ***A proxy is only visible as one when something arrives that it is wrong about.***

**The replacement needs no argument, because `0058` made the real quantity computable.** Floor area on
a block is `ratio × blockTiles²` and the pattern cancels ([`0058`](0058-the-plot-ratio.md) **F5**), so
floor area per Address is `ratio × blockTiles² ÷ addresses` — and the ratio cancels too. **The ladder
sorts on the Address count, descending. Fewer doors is denser.** Claimed ground survives as the
tie-break and only as that.

⚠ **The five keep the order they had.** At the shipped lattice the door counts are 8, 8, 5, 4, 2 and
the tower's 1, so this replaced the quantity **without moving the ladder it produced** — which is the
strongest evidence available that the proxy was a good one right up until it was not. ⚠ **Detached and
Perimeter TIE on doors there** and are separated by claimed ground, 624 against 1,024: the tie-break
earning its place at the one lattice every picture is drawn from.

⚠ **And it is still not the enum order.** **28 of the 84 reachable lattices depart from it**, so the
ladder is still computed per lattice rather than fixed once, and the machinery that does so is still
earning its place.

## F2 — The shipped ladder, and what the city looks like

At `block_tiles = 32`, `lots_per_segment = 5`, `storeys_per_rung = 3`:

| rung | pattern | doors | claims | storeys |
|---|---|---|---|---|
| 0 | detached | 8 | 61% | 3 |
| 1 | perimeter | 8 | 100% | 5 |
| 2 | back-to-back | 5 | 100% | 8 |
| 3 | courtyard | 4 | 86% | 12 |
| 4 | slab | 2 | 100% | 14 |
| 5 | **tower** | **1** | **25%** | **68** |

Driven on `platted.toml` at 20,000 Citizens, Tick 21,514: **235 Buildings, of which 12 stand 241.5 m**,
on footprints of **384 and 960 m²** against the city's 700-to-2,600 m² ordinary range.
***A tall building is now a smaller building, which is the sentence none of this could say yesterday.***

## F3 — A re-plat now replaces more ground with less, and a test title says it never does

`RecarveTests.A_pattern_claiming_less_ground_never_replaces_one` asserts the ratchet. **Its title is now
literally false**: a tower claims a quarter of its block against a slab's whole one and sits *above*
it, so a pattern claiming less ground does replace one, and must.

⚠ **The name is kept on purpose, because the failure it guards against is the one it was named for** —
the ratchet reading *area* rather than the *ladder*. `plans/0053` retired the area proxy when
`Courtyard` was found claiming less than `BackToBack`; ***the tower is the case that would have made
the old rule absurd rather than merely wrong.***

⚠ **Three tests in that file asserted the literal name `Slab` as *what the top band gets*.** They now
assert `BlockPatterns.Count - 1` through `Rung`, because the name was a **second copy of the ladder's
ordering** sitting in a file about re-platting — `plans/0012` *Cause 1* in miniature, and it went red
the moment a sixth rung existed.

## F4 — The golden baseline did not move, and that is worth a sentence

**A sixth rung, a new sort quantity and a new geometry moved no recorded hash.** `declining.toml` and
`congested.toml` declare no `[[band]]`, so every block on them is `Detached`, whose storeys are
unchanged since [`0058`](0058-the-plot-ratio.md). ***The baseline is blind to the pattern ladder
entirely***, which is a real gap rather than good news: every ladder change to date has had to be
verified somewhere else.

---

## Open

**Q1 — one door for seventy storeys.** A tower's floor area is about `256 × 68` Tiles behind a single
Address, so every Trip its residents make starts at one point. **That is correct for a tower and it
makes `AddressCount` do two jobs**: an *entrance count*, which the movement model reads, and an
*intensity rank*, which F1 now sorts on. ***They agree today because a form with one entrance really is
the densest one***, and nothing says the next form will keep them agreeing.

**Q2 — the Address sits off the footprint.** `Narrow` centres the parcel and the door stays at the
first Address on the face, so a tower's street door is up to half a block from the tower. A Lot's
position and its footprint have been different quantities since `plans/0052`; **the tower is where the
gap first becomes visible.** Whether the door should follow the parcel, or the parcel the door, is
undesigned rather than unbuilt (`adr/0070`).

**Q3 — three quarters of a tower block is open ground and nothing uses it.** `Detached`'s leftover is
scrub and `Courtyard`'s is a courtyard; a tower's is a plaza, and all three are the same absence of a
mechanism. The `yard` layer draws the parcel remainder, not the block remainder.

**Q4 — [`0058`](0058-the-plot-ratio.md) **Q1** is now sharper, not answered.** The sparsest rung's plot
ratio decides how big every generated city is, and the ladder is a rung longer, so `ForBand` divides
the same bands across six rungs instead of five. **Every banded world's pattern assignment moved** and
no instrument reports a generated city's extent — `0058` **Q3**.
