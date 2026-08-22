# Base fertility is Ruleset data keyed by terrain type, and the old name invented a field

**`terrain suitability` is renamed **Base Fertility**, and it is not a field.** It is a **Ruleset value
keyed by terrain type** — the fertility of ground nobody has touched. What is stored per Cell is
**terrain type**, a world-creation column that is `(saved AND hashed)`, and **two** Ruleset values are
keyed by it: Base Fertility, and the Sealing decay rate that
[`0022`](0022-land-is-a-stock-the-city-spends.md) already specified this way.
[`0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)'s
**baked per-Cell suitability column is superseded**; its placement of five inventory rows at milestone 24
stands untouched.

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `PLAYER GOVERNS`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
What a term names, and whether a value is state or tuning, are both settled by argument; no measurement
distinguishes them.

## Why

### The term had exactly one consumer, and the corpus never said so

`terrain suitability` appears in six documents and **every occurrence is inside the Fertility formula** —
`CONTEXT.md`, `02 §2.3`, `02 §2.4`, `04 §1`, [`0022`](0022-land-is-a-stock-the-city-spends.md) and
[`0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md).
Nothing else reads it. `CONTEXT.md` → Fertility defines fertility as *"Agricultural capacity"*, so the
term means **the agricultural capacity of untouched, unpolluted ground** — Fertility's ceiling, and
nothing wider.

⚠ **It is not terrain's contribution to buildability, land value or desirability.**
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)'s table gives terrain
those jobs through **height** and **water**, which are different quantities with different consumers.
***A term named after a category is read as covering the category***, and this one covers one formula.

### The name invented the artefact

[`0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)
concluded that milestone 24 owes *"terrain suitability **baked at world creation into a stored per-Cell
column**"*. That artefact does not follow from anything the corpus states about the quantity; it follows
from the **word *terrain*** in its name. Read *terrain* and a terrain-derived per-Cell field is the
natural inference — and the ADR made it, in good faith, while citing
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) correctly throughout.

**`02 §2.3` says the opposite in one sentence**: *"**The generator places Woodland and nothing else.**
Fertility is not on the map — all land begins fertile."*
[`0022`](0022-land-is-a-stock-the-city-spends.md) says it twice more, and its *Why fertility is not on
the map* section is an argument **against** exactly the field `0124` specified.

***A badly named term is not a documentation defect; it is a design defect waiting for somebody to
reason from the name***, and `CONTEXT.md` had no entry for this one to have reasoned from instead. The
rename is the repair, and the entry is what makes it stick.

### The stored column is real, and it is the other thing

Dropping the baked column does **not** leave milestone 24 without a per-Cell terrain artefact.
`CONTEXT.md` → Sealing has always required one: *"Sealing decays at a rate drawn from the Ruleset **keyed
by terrain type** — rock may never recover, floodplain may recover over hundreds of Days."* A rate keyed
by type needs the type stored somewhere per Cell, and nothing stores it.

So `0124` **found a real hole and named the wrong occupant**. The column is terrain **type**; Base
Fertility and the decay rate are both lookups on it. One column, two consumers, and the second was
specified years of documents ago.

### Storing it is what `0022` forbids for its own sibling

[`0022`](0022-land-is-a-stock-the-city-spends.md)'s consequence list: *"**Decay rates are Ruleset data
keyed by terrain type, never stored per Tile.** Storing a rate as state would freeze it into every save
and make retuning a migration over every Tile — the exact failure
[`0015`](0015-all-tuning-data-is-hot-reloadable.md) exists to prevent."*

Baking Base Fertility into a saved column does to one term precisely what that sentence forbids for the
term beside it, on identical reasoning. **Two values keyed by one column, and only one of them was
protected**, because only one of them had been thought about as tuning.

### `0021`'s checkable rule is satisfied by precedent rather than by a new argument

`0124`'s reason for baking was that Fertility composes **inside a Tick**, and
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) forbids a Tick phase
reading a terrain value. Under this decision a Tick reads **a stored column and a Ruleset table** — which
is what Sealing's decay was always specified to do, and what
`0021` itself licenses when it says the boundary is *"temporal, not categorical"*.

⚠ **The bake is not merely unnecessary, it was load-bearing for nothing**: no Tick-time consumer needs a
terrain value that a type plus a lookup cannot serve. That is `0124`'s own third revisit trigger,
answered in the negative.

### Whether Base Fertility varies is a Ruleset stance, and exercising it amends `0022`

Because Base Fertility is keyed by type, **varying it costs one Ruleset key and no storage**. The engine
therefore permits both worlds and chooses neither, which is [`0015`](0015-all-tuning-data-is-hot-reloadable.md)'s
ordinary discipline.

🔴 **But the choice is not neutral and must not be made silently.**
[`0022`](0022-land-is-a-stock-the-city-spends.md) refuses a generated fertility gradient because it makes
farm siting *"a lookup … the map has a correct answer to where farms go before the player has done
anything"*, and its replacement for the map's character is **Woodland**. A Ruleset that varies Base
Fertility by type reintroduces exactly that gradient.

**So the shipped demonstration Ruleset states a uniform Base Fertility**, and `0022`'s design stance is
what a default world shows. ⚠ **A Ruleset that varies it amends `0022` and owes that amendment** — the
key exists so the question can be looked at, not so it can be answered by tuning.

***What the ground decides in the shipped world is not where you may farm, but whether your damage is
reversible*** — rock never recovers, floodplain does — which is a decision with a posted price rather
than a lottery, and it is the same shape `01 §5.2` wants from hazard.

## Consequences

- **`CONTEXT.md` gains two entries** — **Terrain** and **Base Fertility** — and its **Fertility** entry
  restates the formula with the new name. It had no entry for either, which is what let the old name go
  unexamined.
- **The formula reads `base fertility − Sealing − pollution`** in `CONTEXT.md`, `02 §2.3`, `02 §2.4` and
  `04 §1`. ⚠ **[`0022`](0022-land-is-a-stock-the-city-spends.md) and
  [`0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)
  are amended with a note and not rewritten**, per [`PROCESS.md`](../../PROCESS.md) → *Conventions*;
  `0124`'s filename keeps the old term because a filename is its claim.
- **Milestone 24's named artefact changes and its size does not.** It still owes one world-creation
  per-Cell column; the column holds terrain **type**.
  [`plans/0038`](../../plans/0038-terrain-and-the-land-rows.md) task 2 is restated.
- **Two Ruleset values are keyed by terrain type**, and both are **tuning, hot-reloadable, hash-bearing**
  — Base Fertility and the Sealing decay rate. Each owes a ratifier under
  [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md), and the world
  they are ratified against is one with varied terrain, which milestone 24 builds.
- **The terrain type column is world-creation, saved and hashed.** A type is a fact about the ground; the
  values keyed by it are not.
- ⚠ **The arithmetic is NOT settled here and is a separate decision.** `base fertility − Sealing −
  pollution` still subtracts three quantities in three units — a Tile count, a Q16.16 stock, and this
  one. `MapLayers.Desirability` faced the same problem and solved it with **weights**; Fertility has none
  written. Split out rather than bundled, because a status coarser than the claims it covers is
  [`plans/0012`](../../plans/0012-corpus-audit.md)'s granularity defect.

## What would trigger revisiting

- **A second consumer for Base Fertility appears.** It is named after one formula and belongs to it; a
  reader that is not Fertility would mean the quantity is wider than this ADR says and the name is wrong
  again.
- **A Ruleset wants Base Fertility to vary by type.** That is one key and no code, and it **amends**
  [`0022`](0022-land-is-a-stock-the-city-spends.md) rather than tuning within it — so it comes back here
  and to that ADR, not to a Ruleset review.
- **A Tick-time consumer of terrain is found that a stored type plus a Ruleset lookup cannot serve.**
  Then the bake `0124` specified was right after all, and
  [`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)'s checkable rule is
  what is under pressure rather than this decision.
- **Terrain type turns out to need more than a small enumeration** — enough types that a per-Cell type is
  a worse representation than a per-Cell scalar. Then the storage question reopens with the naming
  settled, which is the right order.
