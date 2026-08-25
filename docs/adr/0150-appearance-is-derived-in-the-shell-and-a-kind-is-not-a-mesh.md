# Appearance is derived in the shell, and a kind is not a mesh

**A Building's appearance is composed in `Borough.Godot`, from the per-frame snapshot plus the
Building's monotonic id, and it never enters the `World`. No column, no saved field, no contribution to
the State Hash. And **a `[[building]]` kind is not a mesh id**: what a Building looks like is derived
from its kind *and* its declared occupancy, its tenant's trade, its Failure Pressure, its Lot's
frontage, and the land-value Cell under its footprint — so the 254-kind ceiling bounds what the city
can **simulate** and never what it can **look like**.**
`FAST ITERATION` `EMERGENCE` `LEGIBLE CAUSE`

⚠ **This decides a boundary and deliberately decides nothing about geometry.** The renderer is
*unbuilt*, and under [`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) that is not
a licence to design it here. What is taken now is the one part that is cheap now and expensive later,
on the same ground [`05 §9`](../05-technical-architecture.md) already applies to determinism, Chunk
size and routing: *anything on that list is validated early or not at all.*

## Why

### The alternative reintroduces asset pools from the storage layer

[`adr/0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md) refused Cities: Skylines'
model in terms this ADR only has to repeat one level down. There, growable prefabs are authored into
**mutually exclusive asset pools**, so a high-density cell cannot spawn a house, ever, and bad land
value yields a level-1 apartment rather than a bungalow. That ADR called it *placing buildings by
proxy* and rejected it because it makes a whole class of diagnosis unsayable.

A one-to-one `kind → mesh` mapping arrives at the same place by a worse road, because nobody would
have argued for it. `BuildingTable.Kind` is a `byte` and `RulesetLoader` refuses a file declaring more
than 254 Building kinds; a kind is a dense small integer because the **interpreter** wants one
(`adr/0048`). ***Sizing the city's visual vocabulary to a namespace chosen for the Rule engine's
convenience is an accident wearing a decision's clothes.*** The look of a city is not a property the
simulation has an opinion about, and it must not inherit one by aliasing.

### Art must not be able to move the State Hash

[`05 §4`](../05-technical-architecture.md) states the test: **a change is an optimisation if the State
Hash is unchanged, and a design change otherwise, however it was motivated.** An appearance stored as
a `World` column would be folded — the hash folds values, and every field is declared once as
`(saved AND hashed)` or `(derived AND rebuilt)` — so re-proportioning a façade would be a change to
the city, indistinguishable at the gate from re-tuning a production ratio.

[`adr/0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) says
moving the hash costs nothing while nobody is carrying a save, and forbids citing hash movement as a
reason to defer work. ***That is a licence with an expiry, and this decision is the one to take before
it runs out.*** After saves exist in players' hands, moving art out of the hashed set is a migration;
before they do, it is a sentence. `FAST ITERATION` is the tag because the property bought is exactly
the one that tag protects — the art loop is a rebuild-and-look loop, and a rebuild that could invalidate
a save is a loop nobody runs.

### `adr/0007` stops being a discipline and becomes a structural fact

[`adr/0007`](0007-stress-driven-simulation-detail.md) puts fidelity on the **place** rather than on the
camera, and the standing rule beneath it is that the renderer cannot influence the simulation. Today
that is enforced by nobody writing the wrong line. If appearance never enters the `World`, there is no
channel through which it could: the shell reads a snapshot and writes pixels, and the arrow has one
direction because there is only one arrow. This is the same move `05 §4`'s lints make everywhere else
— narrow the door rather than document the discipline.

### The derivation already exists, and it is already legible

Nothing here needs new simulation state, which is the test that separates this from a proposal.

| What the shell reads | What it becomes | Already true because |
|---|---|---|
| `occupants`, declared per kind | **massing** — footprint, height, openings | [`adr/0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md); a terrace and a tower differ *physically*, not by tier |
| the tenant's `[[business]]` | **the shopfront** — the ground floor and nothing above it | [`adr/0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)'s second namespace |
| **Failure Pressure**, a duration | **condition** — and it *recovers*, instantly and totally | [`adr/0053`](0053-failure-pressure-is-a-duration-not-a-tally.md): pressure resets the instant failing stops |
| the land-value Cell under the footprint | **material and upkeep**, spatially coherent | `02 §2.4`'s land-value field — ⚠ **two of four terms today, and amenity, the only positive one, is absent, so the field is bounded above by zero** (`adr/0123`) |
| the Lot's frontage and address side | **placement and orientation** | [`adr/0078`](0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md); exact integers, no geometry |
| `hash(world_seed, building_id, purpose_tag)` | **variation between neighbours** | the counter-based scheme in `05 §4`, read rather than extended |

Two of those rows are worth their own sentence.

**The premises are the massing and the tenant is the shopfront**, which is
[`adr/0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)'s own
example arriving in the renderer: *the same shopfront hosts a bakery, then a barber, and the building
did not change.* Under one kind table that sentence is not merely awkward to draw, it is **false** —
the trade would be a property of the walls. The split was forced by the high street and by a
condemnation defect; it pays a third time here, for free, and a re-skin of one storey is the cheapest
visual change in the pipeline.

**Condition needs no decay state**, because Failure Pressure is a duration rather than a tally. A
Building whose Trips start succeeding again *is not a Building working off a debt* — so it stops
looking derelict at the same instant it stops being it, with no second accumulator to bound and no
collection for [`adr/0006`](0006-no-collection-grows-with-elapsed-time.md) to object to.

### Determinism survives without hashing anything

Variation is drawn from `hash(world_seed, entity_id, tick, purpose_tag)`, the counter-based scheme the
core already uses, with a fresh `purpose_tag` so no visual decision correlates with a simulation one.
It is stable across save and reload because a Building's id is **monotonic and never reused** — the
same property that lets a handle column fold a target's id rather than its recycled slot. So the city
looks the same after a load without one byte of appearance being saved. ***Reproducibility and
hash-membership are different requirements, and only the first one is wanted here.***

## Consequences

- **`Borough.Core` returns ids and numbers, and gains no new obligation.** The real leak vector named
  in [`adr/0002`](0002-simulation-is-an-engine-agnostic-library.md) is a method returning a formatted
  string because a panel wanted one; nothing here asks for one. The shell resolves every name through
  the Ruleset, as it already does.
- **`05 §10`'s pipeline table is amended by addition**, not corrected: *per-Chunk MultiMesh per
  archetype* is untouched, and an **archetype is now explicitly a render-side grouping rather than a
  kind.** A pointer is added there so the section names this ADR.
- **The snapshot must carry the six inputs above.** That sharpens
  [`plans/0002`](../../plans/0002-open-questions.md) §C's standing `05 §2` question — *the sim/render
  boundary and the snapshot format* — rather than answering it, and the sharpening is filed there.
- **A kind may map to many looks and a look may serve many kinds.** Neither cardinality is constrained
  by this decision, and neither may be assumed by an implementation.
- ⚠ **The geometry pipeline is NOT decided here.** Instanced archetype meshes against
  procedural-per-Building geometry baked into one mesh per Chunk is **measurable** under
  [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) — a frame
  time and a Chunk-rebuild cost, on a named machine under
  [`adr/0106`](0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)
  — and is filed to `plans/0002` §B against a spike that does not exist. **No document may cite it as
  decided until that number exists.**
- ⚠ **Which quantities compose an appearance is NOT decided here either.** The table above is the
  argument's worked example and not a declared set; it is **arguable**, filed to §C, and it carries
  `plans/0002` item 28's guard one level down: ***capacity alone makes variants a ladder the player
  graduates up***, and a visual vocabulary that reads as a tier list has made the same mistake as a
  Service catalogue that does.
- **Board session L is unchanged in scope and reduced by one piece.** A presentation design still does
  not exist and still gates Phase 3; what it no longer has to decide is where appearance lives.

## What would trigger revisiting

- **A quantity the simulation needs to read back off a façade.** If shadowing, frontage articulation or
  any composed visual ever becomes an input to desirability, appearance has stopped being presentation
  and this boundary is in the wrong place. The tell is a Readout that cannot be computed without the
  renderer.
- **A measured composition cost that outlives a frame.** If deriving appearance per frame is too
  expensive and needs a cache, the cache's *location* reopens — a shell-side cache is still this
  decision; a `World` column is not, and the distinction must not be blurred by calling it derived
  state. ⚠ ***A structure that lives outside the world is not derived state, however it is declared***
  (`05 §4`), and the converse trap is the one to watch here.
- **The presentation design landing and disagreeing.** Session L is the owner of the whole surface;
  this ADR took one piece of it early and on purpose, and an early piece is the kind that gets
  overturned by the design it pre-empted.
- **A kind namespace widened past a byte.** If `BuildingTable.Kind` ever needs more than 254, the
  argument in §1 above loses its sharpest example and should be re-read on its remaining grounds, which
  are the ones that actually carry it.
