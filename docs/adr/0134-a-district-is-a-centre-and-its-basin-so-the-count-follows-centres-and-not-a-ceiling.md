# A District is a centre and its basin, so the count follows centres and not a ceiling

**A District is a concentration of activity together with the ground that drains to it.** It is derived
by a watershed over a Building-density field on the Cell grid, clipped to a road component, seeded only
at concentrations whose **prominence** clears a threshold. **The pooling extent stops being a ceiling.**
Nothing forces a split; a District appears when a *second centre* does, and a large one is bounded
economically by [`adr/0133`](0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md)'s
haulage charge rather than geometrically by a radius.

**A District is therefore a saved entity and not a derived label** — its extent is *updated* by a damped
process, never recomputed from scratch, because the three mechanisms that make it stable all carry
history.

`EMERGENCE` `LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM`

## Why

### The trade is irreducible, and naming it is most of the decision

**No algorithm can find a meaningful boundary in a featureless city, because there is not one to find.**
A monocentric city — one dense core fading to a sparse edge, which is what every world this build can
currently generate produces — has no natural internal line. That is a fact about the geometry and not a
weakness of any candidate. It forces a choice:

| | Count | Boundaries |
|---|---|---|
| **A hard radius** | Bounded, so Districts exist early | **Sometimes arbitrary** — a large monocentric city *must* split, and the line falls somewhere on a featureless ring |
| **⭐ Centres** | Follows the city's own structure — one until there are genuinely two | **Always meaningful**, because a boundary only exists where the field says two concentrations meet |

**You cannot have both**, and the second is chosen. A boundary decides whether a movement is free or is a
truck, and `adr/0013` requires that *"the player must be able to see boundaries, understand that pooling
is what they mean, and be told when a Shipment exists because of one."* ***A straight line through a
neighbourhood is a boundary no explanation can be attached to***, and shipping one knowing it is wrong
spends the pillar the project is built on to buy an earlier Shipment.

### This is what `CONTEXT.md` already says, read without the ceiling

*"The count is **physics rather than a design choice**: the early city has one District because the city
*is* one neighbourhood, and more appear as it outgrows the pooling radius."* The first half is this
decision exactly. **The second half is the sentence that smuggled the ceiling in** — it reads as *a
District splits when it gets big*, and what actually happens when a city outgrows a neighbourhood is that
it **grows a second centre**. ⚠ *"Typically hundreds of Cells"* and the **128-Cell working anchor** were
never a derivation and say so: *"a starting point rather than a derivation"*, with *"what actually pools
convincingly is a playtesting question."* ***An anchor nobody derived, wired to a mechanism as a hard
bound, is how an arbitrary number acquires the authority to force an arbitrary line.***

### Losing early Shipments is correct rather than a cost

Under this decision a small city has **one District and no inter-District freight**, and that is what a
small city is: a high street, not a logistics network. Districts, and therefore Shipments, arrive when the
city is polycentric — which is **milestone 15**'s agglomeration, the mechanism that produces second
centres. `adr/0013`'s payoff, *"this District cannot feed itself and the trucks that fix that are clogging
your one arterial"*, arrives at the size where a player could act on it.

🔴 ⚠ **This is emphatically NOT `adr/0013`'s rejected *pool everything, city-wide*.** That option was
refused because *"it deletes geography with it: if Goods teleport across the map, industrial siting stops
mattering."* The difference is what makes a second District: **a candidate that split only on road
disconnection was considered and rejected here for exactly that reason** — a connected city would be one
District for ever, which is the rejected option wearing a derivation. Splitting on **economic geography**
produces real Districts in any mature city, and the haulage charge means distance costs money even inside
one. Geography is priced at every scale rather than deleted below one.

### The bound moves from geometry to economics, and `adr/0022` is better served

`adr/0022` warns that the design *"silently collapses"* if Shipment cost is ever tuned toward zero, and a
radius prevented that by forbidding large Districts. `adr/0133`'s charge scales with extent, so a large
District's internal carriage costs real money and the abstraction stops being free exactly where it stops
being defensible. ⚠ **The charge is therefore STRUCTURAL under this ADR and not the candidate `adr/0133`
left it as** — with no geometric ceiling, it is the only thing standing between a sprawling city and the
collapse `adr/0022` names. ***A floor on a charge is also a far easier invariant to assert than a ceiling
on an extent***, because it is one number in one place rather than a property of every boundary.

And it drives itself: expensive internal carriage pressures a sprawling single-centre city toward
polycentricity; polycentricity creates Districts; Districts create Shipments. `EMERGENCE`.

### The instability is solved by three known mechanisms, and the corpus already argues for the third

A watershed's failure mode is flicker — a small change flips a ridge Cell, or a new local maximum
re-labels a whole basin.

- **Persistence filtering on seeds.** A concentration becomes a District only when its **prominence** —
  its height above the saddle joining it to a higher concentration — clears a threshold. This kills noise
  maxima, and it is the literal mechanism for *"a genuinely distinct centre"*. ***The thing that makes the
  count meaningful is the same thing that makes it stable***, which is the sign the cut is in the right
  place.
- **Hysteresis on membership.** A Cell changes District only when the field difference clears a band,
  never on a tie.
- **Damping the cadence.** Re-evaluation is slow and a boundary migrates by at most a bounded number of
  Cells per evaluation, so it never jumps. [`04 §4`](../04-economy-and-goods.md) already makes this
  argument for prices — *"an undamped price signal produces the same oscillation pathology as undamped
  congestion feedback"* — and it transfers to a boundary unchanged.

## Rejected

**An anchored tiling clipped to road components.** Stable by construction, cheap, and it would have given
Districts at milestone 12. Rejected because its boundaries are straight lines that cut through
neighbourhoods, which is precisely the boundary `adr/0013` requires the player be able to understand.
***Shipping a mechanism known to be wrong, to buy an earlier demonstration of a different mechanism, is
paying in the pillar to buy a milestone.***

**Splitting only where the road graph disconnects.** Considered and rejected above: it is `adr/0013`'s
*pool everything, city-wide* reached through a derivation.

**Overlapping per-Building pooling balls.** Removes boundaries entirely and so removes every problem in
this ADR. Refused on the corpus's own terms: there is no Bin, so *"the Pool is just a Bin per Good per
District"* fails, `04 §4`'s per-District price has nothing to attach to, and `Scope.Pool` has nothing to
resolve to.

## Consequences

- 🔴 **A District is `(saved AND hashed)`, which reverses [`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md)
  decision 2's expectation.** Persistence, hysteresis and damping all consult the previous state, so
  extent is **not** a pure function of a world snapshot and cannot be rebuilt on load. `DistrictTable`
  and `DistrictId` become real, a District is created and destroyed like any entity, and
  `DerivedRebuildAuditTests` does not apply to it. ⚠ **Determinism is unaffected** — it is still a
  function of the Input Log, so replay and save/reload equivalence both hold.
- 🔴 **Four hash-bearing numbers arrive unset and owe `plans/0002` §D2 rows**
  ([`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)): the
  **prominence threshold**, the **hysteresis band**, the **re-evaluation cadence with its per-evaluation
  Cell bound**, and the **extent scale in `adr/0133`'s charge**. ⚠ **None is tunable on a world that
  exists** — the density field is flat on every shipped Ruleset — so all four wait on a city with
  texture, which is milestone **15**.
- **`CONTEXT.md` → District's extent paragraph is amended.** The 128-Cell anchor stops being a maximum and
  becomes the scale at which the haulage charge bites — ***a curve's parameter rather than a ceiling***,
  which is a different ratification obligation and not a softer one.
- **Milestone 12 ships one District on every current world**, and `Scope.Pool` resolves through it. ⚠ **A
  Ruleset with two separated settlements is what demonstrates a second District — ✅ **shipped 2026-08-22 as `rulesets/twinned.toml`, and the key is `[[lattice]]` rather than `[[settlement]]` because a Settlement is derived and these two are one wherever anybody drives across the gap (`CONTEXT.md` → Lattice)** — and a real inter-District
  Shipment**, and `06`'s Shipments row at 12 is unobservable without one. ***That is milestone 9's land
  value repeating*** — a producer built, correct and with nothing to look at — and naming it here is the
  cheapest place to stop it.
- **`adr/0133`'s haulage charge is promoted from candidate to structural.** Its *"whether the charge ships
  at 12"* sub-question is narrowed: with no geometric bound, something must bound extent, and this is it.
  Its **payee blocker therefore blocks more than it did** — see that ADR's Consequences.
- **The watershed reads a Building-density field over Cells**, which is machinery the build already has:
  ~~`LayerCellTable`, `LayerDiffusion` and `SeparableKernel`~~ 🔴 ⚠ **AMENDED 2026-08-22 by milestone 12
  task 2: it is `BuildingResidency`, and none of those three.** The field is a **count** and was already
  built — 5b-bis cached a per-Cell list length for `adr/0081`'s job search, sized against
  `CellGrid.WorldCellCount`, maintained at the write site and rebuilt with its index — so it is exact at
  every Tick, free to read and carries no schedule. **The three names above are the machinery a
  *smoothed* field would use, and no smoothing shipped**: a kernel means a radius, which would be a fifth
  hash-bearing number where the bullet below enumerates four, and measured on the worlds that exist the
  field is **flat rather than noisy** — a Cell inside a lattice holds exactly ten Buildings — so there is
  nothing for a kernel to do (`plans/0037` **F7**, **F8**). ***This ADR was right about the mechanism and
  wrong about where to look***, which is [`adr/0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
  working as written. **It is not a Map Layer** and must not acquire
  a cadence in `[layers]` by resemblance — its cadence is this ADR's, and a Map Layer's is `adr/0044`'s.

## What would trigger revisiting

- **A mature city that stays stubbornly monocentric.** If agglomeration never produces a second centre at
  the target population, this design yields one District for ever and *is* the rejected option after all.
  The honest response then is not a radius — it is to ask why the city has no second centre.
- **The prominence threshold turning out to be the whole mechanism.** If the count is dominated by that
  one number rather than by the city's shape, *"the count is physics"* is false and the ceiling was merely
  renamed.
- **Boundary migration being visible and disliked.** Damping makes a boundary move slowly rather than not
  at all; if a slowly-crawling District boundary reads worse to a player than a fixed arbitrary one, the
  trade above was scored wrong and this needs re-taking with that evidence.
- **The haulage charge failing to bound extent in practice.** It is now the only bound. If a city sprawls
  into one enormous District and the charge does not bite, `adr/0022`'s collapse is live and a geometric
  backstop returns — as a backstop, not as the mechanism.
