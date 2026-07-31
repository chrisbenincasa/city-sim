# Immigration arrives through the gate, and the Outside is a stock

**There is no immigration rate, no arrival scalar, and no attractiveness meter.** A prospective Household evaluates the city using the same choice model residents use, with **the Outside as one ordinary alternative**. Those that accept arrive as **Trips originating at an Outside Connection**, enter the Unplaced Pool, and house themselves or leave. Those that decline are counted and their reasons recorded.

The Outside is not one place. It is **four Hinterlands**, one per map edge, each described in the same units a District exposes, and each a **stock the city draws down**.

## The problem this solves, and the three answers that failed first

Something must decide how many people want to live here. Every obvious answer is a hidden global scalar deciding local outcomes — structurally the RCI meter that [`00-vision.md`](../00-vision.md) pillar 1 exists to forbid, and that the Unplaced Pool already replaced once.

**A rate derived from city state.** Rejected. Whatever it is derived from, it is still one number that causes buildings, and no amount of causal sourcing changes what it *is*.

**A utility anchor per Life Stage, hand-authored.** Rejected, and the reason generalises past this decision. `V_outside[Family] = 4.7` has **no referent**. Neither a designer nor a playtester nor a player can say whether it is too generous, because there is nothing to compare it against. Roughly five such constants would jointly determine the entire growth curve of every game, at the exact point the design is most sensitive, with no causal story behind their values. Everything else in this design degrades gracefully; that would not.

**An anchor that drifts to track the city.** Rejected as circular — self-limiting, but unexplainable to a player and unfalsifiable in testing.

## The fix is units, not mechanism

Author the Outside **in the same fields a District already exposes** — median rent, median wage, service levels, a commute figure — and run it through the identical utility function every resident uses.

```
Outside (north):  rent §620   wage §1,400   schools tier 2   commute 240 Ticks
Northfield:       rent §940   wage §1,650   schools tier 3   commute 310 Ticks
```

Same information content; one of the two is debuggable. *"Is §620 the right rent for the rest of the world"* is a question a designer can answer, a playtester can argue with, and a player can read off a panel. It also means the Outside becomes **a row in the same comparison view as the player's own Districts** — not an intangible, a competitor's listing.

The general rule, which outlives this ADR: **author in domain units, never in utility units.** Any constant that cannot be stated in something the player already sees is a balance hazard.

[`02-simulation-model.md`](../02-simulation-model.md) §5.4 already required the hook, for an unrelated reason — *"'everything available is terrible, nobody moves in' requires an explicit stay-put / no-choice alternative with its own utility."* The Hinterland is that alternative, made physical.

## Why arrivals are Trips

Because everything else here is, and because it buys four things at once:

- **Immigration becomes located.** *Which* Outside Connection, and how well it is connected, matters. Arrivals are vehicles entering at a specific gate and contributing real congestion.
- **The ceiling stops being a config value.** The gate has throughput. Immigration is bounded by infrastructure the player built and can see — the same *serviceability* shape already preferred elsewhere in the design.
- **Interest and throughput separate cleanly.** Interest is emergent from utility; throughput is physical. When interest exceeds throughput the player sees a queue at the gate, which is the diagnosis *"your city is more attractive than your connections can absorb."*
- **Departure becomes symmetric.** People leave the way they came, through a gate, as a Trip.

**The bright line that keeps this from being RCI in a costume, and it is checkable in review:** RCI was a number that caused *buildings*. This is a comparison that causes *people*, and every one of them then makes an Individual Decision under [`0005`](0005-two-fidelity-tiers.md) and [`0017`](0017-agents-satisfice-they-never-optimise.md). **No Lot ever reads an aggregate demand figure.**

Lineage is RollerCoaster Tycoon rather than any city builder: park rating is not a demand meter, it is an arrival rate, and every arrival is an individual who then decides for itself.

## The Hinterland is a stock, and the city takes the most willing first

A Hinterland is never Ticked and never rendered — it is a small configuration, not a simulated place, and specifically **not** the frozen neighbour that [`0020`](0020-one-live-world-and-settlements-are-derived.md) rejected as a second clock. It holds a population with composition, a rent, and a wage.

Drawing it down does two things for **one** reason:

- The **rate** falls, because the willing are taken first.
- The **mix shifts** toward the Life Stages that weight cheapness hardest, because those are who remain.

Neither needs a rule. Departures refill the Hinterland they leave for, closing conservation, and it recovers slowly on its own.

This is the **third instance of the same pattern**, after Land and Woodland in [`0022`](0022-land-is-a-stock-the-city-spends.md). That the shape keeps arriving from unrelated directions is the strongest available evidence it is the right one.

**There is no population ceiling.** Drawdown is a gradient: exceed the recovery rate and you are spending the stock, reported in the same words as Timber — *"arrivals 120/Day, northern recovery 45/Day."* What runs out is *cheap* immigration, never immigration. `HONEST DEGRADATION`

## Why four, and not one

A single hidden anchor has no referent. **Four comparable markets are each other's referent** — a player may never know what "correct" is, but can see the northern market cheapening while the east stays expensive, and act on it. That is §5.4's own principle (*only utility differences matter*) applied to the interface rather than the maths.

One Hinterland **per edge**, shared by every Outside Connection on it — not one per Connection, or six roads on the north edge would drain six independent Norths. This makes Outside Connection placement a real decision: another road on the same edge buys **throughput into a market you are already draining**; a port on the far edge buys **a different economy**, at the cost of longer hauls.

## What this decides about the shape of a game

| Phase | New residents come from | What it forces |
|---|---|---|
| **Early** | Hinterlands — cheap, willing | nothing; any city works |
| **Mid** | Hinterlands, draining and pricier | diversify connections, or compete on quality |
| **Late** | **the city's own children** | house Families, or stop growing |

**Growth changes source; it never stops.** Extraction gives way to cultivation. This is what makes a balanced density mix *structurally necessary* rather than merely encouraged — see [`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md) — and it is why dwelling-size preference by Life Stage can stay a soft utility term rather than a hard filter. The wall was never there; it was here.

**The transition must be announced, and it is announced as a crossover, not a threshold**: the Day the share of new Households born here exceeds the share arriving from outside. Two curves meeting is a fact nobody chose. Every threshold alert in every city sim is somebody's guess.

The negative example is precise. SimCity 4's *Peanut Butter Point* — growth stalling mid-game with no visible cause — could not be surfaced, because **nothing true was happening**: it was a demand cap, and the community's counterplay was to place parks to raise it, an out-of-world fact learned from a forum. Here the stall is a stock being drawn down, and the counterplay is legible in the thing that is draining.

## Consequences

- **Outside Connections are promoted from logistics to demography.** Their number, placement, and capacity now decide who can arrive at all. This raises `plans/0002` question 3 from a convenience to endgame infrastructure.
- **Goods prices, wages, and rents outside are one object per edge**, not two systems that could contradict each other. This absorbs the open question about whether Outside Connection prices should drift.
- **Sorting is generalised.** [`0010`](0010-one-clock-and-demographics-by-sorting.md) frames it as a schools mechanism; it is what the choice model does whenever the Outside is an alternative. Schools are one term among many, and that ADR's framing needs narrowing.
- **Per-Life-Stage coefficient vectors become required data.** §5.8 already commits to authoring coefficients directly rather than estimating them, so this is more of the same rather than new apparatus. An Empty Nest Household weighting job access at zero is what lets retirement Settlements emerge in parts of the map that never urbanised.
- **Map size acquires a second job.** It no longer has to pace the game, but it now sets how many distinct outside economies exist. Four usable edges and one usable edge are different games.
- **Rejected arrivals must be counted with reasons.** *"1,847 considered the city; 1,610 declined — 1,204 priced out, 302 no school in reach."* Without this the anchor is felt rather than observed, and the whole legibility argument collapses.

## What would trigger revisiting

- **Playtesting showing players never notice the Hinterlands at all**, treating arrivals as weather. That is a UI failure first; diagnose it in that order, as [`0022`](0022-land-is-a-stock-the-city-spends.md) requires for composed fertility.
- **The four markets proving indistinguishable in play** — if a player only ever meaningfully connects to one edge, four Hinterlands is bookkeeping and one would do.
- **Arrival Trips proving a material cost at scale.** The mitigation is fewer, larger arrival events, not a return to a scalar.
