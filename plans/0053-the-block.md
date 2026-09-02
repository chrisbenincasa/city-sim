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
| **3** | **A pattern is a partition function**, and the set is open. Three to start | yes | (a), (b) and (c) all become expressible, and none is privileged |
| **4** | **Carve and re-carve.** Selection at carve time from local conditions; frozen while occupied; re-carved when vacant | yes | ***The city stops being uniform*** |
| **5** | **The parcel, then the footprint** — [`0052`](0052-the-parcel.md) in full | yes | Sealing, Fertility, Woodland, `adr/0025`'s Land axis, and F21 becoming impossible |

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

### Three things the corpus caught that the compiler did not

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

## Open questions

| # | Question | Type |
|---|---|---|
| **Q1** | **What are the perimeter pattern's two depths?** Exhaustive for any winner-depth, so the number is free and something must choose it. ⚠ **Equal parcel area is a candidate derivation and it is circular** — the Lot counts depend on the depths through the corner filter | *arguable* |
| **Q2** | **What does a pattern read at carve time, and in what order?** Band and zone are certain; land value and Building density are available and would supply the continuous variation. **More inputs is not better** — each one is a coupling | *arguable* |
| **Q3** | 🔴 **What stops carve/re-carve oscillating?** `adr/0006`. Hysteresis, or a one-directional trigger, or a bound on re-carves per block | *arguable*, and it gates step 4 |
| **Q4** | **May land value condemn a healthy Building?** *Undesigned* under `adr/0070`. It is the difference between this and SC4's redevelopment, and it is not a constraint on this plan | *arguable* |
| **Q5** | **Does `lots_per_segment` survive as a world number?** [`0052`](0052-the-parcel.md) Q4 asked this of stage 2; **it is really a per-pattern question** and belongs here | *arguable* |
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
