# The region view is the map from far away, and a trajectory names the place it is reported at

**`Observe` has one camera and a hierarchy of places.** The **region view** is a zoom level of the single map rather than a second screen: zoom out far enough and the ground stops being drawn and the Settlement graph is drawn in its place, at true positions. And **every trajectory is reported first at the place its own mechanism makes** — commute-shed failures at the **Settlement**, policy and pooling failures at the **District** — with the drill between them free, because a Settlement *is* a maximal set of Districts.

Guiding concepts: `LEGIBLE CAUSE`, `NO VERDICT`, `HONEST DEGRADATION`, `EMERGENCE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md). Both halves decide what the player is shown; no number refutes either, and the one thing here that *is* measurable — whether a Settlement-level failure is invisible at District level — is a property of a city nobody has run and is named as a revisit trigger rather than claimed as evidence.

## Why

### The region view was described five times and never by the document that owns it

*"A diagram of the commute sheds the city actually has, not a menu of tiles anyone chose"* appears in [`0020`](0020-one-live-world-and-settlements-are-derived.md), [`0085`](0085-nothing-on-this-map-is-far-away-so-a-settlement-is-made-by-a-gap.md), `02 §2.1`, `CONTEXT.md` → Settlement and `00-vision.md`, which lists it among the two outcomes belonging *"in the vision rather than a technical doc"*. `01 §1`'s `Observe` row said *"map overlays and the aggregate panels"* and stopped.

That is not a wording gap. `01` is the document that decides what the player looks at, so a view named everywhere else and nowhere there is a view with **no owner** — and the two halves of this decision are exactly what an owner has to settle and a describer never had to.

### A zoom level rather than a screen, and the reason is SimCity 4's

`adr/0020` has already priced it: *"UI over derived state, so it costs a camera and a stats panel, not a subsystem."* A camera is what a zoom level is.

**A toggled second view is refused because SC4's region screen was a different game state**, and `adr/0020`'s whole decision is that this world is singular and live at all times. Beyond the state question there is a subtler cost: a screen the player switches *to* presents itself as a place-picker, and the one property that makes this diagram honest is that **nobody chose what is on it**. Settlements appear, merge and split as consequences of roads, congestion and gaps. A menu affordance would describe a derived readout as a set of options, which is the same failure `01 §1` refuses when it declines to render the Unplaced Pool as an outstanding-work queue.

Nothing has to be laid out, either. A Settlement's position is its real position, so the diagram is literally what the map looks like from far away, and the transition is continuous rather than a cut.

**Under [`0089`](0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) this stops being a convenience.** The map is 65.5 km across and a 1M city occupies 6.3% of it, so the ground is mostly nothing and **the thing the player navigates by cannot be the ground**. `adr/0089` sized the map by how many Commute Budgets fit across it; this is the view in which that ratio is visible, and the two decisions are the same one seen from the camera.

### `01 §6` chose District when District was the only unit that existed

`01 §6` says *"every trajectory must be expandable by District"*, and it was not a choice over Settlement. At 16.4 km the whole map is one Settlement — `adr/0085` found S2 R1.5 had already measured it, **one Settlement holding all 121 Districts at every Budget rung from 40 Ticks up** — so the alternative had no instances. `adr/0089` makes several possible, and the sentence has to be re-decided rather than re-read.

**The two units answer different questions and `CONTEXT.md` already says which.** A District is *"either player-drawn or automatically derived"*, is *"the boundary within which Goods pool without physical transport"*, and is *"the scope a Policy may be overridden per"*. A Settlement is *"a maximal set of Districts mutually reachable within the Commute Budget"*, **derived, never drawn**.

So the failure mode is concrete: a **Labour mismatch** is produced by travel time, and travel time is what makes a commute shed. Decomposed by District it can read as acceptable in every District while a whole Settlement is starved of workers, because the mismatch is *between* Districts inside one shed and no District contains it. That is `01 §6`'s own hiding-in-aggregates argument — *"aggregates hide exactly that, because it is what aggregates are for"* — one level above where `01 §6` caught it, and it is invisible until a map exists on which two Settlements can.

### One hierarchy, not two axes, and the containment is free

Offering both units everywhere and letting the player slice by either was rejected. It looks like flexibility and costs the thing `01 §6` was written for: a syndrome is *"specific enough to notify on without a magnitude threshold"* precisely because the game knows what pattern to look for, and a player who must first pick the right axis is a player who has to know the diagnosis before they can find it.

The hierarchy needs no mechanism because **a Settlement is defined as a set of Districts**, so the drill is containment rather than a join, and `02 §2.1` already restricts a Settlement to *"a reporting unit only: nothing pools by Settlement and no Rule reads one"* — which is exactly and only what a panel needs. This decision consumes that designation and does not widen it.

### The level is read off the mechanism, which is the same move `01 §6` already makes

`01 §6` derives its sustained-detection **duration** from the mechanism rather than picking it for the interface. The reporting level is the same kind of quantity: it is a property of what produces the failure, not a preference about panels.

| Reported first at | Trajectories | Because |
|---|---|---|
| **Settlement** | Gridlock, Labour mismatch, Retention failure | each is produced by travel time, and a commute shed is what travel time makes |
| **District** | Insolvency, Trade deficit, Quality failure, Capacity failure, Demographic stall, Immiseration | each is produced by something a District bounds — a tax rate, a Goods pool, a service catchment |

Both remain reachable from either end. What the level decides is where the game looks **by default**, which is the only thing deciding whether a failure is found while it is still cheap.

## Consequences

**`01 §1`'s `Observe` row names four surfaces** — the map, the region view it becomes, the overlays over both, and the aggregate panels — and `01 §1` gains the section this ADR is the record of.

**Overlays follow the camera.** At map zoom an overlay tints Cells; at region zoom it tints Settlements, which is the same figure aggregated to the unit being drawn. `01 §7`'s two rules are inherited unchanged: never sharper than the player can act on, never sharper than the simulation underneath. **No new overlay quantity is introduced**, which is what keeps this a camera rather than a subsystem.

**`01 §6`'s spatial axis becomes a hierarchy and its table gains a level column.** The original sentence is corrected in place with the reason it was not a choice, on the standing rule that a decision taken when the alternative had no instances is re-decided rather than defended.

**A Settlement is still a reporting unit and nothing here promotes it.** No Rule reads one, nothing pools by one, and no Policy is scoped to one. Were a Policy ever scoped to a Settlement, the player would be governing a boundary they cannot draw and that moves when a road is laid — which is `01 §5.5`'s *difficulty lives outside the map* violated from the inside.

**Nothing in the build observes any of this.** There is no renderer, no camera, no panel and no Settlement derivation in `Borough.Core` — the union-find over the travel-time matrix that would produce one waits on the matrix. This is design recorded ahead of its milestone, and the slice that builds the region view owns the acceptance test.

## What would trigger revisiting

**A Settlement-level failure proving visible at District level after all.** The load-bearing claim is that some failures hide from District decomposition. If a long run with two or more Settlements shows every Settlement-level syndrome already detectable in at least one of its Districts, the hierarchy is redundant and the level column is noise. **That is measurable, its machine is the first multi-Settlement balance run, and no session may close it.**

**Settlements proving unstable enough to be unreadable.** They merge and split on congestion, so a Settlement that oscillates across the Commute Budget under normal load would make a panel whose subject changed identity while the player read it — a *reporting* unit that cannot be reported at. The repair would be hysteresis, which `adr/0022` already found the extraction frontier needs for the same reason, and not a retreat to District.

**The region view acquiring a verb.** It is a readout. The moment something can be done *to* a Settlement from that view, it becomes a menu of tiles and `adr/0020`'s central property is gone — this is the specific way the SC4 failure would arrive here despite the screen having been refused.

**Zoom failing to carry the transition.** If playtest shows players losing their place when the ground stops being drawn, the answer is a better transition, not a second screen; a screen would trade a legibility problem for a state problem, and `adr/0020` has already priced that trade.
