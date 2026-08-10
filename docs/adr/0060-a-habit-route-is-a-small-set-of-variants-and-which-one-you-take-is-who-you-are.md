# A Habit Route is a small set of variants, and which one you take is who you are

**Habit formation computes `k` candidate routes for a pair and not one. Which candidate a given Citizen adopts is drawn per-Citizen and deterministically — `hash(world_seed, citizen_id, HabitRouteVariant)` — so two neighbours with the same home and the same job take different roads to it, for ever, with no congestion feedback anywhere in the mechanism.**

**The Citizen stores the *index*, never the routes.** The route itself is served from the shared cache keyed by `(origin node, destination node, variant)`, so the per-Citizen store grows by one small integer and not by `k` routes.

`UNIQUE INDIVIDUALS` `BOUNDED KNOWLEDGE` `EMERGENCE` `SOLVE THE ACTUAL PROBLEM`

## Why

### `adr/0046`'s first row was false and nobody had read it back

[`adr/0046`](0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md) argues that the routing model *is* [`adr/0017`](0017-agents-satisfice-they-never-optimise.md) arriving at the one actor class nobody had applied it to, and it prints the mapping:

| `adr/0017` | `adr/0046` |
|---|---|
| a short, **sticky** list | **Habit** — the route I already take |
| a **known** alternative | **Sight** |
| **substantially** better | **Temperament** |

**The first row does not hold.** A Provider List is *a short list of known options that an actor switches between*; Habit as built is **one** route, and a list of one member cannot be switched — it can only be discarded and recomputed. Every other actor class in this simulation satisfices over several known options. Drivers were handed the optimum and instructed to be sticky about it, which is not satisficing, it is optimising with hysteresis.

So this decision does not add a mechanism. **It makes true a row the corpus already published**, and the burden of proof runs the way `adr/0046` already set it: a router that hands every driver the single best path is the thing that needs defending.

### It is the only structural answer to *the network runs out of routes, not road*

S2 R8 measured **87.25% of a city's traffic on 1% of its carriageway, 90.87% of that carriageway empty, at 13% of holding capacity with capacity confirmed realistic.** [`adr/0047`](0047-routing-never-keys-on-the-district.md) removed the immediate cause — one shortest-path tree per District meant one route per (node, District) pair — and **did not remove the shape**: a single shared cost basis still returns one route per node pair, so every Citizen commuting between the same two places drives the same road.

Concentration is therefore not a congestion phenomenon and cannot be fixed by a congestion response. It is a **degeneracy in route supply**, and the only cure is more routes. Variants supply them with no global knowledge, no live cost field, and no feedback loop — which is what makes this affordable at all.

**A city whose traffic uses 1% of its roads does not look like a city**, and this is the failure a player notices before any other in the movement model.

### Taking a worse route is what the corpus already asked for

The objection is that a variant is, by construction, longer in free-flow terms than the optimum, so adopting one is irrational. `adr/0017` answers it: an actor takes a **known** option that is good enough and switches only when a known alternative is **substantially** better. A second-best route is exactly a known option that is good enough, and a population distributed across `k` of them is `adr/0017`'s model rather than a departure from it.

It is also what people do. Two colleagues living on the same street do not drive the same road to the same office, and neither of them is wrong.

### The learning it buys costs nothing, which is the part that decides it

The alternative route to adaptation is a Habit that **recomputes** when its owner has had enough. That version fails on its own terms: a Habit is computed from a slow-moving cost basis, so recomputing against the same basis returns the **identical route** — the Citizen pays a search to learn nothing and is frustrated again tomorrow. Making it work requires a cost basis that remembers congestion, which is a travel-time matrix with time resolution, which is unbuilt, unsized and open in [`0002`](../../plans/0002-open-questions.md) §B.

With variants there is nothing to recompute. **Frustration advances an index.** The new information is not about the road, it is about the driver — *this one annoys me, I'll try the other one* — which is `BOUNDED KNOWLEDGE` rather than a concession to it, and it is free.

## What is argued here and what is not

| Claim | Type | Where it goes |
|---|---|---|
| A Habit is a set rather than a single route | **arguable** | settled here |
| The Citizen stores an index and not the routes | **arguable** | settled here — it is forced by the store being the population |
| A variant switch needs no congestion-aware cost basis | **arguable** | settled here, and it is the reason this shape was chosen |
| **`k`, the number of variants** | **measurable** | **unset** — [`0002`](../../plans/0002-open-questions.md) §D2. Hash-bearing |
| **Whether variants disperse traffic in a real city** | **measurable** | R8's concentration column re-run over a variant-supplied route set, at `06` **5b**'s demand |
| **What variants cost the route cache's hit rate** | **measurable** | `06` **5b**, with the hit rate itself. The key space multiplies by `k` |
| Whether switching destabilises `03 §3.4`'s loop | **measurable** | R8.5's instrument, static against switching |

## Rejected

**Holding the `k` routes on the Citizen — the literal Provider List.** It is the more faithful reading of `adr/0017`, and it multiplies the largest store in the game. Session M established that a Habit Route is stored per **Citizen**, so *the store is the population*; that is why a rotation over it was refused. Multiplying it by `k` to buy a comparison nobody has asked for is the wrong trade, and the index recovers the whole dynamic. **What it costs is stated rather than hidden:** a Citizen knows it *has* alternatives and cannot compare them, so it can switch but never choose. If a consumer for the comparison appears, the routes are recoverable from the cache by key.

**Drawing the variant per Trip.** That is a fresh coin flip per journey, which makes nobody *the sort of person who takes the back way* and re-synchronises the population in aggregate — `adr/0046`'s own argument against a purely per-decision Temperament, one layer up.

**Supplying diversity from congestion instead.** Every driver reading live cost and spreading out is the live global cost field `adr/0046` priced out, and it produces a herd rather than a distribution.

**Perturbing the cost basis per Citizen** — cheaper than `k` searches, and it is a stream by another name: it makes the route a function of a per-Citizen noise field rather than of a declared candidate set, so nothing can enumerate what the alternatives were, and `LEGIBLE CAUSE` loses the ability to say *this driver takes the second-best route*.

## Consequences

- **The route cache's key gains a variant component** — `(origin node, destination node, variant)` — and the key space multiplies by `k`. [`adr/0012`](0012-routing-intent-lives-in-the-agent.md)'s substantive claim is untouched: the key is a pair of **nodes**, never an agent, and two Citizens on the same pair *and the same variant* still share the computed route. **The hit rate divides by roughly `k`**, and the hit rate is already the single unmeasured quantity routing rests on. This decision makes a measurable number worse by a factor it chooses, which is the honest way to state it.
- **Habit formation costs `k` searches instead of one, and that is the cheap half.** At S2 R6.3's rung, formation is **0.316 ms against diversion's 134.135 ms — 0.24% of routing's bill.** Multiplying the 0.24% is the trade being made. *(Name the rung: 40,000 Travellers, a 7-Day Habit, a District-granular tree, an invented origin-destination draw. The ratio is what carries; the milliseconds do not.)*
- **A new `purpose_tag`, `HabitRouteVariant`**, counter-based like every other draw in the simulation. It is not folded into Temperament's tags: which road you take and how easily you are annoyed are different facts about a person, and sharing a tag correlates them invisibly.
- **`k` is hash-bearing and unset**, and enters [`0002`](../../plans/0002-open-questions.md) §D2 with a named ratifier on the day it is chosen, per [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md). **Look for the derivation before reaching for a value** — a candidate is *the number of genuinely distinct corridors between two places on a grid-plus-Arterials layout*, which is a property of `adr/0014`'s road layout and may be derivable rather than chosen.
- **`k` candidate routes require a `k`-shortest-path source**, which the corpus has never specified. `adr/0047` settled the path *source*; it settled it for one route. The candidates must also be **distinct enough to matter** — `k` paths differing by one Segment supply no diversity — so the source owes a dissimilarity criterion, and that is where most of the real work is.
- **`CONTEXT.md` → Habit Route changes shape**, and *Habit Route Variant* joins it.
- **A slow herd becomes possible where none existed.** Temperament damps the herd at the junction, on the Tick. Nothing yet damps a herd at the *switch* — a population that all becomes frustrated with variant 0 in the same week and all moves to variant 1. The switch threshold therefore needs the same treatment Temperament got, and this is recorded as owed rather than solved.

## What would trigger revisiting

- **The route cache's hit rate coming back low at `06` 5b.** The cache is the only exit `adr/0047` left standing, and this decision spends part of it. If the hit rate is marginal, `k` is the first thing to give back — it is a Ruleset number and reducing it to 1 restores exactly today's behaviour, which is why the mechanism was built to be continuous in `k` rather than switchable.
- **Variants failing to disperse.** If R8's concentration column barely moves under a variant-supplied route set, the degeneracy is upstream of route choice — in the road layout itself — and the answer is `adr/0014`'s, not this one.
- **A `k`-shortest source with no affordable dissimilarity criterion.** If the only affordable candidates are near-duplicates, this buys cache misses and no diversity, and the honest response is to withdraw it rather than to lower the bar for *distinct*.
- **Navigation arriving as a mechanic.** A Policy or technology that gives Citizens network-wide knowledge collapses the variant set toward the optimum, which is a *narrowing* of this decision rather than a reversal — and it is the same retrofit path `adr/0046` already names for a satnav.
