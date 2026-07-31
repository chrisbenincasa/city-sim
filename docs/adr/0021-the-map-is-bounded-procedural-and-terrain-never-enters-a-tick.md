# The map is a bounded procedural rectangle, and terrain never enters a Tick

**The world is a bounded rectangle**, sized at world creation, with Chunks stored **sparsely** so extent costs nothing until it is developed. **Terrain is generated procedurally from a shareable seed.** Height is real and the world is not flat — but height enters the simulation **only at construction time**, never inside a Tick phase. **Terraforming is a player verb, priced by cut-and-fill haul distance. Water is immutable.**

## The boundary is load-bearing

An infinite or edgeless world is not merely a bigger version of this one. It deletes two things:

- **Outside Connections live at the map edge.** No edge means no import and no export, which removes the pressure-release valve the entire economy is balanced around.
- **Finite land is a pressure source.** `01-player-experience.md` §5's pressure model assumes land runs out.

So the map is bounded, deliberately and permanently. Its size is a **world-creation constant** in the sense [`0015`](0015-all-tuning-data-is-hot-reloadable.md) defines: read from data, fixed per world, recorded in the save header.

## Sparse Chunks make extent nearly free

An undeveloped Chunk is a null, not an array. Memory and save size therefore scale with **developed area**, not with map area — a 4096² map with a city on 5% of it costs what a 1024² map with the same city costs.

This is stated as a commitment rather than an optimisation because it is the property that makes map extent a non-issue, and it is easy to lose by accident the first time something iterates all Chunks.

## Terrain enters at construction time, never at Tick time

The world must not be flat — topography is most of what gives a map identity, and it is the variation that makes seeds worth sharing. But there is a wide gap between *terrain exists* and *terrain is simulated*, and the expensive half is all on the far side:

| Terrain does | Terrain does not |
|---|---|
| Decide what can be built (maximum buildable grade) | Affect vehicle speed or travel time |
| Set construction cost via earthwork volume | Appear in the routing cost function |
| Force bridges and water crossings | Produce 3D junction geometry |
| Feed land value and desirability | Get read by any Tick phase |

**The checkable rule: if a terrain value is read inside a Tick phase, something has gone wrong.** That keeps the coupling `0014` was written to confine — grade-dependent vehicle performance, elevation in the Road Graph, junction geometry over sloped ground — permanently outside the simulation, while letting terrain do real work in the economy.

Note that "terrain does not reach the simulation" would be *wrong*: construction cost is the treasury, which is simulation state. The boundary is temporal, not categorical.

## Procedural generation, and the version trap

Terrain comes from the world seed, which the Input Log already carries and which [`0003`](0003-deterministic-integer-simulation.md)'s counter-based RNG makes the natural way to write a generator rather than a discipline to maintain.

Three payoffs: **shareable seeds** as a community object; **the map need not be in the save**, which becomes seed-plus-edits; and map variety that is topographic rather than a resource lottery — see [`0022`](0022-land-is-a-stock-the-city-spends.md), which deliberately removes the resource-lottery option.

**The generator's version is pinned in the save header.** If the generator changes in a later build, seed 42 produces different terrain and every existing save silently loads the wrong world — no error, no crash, a city floating over a landscape that moved. This is the same class of failure as `System.Random` changing in .NET 6, which `0003` already bans for exactly this reason.

**Generation must carry playability guarantees.** A seed producing no buildable land, or no water access, is a broken map rather than a hard one. Generation-with-guarantees is real work and is named here so it is scheduled rather than discovered.

## Terraforming, and why it is priced rather than capped

Without terraforming, terrain is a **wall**. With it, terrain is a **price**. The second is straightforwardly more `PLAYER GOVERNS` — the player negotiates with the landscape by spending rather than being refused by the generator.

The obvious way to stop the map being flattened is a superlinear cost curve, and it is rejected: a designed exponent is a fudge factor, which pillar 1 forbids. The honest mechanism costs the same to build and produces a better curve for free:

> **Earthwork cost is volume moved × haul distance to where the spoil goes.** Cut and fill must balance, as they do in real construction.

What emerges without being designed:

- **Levelling a small bump is cheap** — the spoil fills the dip 20 Tiles away.
- **Notching a ridge for a road is moderate** — some hauling.
- **Removing a mountain is ruinous** — there is nowhere nearby to put a mountain, so every cubic metre is hauled across the map.

The number is explicable rather than arbitrary — *"levelling this ridge costs §240,000 because 60,000 m³ has to travel 800 Tiles"* — which is the difference between a price and a penalty. `SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE`

Three riders:

- **Water is immutable.** No dredging, no filling. Hydrology and coastline are what give a map its identity and are the part worth protecting absolutely; everything else is negotiable at a price.
- **Occupied Tiles are locked.** Terraforming works on vacant land only, which removes the entire category of "what happens to the building I just raised twenty metres."
- **Earthworks cost money, not Materials.** Consuming the Materials Good would produce *"I cannot level this hill because the cement plant is down"* — a frustrating non-sequitur rather than an interesting constraint.

The haul model runs at construction time and never inside a Tick, so its cost is paid in development effort rather than in simulation budget.

## Consequences

- **Saves are seed + edits.** A terraformed Chunk stores its heights; an untouched Chunk regenerates. Sparse Chunk storage already provides this, so terraforming costs the save format almost nothing.
- **The generator is a versioned artefact** with its own compatibility story, separate from the save format version and the Ruleset version. Three version numbers in the header, each for a different reason.
- **Rates are Ruleset, not ADR.** Cost per m³, maximum buildable grade, and haul-distance scaling live in the Ruleset per [`0015`](0015-all-tuning-data-is-hot-reloadable.md) and are deliberately absent from this document, so there is exactly one authoritative copy.
- **Bridges are a buildability exception plus a rendering variant**, not a system. A Street or Arterial may span unbuildable water; the Road Graph does not know the difference.
- **Map size is not yet fixed.** See `plans/0002-open-questions.md`.

## What would trigger revisiting

- **The haul model proving unreadable in play** — players unable to predict a terraforming cost before committing to it. The fallback is a flat rate per m³ with a hard cap on deviation from original height, which is a tenth of the work and loses the explanation.
- **Terrain buildability proving too weak a constraint**, such that every map plays identically once flattened. That would be evidence the haul cost is tuned too low, not that the model is wrong — check the Ruleset before this document.
