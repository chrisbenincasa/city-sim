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
***So Q2 is not blocked on an argument; it is blocked on step 5.***

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
| **Q1** | ✅ **ANSWERED, 2026-09-02, and the answer was already in the code under another name.** See *What step 3 shipped* | *arguable* |
| **Q2** | 🔴 **What does a pattern read at carve time, and in what order?** Band and zone are certain; land value and Building density are available and would supply the continuous variation. **More inputs is not better** — each one is a coupling. ⚠ **Step 4 shipped with the BAND ALONE**, and this is now the gap between a banded city and a varied one | *arguable*, and it is the one that still matters |
| **Q3** | ✅ **ANSWERED, 2026-09-02: a ratchet on ground claimed.** See *What step 4 shipped* | *arguable* |
| **Q4** | **May land value condemn a healthy Building?** *Undesigned* under `adr/0070`. It is the difference between this and SC4's redevelopment, and it is not a constraint on this plan | *arguable* |
| **Q5** | 🟡 **Does `lots_per_segment` survive as a world number?** [`0052`](0052-the-parcel.md) Q4 asked this of stage 2; **it is really a per-pattern question** and belongs here. ⚠ **Step 3 produced evidence**: at `lots_per_segment = 1` an exhaustive pattern is not exhaustive | *arguable*, with evidence |
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
