# Deferred

Ideas explored, found interesting, and deliberately **not built yet**. Distinct from rejected decisions, which live in `adr/` — everything here might still happen.

Each entry records what the idea is, why it's parked, **what would trigger revisiting it**, and **what it costs to add later**. That last field matters most: some deferrals are free to reverse and some are not, and knowing which is the difference between a deferral and a mistake.

**Entries that get built leave a line behind, never a silent deletion.** A record with a hole in it is worse than no record, because the absence is indistinguishable from the idea never having been considered.

- ~~**Public transit.**~~ **BUILT** — [`adr/0029`](adr/0029-transit-is-in-and-right-of-way-is-the-only-axis.md), session five. Transit ships, and right-of-way is its only axis. It was admitted not on genre convention but because five mechanisms already in the design — Density, Destitution, Parking, Office, Settlements — each had a cost with no counter-force. Worth remembering as the sharpest instance of this file's own lesson: **a deferral's retrofit cost is not fixed**, and [`adr/0008`](adr/0008-walking-is-a-simulated-leg.md) had already collapsed this one from *irreversible* to *incremental* for entirely unrelated reasons.

---

## Gravity-fed sewage

**Status:** parked as too complicated for now. Sewage is an ordinary Utility with storage; elevation is not read.
**Retrofit cost:** ✅ **Low-medium.** The network resolves at construction time, so it is an admission rule on where a treatment plant may sit and which Districts drain to it — not a change to any Tick.

Sewage runs downhill, and this design has real terrain, elevation, and cut-and-fill terraforming priced by haul. Modelling it is **not** forbidden by [`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md), which only bars terrain from entering a **Tick** — a gravity network resolves once, at construction.

It would be genuinely novel; no city builder in the reference set does it. Low ground becomes valuable for treatment, hilltop development carries a pumping cost, and the sewer becomes a spatial decision rather than a budget line — the one thing that would make the Networked bundle interesting to *site* rather than merely to fund.

It is also exactly the pipe-laying slog CS2 deleted, wearing a better justification.

**Trigger:** revisit only if the Networked bundle plays as a pure budgeting exercise — specifically, if plant siting turns out to be decided by land price alone with no spatial argument.

---

## Water depth, stratification, and tidal action

**Status:** parked under [`02 §2.5`](02-simulation-model.md) guard rule 5 — *a modelling refinement is admitted when a player decision distinguishes it.* These do not distinguish one.
**Retrofit cost:** ✅ **Low.** Depth is already a number (Bin capacity); along-shore advection is the wind-advection term [`02 §2.4`](02-simulation-model.md) has parked for air pollution. Neither is a change to how water is represented, only to what is read from it.

[`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md) makes every Water Body a Bin with a capacity and an outflow rate. Three refinements were raised and rejected, in ascending order of how nearly they qualified:

- **Stratification.** Real contamination is genuinely 3-D — a plume sinks or floats. No player decision distinguishes a stratified lake from a mixed one, so it is a second dimension bought with nothing.
- **Depth.** Not deferred so much as *absorbed*: a deep lake dilutes more, which is capacity. It enters as one number rather than as a dimension.
- **Tidal and along-shore currents.** The closest call. Direction genuinely *does* create a decision — site the outfall down-current of your beaches, exactly as you site industry downwind — so this passes guard rule 5 on its merits. It is parked only because the isotropic version already produces the decision that matters (*don't dump next to your own beach*), which makes direction a refinement rather than a prerequisite. Same reasoning `02 §2.4` already applies to wind.

**Trigger:** build along-shore direction when wind advection is built, since they are one mechanism and one implementation. Revisit stratification only if a Water Body ever needs to hold two Resources whose behaviours differ, which nothing currently suggests.

---

## Absorption varies by ground — parks, terrain, and Policies that clean

**Status:** parked by [`adr/0051`](adr/0051-industrial-pollution-is-a-stock-the-environment-absorbs.md), which ships **one global decay rate** and names this as the end state.
**Retrofit cost:** ✅ **Low, and it gets lower the longer it waits.** Tau is already Ruleset data read at the point of decay; making it a per-Cell lookup changes where the number comes from and nothing about the mechanism. The field it would need is a second sparse Layer on a grid that already exists.

`adr/0051` makes pollution a stock the environment absorbs, at one rate everywhere. In reality absorption is a property of the ground: vegetation, water and open land take up more than asphalt does, and that is the mechanism a park should use.

**It is the right shape for parks specifically, and the reason is worth keeping.** A park modelled as a *negative source* has two failures the absorption model does not: with no factory nearby it produces negative pollution, and a large enough park beside a smokestack produces a clean Cell next to the source. A park that makes its area absorb faster does nothing where there is nothing to absorb, and lowers the level beside a factory without ever erasing it. `adr/0051` records the full argument.

It also gives Policies a second lever with a different feel from the first. A scrubber mandate lowers what a Building emits and shows up immediately at the source; a tree-planting programme raises what the ground absorbs and shows up slowly, over the whole area, exactly as `Sealing`'s terrain-keyed recovery already does. **Two dials on one equilibrium, and both read off the same number the player is already looking at.**

**Why it is parked:** the global rate is unratified and nothing has been balanced against it. Varying a number nobody has fixed yet means tuning two unknowns against each other, and `adr/0044` is the standing example of what that costs. There is also nothing to vary it *with* — parks are not built, and terrain type is not yet read by anything on the Cell grid.

**Trigger:** parks or any greenspace land use entering the build plan; or the global rate being ratified and balance testing then showing that pollution reads the same everywhere in a city whose land plainly does not.

---

## Pricing pollution that crosses the map edge

**Status:** named in [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md) as a loophole, deliberately left open.
**Retrofit cost:** ✅ **Low.** It is a price on an existing transaction — the outflow edge from the last on-map Water Body to a Hinterland already exists.

A river's outflow terminates off the map, so a city sited upstream **exports its Waste to a Hinterland for free.** [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) conserves Money precisely so that exports have a price, and this one does not — which makes upstream siting strictly better than downstream siting for a reason nobody designed.

It is left open because the honest fix is not obviously a price. The alternatives are a Hinterland that degrades as it receives (making it a stock like every other Hinterland, per [`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md)), or a reciprocal inflow — pollution arriving *from* upstream, which the four-Hinterland model already has a natural home for and which would make river position a genuine trade-off rather than a free lunch.

**Trigger:** balance testing showing that upstream Districts are systematically preferred, or the first time a player notices that dumping in the river is free.

---

## Crime types

**Status:** parked. **One** Incident type ships. See [`adr/0030`](adr/0030-crime-is-an-incident-with-no-perpetrator.md).
**Retrofit cost:** ✅ **Low.** An Incident already has a place, a victim, and a response; a type field discriminates which feedback it drives.

Two types would earn their place only if they differ in cause, damage, or response. There is exactly one axis where they do, and it is worth recording so the deferral has a shape rather than being a shrug:

| Type | Deters | Feeds back into |
|---|---|---|
| Against **property** | Businesses — a shop robbed repeatedly closes | jobs, and therefore unemployment |
| Against **persons** | Households — a street people will not walk down | Amenity, and therefore walkability |

Two loops into two systems that both already exist. But one type plus saturating police returns produces most of the same play, and the project's discipline is that adding a category is a design decision rather than a content addition.

**Trigger:** ship one type. Revisit if playtest shows crime reading flat — specifically, if players treat it as a single meter to be minimised rather than as two different problems with two different fixes.

---

## Intersection-level traffic diagnosis and traffic-management tools

**Status:** parked. Segment-level volume only for now.
**Retrofit cost:** ⚠️ **Medium-high.** Not free. See below.

### What it is

Our congestion model accumulates **segment volume** — how many vehicles are on each stretch of road. Two intersections with identical segment volumes can behave completely differently, because what actually saturates a junction is its **turning movements**.

A four-way intersection carrying 800 vehicles/hour on its north approach:

```
  500  straight through   →  cheap; no conflict
  200  turn left          →  expensive; must cross oncoming traffic,
                             and blocks everyone queued behind while waiting
  100  turn right         →  free; crosses nothing
```

Those 200 left-turners can gridlock a junction that would handle the same 800 vehicles fine if they were mostly going straight. Segment volume cannot distinguish these cases. Turning movements can.

### What it would let the player see and do

With turning movements accumulated, the overlay could show *which specific movement* through a junction is saturated — "the left turn from Oak onto Main is the choke point" rather than "Main Street is busy."

That diagnosis only pays off if the player holds tools that act on it:

| Tool | What it does |
|---|---|
| Turn restrictions | Ban left turns at a junction, forcing traffic to route around |
| Dedicated turn lanes | Let turners queue without blocking through-traffic |
| Signal timing and phasing | Give the saturated movement a protected phase |
| One-way streets | Eliminate opposing traffic entirely, so left turns stop conflicting |
| Transit signal priority | Let buses pre-empt the phase |
| Junction type selection | Roundabout vs. signals vs. stop-controlled, each with different capacity profiles |

There is a genuinely good game in that list. Cities: Skylines players enjoy exactly this layer, and the mod ecosystem around it (TMPE especially) is one of the most-installed in the genre.

### Why it's parked

`00-vision.md` has an explicit anti-goal:

> **Not a traffic-management game.** Traffic is a consequence to be diagnosed, not a puzzle box of lane-level tools.

Every fix in the table above is a lane-level tool. Building the diagnosis without the tools would show the player a problem they cannot act on, which is worse than not showing it — an invitation to frustration. **Diagnosis should be exactly as fine-grained as the player's ability to act, and no finer.**

Note how this was decided: the question could not be resolved on technical grounds (turn accumulation is cheap in absolute terms), and an anti-goal written days earlier resolved it in one line. That is the design docs doing their job.

### What would trigger revisiting

- **The anti-goal changes.** If traffic management becomes something we *want* the player doing, this unlocks as a package — tools and diagnosis together, never diagnosis alone.
- **Playtesting shows players reaching for it.** If people are diagnosing junctions by staring at them — which is the observation-dependent behaviour we're designing against — that's a signal the game wants a tool it doesn't have.
- **Congestion patterns feel wrong without it.** Distinct from the overlay question: Microscopic Segments may need to model turn costs to produce realistic queueing, even with no turn overlay and no turn tools. That is a *fidelity* decision inside the traffic model and can be made independently.

### Retrofit cost — read this before assuming it's easy

The accumulator shape is baked into the congestion cycle. Segment volume is a per-segment counter; turning movements are per-(inbound, outbound) pair at each node — roughly 4–12× the accumulators, and the shape difference propagates:

- The **volume-delay function** changes: capacity becomes a property of a movement, not a segment.
- The **travel-time derivation** changes, because turn delay is where a lot of urban travel time actually lives.
- The **overlay rendering** changes from tinting edges to annotating nodes.
- The **cached zone-pair routes** must record which turn they take at each node, not just which segments they traverse.
- Existing **balance tuning is invalidated**, because travel times shift once turn delay is modelled.

None of that is architecturally impossible, but it is not a bolt-on. If the odds of wanting this later feel better than even, it's cheaper to accumulate turns from the start and simply not display them.

---

## Archipelago — disjoint sub-maps on one Tick counter

**Status:** parked. One contiguous bounded world ships instead.
**Retrofit cost:** ⚠️ **High.** Save format and the travel-time matrix both assume contiguity.

The version of "separate maps" that survives [`adr/0010`](adr/0010-one-clock-and-demographics-by-sorting.md): several spatially disjoint sub-maps of different sizes, all advancing on **one Tick counter**, connected only by explicit freight and transit corridors with real travel time. Nothing is ever frozen, so there is no second clock. The Road Graph does not care about contiguity, and Chunks are already the storage unit, so disjoint Chunk sets are structurally cheap.

It answers the thing regions were actually for — genuinely separate boards of different sizes, distinct terrain per board, a real region map, and the *ritual* of starting a fresh tile.

**Why it's parked:** two islands can never come into commute range of each other, so **Settlement merging and splitting cannot happen** — and that behaviour is the single most valuable thing [`adr/0020`](adr/0020-one-live-world-and-settlements-are-derived.md) produces. A region map that changes as a consequence of what you built is worth more than one that is a fixed grid of nine.

**Trigger:** playtesting showing the ritual of starting a fresh tile matters more to players than the merge/split behaviour. That is an empirical question and it is not obvious in advance.

---

## Free-roam citizen following

**Status:** parked. Evidence and click-what's-rendered ship instead.
**Retrofit cost:** Low. A UI layer over records the simulation needs regardless.

The ability to click *any* Citizen anywhere and follow them with the camera for the rest of their Trip.

Parked because it is **diagnostically worthless**. Everything a player needs to fix a problem comes from aggregates with good distributions, drilled into via Evidence. Free-roam browsing means hunting through a hundred thousand people hoping to stumble on something meaningful, when the game could simply hand you the household that illustrates the problem you're looking at.

**What ships instead**, covering the real needs at a fraction of the cost:

| Need | Solution |
|---|---|
| Understand a statistic concretely | **Evidence** — drill from any aggregate to its constituents |
| Interrogate a visual anomaly ("what *are* those stopped cars?") | **Click what's rendered** — already positioned, nearly free |
| Long-term attachment to an individual | **Pin** a Citizen met through Evidence |

The third is the one that was hiding inside the "follow anybody" idea. What players actually enjoy is following *one family across a whole game* — which requires being introduced to someone and keeping them, not browsing strangers.

**Why it's cheap to add later:** the expensive part — persistent individual records regardless of fidelity — is already load-bearing for reasons unrelated to inspection. `adr/0005` commits to individual decisions, which requires per-Household records, sticky Provider Lists, and per-Trip Fates. We bought identity to make the simulation work. Free-roam following is camera work on top of it.

**`adr/0007` made this materially cheaper, and it is worth noticing why.** The old design needed promote-on-click, a slot in a per-Citizen budget, and an honest failure message when that budget was full. None of that exists now. Fidelity belongs to Segments, so every Traveller has a renderable position at all times — statistical ones interpolated from departure and arrival, microscopic ones simulated. Following somebody is reading a position that is already there.

The lesson generalises past this entry: a deferral's retrofit cost is not fixed. Decisions taken for unrelated reasons can quietly collapse it, and nothing prompts you to re-check. This one went from "a mechanic with a budget interaction to design" to "camera work" without anyone touching the entry.

**Trigger:** cheap polish time near a release, or playtesting showing players trying to click things they can't.

**The real toil sink to keep avoiding** is not following — it is depth *inside* the Citizen. Life histories, relationships, memories, grudges. That is where Dwarf Fortress's decades went, and it remains an anti-goal.

---

## Illegal parking as an overflow channel

**Status:** parked. Graceful shed-widening ships instead.
**Retrofit cost:** Low. An extra branch at the end of an existing lookup.

When a Parking Shed is exhausted, `adr/0009` widens it — you park further away and walk longer. The alternative explored was an **overflow channel**: park illegally near the destination, arrive on time, and incur a penalty (a fine, a citizen satisfaction hit, or a contribution to Segment stress from a blocked kerb lane).

It is appealing because it is what actually happens in real cities, and because it separates two things graceful widening conflates: *there is nowhere to park* and *there is nowhere legitimate to park*. A district where 60% of arrivals park illegally is a sharper diagnosis than one where average walk time crept up by four minutes.

Parked because the penalty is the hard part, not the mechanism. A fine needs an enforcement model to be anything other than an arbitrary tax; a satisfaction hit needs to be distinguishable from every other satisfaction hit; a stress contribution needs kerb lanes modelled as distinct from travel lanes. Each is a small system, and together they are a bigger one than the problem justifies at MVP.

**Where it would slot in:** the nearest-first shed query already returns "no capacity found." Illegal parking is one more tier consulted after the legal ones, with a cost attached. Nothing structural changes.

**Trigger:** if playtesting shows graceful widening is too quiet — that players never notice parking pressure until the district is already failing. That would be evidence the gradient is *too* gradual, and illegal parking is the sharper signal.

---

## Household "would return if conditions improved"

**Status:** parked. Departure is permanent.
**Retrofit cost:** Low, but see the caveat.

A Household that gives up looking for housing leaves permanently. The alternative — remembering them and having them return once the city improves — gives the city a memory and produces a satisfying narrative beat.

Parked because it is **mechanically redundant**: a city that fixes its housing sees attractiveness rise and immigration generate *new* Households, which is the outcome the player wanted. The return mechanic buys the emotional payoff a second time at the cost of an archive.

**The caveat is the real reason:** an archive of departed Households is a collection that grows with elapsed time, which `adr/0006` prohibits outright. Any future version must be bounded — a fixed-size recent-departures ring, or regenerating a plausible returning Household deterministically from a seed rather than storing one.

**Trigger:** if playtesting shows players want the city to remember who left.

---

## A purpose-built DSL for the Ruleset

**Status:** parked. TOML for now.
**Retrofit cost:** Low. The format is an input to a stable interpreter; changing it doesn't touch the simulation.

GlassBox's custom rule DSL was significantly more readable than the equivalent TOML, and the Ruleset is the file we will read and write most. Parked because a DSL is a parser to write, test, and produce good error messages for, and `adr/0018` sets a standing bias against bespoke infrastructure.

**Trigger:** the TOML has become painful enough to measure — specifically, when rule authoring is visibly slowing down balance iteration.

---

## A fourth labour tier

**Status:** parked at three. Basic, skilled, advanced.
**Retrofit cost:** ✅ **Low.** Tiers are Ruleset data, and the mechanisms that read them — job minimums, employer mix requirements, promotion events, wage submarkets — are all count-agnostic.

### What it is

Three tiers compresses a real distinction at the top: **trained** work and **credentialed** work are not the same thing, and both currently land in tier 3. A fourth would split them — professional versus specialist — giving an individual Citizen a longer arc and the city's workforce composition a longer runway to keep evolving deep into a playthrough.

### Why it's parked

**Thinness lands in the worst possible place.** Wage signals shrink toward the Hinterland anchor in proportion to how few workers a submarket holds (see thread A). The top tier is already the thinnest market *and* the most load-bearing — it staffs Office, which earns exports, which pays the Food and Materials bill that [`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md) makes the endgame. Splitting it makes the single most consequential market in the game the one with the fewest agents in it.

The depth that a fourth tier was wanted for is available more cheaply from **employer mix requirements** — a tier-3 employer needing tier-2 support staff gives combinatorial depth from three tiers, scales with city size, and adds no new market.

### What would trigger revisiting

- **Playtesting showing the workforce settles** — the city reaches a stable tier composition mid-game and it stops being an interesting axis after that. That is precisely the "longer-term growth" gap this was raised to fill.
- **Office proving too easy to staff**, such that the export transition lacks the difficulty the endgame arc needs.
- **A city size well past current targets**, where even a split top tier holds enough agents for a local wage signal to be meaningful without anchor domination.

### What it would look like

Accept that the top tier is **anchor-dominated for most of a playthrough** — which is arguably correct rather than a defect, since a small city genuinely should be importing its specialists rather than growing them. The shrinkage rule already produces that behaviour with no special-casing; the question is only whether it *feels* like a market or like a constant.

---

## Household economics on the Citizen record

**Status:** planned next layer, not parked indefinitely.
**Retrofit cost:** Low *if* the hot/cold record split holds — economics belongs in the cold table, touched only when a Household transacts or the UI inspects it.

Ship with role and routine. Income, expenses, savings, and purchases-missed are the layer that makes the supply chain legible at the individual level, and the record must accommodate them without restructuring.

---

## Education and Health as degrading Needs

**Status:** parked by `adr/0103`, not by this file — the ADR closes the Need set at four and calls a
degradation rule for these two *"owed and deliberately undesigned"*.
**Retrofit cost:** Low. Sustenance and Satisfaction ship as two saved columns on `HouseholdTable` and
one writer, `RuleEngine.MoveNeed`, keyed off `[[resource]] need`. A third and fourth are two more
columns and two more Ruleset keys; nothing about the shape has to move.

### Why it's parked

**A Need is where a frequent private failure accumulates** (`adr/0103`), and the other two have no
frequent private failure to accumulate from. Sustenance falls when a Household's larder Rule blocks on
supply; Satisfaction likewise. Education and Health have **no occasion at all** — no school, no clinic,
no Rule that a Household runs and that can fail — so a degradation rule for either would be inventing
the occasion and the rule together. `RulesetLoader` refuses both **by name**, and the message says
*undesigned* rather than *not a Need*, because `adr/0070` turns on that difference and only *refused*
is evidence.

### What would trigger revisiting

- **A civic Building that a Household draws on.** `Service` is the unapplied verb and `School` is zero
  files; the moment one exists, the occasion exists and the degradation rule follows from it rather
  than being chosen.
- **`02 §5.4`'s aggregation form settling.** Nothing reads a Need yet — the reader is `adr/0102`'s
  housed Departure — and whether four Needs combine additively or multiplicatively decides whether a
  fourth axis is a fourth term or a fourth multiplier.
