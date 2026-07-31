# 00 — Vision

> Vocabulary used here is defined in [`CONTEXT.md`](../CONTEXT.md). Guiding-concept tags in `SMALL CAPS` refer to that file.

---

## The one-line version

A city builder where the city is made of people you can actually meet, the economy is made of goods that actually move, and when something goes wrong the game can tell you exactly why.

---

## The fantasy

You are not placing buildings. You are creating the conditions under which a city grows itself, and then living with what it becomes.

You paint zones, lay streets, run utilities, set budgets, and pass policy. In response, households look for homes near jobs they can reach; businesses open where inputs arrive and customers walk in; industry clusters where it can get its materials out. Nothing is placed because a formula said demand was 40%. Every building exists because specific simulated people had a specific reason to want it there.

And when a neighbourhood starts dying, you can click on it and find out why — not a red icon, but a chain: *this street's commute crossed the budget → these households' trips to work started failing → they left → the shop that served them lost its customers → it closed*.

That chain is the product. Everything else is in service of it.

---

## Pillars

### 1. Causally honest

Every number the player sees traces to something real in the simulation. `EMERGENCE` `SOLVE THE ACTUAL PROBLEM`

The RCI demand meter in SimCity is a magic global number produced by a formula. Here, residential growth is an *observable consequence*: these households want housing, within reach of these jobs, and this lot has the access to support it. Commercial growth means goods physically arrived and citizens physically spent money. There is no fudge factor and no hidden multiplier the player can't reason about.

This is a constraint on us, not a feature for the marketing page. It means we cannot patch a balance problem by adding a coefficient. When something feels wrong, we have to find the mechanism producing it.

### 2. The city is made of people

Citizens have persistent identity, a real home, and a real job — at every fidelity tier, always. `UNIQUE INDIVIDUALS`

This is the specific promise SimCity 2013 made and broke. Its sims went to the *nearest* house and the *nearest* job rather than *their* house and *their* job, because routing information lived in the world instead of in the agent. Everything people mocked about that game follows from that one decision.

We are making the opposite decision, permanently, and it is recorded as an ADR so it cannot be accidentally reversed.

The corollary is that fidelity and existence are different things. When the simulation can't afford to move ten thousand people tile-by-tile, it resolves their commutes statistically — but they still have names, still live where they live, still work where they work, and can still be clicked on and inspected. What varies is *how their movement is computed*, never *whether they are real*.

And **only** their movement. Every Citizen makes its own decisions at every fidelity tier — no Citizen ever inherits a choice made for a group. That is a harder commitment than it sounds, and it is the one that keeps this pillar honest: a population that shares a brain is a statistic wearing faces, which is exactly what we said we would not build. See [`adr/0005-two-fidelity-tiers.md`](adr/0005-two-fidelity-tiers.md).

### 3. You govern, you don't place

The player zones, connects, funds, and regulates. The city decides what actually gets built. `PLAYER GOVERNS`

You do not choose that this lot becomes a bakery. You zone for commerce, ensure flour can reach it and customers can walk to it, and a bakery appears because those conditions were met. If you starve it of flour, it fails — and that failure is your doing, indirectly, which is the interesting kind.

This extends to the supply chain. You are not managing production ratios on a spreadsheet. You are *enabling* chains to function through infrastructure and policy.

### 4. Legible failure

When something goes wrong, the game can tell you exactly why, and point at the buildings, goods, and citizens responsible. `LEGIBLE CAUSE` `HONEST DEGRADATION`

Failed trips are recorded with a cause, not silently discarded. Shortages name the chain that broke. Declining buildings say what condition they stopped meeting. When the simulation hits its own limits, it reports them plainly rather than quietly degrading.

The mechanism is **Evidence**: every aggregate number in the game expands into the specific entities behind it.

> **380 households departed unhoused this month** ▸
>
> *— the Reyes household, 2 adults 2 children, income §2,400/mo. Considered 20 dwellings over 4 months. Cheapest acceptable: §1,100. Nearest reachable job: 41 min, over budget.*
>
> *— the Okafor household, 1 adult, income §1,900/mo. Considered 20, rejected all on affordability.*
>
> *[show 3 more] [show on map] [pin this household]*

Same information as the statistic, made concrete — and concrete examples are how people actually understand systems. The statistic tells you a problem exists; the example tells you what it *is*.

This is not a citizen feature. It is a general pattern: click a road on the traffic overlay and get the trips using it; click a shortage and get the buildings starved of input. Every summary retains a pointer to its constituents, which makes the causal chain *navigable* rather than merely *true*.

This pillar is load-bearing in a way the others aren't — see below.

---

## The strongest argument against this design, and our answer

Don Hopkins, who worked with Will Wright on SimCity and The Sims, on Citybound — a project with almost exactly our ambition:

> "Trying to simulate every microscopic detail doesn't necessarily translate to a fun game (or even a realistic simulation). … Will Wright defined the **'Simulator Effect'** as how game players imagine a simulation is vastly more detailed, deep, rich, and complex than it actually is: a magical misunderstanding that you shouldn't talk them out of. He designs games to run on two computers at once: the electronic one on the player's desk, running his shallow tame simulation, and the biological one in the player's head, running their deep wild imagination."

Andrew Willmott, GlassBox's architect, arrived at the same place independently: statistical simulations let players rationalise random or even buggy behaviour as smart AI. Closing the visualisation gap removes that grace.

This objection is correct as stated, and it must be answered rather than waved away.

**Our answer: the payoff of deep simulation is not realism. It is explicability.**

A statistical model cannot answer "why is this neighbourhood dying?" — it can only report that it is. A microscopic model can answer it, all the way down to a named household whose commute crossed a threshold. That is a real, perceivable benefit that no amount of clever faking provides, because the causal chain either exists in the data or it doesn't.

Two hard consequences follow, and both are commitments:

**Evidence is built early, not late.** If the causal chain is not visible to the player, the simulation's depth is invisible and Hopkins is right — players genuinely cannot distinguish a deep simulation from a shallow one, *except* by drilling into it and finding something coherent underneath. Evidence is how they tell. It is scheduled as a foundational feature, before UI polish and before most content, and it doubles as the only viable debugger for emergent behaviour.

Note what this does *not* require: free-roam browsing of the population. Being able to click any of a hundred thousand dots is a fluff feature — pleasant, cheap to add later, and diagnostically worthless, because you would be hunting for a problem rather than being handed one. What earns its keep is the drill-down.

**Every visible agent is a promise.** Rendering an individual invites the player to judge its behaviour. Where we cannot afford intelligent behaviour, we do not draw individuals. This is why not every citizen is rendered, and why that is a design decision rather than a technical compromise.

---

## What we take from each reference

Being explicit about lineage, because these are the games whose corpses we are learning from.

| Source | What we take | What we refuse |
|---|---|---|
| **SimCity 2013 / GlassBox** | The production model: integer bins, atomic rules, data-driven and hot-reloadable, zone rules that grow the city by sampling lots | Routing state stored in the world. This is the single decision we are most determined not to repeat. |
| **Cities: Skylines 1** | The split between persistent record and transient embodiment, with a hard cap on the expensive half | Silent cap exhaustion — garbage piling up with no explanation. Our caps are visible. |
| **Cities: Skylines 2** | Cost-based routing with multiple weighted factors | Uncapped agents with dynamic re-pathing. Its simulation collapse is the counter-experiment that validates capping. |
| **SimCity 4** | The commute-time budget with trip failure, which makes geography matter and bounds pathfinding work | Optimising routes for distance while scoring the player on time. |
| **Anno 1800** | Only simulating logistics where the player is meant to optimise; exact integer ratios players can plan on paper | Making the supply chain the whole game. |
| **Factorio** | Sleeping entities that cost nothing until something wakes them; low-frequency simulation with render-side extrapolation | Item-level logistics simulation. |
| **Dwarf Fortress** | Depth as a source of stories; simulate one layer below what the player sees, no further | Unbounded object accumulation. Every producer needs a sink. |
| **Citybound** | Lane-as-entity traffic; routing like network packets rather than per-agent search; bounded satisficing households; relative need values | Building your own actor runtime, allocator, geometry kernel, renderer, and toolchain. Ten bespoke libraries, three engine rewrites, no shipped game. |
| **Watch Dogs: Legion** | Persistent identity without persistent memory — detail generated deterministically from a seed rather than stored | — |

---

## Anti-goals

Things this game is deliberately not. Each of these is a boundary we expect to be tempted to cross.

**Not a factory game.** Supply chains create meaningful pressure; they are not the primary optimisation surface. Goods pool freely within a district precisely so the player isn't routing trucks. If zoning ever becomes an afterthought to logistics, the design has drifted and we should notice.

**Not photorealistic.** Low-poly is a permanent commitment, not a placeholder. It is what makes the agent counts affordable, and Cities: Skylines 2 is the cautionary tale — it shipped with 121 million vertices per frame, no occlusion culling, and character models with fully-modelled teeth. Its simulation was fine. Its renderer wasn't.

**Not a traffic-management game.** Traffic is a consequence to be diagnosed, not a puzzle box of lane-level tools. We simulate traffic in detail so that congestion is *real*, not so the player can micromanage turn restrictions.

**Not a life simulator.** Citizens have roles and routines, and eventually household economics. They do not have relationships, memories, or grudges. If that depth is ever wanted, it is generated on demand from a seed rather than stored — but the default answer is no.

**Not multiplayer.** The deterministic core keeps the option technically open, and that determinism is worth having for entirely different reasons (replay, testing, bug reports). But nothing in the design is shaped by multiplayer, and architecting for distribution before having a game is a documented way to not have a game.

**Not moddable at launch** — though the data-driven ruleset means it will be moddable almost by accident. That is a reason to keep the rule format clean, not a reason to build mod tooling now.

**Not on a schedule.** This is a long solo project at an irregular pace. Every milestone leaves the project runnable, because there will be gaps of weeks and it must be re-enterable cold.

---

## Adjustable pressure

The game should span relaxing sandbox to genuine challenge without becoming two different games.

Three independent, separately tunable pressure sources:

1. **Economic** — service costs, tax tolerance, debt interest. The budget as antagonist.
2. **Logistics** — shortages when supply chains break under growth. This must be visible in individual citizens' unmet needs, not just a global happiness number.
3. **Shocks** — recessions, migration waves, resource depletion, infrastructure failure.

Constraints on how the intensity dial works:

- **It scales parameters; it does not disable systems.** Relaxing mode means generous margins and rare shocks, not a different code path. A disabled subsystem is a second game that has to be balanced and tested separately.
- **Shocks are seeded and deterministic**, derived from the world seed and tick. Never from wall-clock randomness — that would break replay.
- **Every pressure source has a legible cause.** The failure mode is a player who loses and doesn't know why. `LEGIBLE CAUSE`
- **Pressure emerges from the simulation** wherever possible. A recession that shifts demand parameters and lets consequences propagate is better than one that subtracts money.

---

## Scope commitments

Settled decisions that bound the project. Full rationale lives in the ADRs.

| | |
|---|---|
| **Setting** | Single modern era. Progression through unlocks, not through time periods. |
| **Initial scale** | ~10,000 citizens playable. Architecture supports far more; raising the ceiling should be tuning, not rewriting. |
| **Goods** | Between three and eight, with real production chains. Adding one is a design decision, not content. |
| **Roads** | Grid-snapped streets, plus a small number of freeform arterials using authored junction pieces. |
| **Citizens** | Ship with role and routine. Household economics is the planned next layer and the record must accommodate it without restructuring. |
| **Transport** | Trips are sequences of legs from day one, even while only driving exists. Whether transit is ever implemented is an open question — see below. |
| **Visuals** | Low-poly 3D. Not every citizen is rendered. |

---

## Open questions

Real forks, not oversights. Each is owned by the document that will resolve it.

1. **Is multi-modal transport in the vision at all?** Pedestrians and public transit. This is the highest-priority open question: Citybound built cars first and planned to add the rest later, and the cars-only assumption shaped its lane model, routing model, trip model, and zoning model so thoroughly that it was never retrofitted. The `Trip`-as-legs abstraction protects us structurally, but whether transit is ever *implemented* changes the design. → `docs/01-player-experience.md`

   **The case changed materially with [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md).** Transit was previously argued on realism and variety, which is a weak case for the largest unbuilt system in the project. It now has a **mechanical job**: wages move in both directions, so an employer nobody can reach must pay a premium to compensate for the commute. **Transit widens labour sheds, which lets employers cut wages, which improves margins, which supports employment** — a payoff measurable in the same units as everything else rather than a vibe. It is also one of the five exits from destitution ([`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md)), which makes **infrastructure an anti-poverty instrument** and gives a subway line a second justification entirely independent of congestion.
2. ~~**How long is a game day?**~~ ~~Does the city age?~~ **Settled.** A Day is 8192 Ticks — 8m32s at the default speed, 17m at the slowest — and the city does not age. See [`adr/0019`](adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md). The lesson worth keeping: the Day's length in *real seconds* is free and lives entirely in the speed control, while its length in *Ticks* is the traffic balance in disguise. Two questions wearing one name.
3. ~~**What is the map?**~~ **Settled.** One bounded procedurally-generated rectangle, entirely live, with **Settlements** — commute sheds — emerging, merging and splitting as consequences of what the player builds. See [`adr/0020`](adr/0020-one-live-world-and-settlements-are-derived.md)–[`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md). Two things came out of this that belong in the vision rather than a technical doc: **the region view is a diagram of the city's real commute structure, not a menu of tiles**; and **land is a stock the city spends**, so a mature city is a net importer of Food and Materials and its growth costs rise with its size. Remaining sub-questions in `plans/0002-open-questions.md`.
4. ~~**Which goods, specifically?**~~ **Settled at five** — Produce, Food, Timber, Materials, Consumer Goods — with two extraction steps, three processing steps, and a maximum chain depth of three. The asymmetry between the three sinks is the design: Food fails fast and hard (crises), Consumer Goods fail slowly and softly (decline), and Materials are not a Need at all but gate construction, so a shortage stops growth with nobody unhappy anywhere. A sixth Good must *replace* one rather than extend the list. **Office is the employer that produces no Good**, and money is the conserved stock that is deliberately not one. → `docs/04-economy-and-goods.md` §1

---

## How we will know this is working

Not metrics — smell tests. If these stop being true, something has gone wrong.

- Every number on screen can be drilled into, and what you find underneath is coherent.
- When a building declines, the game names the condition it stopped meeting.
- Changing a production ratio and seeing the effect takes seconds, not a rebuild. `FAST ITERATION`
- A bug report is an input log, and it reproduces exactly.
- The city surprises us — it produces patterns we didn't design.
