# Density is a cap, and it trades Land for Materials

**The player sets a ceiling, never a floor.** Zoning for density is permission, not instruction: a high band on land nothing wants to build on grows nothing, and that is information rather than a bug. Density is **capacity, not quality** — a high-density slum and a high-density tower are the same band. And it is the lever that **trades Land for Materials**, which is what makes choosing a band a strategy rather than a preference.

## The prior art disagrees, which is why this needs recording

| Game | Density is… | Set by | Emergent axis alongside |
|---|---|---|---|
| **SimCity 4** | a **cap** | player paints Low/Med/High | *Stage* 1–8, *Wealth* §/§§/§§§ |
| **SimCity 2013** | a **cap** | **derived from adjacent road tier** | density value, driven by happiness |
| **Cities: Skylines 1 & 2** | a **command** | player paints the band | *Level* 1–5 |

Every one of them separates density from quality. **That convergence is adopted**; the disagreement about cap-versus-command is not.

### Against the command model

In CS, growable prefabs are authored into mutually exclusive asset pools — a high-density cell *cannot* spawn a house, ever. Bad land value yields a level-1 apartment, never a bungalow.

This is placing buildings by proxy, and [`01-player-experience.md`](../01-player-experience.md) §2 names that as the line separating this from a city *builder*: **"the player never places a Building that Citizens live or work in."** It also makes a whole class of diagnosis unsayable. *"You upzoned Northfield six Days ago and nothing was built, because land value there is §40 and a mid-rise needs §180"* cannot exist under a command model — the tower simply appears. `LEGIBLE CAUSE` `PLAYER GOVERNS`

### Against the road-derived cap, which is the tempting one

SimCity 2013 has no density brush at all; the adjacent road tier sets the ceiling. It is seductive here, because it would make density *a condition read off the map* rather than a painted number — the same shape already preferred elsewhere in this design.

It is rejected on a specific ground: **SC2013 needed that gate because its traffic was fake. Ours is not.**

If a player puts two hundred Households behind one Access Point on a cul-de-sac, this simulation already answers. Segment Stress rises, the Segment goes Microscopic under [`0007`](0007-stress-driven-simulation-detail.md), Trips blow the Commute Budget, the Building accumulates failure pressure and declines with its reason recorded. **A road-derived cap would pre-empt the lesson the engine exists to teach** — refusing to let the mistake happen instead of letting it happen and explaining it.

**Geometry still matters, and that is not this.** A future reader will notice that street layout affects what gets built — block spacing determines how much land a parcel can hold, and `02-simulation-model.md` §2.2 refuses frontage to land the network cannot reach — and may reasonably conclude we contradicted ourselves. We did not, and the distinction is worth stating precisely:

| | What it is | Verdict |
|---|---|---|
| Road **type** granting permission for height | a **gate** — the game refuses to build what the network "cannot support" | **Rejected here.** It is a rule about what you are allowed to build, and it pre-empts the simulation. |
| Block **geometry** determining parcel size and frontage | a **physical consequence** — how much land is in the Lot, and whether it touches a street at all | **Kept.** It is not a rule at all; it is arithmetic over what the player drew. |

The first substitutes the designer's judgement for the simulation's. The second *is* the simulation. A player who draws sparse streets does not get a refusal — they get dead block interiors and a funnel through one Access Point, and both of those are consequences that explain themselves.

### And explicitly against SC4's Stage Caps

SC4 gated building stage on **regional population** thresholds. That is a hidden global scalar deciding local outcomes with no causal story — the same object as the RCI meter that [`00-vision.md`](../00-vision.md) pillar 1 forbids. The cap model is adopted; this part of its implementation is not.

## Capacity, not quality

Density says how many Occupants a Lot may carry. It says nothing about who they are or what they pay — those come from the choice model and the price system, which already produce them.

Collapsing the two into one ladder would make density *a thing the player graduates to* rather than a thing the player chooses between, and that framing is what guarantees the degenerate strategy of painting maximum density everywhere.

A consequence that corrects an existing document: **Buildings do not shrink.** [`02-simulation-model.md`](../02-simulation-model.md) §5.9 says a Building "declines a density level," which is physically incoherent. Decline drains occupancy and quality; the density ladder is walked at construction only, and the band is re-tested when a Lot redevelops.

## Two routes, not one scale

There are two physically distinct ways to reach the same residents per hectare, and they are separate bands rather than points on a line:

| Route | Shape | Access Points | Cost |
|---|---|---|---|
| **Subdivide** | many small Buildings, narrow Lots — terraces, row housing | one per Building; traffic distributed | consumes frontage |
| **Stack** | one Building, many Occupants — apartment blocks | **one, shared**; traffic concentrated | funnels everything through a point |

Because a Lot holds exactly one Building and a Building has one vehicle and one pedestrian Access Point, twenty Households in one block are **one** Access Point and a single Parking Shed query, where twenty terraces are twenty. Stacking earns that back in logistics: one Building is one set of Bins, so a single delivery serves twenty Households.

**Access Points per capita is the axis that separates them**, which is what makes a middle band mean something rather than interpolate. Cities: Skylines 2 shipped row housing as a distinct band for the same underlying reason.

## The strategic axis: Land against Materials

[`0022`](0022-land-is-a-stock-the-city-spends.md) left two scarce stocks that behave differently — **Land**, finite and sealed near-permanently, and **Materials**, locally finite then imported forever. Density is the lever converting one into the other:

- **Sprawl spends Land** and is Materials-cheap per resident.
- **Height spends Materials** and is Land-cheap per resident.

This is not a tuned trade-off; it is the physical one. The correct band therefore depends on which stock the city is short of *at that moment*, and the answer changes across a single playthrough — a forested, Materials-rich seed with cheap land favours sprawl; a city that has sealed its ground and is importing Timber anyway should build up.

It also supplies the endgame lever [`0022`](0022-land-is-a-stock-the-city-spends.md) asked for and rated more highly than replanting: **building on already-sealed land costs zero Land, because the Land is already spent.** Upzoning is how a city with no unsealed ground keeps growing, at a permanently higher Materials import bill. Sealing being near-permanent is therefore not a dead end, and the decay constant does not need loosening to fix one.

## What stops the player painting maximum density everywhere

Not a fee, not a filter, and not a penalty for misjudging demand — all three were considered and discarded as taxes on error rather than reasons to choose.

**The Hinterland runs dry.** Under [`0023`](0023-immigration-arrives-through-the-gate.md), the late game's only source of new residents is the city's own children, which requires housing Families. A city that offered nothing but towers has a demographic profile, not a penalty — and no second act. `Retention` stops being a nice-to-have metric and becomes the only growth channel left.

This is why dwelling-size preference by Life Stage can remain a **soft utility term rather than a hard filter**. Dense family housing is the global norm and blocking it would be false. The constraint was never that Families *cannot* live in towers; it is that a city optimised entirely for one segment eventually depends on a tap that stops flowing.

## Consequences

- **Density is a Zone attribute, not a regulatory one.** `01-player-experience.md` §2 assigned it to two verbs at the time this was written; the duplicate has since been removed, and emptying that verb was part of what led to `Fund` and `Regulate` merging into `Govern`. A blanket upzone remains expressible as a Policy over already-zoned land.
- **Downzoning may be a command where upzoning is permissive**, following SC4, whose asymmetry is deliberate: dropping the band demolishes over-tall Buildings immediately. Adopted in shape, unresolved in detail.
- **A Building holds many Occupants**, which puts the first container between a Citizen and their dwelling. Bounded by a hard invariant: **a Building aggregates logistics, never decisions.** It may hold Bins, one Access Point, one Parking Shed. It may never hold a Need, money, a Provider List, or a Trip. *A Building field that would have to be averaged across its Occupants is a Cohort forming* — which [`0005`](0005-two-fidelity-tiers.md) deleted, and which would re-enter here if anywhere.
- **Lot subdivision must vary by band**, per §2.2, and re-subdivision still only touches vacant land — so upzoning a built block does nothing until its Buildings go, which is how redevelopment becomes a real endgame activity rather than a formality.
- **Industry sits on the same axis and simply stacks badly**, because a foundry does not go on floor twelve. No second vocabulary is needed and no industrial *type* is ever painted — type emerges from what is reachable, per §5.5.
- **Density affects the Segment Stress distribution, not just population.** A stacked city concentrates load at fewer, larger nodes; a subdivided one spreads it. This is a live input to the routing and fidelity budget questions, not a cosmetic difference.

## What would trigger revisiting

- **Players reading "I upzoned and nothing happened" as a bug rather than information**, persistently, despite the diagnosis being available. That is a UI failure first; the command model is the fallback and it is a real loss of `PLAYER GOVERNS`.
- **The Land–Materials trade proving one-sided in balance testing** — if height is always correct, or never is, the axis is decorative and the Materials cost curve for stacking is the thing to examine first.
- **Subdivide and stack proving indistinguishable in play.** If Access Points per capita does not produce a felt difference in traffic, the middle bands are content rather than mechanics and should collapse.
