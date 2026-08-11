# Player experience

What the player actually does, what it feels like over a session and over a campaign, and how the game communicates a simulation this deep without drowning anybody.

This document owns two open decisions that propagate widely: **whether transit is ever implemented**, and **whether car ownership is a choice**. Both are logged in §8.

---

## 1. The core loop

> **Observe → Diagnose → Intervene → Wait → Observe**

The `Wait` step is not filler. It is where the simulation does the work the player is here to watch, and the design's job is to make waiting *interesting* rather than idle. That means the city must be legibly changing at all times: Households arriving and departing, Trips succeeding and failing, Businesses stocking and starving.

Each step has a home in the interface:

| Step | Where it happens |
|---|---|
| **Observe** | Map overlays and the aggregate panels |
| **Diagnose** | **Evidence** — drilling from any aggregate to its named constituents |
| **Intervene** | The five verbs (§2) |
| **Wait** | The city, running |

The loop is deliberately slower than a builder's. This is not a game about laying track efficiently; it is a game about forming a hypothesis and testing it against a city that will not lie to you.

---

## 2. The player's verbs

There are five, and the list is short on purpose. `PLAYER GOVERNS`

| Verb | What it does | What it does *not* do |
|---|---|---|
| **Zone** | Paint a permission set over land — Residential, Commercial, Office, Industry-Extraction, Industry-Processing — with a density band | Place buildings. Zone Rules decide what actually gets built, and when. The band is a **ceiling**, never a floor |
| **Connect** | Lay Streets on the grid, draw Arterials, place authored Junction pieces | Micromanage lanes, signals, or turn restrictions |
| | ⚠ *As built (5a-bis): **Streets only**, one **Segment** per act — an origin intersection and an axis, lay or bulldoze ([`adr/0077`](../docs/adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md)). Arterials and Junction pieces are **refused by name** with their successor written beside the refusal, because a spline is many control points and is not one command, and a Junction piece needs the authored library `adr/0014` calls content.* | |
| **Service** | Place schools, utilities, health, fire, waste — buildings with a catchment | Guarantee that they are reached, or decide how many staff them |
| **Govern** | Set taxes, service funding, transfers, and constraints; borrow. Globally by default, overridden per District | Directly set outcomes. Every Policy is a Rule with a named payer and named beneficiaries |
| **Inspect** | Overlays, Evidence, Pin | Change anything |

**The unit of a road edit is a Segment, not a Tile, and that is a property of the graph rather than of the interface.** `adr/0014` says Streets snap to the grid, which reads as though a Street were paintable Tile by Tile; the Road Graph puts nodes only at intersections a block apart, so one Street Segment spans an entire block face. A per-Tile command would either split Segments — which `CONTEXT` → Address refuses, because it is what holds the graph at ~30,000 rather than 150,000–300,000 — or accumulate dozens of commands into one edge and leave all but one of them meaning nothing. **What the player drags across the screen is a presentation question; what reaches the Input Log is one edge.**

**`Fund` and `Regulate` were merged into `Govern`.** They were never different acts — both are *"set a parameter on a Rule the city then obeys"* — and the split was drawn on subject matter (money versus law) rather than on what the player does. `adr/0025` then emptied `Regulate` further by moving density caps into zoning, leaving a two-item verb. Constraint-versus-flow is a distinction *inside* Govern, not a division of the verb set.

**One honest cost, recorded so nobody later "fixes" it:** the `PLAYER GOVERNS` pillar's own wording is that *"the player zones, connects, funds, and regulates"* — so in the pillar's sense, **all five verbs are governing**, and naming one of them Govern overlaps. That is accepted. Govern is the best available word for *the things you set*, and the pillar text should not be narrowed to match, because the pillar is about the relationship between player and city, not about one menu.

**`Service` is the design's acknowledged placement exception.** Pillar 3 is govern-don't-place, and a fire station appearing wherever the simulation likes is bad play — so the player places service Buildings, and only those. Note what the player still does *not* control: staffing is demand-determined by catchment, so the number of teachers is set by the number of children (see `adr/0026`).

**The player never places a Building that Citizens live or work in.** That is the line that separates this from a city *builder*, and it is what makes the city's growth an answer rather than an instruction. When a zoned Lot stays empty, that is information.

**Inspect is a first-class verb, not a menu.** Roughly half the play time should be spent here, and the interface should be built as though that were true.

---

## 3. The first ten minutes

The opening must teach the causal chain, not the controls.

1. A road from the **Outside Connection** inward. Immediately, the map has an inside and an outside.
2. Zone a few Residential Lots. Nothing happens for a moment — then Households from the **Unplaced Pool** choose them, and buildings appear *because someone chose to live there*.
3. Zone Commercial. A shop opens, and its shelves are visibly empty until Goods arrive.
4. Click a Household. See where they live, where they work, what they need, and where their last Trip went.

Step 4 is the one that has to land. It is the moment the player learns that everything on screen is made of people, and that the game will always answer *why*.

**What is deliberately absent from the first ten minutes:** budget pressure, shocks, and any failure state. The opening teaches the loop; pressure arrives once the loop is understood.

---

## 4. Two hours, and twenty

**Around two hours** the first genuine constraint bites, and it should be a *spatial* one rather than a financial one. The classic shape: housing and jobs have grown in different places, commutes lengthen, and Trips start failing their **Commute Budget**. Businesses whose customers cannot reach them decline. The fix is not more money — it is geography, and the player has to read the map to find it.

This is also when the first Departures appear, and the distinction between **unhoused** (a capacity failure — build more) and **housed** (a quality failure — fix what you have) does real teaching work.

**Around twenty hours** the city is large enough that the demographic engine becomes the main event. Two independent forces are running:

- **Sorting** — who chooses to arrive and leave
- **Life Stages** — the population the city generates for itself

And they pull against each other in the way the design is really about: **affordability drives internal generation, attractiveness drives immigration, and attractiveness raises prices.** A city can be dying of its own desirability, with every attractiveness indicator excellent and the **Replacement Rate** quietly below 2.0. Reading which of the two channels is failing is the deepest skill the game asks for.

By this point the player should be managing Districts rather than Lots, thinking in Arterials rather than Streets, and using overlays as their primary view.

---

## 5. Pressure, and the intensity dial

### 5.1 Two axes, not three layers

Every trajectory in §6 bottoms out in one of exactly two scarcities, and the difference between them is whether money can solve it.

| Axis | Scarce thing | Why it binds | Reads as | Buyable out of? |
|---|---|---|---|---|
| **The Bill** | Goods, Materials, Food, Land, road capacity | Everything physical is importable or buildable at a price. The Outside Connection never refuses. | Money draining — the treasury, the balance of payments, empty shelves, jammed Segments | **Yes, always.** The dead end is expensive, not closed |
| **The Clock** | People, and the skills they carry | A Hinterland recovers at a rate. A Life Stage takes Days. Tier 1 → 2 is an Event Wheel countdown; tier 2 → 3 needs schooling. | Vacancies unfilled, Retention falling, Replacement Rate under 2.0, arrivals skewing cheap | **No.** No amount of money exceeds a recovery rate |

This is the through-line stated generally: **Goods are price-constrained; people are rate-constrained.** An earlier draft split pressure into Economic, Logistics, and Shocks, which failed two ways — logistics failures resolve into money, so those two were one axis wearing two hats, and seven of §6's nine trajectories had no home at all.

**The two axes are not independent, and [`adr/0035`](adr/0035-infrastructure-is-priced-by-what-it-consumes.md) is the first mechanism that couples them.** Infrastructure Upkeep is an automatic draw; borrowing is a player action rather than an automatic overdraft; so a treasury that empties leaves the maintenance Rule unable to draw, and unrenewed road life lowers capacity and free-flow speed. **An unpaid bill lengthens every commute.** The Bill becomes the Clock — not by a rule saying so, but because the thing money was buying was travel time all along. It is also what makes a fiscal crisis legible on the map rather than only in a number. `LEGIBLE CAUSE`

| Trajectory (§6) | Axis | |
|---|---|---|
| Insolvency | Bill | |
| Trade deficit | Bill | a *different* bill — the money supply, not the treasury |
| Gridlock | Bill | capacity you underbought; the fix is Materials and Land |
| Capacity failure | Bill | housing is construction; construction is Materials |
| Quality failure | **Both** | services are a bill; the Households leaving are a rate you cannot refill |
| Immiseration | **Both** | four of five exits are spatial; the fifth restores agency so the rest become reachable |
| Demographic stall | Clock | |
| Retention failure | Clock | |
| Labour mismatch | Clock | vacancies and unemployment together is the Clock's signature reading |

The two `Both` rows are the two the design cares most about, which is a sign the axes cut at a joint rather than sorting a list. Off-diagonal dial settings are therefore real games rather than multipliers: slack Bill with tight Clock is a rich city that cannot staff itself; tight Bill with slack Clock is a crowded city it cannot feed.

### 5.2 Shocks and disasters

Neither is a source of pressure. Both are **perturbations applied to the two axes** — a schedule for tensions that already exist. They are separated because they probe different properties.

| | **Shock** | **Disaster** |
|---|---|---|
| Where | the **Hinterland** — outside the map | the **mainland** — a footprint of Tiles |
| What moves | the authored figures: prices, wage, rent, population, composition | the world: Segments out, Buildings destroyed, Bins emptied, a Map Layer spiked |
| Onset | slow — drifts in and out over Days | sudden, with recovery over Days |
| Tests | **exposure** — how much of the economy runs through one edge | **redundancy** — whether an alternative exists |
| Can be *good* | **yes** — a boom edge, a migration wave | no |

**A shock is a movement in a Hinterland's authored figures, and nothing else.** That gives the layer one home, states every shock in domain units a player can read off a panel, and lets it propagate through chains the city already has — import price anchors Goods prices, the Hinterland wage anchors the wage surface, Hinterland attractiveness drives arrivals. Four edges drift independently, so shocks are spatially differentiated with no new machinery. Nothing here subtracts money directly.

**Disasters are not aimed at the player.** They simulate events outside human control, and the city's exposure to them is something the player authored by siting.

> **A disaster is the only instrument that can measure redundancy, because redundancy is invisible while nothing has failed.**

A city with one bridge to its industrial District and a city with three are identical on every overlay — same volumes, same commute times, same land values — until the bridge closes. No amount of `Inspect` finds that, because nothing is wrong yet. Same shape as §6's *notify what the player cannot be looking at*.

Three properties keep this a test rather than a tax:

**The city sets severity; the dial sets only frequency.** A disaster's initial footprint is small and fixed. What varies — by orders of magnitude — is how far it spreads before containment, and containment is an **ordinary Trip that can fail**. `Trip Fate` already enumerates *no route found* and *exceeded commute budget*, so a fire station behind a jammed Arterial loses a District and `Evidence` names the response Trips that overran. This is what §2's *"Service does not guarantee that they are reached"* has been describing with no mechanism behind it. No severity constant is authored anywhere; the only constants are a frequency interval and a spread rate, both **durations**, both scale-free.

**Every effect is an existing verb.** A Segment removed bumps the **Epoch** and cached routes revalidate lazily. Destroyed Buildings vacate **Lots**, which normal redevelopment reoccupies at Materials cost — so recovery time is the Bill axis reading the disaster back to the player. A fire spikes the pollution **Map Layer**. A proposed disaster effect that cannot be written in this vocabulary is a bolt-on.

**Hazard is terrain, precomputed, and visible from Tick zero.** Hazard regions are derived at world generation, never read from terrain during a Tick — so [`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) holds — and shown as an ordinary overlay. Cheap riverside land becomes a **decision with a posted price** rather than an ambush.

The catalogue, kept deliberately short:

| Disaster | Spreads via | Contained by | What it tests |
|---|---|---|---|
| **Flood** | precomputed floodplain, by depth | nothing; it recedes | **where you chose to build**, and whether Arterials cross it |
| **Urban fire** | adjacency; worse in stacked stock | fire service reachability | the road network |
| **Wildfire** | **Woodland — the fuel is the resource** | reachability, and how much Woodland remains | siting against the extraction frontier |

Wildfire needs no rule to become interesting: Woodland regrows on unsealed, unoccupied land, so a mature city **accumulates fuel on its own** as its frontier migrates outward. And a burn is a clearing minus the payout — it takes the Timber and leaves the fertile ground behind, exactly as a harvest would — so it is not purely a loss. `NO VERDICT`

Stacking is riskier than subdividing, also with no rule written: one Building destroyed displaces twenty Households instead of one. That hands [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s subdivide-versus-stack decision a third axis alongside Access Points and logistics. **Severance** gains a sharper meaning too — an Arterial with no crossing can mean the fire crew has no route.

**Utility failure** is the obvious fourth entry and is **blocked** on the utilities network, which is undesigned. **Outbreak** — spreading over the Trip graph, the one mechanism where being well-connected is a liability — **earthquake**, and **wind** are in [`deferred.md`](deferred.md).

### 5.3 Disasters are world-scheduled

The footprint and timing are `f(seed, Tick)` over precomputed hazard regions, with **no reference to what is standing there**.

This is the only version where the hazard overlay tells the truth. A disaster that fired only where there was something to lose would make riverside land cheap-*until-you-use-it*, and the overlay would be describing a trap rather than a price. World-scheduling means a player who sites carefully genuinely never sees a flood do anything, which is the correct reward and which city-scheduling structurally cannot give. It also keeps the dial honest: a disaster that scaled with the player's success would be an internal difficulty modifier, which §5.5 forbids.

Two riders:

- **A grace period in Days**, because §3 requires no shocks in the first ten minutes. A Tick condition, not a state condition, so the schedule stays a pure function.
- **Uninteresting disasters still fire, and are still reported.** *"Riverside floodplain inundated — 0 Buildings affected"* is the game telling a player that a zoning decision made forty Days ago was correct. There is no other way to be told that. `LEGIBLE CAUSE`

### 5.4 What the dial actually scales

| Sub-dial | Scales | Authored in | What it changes about play |
|---|---|---|---|
| **The Bill** | each Hinterland's **price level**, and the rate it lends at | § per unit, % | how expensive it is to import your way out of a physical shortage |
| **The Clock** | each Hinterland's **depth** and **recovery rate** | Households, Households/Day | *when* the Extraction → Cultivation transition arrives — early and forced, or late and optional |
| **Acts of God** | the **frequency interval** for Flood and Fire | Days | how often the city is tested |

Two things follow. **The list introduces no new parameters** — every entry is a figure the Hinterland already carries under [`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md) and [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md), plus one interval. Nothing is authored twice and nothing can drift out of sync with the model. And *"it scales parameters, never disables systems"* becomes **structural rather than aspirational**, because the dial has no reachable surface other than config the simulation reads anyway. No branch anywhere is written on intensity; two builds at opposite settings are the same binary reading a different Hinterland table.

An earlier draft named **tax tolerance** as an antagonist. There is no such scalar — tolerance is emergent, a Household comparing the city against a Hinterland with the same utility function everyone uses. It was a knob borrowed from the genre rather than from this design, as *"demand parameters"* was in the same paragraph.

### 5.5 Difficulty lives outside the map

> **The dial sets the terms of trade with the world outside. The difficulty inside the map is authored by the player, and the simulation only reports it.**

A tower behind a cul-de-sac strangles itself through Segment Stress and the Commute Budget at *every* dial setting. Nothing external is involved: the player made the geography, the geography made the failure. `PLAYER GOVERNS` `EMERGENCE`

Which sharpens what the dial is for:

> **The dial does not change the cost of a mistake. It changes the cost of recovering from one.**

The tower still strangles. What the dial decides is whether Materials can be imported through the shortfall while it is fixed (Bill), and whether the Households who left are replaceable (Clock). **Mistakes are made inside; the price of undoing them is quoted outside.**

The bound this accepts, recorded so nobody later relaxes it: the dial **cannot** make construction slower, services costlier to run, or decline steeper. Every one of those is a modifier on the city, and admitting one puts the constraint back to being policed by hand.

It also cannot be escaped. [`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md) makes a mature city a permanent net importer, [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) makes the gate money's only source and sink, and `adr/0023` makes it the only way people arrive. There is no border-closing strategy that opts out.

One consequence worth knowing when tuning: **the opening is nearly identical at every setting**, since §3 removes budget pressure and failure states from the first ten minutes and the Bill and Clock have nothing to act on until a mistake exists to price. Acts of God is therefore the dial's only early expression, and the one that makes a chosen setting feel chosen. Bill bites on §4's two-hour schedule; Clock on its twenty-hour one.

### 5.6 Modes, and what a lock is for

A **Mode** is a named preset over the dial, plus a lock policy. Not a separate concept.

| Mode | Bill / Clock / Acts | Dial | Why that lock policy |
|---|---|---|---|
| **Relaxed** | generous | freely adjustable | the setting is a comfort control |
| **Balanced** *(default)* | authored midpoint | freely adjustable | |
| **Challenging** | authored harsh point | set at creation, then fixed | commitment is the point |
| **Extreme** | authored near-max | not settable | the terms *are* the scenario |

**The lock is opted into, never imposed.** §2's *nothing is hidden or locked* is about the game withholding levers; a player choosing Extreme is choosing the constraint, and a player who does not want it does not pick that Mode. `NO VERDICT` holds — the game is not deciding anyone should be challenged.

**Mode is chosen at world creation and fixed for the world's life.** It is the design's only irreversible player-facing choice, and it is the price of the lock meaning anything. A downgrade path was considered and rejected: it earns nothing and lets a player quietly convert Extreme into Balanced at the first real setback, which is the exact failure the lock exists to prevent. The dial moves within whatever freedom the Mode grants; the Mode does not.

**Randomisation is orthogonal to Mode.** A separate toggle: the Mode sets a *range*, and randomise decides whether the player picks the point inside it or the seed does. Same mechanism [`adr/0027`](adr/0027-preference-is-drawn-per-household-and-persists-for-life.md) uses for Taste, where a Life Stage supplies a base and a range.

It costs nothing in legibility, because **a randomised dial is still fully readable**: its parameters *are* the figures on the Hinterland panel. A player with randomised edges does not face an unknown difficulty — they face four outside economies of different character, discovered by reading the world, which is the opening's real reconnaissance. Relaxed players should have that too.

**The corners of the cube are not all valid games.** Every Mode is a hand-validated point and the free sliders have floors. A Hinterland at minimum depth and minimum recovery produces no immigration at all, which breaks §3 outright — the opening depends on Households from the Unplaced Pool choosing zoned Lots. This is the second instance of a rule `adr/0021` already established for terrain: **a setting that produces no playable game is broken, not hard.**

### 5.7 Constraints on the dial

- **It scales parameters; it never disables systems.** Structural under §5.4 rather than a promise: there is no code path to disable.
- **It never touches an instrument.** Not detection, not notification thresholds, not `Evidence`. §6 derives its sustained-detection duration *from the mechanism* — the time abandonment contagion takes to reach neighbours — and a dial scaling that would return it to being somebody's guess. The tempting version is *hard mode warns you less*, which is difficulty-by-information-denial; the relaxed version is worse, since it hides problems from the player least equipped to find them. **The dial makes the city harder. It never makes the game less honest.**
  The separation is checkable rather than aspirational, via the **Input Log**: the dial is a simulation input and enters it; notification verbosity is presentation and does not. Two replays at different verbosity must produce identical State Hashes.
- **Shocks and disasters are seeded and deterministic.** Derived from the world seed and Tick, never wall-clock randomness, or replay breaks. See [`adr/0003`](adr/0003-deterministic-integer-simulation.md).
- **Every pressure has a legible cause.** The failure mode is a player who loses and does not know why. Each must be traceable through **Evidence** to the specific Buildings, Goods, or Households involved.
- **Prefer pressure that emerges over pressure that is scripted.** A Hinterland whose prices move and whose consequences propagate through the city's own chains is better than anything that subtracts money.

---

## 6. Failure, and what losing means

There is no game-over screen. There are **trajectories**, and the game's obligation is to make a bad one visible early enough to act on.

| Trajectory | Leading indicator | What it means |
|---|---|---|
| **Insolvency** | Debt service outrunning revenue | The classic. Recoverable, painful. |
| **Capacity failure** | Unplaced Pool growing, unhoused Departures rising | The city generates demand it cannot physically house |
| **Quality failure** | Housed Departures rising | The city houses people and then fails them |
| **Demographic stall** | Replacement Rate below 2.0, Childless share climbing | Too expensive to start a family |
| **Retention failure** | Spawned Households departing | Too expensive to stay in — you raised them and priced them out |
| **Gridlock** | The commute-time distribution's upper tail sliding toward the **Commute Budget wedge** | Trips are approaching failure across the board. Capacity you underbought — a Bill failure |

Three further trajectories arrived with the economy and labour work, and the third is the one with no counterpart above:

| Trajectory | Leading indicator | What it means |
|---|---|---|
| **Immiseration** | Destitute Departures rising, unemployment persisting | Neither capacity nor quality — the city **trapped** them. Five exits exist, only one of which is a transfer ([`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md)) |
| **Labour mismatch** | Vacancies and unemployment coexisting; underemployment share climbing | Shows up as Departures, but the remedy is **transport**, not housing ([`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md)) |
| **Trade deficit** | Imports exceeding exports; the money supply contracting | **Not insolvency.** Raising taxes does not fix it — that moves money inside a shrinking pool. The only remedies are fewer imports or more exports |

Note that four of the original six are demographic rather than financial. That is deliberate — a city sim whose only failure state is bankruptcy is a spreadsheet with roads.

**Every indicator in both tables is a state of the city, never a state of the simulator.** An earlier draft gave Gridlock the indicator *"Microscopic Segment budget exhausted"* — a software ceiling, triggered by a number in a config file, which fails the rule in §5 that an authored constant is acceptable only when it is the same thing the player is shown. It also let a resource limit borrow a diagnosis's authority. Reaching the **Microscopic Cap** means the simulation grows less precise where it was most needed; that is surfaced by `§7`'s existing requirement that an overlay mark a modelled number as modelled, and it is not an event. See [`03 §3.9`](03-agent-architecture.md).

The general rule, worth holding: **a trajectory names something happening to the city.** If an indicator would change when the simulation is optimised, it is not one.

### Failure is spatial as well as citywide

Every indicator above is a single number for the whole city, which was sufficient while the city was economically uniform. It no longer is, and it was made non-uniform deliberately: District-scoped Policy, abandonment contagion, and wage surfaces that differ by location. **A city can therefore score acceptably on every citywide indicator while containing a District in freefall** — aggregates hide exactly that, because it is what aggregates are for.

So **every trajectory must be expandable by District.** This is `Evidence` gaining a spatial axis rather than a new system: asking *"why is my tax base shrinking"* must decompose to a place, not stop at an average.

**No trajectory is terminal.** A District with no population, no land value, and full Sealing still has a recovery path, assembled from levers that exist for other reasons: remediation (pay to unseal), clearance of abandoned stock, a District tax override to zero, a service funding override upward, running transit in, and rezoning to a lower band so cheaper uses can bid. **The dead end is expensive, not closed** — consistent with scarcity being a gradient everywhere else in this design. None of these are unlocked or hidden; per §2 they are ordinary Policies whose preview simply reads *"applies to 0 Tiles"* until there is something to act on.

### Notification, and what earns one

**It should be straightforward for a player to diagnose a negative trajectory in their city.** That is the standard the whole information design is held to, and it is why **Evidence** is scheduled early in [`06-roadmap.md`](06-roadmap.md) rather than treated as polish.

*(An earlier draft said "in under a minute." That was dramatic rather than useful — unmeasurable, and it silently assumed the player already knew they were losing.)*

The aspiration is judged by humans in playtest. What is **checkable in a build** is the structural precondition beneath it: **no orphan figures.** Every displayed aggregate has a navigable path to its constituents, and every trajectory above reaches a named root cause through links the panels themselves provide. That is a test over the Evidence graph, it fails loudly when someone adds a figure without wiring it, and it is a constraint on the **simulation** rather than the interface — `CONTEXT.md` already states the principle: *if a figure cannot name its constituents, the simulation is computing it wrong.*

Note deliberately that the invariant checks **connectivity, not brevity**. A depth limit would reward collapsing chains — jumping from symptom to root reads as compliance while explaining less — so how *short* the chain should be is left to judgement, and only whether it *exists* is automated.

**Most causes need no history.** A trade deficit traces back through Materials imports to exhausted Woodland to Sealing — all of which are readable from present state, not from a log. Where history is genuinely needed, the fixed-size time series [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md) permits is sufficient, because Departures are already required to be counted **by reason**: the reasons survive even though the people do not.

But that test quietly assumes the player *knows* they are losing, and localised failure breaks the assumption — a player deep in one District can miss another declining entirely. Hence:

> **Notify what the player cannot be looking at.** Visible from anywhere — a boom, citywide construction — needs no notification. Visible only from *somewhere*, or from *nowhere at all*, earns one.

Notifications state an **event**, never a state, because an event is a fact and a state is a judgement: *"Eastfield: 34 Buildings abandoned this week"*, not *"Eastfield is failing."* Level-headed language, a feed rather than a modal, no recommendation attached, and clickable through to Evidence — **a camera hint, not a verdict.** `NO VERDICT`

**What earns one: a named trajectory becoming detectable in a place.** The table above *is* the trigger definition — every row is already a syndrome, a pattern of several indicators moving together, which is what makes it specific enough to notify on without a magnitude threshold. Three consequences: the feed is bounded by design (only these rows can fire), the trigger is documented in-game rather than buried in config, and there is one definition of "a problem" rather than a failure taxonomy and an alert system drifting apart.

Two detection paths, because they have complementary blind spots:

| Path | Catches | Behaviour |
|---|---|---|
| **Crossover** | healthy → declining | fires once, at the transition. Silent for a District that was never healthy |
| **Sustained correlation** | never healthy, so no transition exists | fires once the syndrome has persisted |
| **Pin**, generalised from Households to places and metrics | anything not named above | the player's own judgement, not ours |

Sustained detection introduces a **duration**, and durations are the acceptable kind of constant: scale-free, meaning the same thing in a village and a metropolis, where a magnitude threshold needs retuning as the city grows. Better, it can be *derived* — long enough that consequences are becoming hard to reverse, which for abandonment is the time contagion takes to reach neighbours. A number read off the mechanism rather than picked for the interface.

> **The general rule this settles: an authored constant is acceptable when it is the same thing the player is shown.** A threshold in a config file fails that test. A named failure mode in a table the player can read passes it.

The third row is the honest limit — the first two can only warn about failures we thought of.

---

## 7. Information design

### Overlays are a primary view, not a debug tool

The later SimCity games got this right and it is worth taking seriously: a map tinted by a single variable is the fastest diagnostic instrument in the genre. Expect overlays for traffic volume, land value, pollution (air, water, noise), utility coverage, service catchment, commute time, affordability, and Household composition.

Two rules:

- **An overlay must never be sharper than the player's ability to act on it.** This is why turning-movement diagnosis is deferred — see [`deferred.md`](deferred.md). Showing a problem with no corresponding verb is an invitation to frustration.
- **An overlay must never be sharper than the simulation underneath it.** Under [`adr/0007`](adr/0007-stress-driven-simulation-detail.md), congestion is exact where it is Microscopic and modelled where it is Statistical. Where the overlay is showing a modelled number, it should not pretend otherwise.

### Evidence is the spine

Every aggregate the game displays can be expanded into the specific entities behind it. Not "residential demand: 62%" but *"412 Households want to move in; 380 can't find anything under §900; 32 can't reach a job inside their Commute Budget"* — and each of those numbers opens into the actual Households.

This is a constraint on the simulation rather than a UI feature. **If a figure cannot name its constituents, the simulation is computing it wrong.**

### Pin, and the one family you follow

Players do not want to browse strangers; they want to follow *someone they were introduced to*. A Household met through Evidence can be **Pinned** and surfaced persistently thereafter, with a fixed-size ring of recent Trips.

Free-roam browsing of the population is explicitly not a mechanic — see [`deferred.md`](deferred.md) for why it is diagnostically worthless and what ships instead.

### Time is an arc, not a clock

There is no hour and no minute — see [`02-simulation-model.md` §1.2](02-simulation-model.md). Time of day is a **sun arc** with named phases: dawn, morning peak, midday, evening peak, night.

This is not decoration. A numeric clock makes a claim that can be checked against what the player is watching, and under any workable set of rates that claim is false — Cities: Skylines' calendar runs 112× faster than its own day/night cycle, which is why its players report cars taking "weeks" to cross town. An arc makes no numeric claim and so cannot be caught lying. Colossal Order reached the same place empirically and shipped a sun/moon arc rather than a clock.

**Commute Budget is drawn as a wedge on that same arc.** The budget and the day become one visual object, so there is no conversion between them to be dishonest about, and a failed Trip is a wedge that overran — shown against the day it overran in. `LEGIBLE CAUSE`

### Speed is where pacing lives, and Study is where truth lives

Four speeds and a pause. The simulation cannot observe which one is selected, so no speed changes any outcome; a longer Day at a slower speed buys the player real seconds to react, not a different game.

The default is **Normal**, not the slowest. The slowest — **Study** — is the speed at which rendered traffic is visually truthful, because apparent vehicle speed scales with the tick rate while the mechanics do not. Traffic looks true at exactly the speed where a player slows down to inspect it, which is the same principle as [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) arriving on a different axis.

The concession, recorded rather than discovered later: a player who never touches the speed control sees traffic running roughly twice as fast as its apparent size warrants, forever.

### The rendering promise

**Every visible agent is a promise you have to keep.** Willmott's observation about GlassBox is the warning: statistical simulations let players rationalise random or buggy behaviour as intelligence, and closing the visualisation gap removes that grace. If a behaviour cannot be afforded at full fidelity, it must not be drawn individually.

---

## 8. Open questions

1. **Is transit ever implemented?** [`adr/0008`](adr/0008-walking-is-a-simulated-leg.md) removed the irreversibility — a bus is a Leg type inserted into machinery that already handles Legs — so this is now a scope question rather than an architectural one. It remains the largest single unbuilt system and it interacts with 2, 3, and 4 below.
2. **Is car ownership a choice?** Every Household owning a car is the simple assumption. Making ownership respond to walkability and transit access closes the loop, letting parking pressure feed back into whether people drive at all. Only becomes interesting once transit exists.
3. **Open map or progressive land unlock?** Progressive unlock has an argument beyond pacing: it is a *physical* damper on the population feedback loop in [`adr/0011`](adr/0011-household-life-stages-and-self-generating-population.md), forcing a choice between density and family formation. If unlock, the gate should be **serviceability** — road network reaching the border, utilities with headroom — rather than a population or money threshold, so it stays a condition read off the map rather than a number in a config file.
4. **What does the education system actually look like?** Under [`adr/0010`](adr/0010-one-clock-and-demographics-by-sorting.md) schools work by **Sorting** — good schools attract already-educated Households — while under [`adr/0011`](adr/0011-household-life-stages-and-self-generating-population.md) school capacity is a **flow** serving Households passing through Family and Mature Family stages. Both are true simultaneously and the interface has to convey a *rate* rather than a count, which is harder.
5. ~~**How is the intensity dial surfaced?**~~ **Closed in §5.6.** A **Mode** is a named preset plus a lock policy, with the three sub-dials — Bill, Clock, Acts of God — exposed underneath to whatever extent the Mode permits. Randomisation is an orthogonal toggle drawing within the Mode's range.
