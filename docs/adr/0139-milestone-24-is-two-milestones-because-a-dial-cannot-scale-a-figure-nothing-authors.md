# Milestone 24 is two milestones, because a dial cannot scale a figure nothing authors

> ⚠ **Corrected the same day by [`0140`](0140-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md).**
> *"the baked terrain-suitability column"* below was this document quoting
> [`0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)'s
> artefact, which is now superseded: the column holds **terrain type**, and **Base Fertility** is Ruleset
> data keyed off it. **This ADR's own decision — the split, and its grounds — is untouched**, because the
> split turns on the Hinterland figures and not on what the terrain half stores.

**Milestone 24 splits.** Its **terrain half** — the generator, the baked terrain-suitability column, and
the five land rows [`0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)
moved onto it — has **no upstream at all** and is buildable now, scoped in
[`plans/0038`](../../plans/0038-terrain-and-the-land-rows.md). Its **Shocks-and-Dial half** — Shocks,
Disasters, the Intensity Dial, Modes and the lock policy — is **UNPLACED**, blocked on milestones **13**,
**15** and **16**, because [`0131`](0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)
authors there every Hinterland figure that a Shock moves and that the dial scales. `06`'s row 24 is
rewritten rather than quietly emptied.

Guiding concepts: `HONEST DEGRADATION`, `SOLVE THE ACTUAL PROBLEM`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
Where a milestone's rows sit is a sequencing question and names no number.

## Why

### The split was offered in advance, and the scoping is what took it

[`0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)'s
own *What would trigger revisiting* reads: *"**Milestone 24 is split.** It now carries terrain
generation, Shocks, the Intensity Dial and five land rows, and the option of a separate *land systems on
terrain* milestone after it was considered and not taken. **If 24 is scoped and found to be two
milestones, this is where the split was first available.**"*

It was scoped on 2026-08-22 and it is two milestones. **This ADR is that trigger firing**, and the
reason it fires is not the one that ADR anticipated — it expected the *land systems* to be the separable
part, and the separable part is the **dial**.

### The two halves have opposite dependency shapes, and the row could only show one of them

[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) gives terrain exactly one
producer, the seed generator, and **nothing produces an input to it**. No milestone between 12 and 24
supplies terrain with anything. So the terrain half has **zero upstream**.

The other half has three, and [`0131`](0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)
is what makes them visible, because it settles **which milestone authors each Hinterland field**:

| Hinterland field | Authored at |
|---|---|
| edge identity, and what its emigrants carry | **11** |
| price per Good | **13** |
| median wage | **15** |
| depth and recovery rate | **16** |
| median rent, service levels, the commute figure | **16** |

`01 §5.2` defines a **Shock** as *"a movement in a Hinterland's authored figures, and nothing else"*,
naming **prices, wage, rent, population, composition**. `01 §5.4` gives the **Intensity Dial** three
sub-dials: **Bill** scales a Hinterland's price level and the rate it lends at; **Clock** scales its
depth and recovery rate; **Acts of God** scales a frequency interval.

🔴 **The build carries none of those figures.** A `[[hinterland]]` block states three keys — `edge`,
`emigrant_balance_min`, `emigrant_balance_max`. What it does carry, *what an emigrant carries*, is the
one figure `01 §5.2`'s list does not name.

***A row that names two mechanisms records the position of whichever one was thought about when it was
written***, and 24's row was written about terrain.

### `01 §5.4`'s cost claim is true of the design and false of the build, and that is what makes a partial dial dangerous

`01 §5.4` closes with the property that makes the dial safe: *"**The list introduces no new parameters**
— every entry is a figure the Hinterland already carries … Nothing is authored twice and nothing can
drift out of sync with the model."* And it draws the conclusion the whole section rests on: *"'it scales
parameters, never disables systems' becomes **structural rather than aspirational**, because the dial
has no reachable surface other than config the simulation reads anyway."*

**That argument is sound and its premise is a hypothesis about the build.** The figures are *specified*,
in [`0023`](0023-immigration-arrives-through-the-gate.md) and
[`0026`](0026-wages-are-posted-locally-and-never-cleared.md), and **unbuilt** —
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) types that *unbuilt*, not *refused*, so
the answer is **build them**, at the milestones `0131` names.

⚠ **A dial built before them would have to author its own parameters**, and a dial with a parameter of
its own is *"a modifier on the city"* — which `01 §5.5` forbids **by name**, recording the bound *"so
nobody later relaxes it"*. ***The absence that blocks the dial is the same absence that guarantees it
stays honest***, so shipping it early does not merely ship less, it ships the thing the section was
written to prevent.

This is [`plans/0003`](../../plans/0003-build-plan.md)'s 5a-bis lesson at its next sighting:
***a design document's precondition is a hypothesis about the build.***

### Hazard Regions go with the generator and Disasters do not, and the seam is real rather than convenient

`CONTEXT.md` → Hazard Region is *"ground where a Disaster can occur, **derived from terrain at world
generation**"*. It is generator output, it is never read inside a Tick, and it is an overlay from the
first Tick. So it belongs to the half that has a generator.

What has no home is the thing that **fires** on it. `01 §5.2`'s catalogue makes that plain: **Urban
fire** is contained by *fire service reachability*, and a Service is milestone **15**; **Wildfire**
spreads via **Woodland**, which the terrain half builds; and a destroyed Building *"vacates a Lot, which
normal redevelopment reoccupies at Materials cost"*, which is the Goods chain.

***Deriving where something could happen is the terrain milestone's work; scheduling it belongs to the
milestone that has something to schedule.***

### The alternative was considered and refused

**Keeping 24 whole and building only the reachable part** was the obvious option. It is refused because
`06` rule 4's corollary is the stopping rule — *once the risk is retired, the milestone is done* — and a
single row whose risk is retired in two places, milestones apart, cannot be stopped. It would stand open
across 13, 15 and 16, and every session reading the board would find a live row it could not finish.
⚠ **And rule 2 is the sharper objection**: a milestone must leave the project runnable, and a 24 that
ships terrain and waits three milestones for its dial is the *"60% done, then sits for three weeks"*
shape rule 3 exists to prevent.

## Consequences

- **`06`'s milestone table gains no row and its row 24 is rewritten.** The terrain half keeps the number
  **24**; the Shocks-and-Dial half is **UNPLACED** and joins *Mechanisms with no milestone yet*.
  ⚠ **Nothing is renumbered** — [`PROCESS.md`](../../PROCESS.md) → *Numbering* freezes a shipped
  milestone's number and this splits an unshipped one in place.
- **The terrain half is startable immediately and beside milestone 12**, which is what
  [`plans/0038`](../../plans/0038-terrain-and-the-land-rows.md) records and why it was scoped out of
  sequence.
- **`01 §5.4`'s *"introduces no new parameters"* keeps a caveat**: it is a statement about the design,
  and the figures it names arrive at 13, 15 and 16. ***The claim is what makes the dial safe, so it must
  not be read as a claim that the dial is cheap.***
- **Three Hinterland-field milestones acquire a consumer they did not know they had.** 13's price per
  Good, 15's median wage and 16's depth and recovery are each now read by the dial as well as by the
  mechanism they were authored for.
- **This ADR opens no [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
  number**, and `plans/0038` records the ones the terrain half owes.

## What would trigger revisiting

- **A Hinterland field lands early.** If 13's price per Good ships ahead of its milestone for some other
  reason, the Bill sub-dial's blocker is partly gone and this split is worth re-reading — though the
  Clock's two figures at 16 would still hold it.
- **Somebody proposes a dial parameter of the dial's own.** That would make the Shocks-and-Dial half
  buildable now and is exactly what `01 §5.5` forbids; it needs that section reopened, not this ADR.
- **The terrain half is scoped and found to be two milestones in its turn.**
  [`0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)
  offered a *land systems on terrain* split that was considered and not taken; `plans/0038` carries ten
  tasks, and Water Bodies plus Woodland are the seam if it needs one.
- **Shocks are found to be separable from the dial.** This ADR treats them as one half because they
  share a blocker, not because they share a mechanism — a Shock moves a figure and the dial scales one.
  If Shocks acquire a home before the dial does, that is a further split and not a contradiction of this
  one.
