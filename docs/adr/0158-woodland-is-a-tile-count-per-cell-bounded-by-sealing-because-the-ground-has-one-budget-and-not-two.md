# Woodland is a Tile count per Cell bounded by Sealing, because the ground has one budget and not two

> **⚠ Corrected 2026-08-24, hours after it was written, and about the table rather than the quantity.**
> The first draft put the column on **`LayerCellTable`**, reasoning that Woodland joins the category
> Sealing already occupies. ***That is right about the category and wrong about the storage, and what it
> missed is a sentence `plans/0042` **F7** had already written***: *"The Cell table's sparsity was
> load-bearing and stated nowhere."* `SetLandValueTargets` is `O(live Cell rows)` at four desirability
> samples each. **Woodland is the one quantity that exists where the city is not**, so placing it there
> creates a row for every wooded Cell at world creation and makes the table dense on **every** world,
> including the eight where it is near-empty. **Woodland gets a dense table of its own instead.**
>
> ⚠ **The cost is already measured, and by task 2 rather than by this decision.** `TerrainCellTable`'s
> own doc-comment records that ensuring whole-map Cell residency on `minimal.toml` took the land value
> target pass from about **2.5 ms** to about **114 ms** — and states in the same breath that the reading
> was taken on a machine that was not quiet, that **no document may quote it**
> ([`0106`](0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)),
> and that what survives a spoiled reading is the **ratio and the sign**. That is what is relied on here
> and nothing more. ⚠ **F7's 88 seconds is a different cost and is not quoted here**, because the comment
> carrying the right figure says so explicitly.
>
> The category argument below stands untouched; only the address changes.
>
> 🔴 **This is the second time in one milestone that the answer was a table of its own, and the first
> was task 2 deciding the same thing for the same reason** (**F8**). ***The argument did not need
> re-deriving and the measurement did not need re-taking; both were in the file next door.*** What the
> correction cost was the hour between writing the draft and reading `TerrainCellTable`'s header —
> [`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) paying out in
> the ordinary way: **the sentence about the mechanism told me which symbol to read.**

**Woodland is stored as a count of wooded Tiles per Cell** — `(saved AND hashed)`, on a dense
**`WoodlandCellTable`** of its own, one row per Cell allocated at construction exactly as
`TerrainCellTable` is, and conceptually what [`0022`](0022-land-is-a-stock-the-city-spends.md)'s
Sealing already is: **a count, not a field**, with no kernel and no diffusion. ~~on `LayerCellTable`
beside Sealing, and a member of the `Layer` enum~~ — see the correction above. **Its bound is
`TilesInCell − Sealing`**, which holds **across two tables** — one indirection for a reader, and
nothing at all for the land-value pass, which is not a rule anybody authors but the
arithmetic of what the two counts already mean. **Replanting does not ship with it** and is
*undesigned* under [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) rather than
unbuilt.

Guiding concepts: `EMERGENCE`, `SOLVE THE ACTUAL PROBLEM`, `NO VERDICT`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
Where a quantity lives is settled by argument; no measurement distinguishes a Tile flag from a Cell
count, because both can be made to produce the same city.

## Why

**The corpus named Woodland in eleven places and gave it a disposition in none.** `CONTEXT.md` has a
first-class entry for it, [`0022`](0022-land-is-a-stock-the-city-spends.md) decides its whole arc, and
[`0090`](0090-the-generator-makes-land-and-the-player-makes-every-road.md) closes the generator's remit
around it — and not one of them says whether it is a column, a flag, a Layer or a Building. ***This is
the fourth artefact in one milestone named in many documents and defined in none***, after the
milestone's own precondition 1, decision 7's terrain enumeration, and **F8**'s column that turned out to
live in neither of the two places on offer. The pattern is now frequent enough to be the expected case
rather than the surprise.

**It is not terrain, and the reason is a rule rather than a taste.**
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) makes terrain generated
once and never changed, and `TerrainCellTable` is built to that promise — every row allocated at
construction, no `Create`, no growth path. **Woodland is cleared and it regrows.** A mutable column on
that table would falsify the sentence the table exists to keep, and the analyser that caught the
fingerprint field on the day it was written would not catch this one, because nothing about a mutable
`int` looks wrong.

**Nor is it any of the three things it is adjacent to.** Forestry is the Building; Timber is the Good;
a Resource in this project's sense is *anything held in a Bin* (`CONTEXT.md`), and Woodland is held in
no Bin because it is ground. What is left is the category Sealing already occupies: **a per-Cell count
that changes on events, is never diffused, and has no cadence of its own**. `Layer` documents that
category in its own comment for Sealing — *"a count, not a field: it has no kernel and no cadence"* —
so Woodland joins an existing shape rather than opening a new one.

⚠ **The category and the address are different questions, and conflating them is what the correction
at the top had to undo.** Woodland belongs to Sealing's *category* and cannot share Sealing's *table*,
because the thing that makes `LayerCellTable` affordable is that it is sparse and the thing that makes
Woodland Woodland is that it covers the ground nobody has touched. So it takes `TerrainCellTable`'s
**shape** — dense, one row per Cell, allocated at construction, no `Create` and no growth path — while
taking Sealing's **semantics**. ***A table that is dense by construction cannot be made dense by a
writer***, which is the whole of why this costs the land-value pass nothing.

### The one budget, which is the part that is not bookkeeping

A Cell is 32×32 Tiles. **Sealing counts the Tiles in it ever built on. Woodland counts the Tiles in it
that are wooded. The two sets cannot overlap**, because `CONTEXT.md` → Zone already decided what
happens when they would: *building over forest clears it and forfeits the harvest, which is a cost and
never a refusal.* So

```
Woodland + Sealing ≤ TilesInCell
```

is not an invariant somebody imposed on the model. **It is the model.** Three things fall out of it
that would otherwise each have needed deciding:

- **Sealing clears forest with no verb and no event.** `0091` fixed the six verbs and none of them
  clears ground; `0022`'s 2026-08-12 amendment says outright that *no verb clears Woodland*. Under this
  bound, `Seal` reducing Woodland to fit **is** the clearing, and it happens at the one door every
  Building already comes through.
- **The frontier migrates without a system.** `0022:42` requires that a Forestry Building whose local
  Woodland is exhausted declines through the ordinary path rather than through a *move the logging camp*
  mechanism. A count that falls as Sealing rises supplies exactly that, and supplies it as arithmetic.
- **Woodland area becomes a readout of Sealing.** This is the consequence worth naming, because
  `plans/0042` decision 5 has been blocked on the want of one. Sealing's decay rate is a *measurable*
  number under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
  whose ratifier owes a **quantity** under
  [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md), and **F9**
  found that nothing in the build reads Sealing against terrain except `MapLayers.Fertility`, which
  nothing reads either. Woodland bounded by Sealing is a second reader, and unlike Fertility it
  accumulates over a run rather than being composed at the point of use — so *how fast does land
  recover* acquires a curve somebody can plot. ⚠ **This does not ratify anything on its own**, and the
  trap is worth stating: if 8b's regrowth rate is itself unratified, then two unratified numbers are
  measuring each other. What the bound supplies is a **readout**, not a ratifier.

### Why not a per-Tile flag, which is what `CONTEXT.md` literally says

`CONTEXT.md` → Woodland says *"Forest **Tiles** are not farmable while wooded"*, and read literally
that is per-Tile state. It is refused on two grounds and the second is the stronger. The map is
16,384² Tiles, so a bit per Tile is **268 million bits, 33 MB**, saved and hashed, for a quantity whose
every consumer — Fertility, a Forestry Building's conditions, Wildfire's fuel — reads it *per Cell*.
And **the design has no per-Tile ground state anywhere**: Sealing, Fertility, pollution, land value and
terrain type are all per Cell, and `02 §2.5`'s guard procedure exists precisely to stop the resolution
of a new quantity being chosen by reflex. ***A count of Tiles held per Cell is what that sentence
means; a flag per Tile is what it says.*** The entry is corrected rather than the design bent to it.

### Replanting is undesigned, and shipping a version of it would invent the mechanism

`0022:68` specifies replanting as *"a Rule and a land designation rather than a system"*. **There is no
land designation in the build or in the design.** `0091` closed the verb set at six with no designation
among them, `01-player-experience.md` does not mention replanting anywhere — not in §5.4's dial table,
not in the Policy chapter — and no Policy body, predicate or price exists for it.
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) classifies that as **undesigned**,
which is the class whose answer is *design it*, and explicitly not *refused*, which is the only class
that counts as evidence. Building a minimal version here would author the designation by accident, in a
task whose subject is ground — and a mechanism invented as a side effect of a storage decision is the
shape `01 §5.5` forbids by name.

## Consequences

- **A new `WoodlandCellTable` joins the world with one `Saved<int>("woodland")` column**, dense at
  `CellGrid.WorldCellCount` rows — about **1 MB**, saved and hashed, single-buffered. `LayerCellTable`
  is untouched and stays sparse. The State
  Hash moves on every world where the generator places forest, which is every world. Under
  [`0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) that is a
  cost of nothing while nobody is carrying a save, and it is **not** a reason to defer, narrow or split
  anything here.
- **`MapLayers.Seal` acquires a second effect and stops being a pure accumulator.** Sealing a Tile now
  displaces Woodland where the Cell is full. This is the ADR's whole point and it is still worth
  flagging: a reader of `Seal` who expects one column touched will be wrong.
- **The bound is checkable, so it becomes an invariant.** `Woodland + Sealing ≤ TilesInCell` is
  `O(1)` at the write site, which is where `02 §10` puts a check of that frequency.
- **`plans/0042` task 8 splits into 8a and 8b** along the line task 3 and task 4 are already split on:
  the write path ships without the rate, and the rate ships with its own `plans/0002` §D1 row. **8a
  authors no number**, exactly as `TerrainGenerator` authors none.
- **`0022:137`'s *"regrowth speed is the load-bearing constant"* acquires an owner it has never had.**
  It is 8b's, and 8b cannot land without a named machine, world and quantity under `0052`.
  ✅ **LANDED 2026-08-24.** `[layers] woodland_regrowth_days = 512` in `rulesets/varied.toml`, on a
  cadence of a Day at offset 80, with a `plans/0002` §D1 row and milestone 24's long run as its
  ratifier. ⚠ **It needed a column this ADR did not anticipate** — `WoodlandCellTable.Potential`,
  saved, written once by the generator — because regrowth needs a **ceiling**, and both ceilings that
  need no column are wrong: the bare Cell converges every map on uniform forest and erases the seed's
  character, and the terrain type disagrees with `WoodlandGenerator` on the first Tick because that
  generator reads its own noise field. ✅ **The pair is also the readout this ADR asked for** —
  `Potential − Tiles` summed over the map is *what the city has spent*, which is
  [`0022`](0022-land-is-a-stock-the-city-spends.md)'s own title made observable.
- ⚠ **And the trap named above is now live rather than hypothetical.** Sealing's decay landed
  unratified at task 4 and regrowth landed unratified here, so a long run reading Woodland against
  Sealing has **two unratified numbers measuring each other**. ***What the bound supplies is a readout
  and not a ratifier***, so the run reads each against its own stated duration.
- **Woodland has no consumer, and that is recorded rather than dodged.** The Timber chain is unplaced —
  `06` has no row for Forestry, and `02 §2.3` says outright that no milestone builds a farm. So 8a
  ships a producer nobody reads, which is **F9** arriving a second time three tasks later. It is taken
  anyway, because the alternative is that the ground half waits on a chain that waits on the ground.

## What would trigger revisiting

- **A consumer that genuinely needs Tile resolution.** Wildfire spreading Tile to Tile within a Cell
  would be the real case, and `01 §5.2` does not currently specify it that way — it spreads *via
  Woodland*, with no resolution stated. If Wildfire is scoped and needs Tiles, this is where the cost
  of the count was paid and where the flag comes back.
- **A second ground-pinned resource.** `CONTEXT.md` → Zone states its rule over *resources* rather than
  over Woodland *"so that any later ground-pinned resource inherits it without a second decision"*. A
  second one arriving would ask whether the single `Woodland` column should have been a family, and the
  honest answer today is that one member cannot tell.
- **Sealing ceasing to bound it.** If a Cell can ever be both built on and wooded — a Building kind
  that keeps its trees, a park — the one-budget argument fails and the two counts separate. Nothing in
  the design does that today; `[[building]] footprint_tiles` seals everything it covers.
- **Replanting being designed.** When a land designation exists, the *undesigned* classification here
  is spent and replanting becomes ordinary unbuilt work with a home.
