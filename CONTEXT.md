# CONTEXT — Domain Language

This file defines the vocabulary of the project. Every design document, every ADR, and every identifier in the code uses these terms with exactly these meanings. If a concept needs a name that isn't here, add it here first.

Terms are grouped by the layer they belong to. Where a term is borrowed from a prior game, that lineage is noted — it makes the reference material searchable.

**This file is the vocabulary of the city. [`PROCESS.md`](PROCESS.md) is the vocabulary of the project** — slice, spike, gate, session, milestone. Nothing about how work is organised belongs here. Two words appear in both registers and mean different things in each, **Slice** and **Phase**; `PROCESS.md` says how to tell them apart, and the Tick-phase meaning of *Phase* is the one defined below.

---

## Guiding concepts

A small controlled vocabulary. Every significant design decision should reference at least one of these tags. A decision that references none is a decision without a justification, and that is exactly what this list exists to expose.

| Tag | Meaning |
|---|---|
| `EMERGENCE` | Complex behaviour arises from simple local interactions. We do not script outcomes we could let the simulation produce. |
| `LEGIBLE CAUSE` | Every effect the player observes can be traced to its cause through the UI. If it can't be explained, it shouldn't be simulated. |
| `UNIQUE INDIVIDUALS` | Citizens are persistent, identifiable people — always, at every fidelity tier. Never anonymous statistics wearing a face. |
| `BOUNDED KNOWLEDGE` | Simulated actors have partial information and satisfice. They never optimise globally. This is both cheaper and more realistic. |
| `SOLVE THE ACTUAL PROBLEM` | Model the real mechanism, not a special case of it — but only where the mechanism is something the player can perceive or act on. |
| `HONEST DEGRADATION` | When the simulation hits a limit, it says so plainly. Nothing fails silently. |
| `PLAYER GOVERNS` | The player zones, connects, funds, and regulates. The city decides what actually gets built. |
| `NO VERDICT` | A mechanism must admit more than one outcome. If a system can only ever produce one lesson, it is an argument wearing a simulation's clothes — and the simulation has stopped modelling and started editorialising. The test is not whether an outcome is bleak; it is whether the player could have caused a different one. |
| `FAST ITERATION` | Tuning the simulation must take seconds. A rule that requires a rebuild to change is a rule that will never be balanced. |

---

## World and space

**Tile**
The atomic unit of ground. Integer coordinates. Carries terrain type, zone designation, and ownership. Everything spatial ultimately resolves to tiles.

**Cell**
The resolution at which the city's environment varies: a 32×32 block of Tiles (≈128 m), and the storage unit of every Map Layer.

The Cell is a **design constant and is never available for tuning.** Its size *is* the resolution of pollution, which feeds Fertility, Desirability and therefore the choice model — so changing it changes the State Hash. It was split from the Chunk, the technical partition it used to share a number with, precisely because that partition *is* tunable and this one is not. See `docs/adr/0034-fields-are-sorted-by-source-geometry.md`.
_Avoid_: Chunk (a purely technical partition, defined in `docs/05-technical-architecture.md` §5), grid square, layer cell.

**District**
A contiguous named region, either player-drawn or automatically derived. A District is the boundary within which Goods pool without physical transport, and the scope a Policy may be overridden per. Typically hundreds of Cells.

**It is not the granularity of the travel-time matrix, and it is not where routing happens** (`adr/0047`). That role was welded on, sized by a Goods playtest, and is now the routing partition's — so redrawing a boundary changes what pools, never what a Traveller drives.

**A contiguous set of Cells, never of Chunks.** The Cell is frozen and the Chunk is tunable (`05 §4` lists Chunk size as hash-preserving), so boundaries made of Chunks would let a profiler move what a District *is* — and District extent decides Goods pooling, which is a change to the city. (It no longer decides matrix granularity; `adr/0047` removed that role, and the argument stands on pooling alone.) Cell alignment costs nothing, the Cell being a strict divisor of the Chunk.

**Its maximum extent is bounded by the pooling abstraction's own validity** (`02 §2.1`): a District can only be as large as the area within which *ignoring transport* is a defensible simplification, because a District large enough to span a genuine delivery has deleted the Shipment that delivery should have been — the collapse `adr/0022` warns of. **Working anchor: 128 Cells — 2.10 km², ~1.45 km across.** A starting point rather than a derivation: *what actually pools convincingly is a playtesting question*, and the number should be expected to move once there is a city to feel it in.

The count is therefore **physics rather than a design choice**: the early city has one District because the city *is* one neighbourhood, and more appear as it outgrows the pooling radius.

**Zone**
A **permission set over land**: it lists the uses allowed there and forbids every other. A Zone never places a Building and never causes one — zoning Residential does not build houses, it forbids everything that is not housing. Density is the intensity cap *within* a permission, not a separate concept. Mixed use needs no machinery: it is a permission set with more than one entry.

**Nothing is permitted by default. Unzoned land builds nothing** — including farms and forestry. Extraction-by-default was considered and rejected: it is more elegant, and it would mean a player's food supply arranged itself before they knew they had one. The first ten minutes teach by *doing*, and a chain the player never built is a chain they cannot be taught to diagnose. It would also make a seed's fertility decide whether a city survives its opening, which is variance in the one place the design cannot afford it.

Zone families follow the Goods chain rather than a tradition. Industry splits by **what determines its location**, which is the real difference:

| Family | Location dictated by |
|---|---|
| Residential, Commercial | the market — bid price on a Lot |
| **Industry — Extraction** | the **ground**: Fertility, Woodland. Not a market decision at all |
| **Industry — Processing** | **reachability** of inputs and buyers |

Agriculture is not a special case needing its own verb; it is Extraction, and it needs *protection* rather than placement. A farm can never outbid housing, so an agricultural Zone is the player overriding an auction farms always lose — which is the counter-force `adr/0022` flagged as missing, and what a greenbelt actually is.

**Nobody paints nuisance.** Dirty industry is not a band the player selects; it is where Materials come from, and Materials gate all construction — so **pollution is the price of growth and importing is the price of clean air**, with no configuration avoiding both. The player's levers are where the inputs are, what they regulate, and what bill they accept.

**Amenity**
The count of distinct Business types reachable **on foot**, entering the residential choice utility with diminishing returns. One of the three agglomeration forces, and the one that rewards mixed use.

**Walkable** is the load-bearing word. It is what `adr/0008` promised and had not yet collected — *"a corner shop is viable because people can physically reach it on foot"* — and it is what makes a centre of offices, shops, and homes outperform a centre of offices alone without any rule saying so.

**Lot**
A developable parcel with road frontage, carved out of a Zone. A Lot is either vacant or holds exactly one Building. Lots are generated by subdividing zoned land against the Street network; they are not painted directly by the player.

**Frontage**
The contact between a Lot and a Street it can take access from — the geometric precondition for a Lot existing at all. The subdivider carves zoned land against the Street network, and land it cannot give frontage stays unlotted and undevelopable. **Only Streets grant frontage.** Arterials carry none and have no Access Point, so nothing zones onto one (`adr/0014`) — which is why Arterial corridors need something to be made of.

**Frontage is arithmetic, not a rule.** Block geometry decides how much land is in a parcel and whether it touches a street at all; that is a physical consequence of what the player drew, and `adr/0025` keeps it for precisely the reason it rejects a road-derived density cap. A player who draws sparse streets is not refused — they get dead block interiors, and the interior explains itself. `LEGIBLE CAUSE`

**It is also a stock the player spends.** Subdividing consumes frontage — narrow terraced Lots eat the available street edge to buy one Access Point each — where stacking preserves it and funnels every Trip through a point. That is one half of the density trade above.

**"No frontage" is an `Evidence` answer**, one of the four reasons a Lot is vacant, alongside no Household in the Unplaced Pool that would accept it, conditions below tolerance, and no capital. And because every Lot has frontage by construction, every Building is on the Road Graph — which is what lets Utilities ride it with no second network to draw.

**Frontage is built as of 5a-bis, and it is `(derived AND rebuilt)` rather than saved** ([`docs/adr/0078`](docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md)). A Lot no more stores its frontage than an Arc stores its cost, because both are functions of the Segments — and the edit a stored copy would not reach is the player bulldozing the Street. The rebuild runs on the **Epoch**, which makes this the first consumer of `adr/0012`'s invalidation contract outside routing.

⚠ **The clause above is *by construction* and not *for ever*, and the distinction only became reachable when the player could edit roads.** A Lot is only ever **created** with frontage; the Street it fronts can be bulldozed afterwards. A **vacant** Lot that loses its frontage is deleted and becomes land again; an **occupied** one is kept, its Building stands with its Occupants, and its Access Point becomes `Address.None` — a named absence rather than a stale handle, which milestone 5b reads and reports as *no route found* ([`docs/adr/0079`](docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)). So the checkable invariant is ***every vacant Lot has frontage***, never *every Lot*: the stronger form fails on correct behaviour, and an invariant that does that is the one somebody disables to ship.

**Density**
How much Building a Lot may carry. The player sets a **ceiling**, never a floor — zoning for density is permission, not instruction, and a high band on land nothing wants to build on grows nothing. `PLAYER GOVERNS`

Density is **capacity, not quality.** A high-density slum and a high-density tower are the same band; what separates them is who lives there and what they pay, which the choice model and the price system already produce. The two are independent axes and must never be collapsed into one ladder — every reference game keeps them separate, and the alternative makes density a thing the player graduates to rather than a thing the player chooses between.

Density is the lever that **trades Land for Materials**, which is what makes it a strategic decision rather than a preference. Sprawl spends Land, which is finite and sealed near-permanently; height spends Materials, which run out locally and are then imported forever (`adr/0022`). The right band therefore depends on which stock the city is short of, and that changes over one playthrough. It is also how a city with no unsealed ground keeps growing: building on already-sealed land costs no Land, because the Land is already spent.

There are **two physically distinct routes to the same residents per hectare**, and they are separate bands rather than points on one scale:

| Route | Shape | Access Points | Cost |
|---|---|---|---|
| **Subdivide** | many small Buildings, narrow Lots — terraces, row housing | one per Building — traffic stays distributed | consumes frontage |
| **Stack** | one Building, many Occupants — apartment blocks | **one, shared** — traffic concentrates | funnels everything through a point |

Because a Lot holds exactly one Building and a Building has one vehicle and one pedestrian Access Point, **stacking concentrates Trips at a point** — twenty Households in one block are one Access Point and a single `Parking Shed` query, where twenty terraces are twenty. Stacking earns that cost back in logistics: one Building is one set of Bins, so a single delivery serves twenty Households. Access Points per capita is the axis that genuinely separates the two, which is what makes a middle band mean something instead of interpolating.

Nothing needs to grant permission for height based on road capacity — a tower behind a cul-de-sac strangles itself through Segment Stress and the Commute Budget, and says why.

**Buildings do not shrink.** The density ladder is walked at construction only; decline drains occupancy and quality, and the band is re-tested when a Lot redevelops.

**Settlement**
A maximal set of Districts mutually reachable within the Commute Budget. **Derived, never drawn** — a connected component of the District graph, recomputed when the travel-time matrix rebuilds.

Settlements appear when jobs cluster out of range of the existing centre, **merge** when a new road brings two inside the Budget of each other, and **split** when congestion pushes travel times past it. The region view is therefore a *diagram of the commute sheds the city actually has*, not a menu of tiles anyone chose.

A Settlement is a **reporting and diagnosis unit, not a simulation unit**. Nothing pools by Settlement, nothing is budgeted by Settlement, and no Rule reads one — Districts remain the granularity of Goods pooling. They are **not** the granularity of the travel-time matrix; `adr/0047` removed that role from the District entirely. See `docs/adr/0020-one-live-world-and-settlements-are-derived.md`.

**Map Layer**
A coarse scalar field covering the world, stored **one value per Cell** as integers, double-buffered, and updated on a staggered low-frequency schedule. Layers are *composed* at the point of use rather than baked into derived layers.

A Layer is a **convolution of a source field with a bounded kernel, never an iterative relaxation.** That is what makes twenty factories superpose exactly, and what makes `02 §2.4`'s incremental re-diffusion exact rather than approximate. Under relaxation neither property holds.

**Only wide-range point sources are Map Layers.** Fields are sorted by the geometry of the thing that emits them, and short-range line sources are queries instead — see `Noise`. **One field, one geometry, one range:** a proposed Layer with two source geometries, or with two ranges more than about 5× apart, is two fields wearing one name. See `docs/adr/0034-fields-are-sorted-by-source-geometry.md`.

**Noise**
**Not a Map Layer.** A point-of-use query: the volume on a Lot's own frontage Street, plus the summed contribution of every linear source within ~300 m **whose contribution there exceeds the ambient background**. Noise is short-range and falls off logarithmically, so most of it happens inside 100 m — a gradient that fits inside a single Cell, and which a Cell-resolution field therefore flattens into *is there a road here*.

**The query enumerates by loudness, never by road class.** A crossover rather than a threshold: the background *is* the local-Street level the query already computes, so nobody authors a number, the enumerated set stays small by definition, and a city with a high noise floor correctly notices fewer individual sources.

What it depends on is **bimodally distributed traffic volume** — a uniform background plus a bounded set that stands out, with nothing in between — which is a property of `adr/0014`'s grid-plus-sparse-Arterials layout rather than of its labels. A transit line on a **Reserved** right-of-way is the design's one manufacturer of the middle case, putting Arterial-scale volume onto a grid Street; enumerating by loudness catches it where enumerating Arterials would not. Near-road air pollution is the same query with different weights.

**Sealing**
The record of development on the ground: the count of Tiles in a Cell ever built on. One house seals 1/1024 of its Cell. Sealing decays at a rate drawn from the Ruleset **keyed by terrain type** — rock may never recover, floodplain may recover over hundreds of Days. The rate is never stored per Tile; storing it would freeze it into every save.

**Roads Seal, and so does every other built Tile.** The road network is therefore visible to Fertility, so a paved city genuinely has less farmland — which is correct and was previously missing, since Sealing had only ever been discussed through Buildings. The **verge beside an Arterial is never built on**, so it stays unsealed and Woodland regrows on it; a highway sterilises land for *development* without sealing it against *recovery*.

**Fertility**
Agricultural capacity. **Composed at the point of use, never stored:** `terrain suitability − Sealing − pollution`. All land begins fertile and development degrades it, so fertility is a fact the player *makes* rather than one the generator deals. Farms in turn emit into pollution, so agriculture and housing repel each other without any rule saying so. See `docs/adr/0022-land-is-a-stock-the-city-spends.md`.

**Woodland**
The one generated resource. Forest Tiles are not farmable while wooded; clearing yields Timber as a one-time harvest and leaves fertile ground behind. Woodland regrows slowly on unsealed, unoccupied land, so land use is cyclical and the extraction frontier migrates outward on its own through ordinary Building decline.

**Water Body**
A pond, lake, river, bay or stretch of coast. Generated once and immutable (`adr/0021`), so the water network and its flow directions are generator output and are never read inside a Tick. Every Water Body is a **Bin holding the Waste family** (`adr/0031`) with two parameters: a **capacity**, and an **outflow rate** to the next body downstream — terminating in a Hinterland off the map edge.

Those two numbers produce every behaviour with no taxonomy of water types. A pond has no outflow and fills. A river's outflow exceeds any plausible inflow, so it stays clean and **exports the consequence downstream onto somebody else** — the one asymmetric spatial relationship in the design, since every other consequence is suffered equally by its cause and its neighbours. The represented sea is large and slow-draining, so it absorbs a great deal and *still fills* if inflow outruns outflow.

**Nothing is an infinite sink.** The map holds only a section of the ocean, and a section is bounded — so dumping is never free, it is only cheaper. What capacity actually decides is whether pollution behaves as a **debt** (a small body: accumulates, permanent, and the amenity is gone) or a **rent** (a large one: tracks current throughput and recovers when you stop). That is a gradient rather than two categories, which is the point.

**A Water Body's effect on land is a shoreline line source** whose intensity is the Bin's level — so a fouled beach degrades adjacent land value and removes a walkable Amenity destination. This is also why *area* needs no source geometry of its own: an area's influence on land is its perimeter, and a coastline and a pond are one geometry at two lengths.

---

## Buildings and the rule engine

Lineage: this layer is adapted from SimCity 2013's **GlassBox** engine, whose production model was excellent even though its movement model was not.

**Building**
Anything that occupies a Lot, holds Bins, and runs Rules. Houses, shops, factories, farms, and service buildings are all Buildings. A Building has a footprint (the set of Tiles it covers) and interacts with Map Layers through that footprint. GlassBox called this a "Unit"; we say Building because we have no non-building instances of the abstraction.

A Building holds **zero or more Occupants**, up to a capacity **declared by its kind** — an apartment block is one Building and many Households. This is the only structure in the design that sits between a person and their dwelling, and it is bounded by a hard invariant:

> **A Building aggregates logistics. It never aggregates decisions.**

It may hold Bins its Occupants draw from, one Access Point they all share, and one Parking Shed they all query. It may **never** hold a Need, money, a Provider List, a Trip, or anything a Household decides. If a field would differ between two Occupants, it lives on the Occupant. The checkable rule: *a Building field that would have to be averaged across its Occupants is a Cohort forming* — which `adr/0005` deleted, and which would re-enter here if anywhere.

**How many it may hold is a property of the Ruleset in force, not of the Building** (`adr/0068`): the capacity is declared per kind, derived and rebuilt when a Ruleset is adopted, and never saved — the same disposition a Bin's ceiling has and for the same reason. A density band expresses itself as **which kinds a Lot permits**, so `adr/0025`'s *"density says how many Occupants a Lot may carry"* runs through the permission set rather than through a second mechanism. A Building standing over a lowered ceiling **evicts the overflow into the Unplaced Pool** — occupancy has no consumer, so unlike an over-full Bin it would never drain on its own.

**Zero is a real state**, because construction and placement are different mechanisms (`adr/0069`): a Zone Rule raises a Building and houses nobody, and the placement pass fills it over the following Days.

**Failure Pressure**
What a Building accumulates when the city stops working for it. Three sources: **Trips to or from it failing**, **its Rules repeatedly reaching a reporting terminal**, and **local conditions falling below its Occupants' tolerance**. Past a threshold it loses occupancy and quality; past a further one it is **abandoned**, its Occupants are evicted into the Unplaced Pool, and its Lot returns to vacant.

**It is a property of the Building and of nothing else.** Not of the Lot, not of the Zone, not of what the land is currently zoned for — which is why repainting a permission set cannot save a Building or condemn one.

**It is a duration, not a tally**, and that distinction is the whole mechanism. Pressure is *how long this Building has been continuously failing* — measured from the moment it entered the failing state — and it resets the instant it stops. Recovery is therefore total and immediate: a Building whose Trips start succeeding again is not a Building working off a debt, it is a Building that is fine.

**Counting failure events instead would invert severity, which is the trap here.** A Rule that fails does not retry; it subscribes to what it is short of and sleeps until supply arrives (`adr/0045`). So a Building that is *comprehensively* starved generates **one** failure event and then silence, while a Building that is *intermittently* supplied wakes and fails over and over. Tally them and the healthier Building is condemned first. Duration gets the ordering right, and needs no decay rate to stay bounded — nothing accrues, so nothing can run away.

**The accumulated condition is retained and readable** — *"abandoned: 74% of work trips exceeded commute budget over 30 days"*, never a sad-face icon. An abandonment nobody can explain is the thing this whole design exists to refuse. `LEGIBLE CAUSE`

**Derelict**
A Building the **Ruleset in force cannot describe** — its kind is not declared, so it holds whatever Bins survived, houses whoever lived there, and runs nothing at all. It is **derived, never recorded**: there is no derelict flag, and the state is read off the kind (`adr/0057`).

**It is not abandonment and shares none of its machinery.** Abandonment is what the *city* does to a Building — Failure Pressure past a threshold, with a sentence naming the cause. Dereliction is what a **Ruleset edit** does to one, and the only true sentence about it is *the Rules no longer describe this*, which is a statement about a file rather than about the city. A derelict Building therefore has no failure pressure and cannot be condemned by it; it stands until the player clears it. `PLAYER GOVERNS`

**Nothing a player does can produce it**, which is the reason the two must not be conflated. The Ruleset changes under a live city only when a designer is balancing (`adr/0015`) or when a save meets a different Ruleset (`05 §7`) — so this is **development-time state**, and it is in this vocabulary because the code and `02 §4.3` both name it, not because a player will ever meet one.

**Outside Connection**
A special Building at the map edge representing the rest of the world. Absorbs surplus Goods and supplies deficits, at a price. The pressure-release valve that keeps the economy from having to balance perfectly.

It is also **the city's gate**. Households arrive and depart through it as ordinary Trips, so immigration is physical, located, and congestion-bearing rather than a number added to a pool. Its throughput is what bounds arrivals — infrastructure the player built, not a constant someone chose.

**Hinterland**
The economy behind one map edge, shared by every Outside Connection on that edge. Not a simulated place — never Ticked, never rendered — but a small configuration described in **the same units a District exposes**: median rent, median wage, **a price per Good**, service levels, a commute figure. It is **the one authored anchor under every price in the design** — Goods, rents and wages all bound to it, so a designer authors four objects and never writes a price anywhere else (`adr/0026`, `adr/0050`). Prospective Households compare it against the city using the identical utility function residents use, so the Outside is an ordinary alternative in the choice model rather than a special case. Authoring it in domain units instead of utility units is the whole point: *"is §620 the right rent out there"* is a question a designer can answer and a player can read off a panel, and `V = 4.7` is not.

A Hinterland is **a stock the city spends** — the third instance of the pattern, after Land and Woodland. It holds a population with composition, and the city **takes the most willing first**, so drawing it down raises its rate *and* skews its mix toward the stages that weight cheapness hardest. Both effects have the same cause and neither needs a rule. Departures refill the Hinterland they leave for, and it recovers slowly on its own.

**There is no population ceiling.** Drawdown is a gradient, not a wall — exceed the recovery rate and you are spending the stock, exactly as with Timber, and the readout says so in the same words. What runs out is *cheap* immigration, never immigration.

Four edges means four Hinterlands drifting independently, which is what makes the Outside legible: a single hidden anchor has no referent, but four comparable markets are each other's referent. It also gives Outside Connection placement real stakes — a second road on the same edge buys throughput into a market you are already draining; a port on the far edge buys a different economy, at the cost of longer hauls.

**The map edge is load-bearing, and this is why the world is bounded.** No edge means no import and no export. It is also what converts the two resource ratchets — permanent Sealing and slow Woodland regrowth — from collapse into expense: a mature city is a net importer of Food and Materials, which is a bill rather than a wall.

**Bin**
An integer store of one resource with a capacity. Its level is a `long`, and so is every quantity on the path that writes it (`adr/0065`) — the Household's Money is a `long` and money is a Resource held in a Bin, so a narrower Bin would narrow a payment. No floats, no continuous flows. Bins live on Buildings, on Districts (as Pools), and globally (the treasury).

**A Bin's level never leaves `[0, capacity]` by any write**, and it can be **above** the ceiling all the same: a capacity is not a property of the Bin but of the Ruleset in force (`adr/0064`), so lowering one in a reload leaves standing Bins holding more than the new ceiling. Such a Bin **drains rather than clamps** — the stock stays, producers stop because headroom is negative, consumers are untouched, and it comes back into range by being spent. Clamping would destroy Goods on a keystroke and break conservation, which is the one property the Bin exists to give.

**Which Bins a Building has is declared per Building kind**, in the Ruleset, with each one's capacity — so *how many Bins* has a structural answer rather than a guess, and a capacity is an ordinary tuning number under `adr/0015` rather than a constant somebody chose in code. `(kind, Resource)` is a **key**: one Bin, one Resource, and a kind declaring two of one Resource is refused. A Building is given exactly its kind's Bins when it is built, and the ceiling on each is **derived and rebuilt** from the declaration rather than stored — at load, and at every swap.

**A Bin is written through one mutator, and the write drains the wait list.** There is no assignable level: a Bin written without draining its wait list leaves every waiter asleep for ever, with no error and no timer to rescue it (`05 §9`), so the door is narrowed rather than the discipline documented. The drain runs **from the head, only while the Bin's *current* level still covers the head's requirement** — and that requirement is **derived at the drain, never a number the waiter recorded when it failed** (`adr/0063`). It stops at the first waiter it cannot cover rather than skipping past it, and it spends the level down as it wakes, so a Bin holding six wakes the one waiter that needs six, or the two that need three each, and no more. Skipping would let a large waiter starve behind small ones. Waking everybody would let **small waiters beat large ones on quantity** — not on identity, since Phase 3's order is a fresh per-Tick shuffle and no Building can hold a standing advantage.

**A waiter is woken only when it can be completed, and is then served completely.** This is what makes shortage a gradient: turns rotate, so under half supply every bakery bakes half as often. **Evenness is over time and never within a single arrival** — dividing each delivery among the waiters instead would leave every one of them holding part of a threshold and none of them able to fire, which is less output than starving the late-built ones would have produced. Accumulating toward a threshold is a thing a Ruleset may **author**, as an acquisition Rule feeding the consumer's own Bin; it is not what the wait list does (`adr/0063`).

**Rule Instance**
One `Bin Rule` on one `Building` — the row carrying where that Rule has got to. It is created with the Building and freed with it, and it is in exactly one of two states at every moment: **armed**, scheduled on the `Event Wheel` for the Tick its `rate` re-armed it to, or **waiting**, on the wait list of the one Bin it was short of, recording *which* Bin stopped it and *why* — never *how much*, which is derived from the Bin when the drain asks (`adr/0063`).

**The two states share one link, and that is the design rather than an economy of columns.** `02 §4.1` says a Rule that fires re-arms and a Rule that fails subscribes *instead of* re-arming; a row that could be armed and waiting at once would be a Rule that polls *and* subscribes, which is the defect subscription exists to remove. Making the states share a link makes that unrepresentable rather than checked.

**Nothing is allocated when a Rule subscribes and nothing is freed when it wakes.** A Rule Instance's whole life is its Building's, so shortage *churn* — which `02 §4.1` identifies as the real cost driver, ahead of chain depth and ahead of how broken the city is — costs no rows at all.

**Rule**
A transformation the Ruleset declares and the simulation applies. Rules come in **two families**, and which family a mechanism belongs to is decided by one question: *does it wait on a specific named thing, or sweep a population?*

**Bin Rule**
An atomic transformation over Bins, attached to one Building. It declares inputs and outputs against **four scopes** — local Bins, the District Pool, global Bins, and Map Layer cells under the footprint (write-only, since a layer cell has no capacity to exceed). **A Bin Rule applies in its entirety or not at all** — if any Bin would go negative or exceed capacity, nothing happens.

**A scope answers *whose is it*, not *where do I look*.** A `local` term is free because the Bin is already the Building's; a term crossing an **ownership boundary** is a **trade**, and the Good moves one way while money moves the other at the prevailing price. **No payment is ever written in a Rule** — the price is emergent, the quantity is the term's `amount`, and the counterparty follows from the scope, so nothing is left for a designer to author. This is what lets `amount` stay a fixed integer for ever, and it is why the District Pool is **a market** rather than a wider Bin lookup. `adr/0050`

A Bin Rule carries a firing rate, an apply count, and an optional fallback Rule invoked on failure. Fallback chaining is how supply-chain substitution works: *can't source locally → import* — **source** substitution and never input substitution, so every link in a chain relieves the same Bin the head failed on and **a failed chain subscribes once, at its head**. A link whose rescue arrives later rather than this Tick declares the Bin it `fills`. The **rate is a reschedule interval, not a polling period**: success re-arms it, and **failure subscribes it to the Bin that was short** rather than retrying on a timer, so a starved District costs nothing until supply arrives. The last link of a chain is a **reporting terminal**, which records the condition and leaves the chain failed — a terminal that *succeeded* would re-arm the head on its rate and walk the chain forever. Each Bin therefore holds a wait list, drained round-robin so shortage degrades a District evenly instead of starving its late-built Buildings. **Apply count is authored per Rule.** A Rule with `{min, max}` applies as many times as its inputs allow within that band and fails if it cannot reach `min`; `min = max` is the fixed case, so one form spells both. Which it is, is a modelling decision and never a performance one — *greedy when the actor works through its stock, fixed when the actor owes a quantum.* **Or the count is derived** from a Readout rather than being a literal, which is how proportion is expressed without leaving integers. Never both: *greed handles what is consumed, derived handles what is consulted.*

There is **no proximity scope.** Nearest-first selection among nearby options — a Parking Shed, an Amenity set, a Provider List, dispatch to the nearest station — always belongs to something that moves. *Movers choose; Rules transform.*

**Sweep Rule**
A Rule that fires on a **time trigger**, walks a population, tests real simulation state, and acts on those that qualify. Attached to the city or a District rather than to a Building. It never subscribes and never waits, because an entity cannot know whether it qualifies without being evaluated. Its two instances are the **Zone Rule** and the **Policy**.

**Zone Rule**
A Sweep Rule over Lots: it creates, upgrades, downgrades, or demolishes Buildings. It **samples** a small random set rather than scanning — not to save work, but because sampling *is* the behaviour model. Developers do not evaluate every Lot either. That it also keeps growth cost constant regardless of Zone size is a second benefit, not the reason.

**The sample decides where to build; it never decides what dies.** The *developers do not evaluate every Lot* argument is about an actor choosing among alternatives, and **abandonment has no actor** — a Building does not fall over because somebody looked at it. So a sampled Lot's Building is condemned on a quantity that was already true before the sample arrived: how long it has been failing (see **Failure Pressure**). The sample is when the city *notices*, not when the Building *fails*. Sampling the accumulation rather than the observation would give every condemned Building a random lifetime whose distribution is set by sample size and Lot count, which models nothing.

**The permission set scopes what a Zone Rule may build, never which Lots it looks at.** A Rule that only ever sampled Lots already carrying its bit would make a Building's mortality depend on the player's paint — repaint the land and the Building becomes unreachable and therefore immortal. Permission is a term in the *create* predicate; the population is Lots.

A Policy, by contrast, **sweeps** its whole population, because a transfer is an entitlement rather than a behaviour and paying a random subset would be a defect.

**A mechanism never changes family for performance reasons.** The two differ in observable behaviour — how fast an input propagates, who wins a contested resource, and what `Evidence` reports on failure — so moving one is a change to the city, not an optimisation.

**Ruleset**
The complete body of Rules, Zone Rules, and tuning constants, loaded from data files at runtime and **hot-reloadable**. The compiled binary is a stable interpreter for the Ruleset. `FAST ITERATION`

**It is validated where it is parsed, and a malformed one is refused rather than warned about.** Every name is resolved to an id before the simulation sees it, so the simulation never reads a string and never meets a Rule it cannot run. A Ruleset that would load with a broken chain produces a Building that fails silently, which is the outcome the refusals exist to prevent. `adr/0048`

**A reload is a *transition*, not a command.** It is a property of a Tick rather than an event inside one: the Ruleset is swapped at the top of Phase 0, so a Tick has exactly one Ruleset and the commands in the reloading Tick run under the **new** Rules. The Input Log carries the transition as a pair of content hashes, so a replay reproduces it by construction and there is no reload verb. **A declaration's identity across two files is its name, never its id** — an id is a position, and removing a declaration from the middle of a file renumbers everything below it — which is what makes *this Bin's Resource survived* a question with an answer. **What the swap destroyed is kept as state**, capped, because a defect caused by a degradation three patches ago is upstream of every snapshot anybody holds (`05 §7`). `adr/0015`

**Derelict**
A Building whose kind the Ruleset in force can no longer describe. **Derived, never a stored flag** — it is `Kind == 0`, the row naming nothing — and the distinction is load-bearing rather than stylistic: the only actor who removes a kind from a running city is a designer balancing, and a designer's commonest move is *undo*. A mark that recorded the removal and never cleared would leave them a city of permanently inert Buildings, which is the failure `adr/0015` exists to prevent arriving through the mechanism written to serve it.

A derelict Building **still stands, still holds its Occupants and still occupies its Lot**. What it does not do is run Rules — it has none, because its kind has none — so it has no failures of its own to die of, and nothing about it decays. It is recovered by a reload that describes its kind again. `HONEST DEGRADATION`

---

## Economy

**Resource**
Anything held in a Bin and moved by Rules. The single abstraction beneath Goods, Utilities, and Money — which were three separate mechanisms until it was noticed that **every exception to "it's a Good" was an exception on exactly one axis: transport.** `adr/0024` spent an ADR proving Money is conserved-like-a-Good-and-not-a-Good, and that entire argument reduces to one boolean.

Resource is a **mechanism-level** term. The player never sees it; the player sees the families, which stay named and distinct because a food chain and a power grid are different things to think about even when they are the same thing to compute.

| Family | Inter-District movement | Members |
|---|---|---|
| **Good** | a **Shipment** — a Vehicle on the Road Graph, contributing congestion | Produce, Food, Timber, Materials, Consumer Goods, Waste |
| **Utility** | flow along the District adjacency graph. No Vehicle, no congestion | Power, Water, Sewage |
| **Money** | none | — |

Two parameters distinguish every member, and they are **not** the same field:

- **Capacity** — the instantaneous ceiling on a Bin. Finite for everything physical; **unbounded** for Money, because no physical ceiling exists. Write the Rule's check as `delta > capacity − level` so it cannot overflow. *Prefer an explicit* unbounded *to a large sentinel* was the original wording and `adr/0065` **withdraws it as unachievable**: arbitrary precision means a managed type, which lint 7 forbids in the core, so *unbounded* is `long.MaxValue` and is **named as a ceiling** — one far enough away that approaching it is a defect caught by the long-run test's magnitude clause, never a headroom refusal and never a panic. The honest sentence is better than the word it replaces, because the row always held a sentinel and calling it unbounded is what let it escape into things that divide by it.
- **Storage** — whether a Bin carries over between periods. **Zero** for Power, which is what *"there is no electricity warehouse"* actually means; large for Water; filling for Sewage and Waste, where the failure is at the top of the Bin rather than the bottom.

**The constraint is chain depth, not list length.** An earlier rule guarded the anti-goal by counting — *five Goods, and a sixth must replace something* — which cannot explain why Waste is fine and Steel is not. The real constraint was always stated alongside it: **maximum chain depth of three**, and supply chains exist to create pressure that propagates into people, never to be an optimisation surface.

A **terminal** Resource, produced or consumed one hop from the edge of the graph, is nearly free. A **link**, with both an upstream and a downstream, adds a chain and is expensive. Power, Water, Sewage and Waste are all depth-1 terminals, so the list grows from five to nine and the logistics complexity of the game does not change at all. `Ore → Steel → Materials → Consumer Goods` is depth four, over the ceiling, and refused **with a reason**.

**Good**
A Resource whose movement between Districts requires a **Vehicle** — and therefore the only family that contributes congestion, occupies road capacity, and can be strangled by a jam. Waste is a Good: it is produced at Buildings, accumulates in Bins, is hauled by vehicle, and may be exported through an Outside Connection at a price, which puts *paying someone else to take your garbage* on the balance of payments beside Food and Materials.

**Money**
A **conserved stock that flows without transport.** Conserved like a Good — it is never created or destroyed inside the city — but not a Good: it needs no Shipment, occupies no Vehicle, and contributes no congestion. Held by Households, by Businesses, and by the treasury.

**The Outside Connection is the only source and sink**, exactly as it is for Goods, which is what keeps conservation intact without inventing a sixth Good. Money enters as payment for exports and leaves as payment for imports, so **the city has a balance of payments** — and that is what makes the endgame in `adr/0022` mechanical rather than narrative. A city whose land is sealed and whose schools were never built imports Food and Materials, exports nothing, and watches its money supply drain. Borrowing is the damper: a deficit becomes a debt burden, never a stop — but it is **a player action, never an automatic overdraft**, so the treasury genuinely empties and the Rules that could not draw simply wait.

**Velocity is emergent, not tuned.** A Household buys what its Needs demand from its Provider List, holds a reserve sized by its Life Stage — a Young Household saving toward forming a family needs a deeper buffer than an Empty Nest — and spends the remainder on Consumer Goods, which is the Satisfaction Need already in the model. Hoarding is bounded because saving has a purpose and therefore a ceiling.

**Poverty is an absorbing state, and it emerges rather than being designed.** A Household at zero money cannot buy Food, cannot afford to move, and cannot reach a job that requires a car it cannot buy. Every exit costs money it does not have. This is not corrected by the simulation and not prevented by it; it is surfaced, and what happens next is a Policy decision the player makes or declines to make.

**Upkeep**
The standing cost of infrastructure the city has built, drawn per Day. **Not an authored rate.** Every piece of infrastructure has a **design life** — a duration in Days at zero traffic — and its Upkeep is `construction cost ÷ effective life`. Traffic consumes that life faster, so **Upkeep is a base term plus a wear term** and the wear term reads the volume the Segment already tracks.

The formulation is chosen so that the only authored number is a **duration**, which is scale-free and means the same thing in a village and a metropolis, rather than a magnitude in §/Day which does not. It is also the same amortisation real infrastructure budgets use.

**It is legible as a pipeline rather than as a bill.** *"This corridor has 40% of its life left; at current freight volume it needs rebuilding in 120 Days"* is a reading of present state, not a forecast — the same move that made `Schooling`'s lag legible.

**Consequences nobody has to author.** The Bill responds to *how* the city is used and not merely how large it is; freight routing acquires an economic cost; and **transit's case strengthens with no rule written**, because fewer vehicle-km is less wear. Wear scales superlinearly with axle load, so **freight dominates it and commuters barely register** — which makes a Processing corridor the most expensive road in the city to keep. A road built and not used is nearly free, which is forgiving of experimentation.

**The draw is automatic; there is no maintenance funding lever.** A slider whose only sensible setting is *as high as affordable* is not a decision. What the player controls is the **rebuild threshold**, which is a `Policy` — *rebuild any Segment below N% of life* — because infrastructure decays slowly and then rapidly, so an early cheap treatment beats a late reconstruction. **It is District-overridable, and that is what makes it a choice**: the cheap strategy needs capital sooner and more often, so a player who commits to it cannot commit everywhere and must decide *which* corridors to preserve. Deferring and betting on future revenue is an equally coherent play style.

**Unfunded Upkeep is not a decay system.** It is a `Bin Rule` that could not draw, so it waits — and unrenewed life means capacity and free-flow speed fall, both of which are Road Graph attributes the `Volume-Delay Function` already reads. **The Bill therefore converts into the Clock**: an unpaid bill lengthens every commute, which is the first mechanism coupling `01-player §5`'s two pressure axes.

**Infrastructure**
Anything the city builds that is not a Building: roads, Junction pieces, transit right-of-way, plants. **It is paid for three times** — in **Money**, in **Materials**, and in **Land** — and none of the three is a budget the designer set. See `docs/adr/0035-infrastructure-is-priced-by-what-it-consumes.md`.

**Public construction is a stimulus or a leak, and conservation decides which.** Under `adr/0024` money is conserved, so a § spent on a road is not destroyed — it becomes somebody's income. Where it lands depends on where the Materials came from: bought from local Processing it becomes local wages, and imported it leaves through the gate. **A city that builds while importing all its Materials is exporting its own stimulus.**

**Need**
A Household's satisfaction with respect to one dimension of life — food, rest, comfort. Expressed as a **relative** scalar where **0 is ideal** and negative values indicate deficit. Needs are explicitly *not* stockpiles; modelling them as absolute quantities makes the economy unstable and nearly impossible to balance.

**Pool**
A District's abstract Goods store. Goods moving between Buildings within a District pass through the Pool instantly, subject to connectivity. No Vehicle is simulated. This reserves the expensive transport simulation for movements the player is actually meant to optimise.

**Shipment**
A physical movement of Goods between Districts, or to and from an Outside Connection. Carried by a Vehicle on the Road Graph, contributing real congestion.

**Household**
The residential economic actor: a group of Citizens sharing a dwelling and finances. Holds Needs, money, and a **Provider List** — a short, sticky set of known shops, workplaces, and services. A Household switches provider only when a *known* alternative is *substantially* better. `BOUNDED KNOWLEDGE`

**The Provider List is an intrusive index list, and `adr/0017`'s length is a cap rather than an allocation** (`adr/0066`). A head on the Household, a `next` on the entry, entries drawn from one shared pool — the same structure every other collection in the core uses, and the rule it had been the only exception to. So its memory follows **what Households actually know** rather than what they are permitted to know, and raising the cap costs nothing until somebody learns a shop. Any per-entry state is paid per entry that exists.

**Visiting a provider is a Trip, and a Household visits one per shopping occasion** (`adr/0067`). Finding nothing is recorded on the Household — consecutive failed occasions, and a refusal reason, since a provider can fail on price or distance as well as on stock — never as a `Trip Fate`, whose four outcomes are all properties of the journey. A cursor advances on failure and resets on success, so a provider that failed is skipped for exactly one occasion: a deprioritisation that is a **duration**, with its window derived from the mechanism rather than chosen.

**A Provider List entry carries its Mode.** *How I get to work* is decided when the job is taken, not every morning — so mode choice costs nothing per Trip and is more realistic than re-deciding daily. Available modes are walking always, driving if the Household owns a car, and Transit if a stop is in walking reach at both ends.

**Re-evaluation happens on a countdown, or immediately on a failed Trip.** The countdown is an ordinary Event Wheel entry, the same machinery `adr/0011` uses for Life Stages; the failure trigger is free, because `Trip Fate` already records it. Nobody reconsiders their commute because a metro opened — they reconsider it on a schedule, or on the Day their commute actually fails. This is what keeps a network edit from forcing a global invalidation, and it makes **ridership ramp rather than snap**, which is both true and a readout the player can be shown honestly.

**Skill Tier**
What a Citizen is qualified for. Three tiers, and the boundaries between them are **not the same kind of boundary** — which is what makes the asymmetry self-explaining rather than arbitrary:

> **Tiers 1 and 2 are separated by learnable skill. Tiers 2 and 3 are separated by a credential.**

So experience carries 1 → 2, and **only schooling reaches 3** — not as a balance wall but as a category boundary. An apprentice becomes a technician by doing the work; a technician does not become an analyst by staying longer. This is what protects the education → Office → exports chain from a bypass, and it is the one place in the design where *scarcity is a gradient, never a wall* correctly does not apply, because a category is not a quantity.

**Within a tier, experience is continuous, with a ceiling.** A Citizen accumulates experience on the job and becomes more valuable, up to the band's limit and never past it. This is the design's only source of **productivity growth** — without it a city of 10,000 produces the same on Day 100 and Day 5,000 — and it is an *intensive* growth margin beside the extensive one. It does not thin the labour market, because thinness is a property of the number of segments and the segments remain three.

**Life Stage is composition. What a Household is *doing* is a separate field.** Studying and Unemployment are states, not stages — an In Education Household is 1–2 adults with no children, which is a Young Household exactly, so it fails the definition of a stage. This is what keeps the stage table from accumulating rows that are really occupations.

A Household also has a **Life Stage**, which determines its composition — how many adults, how many children — and therefore its school demand, dwelling size preference, and willingness to move.

**Taste**
A Household's private position on each preference axis: how much it weighs quiet against variety, space against centrality, rent against commute. Drawn at formation from the Household's own seeded stream and **persistent for life** — Life Stage supplies a base and a *range*, and Taste picks the point within it, so someone who always valued quiet still values quiet once they have children.

Taste is not the same as the choice model's random component, and the difference is the point. `ε` is re-drawn at every decision, so it makes a Household randomly *inconsistent*; Taste is stable, so a Household is consistently *itself*. A Pinned family that reliably chooses quiet neighbourhoods is a character; one that chooses differently every time is noise wearing a name. `UNIQUE INDIVIDUALS`

It is also the same move `adr/0017` made for knowledge, applied to preference: variation acquires a **cause** rather than a seed. And **the width of a stage's range is as expressive as its midpoint** — Empty Nest Households genuinely diverge, some downsizing into walkable centres and others leaving for quiet, so that range is wide; Family is narrower, because schools matter to nearly all of them.

**Life Stage**
Where a Household is in its life. Stages advance on a per-Household countdown in Days, as an ordinary event on the Event Wheel. **Citizens themselves never age**: adults carry a static age drawn on formation, and a child's schooling tier is derived from the Household's stage.

| Stage | Composition | Exit |
|---|---|---|
| **Young** | 1–2 adults, no children | **Decision:** how many children, possibly zero. Zero leads to Childless. |
| **Family** | adults + young children | Scheduled. Primary school demand. Strongly reluctant to move. |
| **Mature Family** | adults + teens | Scheduled. Secondary school demand. Children leave and form new Young Households. |
| **Childless** | adults who never had children | **Decision:** when to dissolve. |
| **Empty Nest** | adults whose children have left | **Decision:** when to dissolve. |

Childless and Empty Nest behave identically and are kept separate on purpose: one says the city is too expensive to **start** a family, the other that it is too expensive to **stay** in.

**Replacement Rate**
Children per Household. **Two is exact replacement** — two children become two adults — so the threshold is a consequence of conservation rather than a chosen constant, and can be shown to the player as a diagnosis rather than a target.

**Retention**
Of the Households the city generated itself, the share that found housing here rather than becoming Departures. Distinguishes a city that fails to produce a next generation from one that produces and then prices it out.

**Sorting**
The other demographic channel: the city's mix changes because **different kinds of Household arrive and leave**. Sorting is how schools close the loop to workforce — good schools attract already-educated Households rather than educating people in place, which is cheaper and closer to how real cities work.

Between them, Sorting and Life Stages give the city two independent demographic engines. They pull against each other: **affordability drives internal generation, attractiveness drives immigration, and attractiveness raises prices.** A city can be dying of its own desirability.

See `docs/adr/0010-one-clock-and-demographics-by-sorting.md` and `docs/adr/0011-household-life-stages-and-self-generating-population.md`.

**Business**
The commercial or industrial economic actor occupying a Building. Consumes inputs, produces outputs, employs Citizens, and offers Goods or services to the market.

**Policy**
The player's non-spatial lever: tax rates, service funding, regulations, and transfers. Lineage: SimCity 4's ordinances, which had the right shape and too little depth.

**Policy is never prescriptive.** The game supplies levers and reports consequences; it never encodes a correct setting, and no advisor recommends one. A player may spend the city's coffers to raise the floor under Households that need it, or may decline and let wealth disparity express itself spatially — very good neighbourhoods and very bad ones, each diagnosable. Both are supported outcomes. The design's commitment is that the consequences of either are **visible and traceable**, not that one of them is right. `LEGIBLE CAUSE` `PLAYER GOVERNS` `NO VERDICT`

Four properties separate this from a list of toggles:

**A Policy is a Rule, never a modifier** — specifically a **Sweep Rule**. The test is whether `Evidence` can expand it into specific entities. *"Supplement 15% of gross income below the poverty line"* names 1,240 Households and an amount for each — a Rule, and it moves conserved Money between named parties. *"+10% desirability here"* names nothing and drains nothing — a modifier, and the shallow version.

The sweep **is** the expansion, which is what makes the Evidence test cheap rather than aspirational: the same pass that pays the 1,240 Households is the pass that can name them. Percentages are preferred to flat amounts because a flat amount goes stale as the economy grows — and because a percentage is expressible as a **derived apply count**, which keeps a Policy inside the ordinary Rule format instead of requiring a query language.

Where a Policy's payer runs dry, it pays whom it reaches and reports where it stopped. The scan start rotates per trigger, so exhaustion is a gradient across the population rather than a permanent line between the always-paid and the never-paid.

**Policies are parameterised, not toggled**, and their parameters meet a real distribution rather than a constant. Where a threshold is needed the player sets it, and the game shows an honest reference point beside it — *"a Household needs §34/Day for Food alone"* — composed from real prices, in domain units. The game does not decide where the line goes.

**Effects are previewable**, which is `Evidence` run forward instead of backward: *"paid by these 3,200 Households and 140 Businesses; benefits these 1,240; §84k/Day."*

**No Policy is ever hidden or locked.** Gating a lever behind a population figure or a milestone is a hidden scalar deciding what the player may do; gating it behind *relevance* is worse, because the game's idea of relevance is not the player's. `NO VERDICT` applies to the interface as much as to the mechanics. The preview above **is** the relevance signal, and it works by stating a fact rather than a judgement — *"currently applies to 0 Buildings; would apply to all future development in Northfield."*

That second clause matters, because Policies divide into two kinds and one of them is normally enacted *before* it does anything:

| Kind | Acts on | Preemption |
|---|---|---|
| **Constraint** — emission limits, parking minimums, nuisance caps | what gets **built** | the normal case. This is what regulation is: set the rule, and development conforms. Retrofitting is expensive or impossible. |
| **Flow** — transfers, tax rates, funding levels | Money already moving | reactive by nature. Enacting a transfer with nobody destitute costs nothing and does nothing. |

A relevance indicator naive to that distinction would dim an emission limit at exactly the moment it is most valuable to set.

**Anything attached to a place can be overridden per District**; only instruments acting on the city's single balance sheet — borrowing, essentially — are irreducibly global. Global is the default level, not a separate category, so a player who never overrides anything plays a complete game.

**The constraint on Policy is incidence, not affordability.** Every Policy that helps someone is paid for by someone, and there is no setting that favours everyone. That is what keeps Policy interesting after the treasury is healthy — the question is never *can I afford this* but *who pays*. Incidence has teeth because the chains are circular: a city that taxes its low-income Households into departure loses the tier-1 employment that produces tier-2 workers, and cannot then staff the Offices paying its import bill.

**Evidence**
The rule that every aggregate figure the game displays can be expanded into the **specific entities behind it** — the households counted, the trips that failed, the buildings starved of input. Every summary retains a pointer to its constituents.

This is the mechanism by which `LEGIBLE CAUSE` becomes real rather than aspirational: it makes a causal chain *navigable*, not merely true. It is also a constraint on the simulation, not a UI feature — if a figure cannot name its constituents, the simulation is computing it wrong.

Evidence is the primary way a player encounters an individual Citizen. Free-roam browsing of the population is explicitly **not** the mechanism, and is not required for any diagnosis.

**Readout**
A named scalar an entity exposes for reading. **The declared set has one member today, `occupancy`** — a Readout is admitted when a Rule reads it, so gross income, time unemployed and composed fertility name the intended shape and are refused by the loader until the state behind them exists. (`experience` is **struck**: it folds into the labour Bin as a per-worker deposit multiplier, `02 §4.1`.) A Readout is **read-only** — never consumed, never conserved, never subscribed to — which is what separates it from a Bin. Bins are what a Rule *spends*; Readouts are what a Rule *consults*.

**The set is declared in the simulation, and every Readout is inspectable** — so nothing the simulation acts on is hidden from the player, by construction rather than by a rule pointing at a panel. The shell owns how a Readout is rendered; it never owns which ones exist.

**The converse does not hold, and the difference is the point.** The inspectable surface is far larger than the Readout set: Bin levels, Occupants, which Rule last ran, a Trip's Fate. Declaring a scalar a Readout is not a decision to *show* it — it is a decision to let every future Rule *act* on it. The test is `02 §2.5`'s, the one that demoted service coverage from a Map Layer to an overlay: **does a Rule read it, or is it only displayed?** Display-only is a display, never a Readout.

**The Readout set is declared in the simulation, and the Evidence surface reads it** — that direction, and not the reverse:

> **A Rule may read anything the player can see, and nothing else.**

> **This entry previously bound the set to `Evidence`, and the bound is inverted** (`02 §4.1`). `02 §9` is an obligation to *expand* aggregates and contains no enumeration, so the old direction pointed the readable set at a non-set. Declaring it simulation-side keeps the guarantee below intact — the set is small, named, and inspectable by construction — while removing a dependency on a presentation design that does not exist.

A Policy whose predicate could consult a hidden internal would be one no player could ever explain being subject to, so binding Rule reads to the same quantities the game already displays makes every Rule explicable by construction. It also forces any new readable quantity to be surfaced *before* it can be depended on, and gives the Ruleset a stable named interface — a data file referring to an unknown Readout fails to load loudly, where a data file referring to a moved field would fail silently. `LEGIBLE CAUSE`

**Pin**
A Citizen or Household the player has chosen to keep track of after meeting them through Evidence. Pinned entities are surfaced persistently, and retain a **fixed-size ring** of recent Trips so the player can see how things have been going for them. Fixed-size because a per-Pin history that grew with elapsed time is exactly what `docs/adr/0006-no-collection-grows-with-elapsed-time.md` prohibits; the number of Pins is player-bounded, but a game's worth of Trips is not.

Pinning is how the game delivers long-term attachment to individuals without needing free-roam inspection: players do not want to click strangers, they want to follow *someone they were introduced to*.

**Unplaced Pool**
The set of Households currently seeking housing — immigrants, existing Households that decided to move, **Households the city generated itself** when a Mature Family's children left home, and **Households evicted when the Building they lived in was demolished**. All four enter on equal terms, which is what makes a city failing to house its own children visible rather than a special case.

**Eviction is the one route the Household did not choose**, and it is the reason the Pool cannot be described as *Households seeking to move*. The other three are looking; an evicted Household is looking because the city stopped housing it. It arrives with its Money and Savings intact — losing a dwelling is not losing what you own — which is also what keeps demolition from being a hole in `adr/0024`'s conserved Money.

A Household that finds no acceptable dwelling in a cycle stays in the Pool with a **recorded refusal reason**.

**What drains the Pool is placement, and placement is not construction** (`adr/0069`). A sampled pass runs each cycle over vacant capacity in Buildings that already stand; a Zone Rule raising a new Building houses nobody. The ordering is what makes the Pool a demand signal rather than a population count: placement runs **first**, so a Household still in the Pool when a developer looks is one **the standing stock could not house** — a developer does not build while there are empty flats.

The Pool *is* the demand signal. It replaces the global RCI demand scalar found in other city builders, and it is strictly better as an interface: "412 Households want to move in; 380 can't find anything under §900; 32 can't reach a job inside their Commute Budget" is a diagnosis rather than a bar chart.

The Pool is bounded — Households give up after a limited number of failed attempts and become a Departure. `LEGIBLE CAUSE`

**That is the bound the design relies on, and it is not the bound in force today.** Nothing creates a Household after world creation, so the Pool is currently a subset of a population fixed at that moment and cannot grow with elapsed time whatever it does. `adr/0006` is therefore satisfied for a reason that has nothing to do with Departure — **and the day immigration arrives, that reason evaporates and Departure becomes load-bearing**. Whoever builds the gate owes the give-up rule in the same milestone.

**Departure**
A Household leaving the city permanently. Two channels, counted and surfaced **separately**, because they are different diagnoses with different remedies:

| Channel | Meaning | What it tells the player |
|---|---|---|
| **Unhoused departure** | Entered the Unplaced Pool, failed repeatedly, gave up | A **capacity** failure — the city generated demand it could not physically accommodate. *Build more.* |
| **Housed departure** | Was living in the city and **chose** to leave — needs unmet, rent unaffordable, neighbourhood declined | A **quality** failure — the city accommodated them and then failed them. *Fix what you have.* |
| **Destitute departure** | Could not work and could not afford to leave, until nothing was left | An **economic** failure — the city trapped them. *This is what Policy is for.* |

The third channel is what stops leaving-by-choice and leaving-by-collapse sharing a row, which they otherwise would. It is also the readout for a decision the game deliberately declines to make for the player: a city with transfers shows near-zero destitute departures and a cost, a city without shows a number and no bill.

Cutting across both channels, Departures are also reported **by Life Stage**, because "families with children are leaving" and "childless Households are leaving" are unrelated diagnoses. Departures of newly-spawned Young Households are the sharpest signal of all — those are the city's own children, priced out. See **Retention**.

Departure rate is a distinct demand signal from Pool size: Pool size is a *stock* of latent demand, departure rate is a *flow* measuring how badly the city is failing to convert its own attractiveness into capacity. A city can have a large Pool and be healthy, or a small Pool and be in crisis; only the flow distinguishes them.

Departure is permanent. No departed Household is retained in any form — see `docs/adr/0006-no-collection-grows-with-elapsed-time.md`.

**Unemployment**
A Household with no workplace. A real state, not an instant Departure — losing a job starts a decline the player can watch rather than an event they are told about. Savings drain, discretionary spending stops first, the housing search widens, and destitution is the terminal case.

**Destitution is a reachability failure wearing a money costume.** The Household has no money *because* it cannot reach work, so the exits are the things that change reachability — and only one of them is a transfer:

| Exit | Lever | Character |
|---|---|---|
| **Downsize** | none — the Household acts | automatic, and the gradient before the wall. Closed if the city built no cheap housing. |
| **Transport reaches them** | Connect | infrastructure as anti-poverty policy — a bus line puts jobs back in range |
| **Jobs reach them** | Zone | mixed use, or employment near where the poor already live |
| **Their children escape** | Service | generational, via education and Life Stage. Slow, and the Household itself never recovers. |
| **Transfers** | Policy | does not fix reachability — it **restores agency**, lifting a Household out of the absorbing state so the other four become available again |

That last row is why transfers are not a band-aid and not a cure: a Household at zero money cannot act on any option, so money is what makes the spatial fixes reachable *for the people who need them*. The exits compose rather than compete, and the game takes no position on which the player should use.

**A city with only expensive housing has closed the first exit**, which is where this meets Density — a monoculture of high-end stock does not merely fail Families, it removes the ramp that keeps a bad Day from becoming a permanent condition.

**Occupant**
A Household or a Business. What fills a Building.

---

## Services

**Service**
A capability the city provides rather than sells: education, health, safety, waste, recreation, and the utilities. Delivered by a Building the player places — the design's one placement exception — staffed by Citizens, and paid for from the treasury. **A Service is never a Good**: it has no Bin, is not conserved, and is never shipped.

**Delivery Mode**
How a Service reaches the people who use it. Three modes, distinguished by **who makes the journey** — which is the axis that actually separates these systems, not what they provide.

| Mode | Who moves | Services |
|---|---|---|
| **Attended** | the Household | education, health, recreation |
| **Dispatched** | the Service | fire, police, waste collection |
| **Networked** | nobody | power, water, sewage |

Attended and Dispatched are both ordinary **Trips**, with Legs, a Commute Budget, and a **Trip Fate**. They are therefore subject to congestion, parking, and **Severance** — an Arterial between a neighbourhood and its school is a school the neighbourhood does not have. Only Networked Services move nothing, and they are the genuine outlier in this group.

**Schooling**
A quantity **accumulated per Day a school Trip completes**, never a state a school confers. Three school levels sit under the three Skill Tiers, and they attach to the Life Stages that already exist:

| Level | Attended by | Effect |
|---|---|---|
| **Primary** | Family | a **gate**, not a producer — Tier 1 is the floor already, so primary earns its place because secondary cannot accumulate without it |
| **Secondary** | Mature Family | qualifies for Tier 2 |
| **University** | a Young Household **In Education** | qualifies for Tier 3 — the only route there |

**A school does not educate an adult. It sets the tier of a Household that does not exist yet** — schooling accumulated across Family and Mature Family decides the Skill Tier of the new Young Household formed when children leave home. The output therefore arrives a full Life Stage after its cause.

**Every failure routes through one channel: Trips that did not complete.** Severance, congestion, a school over capacity, a transit line never run — all of them are the same number, and `Evidence` expands it into the specific Trips and the junction that ate them. There is no Day on which a child is refused an education, only a city where fewer of them arrive. The constant is a **duration** — Days of completed attendance per level — which is scale-free and therefore preferred to a magnitude.

**The lag is made legible by showing the pipeline, not the output.** The cohort is in flight and its state is readable now: *"1,240 children in secondary; at current attendance 780 will qualify for Tier 2 and 460 will not."* That is a reading of present state rather than a forecast, and it is why accumulation is what makes the lag legible — a conferred model would have nothing in flight to show.

**In Education**
A state of a Young Household, not a Life Stage. Occupies a dwelling, consumes Food and Services, pays little tax, and **supplies no labour** — the design's first Household that is a net fiscal cost by design, which is what turns education from a line item into an investment with a visible payback. Student housing emerges from it with no rule written: a Household with no car, no income, and a fixed destination bids for cheap, dense, walkable land near the university.

**Utility**
The Networked Services — power, water, sewage. **One abstraction, distinguished by a single parameter: storage capacity.**

**Nobody draws a utility network.** Utilities ride the Road Graph, and a Lot already requires road frontage to exist — so every Building is connected by construction. This deletes an input mode and an entire class of *why isn't this working*. Distribution reuses `adr/0013` exactly: pool within a District, flow along the District adjacency graph between them, import through an Outside Connection at a price. Tens of nodes, no Vehicle, no second network to draw, save, version, or revalidate.

Storage — carry-over between periods, *not* Bin capacity — is what separates the three, and the difference is physical rather than designed:

| | Storage | Fails | Because |
|---|---|---|---|
| **Power** | **none** | instantly. Generation and demand must balance within the period | there is no electricity warehouse |
| **Water** | large | drains a reserve, *then* bites | this is what a water tower **is** |
| **Sewage** | fills | backs up and overflows | treatment has a capacity |

One Bin covers all three, because a Bin is already `[0, capacity]` and a Rule already fails if a Bin *"would go negative **or exceed capacity**."* **Water fails at the bottom of the Bin; Sewage fails at the top.**

What remains a real decision is where the plants go — a Building with a footprint, a Materials cost, a pollution emission, a Land cost, and a labour demand, so siting one is the same argument as siting any dirty industry. Deficit is a **gradient**: a brownout, never a Building that stops working. Sewage overflow spikes the pollution layer and therefore degrades **Fertility**, so a city that under-builds treatment poisons its own farmland with no rule saying so.

**Incident**
A discrete crime, occurring at a Building, dispatching a police response **Trip that can fail**. Incidents are events rather than a field, because a Map Layer names nobody and `Evidence` requires that every figure expand into its constituents.

**An Incident has a victim, a place, and a response. It has no perpetrator.** Not squeamishness — there is no simulated fact that distinguishes an offender from a non-offender, so naming one would assert something the model never computed. Drawing a name at random is variation without a cause, which `adr/0027` exists to forbid; drawing it deterministically means the game accuses the poorest Household in the District, every time. `Evidence` is satisfied regardless, because the constituents of a crime rate genuinely *are* the unemployed Households the rate was derived from.

**Crime**
The Incident rate, derived from **unemployment** — and in this design unemployment is not a property of people. Per `Destitution`, it is a reachability failure the player's network, zoning, and policy produced. So the causal story is *"unreachable jobs cause problems,"* never *"poor people cause problems,"* and the lever is a bus line.

> **Crime reads employment. Never income, never skill tier, never Life Stage.**

Crime is an **entry point to the decline cycle**, not a second decline system: incidents drive Businesses out, which removes jobs and Amenity, which raises unemployment. Damped at the bottom by bid price, exactly as abandonment is — a cycle, not a spiral. Its value over abandonment alone is that it arrives *early*, where abandonment only appears in Districts that are already dead.

**Police suppress the symptom, never the cause.** Response reachability lowers the Incident rate and does nothing to vacancy or to unemployment, so policing a declining District buys a quieter decline rather than a recovery — which is what keeps neglect from being containable. Fire and police are the same shape: one mechanism, dispatch reachability, producing a response Trip and an ambient term.

**Presence counts, and it counts twice.** Safety and **intrusion** are both `adr/0027` Taste axes reading the same quantity — safety saturates with diminishing returns, intrusion does not, and the two curves cross. Nobody authors where. The optimum police density therefore differs per Household and the city cannot satisfy all of them, which is `Policy`'s incidence rule arriving somewhere unexpected.

The same holds for fire, and its consequence lands on the demographic engine: safety-indifferent Households skew away from Family and Mature Family, so an under-protected city quietly loses its **internal generation** and reads it in Replacement Rate rather than in a satisfaction bar. **Under-provide safety and you do not get a worse city — you get a different population.**

**Service coverage is an overlay, not a mechanism.** The Map Layer is composed from the same reachability the Trips use, never from a distance radius the Trips ignore. This is `01-player §7`'s rule that an overlay must never be sharper than the simulation beneath it, applied where it previously did not hold.

---

## Citizens and fidelity

Lineage: the split between persistent record and transient embodiment is Cities: Skylines 1's. The idea that detail is allocated rather than universal is Watch Dogs: Legion's *Census* — but we allocate it to **places**, not people, which no reference game does.

**Citizen**
The persistent record of one person: identity, age, home, workplace, education, current activity. A Citizen **always exists** regardless of fidelity tier. This record is small — tens of bytes — and cheap enough to keep millions of.

**Fidelity**
How movement is computed. Never whether a Citizen exists, and never how it decides.

**Fidelity is a property of place, not of person.** A road Segment is either Microscopic or Statistical; a Traveller inherits the fidelity of wherever it currently is. There is no per-Citizen fidelity, no promotion pool, and no eviction policy.

**The ladder governs vehicular movement only.** A **walk Leg is always Statistical** — pedestrian networks do not saturate at this scale, so `distance / speed` is the exact answer rather than an approximation, and there is nothing a second tier could find. Pedestrians therefore never contribute to Stress. Reopened only if transit is built, since a stop is a queue with a capacity.

| Segment state | Travellers on it | Travel time from |
|---|---|---|
| **Statistical** | Time-advanced. Position interpolable for rendering. | `distance / speed` — free-flow, exact |
| **Microscopic** | Real vehicles: 1-D Lane queues, car-following, junction conflicts | Emergent from simulation |

A single Trip normally spans both, transitioning at Segment boundaries. See `docs/adr/0007-stress-driven-simulation-detail.md`.

**Stress**
The trigger that decides a Segment's fidelity: `volume / capacity × complexity_factor`. A Segment becomes Microscopic above a high threshold and returns to Statistical below a lower one — the gap is **hysteresis**, and without it Segments flicker between regimes as volume oscillates.

Two properties make Stress trustworthy as a trigger:

- **It consumes a count, not a model.** Volume comes from Trips actually in flight; capacity is static. We do not use the volume-delay function to decide where the volume-delay function can be trusted.
- **Its errors self-correct toward detection.** If travel-time estimates *under*state congestion, routing over-uses the Segment, volume rises, and it crosses into Microscopic simulation — the failure feeds the detector.

**Audit**
A small rotating sample of unstressed Segments, selected deterministically by Tick, that are microscopically simulated anyway. Catches failures Stress cannot anticipate — principally junctions that fail at *low* volume because of turning conflicts — and doubles as continuous running validation of the statistical layer.

**Individual Decision**
The rule that every Household and Business evaluates its own choices — where to live, where to work, where to shop — independently, and draws its own outcome. Decisions are **never shared across a group**, even between Citizens with identical attributes.

This is a behavioural commitment, not a performance one. The whole premise of the choice model is that identical Households choose *differently* because of preferences we did not model. Sharing a decision across a group asserts the opposite, and reintroduces the herd behaviour that the choice model's scale parameter exists to prevent. `UNIQUE INDIVIDUALS` `BOUNDED KNOWLEDGE`

When decision cost needs reducing, the levers are **sampling fewer candidates** or **deciding less often** — never deciding collectively.

**Microscopic Cap**
The ceiling on how many **Vehicles** may be under microscopic simulation at once — *not* how many Segments. A Lane's per-Tick cost is one pass over its queue, so a six-lane arterial and a residential street are not one slot each, and occupancy varies most in the direction that binds: a Segment holds the most Vehicles exactly when it is jammed, which is why it was promoted. Counting Segments would make the Cap least accurate when it is doing its job. `adr/0062`

**Nothing is ever evicted to make room.** A full Cap **refuses**: force-promotion is admitted ahead of stress-promotion, because spillback is a correctness criterion and better travel times are an accuracy one; ties break on Stress and then on Segment id; and a refused Segment stays Statistical with its overlay reading *modelled*. The reason is not that the newest stress matters least — that argument was made and withdrawn as an unmeasured claim about the VDF — it is that **a queued Segment holds state nothing can rebuild** (queue position, headway, a Switch Lane traversal in progress) while a Segment that has just crossed the threshold holds none.

*(Formerly **Fidelity Budget**, and renamed deliberately. The corpus already has a **Commute Budget**, which is a gameplay concept the player reads and acts on. Sharing the word let an internal ceiling borrow a game mechanic's authority — which is exactly the error described below. `adr/0007`, `adr/0019`, `adr/0020` and `adr/0025` still use the old name.)*

*(Formerly **Fidelity Budget**, and renamed deliberately. The corpus already has a **Commute Budget**, which is a gameplay concept the player reads and acts on. Sharing the word let an internal ceiling borrow a game mechanic's authority — which is exactly the error described below. `adr/0007`, `adr/0019`, `adr/0020` and `adr/0025` still use the old name.)*

**A world constant** — derived from world configuration, identical on every machine, never player-adjustable. It decides which Segments are simulated, so it changes travel times, route choices, and therefore the city, which puts it under the same rule as Speed: anything the host could vary must not be able to change an outcome. A tunable Cap would mean one Input Log producing different cities on different machines, costing portable replay and headless regression testing. Hardware limits are absorbed elsewhere entirely — a slow machine advances **fewer Ticks per second**.

**Reaching the Cap is not a failure of the city, and is not a trajectory.** What actually happens is that the most congested Segments get their travel times from the VDF, which is structurally wrong there — so *the simulation becomes less accurate exactly where accuracy mattered most.* That is a fact about the simulator, not about the city, and an earlier draft made it a named failure mode in `01-player §6`. It was removed: a failure triggered by a number in a config file fails the project's own test that **an authored constant is acceptable only when it is the same thing the player is shown.**

`HONEST DEGRADATION` is satisfied by a rule that already exists rather than by an event. Under `01-player §7` an overlay must never be sharper than the simulation beneath it, and must mark a modelled number as modelled. Reaching the Cap therefore surfaces as **more of the traffic overlay reading *modelled* rather than *exact***. Nothing needs to be announced.

**Promotion / Demotion**
A Segment changing fidelity, and the Travellers on it changing representation to match. Promotion materialises real vehicles from in-flight Trips; demotion converts them back to arrival times. Keyed on Segment boundaries, so a Traveller only ever changes representation while crossing one.

Because no Citizen record is ever collapsed or discarded, both directions are **reconstructible by construction**: the record plus the current Tick is always sufficient. The invariant that carries the most weight is that **conserved quantities live on the Citizen record, never on the embodiment** — a Traveller is a view, not an owner.

**Traveller**
A Citizen currently on a Trip. Transient, created on demand when travel is required and released on arrival. A Traveller on a Microscopic Segment is a real vehicle in a Lane queue; on a Statistical Segment it is an origin, a destination, and an arrival Tick.

**It is the cursor over a Trip's Legs, and it holds nothing else**: which Citizen, which Trip, which Leg it is on, and when it arrives. The **plan** is the Leg's. Every durable thing therefore sits on a row that outlives the journey and every transient thing on a row that is released — which is what makes *a Traveller is a view, not an owner* hold by construction. See [`docs/adr/0075`](docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md).

**Habit Route**
The route a **Citizen** normally takes between two places, computed from a **slow-moving** cost basis and reused across many Trips. It is the road network's Provider List: `adr/0017`'s sticky incumbent, one actor class further on.

**It is one of several.** Formation computes `k` candidate routes for a pair, and which one a Citizen adopts is drawn from **who that Citizen is** — `hash(world_seed, citizen_id, HabitRouteVariant)` — so two neighbours with the same home and the same job take different roads to it, permanently, with no congestion feedback in the mechanism. The Citizen stores the **index** and not the routes; the route is served from the shared cache, keyed by `(origin node, destination node, variant)`. This is what makes the Provider List row of `adr/0046`'s own table true: a list of one member cannot be switched, only discarded, and every other actor in this city switches. It is also the only structural supply of **route diversity** — without it one shared cost basis returns one route per pair and the city's traffic concentrates on a fraction of its road. See `docs/adr/0060`.

**It belongs to the Citizen, never to the Traveller**, and this entry said otherwise until session M. A thing *reused across many Trips* is conserved across embodiments, and a Traveller is released on arrival — so storing it there would break this file's own rule that **a Traveller is a view, not an owner** (see *Promotion / Demotion*). A Traveller reads its Citizen's Habit Route; it never holds one.

A Habit Route is **deliberately out of date about congestion, and only boundedly out of date about the network** — two different kinds of wrong, and conflating them cost session M a day. Nothing recomputes it because a road got busy; that is what Sight is for, and `adr/0046`'s **static Habit** is ratified over exactly that case and no other. A road being **built or demolished** is not staleness at all — the route is wrong about what *exists*, which no amount of local self-correction repairs. What a Habit Route is permitted to be wrong about, and for how long, is stated in `adr/0012`. See `docs/adr/0046`, `docs/adr/0012`. `BOUNDED KNOWLEDGE`

**Sight Horizon**
How far ahead a Traveller can see live conditions: the number of Segments along the Habit Route whose current cost it reads before choosing which arc to take out of a junction. Beyond the horizon it falls back to its lagged expectation of the rest of the journey.

**Its floor is a property of the Road Graph, not a preference.** A Traveller looking fewer Segments ahead than the distance to its next *branching* node receives a signal it cannot act on — it will still be committed to the corridor when it reaches the jam.

**And its ceiling is the same number, so it is derived rather than tuned: the Sight Horizon is 1 Segment.** A driver *has* its own route, so it can read live cost `N` Segments along it; it has no route down an alternative beyond the first arc, because nothing searches. At 1 the comparison is symmetric — one live arc against one live arc — and above 1 it is not, biasing diversion by an artefact of what the driver holds rather than of what the road is. `adr/0046`

**Do not confuse it with the Rejoin crossing budget**, which is a radius around a route a Traveller has *left* rather than a lookahead along one it is still on. Those are the two parameters this name was wearing; the first is 1 and derived, the second is unset. `adr/0061`

**Diversion / Rejoin**
A **Diversion** is a Traveller leaving its Habit Route at a junction, because Sight found an alternative arc better by more than its Temperament. A **Rejoin** is what it does next: it returns to the Habit Route rather than working out a new one. **A Rejoin is not a search.** The Traveller carries a **Rejoin Target** — the node on its Habit Route that it declined to enter — and at each following junction applies the rule it already has, taking the arc that reduces graph distance to that Target, until it is standing on the route again.

**The whole point is that a Diversion costs a decision and never a route.** Sight makes diverting *routine* rather than exceptional, so anything a diverting Traveller does is paid at the rate the city diverts, not the rate it departs; a mid-journey re-search at that rate is the largest number in the corpus. Nothing here is a cheaper search — there is no search.

**A Rejoin that does not succeed does not search either.** The Traveller stops aiming at the Target, points the same rule at its destination and carries on; the cost arrives as minutes, which the **Commute Budget** already scores, and there is no Trip Fate for a lost driver. **A Rejoin is abandoned** when no arc out of the Node it stands on reduces the straight-line distance to its Target — a test that is wrong exactly where the map is deceptive, across a river or a severance, which is `BOUNDED KNOWLEDGE` behaving correctly rather than a defect.

*This condition was called **stranded** until session F, which is the name of a **Trip Fate** — one word over two conditions with opposite consequences, in the same file. Abandoning a Rejoin ends a rule, not a journey; the Traveller carries on and the Trip is unaffected.* See `docs/adr/0061`, `docs/adr/0046`, `docs/adr/0012`. `BOUNDED KNOWLEDGE` `SOLVE THE ACTUAL PROBLEM` `HONEST DEGRADATION`

**Aggravation**
The fraction of a Citizen's recent journeys on a Habit Route that diverted or stranded. Crossing its threshold makes the Citizen **switch to another variant** of that Habit — it never recomputes a route.

**A fraction and never a tally**, for `adr/0053`'s reason: a count is not scale-free, so somebody who drives four times a Day would switch four times as often as somebody who drives once, and one Ruleset number would mean four different things. It **drains to zero on a switch**, which is what gives it a sink.

**It is not an adaptive Habit**, and the distinction is exact: an adaptive Habit recomputes a route against a cost basis; this chooses differently among candidates that were computed once and never change. Nothing here reads a live cost to build a route, so `adr/0046`'s static Habit is untouched. See `docs/adr/0061`.

**Temperament**
A Citizen's threshold for how much better an alternative must be before it is worth diverting to — `adr/0017`'s *"substantially better… by enough to be worth the bother"*, given a number at last.

**A stable base plus per-decision jitter, and both halves are load-bearing.** The base is character and persists for life; the jitter is what kind of morning this is. Without the base nobody is ever *the sort of person who takes the back roads*; without the jitter the same driver takes the same decision on the same data every Day and the flow re-synchronises. Two `purpose_tag`s, never one.

Temperament is what keeps congestion response from being a **herd**: an identical rule over an identical input diverts everybody at once, jams the alternative, and diverts everybody back for ever. It is the case where `UNIQUE INDIVIDUALS` is what makes the city *work* rather than what makes it interesting. `EMERGENCE`

---

## Movement

Lineage: lane-as-entity and switch lanes are Citybound's; the commute budget is SimCity 4's.

**Trip**
A journey with a **Trip Purpose**, an origin and destination **Address**, an ordered sequence of Legs, and a **Trip Fate**. Trips are first-class objects, not transient calculations, because a failed Trip must be reportable. `LEGIBLE CAUSE`

It also records **which Leg failed**, which is the difference between *no route found* and *no route found on Leg 1 of 3, on foot, from here to there*.

*Spell the purpose **Trip Purpose** and never abbreviate it: `purpose_tag` is the counter-based RNG tag, an unrelated concept one word away.*

**Leg**
One mode-homogeneous segment of a Trip: a mode, two **Addresses**, a travel time, and the next Leg. **Walking and driving are both implemented Legs**; transit is a Leg type that may or may not ever be added. A car commute is never fewer than three Legs — `walk → drive → walk` — because Buildings connect to the pedestrian network rather than directly to the Road Graph.

**A Leg is the *plan* and a Traveller is the *cursor*.** The Leg holds what was decided; the Traveller holds where it has got to and when it arrives. That division is what makes *a Traveller is a view, not an owner* true by construction rather than by discipline.

**A Leg stores a cost, never a path.** A walk Leg's route is searched, `distance / speed` is taken, and the Segment list is discarded — pedestrians contribute no volume and no Stress, so nothing reads it. A drive Leg's path lives in the shared route cache off the **Citizen**'s Habit Route index, which is where the design has already twice decided a route belongs.

**Every Leg of a Trip is created at once, when the Trip is created**, because a Trip that cannot be read until it finishes is not reportable and cannot be counted.

The multi-Leg structure exists from the first line of code. Retrofitting it is not an incremental change, and walking rather than transit is the decision that makes it irreversible. See [`docs/adr/0008`](docs/adr/0008-walking-is-a-simulated-leg.md), [`docs/adr/0075`](docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md).

**Transit**
A network the player draws whose output is **reachability**, not coverage. A transit stop is not a destination — nobody's Need is met by arriving at one — it is an **Access Point onto a different network layer**, which is why Transit is `Connect` in placement and `Govern` in operation, and is not a Service.

**The player sets vehicle count; frequency is emergent.** Frequency is `vehicles ÷ round-trip time`, and round-trip time is measured over the same Segments cars use — so buses caught in the congestion they were meant to relieve deliver fewer departures than were paid for, and bunching needs no rule. A directly-set frequency would oblige the simulation to conjure vehicles precisely when the network is worst.

Every scarcity in Transit is a **gradient**: an underfunded line is slow rather than broken, and a full vehicle is the next one rather than a refusal. Waiting is Leg cost, spent against the **Commute Budget** like any other travel — which is why stops need no fidelity tier of their own, and why `adr/0008`'s multi-Leg structure was the load-bearing prerequisite.

**Transit only pays at density.** A line through sprawl carries nobody and bills the treasury forever. This is what keeps it a decision rather than a reward. `NO VERDICT`

**Severance**
A part of the city made unreachable on foot by infrastructure — most often an Arterial with no crossing splitting a neighbourhood from the shops and stops that served it. Severance is **emergent, never scripted**: Arterials simply carry no pedestrian edges except at authored junction pieces, so the walk route genuinely does not exist.

It is the clearest payoff of treating walking as real. A city can be perfectly well connected for cars and broken for people, and the game can say so. `LEGIBLE CAUSE`

**Address**
**A location on the Road Graph: a Segment, an offset along it, and which side of it.** Never a Node. It is the value every query about *where something is* takes and returns — a Building's Access Point is a Building's Address, a Leg runs from one Address to another, and a parking Bin will have one.

**The word is chosen because a street address is literally this triple**: a distance along a street plus an odd or even side. The **side** is left or right of the Segment's forward direction, which is fixed A→B by its endpoints, so it needs no geometry and no coordinate — the simulation still never sees a spline.

**An Address is an offset along a Segment, never a Node.** A Segment's Nodes are intersections and an Address is not one. The arithmetic makes this structural rather than a matter of taste: five Buildings share a Segment at the working figures, so promoting Addresses to Nodes would split every Segment five ways and put the Road Graph at 150,000–300,000 Segments instead of ~30,000. **A routing query is therefore `Address → Address` rather than node-to-node, which is the query shape everything downstream must be measured on.**

**Side of street is here rather than in the graph**, and that is a decision rather than an omission. A crossing is a **cost term** applied when two Addresses share a Segment and differ in side — exact for the across-the-street case that walkability turns on, and silent elsewhere, because *the same side* stops meaning anything once a route turns a corner. Two footway edges per Street would express more and cost a tripled graph, one Epoch and one search. See [`docs/adr/0074`](docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md).

**Access Point**
**Where a Building meets a network: a Building's Address.** Every Building has a **pedestrian** one and a **vehicle** one, and today they hold the same Address.

The distinction is load-bearing rather than pedantic: a car's real access point is **wherever it managed to park**, which is generally not its destination. The gap between the two is the walk Leg, and its length is the whole of what parking scarcity does to the player. They diverge when parking acquires a location and when freight needs a loading kerb.

⚠ **The subdivider was the third entry on that list until 5a-bis shipped, and it belongs on it no longer.** `LotSubdivider` derives **one** Address per Lot, from the Lot's own saved position and side — a second would need a second saved fact, and inventing one for a consumer that does not exist is what `adr/0070` forbids. So *the two hold the same Address* is the **built** behaviour rather than an interim simplification, and the divergence list is two entries, both in later milestones. The correction is recorded rather than deleted because the entry was written before the subdivider existed and was a perfectly reasonable guess about it.

⚠ **An Access Point is `(derived AND rebuilt)`, not saved** ([`docs/adr/0078`](docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md)). What is saved is the **Lot's position and side**, which is a place on the ground; the `(Segment, offset)` pair is a function of the graph and is rebuilt on the Epoch. Saving it is not a choice, and the reason is **staleness rather than danger**: frontage is a function of the graph, so laying a *new* Street can give a Lot a better front door without touching the old one — a saved copy would be simply **wrong**, with nothing invalidated and nothing to notice. That is the argument that derives a Bin's capacity (`adr/0064`) and a Building's occupancy (`adr/0068`), reaching a third row. The front door is still a property of the city rather than a cache — the part of it that is a property of the city is the part that is saved.

⚠ *A saved `Handle` to a bulldozed Segment would **not** dangle, and the distinction matters because it is the mechanism a Leg depends on.* Freeing a row bumps its generation, so an old handle fails to resolve rather than silently addressing the next road laid in that slot; `Reference.Severable` is the declaration for a target the design expects to disappear. That is why a **Leg**'s endpoints *are* saved handles — a Leg's plan cannot be re-derived, since the graph state it was searched over is gone (`docs/adr/0075`) — while an Access Point, which can, is not. **What must never be saved is a bare slot**, which carries no generation and would resolve to whatever occupies it next.

⚠ **The Parking Shed is queried around the *pedestrian* Access Point, not the vehicle one**, so the vehicle Access Point has no consumer until parking exists. It is modelled anyway, because `adr/0008`'s third consequence exists precisely so that parking does not restructure the Building later. **The vehicle Access Point is never a fallback from a failed Shed query** — a full car park must not cost less than an empty one. See [`docs/adr/0074`](docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md), [`docs/adr/0008`](docs/adr/0008-walking-is-a-simulated-leg.md).

**Parking Shed**
The set of parking Bins within acceptable walking distance of a destination's pedestrian Access Point. On arrival it is queried **nearest-first**, taking the first Bin with free capacity — a handful of lookups, never a search.

Scarcity widens the shed rather than blocking the Trip: you park further away, the walk Leg grows, and the Trip fails only if it exceeds its Commute Budget. There is deliberately **no *no parking* Trip Fate** — pressure arrives as a gradient of rising walk times before it arrives as failure. `HONEST DEGRADATION`

See `docs/adr/0009-parking-is-modelled-supply-never-search.md`.

**Trip Fate**
The recorded outcome of a Trip: *completed*, *no route found*, *exceeded commute budget*, or *stranded* (the network changed mid-journey). Never silently discarded.

**The set is closed at four, and the rule that closes it has two clauses.** **A Fate names how the *journey* ended** — so anything that fails at the far end is another object's outcome and the Trip **completed**. And **anything that arrives as *time*** is scored by the **Commute Budget**, which is not a Fate. Every candidate fifth the corpus has met falls to one clause or the other, which is why it has been refused three times by three authors reaching independently for the same argument: `adr/0067` (*"the Trip completed; what failed is the purchase"*), → Parking Shed (*"deliberately no *no parking* Trip Fate"*), and → Diversion (*"there is no Trip Fate for a lost driver"*). **A fifth Fate is always a proposal to record something twice**, somewhere less diagnosable than where it already is.

**A destination demolished mid-Trip needs no new Fate**, and it is the common case rather than a corner under any Ruleset that condemns Buildings. If the **Segment** is gone the Trip is *stranded*; if the **Building** is gone and the Segment is fine the Trip **completed** and the purpose failed at the far end — because an **Address** outlives its Building, so a Traveller can genuinely arrive at a plot of rubble.

*Stranded* is this Fate and nothing else. The lost-driver condition that once shared the word is **a Rejoin being abandoned**, which is not a Fate. See [`docs/adr/0076`](docs/adr/0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md). `LEGIBLE CAUSE` `NO VERDICT`

**Commute Budget**
The maximum acceptable cost of a Trip. A Trip exceeding it **fails**, and a Building whose Trips keep failing declines and is eventually abandoned. This makes geography matter, bounds pathfinding work, and is legible to the player. **The cost function must be the same quantity the player is scored on** — optimising routes for distance while judging the player on time is what made SimCity 4's traffic system unlearnable.

**One currency, and it is clock minutes.** Walking minutes and driving minutes count the same, and there is **no per-mode weight**. People do not value the two equally — walking time is worth roughly twice in-vehicle time in the literature — but a weight inside the Budget would make the scored quantity differ from the **displayed** one, which is the unlearnability failure this entry exists to prevent, arriving through the door it locked. **Distaste for walking belongs to the choice model**, on a Provider List entry's mode, one layer up from the cost function. See [`docs/adr/0008`](docs/adr/0008-walking-is-a-simulated-leg.md).

**Lane**
The atomic unit of road, and the entity that owns its Vehicles as a **sorted one-dimensional queue**. A Lane updates all of its Vehicles in one pass. Cars are not independent entities and do not hold references to each other.

**Overlap**
A declared relationship between two Lanes that physically interact — parallel, opposing, or crossing. Overlapping Lanes exchange their Vehicles' projected positions each tick as obstacles mapped into each other's coordinate space. This is how two-dimensional traffic behaviour emerges from one-dimensional queues.

**Switch Lane**
An invisible Lane spanning the Overlap between two parallel Lanes, where lane changes occur. The two normal Lanes connect to the Switch Lane rather than to each other. This turns merging — the hardest special case in traffic simulation — into an ordinary object with ordinary rules, including merges the driver aborts when required braking exceeds comfort.

**Street**
A grid-snapped road. The common case. Intersections between Streets are trivial and the Road Graph falls out of the Tile grid directly.

**Arterial**
A freeform spline road — highway, rail, major boulevard. Deliberately rare. Arterial-to-Arterial and Arterial-to-Street connections use **authored Junction pieces** rather than procedurally generated geometry, which is what confines the computational-geometry cost that consumed Citybound.

**Junction**
A pre-authored connection piece (cloverleaf, diamond, trumpet, on-ramp) placed by the player where Arterials meet.

**Road Graph**
The routing abstraction: nodes and edges, uniform regardless of how a road was drawn. **The simulation never sees a spline.** Geometry is a rendering concern.

**One graph, with mode masks.** Pedestrian and vehicle edges are the same structure, tagged by which modes may traverse them — not two parallel networks. This gives one Epoch covering both, one revalidation path, and a multi-Leg Trip routed by a single mode-aware search rather than stitched across two structures. It is also what makes **Severance** emergent: nobody deletes a pedestrian route, the mask simply never granted one.

**Node**
**A vertex of the Road Graph: a point where Segments meet and a route may branch.** For Streets the Nodes are intersections and fall out of the Tile grid directly; for Arterials they are the authored **Junction** pieces.

**A Node is where a choice exists, and that is what it is for.** It is the unit the routing machinery is keyed in: a route cache entry is `(origin node, destination node, variant)`, a **Rejoin Target** is the Node a diverting Traveller declined to enter, the **Sight Horizon**'s floor is the distance to the next *branching* Node, and `adr/0040`'s pathfinding cluster is a set of them.

**Nothing in the city is located at a Node.** A Building's Access Point is an **Address** — an offset along a Segment — and never a Node, because promoting places to Nodes would split every Segment by the number of Buildings on it. So the graph's vertices are junctions and only junctions, which is what keeps the Segment count at its working figure.

_Avoid_: "vertex", "intersection" as a synonym — the first is the graph-theoretic word `05` may use and design prose may not, and the second is true only of Streets.

**Segment**
**An edge of the Road Graph: one run of road between two adjacent Nodes.** For Streets the nodes are intersections and both fall out of the Tile grid directly; for Arterials they are the authored Junction pieces. A Segment carries the attributes every other system reads off it — capacity, free-flow speed, mode mask, current volume, and its **Fidelity**.

It is the unit almost everything about movement is counted in, which is why it needs stating precisely. **Fidelity is a property of the Segment**, not of the Traveller on it; the **Microscopic Cap** counts **Vehicles** and not Segments (`adr/0062` — this entry said otherwise, and a list of things counted per Segment is precisely how a wrong unit propagates unnoticed); the **VDF** is evaluated on one Segment's own `volume / capacity`; **Stress** is that ratio times a complexity factor; and `adr/0035` prices **Upkeep** against capacity and free-flow speed, which are Segment attributes. A Microscopic Segment owns **Lanes**; a Statistical one has none at all.

**It is not a Tile and it is not a whole road.** A Tile-length edge would put millions of them on a 4096² map, and a run between authored Junctions would put almost none on a city that is mostly Streets — both by more than an order of magnitude. The working figure is **~30,000 Segments at 1,000,000 Citizens, about four Lanes each**, which puts a Segment at roughly a block-length link. That figure rests on a road-density assumption nothing in this corpus has yet argued, and it is spike **S2**'s to replace.

**Walking does not add Segments.** The mode mask is *an edge property, not a second edge set* (`03 §3.7`), so a Street's footway is the same Segment with the foot bit set, and the pedestrian network is a **subgraph** rather than an addition. The figure is therefore not inflated by `adr/0008`'s walk Legs. What *is* additional and unsized is the small set of **foot-only Segments** — crossings at authored Junction pieces, paths, pedestrian precincts. They are few and they are the edges **Severance** turns on, so nothing may size the graph by omitting them.

_Avoid_: "road", "link", "edge" as loose synonyms — the first two are ambiguous between the Segment and the whole street a player drew, and "edge" is the graph-theoretic word for the same object and is fine in `05` but not in design prose.

**Arc**
**One permitted direction of travel along a Segment.** A Segment has two, and each carries the **mode mask** valid in that direction; the Segment's own mask is their union.

**The Arc rather than the Segment is where a mask lives, and a one-way street is why.** It carries cars one way and pedestrians both, so a single mask per Segment forces a choice between a second edge set for foot — the two parallel networks this design rejects by name — and a street nobody may walk down. Holding the mask on the Arc is still *one graph, one Arc set, one Epoch*; the Segment's mask stays meaningful for every reader asking *what is this road for* rather than *may I go this way*. See [`docs/adr/0072`](docs/adr/0072-the-mode-mask-is-saved-on-the-arc-and-the-segments-is-derived.md).

**It is derived, and it is the only part of the Road Graph that is.** An Arc is a function of the Segments — its target is one of their endpoints, its mask is that direction's, its traversal cost is a division of length by speed — so it is rebuilt rather than saved and never reaches the State Hash. That is what `adr/0040` means by the abstract routing structure being free to change forever.

**Why the graph is directed at all**: the VDF is evaluated on one Segment's own `volume / capacity` and Lanes are directional queues, so `cost(A→B) ≠ cost(B→A)`. Three things follow and none is a local retrofit — a route cache key is an *ordered* node pair, a travel-time matrix is asymmetric and nothing may halve it by symmetry, and the adjacency stores two Arcs where an undirected graph would store one edge.

_Avoid_: "half-edge", "directed edge" — the same object under the graph-theoretic names `05` may use and design prose may not.

**Volume-Delay Function** (VDF)
The formula that estimates a Segment's travel time from how busy it is — the standard being BPR, `free_flow × (1 + α(volume/capacity)^β)`. Cheap, and the mechanism behind every Statistical Segment.

It is **exact when free-flowing and wrong when saturated**, and the reason is structural rather than a matter of calibration: it is a memoryless function of one Segment's own `volume/capacity`, so it cannot represent queueing, spillback from the Segment ahead, or a jam persisting after the volume that caused it has gone. That single limitation is what the Microscopic tier exists to cover, and it is why the two tiers are *expected to disagree* exactly where one of them is in use.

**Epoch**
A monotonically increasing version counter on the Road Graph, bumped on any edit. Cached routes record the Epoch they were computed under and revalidate lazily on next use. Never a global flush.

**"Never a global flush" is a claim about *when you pay*, never about *what survives*, and the two must not be read as one.** A single counter for the whole graph carries no location, so a route cannot tell whether an edit touched it — every edit anywhere invalidates everything, merely lazily. Since a Road Graph edit is the player's core verb rather than a rare fault, **the granularity of the Epoch decides whether route caching is worth anything at all.** Whether it is per-graph, per-cluster or per-Segment is a pure optimisation under `05 §4` — every granularity is conservative and recomputation is deterministic, so the State Hash is identical across them — and spike **S2** settles it by measurement.

---

## Simulation mechanics

**Tick**
The atomic simulation step. An unsigned integer counter. The simulation core has no concept of wall-clock time — the host decides when to advance it. This is what makes fast-forward, headless testing, and replay free.

**The World holds it, as a saved and hashed column** — a Simulation is the loop and the Tick is state, so nothing has to be told what time it is (`adr/0058`). The stored value is the *next* Tick to run.

The Tick is fine-grained **because of traffic and nothing else**. Every other process in the simulation is a discrete event that the Event Wheel can schedule at any granularity; car-following is the only continuous one, and it is what sets the resolution everything else inherits.

**Day**
The period of a Household's routine, and the only time unit above the Tick. There is **one clock**: rush hours, building decline, Household departure, and growth all run on it. There is no calendar, no year, and no second slower time base for growth — a conversion factor between two time scales would break the literal truth of statements like "this shop closed because its customers' commutes got too long." See `docs/adr/0010-one-clock-and-demographics-by-sorting.md`.

A Day is a fixed number of Ticks, set at world creation. Note what this makes it: **the Day is not a unit the simulation converts to, it is a duration the simulation contains**, so the ratio between a commute and a Day is a real dimensionless fact about the world — and it *is* the traffic balance, because share of life spent in transit is the same quantity as share of the population on the road. Lengthening or shortening the Day is therefore a balance change, never a pacing change. See `docs/adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md` and `docs/02-simulation-model.md` §1.2.

**Speed**
How fast the host advances Ticks in real time. Purely a host concern — the simulation cannot observe it, so no speed setting can change any outcome. Speed is where **all** pacing lives: session length, how long the player has to react to a mistake, and how much of a city's growth fits in one sitting.

The default is not the slowest setting. The slowest, **Study**, is the speed at which rendered traffic is visually truthful; above it, vehicles read as faster than their apparent size warrants. Mechanics are identical at every speed.

**Event Wheel**
The bucket structure that lets idle entities cost nothing. Every scheduled row carries a `next_event_tick`; buckets are keyed by that value. A Citizen at work for a third of a Day sits in exactly one bucket and is touched once. This converts cost from *number of Citizens* to *number of Citizens with something happening right now*.

**It has two levels** — a fine wheel of one bucket per Tick and a coarse wheel of one bucket per Day, the coarse cascading into the fine at each Day boundary (`adr/0056`). The fine wheel's period is exactly one Day, so anything sleeping in Days is the coarse wheel's. **There is one wheel per scheduled table**, not one wheel over tagged entities.

A scheduled row is in exactly one of **armed** or **waiting** at every moment, and is unlinked when its owner row is freed. That is what bounds the structure under `adr/0006`: membership is a *partition* of the live rows rather than an accumulation, so the wheel cannot grow with elapsed time.

**Input Log**
The record of `(world seed, configuration, Ruleset content hash, inputs per Tick)` that fully determines a session. Small enough to attach to a bug report. Everything that affects simulation state must enter through it.

**The Ruleset content hash is a fourth member rather than part of the configuration**, and the distinction is the whole reason it is named separately: configuration is set at world creation and baked into the save, while the Ruleset is hot-reloadable, so it can change *within* a run. A reload therefore appears in the log as a transition carrying **both** hashes.

**That sentence used to end *"— a replay needs the Rules' content, not the news that they changed"*, and it argued against the clause it was attached to.** A hash **is** the news that they changed; it is not the content. The two are separated by where they travel: the **Input Log carries hashes**, because it is shared between people who have the Rulesets in a repository, and the **crash artifact carries the content**, because it is attached to an issue by somebody who may not. Only a developer ever reloads — a player never does — so a multi-reload log is a developer's artefact and the artifact is the thing that travels to strangers. `adr/0048` It is the **content** and never the name or the path: a replay run against a different Ruleset is a different simulation and will diverge, which is arithmetic rather than a bug, so the headless runner refuses it outright instead of reporting a divergence it caused itself.

Note what this implies about Fidelity: nothing about the camera appears here, and nothing needs to. Fidelity is *derived* from simulation state the Log already determines, so a replay reproduces the same Microscopic Segments without ever recording which ones they were. An earlier design made the focus point an input precisely so it could be replayed; making fidelity a consequence of the simulation rather than of observation removed the input altogether.

**State Hash**
A hash of the entire simulation state, taken every N Ticks. Two runs of the same Input Log must produce identical hash sequences. When they diverge, the first differing hash identifies the exact Tick a bug entered.

**Census**
A periodic reading of every collection's size and of what the Rule engine did, kept in a fixed ring, and the history that `series(metric, window)` is answered from. `adr/0006`'s constraint — no collection may grow as a function of elapsed game time — is a claim about a *run* rather than about a moment, so nothing that inspects a single Tick can see it violated. A series can.

**It reads three counters per table, not one, because they fail differently.** Live rows are the city's size and on their own are evidence of nothing; *slots* only ever rise when a create finds the free list empty, so **slots climbing while live is flat** is the leak with the population held constant — invisible in a row count. Capacity is the third because it is the one that costs memory.

**It reads two kinds of number, and the difference is what a metric's family names.** A table counter is a **level**: read it at any cadence and it means the same thing, because it is the size of something that exists. A Rule counter — *due Rule Instances*, *evaluations*, *chain rungs* — is a **flow**: it has no value at an instant, only over an interval, so it is accumulated across every Tick, read as a **sum and a peak** over the interval since the last reading, and *drained by the reading*. Sampling a flow the way a level is sampled would report one Tick in sixty-four of a quantity `02 §4` makes deliberately bursty; the peak is what a per-Tick budget is held against and the sum is what a run cost. **The accumulator sees every Tick even when it is read every sixty-fourth**, which is why there is one census cadence and not a finer second one for flows.

**The ring is finite by construction, since the alternative is the defect it detects.** A census that appended a reading per cadence for the length of a run would itself be a collection growing with elapsed game time. The oldest reading is overwritten, and that overwriting is the sink. A window reaching further back than the surviving history is answered over the part that survives and **marked incomplete** — a silently shortened window would let a reader conclude *flat over the whole run* from its tail.

It belongs to a **run**, never to the World: a world has no history until something steps it, and an instrument sitting in simulation state would be one the State Hash and the save each needed an answer for. A Census never changes the city it measures.

**Crash artifact**
What a panic produces: the last checkpoint, the Input Log since it, the Ruleset content hash actually in force, and the Tick the panic landed on. **It is a reproduction, not a dump.** A dump lets you inspect the aftermath; this lets you replay to the Tick before and single-step into the failure under a debugger, as many times as you like. Small enough to attach to an issue.

It costs no new machinery, which is `adr/0037`'s doing: crash forensics used to be justified by the Past/Future double buffer — *a Tick that panics while computing the Future leaves the Past intact* — and deleting that buffer made the guarantee **stronger**, because determinism plus the Input Log reproduce the failure rather than merely preserving its corpse.

The file is a header and then a Log, verbatim, so cutting it at the separator yields a replayable one and no tooling is needed to get there — which matters most here, because this is the artefact written at the moment tooling is least trustworthy. The runner accepts it wherever it accepts a Log, since replaying it is the only thing anybody wants to do with one.

Before milestone 10 there are no checkpoints, so the reproduction starts at world creation and the artifact is the seed plus the whole Log — equivalent, and smaller. The field is written anyway, so that milestone fills one in rather than replacing a mechanism.

---

## Pressure and difficulty

**The Bill** / **The Clock**
The two axes every failure resolves into, separated by whether money can solve it. **The Bill** is everything price-constrained — Goods, Materials, Food, Land, road capacity — all of which the Outside Connection will supply at a cost, so scarcity arrives as an expense. **The Clock** is everything rate-constrained — people, and the skills they carry — where a Hinterland recovers at a rate, a Life Stage takes Days, and no amount of money goes faster.

Only the Clock can genuinely stop a city, which is why the labour pipeline is the hardest constraint in the design.

**Intensity Dial**
The player's control over the world's terms, in three sub-dials: the Bill (Hinterland price levels and lending rate), the Clock (Hinterland depth and recovery rate), and Acts of God (the frequency interval for Disasters).

> **The dial sets the terms of trade with the world outside. The difficulty inside the map is authored by the player.**

Which means it **does not change the cost of a mistake, only the cost of recovering from one.** Every parameter it touches lives outside the map, so no system in the city is written against it — and it can never touch an instrument. Detection, notification, and `Evidence` report the same truth at every setting: *the dial makes the city harder, it never makes the game less honest.*

**Mode**
A named preset over the Intensity Dial, plus a lock policy governing whether the player may adjust it afterward. Chosen at world creation and fixed for the world's life — the design's only irreversible player-facing choice, and the price of a lock meaning anything. A lock is always **opted into, never imposed**.

**Shock**
A movement in a Hinterland's authored figures — prices, wage, rent, population, composition — and nothing else. Slow-onset, may be favourable, and propagates only through chains the city already has. Tests the city's **exposure**: how much of its economy runs through one edge.

**Disaster**
A sudden perturbation with a bounded footprint of Tiles. **World-scheduled** — timing and place are a function of seed and Tick over precomputed Hazard Regions, with no reference to what is standing there. Disasters are not aimed at the player; a city's exposure to one is something the player authored by siting.

A Disaster tests **redundancy**, which is the only thing that can: two cities differing in nothing but a spare route are identical on every overlay until one is needed. Its initial footprint is small and fixed — **the city sets severity**, because containment is an ordinary Trip that can fail, and everything a Disaster does is an existing verb (a Segment out bumps the Epoch, a destroyed Building vacates its Lot, a fire spikes a Map Layer).

**Hazard Region**
Ground where a Disaster can occur, derived from terrain at world generation and shown as an ordinary overlay from the first Tick. Never read during a Tick, so `adr/0021` holds. Its purpose is to make risky land a **decision with a posted price** rather than an ambush.

---

## Terms we deliberately do not use

- **"Agent"** — too vague. Say Citizen (the record), Traveller (the embodiment), or Vehicle.
- **"Cohort"** — removed. It named a third fidelity tier that collapsed Citizens into a shared record with a count. Rejected: see `docs/adr/0005-two-fidelity-tiers.md`. There is no group-level Citizen representation anywhere in this design.
- **"A Detailed Citizen" / "a Statistical Citizen"** — a category error since `adr/0007`. Citizens do not have fidelity; Segments do. Say "a Traveller on a Microscopic Segment," or just name the Segment.
- **"Year" / "month" / "season"** — there is no calendar. Say Day. Anything that would naturally be annual must be expressed in Days or must not exist.
- **"Hour" / "minute"** — there is no clock face either. Time of day is a **sun arc** with named phases (dawn, morning peak, midday, evening peak, night), and durations are Ticks internally and arc wedges on screen. An hour would not land on a Tick boundary, and more importantly an arc makes no numeric claim, so it cannot be caught contradicting what the player is watching. Say Tick, Day, or a phase name.
- **"Unit"** — collides with RTS usage. Say Building.
- **"Demand"** — there is no global demand scalar in this design. Say what actually drives growth: reachable jobs, delivered Goods, satisfied Needs. A "recession that shifts demand parameters" is the RCI bar wearing a new name; say what moved in which **Hinterland**.
- **"Tax tolerance"** — no such scalar. Tolerance is emergent: a Household compares the city against a Hinterland using the same utility function everyone uses, and leaves or does not. Any named tunable that is really an outcome of the choice model should be suspected of the same error.
- **"Difficulty modifier"** — nothing in the city is scaled by intensity. The **Intensity Dial** acts only on the world outside the map. A proposal to make construction slower or decline steeper at a higher setting is a proposal to abandon that.
- **"Sim"** as a noun for a person — say Citizen.
- **"Entity"** — used only when genuinely speaking about the storage layer in the abstract.
- **"Region"** as a set of separately-saved city tiles — there is one world and one Tick counter. Say Settlement for a commute shed, District for a Goods-pooling region, or World for the whole map. The SimCity 4 model is foreclosed by `docs/adr/0020-one-live-world-and-settlements-are-derived.md`, on the grounds that frozen neighbours are a second clock.
- **"Resource deposit" / "resource map"** — the generator places Woodland and nothing else. Fertility is composed from what the player has done to the ground, not dealt. Say Woodland, or Fertility.
