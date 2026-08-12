# Nothing on this map is far away, so a Settlement is made by a gap

**The map stays at 4096² Tiles and it is sized by density alone.** The corner-to-corner figure
[`plans/0002`](../../plans/0002-open-questions.md) ledger #1 gave as its second ground is withdrawn: a
full crossing costs **112–224 Ticks by car**, which is **1.4–2.7% of a Day**, against the **141%** that
entry states. [`0020`](0020-one-live-world-and-settlements-are-derived.md)'s Settlement therefore does
not fragment by **distance** at any Commute Budget that also bounds a Trip — S2 R1.5 measured **one
Settlement holding all 121 Districts at every Budget from 40 Ticks upward**, and a 30-minute Budget is
**171**. What separates Settlements on this map is a **discontinuity**: undeveloped ground, a Severance
barrier, or a mode the Household does not have. The **2048² fallback is struck**.

`SOLVE THE ACTUAL PROBLEM` `EMERGENCE`

## Why

### The number the map was chosen against is out by a factor of fifty to seventy

Ledger #1 closed the map at 4096² on two grounds. The first is density: 268 km² at the 1M floor is
~3,700/km², which is Los Angeles, and the endgame wants sprawl. The second is travel — *"the
three-quarters-of-a-Day corner-to-corner figure is not a defect; it is what makes far Settlements
genuinely separate"* — and its table prices a 4096² crossing at **141% of a Day**.

Both of that entry's tables convert with `02 §1.2`'s **0.5 Tile/Tick**.
[`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md) settled that a
Tick is **10.546875 s of in-world time**, so a 50 km/h vehicle covers **36.6 Tiles/Tick**. Re-deriving
against a 16.384 km side and 5.6889 Ticks per clock minute:

| Crossing | Distance | At 50 km/h | Ticks | Share of a Day |
|---|---|---|---|---|
| One edge, straight | 16.38 km | 19.7 min | **112** | 1.4% |
| Corner to corner, Euclidean | 23.17 km | 27.8 min | **158** | 1.9% |
| Corner to corner, Manhattan | 32.77 km | 39.3 min | **224** | 2.7% |
| Corner to corner, Manhattan, on Arterials at 90 km/h | 32.77 km | 21.8 min | **124** | 1.5% |
| Corner to corner, Manhattan, **on foot at 5 km/h** | 32.77 km | 6 h 33 min | **2,237** | **27.3%** |

The straight-edge row reproduces `0082`'s own 112 Ticks to the digit, which is the check that the rest
of the column is arithmetic rather than a second guess. A real crossing sits between the Euclidean floor
and the Manhattan ceiling because Arterials exist, so **1.9–2.7%** is the honest band and **141%** is out
by 52–73×.

This is not a new measurement. It is `0082`'s number reaching a document `0082` did not visit — the same
shape that ADR names in itself, where *a degree of freedom is spent by the first document that uses it
and nothing announces the spending*. Here nothing announced the **correction** either.

### S2 measured the consequence before anybody could read it

The interesting half is not the arithmetic. It is that the corpus already holds the *outcome*, taken for
an unrelated reason, in [`spike-results`](../spike-results.md) → *S2 R1.5*. That round swept the Commute
Budget to compare `0020`'s union-find against Tarjan, and the column nobody was reading is the Settlement
count:

| Commute Budget | Settlements (union-find) | Settlements (Tarjan SCC) | Largest component |
|---|---|---|---|
| 20 Ticks | 6 | 8 | 90 of 121 |
| 30 Ticks | 2 | 2 | 120 of 121 |
| **40 Ticks and every rung above, to 120** | **1** | **1** | **121 of 121** |

**The mechanism returns more than one Settlement only below about 35 Ticks, which is six clock minutes.**
A Commute Budget that bounds a commute is 30 minutes — 171 Ticks, five times looser — and the sweep never
reached it, because it was sweeping for a different question and 120 Ticks was already deep into the
region where the answer stopped changing.

Two riders, and both narrow the claim rather than widen it. R1.5 ran on S2's **frozen cost basis** — free
flow, no route ever invalidated because a road got busy — so this is a statement about an uncongested
city. And its 121 Districts are a **uniform** synthetic city, so it is the mature contiguous case, which
is exactly the case ledger #1's *"several genuinely separate cities"* was a claim about.

### The Commute Budget cannot both bound a Trip and separate a Settlement

`CONTEXT.md` → Commute Budget gives that number one job: *the maximum acceptable cost of a Trip*, above
which a Trip fails and a Building whose Trips keep failing declines. `0020` gives it a second: the edge
predicate of the District graph whose components are Settlements. On a map whose diameter is a fifth of
the first job's plausible value, **the second job requires a number the first job forbids.** A six-minute
maximum acceptable commute abandons the city.

That is [`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md)'s own
shape recurring one document over: *one number being asked to satisfy two independent constraints.*
`0082` resolved its case by giving the sub-model its own clock. **This one does not resolve that way**,
and the reason is worth stating, because a second threshold is the obvious move and it is wrong: a
Settlement threshold that is not the Commute Budget is a number nobody can read off anything, and it
would delete the property that makes the region view honest — that the diagram shows the commute sheds
the city **actually has**, rather than a partition somebody tuned. `0020`'s *derived, never drawn*
survives only while the two numbers are one number.

So the Budget keeps its one job, and the Settlement stops being a distance object.

### What actually makes a second downtown, and it was in the design already

A connected component does not fragment because a graph is large. It fragments because an **edge is
missing**, and connectivity is transitive — so on a contiguously-developed lattice with any Budget above
the District spacing there is exactly one component, at 4096² or at 40962². **Map size was never the
lever.** Three things break edges, and the design owns all three:

- **Undeveloped ground.** [`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)
  stores an undeveloped Chunk as a null, and `01-player §8 Q3`'s unlock-by-serviceability means the
  early and middle city is a few patches on a large map with nothing between them. There is no road, so
  there is no edge, so there are two Settlements — and they merge when the player builds the road
  between, which is `0020`'s middle row working exactly as written.
- **Severance.** `03 §3.7`'s barrier is an *absent* crossing, which is an absent edge and not a long one.
  Milestone 5a built it, `rulesets/severance.toml` is the rung where it bites, and its own measurement
  found the effect is about **an absolute count of crossings kept** rather than a distance.
- **Mode.** The foot row of the table above is **27.3% of a Day**, twenty times the driving row, and
  `0008` plus `CONTEXT.md` → Commute Budget put both in the same currency with no per-mode weight. A
  Household without a car inhabits a materially smaller map than one with a car, on the same ground.

The third is the one with a hole in it, and it is a hole in a *specification* rather than in code.
`0020` reads the Settlement off *"the travel-time matrix"*, singular and modeless, while
[`0072`](0072-the-mode-mask-is-saved-on-the-arc-and-the-segments-is-derived.md) gives every Arc a **mode
mask** and 5a already computes **per-mode connectivity components**. At 4096² that discrepancy stops
being tidiness: mode is one of only three things left that can produce a Settlement at all, so a modeless
matrix produces the driving answer for everybody and the mechanism loses a third of what remains of it.
The matrix is milestone **5c**'s and does not exist, so this ADR states the requirement on the
specification 5c will build from and settles nothing about the structure —
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) forbids the rest.

### The density argument is untouched, and it is now the whole of the case

Nothing above reaches the first ground. 268 km² at 1M is 3,700/km² whatever a Tick is worth, because
neither term is a duration. 2048² would be 15,000/km² — Paris — which is a different game and not the one
`0022`'s macro-arc or `0020`'s polycentricity describe. So the decision survives and **both of the
reasons it was given have been replaced**: the density ground stands alone where it used to be half of a
pair, and the travel ground is gone.

**The 2048² fallback goes with it.** It was contingent — *"if spike S2 comes back badly"* — and S2 has
come back: the cluster is 8, the path source is chosen, and R8 closed the congestion loop.
[`plans/0010`](../../plans/0010-s2-routing.md) had separately recorded that the fallback was
*under-argued as well as the threshold*, since it *"changes the world in answer to a pacing symptom, in a
design with an explicit home for pacing that is not the map."* Keeping a struck-through escape hatch on a
decision this load-bearing invites a later reader to take it for a live option.

## Consequences

- **`plans/0002` ledger #1's second ground is struck and its two tables are corrected.** The *share of a
  Day* column in both is wrong by the same factor throughout, so it is replaced rather than annotated;
  the density column is correct and stays.
- **`0020`'s appear/merge/split table loses its first row.** *"Jobs cluster somewhere out of range of the
  existing centre"* cannot happen by range on this map and is replaced by *development is discontinuous*.
  The merge row is unaffected and the split row becomes **the only congestion-driven entry**, which
  raises its stakes: it is now the sole mechanism by which a mature contiguous city fragments, and it has
  never been measured, because every S2 round that touched Settlements ran free-flow.
- **`CONTEXT.md` → Settlement is amended** on the same sentence, and gains the mode qualification.
- **The Commute Budget's ratifier is unchanged and its stakes are higher.** It is still a percentile of
  milestone **5b-bis**'s Trip-cost distribution and is still unset. What this ADR adds is that whatever
  that percentile turns out to be, it will not be six minutes, so the Settlement count in an uncongested
  mature city is **1** and any figure quoted from R1.5's tight rungs is a statement about a Budget the
  design will not choose.
- **A row for `0013`, not a correction to it.** Fewer Settlements is not a cost; but *the whole map is
  one commute shed* means the choice model's candidate set is not spatially bounded by Settlement
  membership, and anything that was quietly relying on Settlement as a pruning device has lost it. No
  such consumer exists today — `CONTEXT.md` → Settlement already says *nothing pools by Settlement,
  nothing is budgeted by Settlement, and no Rule reads one* — so this is a prohibition to keep rather
  than a defect to fix.
- **Nothing about the save changes.** Map extent has never been what a save costs
  ([`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md): a 4096² map with a
  city on 5% of it costs what a 1024² map with the same city costs), which is why this decision and
  [`0086`](0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md) do not
  contend even though ledger #1 lists the save header as one of the things it blocks.

## What would trigger revisiting

- **A congested measurement fragmenting a mature city.** This is the one live Settlement generator left
  in a contiguous city and every number above is free-flow. If congestion at a realistic Budget splits
  121 Districts into two or more, the mechanism is healthier than this ADR says and the *split* row
  carries it alone. That is a milestone **5c** measurement — it needs the travel-time matrix on a
  congested cost basis — and it should be taken before anybody concludes Settlements are decorative.
- **The Commute Budget landing far below thirty minutes.** The whole argument is a ratio between the map
  diameter and that number. At 40 Ticks — seven clock minutes — the map fragments, so a Budget in that
  region would restore distance-driven Settlements and reopen this. 5b-bis produces the distribution;
  the percentile chosen off it is what to watch.
- **The Tile ceasing to be ~4 m.** Every metre figure here derives from it, and it is the other half of
  the exchange rate `0082` found had been spent three times independently. A different Tile is a
  different map at the same Tile count.
- **A Settlement acquiring mechanical authority.** `0020` already flags this as the boundary to
  re-examine deliberately. If a Rule ever reads a Settlement, *one Settlement everywhere* stops being a
  reporting curiosity and starts being a mechanism that does nothing, and the second-threshold option
  refused above has to be re-argued on much worse terms.
