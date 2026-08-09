# Routing intent lives in the Traveller, never in the world

**A Traveller carries its own destination. The Road Graph carries directions, never intentions.** A flow field may tell a Traveller which way to turn; it must never tell it where it is going. Every Trip has an origin and a destination that belong to a specific Citizen, and no shared structure in the world is ever permitted to supply that destination on the Citizen's behalf.

This is stated as an ADR rather than left implicit because it is the single decision that separates this design from SimCity 2013's, it is invisible in a profile, and it is exactly the kind of thing that gets reversed by accident in the name of an optimisation.

## Why

SimCity 2013's GlassBox attempted almost precisely this fusion — a rule-driven production economy with agents moving between Buildings — and its production model was good enough that we adopt it nearly verbatim (see `CONTEXT.md`, *Buildings and the rule engine*). Its movement model was catastrophic, and Andrew Willmott named the cause at GDC 2012 **as a feature**:

> "No per-agent routing info."

Agents descended a shared cost-to-nearest-sink gradient field. The consequence is not a bug list, it is a single structural fact: **a Sim could not have *my* home or *my* job — only "the nearest thing advertising `Home`".** Every mocked behaviour follows inevitably from that one sentence, and none of them is fixable without changing it:

- **Memoryless Sims.** A worker returning at the end of a shift descends the Home gradient and arrives at whichever house is nearest. There is nothing to remember with.
- **Tourist packs.** Identical agents on an identical field take identical paths, because the field is the decision.
- **Fire trucks clumping at one fire.** Every truck reads the same gradient. The field cannot express "that one is already handled" because the field has no idea who is asking.

It also explains the 2 km × 2 km map cap. A field must be maintained over the whole map for every sink type, so cost scales with **map area × sink types**, not with the number of Trips actually in flight. Small maps were not an art decision.

Cities: Skylines 1 supplies the missing piece, and it is the shape we take. CS1 split the persistent record from the transient embodiment:

| | Holds | Cap |
|---|---|---|
| `Citizen` | Real home, real workplace — permanent identity | ~1,048,576 |
| `CitizenInstance` | The materialised walking/driving thing, created on demand, released on arrival | 65,536 |

**This is not a compromise on deep simulation; it is strictly better than embodying everyone.** Identity is preserved permanently and cost is paid only for motion. Our Citizen / Traveller split is this split, renamed (`CONTEXT.md`, *Citizens and fidelity*), and Watch Dogs: Legion's *Census* and RimWorld's `WorldPawns` are the same idea arrived at independently.

The rule holds at **every** Fidelity tier, which is why it is not subsumed by [`0007`](0007-stress-driven-simulation-detail.md). A Traveller on a Statistical Segment is an origin, a destination, and an arrival Tick — it is *cheaper*, not *vaguer*. Statistical resolution changes how movement is computed; it never changes whose Trip it is. `UNIQUE INDIVIDUALS`

It is also what makes the **Provider List** meaningful. A Household holds a short, sticky set of known shops and workplaces and switches only when a *known* alternative is *substantially* better. A gradient field is the exact opposite model: perfect global knowledge of the nearest sink, re-evaluated continuously, shared by everyone. Satisficing over a private list is cheaper *and* more realistic, and it cannot be expressed in a structure that does not know who is reading it. `BOUNDED KNOWLEDGE`

## Rejected

**Flow fields for commuting.** Rejected on shape, not on cost. One field serves one destination, so commutes — many origins, many destinations — need one field per workplace. Fields are the wrong data structure for that query, and reaching for them anyway is precisely the error GlassBox made.

**Flow fields remain legitimate for genuinely one-to-many queries**, where the destination set is small and the field is shared honestly: nearest hospital, nearest fire station, nearest map exit. One field per **destination**. The test is simple — if every consumer of the field would accept *any* qualifying endpoint, a field is correct; if a specific endpoint belongs to a specific Citizen, it is not.

**Storing the destination on the Lane or Segment** — routing tables keyed by destination, as in the distance-vector option under evaluation for pathfinding — is *not* a violation and must not be confused with one. Such a table answers "which successor leads to X?"; the Traveller still supplies X. That is the world giving directions, which is allowed.

## Consequences

- **Destination is a required field on every Trip, at every Fidelity.** There is no representation of a Traveller that lacks one. A code path that would construct one is a bug, not an optimisation.
- **Route caching is keyed by origin-destination pair, not by agent**, and invalidated lazily against the Road Graph Epoch. Sharing a *computed route* between two Citizens travelling the same pair is fine — sharing the *choice of destination* is not. The distinction is the whole ADR.

  > **AMENDED by S2 R5 and R6, on measurement.** This bullet's substantive claim — that a route is keyed by *pair* and never by *agent* — survives untouched; it is the sentence's other three clauses that were underspecified or wrong. What follows replaces them. Numbers in [`spike-results`](../spike-results.md) → *S2 R5*, *S2 R6*.
  >
  > **The key is a pair of nodes, not a pair of Buildings, and the endpoint is chosen at insert.** *"Origin-destination pair"* was written before anyone knew an Access Point is a `(Segment, offset)`, and the phrase is ambiguous between a key space of nodes² and one of Buildings². It is **nodes²**. An Access Point's Segment has two ends and the naive choice — always the `a` end — **costs exactly 2× the nearest end on every rung, geometrically**, and the fix is one comparison at insert with the key space unchanged.
  >
  > **Quote the induced error as an absolute and never as a percentage.** R6.1a measured it at **0.86–0.94 Ticks, flat across the entire origin-destination family**, while the same quantity expressed as a percentage swings **1.84% → 9.70%** with the trip distribution. The absolute is the property of the graph; the percentage is a property of whichever draw somebody used. *This is the second time this shape has been found in S2 — the rung-invariant number is the one to carry — and here it exists.*
  >
  > **A coarser key's *benefit* is unconfirmed and may not be cited.** R6.1b built and swept two candidate coarsenings and **the collapse column reads 1.00× on every row of both**: a node key merges two Trips only if they coincide at *both* ends, and 33,018 Segments is 1.09 × 10⁹ ordered pairs. So the price of the key is settled exactly and the benefit cannot be settled at all until Trip generation exists (`06` 5b). **Do not repeat the five-Buildings argument that used to sit behind the coarse key.**
  >
  > **Eviction is fixed capacity, four-way LRU, indexed on the high bits.** `adr/0017` already showed the pattern — fixed capacity, least-used eviction — and nobody had sized it for routes; this is that number. Conflict misses fall **20.0% → 10.6% → 3.8% → 1.4%** across 1, 2, 4 and 8 ways against a fully-associative bound of 0.0%, and **four ways recovers most of the gap at four contiguous probes** on a cache line the entry already occupies. The high-bit index is a **robustness** fix and not a throughput one: level-or-worse on random keys, and worth **31.2% → 21.7%** on a concentrated destination pool. Both changes are worth making and only one of them shows up in the average case.
  >
  > **A miss is reported as blame, not as a rate.** The cache's absolute hit rate rests on Trip repetition and is unmeasurable before `06` 5b, but a lookup that *should* have hit is a pure loss whatever the repetition rate turns out to be. On that split, **R5.3's 28–31% "miss floor" is 20.0% conflict and 0.0% capacity** — every one a lookup a perfect cache of the same size would have served. It was never a floor; it was a bug with a fix. **Load is the axis that dominates and it had never been swept**: conflict at four ways runs 1.0% → 3.8% → 16.2% across 0.25×, 0.50× and 1.00×.
  >
  > **"Invalidated lazily against the Road Graph Epoch" says when you pay, not what survives** — the distinction [`CONTEXT.md`](../../CONTEXT.md) → Epoch has since taken and which `05 §3` is still owed. It also implies a granularity this ADR never chose, and the choice is load-bearing: **a single-counter Epoch *is* a global flush.** Under a sustained edit storm it retains **9%** of the no-edit ceiling where a per-Segment Epoch retains **96%**, which fires `plans/0010`'s *global flush* tripwire on a design commitment rather than on a number.
  >
  > **And the two consumers of invalidation do not want the same mechanism**, which is the finding that stops this being one contract. Routes needed a *temporal* answer, because R5.4 found **no Epoch rung both affordable and correct** across the core verb — a TTL rotation at 0.40 forced refreshes per Tick clears the wrongly-valid count **38 → 0 while retaining 97.08%**. The Parking Shed needs no rotation at all: **per-Segment witnessed by paths is the only rung that fits**, at 26.10% of a Tick worst case against a global rung's 1,638.20%.
  >
  > **The invalidation *contract* — what a stored route is allowed to be wrong about — was session M's, and it is below.** Neither the key nor the eviction policy is downstream of it, which is why they were settled here and it was not.

- **The invalidation contract is asymmetric: never wrong about a removal, boundedly wrong about an addition.** `SESSION M` `BOUNDED KNOWLEDGE` `PLAYER GOVERNS`

  > A stored route is **never wrong about a Segment that was removed.** The test is exact — *does this route contain that Segment* — it is containment rather than geometry, and the per-Segment Epoch runs it at the moment of the edit. There is no interval during which a Traveller may drive through a bulldozed road.
  >
  > A stored route **may be wrong about a Segment that was added**, for at most `T` Ticks **of its owner's travel**, and never for longer. `T` is a Ruleset number and it *is* the learning rate: it is the answer to *how long after a player builds a bypass does the city start using it*.
  >
  > **The asymmetry is forced, not chosen.** For a removal the exact test exists. For an addition it cannot: the new Segment is on no existing route, so containment has nothing to match and only proximity is left — and S2 R1.7 measured what proximity does, missing **309 of 429** changed entries on a central edit **and missing them silently**. There is no sound targeted test for addition, so the contract cannot promise one.
  >
  > **Two setters, one bit, one drain — and it is not two mechanisms.** A Habit carries a **stale bit**. Age past `T` sets it. An addition within `d` sets it. Nothing else does, and **neither setter recomputes anything**: both are satisfied by the same drain at Trip start, on the budget line that was already there. *Never update* was ruled out explicitly and this is where the ruling-out bites.
  >
  > **The wake is unsound alone and the bound is what makes it usable.** Proximity is the only test available for an addition and R1.7 measured it missing **309 of 429** silently; behind a bound, that miss costs **responsiveness rather than correctness**, which is the whole argument for having both. Read the other way round: the wake buys nothing a player could not eventually get from `T` — it buys the difference between *the bypass fills this evening* and *the bypass fills eventually*, and that is a `PLAYER GOVERNS` purchase, not a correctness one.
  >
  > **What the collapse costs, stated rather than absorbed.** One bit cannot say *why* a Habit is stale, so *"this driver did not know about your bypass"* and *"this driver's habit simply aged out"* become one answer where `LEGIBLE CAUSE` would want two. **A second bit recovers it**, and that is recorded here as available rather than taken — the consumer is `01 §5`'s notification surface, which does not exist, and a bit is cheaper to add then than to justify now.
  >
  > **The backstop is checked at use, not swept on a cadence, and this is the load-bearing half.** A rotation's cost is `store size ÷ period`, so **a rotation couples the learning rate to the population**: double the city and the same bill buys half the responsiveness. A Habit Route is stored per **Citizen** ([`CONTEXT.md`](../../CONTEXT.md) → Habit Route, corrected by this session), so the store *is* the population. Checking at Trip start instead bounds the bill by `trips_started × P(stale)` — a quantity R3's tripwire already budgets — and a Habit nobody uses costs nothing to leave stale, which is both cheaper and more honest. **Staleness is therefore measured in journeys, not in wall-clock Ticks.**
  >
  > **R5.5.4's rotation is retained where it was measured and nowhere else.** It rotated the **shared pair-keyed cache**, resident population **412** — a store whose size is a design choice and therefore constant in the city. 0.40 forced refreshes per Tick is affordable there and stays. It was never evidence about the Habit store, which R5.5.4 did not have in front of it and which is four orders of magnitude larger.

  **The two numbers this contract introduces are both unset**, and both are hash-bearing under [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md): the staleness bound `T` and the wake radius `d`. They are entered in [`0002`](../../plans/0002-open-questions.md) §D2 with named ratifiers, and neither is chosen here — the contract is a *shape* and the parameters need a store size and a Trip rate that arrive with Trip generation (`06` 5b).
- **Service dispatch must be assigned, not gradient-descended.** Fire trucks clumping is the visible symptom of the field owning intent, so an assignment step giving each vehicle a specific incident is mandatory rather than a refinement.
- **Trip Fate stays attributable.** Because a failed Trip names a Citizen, an origin, and a destination, it can be reported. Under GlassBox there was no fact of the matter about where an agent had been trying to go, so there was nothing to report. This is what makes `LEGIBLE CAUSE` mechanically possible rather than aspirational.
- **Memory cost is per-Traveller, not per-map-cell**, so it scales with Trips in flight and imposes no cap on map size.

## What would trigger revisiting

- **Nothing about performance.** If per-Traveller intent is expensive, the levers are the ones [`0005`](0005-two-fidelity-tiers.md) already names — sample fewer candidates, decide less often, embody fewer Travellers at once. Moving intent into the world is not on the list, and a proposal to do so should be read as a proposal to abandon this design.
- **A genuinely one-to-many destination class we had not anticipated** — some new service where any qualifying endpoint really will do. That is a new flow field, not a change to this decision.
- **Trip generation arriving** (`06` milestone 5b), which is the first thing that can measure the cache's **hit rate** and therefore whether the amended caching scheme above is worth its complexity at all. Everything in the amendment is a statement about *avoidable loss*; none of it establishes that the cache pays for itself, and no S2 figure can. **If the hit rate comes back low, the amendment stands and the cache does not** — R6.1b has already closed the one other candidate source of collapse, so repetition is the whole of the case.
- **A diverting Traveller turning out to re-search.** S2 R6.3 priced a mid-journey re-search at **861.87% of the Tick budget** at a measured diversion rate, and found the cache would need an **88.5%** hit rate on its worst input to rescue it. If session M answers with re-search rather than with *rejoin the Habit Route*, this ADR's caching bullet becomes the load-bearing part of the design rather than an optimisation, and it should be re-argued at that weight.
- **`P(stale)` at Trip start turning out to be near 1.** This is the **single** trigger that both `T` and `d` fail through, which is a consequence of the collapse above: neither setter recomputes, so neither has a cost of its own — they have a *shared* cost, and it is the drain. The bound rests on most Trips finding their Habit fresh. If the steady-state staleness fraction is high, the drain degenerates into a route computation per Trip start and the bound stops bounding anything. Two independent things can cause it and they want opposite repairs: **`T` too short** (lengthen it), or **`d` too wide, so one ordinary edit marks most of the city** (narrow it, or drop the wake). Measured by S2 **R6.4.3** for the `d` half on the graph alone, and by Trip generation (`06` 5b) for the `T` half. **If neither repair works the store size is the problem**, and the answer is fewer Habits rather than a longer `T`.
- **The reverse index not paying for itself** (S2 **R6.4.3**). The wake needs a *space → routes* index, which R1.7 named as the missing piece and which nothing has ever priced. If its maintenance cost — paid per route **insert**, not per edit, and therefore on the common path rather than the rare one — exceeds what the wake buys, the wake goes and the contract survives on `T` alone. **That is a loss of responsiveness and not of correctness**, which is the property the belt-and-braces shape was chosen for.
