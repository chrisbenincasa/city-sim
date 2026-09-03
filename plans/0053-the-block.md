# 0053 — The Block

Scoped 2026-09-02, against `the-parcel` at `5dbfd0a`. **A block becomes a thing the city knows about.**

⚠ **Written under the same temporary relief from [`0045`](0045-amnesty.md)** granted to
[`0052`](0052-the-parcel.md), and for the same reason: this is *planning*. The ADRs it names are
written when the shape is agreed, not now.

🔴 **This is the trunk. [`0052`](0052-the-parcel.md) is step 5 of it** and was scoped before this
document existed, so read that one for the parcel's findings — **G1**–**G8**, the Sealing
measurements and Q1–Q6 all live there in full and are not repeated here.

---

## The finding

**A block is not represented anywhere, so there is nowhere to put a per-block decision, so every block
is identical by construction.**

This was found by proposing five different block subdivisions in one sitting and watching every one of
them turn into a world constant. It was not the patterns. ***A pattern with nowhere to live becomes a
default.***

| What was looked for | What is there |
|---|---|
| A `BlockTable` | 🔴 **None.** A block is a `(column, row)` index into `StreetGrid` with no state at all |
| A block's Zone | 🔴 **On the Lot.** `LotTable.Zone`, and `LotSubdivider.Relot` reads a block's Zone back **off whichever Lots survived on it** |
| A density band | 🔴 **Nothing in `Borough.Core` at all.** [`adr/0025`](../docs/adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md) decided bands exist; nothing implemented one |

⚠ **The Zone limitation is already known and already written down**, in `LotSubdivider.Relot`'s own
remarks — which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
working, and the only reason this document did not have to discover it:

> **A block's Zone is read off the Lots that survived on it**, because land does not carry a permission
> set of its own… So a block that was zoned and then lost *every* Lot has forgotten it was zoned…
> **That is a real limitation and it is named here rather than hidden.**

### And `adr/0025` already asked for the fix

> **Lot subdivision must vary by band**, per §2.2, and re-subdivision still only touches vacant land.

***That sentence is this entire document, decided, written down, and undischarged.*** Until there is
more than one band there is one pattern, whatever the pattern set contains — so **adding patterns
before adding bands produces a uniform city made of a different shape.**

---

## The five steps, in dependency order

| # | Step | Hash | What it unblocks |
|---|---|---|---|
| **1** | **A Block is a row.** `BlockTable`, keyed by lattice position. Saved: zone, band, **and the pattern it was carved with**. `zone` writes the block rather than the Lots | yes | Somewhere for a per-block decision to live. Discharges `Relot`'s named limitation |
| **2** | ✅ **Bands ship** — *2026-09-02*. `adr/0025`'s cap is a real value on the block **and the Zone Rule reads it**. ⚠ **The generator paints them** — see *The two calls* | no, and that is a finding | More than one of anything |
| **3** | ✅ **A pattern is a partition function** — *2026-09-02*. Three to start, the set open | no, and that is a finding | (a), (b) and (c) all become expressible, and none is privileged |
| **4** | 🟡 **Carve and re-carve** — *2026-09-02*. Selection at carve time, frozen while occupied, re-platted when vacant. ⚠ **Selection reads the band and nothing else**, so the city is banded rather than varied | yes, and it is an ENCODING move | ***The city stops being uniform*** — partly |
| **5** | ✅ **The parcel, then the footprint** — *2026-09-02*, [`0052`](0052-the-parcel.md) stage 1 in full. ⚠ **It did not flatten the picture as that plan predicted** — see *What step 5 shipped* | yes, and Sealing moved | Sealing, Fertility, Woodland, `adr/0025`'s Land axis, and F21 becoming **impossible** |

⚠ **Steps 1–2 are the trunk and neither is in [`0052`](0052-the-parcel.md).** That plan is not wrong;
it is a leaf on a tree that does not exist yet.

---

## What is saved and what is derived, which is the subtle part

| | Disposition | Why |
|---|---|---|
| A block's **zone** and **band** | **Saved** | The player set them, or the generator did. Nothing derives a permission |
| A block's **pattern** | 🔴 **Saved** | ***It is a historical fact about conditions that are gone.*** The block was carved when land value here was low; it is not low now; **the pattern cannot be recomputed** |
| A Lot's **parcel** | ✅ **Derived** | A pure function of the block's saved pattern and the lattice, exactly as frontage is a pure function of the Lot's saved position |

⚠ **This is [`adr/0078`](../docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md)'s
own test applied honestly, and it lands on opposite sides for the two.** That ADR's rule is *a fact
computed from other saved facts must not be stored beside them*. A frontage **is** so computable. A
carving decision **is not** — the inputs are gone. ***So saving the pattern is not the shape `adr/0063`
and `adr/0064` were deleted for; saving the parcel would be.***

---

## The pattern set

**Three to start**, one per band, chosen because each is a real subdivision of a real city block and
because three is the fewest that can look unlike each other.

| Pattern | Shape | Exhaustive? | Grounded in |
|---|---|---|---|
| **Detached** | Plots on all four faces, ground left over between the back fences | 🔴 **No, deliberately** | A suburban block. The leftover ground is **correct** — back gardens do not meet, and there is scrub between them |
| **Back-to-back** | One pair of streets takes the whole block; plots meet along the centre line; the cross streets get gable ends | ✅ Yes | A Portland 200 ft block — eight 50 × 100 ft lots in two rows. A British terrace. Manhattan |
| **Perimeter** | All four faces; the winning pair takes a shallow strip, the losing pair splits the middle band down the centre | ✅ Yes | Barcelona, Paris, Berlin. ⚠ **The two pairs get different depths, and that difference is a number this plan owes** |

🔴 ⚠ **THE EXHAUSTIVENESS TEST ASSERTS EACH PATTERN AGAINST ITS OWN CLAIM, NEVER AGAINST ONE RULE.**
A pattern declares whether it tiles the block; the test then checks that no Tile is claimed twice
**by any pattern**, and that no Tile is unclaimed **by a pattern that said it was exhaustive**.
***Overlap is a defect in all three; leftover ground is a defect in two of them and the point of the
third.***

### What was rejected, and why it is recorded

⚠ **The mitre — four faces each taking a triangle to the block's centre.** Tiles a square exactly, is
non-degenerate, and keeps every face's Lots. **Refused because buildings are not built that way**: at a
real corner one street wins and the cross street's terrace begins after the corner building, which is
what `PastTheCorner`'s existing allocation already does and already says. ***A geometry that tiles is
not thereby a geometry that is built.*** The shell reached the same conclusion for the drawing one
commit in, and renamed the method.

---

## Carve and re-carve, which is where the variation comes from

**Selection happens at carve time, from what the block can see: its band, its zone, and the land value
and Building density already standing around it.** Both fields exist, both are already built, and both
vary continuously across the map.

***Then it is frozen while the block is occupied***, which is `02 §2.2`'s existing preservation rule
and needs nothing new:

> Re-subdivision happens when the street network changes, and **must preserve existing Buildings — only
> vacant land re-parcels.**

🔴 **So the city's texture is its growth history**, and this is the whole answer to *how does it stop
looking uniform*: **a block carved when its surroundings were cheap keeps its coarse plots when the
area becomes expensive.** No randomness, no jitter, no authored variety table. ***A city that grew over
time into changing conditions looks like one.***

### Two re-carve mechanisms, at two granularities

| | Trigger | Granularity | Grounded in |
|---|---|---|---|
| **Whole-block re-carve** | The block is **entirely vacant** and a Rule wants a kind the pattern cannot hold | The whole block, dramatically | `adr/0025`: *"upzoning a built block does nothing until its Buildings go, **which is how redevelopment becomes a real endgame activity rather than a formality**"* |
| **Amalgamation** | **Adjacent vacant Lots**, where the band admits a kind needing more ground than either holds | Two Lots, incrementally | ⚠ **Not new** — it is `adr/0025`'s **Subdivide** route run backwards, and that ADR already names Subdivide and Stack as the two physically distinct routes to density |

⚠ **The coarse one alone is not enough**, and the reason is legible: a block with three derelicts and
five standing Buildings never redevelops, it just has gaps. **That is real** — it is what a declining
neighbourhood looks like — **but it means every redevelopment waits for a whole block to empty**, where
real redevelopment proceeds two plots at a time.

✅ **The trigger is the same term [`0052`](0052-the-parcel.md) **G1** already says stage 2 needs.**
Vacancy is the *permission*; the *trigger* is a Zone Rule wanting to build and finding the parcel wrong
for its kind. So the parcel↔kind matching term does double duty and is not a second mechanism.

### 🔴 The difference from SC4, and the gap it leaves

**SC4 tears a building down because conditions changed.** Here a Building goes because its Rules starve
and it is condemned — [`adr/0053`](../docs/adr/0053-failure-pressure-is-a-duration-not-a-tally.md),
failure pressure as a duration. ***So a healthy old block never redevelops, where in SC4 it would.***

⚠ **In reality a great deal of redevelopment happens to perfectly functional buildings, because the
land became worth more than what stands on it.** Nothing in this design has decided whether land value
may condemn a healthy Building. Under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) that is
***undesigned*** rather than refused — so it is **Q4** below and not a constraint on anything here.

### ⚠ `adr/0006`: amalgamation is a sink and subdivision is a source

**A pair that can push both ways on the same conditions can oscillate**, and a block that re-carves
every time a threshold is crossed is churn with a State Hash moving under it. The trigger needs
hysteresis, or it needs to be one-directional per condition. **Named now rather than solved now** —
[`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md) is what it would be filed
against, and **Q3** is where it is owed.

---

## The two calls this plan makes, with the arguments

**1. The generator paints bands. They are not derived from land value.**

⚠ **A band derived from land value is [`adr/0025`](../docs/adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s
rejected road-derived cap wearing a different hat**, and that ADR is explicit about why:

> **A road-derived cap would pre-empt the lesson the engine exists to teach** — refusing to let the
> mistake happen instead of letting it happen and explaining it.

Deriving from land value instead of road tier does not change the objection. ✅ **So the band is
world-creation data**: a Ruleset declares its bands and the generator lays them out **exactly as it
lays zones today**, and the player verb arrives later and overwrites. ***This contradicts nothing and
leaves no scaffolding to remove.*** ⚠ **It is not a derived cap and must never become one** — the
generator setting an initial value is the same act as the generator zoning land, and is not a rule
reading conditions.

**2. Three patterns, one per band, to start.**

Enough for selection to matter; few enough to tune against something legible. ⚠ **The set is open by
construction** and the number is a starting point rather than a decision — what is decided is that
**a pattern declares its own exhaustiveness claim**, because that is what lets a suburban block keep
its leftover ground without weakening the test that kills the dead interior everywhere else.

---

## What step 2 shipped

**Shipped 2026-09-02**, in two commits. A `[[band]]` table in the Ruleset, a `Band` on every carved
block, a generator that paints rings, and **one `&` in `ZoneRuleEngine.Admits`**.

```
(_world.Lots.Zone[lot] & definition.Admits & _world.BandAdmitting(lot)) != 0
```

***That third term is the whole of `adr/0025`'s cap.*** A band is applied by intersection, so it joins
the expression that was already there rather than becoming a predicate of its own, and it can only
ever take a permission away.

⚠ **The State Hash did not move, and that is a finding rather than an accident.** Every absence
answers all-bits-set — a Lot off the lattice, a block with no row, band `0`, a Ruleset with no
`[[band]]` — so a bandless world computes *exactly* the expression this replaced. `adr/0025`'s cap
therefore arrives with no migration and no baseline to re-record. **The restrictive reading would have
given every Lot in every bandless world a permission set of zero and built no city at all.**

### 🔴 The case the demonstration had to show, and did not at first

`banded.toml` shipped in the first commit as `minimal.toml` plus two `[[band]]` tables — and
`minimal.toml` has **one** Zone Rule, on bit 0, which **both** bands admit. ***So the cap was wired in
and refused nothing***, and the suite was green because there was nothing for it to be green about.

**A demonstration Ruleset that cannot express the thing it demonstrates is a fixture, not a
demonstration.** The repair is a second kind — `shopfront`, deliberately the smallest one that loads —
and a Zone Rule on bit 1, so that `suburban` admitting `[0]` has something to withhold.

⚠ **The case worth asserting is a Lot that CARRIES the trade bit and is refused anyway.** Land refused
because nobody zoned it proves nothing, because nothing zoned it. `BandAdmissionTests` asserts **both**
directions in one world — some Lots refused, some kept — because a `BandAdmitting` hard-wired to zero
would satisfy the first half alone.

### ⚠ The `LotTable.BlockSlot` column in the table above was not built

The step table says *a Lot's parcel is derived*; this plan also proposed a derived `BlockSlot` column
to get from a Lot to its block. **`Frontage.BlockOf` is that, computed** — ten integer operations off
the Lot's saved position and side, reading nothing derived.

🔴 **A position alone cannot answer it, and the side is what closes it.** Two blocks share every face
line: the Segment at lattice row `r` is the north block's south face and the south block's north face,
and **both lay Lots along it**. Which block a Lot belongs to is therefore a property of *which side of
the street it stands on*, which is exactly what `adr/0074` says an Address is for.

The column is the right answer the day something wants this in bulk. Nothing does: a column would cost
a clear on every `RebuildDerived` — [`0052`](0052-the-parcel.md)'s Q3 measured that pass as
`O(capacity)` and dominated by its clears — to save ten operations on a sample of three.

### Three things the corpus caught that the compiler did not, in step 2

Recording these because each is a guard earning its cost, and none of the three was on the checklist:

- **`Ruleset.WithLayers` did not carry `Bands`.** A hot-reload would have returned a Ruleset with its
  bands silently at their defaults. `RulesetWithLayersTests` names the property and the second site.
- **`adr/0048`'s refusal count went 217 → 223**, and `RefusalCountTests` reads the loader rather than
  trusting the ADR. Six new refusals, enumerated there.
- **A doc comment orphaned itself.** Inserting `ReadBands` above `ReadZoneRules` left the latter's
  documentation bound to the former. `DocCommentAttachmentTests`, third time this session.

### ⚠ A new key kind, which is a shape this Ruleset had never had

`admits = [0, 1]` is **a bare array of whole numbers**, and every other array key in the Ruleset —
`inputs`, `bins`, `prices` — is an array of *inline tables*. The loader had one `Array` kind meaning
the second, so the generated schema published `admits` as an array of objects and the key reference
printed *array of inline tables*. ***Both artefacts were wrong in the same way and neither would have
failed a test***, because the generators and the checks read the same enum.

`RulesetKeyKind.Numbers` is the repair. ⚠ **The alternative was to bend the file to the loader** —
`admits = [ { zone = 0 } ]` — and that is the wrong direction: a set of bit indices has no field to
name.

---

## What step 3 shipped

**Shipped 2026-09-02.** `BlockPattern` — three of them — and `BlockPatterns.Carve`, which is the
partition function. `LotSubdivider` no longer knows the shape of a block: it reads the block's saved
pattern, walks the parcels that pattern yields, and turns each into an Address.

⚠ **The four faces used to be four calls with their sides written out as constants**, which is a
partition with one shape compiled into it. That is why this is a rewrite of the carve rather than a
branch inside it.

| Pattern | Faces | Depth | Exhaustive |
|---|---|---|---|
| **Detached** | all four | the strip | 🔴 no, and the leftover is the point |
| **Back-to-back** | the winning pair only | half the block | ✅ yes |
| **Perimeter** | all four | strip on the winner, half-block on the loser | ✅ yes |

**The State Hash did not move.** `Detached` is `0`, `0` is what a fresh row holds, and `Detached`
reproduces the old carve Lot for Lot and in the same order — face order is `BlockFace`'s order, which
is the order the four hard-coded calls were in.

### ✅ Q1 answered, and the answer was already in the code wearing another name

**The perimeter pattern is exhaustive for any winner-depth**, which is what made the depth a free
number and Q1 a real question. ***It is not free: `LotSubdivider.CornerTiles` was already computing
it.***

That method reserved ground at each end of the north–south faces so the corner belongs to one face,
and **its own doc comment said the reservation was standing in for a depth the class had none of**:

> ⚠ **THE RESERVATION IS ONE LOT'S FRONTAGE AND IT IS NOT A DEPTH.** A depth is what the corner is
> really made of, and this class has none to offer.

It *is* the depth. A corner reservation and a parcel's depth are one quantity seen from two
directions, so the formula moved to `BlockPatterns.StripTiles` and `CornerTiles` delegates —
**written apart they would have drifted.**

Both its terms are derived. `block_tiles ÷ lots_per_segment` is one Lot's frontage, so a strip that
deep makes a square-ish plot — **a plot is about as deep as it is wide**, which is the only shape
claim in the file. `block_tiles ÷ 4` caps it so the middle band never closes.

⚠ **`adr/0078` is untouched.** What it refused is an **authored** depth key, and there still is not
one: this is a consequence of two keys that already existed.

### 🔴 The dead block interior stopped being a property of every block

`plans/0012` had already filed the unconditional version as **Cause 4** — ***a response identical
whatever the player does is a constant, not a punishment*** — and said the lever would arrive in
[`0052`](0052-the-parcel.md). **It arrived here instead.** Two of the three patterns tile their block
exactly; the interior is now a property of `Detached`, where it is *correct*.

⚠ **At the shipped 32 Tiles and 5 Lots the strip is 6 deep, so a detached block is 39% scrub** — 400
Tiles of 1,024. That is large for a suburb. ***It is a consequence of the shipped block size rather
than a number anybody chose***, so the test reports it and asserts only the shape: the leftover is the
interior square and nothing else.

The entry in `0012` is amended rather than struck — the sentences in `02 §2.2` and `adr/0078` are now
wrong in a **second** way, and neither document has been touched.

### ⚠ An exhaustive pattern is not exhaustive when a face carries no Address

**Found by the range sweep, and it is Q5's first hard evidence.** A Segment's Lots alternate sides by
parity, so at `lots_per_segment = 1` one parity takes the only Lot and **the opposite side of every
face gets none**. An exhaustive pattern then leaves that face's ground unclaimed and its claim is
false on that block.

🔴 ⚠ **CORRECTED IN STEP 4: IT IS NOT CONFINED TO ONE LOT A SEGMENT, AND THE SENTENCE ABOVE IS
UNDERSTATED.** The **corner reservation** reaches it too. At `block_tiles = 12` with 3 Lots a Segment
the reservation is 3 deep, the offsets are 2, 6 and 10, and **the east face's parity holds only 2 and
10 — both outside the reach**. So Perimeter drops that half-band and claims 108 Tiles where
back-to-back claims 144. ***The case is a property of the interaction between parity and the corner
filter, not of a degenerate key value.*** Step 4's ladder test found it by inverting.

***A pattern's exhaustiveness is conditional on a Ruleset it does not own.*** The test asserts the
failure rather than skipping it, so the day the limit is fixed the test says so by name.

⚠ **Everything else holds across the range**: `block_tiles` 4–64 against `lots_per_segment` 2–8, all
three patterns, no Tile claimed twice and nothing left over by a pattern that claims to tile.

### What is still notional

**A parcel's ground is computed and dropped.** Nothing reads it — the footprint that will is step 5 —
and it is computed at the carve anyway because the Address and the ground behind it have to come out
of one function or they can disagree.

---

## What step 4 shipped

**Shipped 2026-09-02.** Selection at the first carve, and a re-plat with a ratchet.

- **`BlockPatterns.ForBand`** — the band's position picks the pattern's position. Two ordinals against
  each other, and no number in between.
- **`BlockTable.Pattern` is one-based**: `0` is *nobody has decided*, not `Detached`.
- **`LotSubdivider.SubdivideBlock` selects on the first carve and never again.** A block that has been
  carved keeps what it was carved with, whatever its band says now.
- **`LotSubdivider.RecarveBlock`** — the re-plat, with two gates.

### ✅ Q3 answered: a ratchet, not hysteresis

**A pattern may only be replaced by one claiming strictly more of the block's ground.** That is
monotone, it is bounded above by the block's own area, and so it terminates: ***a block cannot re-plat
more times than it has ground to give.***

⚠ **Hysteresis was the obvious answer and it was refused.** A hysteresis band is a width, a width is a
number, and a hash-bearing number invented to damp a mechanism is exactly what `adr/0052` wants a
ratifier for — and nobody could name one.

**It is also what a real city does.** Re-platting is an intensification: a block is re-divided to get
more out of it. ***Nobody re-plats a block in order to use less of it*** — land that stops being wanted
is abandoned, not re-surveyed into bigger lots, and abandonment is a different mechanism with a
different name.

⚠ **The consequence is that two patterns claiming the same ground can never replace each other.**
Back-to-back and perimeter both tile their block, so a terrace never becomes a perimeter block and the
reverse never happens either. **That is the ratchet working rather than failing** — the two are
alternatives at one intensity, and choosing between them again later would be *choosing again* rather
than intensifying.

### The two gates, and what each answers

| Gate | Question | Source |
|---|---|---|
| **Vacancy** | *May it?* | `02 §2.2` — *only vacant land re-parcels*. One standing Building refuses the whole block |
| **The ratchet** | *Does it terminate?* | Above |

**And that is `adr/0025`'s redevelopment endgame end to end**: paint a denser band on a built block,
nothing happens, the block empties, and the re-plat lays the pattern the new band asks for. *"Upzoning
a built block does nothing until its Buildings go, which is how redevelopment becomes a real endgame
activity rather than a formality."*

### 🔴 The State Hash moved, and it is an ENCODING move

`Pattern` went one-based, so every carved block's byte went `0` → `1`. **The carve itself is
unchanged** — `GoldenSessionCoverageTests` asserts exact carved-Lot counts and passed untouched, and
`ForBand` returns `Detached` for every block of every bandless world. Three golden traces re-recorded;
`world-hash.txt` did not move.

### 🔴 Nothing calls the re-plat on the occasion that matters

It runs from `Resubdivide`, so **a road edit is the trigger**. The occasion that ought to trigger it is
**the block's last Building going**, which happens inside a Tick at `ZoneRuleEngine.Condemn` and has no
hook. ***So redevelopment is available and is not scheduled.*** Named here rather than left to be
discovered.

### 🔴 Q2 is now the gap, and it is the one that still matters

**Selection reads the band and stops.** So the city is **banded**: concentric rings of pattern, uniform
within a ring. That is an improvement on one pattern everywhere and ***it is not the varied city this
plan set out to build.***

⚠ **Every candidate rule for reading the continuous fields needs cut points nobody has derived.** Land
value and Building density both vary smoothly; turning either into a choice among three patterns takes
two thresholds, and a threshold invented here is a hash-bearing number with no ratifier.

**One derivation was explored and does not close yet.** A pattern's own *claimed-ground share* is a
natural cut point — pick the pattern whose share is the smallest one at or above what is already built
around the block — which needs **built ground**, and built ground is the footprint, which is step 5.
~~***So Q2 is not blocked on an argument; it is blocked on step 5.***~~

🔴 ✅ **UNBLOCKED 2026-09-02: step 5 shipped and built ground is now a real quantity.** A Cell's
Sealing is the ground actually spent — **81% of it Buildings on the shipped world** where it was 4% —
so the neighbourhood term the derivation wanted can now be read off `MapLayers`. ⚠ **What it is
still short of is the cut points**, and the derivation above avoids inventing them only if
*claimed-ground share* is genuinely the ordering, which is the argument nobody has had yet.
***So Q2 is no longer blocked on a mechanism; it is blocked on a sitting.***

---

## What step 5 shipped

✅ **A Lot carries a parcel, a Building covers it, and `footprint_tiles` is gone.** Four
`(derived AND rebuilt)` columns on `LotTable` — `parcel_east`, `parcel_north`, `parcel_wide`,
`parcel_deep` — written by the carve and rebuilt on the epoch by `World.RebuildParcels`, matched to
Lots by `(face, offset)`. `ParcelTests` is what holds them.

🔴 **The ground under a Building was invented in SIX places and is now decided in one.** Five were in
the shell — a setback, a stretch of kerb, a corner reserve, a depth and a re-centring — and the sixth
was `[[building]] footprint_tiles` in the core, which cited `adr/0078` as its reason for existing.
***A partition of a block cannot overlap; six inventions can***, which is `plans/0049` **F21** stated
as a property rather than as a bug. The shell's `Kerb`, `PastTheCorner`, `Deepest` and `Depth` are
deleted, and so is `PlotDepthMetres`, the 26 m the last three rested on.

### 🔴 The measurement moved, and it moved further than the plan expected

[`0052`](0052-the-parcel.md) **G4** measured Buildings at **4%** of all Sealing with a peak Cell at
**11.2%** — a city whose ground was a rounding error against its roads. `SealingMeasurementTests`,
re-pointed at the parcel and given a saturation counter:

| World | Buildings | Mean parcel | Buildings' share | Peak Cell | Saturated |
|---|---|---|---|---|---|
| `minimal.toml` — `block_tiles = 32` | 481 on **37,296** Tiles | **77** | **81%**, roads 18% | 733 = **71.5%** | **0** of 143 |
| `severance.toml` — `block_tiles = 256` | 481 on **2,480,844** Tiles | **5,157** | **97%**, roads 2% | 1,024 = **100%** | 🔴 **1,576** of 4,812 |

⚠ **The second row is the honest consequence and not a defect.** A quarter-kilometre block carves a
parcel five Cells' worth of ground, the whole of it is spent because a garden is developed, and a
Cell with nothing left has **Fertility 0** and stops telling two differently-built Cells apart. **The
lever that would fix it is a coverage fraction**, and it is parked in [`0052`](0052-the-parcel.md) as
a deferred derivation *because it would introduce a number*. ***The saturation is what the absence of
that number costs, stated rather than hidden.***

### 🔴 `MapLayers.Seal` could not express a parcel, and the measurement is what found it

`Seal(cell, cell, count)` takes **one Cell** and a count, which is exactly right for a footprint of
one to four Tiles and wrong for a rectangle bigger than a Cell. The first run put a whole 5,157-Tile
parcel into the Lot's own Cell: **481 saturated Cells, Buildings at 430% of all Sealing, and roads
reading NEGATIVE.** `SealGround` spreads by exact per-Cell overlap, so the parts sum to the whole and
`CONTEXT.md` → Sealing reads literally.

⚠ **Nothing would have caught this but the instrument.** Every assertion in the suite passed both
before and after; a share over 100% is not a compile error and not an invariant. ***A quantity nobody
prints is a quantity nobody checks.***

### ✅ The picture did NOT flatten, and [`0052`](0052-the-parcel.md) **G3** predicted it would

That plan's own warning — *stage 1 deletes a jitter and installs a constant*, **"the frame will get
flatter"** — did not happen, and the reason is worth keeping. **The jitter did not have to be
deleted; it had to move down a level.** The shell used to invent *a depth in metres*; it now draws a
**share of a depth the city holds**. So `DepthFillLow`–`DepthFillHigh` still varies one building
against its neighbour, and the parcel underneath varies one *block* against another — which the old
constant could not do at all, because 26 m was 26 m on a 32-Tile block and on a 256-Tile one.
***Two independent sources of variation where there was one.***

### ⚠ What the loader gained by losing a key

`[[building]] footprint_tiles` is **retired** rather than deleted: `RefuseRetired` names it, says why,
and tells an author what to do instead — *"make the ground bigger by drawing bigger blocks."*
`adr/0048`'s refusal count went **223 → 222** while the loader got stricter, for the reason
[`plans/0050`](0050-the-ruleset-sweep.md) recorded the first time it happened: a retirement is one
call site where a refusal is one call site plus a sentence somewhere else.

### 🔴 What is still owed

⚠ **Nothing re-plats on the occasion that matters**, which step 4 already recorded and step 5 does
not discharge. `RecarveBlock` is called from `Relot` and from tests; **no Rule, no policy and no
player verb calls it**, so a block's pattern is chosen once at founding and never revisited in any
shipped world.

⚠ **`InAYard` is a floor rather than a guarantee.** It clears the **detached** strip, because a margin
honest about a back-to-back block — whose parcels meet at the centre line and leave no yard at all —
would suppress the courtyard on every block in the city. What covers the denser patterns is the
Woodland itself: `Seal` takes a Cell's trees back as its ground is built on, and a terraced block now
thins itself out.

🔴 **The shell is compile-checked and not run.** `dotnet build src/Borough.Godot` succeeds; there is no
GPU here, so ***nobody has looked at the city this changed***, and under the amnesty's own amended
Definition of done that is exactly the clause that is not satisfied.

---

## What the coarse patterns shipped — *2026-09-02*

🔴 **A pattern now declares HOW MANY parcels a face is cut into, and until it could the set could not
express a block holding two large buildings.** `Carve` walked `index < lots_per_segment` on every face
of every pattern, so a pattern could vary *which faces carry* and *how deep they reach* and never *how
many* — ***at the shipped 32 and 5 every block in every pattern carried the same eight parcels.***
`BlockPatterns.ParcelsPerFace` is the ceiling; a face with fewer Addresses than it asks for gets one
each, so the coarsening can only join and never split. **The Addresses do not move — fewer of them are
kept**, so `adr/0074`'s side-of-street and the door on the drawing are untouched, and what a coarse
pattern does is give one Address the ground several would have divided. That is `adr/0025`'s
**Amalgamation** arriving as geometry rather than as a verb.

**Two patterns use it.** `Courtyard` — four buildings round a hole, one parcel a face, a third of the
block deep — and `Slab`, which is `BackToBack`'s faces and depths with one parcel each. ⚠ **The
coarsening is a property of its own and not a fifth geometry**, which is why `Slab` adds no case to
`DepthTiles`.

### 🔴 The corner reservation was coupled to the strip and nothing said so

**It read `StripTiles` while every face read `DepthTiles`**, and those were the same number for all
three original patterns — Detached and Perimeter both take a strip on the east–west pair — so the
coupling was invisible. `Courtyard` reaches a **third** of the block, and a reservation still set to
the strip let the north and south parcels run under the east and west ones: ***two rectangles
overlapping by 16 Tiles a block.*** The reservation is now the south face's own `DepthTiles`.
**`No_two_parcels_in_the_city_overlap` is what would have caught it** and the reason it was written.

### 🔴 The ladder is a function of the two Ruleset keys, and Q3's answer is corrected

⚠ **THIS DOCUMENT SAID Q3 WAS ANSWERED BY *a ratchet on ground claimed*, AND GROUND CLAIMED WAS A
PROXY THAT BROKE THE DAY THE SET GREW.** `Courtyard` claims **880** Tiles of a 32-Tile block against
`BackToBack`'s **1,024**, because its middle third is a courtyard — so the denser form claimed less and
the ratchet would have refused every intensification into it. ***The ratchet always meant the ladder***,
and it reads `BlockPatterns.Rung` now.

🔴 **AND THE LADDER IS NOT THE ENUM ORDER, WHICH THIS PLAN ALSO ASSERTED.** The quantity is **ground per
Address, ascending** — how much land stands behind one door — and at the shipped lattice that is
Detached 78, Perimeter 128, BackToBack 204, Courtyard 220, Slab 512. ⚠ **21 of the 73 reachable
lattices invert it**, and they are not a fringe: ***every one of them is `lots_per_segment = 4***`,
where a terrace and a courtyard block carry four Addresses each, so the comparison collapses back onto
area and the courtyard sorts below the terrace it is denser than. **A property asserted to be
lattice-independent turned out to be a function of the lattice.** `BlockPatterns.Ladder` computes it
for the lattice in hand; both keys are world-creation, so within a world it is still a constant and the
ratchet still compares two positions on one ladder. ⚠ **This is Q5's second piece of hard evidence** and
it is stronger than the first — `lots_per_segment` does not merely degrade a pattern at 1, it
**reorders the set** at 4.

### 🔴 Both ends of the ladder were unreachable, and no test could have said so

`ForBand` scaled `(band-1) × Count / bandCount`, which lands the top band **one rung short of the top**.
At three patterns nobody noticed; at five it means ***the coarsest pattern in the set could not be
selected by any Ruleset that could be written.*** It divides by `bandCount - 1` now, so the last band is
the last rung by construction. ⚠ **A single declared band takes the TOP**, because band 0 already holds
the bottom and a lone declaration landing on rung 0 would be inert.

### `rulesets/platted.toml`, because `banded.toml` cannot walk the ladder

**`banded.toml` declares two bands and its header argues why** — there are exactly two zone bits, so a
third band there would repeat a set and demonstrate nothing *about admission*. Under the fixed scaling
its two bands are the two **ends**, Detached and Slab with nothing between. **`platted.toml` repeats the
`admits` set on purpose**: five bands, every one admitting both bits, so admission is held constant and
the pattern is the only thing that varies between one ring and the next. ***Two files, two variables,
one held still in each.***

✅ **And it was looked at.** Five concentric rings at 40,000 Citizens, and the courtyard blocks read as
courtyards from the air — four buildings round a green hole — while the core is long slabs. ⚠ **What is
still uniform is HEIGHT**: every building is about four storeys whatever its footprint, which is what
capacity-from-ground and massing are for.

---

## What the footprint shipped — *2026-09-02*

🔴 **THE SIMULATION AND THE DRAWING DISAGREED ABOUT ONE RECTANGLE BY ABOUT A FACTOR OF TWO, AND THE
DRAWING WAS RIGHT.** `World.CreateBuilding` sealed the **whole parcel** — garden included — reading
`adr/0022`'s *Land is a stock the city spends* as *you cannot farm somebody's back garden*. Meanwhile
`Borough.Godot` drew a wall on **55–100%** of the frontage and **45–85%** of the depth, so half the
sealed ground had visible grass on it. ***A Building had two footprints and neither knew about the
other.*** `CONTEXT.md` → Building settles it: a Building *"interacts with Map Layers through that
footprint"*, and the footprint is the walls.

**`LotTable.Footprint*` is the one rectangle now** — four derived columns beside the parcel, produced
by the same call, rebuilt by the same pass. Sealing takes it and the shell draws it, so they cannot
part company. ⚠ **The old comment predicted its own repair** — *"a coverage fraction can arrive later
as a multiplier defaulting to 1 without this having been wrong"* — and it is **not a multiplier**.

### 🔴 One Ruleset key replaced four drawing constants, and the shape is derived from it

`[lots] setback_tiles`: **the most ground a Building leaves on each side of its parcel**. ⚠ **A LENGTH
AND NOT A FRACTION, and that is the whole of its shape.** Coverage then rises with the parcel without
anybody stating that it should — at the shipped lattice a detached plot keeps about **44%** of itself,
a terrace's **58%**, a slab's **77%**. ***A fraction would have made every density cover the same share
of its ground***, which is the failure `DepthFillLow`'s own remark described one project over. **The
four sides are drawn independently per patch of ground**, so a street varies and some walls stand on
the pavement, which is what a terrace is. ⚠ **The draw is on the PARCEL'S CORNER and not the Lot's id**
— a footprint is a property of the patch, so a re-laid Lot puts its Building back where the last one
stood.

⚠ **A `plans/0002` §D1 ROW IS OWED AND IS NOT THERE, and the reason is not that nobody thought of
it.** [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
is **suspended** by [`plans/0045`](0045-amnesty.md), so a ratifier is not owed on the day; and
`plans/0002` is **at its amnesty word ceiling**, so the row was written, cut twice and then dropped —
`CorpusBudgetTests` refused all three lengths. ⚠ **The obvious ratifier is unavailable anyway**:
Sealing feeds Fertility and Woodland and nothing yet reads either for anything a player sees, so ***the
number that would refute this has no consumer***. **It goes in §D1 the day the amnesty lifts**, and this
paragraph is what will still be here to say so.

⚠ **Required of every Ruleset that states `[lots]`, and a stated zero is a real city** — every Building
covers its whole parcel, which is what the simulation did before the key existed. **Absence is refused**
so that a file has to say which of the two it wants.

🔴 **`adr/0015` arrived by a route it does not name.** A number a designer would want to change is
Ruleset data; these four had escaped that by being called *drawing constants*. ***A number the shell
invents about the city is a number the city cannot see*** — it moves no State Hash, no Ruleset retunes
it, and no test can hold it to anything.

### What it cost, and what it bought

**Four hash literals in three files, three trace re-records and 33 Ruleset edits.** The State Hash moves
because Sealing moves, which is a design change under `05 §4` and is the point rather than a side
effect. `adr/0048`'s count of record goes **222 → 223**.

✅ **AND THE TREES CAME BACK**, which is what `plans/0052`'s own *What is still owed* was about. `adr/0159`
caps Woodland at `1024 − Sealing`, so sealing the whole parcel had stripped every built block; one Cell
in the core reads **sealing 680** where it read **944**. Looked at on `platted.toml` at 40,000 Citizens:
gardens between the houses, and a courtyard block with trees standing in the courtyard.

---

## What capacity shipped — *2026-09-02*

🔴 **A BUILDING'S OCCUPANCY STOPPED BEING A NUMBER SOMEBODY WROTE DOWN AND BECAME A PROPERTY OF THE
GROUND IT STANDS ON.** `[[building]] occupants`, `jobs` and `parking` are retired. What a Building
holds is its **floor area** — footprint × storeys — over a rate in the new `[capacity]` table:
`floor_tiles_per_occupant`, `floor_tiles_per_job`, `floor_tiles_per_parking_space`. What a *kind*
⚠ **`tenanted` BECAME TWO KEYS on 2026-09-02** — `houses` and `premises`, because one boolean could
not say *workplace and not a home* ([`plans/0054`](0054-the-kind.md) F1). Everything below is
unchanged in substance: what a kind
declares now is **whether it does the thing at all**: `[[building]] tenanted` and `parked`, two truth
keys, a shape this loader had never had. ***Capacity is geometry; behaviour is content.***

**The evidence was in `rulesets/` the whole time.** Thirty-nine `occupants` declarations across the
shipped files carried **two** distinct kinds and **three** distinct values; thirty-two `jobs`
declarations carried two. ***A number repeated thirty times is not thirty decisions*** — and a game
whose cities are meant to run to a million people cannot ask an author to write one down for every
combination of form and use. That was the argument the user made when this step was authorised, and
it is the one the files turned out to have been making already.

### 🔴 Every rate is a division and not a choice

**The anchor is the city that already stood.** A Building on a Detached parcel at the shipped lattice
— `block_tiles = 32`, `lots_per_segment = 5` — has a 78-Tile parcel, a **51-Tile footprint** and two
storeys, so **102 Tiles of floor**. That Building is the one 28 of the 34 shipped kinds described, and
what they said about it was 4 occupants, 8 jobs, 8 parking spaces. So `102 / 4 = 25`, `25 / 8 = 3`
(one tenancy's share, because a Business occupies one tenancy — `adr/0141`), `102 / 8 = 12`.

⚠ **So the suburb is the city it was and everything denser is new.** The same three rates give a
perimeter block **11**, a back-to-back **24**, a courtyard **35** and a slab **107**, and nobody wrote
one of those down. Measured on the standing generator: `minimal.toml` at 2,000 Citizens produces
Buildings holding **1 to 10** (`1×26 2×54 3×51 4×31 5×25 6×27 7×13 8×6 9×8 10×4`), and
`platted.toml` at 10,000 produces **thirty-four distinct occupancies from 1 to 35**. ***A number
nobody authored, varying because the ground varies, is what this step was for.***

### 🔴 The daylight bound, which a superblock demanded

`BuildingPlan.DaylightTiles = 4`, and it is a **design constant like the Cell rather than Ruleset
data**. Without it a pattern with a large parcel produces one enormous Building: `severance.toml` at
`block_tiles = 256` gave a Detached parcel of **48×48**, whose floor divided by 25 held about **150
Households**, so 400 people lived in **two Buildings** and the file stopped severing anything.

**The bound is daylight and the derivation is a room's.** A habitable room reaches about 7 m from a
window, so a plan two rooms deep is about 16 m — **4 Tiles**. `HabitableTiles` therefore hollows out
any rectangle deeper than that on both axes and counts the ring. ⚠ **It changes the shipped lattice
only for slabs**: 16×16 keeps 192 of 256, and every smaller footprint is entirely within reach of a
wall already. ***A capacity derived from area needs a bound that area does not carry.***

### 🔴 Three places had silently written the old fixed 4, and only varying it found them

`rulesets/maintained.toml` abandoned **31 of 263 Buildings** the first time occupancy varied. The
cause was three separate copies of the retired constant: a consumer Rule scaled its draw by
`derived = "occupancy"`, a producer Rule's `max` was 4, and the Bin between them had `capacity = 4`.
⚠ ***A consumer whose draw derives from the ground, feeding out of a store that does not, is
unsatisfiable by construction*** — and the Bin was the copy nobody thought of as a copy. All three
reverted to fixed applies, and the file's header now says the diff against `declining.toml` is
**exactly one rule** rather than describing a mechanism it no longer has.

### 🔴 An estimate of a floored quantity left 23 of 360 Households homeless

`SyntheticCity.WantedBuildings` estimated `households / typical` and subdivided that many blocks.
`CapacityRuleset.Holds` **floors**, so the estimate was systematically short and the difference queued
in the Unplaced Pool for ever — `adr/0006`'s shape produced by arithmetic rather than by a missing
sink, which is the second time this generator has done exactly that. ***The repair is to stop
estimating***: `Subdivide` now counts the room it has actually carved, Lot by Lot, and stops when it
has enough. `WantedBuildings` and `TypicalOccupancy` are deleted.

⚠ **Vacancy is now a DERIVED property of the generator rather than an accident of the estimate.** The
pass finishes the block it was crossing when it reached the population and then lays **one more whole
block**. A city built to exactly its population accepts no arrival and founds no Business, so the
slack has to be there — and it has to be a *rule* rather than a margin somebody picked.

### 🔴 A derived ceiling of 1 met `adr/0147`, and seven Households were evicted on arrival

`adr/0147` has **one ceiling count both kinds of tenant**, so a Building holding a Business has one
fewer tenancy for Households. On small ground the derived ceiling is **1**, and `Holds` floors at 1
for any positive floor — so the Building took a Household beside its Business and `EvictOverflow`
removed it the same Tick. **Two repairs, and both are about a distinction that did not exist while
the number was authored**: `Room`/`RoomOn` now tell *undeclared* from *a real zero* rather than
flooring both to 1, and `World.CreateBuilding` only gives a kind its own trade where the ceiling is
**above 1**. ⚠ **The mechanism that reported this was written for an occasion nobody had ever
staged** — a designer lowering a Ruleset number — and `adr/0068` carries a banner saying so.

### 🔴 The floor areas are about 4× a real Building's, and the parcels are right

⚠ **`[capacity] floor_tiles_per_occupant = 25` is not what a dwelling measures.** A Tile is 16 m², so
a real 96 m² dwelling is **6 Tiles** and this is four of them. The rate is absorbing a defect one
layer down: `[lots] setback_tiles` is a **LENGTH**, so an 8 m margin is negligible against a 48 m-deep
parcel and a Detached Building covers **65%** of its plot where a real one covers a fifth to a third.

***Anchoring on the standing city is what keeps the occupancies right while the footprint is wrong***
— the two errors are reciprocal and their product is the city that already worked. ⚠ **So the two
numbers must move together or not at all.** A `floor_tiles_per_occupant` of about 3 would be the real
figure, and adopting it against today's footprints would put about eight times the population in the
same Buildings. **This is filed rather than worked around**, per
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md),
and `rulesets/minimal.toml`'s own header states it in the file where the rate is authored.

### 🔴 `Borough.Godot` DID NOT COMPILE, and 2,679 green tests said nothing

The step deleted `BuildingKindDefinition.Occupants`. **Three sites in `Main.cs` read it** — the
Building height, the massing's occupancy tint and the click readout — so the shell was a build error
for the length of the step. ⚠ **`Borough.Godot` is not in the solution**, so `dotnet build`, the
assertion lane and the full suite were all green over a shell that could not start. ***A project
outside the build is a project outside every gate.*** The `drive` skill's **F26** already names the
neighbouring failure — a stale binary starting anyway and drawing an old city — and this is the same
hole reached from the other side.

✅ **The repair improves the drawing rather than restoring it.** The height now reads
**`LotTable.Storeys`** — the block pattern's own number — instead of inferring a shape from a
Ruleset's occupancy count. So the picture's height and the Building's capacity are the *same two
multiplicands*, footprint and storeys, rather than two guesses that happened to agree.

### ⚠ Two markets stopped differing, and the fixture moved rather than the assertion

`MarketDumpTests` asserts the **difference** between `provisioned.toml` and `oversupplied.toml`: one
under a Day's cover and flat at the ceiling, the other glutted and falling. At 2,000 Citizens the
glutted world came under cover too — **652 of stock against a draw of 699** — and printed the scarce
world's column. Swept at the shipped horizon, the band is **3,000 upward**; the fixture sits at
**4,000**, where the cover ratios are 1.75× and 0.16×.

🔴 **Time separates these two worlds as well, and that is why the horizon was not the lever.** Run
either at 98,304 Ticks and it gluts — `provisioned.toml` reaches **100 → 21**, because stock
accumulates while the draw decays. ***A contrast that only holds at one moment is not a contrast
between the files.***

⚠ **And a District with NO DRAW AT ALL is a third state the dump was reading as the first.** Cover is
stock over a daily draw, so `Held > Rate` calls a row glutted the instant it holds anything and
nobody buys. `provisioned.toml` grew exactly such a row — 2 sellers, 188 sundries, a rate of nothing —
and `--market` printed a **defect report about a defect that had been fixed**. The dump now counts
and names it separately. ***A degenerate denominator is named, never quietly asserted against.***

### ⚠ The wage-period spread is a FINDING and it is not explained

`WageTests` compares what a city pays over a fixed run at pay periods of 14, 7 and 1 Day. The reading
was **2,701,864 / 2,809,328 / 2,950,152** — a 9% spread the remark attributed to a fixed-length run
catching a different amount of the last part-period. It now reads **6,946,816 / 8,007,680 /
9,352,912**: **35%**, same direction. ⚠ **The run was moved to 56 Days, a whole number of every period
under test, so the part-period is eliminated by construction and the spread survives it.** The old
explanation was never checked and is wrong.

**Two candidates are named and neither is asserted** ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)):
a longer period presents a **larger lump** against the employer's balance, which `adr/0142` makes the
source of every wage — and this step made Businesses fewer and larger, which would amplify exactly
that; or entitlement is lost when a job ends between paydays, which the population's churn would also
amplify. The band is **widened to bound the order** rather than tightened around a number nobody has
explained.

### What it cost, and what it bought

**122 files: 34 Rulesets, 62 test files, the loader, the generator, the world, the shell and five
golden artefacts.** The State Hash moves because occupancy moves, which is a design change under
`05 §4` and is the point.

⚠ **All five golden artefacts were re-recorded, which is the widest re-baseline that directory
records** — every shipped Ruleset gained `[capacity]`, so all three baseline content hashes moved at
once. The Ruleset hashes had to be moved **before** the traces could be regenerated: a session log
records what it opens on and what it reloads into, and the runner refuses a replay whose Ruleset does
not match. ***The refusal is the feature; the ordering is what it costs.*** `world-hash.txt` moved on
its hash and on **none of its five counts**, which is this change's honest signature — the fixture
places its Buildings by hand, so what moved is what each row *means*.

⚠ **`adr/0048`'s count of record is UNCHANGED at 223, and three refusals went out while three came
in.** ***The first recount where the number is right and every reason behind it moved.*** Out:
`occupants`, `jobs`, `parking`, all three into `RefuseRetired`. In: a second `[capacity]` table, one
shared range check over the three rates, and one over the two truth keys.

🔴 **`adr/0068`'s title is half-falsified** — occupancy is no longer declared by a kind — **and its
eviction clause is stronger than when it was written.** The document carries a banner and
[`plans/0012`](0012-corpus-audit.md) carries the correction owed, which cannot be paid while
[`plans/0045`](0045-amnesty.md) forbids a new number and `PROCESS.md` puts the claim in the filename.

---

## What the massing shipped — *2026-09-02*

🔴 **A BUILDING WHOSE FLOOR THE CITY COUNTED AS A RING WAS DRAWN AS A SOLID BOX, AND THAT IS THE SAME
DEFECT FOR THE THIRD TIME.** `BuildingPlan.HabitableTiles` has hollowed a deep footprint since step 3
— the middle of a big plan is dark and is not floor — and `Borough.Godot` drew one box over the lot of
it. ***The parcel against the footprint at [`0052`](0052-the-parcel.md) stage 1, the height against
the occupancy at step 3, and now the plan against the mass.*** The repair is the same one all three
times: **one derivation with two readers**, `BuildingPlan.Hollow`, which the capacity subtracts and
the shell draws.

**A courtyard Building is four wings now** — south, north, and two flanks between them, so the corners
belong to the east–west wings and no Tile is drawn twice. Each wing's ridge runs along its own long
axis, which is not a draw: a ring's roof runs *round* the ring, and the solid case's minority
cross-gable would be a roof pitched over a 16 m span pretending to be a building.

### ✅ A hole under a wing's thickness is not a hole, and the shell is what asked

**The arithmetic alone hollows at 9 × 9 and its first holes are ONE TILE wide.** Measured on
`platted.toml`: the commonest hollow footprint was `14 × 9`, hole `6 × 1`. ***A four-metre gap between
two sixteen-metre wings is a light well and not a courtyard***, and a drawing that opened it would
read as a crack in the roof.

⚠ **The threshold is derived and no second constant arrives.** It is the wing's own thickness —
`DaylightTiles` — so the first footprint that hollows is `4 + 4 + 4 = 12` Tiles, **48 m**, on both
axes. *A gap narrower than the thing bounding it is a slot rather than a place.* ⚠ **And it had to
move the CAPACITY as well as the drawing**: a `12 × 9` footprint holds 108 Tiles of floor now and held
104. The alternative was two rectangles again.

🔴 **THE STATE HASH DID NOT MOVE AND THAT IS A FACT ABOUT THE BASELINE RATHER THAN ABOUT THE CHANGE.**
Every golden artefact passed untouched, because `declining.toml` and `congested.toml` carry no
footprint the threshold reaches — **`minimal.toml` at 2,000 Citizens has ZERO rings across 288 Lots**.
***So the goldens do not cover this at all***, and what does is `BuildingPlanTests`, which is the file
this step also had to write.

### 🔴 The constant shipped in step 3 with no test, and the prose was wrong about it

**`BuildingPlan` had no test of any kind.** It is the only thing between a parcel's area and how many
people live on it, and the wing thickness could have moved a Tile in either direction with the whole
suite green — because every assertion reads occupancy against whatever the plan happens to say.
***A derivation with two readers and no test is a convention.***

🔴 **AND THE ONE-TILE MOVE WAS AVAILABLE, because the remark and the arithmetic disagreed.** The type's
own sentence read *"no point in a Building may be further than `DaylightTiles` from an outside wall"*,
which describes a wing of **5**; the code has always cut **4**, which is the other reading of the same
justification — *16 m across, lit from both faces*. ⚠ **The two are a factor of two apart and both are
coherent**, so nothing but a reader's guess stood between them. ***The prose was wrong and the number
was right***, so this is a correction to a sentence and not to a city — and it is worth its paragraph
because the wrong reading is the plausible one, and adopting it would have raised every capacity in
the game.

### ⚠ It bounds the capacity and says nothing about whether the form is buildable

On `severance.toml`, whose block is 256 Tiles, `Hollow` returns a ring around a hole of **117 × 40** —
one Address holding a courtyard **468 m by 160 m**. ***The floor is right and the shape is absurd.***
The cause is the pattern handing one Address a quarter of a superblock, not the plan; **filed here
rather than worked around**, because the fix belongs to the pattern set.

### 🔴 An L-plan is UNDESIGNED rather than unbuilt, and the corner is why

**This step was scoped as *L-plans and courtyards* and ships the courtyards alone**, so the missing
half gets a name and a reason rather than a shape drawn because it would look right
([`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) — only *refused* is
evidence, and this is *undesigned*).

**What would produce an L is a Building fronting two Streets at a block's corner, and a corner parcel
does not hold the ground round the corner.** The cross face's parcel does, because ***a pattern is a
partition***. Drawing an arm there would put a wall on a neighbour's plot, which is precisely the
two-footprints defect step 2 spent itself closing. `BlockPattern` already refused the mitre in the same
words — ***at a real corner one street wins*** — so an L needs a partition that hands a corner to one
Address, which is a change to the **pattern set** and not to `BuildingPlan`.

⚠ **The other candidate was a rear return** — a tall front block with a lower wing behind it, which is
what a deep terrace actually is — and it was rejected for having **no derivation at all**. Nothing in
the city says where the step is or how much of the depth it takes, so every number in it would be the
shell inventing a fact about a Building. ***That is the class of thing `plans/0052` stage 1 deleted
five of.***

### What it cost, and what it bought

**Two files of code and one of tests.** `BuildingPlan` gains `Hollow`; `Borough.Godot` gains `Wings`
and loses nothing.

⚠ **`Massings` now returns a count it did not before.** A courtyard writes **four** bodies, so the
readout's *Buildings* figure would have counted wings — it counts changes of Id instead, which is
exact because a ring's wings are emitted consecutively. ***The layer's instance count and the city's
Building count stopped being the same number***, and the readout says *Buildings*.

⚠ **A ring gets NO outbuilding, and the reason is geometric rather than a taste.** The shed stands in
the back garden and a ring has no back garden — its open ground is *inside* it. Measured on the shot:
the yard layer falls **406 → 380** while the body layer rises **542 → 662** and the roof **376 → 442**.

✅ **AND IT WAS LOOKED AT.** `platted.toml` at 10,000 Citizens, Tick 1,026: **40 of 542 standing
Buildings are rings**, and from the air the dense rings read as Berlin Blockrand — a continuous
four-storey street wall with a green or paved court inside it — while the ordinary city stays solid
buildings with gardens between them. ⚠ **What surprised me is that the courts are not all green**:
Sealing takes the whole footprint, so a court inside a Building carries no Woodland and draws as
paving, and the two states sit side by side in one frame. ***That is the Sealing rule made visible for
the first time***, and nothing was changed to produce it.

---

## What this does to [`0052`](0052-the-parcel.md)

✅ **Nothing in it is withdrawn.** G1–G8 stand, the Sealing measurements stand, Q3's answer stands, and
its stage 1 is this document's step 5.

⚠ **What changes is its scope claim.** It reads as though the parcel is the first move; it is the last
of five. **It gets a banner pointing here** and keeps everything else.

⚠ **And two of its open questions are answered by this document rather than by it.** **Q1** — *what is
a parcel's depth derived from* — was mis-posed: **there is no single depth**, because there is no
single pattern. **Q2** — opposing-face arbitration — is a property of each pattern rather than a
question with one answer.

---

## 🔴 The `parking` retirement left two instruments unloadable, and nothing ran them for a day

**FOUND 2026-09-03 by [`0055`](0055-the-middle.md)'s whole-suite run.** `bd81389` retired
`[[building]] parking` into `RefuseRetired` — occupancy divides the ground now, so how many Vehicles
a Building parks is derived from floor area over `[capacity] floor_tiles_per_parking_space`. **Two
test classes build their own TOML by hand and still state the key**, so both are refused at load:

| Class | Where |
|---|---|
| `ParkingScarcityTests.The_walk_and_the_exhaustion_as_occupancy_climbs` | `Ruleset(int rung)` — the sweep's whole point is cutting `parking` rung by rung |
| `ParkingArrivalStreamTests.The_arrival_peak_is_land_use_as_much_as_it_is_the_city` | `WithASecondEmployingKind()` |

⚠ **Both are INSTRUMENT tier, which is why a day passed.** `scripts/test.sh` filters them out and the
post-submit lane is the only thing that runs them — so ***a retirement that breaks an instrument is
invisible to the lane the retirement was made in.*** That is [`0032`](0032-test-tiers.md)'s axis doing
exactly what it says it does and costing exactly what it says it costs.

🔴 **The scarcity one is not a rename.** Its sweep is *cut `parking` rung by rung and watch the walk
lengthen*, and there is no key left to cut — the number is derived from the ground. **So the
instrument needs re-aiming at `floor_tiles_per_parking_space` and that is a different sweep**, not a
find-and-replace. It is filed here rather than repaired because it is this plan's step that retired
the key.

## Open questions

| # | Question | Type |
|---|---|---|
| **Q1** | ✅ **ANSWERED, 2026-09-02, and the answer was already in the code under another name.** See *What step 3 shipped* | *arguable* |
| **Q2** | 🔴 **What does a pattern read at carve time, and in what order?** Band and zone are certain; land value and Building density are available and would supply the continuous variation. **More inputs is not better** — each one is a coupling. ⚠ **Step 4 shipped with the BAND ALONE**, and this is now the gap between a banded city and a varied one | *arguable*, and it is the one that still matters |
| **Q3** | ✅ **ANSWERED, 2026-09-02: a ratchet.** ⚠ **The answer was AMENDED the same day** — it ratcheted on *ground claimed*, which a pattern with a hole in it inverts. It reads the ladder. See *What the coarse patterns shipped* | *arguable* |
| **Q4** | **May land value condemn a healthy Building?** *Undesigned* under `adr/0070`. It is the difference between this and SC4's redevelopment, and it is not a constraint on this plan | *arguable* |
| **Q5** | 🔴 **Does `lots_per_segment` survive as a world number?** [`0052`](0052-the-parcel.md) Q4 asked this of stage 2; **it is really a per-pattern question** and belongs here. ⚠ **Two pieces of evidence and the second is much stronger**: at `1` an exhaustive pattern is not exhaustive, and at **`4` the intensity ladder REORDERS** — see *What the coarse patterns shipped* | *arguable*, with evidence |
| **Q6** | ✅ **ANSWERED, 2026-09-02, and the answer is an existing idiom.** See below | *measurable* |

### Q6 — what a `BlockTable` costs, and why it is not a new pattern

🔴 ⚠ **THE FIRST ANSWER WRITTEN HERE WAS WRONG AND IT IS `plans/0012` CAUSE 5, COMMITTED IN THIS
DOCUMENT.** It read *"the lattice is 512²"* as though that were a property of the map. It is not.
**The lattice is `(WorldTiles / block_tiles + 1)²`**, and `block_tiles` is **tuning Ruleset data whose
loader floor is 1** (`RulesetLoader.cs:5197`). The `lots_per_segment ≤ block_tiles` refusal constrains
`lots_per_segment` **from above**, not `block_tiles` from below — so a 1-Tile block is a legal world.

| `block_tiles` | Span | Lattice | Dense `int[]` index |
|---|---|---|---|
| **32** — shipped | 513 | 263,169 | **1.05 MB** |
| 8 | 2,049 | 4.2 M | 16.8 MB |
| 5 | 3,278 | 10.7 M | 43 MB |
| **1** — the loader's floor | 16,385 | **268 M** | 🔴 **1.07 GB** |

🔴 ***A `BlockResidency` is unlike all five existing residencies, and this is the one thing about step 1
that is genuinely new.*** `CellResidency`, `BuildingResidency`, `DistrictResidency`, `FloodResidency`
and `WaterResidency` every one index **`CellGrid`, a design constant that never moves**. This one
indexes a lattice **sized by a tuning key**. ***The house pattern does not transfer unexamined, and
the sentence below that said it did was the error.***

- ✅ **Size the index lazily from `StreetGrid.Span` at world creation.** Correct at every block size,
  and `Span` is already known there. **The exposure is then named rather than allocated silently.**
- ⚠ **`block_tiles`' floor of 1 is separately suspect** and is not this plan's to fix: a 1-Tile block
  is a world where **every Tile is a Street**, which is not a city. Owed to
  [`0012`](0012-corpus-audit.md) as its own filing rather than folded in here.

**The costs below stand at the shipped block size and nowhere else.**

| Shape | Cost | Standing |
|---|---|---|
| **Dense table** — a row per lattice block | **~5.2 MB** at `block_tiles = 32`, allocated whether the city reaches them or not (16 bytes of `Rows` identity per row before any column) | Wasteful, and it sizes the world by the **map** rather than by the **city** |
| ✅ **Sparse — a lazily-sized `BlockResidency` plus a table** | **~1 MB** for the index at the shipped block size, **plus ~20 bytes per ZONED block**. A 10,000-block city is **~1.2 MB** | **Taken**, with the index sized from `Span` rather than from a constant |

⚠ **`adr/0006` is not reached either way and saying it was would be wrong.** That ADR forbids a
collection that **grows with elapsed time**; a lattice-sized array is fixed at world creation and grows
with nothing. The sparse form is taken because it is **4× cheaper at the shipped block size**, not
because the dense one was a violation.

🔴 ⚠ **AND DO NOT COLLAPSE A BLOCK ONTO A CELL.** At shipped figures `block_tiles = 32` and
`CellGrid.TilesPerCell = 32`, so a block **is** a Cell and `CellGrid.Index` would address it exactly.
***That coincidence is not a guarantee***: `block_tiles` is tuning Ruleset data and the Cell is a
design constant that never moves. A `BlockResidency` indexes the **lattice**, and it must compute its
own index from `StreetGrid.Span` rather than borrowing `CellGrid`'s.
