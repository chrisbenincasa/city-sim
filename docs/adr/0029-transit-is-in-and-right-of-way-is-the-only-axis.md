# Transit is in the design, and right-of-way is the only axis

**Transit ships.** [`0008`](0008-walking-is-a-simulated-leg.md) parked it as *"a Leg type that may or may not ever be added"* and `plans/0002` called it the largest single open question in the project. It is now settled in the affirmative, owned by **`Connect` in placement and `Govern` in operation**, and its entire design surface is **one axis: whether a line shares right-of-way with cars.**

*(`0028` is reserved for the difficulty-is-exogenous decision, offered in session four and still unwritten.)*

## What forced it

Not realism, and not the genre. Transit was admitted because **five mechanisms already in the design have a cost and no counter-force**, and transit is the counter-force to all five. That is the argument; the genre convention is a coincidence.

| Mechanism | The cost it already has | What was missing |
|---|---|---|
| Density ([`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md)) | stacking concentrates every Trip on one Access Point | anything that makes a concentrated Access Point an **asset** |
| Destitution (`CONTEXT`) | five exits, one of which reads *"a bus line puts jobs back in range"* | the bus line |
| Parking ([`0009`](0009-parking-is-modelled-supply-never-search.md)) | the shed widens forever | anything on the other side of the pressure |
| Office ([`04 §1`](../04-economy-and-goods.md)) | wants to be central **and** outward-connected | a way to staff a dense core without paving it |
| Settlements ([`0020`](0020-one-live-world-and-settlements-are-derived.md)) | derived from commute range, so congestion **splits** them | the only tool that **merges** two |

The tell is that the corpus was already written as though transit existed. `CONTEXT` → Destitution names a bus line as an exit; `CONTEXT` → Arterial already reads *"highway, **rail**, major boulevard."*

## Right-of-way is the whole design

No mode list, no vehicle catalogue, no unlock ladder. The player chooses a right-of-way per line and everything else is consequence. [`03 §3.7`](../03-agent-architecture.md)'s *one graph with mode masks* carries all three bands without amendment.

| Band | Runs on | Congestion | Paid in | Real name |
|---|---|---|---|---|
| **Shared** | ordinary Street/Arterial edges | suffers **and** contributes | vehicles only | bus |
| **Reserved** | the same Segment, a Lane masked transit-only | immune, and **takes capacity from cars** | **road capacity** | bus lane, tram |
| **Separated** | its own edges, transit mask only | none, either direction | **Land and Materials** | metro, commuter rail |

The reserved band is the interesting one: it is the only lever in the design that makes the car network **worse on purpose**. Take a lane, and if the ridership does not arrive you have simply deleted road capacity — and `Evidence` can name who is now late. `NO VERDICT`

## Vehicle count, never frequency

**The player buys vehicles. Frequency is `vehicles ÷ round-trip time`**, and round-trip time is measured over the same Segments cars use. So a player who buys ten buses and lets the corridor jam gets fewer departures than they paid for — *the buses are stuck in the traffic they were meant to relieve* — and bunching needs no rule. A directly-set frequency would oblige the simulation to conjure vehicles precisely when the network is worst, which is the one moment the number must be honest.

## What it does not need

**No new fidelity tier.** [`0007`](0007-stress-driven-simulation-detail.md)'s ladder governs vehicular movement, and transit vehicles are vehicles. `03 §3.7` made walking permanently Statistical *"reopened only if transit is built, since a stop is a queue with a capacity"* — that trigger has now fired and the answer is that it does not reopen. Transit congestion expresses as **wait time**, wait time is Leg cost, and Leg cost is spent against the Commute Budget like any other travel. Every scarcity stays a gradient: an underfunded line is slow rather than broken, and a full vehicle is the next one rather than a refusal.

**No new object for a stop.** A stopping bus is a Vehicle whose desired speed is zero until tick *T*. A Lane is already a sorted queue with car-following, so the slowdown propagates backward and decays on its own — no damping coefficient, and nothing authored. Because a four-lane road is **four Lanes**, a dwelling bus costs one Lane's capacity, so its disruption scales inversely with road width with no rule saying so.

**No new verb.** Placement is `Connect`, because the output is reachability rather than coverage — a stop is not a destination, it is an **Access Point onto a different network layer**. Operation is `Govern`, per line, exactly as `CONTEXT` → Policy already permits for anything place-attached.

## The condition of admission

**Transit must only pay at density.** A line through sprawl carries nobody and bills the treasury forever. If transit is strictly better than no transit it is a reward rather than a decision, and `NO VERDICT` is violated. This is a balance commitment, and the first thing to check in playtest.

## Consequences

- **Ledger #3 (car ownership) goes live**, having been parked on *"only interesting once transit exists."*
- **Ledger #14b sharpens and downgrades.** A stopped bus does not block a road, so discretionary lane changing is a refinement rather than a prerequisite. The honest residual question is whether traffic distributes across parallel Lanes at all.
- **Dwell time is roadmap work**, and it is dwell time specifically — stops themselves are free.
- **A station is an authored Junction piece** in [`0014`](0014-grid-streets-with-freeform-arterials.md)'s sense, and a transit Access Point subject to the same walkable-reach rule as every other Access Point. Coverage therefore emerges rather than being painted.
- [`06-roadmap.md`](../06-roadmap.md) sequences none of this and must be re-derived.
