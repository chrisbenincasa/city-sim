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
- **Service dispatch must be assigned, not gradient-descended.** Fire trucks clumping is the visible symptom of the field owning intent, so an assignment step giving each vehicle a specific incident is mandatory rather than a refinement.
- **Trip Fate stays attributable.** Because a failed Trip names a Citizen, an origin, and a destination, it can be reported. Under GlassBox there was no fact of the matter about where an agent had been trying to go, so there was nothing to report. This is what makes `LEGIBLE CAUSE` mechanically possible rather than aspirational.
- **Memory cost is per-Traveller, not per-map-cell**, so it scales with Trips in flight and imposes no cap on map size.

## What would trigger revisiting

- **Nothing about performance.** If per-Traveller intent is expensive, the levers are the ones [`0005`](0005-two-fidelity-tiers.md) already names — sample fewer candidates, decide less often, embody fewer Travellers at once. Moving intent into the world is not on the list, and a proposal to do so should be read as a proposal to abandon this design.
- **A genuinely one-to-many destination class we had not anticipated** — some new service where any qualifying endpoint really will do. That is a new flow field, not a change to this decision.
