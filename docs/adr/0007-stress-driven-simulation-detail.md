# Simulation detail follows network stress, not the camera

**Road segments are microscopically simulated when they are under stress, and statistically resolved when they are not. The trigger is simulation state — volume over capacity, plus a static junction-complexity term — never camera position.** A rotating audit microscopically simulates a small deterministic sample of unstressed segments to catch failures the trigger cannot anticipate.

Fidelity is therefore a property of **place**, not of **person**. A Traveller is microscopically simulated while on a stressed segment and time-advanced while on an unstressed one.

## Why

We considered three positions and rejected two.

**Microscopic everywhere** is the honest option and it is what Citybound attempted, reaching roughly 400,000 vehicles on a single core. But vehicle count is then the binding constraint on city size, and we have a hard requirement that a 1M-citizen metropolis be reachable **with an honest population count** — no multipliers, no scale factors, no fudging after a threshold. SimCity 2013 displaying a population that was a multiplier over simulated agents is the failure we are most determined not to repeat. Full microscopic simulation is the option that would eventually force it.

**Macroscopic everywhere, with vehicles as animation** scales fine, but makes the volume-delay function the single point of failure for the entire game's credibility. That is disqualifying, because VDFs are worst exactly where they matter most: the standard BPR form is accurate below capacity and diverges from reality above it, since oversaturated traffic exhibits queueing, spillback, and hysteresis that a static function of current volume structurally cannot represent. The system would be least trustworthy at the congested junction the player is staring at while trying to diagnose it.

**Camera-driven detail** — promoting whatever the player is looking at — makes observation change outcomes. Watch a jam and it gets slower; look away and it speeds up. This is the Bannerlord autoresolve problem, where players learned that fought and auto-resolved battles gave materially different results, turning the fidelity boundary into an exploitable mechanic.

**Stress-driven detail avoids all three.** The reasoning that makes it work:

- **It puts the VDF where it is strong.** Unstressed segments are free-flow, where travel time is `distance / speed` — not an approximation but an exact answer. The saturated regime, where VDFs fail, is handled by actual simulation.
- **The visualisation gap closes by itself.** A gap can only exist where behaviour is complex, and behaviour is only complex where there is congestion — which is exactly what gets simulated. On an empty street a car driving at the speed limit is trivially correct. Detail arrives where scrutiny does, without the camera being consulted.
- **The trigger consumes a count, not a model.** `in_flight` is exact — incremented on departure, decremented on arrival. Capacity is static. We are not using the VDF to decide where the VDF can be trusted.
- **It is self-correcting in the dangerous direction.** Route choice does introduce circularity, since attribution of volume to segments depends on travel times. But if the VDF *underestimates* congestion, routing over-uses the segment, volume rises, and the segment crosses the threshold into microscopic simulation, which finds the truth. If it *overestimates*, traffic diverts and raises volume elsewhere, triggering detection there. Both errors push toward detection.
- **It scales.** Only so many segments can be stressed at once, so the microscopic budget is bounded by network stress rather than by population.

## Consequences

- **The per-citizen promotion pool, eviction policy, and per-citizen fidelity budget are all deleted.** There is no question of which Citizen gets promoted; there is only which segment is stressed. This removes a substantial amount of machinery that three earlier drafts assumed.
- **Simulation fidelity and rendering are fully independent.** Rendering may be camera-driven because it cannot affect simulation state. A vehicle on an unstressed segment is animated from its trip; a vehicle on a stressed segment off-screen is simulated but not drawn.
- **Segments need hysteresis** — promote at one threshold, demote at a lower one — or they flicker as volume oscillates.
- **A trip spans both regimes**, transitioning at segment boundaries. This is the promotion/demotion machinery, rekeyed from person to place.
- **Known blind spot: junctions that fail at low volume** because of turning conflicts. V/C reports them as fine. Mitigated by a static complexity term in the trigger and by the rotating audit, but not eliminated — this is coupled to the deferred turning-movement work in `deferred.md` more tightly than that document suggests.
- **Total gridlock exhausts the budget** and falls back to the VDF everywhere, in the regime where it is least trustworthy. This is a city that has already failed, and `HONEST DEGRADATION` requires that we say so rather than hide it.
- **Walking is largely exempt.** Pedestrian ways essentially never stress, so walk Legs resolve statistically almost always — which is what makes [`0008`](0008-walking-is-a-simulated-leg.md) affordable at metropolis scale.
- **A fixed divergence tolerance is asserted in tests.** Where a segment is microscopically simulated, observed travel times must match VDF predictions within tolerance. Exceeding it is a bug report about the VDF, not something to widen the tolerance for. Widening it incrementally is how this design would slide into the failure mode it was built to avoid.

## What would trigger revisiting

Three things, in rough order of likelihood:

- **The rotating Audit keeps finding divergence on unstressed segments.** That would mean stress is the wrong trigger rather than a mis-tuned one — most plausibly because junction failure at low volume is more common than the static complexity term can capture. The response is a better trigger, not a wider tolerance.
- **Microscopic segments cannot be made contiguous cheaply.** Jams that back up across a boundary should extend the Microscopic region; if spillback does not raise the neighbour's volume enough to promote it naturally, the boundary becomes visible as an artefact and the model needs an explicit region-growing rule.
- **Hardware moves far enough that microscopic-everywhere becomes affordable at a million Citizens.** Citybound reached ~400,000 vehicles on a single core, and the gap is not enormous. This would be a welcome reversal, and it would simplify rather than complicate — one regime, no trigger, no Audit, no divergence tolerance.

What must **not** trigger revisiting is a desire to make the visible traffic prettier off-camera. That is camera-driven detail wearing a disguise, and it is rejected above for reasons that do not weaken with familiarity.
