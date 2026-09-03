# The Ruleset key reference

**Generated from `RulesetLoader` by `--key-reference`. Do not edit it** — `RulesetReferenceTests` fails when this file drifts from the loader, exactly as `RulesetSchemaTests` does for `rulesets/ruleset.schema.json`. Regenerate with:

```
dotnet run --project src/Borough.Headless -- \
  --key-reference --ruleset rulesets/minimal.toml > docs/ruleset-reference.md
```

---

## What this is, and what it is not

**The key set is derived and the sentences are authored.** Which keys exist comes from the loader's own record of what its readers asked for, so this page cannot list a key the loader does not read. What each key *does* is written by hand in `src/Borough.Formats/RulesetKeyNotes.cs`, and a test refuses both a key with no sentence and a sentence with no key.

⚠ **It states no values, no defaults and no ranges.** The loader carries the range and delivers it in the refusal, at the moment an author is wrong, which is the only moment it helps. Each file in `rulesets/` carries its own header saying what that file demonstrates. [`plans/0002`](../plans/0002-open-questions.md) §D carries whether a number is ratified — and **nearly every number in this design is not**.

⚠ **It cannot be complete, and the reason is structural.** A reader only ever asks for a table that exists, so a section no shipped Ruleset declares is invisible to the loader's record and therefore to this page. What is listed here is read; what is absent may still be read by a file nobody has written.

⚠ **A listed key is not necessarily a permitted one.** Keys the loader knows only in order to refuse them by name — so an author is told where a key went rather than that it is unknown — are subtracted from this surface and do not appear.

**The loader is the authority. This is orientation.**

---

## The sections

34 sections, 173 keys.

- [`[[band]]`](#band) — 2 keys
- [`[[building]]`](#building) — 13 keys
- [`[[building]] bins`](#building-bins) — 3 keys
- [`[[business]]`](#business) — 5 keys
- [`[[hinterland]]`](#hinterland) — 4 keys
- [`[[hinterland]] prices`](#hinterland-prices) — 2 keys
- [`[[lattice]]`](#lattice) — 2 keys
- [`[[life_stage]]`](#life_stage) — 12 keys
- [`[[policy]]`](#policy) — 5 keys
- [`[[policy]] apply`](#policy-apply) — 4 keys
- [`[[policy]] transfer`](#policy-transfer) — 4 keys
- [`[[resource]]`](#resource) — 3 keys
- [`[[rule]]`](#rule) — 9 keys
- [`[[rule]] apply`](#rule-apply) — 4 keys
- [`[[rule]] inputs`](#rule-inputs) — 3 keys
- [`[[rule]] outputs`](#rule-outputs) — 4 keys
- [`[[terrain]]`](#terrain) — 3 keys
- [`[[zone_rule]]`](#zone_rule) — 7 keys
- [`[capacity]`](#capacity) — 3 keys
- [`[disasters]`](#disasters) — 3 keys
- [`[districts]`](#districts) — 4 keys
- [`[founding]`](#founding) — 2 keys
- [`[households]`](#households) — 3 keys
- [`[jobs]`](#jobs) — 6 keys
- [`[layers]`](#layers) — 20 keys
- [`[lots]`](#lots) — 2 keys
- [`[market]`](#market) — 2 keys
- [`[needs]`](#needs) — 9 keys
- [`[parking]`](#parking) — 2 keys
- [`[placement]`](#placement) — 4 keys
- [`[roads]`](#roads) — 11 keys
- [`[traffic]`](#traffic) — 3 keys
- [`[trips]`](#trips) — 4 keys
- [`[water]`](#water) — 6 keys

---

## `[[band]]`

*An array of tables — a file may declare this more than once.*

**`admits`** · *array of whole numbers*

Which permission bits a Lot in this band may keep -- a list of bit indices, not a mask. It is a CAP applied by intersection against the Lot's own permission set, so a band can only ever take a permission away. Bands are ordered by declaration, least intense first, and a band nobody declared admits everything.

**`name`** · *quoted string*

What this density band is called.

---

## `[[building]]`

*An array of tables — a file may declare this more than once.*

**`abandoned_when_empty_after_days`** · *whole number*

How many Days a Building of this kind may house nobody before the city gives up on it. This reads occupancy where condemn_after_days reads failure, so it kills surplus stock rather than failing stock. It counts Households and not tenants of any kind, so a shop sitting in a dwelling does not keep the clock from starting. Absent means a Building of this kind stands empty for ever.

**`arrivals_per_day`** · *whole number*

How many Households a Building of this kind admits from the Outside each Day. Stating this is what makes the kind an Outside Connection, so absence means the kind is not a gate; a stated zero is refused, because a door that never opens loads clean and does nothing.

**`bins`** · *array of inline tables*

The stores a Building of this kind carries, one entry per Resource. A Rule's local scope draws on these.

**`business`** · *quoted string*

The trade a Building of this kind comes with when it is raised, naming a [[business]]. Absent means it comes with none, which is almost every kind that has ever shipped.

**`collapses_after_days`** · *whole number*

How many Days an abandoned Building stands as a shell before it collapses and the Lot is clear again. Required of any kind that can be abandoned, because shells with no sink are a collection that grows with elapsed time.

**`condemn_after_days`** · *whole number*

How many Days the premises' own Rules may starve continuously before the Building is condemned. Absent means this kind never declines.

**`houses`** · *true or false*

Whether a HOUSEHOLD may take a tenancy in a Building of this kind. It says whether and never how many: the count is the Building's own floor area over [capacity] floor_tiles_per_occupant, so two Buildings of one kind on differently-sized ground hold different numbers. Households and Businesses share the one ceiling, and this is one of the two permissions over it — see premises, which does not follow from this one. Absent means no Household may live here, which is what most kinds are.

**`name`** · *quoted string*

What this kind of Building is called. [[zone_rule]] kind and [[rule]] kind refer to it by this name.

**`parked`** · *true or false*

Whether Buildings of this kind carry parking at all. Whether, and never how many: the count is the Building's floor area over [capacity] floor_tiles_per_parking_space. It exists so that a detached house may carry a driveway where a tower may not — a parking minimum is a property of the city, and an exemption from it is a property of the kind. Absent means the kind provides none.

**`premises`** · *true or false*

Whether a BUSINESS may take a tenancy in a Building of this kind. houses' other half, over the same single ceiling, and it governs both ways a trade arrives: the one this kind comes with and one premised out of the unpremised pool. A kind declaring business and not this is refused, because the trade would have nowhere to sit. A kind declaring both is mixed use — the flats above the shop, competing for the same tenancies. Absent means no Business may take premises here.

**`serves`** · *quoted string*

Which Attended Need a Household travelling here satisfies — education or health. Stating it is what makes the kind a service Building; absence means it is not one.

**`sheds_occupant_after_days`** · *whole number*

How many Days the premises may starve before the Building sheds one Occupant — the first rung of decline, below condemnation. Requires condemn_after_days and must be strictly shorter than it, or the Building would be abandoned before it could shed anything. Absent means the kind sheds nobody on its way down.

**`tenancy_ends_after_days`** · *whole number*

How many Days a tenant's own Rules may starve continuously before the tenancy ends and the tenant is put out. Independent of the premises' threshold: a kind may state either, both or neither. Absent means tenancies here never end.

---

## `[[building]] bins`

*An array of tables — a file may declare this more than once.*

**`capacity`** · *whole number*

How much of that Resource the Bin holds when full. Refused on a money Bin, which has no physical ceiling — a finite one would mean a seller too rich to be paid.

**`owner`** · *quoted string*

Whose Bin this is: the premises', or the tenant's. It is what makes a Rule over this Bin the tenant's Rule rather than the Building's, and a tenant's Bins live exactly as long as the tenancy. Absent means the premises own it.

**`resource`** · *quoted string*

Which Resource this Bin holds, naming a [[resource]]. One Bin per Resource per kind.

---

## `[[business]]`

*An array of tables — a file may declare this more than once.*

**`name`** · *quoted string*

What this trade is called. [[building]] business names it.

**`pay_period_days`** · *whole number*

How many Days pass between paydays at this trade. One is daily; zero would be a payday that never comes round.

**`shift_start_earliest_hour`** · *whole number*

The earliest hour of the Day a Business of this trade may open, as a whole in-world hour. A Workplace draws one start hour and its whole staff share it, so a Citizen stores no start hour at all. Required exactly when the trade employs somebody.

**`shift_start_latest_hour`** · *whole number*

The latest hour a Business of this trade may open. Hour 23 is the ceiling, because this is an hour of the Day rather than a duration and hour 24 is the next Day's hour 0.

**`wage_per_day`** · *whole number*

What one job at this trade pays for one Day worked, as a flat rate. Paired with pay_period_days: each is half a mechanism alone. Absent means the trade pays nothing.

---

## `[[hinterland]]`

*An array of tables — a file may declare this more than once.*

**`edge`** · *quoted string*

Which side of the map this Hinterland sits behind — north, south, east or west — shared by every Outside Connection on that edge. The edge is what a Hinterland is, so there is no default and two Hinterlands may not share one.

**`emigrant_balance_max`** · *whole number*

The most. Stated as a band rather than one figure, because a single figure gives every arrival the same means.

**`emigrant_balance_min`** · *whole number*

The least money a Household arriving from this Hinterland brings with it.

**`prices`** · *array of inline tables*

What this Hinterland will sell each Good for, one entry per Good. These are the only authored anchor under every price in the design: a District's Pool may charge up to the cheapest declared Hinterland price and no more.

---

## `[[hinterland]] prices`

*An array of tables — a file may declare this more than once.*

**`price`** · *whole number*

The import price, which becomes the ceiling on what that Good can cost inside the city. Delete the entry to leave a Good unpriced; a price of zero would make it free everywhere for ever instead.

**`resource`** · *quoted string*

Which Good is being priced, naming a [[resource]]. Only a good may be — a utility is not stocked, and money is what a price is denominated in rather than a thing that has one.

---

## `[[lattice]]`

*An array of tables — a file may declare this more than once.*

**`origin_east_tiles`** · *whole number*

How far east of the map's corner this Street lattice starts, in Tiles, on the block grid. What a file authors is where a settlement is; its extent and its share of the population are derived.

**`origin_north_tiles`** · *whole number*

How far north, on the same grid. Absent [[lattice]] tables altogether mean one lattice at the origin corner, and the gap between two origins is the entire content of a two-settlement world.

---

## `[[life_stage]]`

*An array of tables — a file may declare this more than once.*

**`adult_age_max_days`** · *whole number*

The oldest such age. Required together with the minimum, of any stage a children_become names.

**`adult_age_min_days`** · *whole number*

The youngest age a Citizen carries on becoming an adult in this stage. An age is drawn on formation and never advances, so there is no default that is not a guess about a population. Zero is unavailable: it is what marks a child.

**`centrality_base_percent`** · *whole number*

Where this stage sits on the space-against-centrality axis when it looks for a home — 0 wants room, 100 wants the middle of the city. Absent is the neutral value, which is the placement this build had before preferences existed.

**`centrality_spread_percent`** · *whole number*

The width of the band a Household of this stage draws its own position from, so that a stage is a distribution rather than one opinion. Zero means the stage agrees with itself completely. The band must fit inside the axis.

**`childless`** · *quoted string*

The stage a Household goes to instead when it draws zero children on leaving this one. Paired with the child band: each half alone loads clean and does nothing.

**`children_become`** · *quoted string*

The stage each child forms its own Household in when it leaves home. This is a door into the Unplaced Pool, so a Ruleset stating it owes [placement] gives_up_after_days.

**`children_max`** · *whole number*

The most it bears. The band is inclusive at both ends; equal ends mean every Household bears the same number.

**`children_min`** · *whole number*

The fewest children a Household bears on leaving this stage. Zero is the answer that matters, because drawing zero is what routes a Household to the childless stage.

**`duration_days`** · *whole number*

The fewest Days a Household spends in this stage before its countdown comes due.

**`name`** · *quoted string*

What this stage of a Household's life is called. next, childless and children_become name a stage by this.

**`next`** · *quoted string*

The stage a Household moves to when its countdown comes due. Absent means the stage is terminal, which is the only way to spell that — a stage naming itself is refused.

**`spread_days`** · *whole number*

The width of the window the countdown is drawn over, uniform on [duration, duration + spread). Zero is a real answer and means every Household in this stage leaves it on the same Day — which, for a city whose whole population was created at Tick 0, makes the founding generation's echo run for the whole game.

---

## `[[policy]]`

*An array of tables — a file may declare this more than once.*

**`apply`** · *inline table*

How many times the transfer is applied to each member on one sweep: a band, or a count derived from a Readout.

**`interval`** · *whole number*

How many Ticks between sweeps of this Policy over that population.

**`name`** · *quoted string*

What this Policy is called. The player's governing panel addresses a Policy by this and by nothing else, so an unnamed one is unaddressable rather than addressable by position.

**`sweeps`** · *quoted string*

Which population the Policy runs over: household or business. A third, building, is declared in the design and refused here, because a Building population is whichever rows a predicate picks and there is no predicate.

**`transfer`** · *unasserted*

The movement of money the Policy makes, stated as both ends at once. That is what makes it a transfer rather than a term list: the two sides balance inside one atomic Rule.

---

## `[[policy]] apply`

*An array of tables — a file may declare this more than once.*

**`derived`** · *quoted string*

The name of a Readout whose value sets the count. It must be readable against the population this Policy sweeps — a Readout that only hangs off a Building has no row here to read.

**`max`** · *whole number*

The most. Equal to min is the fixed case.

**`min`** · *whole number*

The fewest applications per member.

**`percent`** · *whole number*

How many applications one unit of that Readout is worth, as a percentage.

---

## `[[policy]] transfer`

*An array of tables — a file may declare this more than once.*

**`amount`** · *whole number*

How much moves per application.

**`from`** · *quoted string*

Which side the money leaves — typically local, the swept member's own balance. The pool and map scopes are refused, because a pool term is a purchase whose payment is implicit and a map cell has no capacity to draw from.

**`resource`** · *quoted string*

Which Resource moves, naming a [[resource]]. The treasury holds conserved Resources only, so a global end means money.

**`to`** · *quoted string*

Which side it arrives at — typically global, the treasury.

---

## `[[resource]]`

*An array of tables — a file may declare this more than once.*

**`family`** · *quoted string*

Which of the three kinds of thing this is: a good moves as a Shipment on the Road Graph and shows up in the traffic, a utility flows along the District adjacency graph, and money is conserved and does not move at all. The family decides transport and whether a Bin holding it has a ceiling, so there is no default.

**`name`** · *quoted string*

What this Resource is called. Every other table refers to it by this name, and the engine never sees the string.

**`need`** · *quoted string*

Which Household Need this Resource feeds when it is consumed. Only sustenance and satisfaction may be named: the other two Needs are fed by travelling to a service Building rather than by buying, and are declared with [[building]] serves instead. Absent means the Resource feeds no Need, which is the ordinary case.

---

## `[[rule]]`

*An array of tables — a file may declare this more than once.*

**`apply`** · *inline table*

How many times the Rule's terms are applied on one firing: either a band, or a count derived from a Readout. Never both — below a band's minimum is a failure and a derived zero is a success, so the two have colliding failure semantics.

**`fills`** · *inline table*

Which Bin a fallback link is relieving, so a chain can be answered as one mechanism. The map scope is refused: nothing ever waits on a Layer, so nothing can rescue a wait on one.

**`inputs`** · *array of inline tables*

What the Rule consumes on each application. A term that cannot be drawn is what makes the firing fail, which is what arms the Failure Pressure the decline thresholds read.

**`kind`** · *quoted string*

Which [[building]] kind this Rule runs on. Whether it is the premises' Rule or its tenant's is derived from the Bins its terms reach, never authored.

**`name`** · *quoted string*

What this Rule is called. on_fail names another Rule by this.

**`on_fail`** · *quoted string*

Another Rule to try when this one's inputs cannot be met — a source ladder over one Bin. A Rule named here is a link rather than a head, so it is reached by walking a failed chain and is never armed on its own rate.

**`outputs`** · *array of inline tables*

What the Rule produces on each application, into a Bin or onto a Map Layer.

**`rate`** · *whole number*

How often a Rule Instance re-arms, in Ticks — its reschedule interval. Distinct from the sweep interval a Zone Rule or a Policy states: a Bin Rule is armed per Building on the Event Wheel, not swept over the city.

**`reports`** · *quoted string*

The name of the Condition this Rule's failure reports under, which is what the diagnosis panel groups by. Absent means the failure is not reported by name.

---

## `[[rule]] apply`

*An array of tables — a file may declare this more than once.*

**`derived`** · *quoted string*

The name of a Readout whose value sets the application count, instead of a band. The readable set is declared in the simulation rather than in the Ruleset, and a Readout must be readable against the entity this Rule hangs off.

**`max`** · *whole number*

The most applications one firing may make. Equal to min is the fixed case rather than a second form.

**`min`** · *whole number*

The fewest applications one firing may make. Falling below this is what makes the firing a failure.

**`percent`** · *unasserted*

How many applications one unit of the derived Readout is worth, as a percentage — 100 is one application per unit. Only meaningful beside derived.

---

## `[[rule]] inputs`

*An array of tables — a file may declare this more than once.*

**`amount`** · *whole number*

How much of it one application draws. A Rule that moves nothing is a Rule with no term rather than a term of zero.

**`resource`** · *quoted string*

Which Resource this term draws, naming a [[resource]].

**`scope`** · *quoted string*

Where the term draws from: local is this Building's own Bin, pool is the District market (a purchase, whose payment is implicit at the prevailing price), and global is the treasury, which holds conserved Resources only. A term reaching the tenant's Bins from a premises Rule is refused, because crossing an ownership boundary is a trade.

---

## `[[rule]] outputs`

*An array of tables — a file may declare this more than once.*

**`amount`** · *whole number*

How much of it one application deposits.

**`layer`** · *quoted string*

Which Map Layer a map-scoped term emits into. A Layer cell has no capacity, so such a term never fails.

**`resource`** · *quoted string*

Which Resource this term deposits, naming a [[resource]].

**`scope`** · *quoted string*

Where the term deposits: local, pool, global, or map. The map scope is write-only, so a Rule may emit to a Layer and can never read or wait on one.

---

## `[[terrain]]`

*An array of tables — a file may declare this more than once.*

**`base_fertility_percent`** · *whole number*

The ceiling this ground's Fertility starts at, as a percentage of fully fertile. 100 is the top of the scale rather than a tuning choice.

**`name`** · *quoted string*

Which of the five terrain types this table prices: ordinary, rock, floodplain, marsh or thin_soil. The set is closed — this selects a type the generator already places rather than declaring one. A file prices all five or none.

**`sealing_decay_tau`** · *whole number*

How many scheduled updates this ground takes to shed its Sealing once it is no longer built on. Zero means never, which is a real answer and is rock's.

---

## `[[zone_rule]]`

*An array of tables — a file may declare this more than once.*

**`build_threshold_days`** · *whole number*

How much unmet demand, in household-Days, must accumulate in a District before this Rule raises a Building there. It is the entry cost for a trade, and today it is the only brake on birth. Absent keeps the older predicate, which reads no demand at all.

**`cooldown_days`** · *whole number*

How many Days a District waits after raising a Building of this kind before it may raise another — what damps the response to the demand signal. Requires build_threshold_days, since a Rule reading no demand has nothing to damp.

**`interval`** · *whole number*

How many Ticks between sweeps of this Rule over the city. Distinct from a Bin Rule's rate: a Zone Rule sweeps, it is not armed per Lot.

**`kind`** · *quoted string*

Which [[building]] kind this Rule raises on a Lot it accepts.

**`name`** · *quoted string*

What this Zone Rule is called.

**`revisit_ticks`** · *whole number*

How long this Rule takes to look at every Lot once, in Ticks. The sample per trigger is derived from this duration rather than authored, so the fraction of the city covered per cycle does not shrink as the city grows. The draw is with replacement, so this is a rate and not coverage.

**`zone`** · *whole number*

Which permission bit a Lot must carry for this Rule to build on it — a bit index, not a mask. A bit no zone command can paint is refused, because the Rule would sample Lots for ever and build nothing.

---

## `[capacity]`

**`floor_tiles_per_job`** · *whole number*

How much floor one job takes, in Tiles. A Business employs its share of its premises' floor area divided by this, its share being one of the Building's tenancies — so one trade employs differently in a terrace and in a slab. Absent means nobody in this city is employed anywhere.

**`floor_tiles_per_occupant`** · *whole number*

How much floor one tenancy takes, in Tiles. A Building's occupancy is its floor area — its footprint on every storey — divided by this, so it varies with the ground the Building stands on rather than with its kind. Omitting the whole [capacity] table means no Building in the city holds anybody.

**`floor_tiles_per_parking_space`** · *whole number*

How much floor a Building must have per parking space it is given, in Tiles. It is a parking minimum, which is a property of a city rather than of a building. Absent means the city has no parking at all.

---

## `[disasters]`

**`flood_every_days`** · *whole number*

How many Days between floods. All three keys are required together, and the table is refused without a [water] flood_level_percent above it — a flood happens on the Hazard Region, so a schedule without one is an event with nowhere to occur. No severity constant is authored anywhere: the only constants are durations.

**`flood_recedes_over_days`** · *whole number*

How many Days it takes to drain away again. A lifetime longer than the interval is overlapping floods, which is a world and is not refused.

**`flood_rises_over_days`** · *whole number*

How many Days a flood takes to reach its full extent. Without it a flood would inundate its whole reach on a single Tick.

---

## `[districts]`

**`hysteresis_percent`** · *whole number*

How decisively the field must favour a new District before a Cell changes hands, as a percentage of the level its own basin reaches it at. Zero would move a Cell on a tie, where the answer is a scan order rather than a finding.

**`migrate_cells`** · *whole number*

The most Cells that may change District in one re-evaluation. A bound of none would compute the new boundary and then refuse to move to it; a large value is how you say undamped.

**`prominence_percent`** · *whole number*

How far a peak in the field must stand above its saddle before it is a centre of its own, as a percentage of its own height. Zero makes every bump a District and the city fragments into as many as it has Cells; 100 is a peak rising from nothing. Delete the table for a city with no Districts at all.

**`revisit_ticks`** · *whole number*

How often the Districts are re-derived. There is no spelling here for finding them once and freezing them.

---

## `[founding]`

**`founding_band`** · *whole number*

What a Household spends to capitalise a shop, moved from its balance into the Business's. At zero every Household could always afford one, so the means test that is the whole of the trigger would stop discriminating. There is deliberately no demand key: a Household founds on its own means, and a key reading shop count or vacancy would be the RCI meter this design refuses.

**`reconsider_ticks`** · *whole number*

How long every Household takes to consider founding once. The founding pass rides [placement]'s trigger rather than owning a cadence of its own, so this must be at least one such interval long.

---

## `[households]`

**`car_ownership_percent`** · *whole number*

What share of Households keeps a car. A Citizen travels in their Household's mode, so this is drive everywhere or walk everywhere; mode choice per Trip is a different question nothing has designed. Omitting the whole [households] table means nobody drives.

**`opening_balance_max`** · *whole number*

The most. Both ends or neither — one end alone reads either as a fixed endowment or as a band reaching down to destitute, and those are different cities. Omitting both endows nobody.

**`opening_balance_min`** · *whole number*

The least money a Household is created with at world creation.

---

## `[jobs]`

**`arrive_early_max_minutes`** · *whole number*

How far ahead of their Shift a Citizen may aim to arrive, in whole in-world minutes. Start hours are on the hour, so without this the whole city departs on a handful of Ticks and the morning comes out a plateau of equal bars. Zero means everybody cuts it fine.

**`candidates`** · *whole number*

How many workplaces one Citizen looks at on one occasion. A person who sees three places with a vacancy and takes the first one near enough to walk to is satisficing, not optimising.

**`interval`** · *whole number*

How many Ticks between passes that assign work to Citizens who have none. Omitting the whole [jobs] table means nobody is ever assigned work. The table is refused without a Commute Budget above it, because the pass has no search radius of its own.

**`revisit_ticks`** · *whole number*

How long the assignment pass takes to look at every Citizen once, in Ticks.

**`shift_hours_max`** · *whole number*

The longest working day. Staff share a start hour and so arrive together; they leave spread over this band, which makes it the evening peak's whole width. Equal bounds are a control rather than a mistake.

**`shift_hours_min`** · *whole number*

The shortest working day in this city, in whole in-world hours. It doubles as the gap between a Citizen's two journeys, so it must exceed the Commute Budget or somebody could still be travelling to work when the roster says they leave it.

---

## `[layers]`

**`desirability_noise_percent`** · *whole number*

How much noise subtracts from desirability. Zero removes the term.

**`desirability_pollution_percent`** · *whole number*

How much pollution subtracts from desirability, as a weight. Zero removes the term rather than defaulting it.

**`desirability_shoreline_percent`** · *whole number*

How much a fouled shoreline subtracts from desirability. Zero removes the term.

**`fertility_pollution_percent`** · *whole number*

How much fully fertile ground a unit of pollution costs, as a percent. It weighs the pollution term for every Cell alike rather than per terrain type, which is why it lives here and not on [[terrain]]. There is deliberately no sealing counterpart: a fully built Cell has no farmland, which pins that coefficient.

**`kernel_metres`** · *whole number*

How far an industrial pollution source reaches, in metres — the width of the diffusion kernel. Baked into the world at creation and refused on reload.

**`land_value_offset`** · *whole number*

Which Tick of that cycle land value fires on.

**`land_value_period`** · *whole number*

How many Ticks between recomputations of the land value field.

**`land_value_tau`** · *whole number*

The time constant land value moves on — how much momentum it has. A period of 1 is land value with no momentum; a tau of 0 is not a time constant, since it divides.

**`noise_intensity_percent`** · *whole number*

How loudly one Vehicle per Tick radiates at one Tile, as a percent. Not a scale: the level is logarithmic above unity and linear below it, so this decides which regime the city sits in.

**`noise_range_metres`** · *whole number*

How far a road's noise reaches from it, in metres.

**`pollution_decay_ticks`** · *whole number*

How long a plume takes to fade, as a duration in Ticks. Zero means it never fades. A decay shorter than the period it runs at is refused, because it would round to zero updates and behave as never while reading as fast.

**`pollution_offset`** · *whole number*

Which Tick of that cycle pollution fires on. It has to sit inside the period, or the Layer would never be recomputed at all.

**`pollution_period`** · *whole number*

How many Ticks between recomputations of the pollution field.

**`sealing_decay_offset`** · *whole number*

Which Tick of that cycle Sealing fires on. How fast each ground actually sheds Sealing is per terrain type, on [[terrain]] sealing_decay_tau.

**`sealing_decay_period`** · *whole number*

How many Ticks between recomputations of the Sealing field.

**`shoreline_intensity_percent`** · *whole number*

How strongly a completely fouled body radiates at one Tile from its edge, as a percent. Its multiplicand is a fill fraction where noise's is an unbounded flow, so the two intensities are not comparable.

**`shoreline_range_metres`** · *whole number*

How far a fouled Water Body reaches from its edge, in metres. A world with no water is spelled by omitting [water], not by writing zero here.

**`woodland_regrowth_days`** · *whole number*

How many Days a Cell cleared of all its forest takes to grow back to what the seed laid. Omit the key for a world where forest never returns; zero is refused, because it would mean instantly rather than never.

**`woodland_regrowth_offset`** · *whole number*

Which Tick of that cycle woodland regrowth fires on.

**`woodland_regrowth_period`** · *whole number*

How many Ticks between recomputations of the woodland field.

---

## `[lots]`

**`lots_per_segment`** · *whole number*

How many Lots one Street Segment carries, both sides together. A Lot is an address with no extent — there is no depth key, because a Lot has no depth. Omitting the whole [lots] table means land cannot be subdivided and the city grows nothing.

**`setback_tiles`** · *whole number*

The most ground a Building leaves on each side of its parcel, in Tiles. It is a length and not a fraction, so how much of a plot gets built on rises with the plot: a detached parcel keeps under half of itself and a slab's covers most of its site. The four sides are drawn independently per patch of ground, so a street varies and some walls stand on the pavement. Zero means every Building covers its whole parcel.

---

## `[market]`

**`decay_percent`** · *whole number*

How much of the standing consumption rate survives one Day — the smoothing under a Pool's price. Zero is allowed and means no smoothing, which is a twitchy market and a real one. Omitting the whole [market] table means every trade clears at the import ceiling for ever. Neither key here is a price: a Pool opens at the ceiling and moves from there, so no seed exists and none is needed.

**`move_cap_percent`** · *whole number*

The furthest a price may travel in one Day, as a percentage of the import ceiling. Zero would mean it never moves, which is what deleting the table already says.

---

## `[needs]`

**`education_degrade`** · *whole number*

How far Education falls on one occasion. Attended rather than bought, so this pair is required exactly of a file declaring a [[building]] that serves education, and refused of one that does not — a rate nothing can reach reads on the page as a mechanism the world has.

**`education_recover`** · *whole number*

How far Education rises when a Household attends a school.

**`floor`** · *whole number*

The deepest deficit any Need may reach. A Need is a relative scalar where 0 is ideal, so this is negative. It is required because an unbounded magnitude is the thing a long run exists to catch.

**`health_degrade`** · *unasserted*

How far Health falls on one occasion. Conditional on a kind serving health, exactly as education's pair is.

**`health_recover`** · *unasserted*

How far Health rises when a Household attends a health service.

**`satisfaction_degrade`** · *whole number*

How far Satisfaction falls on one occasion.

**`satisfaction_recover`** · *whole number*

How far Satisfaction rises when the Household buys what feeds it.

**`sustenance_degrade`** · *whole number*

How far Sustenance falls on one occasion. Stated as a positive step whose direction belongs to the mechanism: a degrade always falls and a recover always rises. Omitting the whole [needs] table means no Household in this city has a Need at all.

**`sustenance_recover`** · *whole number*

How far Sustenance rises when a Household is fed.

---

## `[parking]`

**`radius_metres`** · *whole number*

How far a driver will walk from a Car Park to where they were going. Omitting the whole [parking] table is a city with no Parking Shed at all; a radius of zero would be a city whose Car Parks all exist and none can be reached.

**`shed_keeps`** · *whole number*

How many Car Parks a Building's Parking Shed holds, and therefore how far a query walks before it stops. Not redundant with the radius: the cap bounds the work and the radius bounds the walk, and they bind in different worlds. A shed is stored for every Building at this width, so it is a per-city cost rather than a per-query one.

---

## `[placement]`

**`candidates`** · *whole number*

How many dwellings one Household looks at on one occasion before waiting for the next. A behaviour model rather than a budget: a family that sees three flats and takes the first with room is not an optimiser being approximated.

**`gives_up_after_days`** · *whole number*

How long a Household keeps looking for a home before it gives up and leaves. Required of any Ruleset with a door into the Pool — a gate kind, or a life stage whose children leave home — because a Pool with an inflow and no sink grows without bound. Absent means nobody ever gives up, which is only coherent in a world with no door in it.

**`interval`** · *whole number*

How many Ticks between passes that drain the Unplaced Pool into standing dwellings. Omitting the whole [placement] table means the pass does not run and nobody is ever housed, which is loud rather than quiet — the Pool grows and the Census says so.

**`revisit_ticks`** · *whole number*

How long the placement pass takes to look at everybody waiting in the Pool once, in Ticks. The sample per trigger is derived from this, so the fraction of a housing queue cleared per cycle does not shrink as the queue grows.

---

## `[roads]`

**`arterial_capacity_per_hour`** · *whole number*

The same for an Arterial.

**`arterial_count`** · *whole number*

How many freeform Arterials cross the map. Zero is a city of Streets alone, and therefore a city where Severance cannot happen — which is what every city-modelling Ruleset states, because an Arterial is a player tool that does not belong in a generator.

**`arterial_junction_tiles`** · *whole number*

How many Tiles of Arterial lie between two authored Junction pieces, which is that Arterial's Segment length.

**`arterial_speed_kph`** · *whole number*

Free-flow speed on an Arterial.

**`block_tiles`** · *whole number*

The Street grid spacing in Tiles — the block. It decides Segment count and therefore every routing figure downstream. Omitting the whole [roads] table means the world has no roads at all.

**`foot_crossing_every`** · *whole number*

Keep a pedestrian crossing at every nth Street an Arterial severs. Zero means no crossings and a network cut in two for anybody on foot.

**`foot_path_capacity_per_hour`** · *whole number*

The same for a foot path.

**`foot_paths_per_thousand_blocks`** · *whole number*

How many blocks in a thousand get a cut-through for pedestrians. The stronger Severance lever of the two.

**`street_capacity_per_hour`** · *whole number*

How many Vehicles an hour a Street carries before the volume-delay function starts slowing it.

**`street_speed_kph`** · *whole number*

Free-flow speed on a Street, in km/h, converted exactly at load because the library holds no metres and no seconds.

**`walk_speed_kph`** · *whole number*

Walking speed on foot. It is also what turns the Commute Budget into the box the job search draws candidates from.

---

## `[traffic]`

**`alpha_percent`** · *whole number*

How much slower a Segment is at exactly its capacity, as a percentage — the alpha of the volume-delay function. Omitting the whole [traffic] table means roads never slow down. Note that congestion here is a loop rather than a formula: a slower Vehicle dwells longer, and longer dwell is higher volume.

**`beta`** · *whole number*

The exponent of the volume-delay curve, a small whole number. Below 1 the function is not increasing in volume; above 6 a clamped ratio overflows the fixed-point arithmetic it is computed in.

**`clamp_percent`** · *whole number*

The largest volume-over-capacity ratio the function will read, as a percentage. Below 100 the clamp binds before a Segment is even full; far above it the router is comparing noise.

---

## `[trips]`

**`commute_budget_minutes`** · *whole number*

How long a journey may take before the person making it gives up — the only rung that refuses a Trip. All three rungs are one decision: state every one or none. Omitting them is a city whose commutes are never refused for their length and never graded, which is the only city whose cost distribution is uncensored.

**`commute_fast_minutes`** · *whole number*

The ceiling on a commute counted as fast, in in-world clock minutes. It grades a commute that happens anyway and refuses nothing.

**`commute_moderate_minutes`** · *whole number*

The ceiling on a moderate commute. Above it a commute is unsavoury but still made.

**`crossing_seconds`** · *whole number*

What it costs on foot to reach the other side of a Segment, in in-world seconds. Zero is legitimate and means a shop opposite is the shop next door. A road you wait longer than an hour to cross is not a Street at all — it is an Arterial, which carries no pedestrian Arcs between its Junction pieces.

---

## `[water]`

**`capacity_per_cell`** · *whole number*

How much one wet Cell of a body holds. A body that held nothing would be an infinite sink wearing the opposite spelling.

**`carries`** · *quoted string*

Which Resource a Water Body's Bin holds, naming a [[resource]]. It must be a utility: a body moves its contents along an edge of the water graph with no Vehicle, and a good is by definition a Resource whose movement between Districts needs one.

**`flood_level_percent`** · *whole number*

How high a flood reaches on the same scale, which is what generates the Hazard Region. It must stand above the sea — at or below it would describe ground already under water. Absent means no floodplain, which is a steep coast and a world.

**`outflow_per_exit_per_day`** · *whole number*

How much leaves a body through one exit in a Day — the way out.

**`runoff_per_sealed_cell_per_day`** · *whole number*

What a fully sealed Cell sheds into the body it drains to in a Day — the way in. These four keys stand or fall together; omit all four for water with no level.

**`sea_level_percent`** · *whole number*

How high the sea stands, as a fraction of the height range this world realised. Delete the [water] table for an inland world with no coast; zero would put the sea at the lowest Cell, which is a second spelling of the same thing.

