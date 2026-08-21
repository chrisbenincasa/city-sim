# 0035 — Hinterlands and arrival through the gate

`06` milestone **11**. The brief.

---

## Status

🟡 **SCOPED 2026-08-20, and ✅ ASSESSED FIRST — no document names a gate on it.** The assessment is
recorded in full below as **F1**, because [`0000`](0000-board.md) made the assessment the *first act* of
this row on the strength of milestone 9, which read as ungated by `grep` and turned out to be two
milestones. This one is not gated. It is something else: **its central mechanism is specified in terms
of a mechanism five milestones downstream.**

✅ **DECISION 1 IS SETTLED, 2026-08-20** ([`adr/0128`](../docs/adr/0128-the-gate-ships-before-the-comparison-that-walks-through-it.md))
— **the milestone splits: the gate ships here, the comparison stays at 16.** Seven decisions remain,
five before any task. The problem it answers is below, and the cost it accepts is that **the arrival
door has no autonomous caller until 16**, which is written down on day one rather than found in a task.

🔴 ⚠ **The milestone as written could not be built, and the reason is a sentence every document agrees on.**
[`adr/0023`](../docs/adr/0023-immigration-arrives-through-the-gate.md) opens *"There is no immigration
rate, no arrival scalar, and no attractiveness meter. **A prospective Household evaluates the city using
the same choice model residents use**"*; `CONTEXT.md` → Hinterland says *"the identical utility function
residents use"*; `02 §5.4` says it a third time. **That choice model is `06` milestone 16**, and `06`'s
own dependency graph runs *Price surface, Hinterland, Money → the residential choice model* — so **11
precedes the only mechanism its own ADRs say arrival is made of**. ***A milestone specified by naming
another mechanism inherits that mechanism's position, and nothing in a dependency graph notices when
the naming is inside an ADR rather than inside the graph.*** **Decision 1 owes an answer before any
task starts.**

⚠ **Nothing about this is a gate, and it must not be recorded as one.** Under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) the choice model is
**unbuilt**, not refused, and *the answer to "given X does not exist, should Y compensate?" is build X*.
What makes this a decision rather than a mechanical application of that rule is that building X here
**inverts an edge `06` says is forced**.

✅ **ALL DECISIONS ARE CLOSED, 2026-08-20** — eight owed, two of which split, **ten settled**, four
records: [`adr/0128`](../docs/adr/0128-the-gate-ships-before-the-comparison-that-walks-through-it.md),
[`adr/0129`](../docs/adr/0129-the-pool-waits-at-the-gate-and-an-arrivals-trip-is-the-move-in.md),
[`adr/0130`](../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)
and [`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md),
every one with the user in the room. **The milestone got smaller four times and more honest each time.** They are §*Open decisions* below.

⚠ **An eleventh was found by task 1 on 2026-08-20, after this section said they were all closed, and it
is recorded as decision 9.** The throughput ceiling's **unit** was named in no document, and
[`adr/0088`](../docs/adr/0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md)'s `min()`
compares it against a column denominated in **Vehicles per Day** — which nothing at 11 produces. It is
settled and `adr/0088` is amended in place twice; the milestone got smaller a **fifth** time.
***A scoping sitting closes the decisions it can see, and a unit is visible from the code rather than
from the record that states the formula*** — which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
paying out on the first task of the milestone that quoted it.

---

## Why this milestone exists, in one paragraph

Nothing in the build says where a Household comes from. `World.CreateHousehold` (`World.cs:772`)
**requires a dwelling**, its only production caller is `SyntheticCity.PopulateInto`, and
`World.Unplace` (`World.cs:946`) **refuses an unhoused Household** — so the Unplaced Pool can hold only
a Household the city previously housed, and the city's population is fixed at world creation for the
whole of its life. Money has the same shape one level down: `World.Endow` is *the only production
issuance of money in the build*, it runs once at Populate, and `Invariant.MoneyIsConserved` is an
**exact equality** because there is no door for money to leave by. This milestone builds the door. It is
the same door for both — [`adr/0024`](../docs/adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md),
and every shipped Ruleset says so in its own header: *"the Outside Connection being its only source and
sink … **That Connection is milestone 11 and does not exist**."*

---

## The named risk

`06`: *"That nothing says where Households come from, and that no price in the design has an anchor."*

⚠ **The two halves have different sizes and only the first is retired by a producer.** *Where Households
come from* is a mechanism this milestone can build. *No price has an anchor* is retired by **authoring an
object**, and the consumers that would make the anchor observable — the District Pool (12), the price
surface (13) — are behind it. ***An anchor with no consumer is milestone 9's land value field one
milestone later***, and 9 is the worked example: the producer shipped correct and unobservable, and its
weights are still unratified because nothing reads them. **Decision 6 is where that is confronted rather
than repeated.**

---

## What the build already holds — surveyed 2026-08-20

**Read this table rather than a sentence about it** ([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).
Every row was taken by opening the symbol.

| | State | Where |
|---|---|---|
| **Hinterland** | **Absent — the word does not occur in `src/`, `tests/` or `rulesets/` at all** | — |
| **Outside Connection** | **Absent as a thing; present as a forward reference in nine doc-comments** | `MoneySupplyTable.cs:74`, `World.cs:853`, `Invariant.cs:618`, `Ruleset.cs:218`, `RulesetLoader.cs:1563`, `SyntheticCity.cs:209`, `MoneyDump.cs:29`, `EvidenceDump.cs:193` |
| **Settlement** | **Absent as a computed thing.** Two prose mentions recording that `adr/0020`'s union-find definition and `CONTEXT.md`'s mutual-reachability definition **disagree** | `RoadConnectivity.cs:34` |
| **`TripPurpose.Immigration`** | **Absent — no member and no throw.** Members are `Unset`, `Shopping`, `Commanded`, `Commute` | `TripPurpose.cs:36–113` |
| **A door into the Pool** | **Eviction only.** `World.Unplace`'s two callers are `DestroyBuilding` and `EvictOverflow`, and it **refuses an unhoused Household** (`Invariant.OnlyAHousedHouseholdIsUnplaced`) | `World.cs:946`, `:2869`, `:2122` |
| **Household creation** | **Requires a dwelling in its signature.** One production caller | `World.cs:772`; `SyntheticCity.cs:206` |
| **The Unplaced Pool** | **Real, one column, and it is the demand signal** — `Count => _rows.LiveCount`, *"deliberately not a scalar anything can set"* | `UnplacedTable.cs:41`, `:72` |
| **Money issuance** | **One door, once, at Populate.** `World.Endow` writes `MoneySupply.Issued`; the comment says *"THE ONLY PRODUCTION ISSUANCE OF MONEY IN THE BUILD"* | `World.cs:880`, `SyntheticCity.cs:225` |
| **Conservation** | **An exact equality**, `MoneyLedger.Total == MoneySupply.Issued` | `Invariant.cs:622` |
| **A trade term** | **Throws.** `Scope.Pool` is a named hole and its message says the Pool is *a market, not a wider Bin lookup* — **that is milestone 12, not this one** | `RuleEngine.cs:803` |
| **The loader's money balance check** | **Real, and it over-refuses on purpose**: *"A wage … and an import payment both have real counterparties that no scope can currently name, so both would be refused"* | `RulesetLoader.cs:1538`, `:1561` |
| **`CommandKind.Connect`** | **Real, and it is a different concept** — `01 §2`'s road verb, one Street Segment on a lattice edge. **Not an Outside Connection** | `Command.cs:51`, `Simulation.cs:435` |
| **`[households] opening_balance_min`/`max`** | **Real**, and stated in `rulesets/taxed.toml` alone | `RulesetLoader.cs:2849` |

✅ **The representation question is already answered and the answer makes the milestone small.**
[`adr/0088`](../docs/adr/0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md):
*"**No new table, no new column, no new mechanism.** It is a Building kind with an unusual Bin set and a
position constrained to an edge."* Its throughput is `min(the kind's declared ceiling, the Access
Point's Segment capacity)`, and ***which of the two is binding is the whole readout***.

---

## Open decisions this milestone owes, before the task that needs them

### 1. What does a prospective Household compare, when the choice model is milestone 16? ✅ **SETTLED 2026-08-20 — it does not compare anything here; the milestone splits.** Typed *arguable*

✅ [`adr/0128`](../docs/adr/0128-the-gate-ships-before-the-comparison-that-walks-through-it.md). **The
gate, the Hinterland, the arrival route and the money door ship at 11; the comparison stays at 16**, and
`06`'s edge *Hinterland → the residential choice model* keeps its direction. ⚠ **The cost is stated
rather than discovered**: at 11 the arrival door has **no autonomous caller** — a Command and its tests,
the same shape as `CommandKind.Populate`, which the build already distinguishes as ***the founding door
and not the gate***. So **`06`'s row retires half its risk here**: the *anchor* half, by authoring the
Hinterland; ***a door is not an answer to where from***. **Rejected arrivals and their reasons move to
16 with the comparison** — nothing declines an offer nobody makes — and the obligation is relocated
rather than discharged. ⚠ **The crude-rule option was refused by name** as milestone 9's **F13**: a hole
that throws is safe, and one that returns plausible numbers is a working mechanism that says something
false. **The give-up rule does NOT move with it** — decision 3 stands, because an inflow driven by a
Command is still an inflow and `adr/0006` does not care what called it.

*The problem as scoped, kept because the reasoning is what the ADR rests on:*

**The problem, stated once.** Three documents specify arrival as *the residential choice model with the
Hinterland as an ordinary row*. That model is 16. `06`'s graph makes 16 depend on 11. Building 11 as
specified therefore requires either building 16 inside it or inverting the edge.

**The three answers, and none is free.**

- **Build the comparison here.** `adr/0070` says build X. It is the honest reading, and it makes this
  milestone at least twice its stated size — the logit, `μ`, per-Life-Stage coefficient vectors, the
  sampled candidate set — **none of which has a ratifier and all of which belong to 16**.
- **Split the milestone.** The gate, the Hinterland object, the money door and the arrival *route* ship
  here; **what decides to arrive** ships at 16. ⚠ Then this milestone builds a door nobody walks
  through, which is the `06` row's first half unretired.
- **Ship a stated, deliberately crude acceptance rule** — and **name it a hole rather than a model**, so
  that 16 replaces it rather than tuning it. ⚠ This is the option that can produce a **false working
  mechanism**, which is milestone 9's F13 exactly: *a partial composition is what the named-hole
  discipline does not cover*, because it returns plausible numbers instead of throwing.

⚠ **It is the user's decision and not mine.** [`0024`](0024-session-j-the-save-the-map-and-the-outside.md)
is the record of what happens otherwise: *"This session was run without the user, and it should not have
been… **two of the five decisions I took unilaterally were wrong**"*, and its finding that `adr/0043`
lacks a third type — ***a claim that is the user's to make***.

### 2. Is an arriving Household created into the Pool, and what does that do to the Pool's meaning? ✅ **SETTLED 2026-08-20 — the Pool sits at the gate, and the Trip is the move-in.** Typed *arguable*

✅ **A Household arrives at a gate as an entry event and joins the Pool *there*; the Trip happens when
placement gives it a dwelling — gate → home.** ⚠ **The sequence `adr/0023` states cannot be built
literally, and the build is what says so**: `TripTable.Start` requires an **origin and a destination
Address**, and *"arrive as Trips … enter the Unplaced Pool, and house themselves"* gives the Trip no
destination, because the Pool has not assigned one yet. ***A journey stated in prose can name an
endpoint the mechanism has to compute.*** Reordering it keeps every property the ADR was buying:
arrival is still physical, still located at a specific gate, still bounded by that gate's throughput,
and still congestion-bearing when the move-in runs.

✅ **The gate is a column on the Pool membership, and the placement is forced rather than chosen** —
the gate is known at arrival because throughput bounds arrival *there*, and it is needed again at
placement as the Trip's origin, so it must survive the wait and the wait is the Pool spell. ⚠ **A
lifetime column on the Household was considered and is wrong**: two of the Pool's four entry routes have
no gate at all — a Household the city generated itself, and one evicted by a demolition. ⚠ **And
`adr/0023`'s *people leave the way they came* needs no amendment**: it says people enter through a gate
and leave through a gate, not that it is the same gate. ***Reading a symmetry of shape as a symmetry of
identity invents an obligation the record never stated.***

`World.CreateHousehold` demands a dwelling; `World.Unplace` refuses an unhoused Household by invariant.
So **the Pool cannot today contain a Household that has never lived here**, and arrival needs a second
door — `World.Arrive`, creating unhoused straight into the Pool. ⚠ **The invariant is not an obstacle to
route around; it is the corpus stating what the Pool means**, and `CONTEXT.md` → Unplaced Pool already
says the opposite: *"immigrants, existing Households that decided to move, Households the city generated
itself … and Households evicted. **All four enter on equal terms**."* ***A door the design describes and
an invariant refuses is a disagreement, not a defect***, and which one moves is the decision.

### 3. Does the give-up rule ship here? ✅ **SETTLED 2026-08-20 — yes, and only the unhoused channel comes with it.** Typed *arguable*

✅ **The unhoused Departure ships at 11; the housed and destitute channels do not.** `CONTEXT.md` splits
Departure into three channels and **only the housed one is a comparison** — `adr/0102` — so it is the
only one [`adr/0128`](../docs/adr/0128-the-gate-ships-before-the-comparison-that-walks-through-it.md)
pushes to 16. The unhoused channel is *"entered the Unplaced Pool, failed repeatedly, gave up"*: a bound
and a threshold, needing nothing that does not exist. ⚠ **The build disagreed with the glossary and the
glossary wins**: `UnplacedTable`'s doc-comment routes the give-up counter to *"milestone 9a"* — now 19 —
while `CONTEXT.md` says *"whoever builds the gate owes the give-up rule in the same milestone."* The
glossary is right for a reason the doc-comment could not have known: **it is `adr/0006` that makes it
owed**, and `adr/0006` becomes live the moment an inflow exists.

✅ **There is exactly one reason and that is not a stub.** `PlacementEngine` is blind by design —
*"Acceptance needs rent, a commute and a tolerance; none exists, so any member would take any dwelling"*
— so the only thing that can go wrong is **no room**, which is precisely the **capacity** diagnosis
`CONTEXT.md` assigns to this channel and whose remedy is *build more*. ***A single reason is honest when
the mechanism admits one; it is a stub only when it admits more and records one.***

### 3a. What does a Household give up *on*? ✅ **SETTLED 2026-08-20 — a duration bounds, a count is recorded, and a second bound arrives at 16.** Typed *arguable*

✅ **The Ruleset states how long a Household will keep looking**, and the occasion count derives from
`[placement] revisit_ticks`. This is [`adr/0059`](../docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)
one level down: ***authoring a count where the felt quantity is time makes the felt quantity move
whenever somebody retunes a cadence***, and nobody editing `[placement]` would expect to change how long
families wait for a home. 🔴 ⚠ **And a count of dwellings *considered* cannot fire at all in a city with
no vacancies** — zero offered, zero considered, the counter never advances — **which is the exact failure
this channel exists to diagnose.** ***A bound that cannot trip in its own headline case is not a bound.***

✅ **The count is recorded from day one as Evidence, not as a bound.** `00-vision.md`'s flagship example
reads *"Considered 20 dwellings over 4 months"* — two numbers, one bounding and one describing, and both
are wanted. ⚠ **A second bound on *refusals* is owed at 16 and must not be authored now**: `PlacementEngine`
never refuses anything, it only fails to find room, so a refusal count is identically zero until
acceptance exists. That is milestone 9's `w₃` exactly — *not choosable until the mechanism that gives it
units ships* — and ***an inert number in a Ruleset is one a designer tunes expecting an effect.*** At 16
the two bounds run **first-to-trip**, because a Household that saw fifty dwellings and took none has
learned something one that saw two has not.

⚠ **`CONTEXT.md` → Unplaced Pool said *a limited number of failed attempts* and was corrected in the
same sitting.**

`CONTEXT.md` → Unplaced Pool: *"the day immigration arrives, that reason evaporates and Departure becomes
load-bearing. **Whoever builds the gate owes the give-up rule in the same milestone.**"* `06` schedules
Departure at **19**. ⚠ **This is [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md)
territory and not a nicety**: a Pool with an inflow and no sink is a collection that grows with elapsed
time, and this milestone would be the first to open one.

### 4. Do Settlements ship here? ✅ **SETTLED 2026-08-20 — no; they follow their consumer.** Typed *arguable*

✅ **Nothing reads a Settlement, and that is by design.** `adr/0020` and `CONTEXT.md` both say it: *"a
reporting and diagnosis unit, not a simulation unit. Nothing pools by Settlement, nothing is budgeted by
Settlement, and **no Rule reads one**."* Building it here would ship a producer whose only output is a
number nobody can see until there is a shell — **milestone 9's F17 with the lesson already paid for**.
⚠ **And its algorithm is open**: `adr/0020` is amended on evidence to say union-find computes **weak**
connectivity where the corpus needs **strong**, `RoadConnectivity.cs:34` records the disagreement in the
build, and the correction is *"downstream of a decision still open"* — per-Segment against per-direction
volume. ***A row placed at a milestone by an inventory is not a row that milestone's risk statement
carries***, and `06`'s milestone 11 row never mentioned Settlements at all.

`06` places *Settlements — commute-shed components, merge and split* at **11**; **11's own risk row never
mentions them**. And the algorithm is open: `adr/0020` is amended by S2 R1 on evidence — union-find
computes **weak** connectivity where the corpus needs **strong** — and `RoadConnectivity.cs:34` records
that disagreement in the build. ***A row placed at a milestone by a table is not a row the milestone's
risk statement carries***, which is `06`'s inventory drifting against `06`'s own table.

### 5. Do Shipments ship here? ✅ **SETTLED 2026-08-20 — no, and the contradiction resolves one way only.** Typed *arguable*

✅ **Shipments are behind 12 and 12 is behind 11, so they are not this milestone's** — freight needs
something to carry, and nothing crosses the gate as a Good until `Scope.Pool` exists
([`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)).
`06`'s inventory cell said *"Placed: 11, behind 12"*, which the milestone table makes impossible.
***A permutation applied mechanically to a cell naming two milestones can satisfy one of them.***

`06` inventory: *"Shipments … ✅ **Placed: 11**, behind 12"*. **11 runs before 12.** A row cannot be placed
at 11 and behind 12, and `06` warns two paragraphs above the table that the 2026-08-18 permutation was
applied mechanically. ***A permutation applied to a cell that names two milestones can satisfy one of
them.***

### 6. What ratifies the three §D2 numbers, and against what world? ✅ **SETTLED 2026-08-20 — one becomes ratifiable here, one stays a gap, one moves to 24.** Typed *measurable*

✅ **The throughput ceiling is ratifiable at 11**, because arrivals are what it bounds. ✅ **The price
offset stays a §D2 gap** — with no Good crossing, it still has no consumer, and its ratifier moves to
**12** rather than to 11. ✅ **The generator's count and siting moves to 24**, with the generator
([`adr/0124`](../docs/adr/0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)):
`SyntheticCity` places gates at 11 the way it builds every other test world, so no fragment of the
generator is written here and no world-creation number is pinned before there is a generator to pin it.
⚠ **All three said *milestone 8* and all three are corrected in place** — F4. ⚠ **And the siting row's
derivation was already dead**: *derive it from the unlock rule*, against `adr/0090`'s *"the map is open
— no unlock, no serviceability gate, no boundary"* (F5). ***A number whose derivation was refused is not
made choosable by the milestone that happens to need a world.***

### 6a. Which Hinterland fields ship at 11? ✅ **SETTLED 2026-08-20 — only those with a consumer, and drawdown is not one.** Typed *arguable*

✅ **A Hinterland field is authored in the milestone that reads it** ([`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)):
the **edge identity** and **what its emigrants carry** at 11; depth and recovery at **16**; price per
Good at **13**; median wage at **15**; median rent, service levels and the commute figure at **16**.
🔴 ⚠ **Drawdown in particular must not ship here, and the reason is the strongest thing this sitting
found.** `CONTEXT.md` says the city *takes the most willing first*, so drawing a Hinterland down *"raises
its rate and skews its mix"* — and, in the same entry, *"**there is no population ceiling**; drawdown is
a gradient, not a wall."* **Both properties come from the willingness ordering, and the ordering is the
comparison at 16.** A stock decrementing at 11 has nothing to order by, so it can express only
availability: arrivals, then none. ***A stock without an ordering is a wall, whatever the design calls
it*** — and the wall is the population ceiling the entry refuses **by name**, arriving as an
implementation detail of the mechanism that was supposed to replace it. ✅ **This settles F9 too**, and
not by picking one document's field list: ***the list is never needed all at once.***

The throughput ceiling, the kind's price offset and the generator's count and siting are three §D2 gaps
from `adr/0088`. Two defects, both found by this scoping:

- 🔴 **Their stated ratifier is *milestone 8*, and milestone 8 is Save/load and closed.** They were
  mapped old-10 → 8 — the *wrong half* of the very split [`0012`](0012-corpus-audit.md) recorded as
  paid, where old-10 was read as two things. **They mean this milestone.**
- 🔴 **The siting row says to derive the count from *the unlock rule*, and there is no unlock rule.**
  [`adr/0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md) refused it
  outright: *"The map is **open** — no unlock, no serviceability gate, no boundary."* ***A derivation
  basis can be refused by a later record without anything re-reading the numbers that rest on it.***

⚠ **And a fourth number has two different ratifiers in two ADRs**: `adr/0088` ratifies the gate count
against *the first Ruleset with an Outside Connection*; `adr/0090` ratifies the same number against *the
first play session*, which `06` says belongs to nobody before Phase 3.

### 7. Does money cross the gate in this milestone, and what happens to the exact equality? ✅ **SETTLED 2026-08-20 — yes, carried by people, and the equality gains its flow term.** Typed *arguable*

✅ **An arriving Household draws its balance from its Hinterland; a departing one takes it.**
`MoneySupply.Issued` gains its second writer through **migration** rather than trade, and
`Invariant.MoneyIsConserved` becomes a sum with a flow term — the shape [`0033`](0033-conserved-money-and-the-treasury.md)
wrote in advance: *"let milestone 11 add the term."* ⚠ **Milestone 10's exact-equality acceptance run
expires here**, exactly as that plan said it would. ✅ **`[households] opening_balance_min`/`max` is NOT
reused** — it is a world-founding key, and drawing arrivals from it would make the four edges
interchangeable as money sources while each separately authors an economy its own emigrants did not come
from. ***An anchor that does not reach the thing it anchors is decoration.*** ⚠ **And it is the only
thing that makes any Hinterland field readable at 11**, which is F7's answer: one readable field is the
difference between a number a run can refute and a number nobody can. ✅ **`plans/0002` §C's *where does
a departing actor's balance go* is answered for Households** — a **gate crossing** — and the Business
half is untouched, because nothing destroys one yet.

[`0033`](0033-conserved-money-and-the-treasury.md) wrote milestone 10's conservation assertion
anticipating this: *"Write it that way and let milestone 11 add the term."* So the shape is decided and
the timing is not. ⚠ **Beside it sits a question that is explicitly blocked on this milestone** —
*where does a departing actor's balance go?* — which [`0002`](0002-open-questions.md) types *arguable*
and says is either a **gate crossing** (this milestone's machinery used a second time) or an
**inheritance**.

### 8. What does an import payment pay to? ✅ **SETTLED 2026-08-20 — nothing, because no Good crosses at 11.** Typed *arguable*

✅ **Trade lands at 12 with `Scope.Pool`, where the market is.** `RuleEngine.cs:803`'s own message is the
argument: *"the Pool is a MARKET, not a wider Bin lookup … Implementing this as a Bin lookup ships an
unconserved economy, and no refusal can catch that."* Naming a counterparty scope at 11 invents one a
single milestone before the real market supersedes it. ***Two scopes for one idea, one milestone apart,
is how a superseded mechanism acquires content.*** ⚠ **The loader's over-refusal therefore stands
unchanged** — `RulesetLoader.cs:1538` refuses an import payment for want of a nameable counterparty, and
it is still right to.

`RulesetLoader.cs:1538` records that the money-balance refusal **over-refuses an import payment by
design**, because no scope can name its counterparty. `Scope.Pool` is the market and it is **milestone
12**. So either this milestone names a counterparty scope for the gate, or a Rule cannot buy anything
from the Outside until 12 and the anchor stays unobservable for two milestones.

### 9. What unit is the throughput ceiling in, and does the `min()` ship here? ✅ **SETTLED 2026-08-20 — the ceiling bounds arrivals, and the second term follows freight to 12.** Typed *arguable*

⚠ **This decision was owed and the sitting that closed the other ten did not find it.** It was found by
task 1, before any code, and it is the reason the task list below changed on its first day.

✅ **The declared ceiling ships at 11 and bounds arrivals. The `min()` and the which-binds readout ship
at 12**, with freight — [`adr/0088`](../docs/adr/0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md)
amended in place, twice.

**The problem is a unit, and it belongs to `adr/0088` rather than to this milestone.** That record makes
throughput `min(the kind's declared ceiling, the Access Point's Segment capacity)` and calls *which of
the two binds* **"the whole readout"**. The second operand is `RoadSegmentTable.CapacityPerDay`, and it
is **whole Vehicles per Day** — a Street is 3,600 an hour, so 86,400. `adr/0088` was written about
**Goods**, where that is the right denominator and the `min()` is one unit on both sides. **At 11 no
Good crosses** ([`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)):
what crosses is people and the money they hold, and under
[`adr/0098`](../docs/adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md)
**whether an arriving Household is a Vehicle at all is a property of the Ruleset in force** —
`minimal.toml` states no `[households]` table, so nobody drives and the Segment term bounds nothing.

🔴 ⚠ **So the readout would have shipped inert on every world in the build, and inert in the direction
that reports a false cause**: *the ceiling binds*, on every gate, on every Ruleset, for a reason that is
not about the gate. ***A term that is vacuous on the world the milestone runs on is not a diagnosis.***
That is milestone 9's **F13** arriving through a different door — *a hole that throws is safe, one that
returns plausible numbers is a working mechanism that says something false* — and F13 was refused **by
name** in decision 1 of this same milestone.

✅ **The rule that settles it was already written here**: [`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)'s
*a Hinterland field is authored in the milestone that reads it*, applied to a **term** rather than to a
field. It needs no new argument, and it is **the fifth time this milestone got smaller**.

⚠ **What this does not do is weaken decision 6.** The ceiling is still ratifiable at 11, and for the
reason decision 6 gave — *arrivals are what it bounds*. It is now ratifiable **more** cleanly, because
arrivals are the only thing it bounds.

⚠ **And it leaves `adr/0088`'s two-ceiling rule standing in full.** Nothing here says the Segment term
is wrong; it says it is not readable yet. **12 owes it**, and the amendment in place is what will make
somebody look.

---

## Tasks

⚠ **Every decision is closed, so this list is now a scope rather than a sketch.** Ordered by what the
next task needs. ⚠ **Task 1 changed on the day it started** — decision **9**, above, which the scoping
sitting did not find.

1. **The Outside Connection kind** — a `[[building]]` kind, edge-constrained placement, an Access Point,
   and a **declared throughput ceiling bounding arrivals**. ~~and `min(declared ceiling, Segment
   capacity)` **with which of the two binds reported**, because `adr/0088` makes that *"the whole
   readout"*.~~ 🔴 **The `min()` and the readout MOVED TO 12 with freight** — decision **9**, and
   [`adr/0088`](../docs/adr/0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md) is
   amended in place twice. ⚠ **Relocated, not discharged**, and it stays `adr/0088`'s.
2. **`[[hinterland]]`** — one per edge, authoring **the edge and what its emigrants carry, and nothing
   else** ([`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)).
   ✅ **DONE 2026-08-21.**
3. **`SyntheticCity` places gates**, so there is a world with a door in it — ⚠ **milestone 9's F17 is
   why this is a task and not an assumption.**
   ✅ **DONE 2026-08-21.**
4. **The arrival door** — `World.Arrive` creating unhoused **into the Pool at a gate**, and
   `UnplacedTable`'s second column ([`adr/0129`](../docs/adr/0129-the-pool-waits-at-the-gate-and-an-arrivals-trip-is-the-move-in.md)).
   The Command that drives it, since nothing decides to arrive until 16.
   ✅ **DONE 2026-08-21.**
5. **Money crosses** — the arriving balance drawn from the Hinterland, `MoneySupply.Issued`'s second
   writer, and ~~`Invariant.MoneyIsConserved` rewritten as **supply plus flow**~~ 🔴 **STRUCK: the
   invariant needed no rewrite at all** — F20.
   ✅ **DONE 2026-08-21.**
6. **The move-in Trip** — gate → dwelling, on placement, carrying real congestion. ⚠ **One Trip
   per Citizen**, and the arriving count is carried by the Command rather than modelled.
   ✅ **DONE 2026-08-21.**
7. **The unhoused Departure** — the duration bound, the derived occasion count, the dwellings-considered
   record, and a Departure that leaves through a gate
   ([`adr/0130`](../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)).
   ⚠ **The *"attempts-or-since"* column is `since`**, and ~~a Departure that leaves through a gate~~
   🔴 **makes no Trip** — an unhoused Household has no dwelling, so there is no origin to travel from.
   ✅ **DONE 2026-08-21.**
8. **Something to look at** — a runner mode showing arrivals, the Pool, departures and the money flow.
   ⚠ **It ships the world task 9 was specified against** — `rulesets/crowded.toml` — because **F25**
   found no such world existed.
   ✅ **DONE 2026-08-21.**
9. **The long acceptance run** — ⚠ **on a world where arrivals outpace housing**, because that is the
   only world in which the give-up bound and `adr/0006` can be read at all.
   ✅ **DONE 2026-08-21**, and it found **two production defects** — **F29** and **F30**.

**Struck by the decisions**: the comparison (16), rejected-arrival reasons (16), Settlements (their
consumer), Shipments (behind 12), any Goods trade (12), any counterparty scope (12), depth and recovery
(16), the generator's gate count and siting (24).

### Task 1 — the Outside Connection kind — ✅ **DONE 2026-08-20**

**What ships**: `[[building]] arrivals_per_day` (`KindDefinition.ArrivalsPerDay`), one loader refusal,
`MapEdge`/`MapEdges.Touching` in `Borough.Core.Space`, `World.IsOutsideConnection`,
`World.TryArrivalsPerDay`, `World.EdgeOf`, and `Invariant.OutsideConnectionStandsOnOneEdge` in **both
tiers** — `O(1)` in `World.CreateBuilding` and whole-world in
`WorldInvariants.OutsideConnectionsStandOnAnEdge` (F14). **31 new tests**; the assertion tier is
**1,827 green**. **No State Hash
moved**, and for milestone 9 task 4's reason restated: no shipped Ruleset declares a gate, so there is
no world in which the new column is non-zero. **Task 3 is what makes it observable.**

⚠ **The task's first act was to stop and settle decision 9**, which the scoping sitting did not find.
It is recorded above in full. Three more findings:

- 🔴 **F11 — a doc-comment that granted a permission outlived its premise, and this task was about to
  take it.** `RoadSegmentTable.CapacityPerDay`'s remarks said *"Nothing reads it yet, which is why the
  unit could be chosen freely"* and named the volume-delay function as an unbuilt consumer free to
  *"pick its own denominator without a migration."* **Milestone 6 shipped that function**: `LoadOf`
  reads the column and `TrafficDump` reads it again, so the unit has been load-bearing and
  hash-bearing since 5c task 6. ***A stale sentence about an absence is worse than a stale fact,
  because it is read as a licence rather than as a claim*** — and `plans/0002` §D1's speeds-and-
  capacities row records the **same** sentence going stale on the **same** column on 2026-08-19, one
  day earlier, without anything checking the doc-comment beside it. Corrected in place
  ([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)).
- 🔴 **F12 — `plans/0002` §D2's throughput row stated a *band* that compares two different units**, and
  a band is the one thing that makes an unratified number feel safe. *A road gate cannot exceed its
  Segment's 3,600 Vehicles/hour* brackets Households against Vehicles. **Withdrawn in place.** It is
  `plans/0012` **Cause 5** in its sharpest form: what failed to travel was not a clause but a
  **unit**, and a unit is invisible in a way a clause is not — you can see that a caveat is missing.
- ⚠ **F13 — the map had no boundary.** Under `adr/0021` the map is bounded and `CellGrid.WorldTiles`
  has been the extent since the grid was written, but **no symbol in `Borough.Core` asked whether a
  position was on it**. The obvious repair is a band in Tiles, which is a hash-bearing world-creation
  number needing a ratifier; ***the lattice lands on the boundary exactly***, so the constraint is
  stated with **no number at all**. `MapEdgeTests` holds `LotSubdivider`'s no-set-back premise so that
  a subdivider which later introduces one fails loudly rather than making every gate unplaceable.

- ⚠ **F14 — the write-site guard could not see the case that actually happens, found by reviewing the
  diff rather than by a test.** `arrivals_per_day` is what makes a kind a gate and a Ruleset is
  **hot-reloadable** (`adr/0015`), so adding the key to a kind whose Buildings already stand converts
  every one of them **without `CreateBuilding` being called once** — and `World.Adopt` already walks
  the Buildings on reload (`EvictOverflow`) with nothing to say about position. ***A guard at the
  write site checks the kind a Building was born with, and a hot-reloadable kind is not a property a
  Building was born with.*** Fixed inside task 1: `WorldInvariants.OutsideConnectionsStandOnAnEdge` is
  the whole-world half, on `Invariant.LotIsNotAlreadyBuiltOn`/`LotHoldsExactlyOneBuilding`'s pairing.
  ⚠ **It reports and does not repair**, which is where it parts company from `adr/0068`: lowered
  occupancy evicts because an Occupant can be moved, and a Building cannot be moved to the edge.

⚠ **One thing was deliberately not built: a `min()` with one operand.** `adr/0088`'s second ceiling is
at 12, so the readout is not half-shipped here — there is no *which binds* to report when there is one
bound. ***A formula with a term missing is not a smaller formula.***

### Task 2 — `[[hinterland]]` — ✅ **DONE 2026-08-21**

**What ships**: the `[[hinterland]]` section — `edge` and an `emigrant_balance_min`/`max` band —
`HinterlandDefinition`, `Ruleset.Hinterlands`, `Ruleset.TryHinterland(MapEdge)`, and **five loader
refusals** ([`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md)
recounted a fifth time: **81 at load, 114 sites**). **16 new tests**; the assertion tier is **1,843
green**. **No State Hash moved** — no shipped Ruleset declares a Hinterland, which is task 3, and
**no number reached a Ruleset, so no `plans/0002` §D1 row is owed yet**. That debt lands with the
file that states a band.

⚠ **The band has no reader until task 5, and that is a task boundary rather than a hole.** The draw —
`EmigrantBalance(WorldKey, entityId)` and its `PurposeTag` — ships with `World.Arrive`, on
[`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)'s
own rule applied one level down: *a Hinterland field is authored in the milestone that reads it*, and
what authors the field is not what reads it.

**Three things the shape of the object decided, none of them new arguments:**

- **Three required keys, no optional ones.** At 11 a Hinterland **is** an edge and a band, so there is
  no field left to carry meaning if the band is omitted — a table stating only `edge` declares that an
  economy exists and says nothing about it. That is `adr/0048`'s *loads clean and does nothing* class
  in the shape where the whole object is the thing that does nothing.
- **A zero band is accepted where task 1's `arrivals_per_day = 0` is refused**, and the two zeroes
  look alike. A gate admitting nobody is a door that never opens; a Hinterland whose emigrants carry
  nothing is a **poor economy**, and its Households still arrive, still enter the Pool and still have
  to be housed. ***A zero that is a real answer is not the same zero as one that disables the
  mechanism stating it.***
- ⚠ **The duplicate refusal is on a *value* and not on a name.** `[[policy]]` and `[[zone_rule]]` are
  not registered in a name table because nothing in a Ruleset refers to one, and a Hinterland is not
  registered for that same reason — but it **is** referred to, by the **edge a gate stands on**. So
  the collision that matters is two tables spelling `edge = "south"`, which is invisible until the key
  is read. ***A section can need a uniqueness check without needing a name.***

⚠ **Nothing pairs a gate kind with a Hinterland at load, and nothing could.** Which edge an Outside
Connection stands on is a property of where it was **placed** (`World.EdgeOf`), not of the Ruleset, so
a file declaring a gate and no `[[hinterland]]` is not refusable — the loader cannot see a world. **The
pairing is task 4's**, at arrival.

One finding:

- 🔴 **F15 — `Ruleset.WithLayers` had lost `Parking`, and the paragraph warning about exactly this was
  already sitting beside it.** `Ruleset` is a class rather than a record, so `with` is spelled by hand;
  milestone 10 task 5 found **seven** properties missing from that list at once and wrote the rule
  beside it — *every property added to this class belongs in this list*. Milestone 7 then added
  `Parking` and did not, so a Ruleset put through `WithLayers` came back at `ParkingRuleset.None` — **no
  radius, no shed, arrival never parks** — with no refusal and no throw. Found here the same way the
  first seven were: by a **twelfth** property needing to be threaded. ***A rule written in prose beside
  the code it governs is not a check on that code***, and two sightings one milestone apart is the
  evidence. ⚠ **`RulesetShape` is not the guard either** — it compares *structure* under `adr/0015`, so
  a Ruleset that lost its radius compares **equal**, which is correct for its question and useless for
  this one. Fixed, and `RulesetWithLayersTests` now enumerates the class's properties by reflection and
  holds the list to them — `RefusalCountTests`' shape one level in: **code against code** where that
  one is a document against code. ⚠ **It checks that each name is assigned, not that it is assigned
  from itself**, which is narrower than it could be and is the whole of the failure observed twice.

### Task 3 — `SyntheticCity` places gates — ✅ **DONE 2026-08-21**

**What ships**: `rulesets/bordered.toml` — the **tenth** shipped Ruleset, `minimal.toml` plus a `port`
kind carrying `arrivals_per_day = 12`, **four `[[hinterland]]` blocks**, `arterial_count = 16` and
`[households] car_ownership_percent = 100` — and `SyntheticCity.RaiseGates`, which raises **one
Outside Connection on every map edge**. **8 new tests**; the assertion tier is **1,855 green**. **No State Hash
moved**: the new pass returns 0 on a Ruleset that declares no gate kind, so the other nine files walk
exactly the Lots they always did.

⚠ **It took a second sitting and a second pair of changes, because the task shipped believing two of
the four edges were unreachable.** They were, and the cause filed on the day was wrong twice over —
see **F17** and **F18** below. What reaches an edge is `SyntheticCity.ReachesTheBoundary`, which paves
to `CellGrid.WorldTiles` whenever the Ruleset declares a gate kind, plus `CarveEdgeBlock`, which
subdivides the one lattice block carrying each edge. Neither alone is enough: ***paving to the
boundary puts a Street on the edge and no Lot beside it.***

**The count and the siting are derived rather than chosen, which is what keeps them out of `0002` §D2.**
A gate count would be a hash-bearing world-creation number needing a ratifier, and
[`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)
put that number at **24** with the generator. *How many edges does the land touch* is a property of the
land, so it needs no key — `PavedTiles`' move for the extent, and `adr/0059`'s for a Zone Rule's sample.

⚠ **Gates go up before the dwellings, and the order is forced.** An edge Lot is an **early** Lot —
`Subdivide` walks blocks in lattice order from the origin corner — so by the time the dwelling loop has
taken what it wants, every edge Lot is built on and no gate can be placed at all. It costs an offset in
`SyntheticCity.Dwelling`, because gates now occupy Building slots `0..gates-1`. ***A Household housed
IN the gate is not a Household that came THROUGH it***, and without the offset the first ones would
have been.

**Two `plans/0002` §D2 gaps became §D1 debts**, which is the transition that table exists for: the
throughput ceiling (`arrivals_per_day = 12`) and the emigrant balance bands. ***A gap becomes a debt on
the day a file states a number.*** Both name milestone 11 **task 9**'s acceptance run as their machine
and `bordered.toml` on a world where arrivals outpace housing as their world.

One finding, and it was found by measurement before any code:

- 🔴 **F17 — two of the four map edges cannot be reached by any world this build can generate, and
  every Hinterland behind them is a number no run can refute.** `SyntheticCity.PavedTiles` sizes the
  Street lattice to the Lots the world was allocated for rather than to the map, so it runs from the
  origin corner and **stops**. Measured on `minimal.toml` at 1,000 Citizens: **160 paved Tiles of
  16,384**, 124 Lots, `maxEast = 160`, `maxNorth = 96` — **6 Lots on the west edge, 15 on the south,
  none on a corner, 103 inland**. Reaching `CellGrid.WorldTiles` would take on the order of **2.6
  million Lots**. ⚠ **The good half is that a gate is placeable at all**: the lattice starts at the
  origin, so `east = 0` and `north = 0` Lots exist and **task 1's exact-boundary constraint holds with
  no band and no number**, exactly as F13 argued it would. 🔴 **The bad half is `adr/0125`'s
  unreachable ratifier for the third time** — `plans/0034`'s **F18** was the second, four hours after
  the ADR was written. ***An edge a generator cannot reach is a market nothing can arrive from.*** ✅
  **The difference this time is that it was known on the day rather than after the run**, so the north
  and east bands are filed in `0002` §D1 as **unratifiable until milestone 24** and `bordered.toml`'s
  header says so in the file. ⚠ **All four are authored anyway, and that is the user's decision taken
  with the cost stated**: `CONTEXT.md` → Hinterland makes four comparable markets the thing that gives
  the Outside a referent at all, and a file showing two would not show that shape.
  ~~`GatePlacementTests.Only_the_edges_the_lattice_reaches_get_a_gate` asserts the **set** rather than
  a count, so it goes red in either direction and reopens the two rows rather than letting them
  settle.~~ ✅ **STRUCK THE SAME DAY. The test asserts all four and the two §D1 rows are closed** —
  and what struck it is **F18**, immediately below, which is also the record of what this finding got
  wrong. ⚠ **The generator was one of three named blockers and the only real one.** Milestone 24 was
  named as the trigger and it was not owed the work: paving is a property of the Ruleset's door, not
  of the generator's siting policy, and the siting policy is what 24 owns.

- 🔴 **F18 — the far edges were unreachable for one afternoon, the deferral to milestone 24 was
  wrong, and the second blocker named in its place was wrong too.** Repairing F17 took **two**
  changes rather than the one it names. `SyntheticCity.ReachesTheBoundary` paves to
  `CellGrid.WorldTiles` whenever the Ruleset declares a gate kind — it states no number, because
  *does this Ruleset declare a door* is a property of the Ruleset exactly as `PavedTiles`' extent is
  a property of the population, and it costs **no allocation at all**: `RoadGraph.ExpectedNodes` is
  `(WorldTiles ÷ block_tiles + 1)²` and has never read the extent, so the capacity was always
  reserved. Measured at `block_tiles = 32`: **36 nodes and 61 Segments** at the ordinary extent
  against **263,169 and 535,817** at the map's, laid in **150 ms**. And that is *necessary and not
  sufficient* — `Subdivide` walks blocks from the origin and stops as soon as it has Lots for the
  population, so ***paving to the boundary puts a Street on the edge and no Lot beside it.***
  `CarveEdgeBlock` subdivides the one block carrying each edge, at `(Blocks - 1, 0)` and
  `(0, Blocks - 1)` rather than the far corner, so neither far gate lands on a Lot touching two edges.
  ⚠ **It cost an off-by-one worth recording**: `StreetGrid.Blocks` is `Span - 1` — **blocks, not
  lattice lines** — so the last block is `Blocks - 1`, and reading it as lines produced two gates
  instead of four with nothing saying why.

  🔴 ⚠ **The blocker named in the generator's place was the Commute Budget, and it is not a blocker
  either — it is `adr/0089`.** The claim was that a gate 64,896 m from a corner city is 779 walking
  minutes and 78 driving against a 50-minute ceiling, so paving buys a gate that stands and nobody can
  reach. **Measured** on `bordered.toml` at 1,000 Citizens, gate to nearest dwelling by car: west and
  south **0** minutes, east **62**, north **73**, against a ceiling of **49**. With
  `arterial_count = 0`: **78** and **80**. So sixteen Arterials buy **16 minutes on one edge and 7 on
  the other**, neither reaches the ceiling, and a pure-Arterial run of that distance is **43** minutes
  with no route pure. ***A far gate is made usable by a dwelling beside it, not by a faster road.***
  And the distance is [`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)
  **working rather than failing**: the map is sized by how many Commute Budgets fit across it, so a map
  several budgets wide puts its far edge outside one **by construction**. ⚠ **A budget that binds
  where the design says it should is not a defect, and reading it as one is how a milestone acquires
  work that belongs to nobody.**

  🔴 **What this leaves for task 6, routed there rather than worked around**
  ([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)):
  `TripEngine` judges the Commute Budget on **every** Trip and not only on a commute, so a move-in
  from a far gate to a corner dwelling resolves `TripFate.ExceededCommuteBudget`. The carved block
  leaves vacant Lots beside every gate — measured, at 1,000 and at 4,000 Citizens, the far blocks hold
  Lots and the dwelling loop runs out of Households before reaching them — so **an arrival placed in
  reach of the gate it came through makes a Trip that completes, and one placed in the corner city
  does not.** That is a placement question, and task 6 is where it is answered.

  ⚠ **`GatePlacementTests.A_far_gate_is_routable_and_still_beyond_the_budget` holds both halves** —
  every gate has a finite car route, and the far two have **zero** dwellings inside the Budget. It
  goes red on either, which is what keeps *routable* and *reachable* from collapsing into one word
  again.

### Task 4 — the arrival door — ✅ **DONE 2026-08-21**

**What ships**: `World.TryArrive`, `UnplacedTable.Gate`, `BuildingTable.ArrivalsToday` and
`ArrivalDay`, `CommandKind.Arrive` with `ArrivePayload`, `Simulation.ApplyArrive`, two Invariants —
`AnArrivalCrossesAnOutsideConnection` at the write site and `ThePoolsGateIsAnOutsideConnection`
whole-world — and the codec learning two verbs. **13 new tests**; the assertion tier is **1,864
green**.

🔴 ⚠ **The State Hash moved, deliberately, and the four golden baselines were re-recorded.**
`BuildingTable` gained two saved columns, so every world with a Building in it folds differently.
**No seed bump**, and `World.HashSeed`'s own note is why: *new state is a design change under
`05 §4` — the city genuinely has more in it, the baselines move because the world moved, and signing
that would file a real change as a bookkeeping one.* Nothing but the four golden tests failed, which
is the evidence that no behaviour moved with them.

**A Household can now exist here having never lived here, and that took a second door.**
`World.CreateHousehold` demands a dwelling and `World.Unplace` reports
`OnlyAHousedHouseholdIsUnplaced`, so between them the build could not hold one — while `CONTEXT.md` →
Unplaced Pool says the four entry routes *"all enter on equal terms."* ***A door the design describes
and an invariant refuses is a disagreement, not a defect***, and this is which one moved.

**Nobody makes a Trip, and that is `adr/0129` rather than a gap.** `adr/0023` reads *arrive as Trips,
enter the Pool, house themselves*; `TripTable.Start` takes an origin **and a destination**, and a
Household the Pool has not placed has no destination. The move-in is **task 6**'s.

**The ceiling binds, which is what makes `arrivals_per_day` ratifiable rather than merely stated.**
It is a *rate*, so meeting it takes a count and the Day the count belongs to — hence two columns
rather than one. ⚠ **A per-call bound was rejected as milestone 9's F13**: two arrival events in one
Tick would each take the whole quota, and the thing would read as a daily ceiling while being nothing
of the kind. The reset is **lazy** rather than a per-Day sweep, which costs nothing on the Buildings
nobody arrives at — all of them, in nine of the ten shipped Rulesets — and is right for a world
loaded mid-Day.

⚠ **The two refusals are deliberately different in kind.** A full gate returns `false` and reports
**nothing**: that is the mechanism working, and putting it in the crash artifact would bury the real
fault under a busy Day. A Building that is not a gate returns `false` **and** reports, because the
gate becomes the move-in Trip's origin and the mistake would otherwise surface at placement, long
after the call that was wrong. ***A bound that binds is not a violated invariant.***

⚠ **The verb resolves a Tile to a gate EXACTLY, not to the block it falls in**, which is where it
parts company from `Simulation.OccupiedBuildingIn`. On the shipped lattice **one block carries two
edges** — `CarveEdgeBlock` puts the west and south gates in the block at the origin — so *the gate in
this block* names two Buildings standing in two different Hinterlands, and picking either would
invent the answer to which market the arrivals came from.

One finding, and it was not this milestone's:

- 🔴 **F19 — the Input Log codec could not write two of the seven declared verbs, and the test that
  should have caught it was named after the property and written as a list.** `CommandKind.Populate`
  (milestone 5a) and `CommandKind.Trip` (milestone 5b) were declared, applied by `Simulation`, and
  absent from `InputLogCodec` in **both** directions — `Write` threw *a command with no verb cannot
  be written*, and a hand-written `trip` line was refused as *not a verb this format knows*. ***A verb
  the simulation applies and the codec cannot spell is a session that cannot be reported***, and a log
  is what a crash artifact is made of. `InputLogCodecTests.Every_declared_verb_survives_the_round_trip`
  was a `[Theory]` over **four** hardcoded `[InlineData]` verbs, written when the enum had four, and
  its **name already claimed the whole set** — so the drift sat in the file for two milestones reading
  as covered. ***A test named after a universal and written as a list is a list wearing a proof's
  name.*** Repaired by reflecting over the enum, so the next verb is in the test before anybody writes
  a line of it. Filed as [`plans/0012`](0012-corpus-audit.md) **Cause 1, fourth form** — the first
  sighting of that cause in which the copy that drifted was a **test**.

### Task 5 — money crosses — ✅ **DONE 2026-08-21**

**What ships**: `HinterlandDefinition.EmigrantBalance`, `PurposeTag.EmigrantBalance`, the endowment
inside `World.TryArrive`, and `Invariant.AGateOpensOntoAHinterland`. **5 new tests**; the assertion
tier is **1,869 green**. **No State Hash moved** — nine of the ten shipped Rulesets declare no gate,
so `TryArrive` is never reached on any baseline.

**A world's money supply is no longer a constant**, which is this milestone's Definition of done in
one line. `MoneySupplyTable.Issued` has had exactly one writer since milestone 10 and now has two.
`World.Endow` is still the only door: it deposits through the Bin's wait list and writes the anchor in
one call, so there is no spelling in which the second half can be forgotten (`adr/0031`).

**The draw is `OpeningBalance`'s, on a different tag, and the tag is where the argument is.** Uniform
over the band with no shape parameter — a skew is a second decision with a number in it and nothing
has measured which (`adr/0052`) — on the Household's **monotonic id** at Tick 0, consumed once
because an endowment is *issued* and cannot be recovered by redrawing. ⚠ **Sharing
`PurposeTag.OpeningBalance` would have been the subtlest correlation in that enum and would have
collided with nothing**: the populations do not overlap, a Household is founded *or* it arrives — but
the same id takes the same fraction of whichever span it is given, so ***the family that would have
been richest at the founding is the richest emigrant from every edge***. A correlation between two
populations that never meet is one nothing in the city can refute.

🔴 ⚠ **A gate whose edge has no `[[hinterland]]` admits nobody, and the refusal is F13 rather than
strictness.** Task 2 recorded that nothing pairs a gate kind with a Hinterland at load and nothing
could — which edge a gate stands on is a property of where it was *placed*, and the loader cannot see
a world — so the pairing happens at arrival. The alternative was admitting them carrying zero, and
zero is a **legitimate** emigrant balance: task 2's own record says a Hinterland whose emigrants
arrive penniless is a poor economy rather than an unset field. ***A zero that is a real answer cannot
double as the absence of an answer.*** ⚠ **The two checks that are not built are named rather than
omitted** (`adr/0070`): one in `World.CreateBuilding`, which would say so at placement, and a
whole-world walk, which would catch a reload removing a `[[hinterland]]` from under a standing gate —
**F14's shape a third time**. Neither is built because no shipped Ruleset can produce either.

One finding, and it is about this plan rather than about the build:

- 🔴 **F20 — this milestone's task 5 said `MoneyIsConserved` would be *rewritten as supply plus
  flow*, and nothing needed rewriting. The column had said so a milestone in advance.**
  `MoneySupplyTable.Issued` is declared as money that has entered **net of anything that has left
  it**, and its own doc-comment reads *"milestone 11 gives it its second writer … the gate moves this
  and the invariant is unchanged."* `Invariant.MoneyIsConserved` carried the matching sentence.
  Because `World.Endow` writes the anchor in the same call that deposits, an arrival moves both sides
  together and the equality stays exact. ***A flow term is only owed where the two sides are arrived
  at on different schedules***, and one door that writes both is what makes sure they are not.
  ⚠ **This is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
  from the other side**: that rule says a sentence about a mechanism tells you which symbol to read
  and never what is in it — and here a **plan** described work the build had already made
  unnecessary, which a scoping sitting could not have known and a reader of the symbol would have.
  The task list was written before the symbol was read. **Struck in the list rather than deleted**,
  so the correction is legible.

### Task 6 — the move-in Trip — ✅ **DONE 2026-08-21**

**What ships**: `TripPurpose.Immigration`, `PlacementEngine.MoveIn`, `ArrivePayload`'s **Citizens**
nibble, and `World.TryArrive` creating the people it admits. `Simulation` builds the `PlacementEngine`
**after** the `TripEngine` so it can be handed one. **3 new tests**; the assertion tier is **1,872
green in 50 s**. **No State Hash moved** — nine of the ten shipped Rulesets declare no gate, so nothing
on a baseline reaches a move-in.

**The Trip is started at the placement site rather than armed**, because a move-in happens once per
journey and has no recurrence — which is what separates it from `Commute`, a daily occasion that is a
*phase*. And it is **one Trip per Citizen rather than one per Household**, because
[`adr/0075`](../docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) makes a Traveller a
cursor over a **Citizen's** journey and there is no such thing as a Household on the road. That is also
what makes the congestion real: a Household of four arriving is four Vehicles under `adr/0098`'s
per-Household mode, and collapsing them to one would understate the thing this task exists to produce.

⚠ **Where the arriving Citizens come from is the Command, and that is a decision rather than a
default.** `TryArrive` produced member-less Households, nothing in the build models Life Stage →
composition, and inventing a distribution here would have been a hash-bearing number with no ratifier
([`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)).
So `ArrivePayload` was repacked into one word — Households **8 bits**, Life Stage **4**, Citizens
**4** — which needed **no Input Log version bump**, and the verb keeps the posture it already had for
Life Stage: ***an instrument states what it is standing in for, and does not model it.***

🔴 **The question task 4 routed here is answered *no*, and the answer is a decision this milestone had
already made.** Task 4 asked whether an arrival placed beyond the Commute Budget from the gate it came
through is a **placement** question. It is not. `MoveIn` does not inspect the Fate and does not retry:
a move-in that exceeds the Budget is
[`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) working — the map
is sized by how many Budgets fit across it, so a far gate is outside one **by construction**, measured
on `bordered.toml` at east **62** minutes and north **73** against a ceiling of **49** — and the
Household is housed either way. ***Placement decides where somebody lives; the Trip records how they
got there.*** ⚠ **Making placement prefer a dwelling near the gate would be the comparison, arriving
five milestones early**: that is decision **1** of this milestone, settled as *it does not compare
anything here; the milestone splits*, and reacting to the Fate is the acceptance model at **16**.
So the far gate's unusability is not repaired here and is not a defect — ***a far gate is made usable
by a dwelling beside it, not by a faster road.***

Two findings, and the first is a defect this work introduced.

- 🔴 **F21 — a hand-rolled walk over an encoded column walked off the end of the Household and round
  the whole Citizen table.** `MoveIn`'s first form read the member list directly:

  ```csharp
  for (int citizen = _world.Members.PeekFront(household);
       citizen >= 0;
       citizen = _world.Citizens.MemberNext[citizen])
  ```

  `IndexList` stores `next` **1-based** — `_next[tail] = node + 1`, and **0 is the terminator**
  (`IndexList.cs:124,132`). `PeekFront` decodes it; the loop did not. So it was wrong **twice over**:
  off by one after the first member, and `0` passes a `>= 0` test, so the walk left the Household at
  Citizen slot 0 and went through the whole table and round again, starting a Trip per step without
  bound and recording route hops on each. ***A hand-rolled walk over an encoded column is a decode
  somebody has to remember.*** This was the **only** place in the repository reading `MemberNext` by
  hand; every other list walk goes through `IndexList.Walk`, and now this one does too.

  ⚠ **Found by measurement rather than by review, and two plausible readings of the stack were both
  wrong.** The test was OOM-killing the machine; accumulation across Ticks and a single runaway route
  were each read into the trace and neither was it. A probe settled it — one route records **2** hops,
  while the member walk ran to **100,000**, which is a cycle guard rather than a length.
  ***A number that is round is a limit, not a measurement.***

  | | before | after |
  |---|---|---|
  | `ArrivalTests` peak RSS | 10,651 MB | **603 MB** |
  | `ArrivalTests` duration | 59 s | **3 s** |
  | `ArrivalTests` result | 19/20 OOM | **20/20** |

  ⚠ **Those durations were taken while other work ran on the same six cores, so they are upper bounds
  and not figures for a document to quote** ([`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md),
  and `CLAUDE.md`'s rule that a test-cost capture is a parallelism measurement). The **result** row is
  not a timing figure and is not subject to that caveat.

  The regression test is `ArrivalTests.Placing_an_arrival_starts_a_move_in_trip_from_its_gate`, and it
  is a **count** — two Citizens must produce **exactly two** Immigration Trips. An unbounded walk fails
  it on the number rather than on the clock, which is what keeps the guard from depending on a machine
  that happens to run out of memory.

- 🔴 **F22 — a filed sweep is a counted list, and it drifted in both directions while it waited.**
  `plans/0012` holds *A world's seed has two sources*: `World.Key` exists, and every other mutator
  still takes a `WorldKey` **parameter**, so one world has two sources for one seed with nothing
  checking they agree. Task 5 added `TryArrive` to that list without noticing — ***a new instance of a
  filed pattern is not caught by having filed it*** — and task 6 removed the parameter again. In the
  other direction, the entry names `DestroyBuilding` as a member and `DestroyBuilding` no longer takes
  one, so the ledger's own example had gone stale. **The entry is corrected on the day** rather than
  worked around ([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)),
  and it now names its members explicitly instead of *"and the rest"*.

  ⚠ **This is `plans/0012` Cause 1 with the drifted copy inside `plans/0012`.** The audit ledger is
  itself a document that stores a fact — *which symbols carry this defect* — and it has no mechanism
  keeping that list true. **F19** was the first sighting where the drifted copy was a **test**; this is
  the first where it is the **audit**. ***A ledger of debts accrues debt.*** No detector is proposed:
  a check that a named symbol still has a named parameter is a mechanical test the corpus suite could
  hold, and it is filed rather than built because one instance is not a pattern yet.

### Task 7 — the unhoused Departure — ✅ **DONE 2026-08-21**

**What ships**: `[placement] gives_up_after_days`, the Pool's `Since` and `Considered` columns,
`PlacementEngine.GivesUp`, `World.Depart`, `Invariant.OnlyAnUnhousedHouseholdGivesUp`, and
`PlacementActivity`'s third flow. **13 new tests**; the assertion tier is **1,885 green in 46 s**.
🔴 **The State Hash moved and the two golden traces were re-recorded, with no seed bump** — two new
saved columns are new *state*, which `World.HashSeed`'s own note calls a design change under
`05 §4`; signing it would file a real change as a bookkeeping one. `world-hash.txt` did **not** move,
because the Pool is empty in a `minimal.toml` world and a column over no rows folds nothing.

***`adr/0006` is discharged for the Pool by a mechanism rather than by an absence, for the first
time.*** Until the gate opened, nothing created a Household after world creation, so the Pool was a
subset of a fixed population and could not grow with elapsed time whatever it did. The gate removed
that reason four tasks ago and this is what replaces it.

**The bound is a duration and the Pool stores a *start Tick*, which is `adr/0130`'s consequence
resolved.** That record says the table *"gains an attempts-or-since column"* and leaves the choice
open; it is **since**, and the argument is that `PlacementEngine` draws its sample rather than
sweeping — so under an occasion bound a Household that is never picked accrues no occasions and never
leaves, in a Pool that is growing. ***A bound whose clock only advances when you are lucky is not a
bound.***

🔴 **The loader refuses a Ruleset that declares a gate kind and no give-up duration, and that is
`CONTEXT.md`'s sentence made mechanical.** *"Whoever builds the gate owes the give-up rule in the same
milestone"* was a thing somebody had to remember; it is now `adr/0048`'s 116th refusal site. ⚠ **What
it tests is a declared *kind*, not a placed gate** — and that line is exactly the one **task 5** drew
when it found the gate↔Hinterland pairing *could not* be checked at load, because which edge a gate
stands on is a property of the world. ***A kind is a fact about the file and a Building is a fact
about the world***, and a loader may only refuse on the first. The nine files with no door in them
may omit the key, because `adr/0054`'s reasoning still stands for every one of them.

**The money leaves with them, and the question that was waiting turned out to be already answered.**
`World.DestroyHousehold` has carried a remark since milestone 10 saying the balance is destroyed, that
the omission is deliberate, and that *"the first production caller of this method is what has to answer
it"*. `World.Depart` is that caller. ⚠ **But the answer was not this sitting's to choose**:
[`adr/0024`](../docs/adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) says in its
first paragraph that *"the Outside Connection is its only source and sink"*, so a Household walking out
of the city carrying its savings is the **only** disposal the corpus permits — there is no escheat, no
estate and no treasury claim, and each of those is *undesigned* rather than merely absent (`adr/0070`).
***What was genuinely open was not what happens to the money but which caller writes it down.*** It is
written in `Depart` rather than in `DestroyHousehold`, because destroying a Household is a table
operation with several callers and only one of them means somebody emigrated — folding the supply write
inward would make every future caller silently claim an emigration. Conservation stays an **exact**
equality with no flow term, which is **F20** arriving from the other direction.

⚠ **No Trip is made, and that is not an omission.** The move-in at task 6 is a Trip because both its
endpoints exist. An *unhoused* Household has no dwelling by definition, so there is no origin Address
to travel from and a Trip from the gate to the gate is not a journey. ***The housed Departure is the
one with a journey in it***, and that channel is milestone 16's with the comparison (`adr/0128`).

Three findings.

- 🔴 **F23 — `rulesets/bordered.toml`'s header contradicted itself, and the false half was the one
  carrying an argument.** Line 13 lists `[households] car_ownership_percent = 100` as one of the four
  things the file adds; seventy lines below, the `arrivals_per_day` paragraph read *"this file states
  no `[households]`, so nobody drives and the Segment term would bound nothing"*. The file declares
  the table at line 234, so **everybody here drives** and the Segment term **would** bind on
  something. The conclusion it supports — that `adr/0088`'s `min()` moved to 12 with freight — is a
  decision and is unaffected; what was wrong was the reason given for it.
  ⚠ **This is `plans/0012` Cause 1 arriving WITHIN a single file**, which every prior sighting had
  been between two. And the same header contains the paragraph *"a copied comment is a second copy
  that drifts"*, written by task 3 after it deleted 581 copied comment lines for that exact reason.
  ***A file that documents a failure mode is not thereby immune to it***, and the drift here was
  cheaper to make than the one it warns about: nobody copied anything, the file simply grew a
  `[households]` table at task 3's second sitting and the paragraph that denied one was never re-read.
  Repaired in place with the correction recorded, and the *"nothing arrives through it yet"* paragraph
  — true at task 3 and false since task 4 — rewritten at the same time.

- 🔴 **F24 — a revisit period is a rate and not coverage, so the give-up bound has an expectation and
  no upper bound.** `PlacementEngine.DrawPool` takes `sample` **independent uniform draws** over the
  Pool and deduplicates nothing — a draw **with replacement**. So over one revisit period each member
  is looked at about once and about **1/e of them are not looked at at all**, and a Household waits a
  *geometric* number of periods past its duration rather than at most one.
  ⚠ **Found by a test failing, and the first draft of the doc-comment asserted the false thing** — it
  said the lateness was *"bounded by one revisit period by construction, because that is what a
  revisit period is"*, which is reasoning from the name. ***A period is what something is called, not
  a guarantee about what it covered.***
  **`adr/0006` is satisfied anyway, and by the sample rather than by the bound**: the per-member draw
  probability is `interval ÷ revisit_ticks`, a constant independent of Pool size, because
  `SampleFor` scales the sample *with* the Pool. So the drain rate is proportional to the stock and
  the Pool's **size** is bounded even though one Household's **wait** is not. ⚠ **Those are two
  claims and only the first is what `adr/0006` asks.** Whether the second should also hold is filed
  rather than decided here — a rotating cursor would buy it and is a hash-bearing change to placement
  that this task does not own (`adr/0073`).
  **Routed**: the wording defect in `PlacementRuleset.RevisitTicks` — *"how long the placement pass
  takes to look at everybody in the Unplaced Pool once"*, which is not what a draw with replacement
  does — goes to [`plans/0012`](0012-corpus-audit.md), because it predates this milestone and the
  same sentence is in the loader's refusal text.

- 🔴 **F25 — no shipped world has a housing shortage in it, and eleven tests failed on their fixture
  before one failed on its assertion.** The first draft of `DepartureTests` assumed a generated city
  leaves people waiting — that a non-empty Pool *is* the statement that no dwelling has room.
  `SyntheticCity` houses **everybody**, so the Pool is empty at world creation and every test that
  needed a Household to fail to find a home was testing nothing.
  ***A world with a housing shortage in it has to be built, and this milestone is the first thing that
  ever needed one.*** What builds it is one call rather than a rig: `World.DestroyBuilding` evicts its
  Occupants into the Pool with their balances intact (`adr/0054`), so flattening every dwelling puts
  the whole population into the Pool and leaves the Lots standing empty — after which every candidate
  draw lands on a vacant Lot and `TryHouse` cannot succeed **by construction rather than by luck**,
  which is what a fixture for a bound has to be. ⚠ **This is task 9's problem arriving early**: the
  acceptance run is specified *on a world where arrivals outpace housing*, and the reason that clause
  is in the plan is the same reason these tests failed.

### Task 8 — something to look at — ✅ **DONE 2026-08-21**

**What ships**: `--arrivals`, the **eleventh** runner mode; `rulesets/crowded.toml`, the **twelfth**
shipped Ruleset; and `PlacementCounter.Departed` reaching the Census. **8 new tests**; the assertion
tier is **1,895 green in 50 s**. **No State Hash moved** — a dump reads.

**Four quantities rather than one, because the milestone's mechanism is a pipe with two ends.**
Arrivals are a flow in, the Pool is the stock between, Departures are the flow out, and the money
supply is what all three move. ***A picture of any one of them is a picture of a symptom*** —
`CONTEXT.md` → Departure is explicit that a large Pool can be a healthy city and a small one a city in
crisis, and that only the flow tells them apart.

🔴 **It is the first dump that issues Commands, and that is forced rather than chosen.** Every other
mode steps empty Ticks and watches the city act; this one cannot, because
[`adr/0128`](../docs/adr/0128-the-gate-ships-before-the-comparison-that-walks-through-it.md) puts the
comparison at 16 and **nothing in the build decides to arrive**. So a rate had to come from somewhere.
⚠ **It comes from the Ruleset**: the mode asks each gate for **more than it can take**, once a Day, and
what is admitted is `arrivals_per_day` clipped by the gate itself. ***A demonstration that chose its own
rate would be showing the demonstration.*** The **asked** column is printed beside **admitted** so the
clipping is visible rather than implied, and the test that asserts `refused` is non-zero is the one
that keeps the picture honest.

**`rulesets/crowded.toml` is `bordered.toml` with two numbers changed**, and both are stopwatch
settings: `arrivals_per_day` 12 → 96, so the doors admit faster than the city builds, and
`gives_up_after_days` 120 → **2**, so the Pool's sink is reachable inside a run somebody will sit
through. ⚠ **Neither is a second opinion about the design's numbers** — at 120 Days a demonstration
would need **245,760** Ticks to show one Departure, and `plans/0034` **F17** is what happens when a
mechanism ships correct and unobservable. The header says so at the top, because ***a caveat attached
to a number does not travel with it.***

**It is also the world task 9 was specified against** — *"on a world where arrivals outpace housing,
because that is the only world in which the give-up bound and `adr/0006` can be read at all"* — and
**F25** is the record of discovering no such world existed. Building it here rather than at 9 is the
plan's own prerequisite arriving when something first needed it.

⚠ **`PlacementCounter.Departed` was added to the Census here and not at task 7, which is a task-7
defect this task found.** Task 7 put a third flow on `PlacementActivity` and stopped: the counter
existed, nothing carried it into the instrument layer, and `--census` could not print it. ***A flow
that reaches no instrument is a flow nobody can read***, and the milestone whose Definition of done is
*there is something to look at* is exactly where that shows up. The dump itself drains the engine
directly rather than reading the Census, and ⚠ **the two are mutually exclusive rather than merely
different**: `Census.Observe` drains the same engine, so a dump doing both would read each flow at
whichever ran second and get **zero**.

Two findings, and one is about a test suite rather than about the city.

- 🔴 ~~**F26 — a gated world costs 38.7 ms a Tick at 1,000 Citizens, and the cost does not move with
  population.**~~ **WITHDRAWN 2026-08-21, the same day, and replaced by what the measurement was
  actually of.** The 38.4 ms is `Simulation.VerifyDecideWritesNothing` — a **debug guard**, on by
  default, that folds the whole world **twice per Tick** to check `adr/0037`'s claim that Decide
  writes nothing. With `--no-decide-guard` the same world runs at **0.51 ms a Tick**, against
  `minimal.toml`'s **0.16 ms**. On 535,817 Segments a full-world fold is **~19 ms**, and two of them
  is the entire gap.
  ⚠ **The guard's own doc comment had said so all along** — *"`O(world)` against a phase that is meant
  to be `O(woken)` … turn it off for the 100,000-Tick test and leave it on everywhere else"* — which
  makes this [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
  from the wrong end: ***the sentence naming the symbol was there, and the measurement was taken
  without reading it.***
  🔴 **And the population claim inverted once the guard was off**: 100 / 1,000 / 4,000 Citizens read
  **0.59 / 0.66 / 1.09 ms**, a fixed map floor plus a term that grows with the city — the shape a Tick
  is supposed to have. Under the guard all three read the same, which is what made *independent of
  population* look like a finding. ***A constant that swamps a signal makes every input look like it
  does not matter.***
  ⚠ **The withdrawn entry was the only figure in [`plans/0013`](0013-tick-budget.md) not taken with
  `--no-decide-guard`**, and it was filed beside figures that were, without saying so — which is
  **Cause 5** in its own home: ***a number quoted away from the conditions that qualify it.*** The
  ledger entry is replaced rather than deleted, and it now carries both tables.
  **What survives as real**: nothing tells an operator the guard is on, so a long run on a large world
  is ~75× slower than it needs to be in silence; and **task 9's acceptance run must pass
  `--no-decide-guard`**, on the guard's own instruction.
  ⚠ **The user asked for this.** The entry as filed said *the consumer is not named and this entry
  does not have it* — which was honest, and honesty about a gap is not a substitute for closing it.
  ***An unattributed cost is a guess with a number attached***, and the guess was wrong in both of its
  claims.

- 🔴 **F28 — a guard written against a missing key does not cover a missing table, and the case it
  missed is the worse of the two.** Task 7's loader refusal fires when a Ruleset declares a gate kind
  and states `[placement]` **without** `gives_up_after_days`. It says nothing when a Ruleset declares a
  gate and states **no `[placement]` at all** — which has an inflow into the Unplaced Pool, no housing
  *and* no sink, so the Pool grows without bound in the strongest form of exactly the `adr/0006`
  failure the refusal exists to prevent. Found while building a Ruleset variant for the F26 diagnosis,
  and closed the same day: `adr/0048`'s **117th** refusal site, with the test beside it.
  ⚠ **It is worth its own entry because of how it was found.** Nothing was looking for it — the
  variant was built to turn placement off for a *timing* experiment, and the file loaded when it should
  not have. ***A guard is written against the case its author was thinking about, and the case they
  were not thinking about is not covered by the argument that justified it.***
  ⚠ **The refusal turned sixteen existing tests red on the day it landed, and that is the finding
  rather than the cost.** `OutsideConnectionRulesetLoadTests` and `OutsideConnectionPlacementTests`
  between them author four gate fixtures, and **not one of them stated a `[placement]` table** —
  because none of them runs a Pool, so nothing they assert was ever wrong. They are the shape the
  refusal describes, sitting in the corpus since task 1: ***a fixture that never exercises the
  mechanism it declares is where an unauthored obligation hides***, and the count is what the hole
  was worth. Each now carries the four `[placement]` keys and a `gives_up_after_days`, with a remark
  saying the sink is there because the gate is. ⚠ **The two fixtures spelling `arrivals_per_day = 0`
  and `-1` deliberately do not carry it** — neither is a gate, and appending a sink to them would
  hide the refusal they exist to watch fire.
  ⚠ **And F16's failure mode reappeared while closing this one.** The new test's doc block was
  inserted between an existing block and its `[Fact]`, so two summaries bound to one member and the
  method above lost its documentation. `DocCommentAttachmentTests` named it by file and line in 82 ms.
  ***The check earns its place by catching the hand that wrote it***, which is the only evidence a
  mechanical detector was worth building rather than a rule worth remembering.

- 🔴 **F27 — six tests sharing one fixture built it six times, and `TierBudgetTests` could not have
  caught it.** The first draft of `ArrivalDumpTests` ran an identical four-Day session per test:
  **1m30s**, which would have tripled the working-loop tier for one feature. Cached to one run it is
  **18 s**, and the tier went 46 s → **50 s**.
  ⚠ **The guard that exists watches the wrong axis.** `TierBudgetTests` fails an assertion-tier test
  over **four minutes**; no single test here was near it, and what grew was the **tier**. ***A budget
  per test does not bound a suite***, and the failure mode is a feature landing green with every test
  individually cheap. **Not filed as a defect in the guard**: a whole-suite ceiling is a second number
  with its own ratifier and `CLAUDE.md`'s *past five minutes a test stifles iteration* is a preference
  about a working loop that no measurement settles (`adr/0121`, and `adr/0043` does not reach it). It
  is named here so the next sitting that adds an expensive fixture knows nothing will stop it.

### Task 9 — the long acceptance run — ✅ **DONE 2026-08-21**

**What ships**: `ArrivalLongRunTests` and its `ArrivalLongRun` fixture — a 65,536-Tick run on
`rulesets/crowded.toml` with five assertions over it — plus **two production defects the run found and
nothing else could have** — **F29** and **F30** — each with its regression test, and **F33**, a known
intermittent whose evidence file turned out to hold an unread answer to an open question. **12 new tests** — 1,896 to **1,908**; the assertion tier is **green in
2m45s**, up from 50 s — ⚠ **a capture on a quiet machine**, which the first four readings of it were not, and ⚠ **the acceptance run is the whole of that increase.** 🔴 **No State Hash moved**: both repairs are to derived structures.

**What the run says.** At 1,000 founding Citizens and 384 arrivals a Day through four gates, the Pool
fills from empty over four Days, settles at **~1,470**, and stands there. Placement houses ~270 a Day,
the give-up bound retires ~380 a Day, and the money supply moves in both directions and closes exactly.
⚠ **A standing Pool is the correct answer here and not a failure** — it is the housing shortage this
world was built to have, and `CONTEXT.md` → Departure is explicit that only the *flow* separates a
large healthy Pool from a small desperate one. What is asserted is that the level does not climb.

⚠ **The run is 32 Days rather than the 100,000 Ticks `CLAUDE.md`'s Definition of done names, and the
narrowing is stated rather than quiet.** A gated world pays a paved lattice — **0.51 ms** a Tick — and
this file's arrival rate pays for the churn on top, so the whole thing costs **2.2 ms** a Tick and
100,000 Ticks would be **3m45s**: 4.5× the working tier for one test, and fifteen seconds under
`TierBudgetTests`' four-minute bound, which is close enough that a busy machine turns it red.
**The 100,000-Tick run was made, by hand, on the day this landed**: the Pool read **1,464** on Day 16
and **1,458** on Day 48. ***The obligation was discharged by running it; the committed test is what
keeps it from regressing***, and its tail starts four Days after the Pool settles.

⚠ **Staggered invariants are 4% of the 2.2 ms and the Decide guard is already off**, so the cost is
the mechanism doing its work. It is a Pool of ~1,470 sampled 64 times a Day against three candidates
each, which is `[placement]`'s cadence meeting a Pool this large for the first time.

- 🔴 **F29 — retiring a Citizen had two implementations, and the commute roster reached only one.**
  `World.DestroyCitizen` calls `Commutes.Remove`, then unlinks the member list, then unlists the
  employer, then frees the row. `World.DestroyHousehold` did the last three **by hand** and never the
  first — so every Household destroyed with an employed member left two dangling `CommuteRoster`
  bucket entries, and the next allocation of that Citizen slot was inserted into a list it was already
  in. It presents as a **throw** in `CommuteRoster.Add` during an unrelated employment, ~48 Days into
  a run, naming two slot numbers and no cause.
  ⚠ **`CommuteRoster.Remove`'s own doc comment describes this exact defect** — *"A Citizen removed
  after its row was freed would leave a dangling entry that the next allocation of that slot would
  find itself already in"* — written by whoever built the roster, against the caller that then did not
  call it. ***A warning on the callee does not reach a caller that never arrives.***
  ⚠ **The repair is the consolidation and not the missing call.** `DestroyHousehold` now retires its
  members *through* `DestroyCitizen`, so there is one implementation. The two agreed on the day they
  were written and the roster was added to only one of them — [`adr/0069`](../docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)'s
  own finding, which `World.Employ`'s comment already quotes: *a mechanism living inside another
  mechanism's caller is a mechanism nobody built.*
  ⚠ **Nothing in the invariant tier could have caught it.** `Invariant.NoFreedRowIsStillLinked` walks
  the lists the *tables* declare; the roster is `Derived` and lives in its own structure, so a freed
  row threaded into it is outside what that check can see.

- 🔴 **F30 — `World.ReleaseParking` had one caller, and it was not the one that frees the row.**
  A Citizen destroyed while holding a parking space left `CarParkTable.Occupied` counting a car nobody
  was in. The space was then held for the rest of the run and no Vehicle could take it. Found by this
  task's acceptance run at Tick **65,664**, reading **234** occupied spaces against **233** holders.
  ⚠ **It is F29's shape on a second structure, and one repair closed both**: consolidating
  `DestroyHousehold` through `DestroyCitizen` meant the release only had to be added in one place.
  ***That two independent leaks were fixed by the same edit is the evidence that the duplication was
  the defect and the missing calls were symptoms.***
  🔴 ⚠ **The corpus had this written down, as design, for four milestones.**
  `InvariantTierTests.A_car_park_holding_a_car_nobody_owns_is_caught` produced the leak *by destroying
  a parked Citizen*, and its doc comment explained that this was the realistic route — *"`World.
  DestroyCitizen` unlinks a Citizen from its Household, its employer and its Commute and from no Car
  Park"*. Every word of that was true. ***A test that reaches a violation through a real defect passes
  for the wrong reason and files the defect as intended behaviour*** — and it reads as diligence,
  because naming the realistic route is exactly what a careful check comment does. The test now writes
  the leak directly and its comment records what it used to do.
  ⚠ **Why eleven milestones of long runs never saw it**: until [`adr/0130`](../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)'s
  give-up bound, `DestroyCitizen` had **no production caller at all**. ***A path exercised only by
  fixtures is a path with no long run behind it***, and the fixtures all asserted the one thing the
  path did rather than the four things it owed.

- 🔴 **F31 — a duration bound produces a size bound only after a settling time of its own order, and
  `plans/0002` §D1's stated refutation does not carry that term.** The row for
  `gives_up_after_days = 120` names its refuting observation as *"a Pool that grows monotonically over
  the run means the bound is too long to be a sink."* **Measured on `bordered.toml` at 244 Days —
  500,000 Ticks, the shipped 120-Day value — the Pool grew monotonically for the whole run, reaching
  11,653 and still climbing ~50 a Day, while the bound was working exactly as designed:** mean wait
  **30 Days**, longest **122**, and **4** members over the bound out of 11,646. ***The stated
  refutation fires on a correct build.***
  ⚠ **What the run establishes instead is what the number MEANS, which nothing had written down.** The
  Pool is a queue, so its size is its inflow rate times the mean wait, and the give-up duration caps
  the mean wait and nothing else. So **the Pool's ceiling is `inflow × gives_up_after_days`**, and the
  time to approach it is of the same order as the duration. Both shipped files agree:

  | File | Bound | Outflow a Day | Predicted | Measured | Settled by |
  |---|---|---|---|---|---|
  | `crowded.toml` | **2** Days | ~684 | ~1,370 | **1,458** | Day **4** |
  | `bordered.toml` | **120** Days | ~330 | ~39,600 | **11,653** at Day 244 | not yet |

  ⚠ **So `adr/0006` is satisfied and the reading is subtler than *the collection stopped growing*.** A
  120-Day bound on this world bounds the Pool at roughly forty thousand Households and takes years of
  game time to get there. That is a **coherent design choice and a legible one for the first time** —
  it is not what anybody would have guessed from the number, and it is the sentence a designer needs
  in order to argue with 120.
  ⚠ **Nothing here refutes 120 and nothing ratifies it either.** The row's other stated observation —
  *a Pool that empties while dwellings stand vacant* — did not fire, and the third it names as owed,
  whether four months *feels* right, is a judgement about a played city that `adr/0043` does not reach.
  **What changes is the row's refutation**, which is corrected in place rather than the value moving.
  ⚠ **And the acceptance test could not have found this**, because it runs on the file whose bound is
  2 Days. ***A test sized to observe a mechanism inside a working loop cannot also characterise the
  number the mechanism is tuned by***, which is why this was a hand measurement filed here and not an
  assertion.

- ✅ **F32 — the four Hinterlands are distinguishable, which is the half of the bands' ratifier that
  was easy to leave undone.** `plans/0002` §D1 names two quantities for the emigrant balance bands:
  that `MoneyIsConserved` holds across arrivals and departures, and that the arriving balances are
  **distinguishable between edges**. The first is satisfied by a build in which every edge draws from
  one pooled figure, and it was the only one the run originally checked.
  `ArrivalLongRunTests.The_four_edges_produce_distinguishable_arrivals` closes the second: it walks
  the standing Pool — `UnplacedTable.Gate` is the only place the door a Household came through
  survives, because placement takes the record with it — and asserts the four means come out in the
  **order `crowded.toml` authors**. ⚠ **The order and not the figures**: asserting the numbers would
  be asserting the Ruleset back to itself, while the order fails if the draw ignores the arriving
  Household's own Hinterland. ***An anchor that does not reach the thing it anchors is decoration***
  ([`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)),
  and until this test there was nothing that would have noticed.

- 🔴 **F33 — the acceptance test made a known intermittent fire, and the sample it produced had a
  twin in the evidence file that nobody had ever read.** `LayerQueryTests.Answering_the_query_allocates_nothing`
  went red once in four tier runs and green in the other three. It is **not new and not this
  milestone's**: `plans/0002` §B has been open on it since 2026-08-20 —
  `GC.GetAllocatedBytesForCurrentThread` is served out of a per-thread allocation context that a
  collection on **another** thread flushes — and `AllocationProbe` was built to gather samples of it.
  ⚠ **What this task's run contributed is a reading, and it is in `alloc-probe.csv`**: **3,376 bytes
  with 1 gen0 collection** inside the window. **Under 8,192**, like every one before it.
  🔴 ⚠ **And reading that file to find it turned up a second firing nobody had looked at** — row 300
  of 697, `ZoneRuleTriggerTests`, **5,208 bytes with `0 gen0 / 0 gen1 / 0 gen2`**. ***That is the
  sample §B says it is waiting for.*** The row's own words: *"the half that was owed is still
  untested — a jump requires a collection is a claim about jumps, **nothing jumped**."* Something had
  jumped, months earlier, with no collection anywhere in the window, and the row went on saying the
  question was untested. **Both halves of the pair are now refuted** — a collection is neither
  sufficient nor necessary — while the **bound** survives at six samples, all under one context.
  ⚠ **The instrument was not at fault and neither was anybody's diligence.** Every non-zero row is a
  test that *did* go red: `Record` runs before the assertion and writes a firing through immediately.
  So the sequence was a red suite, a re-run, a green, and a move on — ***which is the correct response
  to an intermittent, and is exactly how the evidence was lost.*** **What was missing was a pointer**:
  nothing in the failure told the reader that the run had just written the sample the open question
  needed.
  ✅ **Closed by `AllocationProbe.Check`**, which the eight sites now go through: it records and
  asserts in one call, and its message names the delta, the collection counts, the 8,192 band that
  separates the intermittent from a regression, and the file — *"go and read it, and put the row in
  `plans/0002` §B, BEFORE re-running."* ⚠ **The eight also wrote the same property in two spellings**,
  which is why §B said *four* for months; one call makes them countable and
  `AllocationAssertionTests` counts them.
  ⚠ **Two traps in building it, and both were the instrument eating itself.** The message test first
  called `Check` with a fabricated delta, appending a **synthetic firing to the evidence file on every
  run**; it now asserts on `Explain`, which does not record. And the happy-path test appended a
  **ninth zero reading**, which breaks §B's *eight sites × N runs* arithmetic; it was deleted rather
  than kept, because ~700 real rows cover it. ***A test for an instrument must not appear in the
  instrument's output.***
  ⚠ **No claim is made that this task raised the firing rate.** Two events across 697 readings and 102
  processes is not a rate anybody can compare against, and the tier did get longer and more
  allocation-heavy. ***An intermittent whose rate you cannot measure is one you may not say you made
  worse or left alone.***

### The doc-comment sweep — ✅ **DONE 2026-08-21**, and it is `plans/0012` **Cause 6**

🔴 **F16 — a description filed under the wrong declaration, forty times, and nothing in the build or
the corpus could see one of them.** Task 1 inserted two members between an existing doc block and its
member; the review that caught it was a human reading the diff, which is a detector that runs once per
reviewer. **Two `///` blocks with no member between them both bind to the member that follows** — so
the member above loses its documentation and the member below starts carrying a description of
something it is not. The compiler is silent, because a duplicate `<summary>` is legal C# and a doc
comment is not parsed unless documentation is being generated.

**The check found forty sites in thirty-one files**: eight where a rewritten block had been left
stacked on the old one, and **thirty-two where a member had genuinely lost its documentation**. Two
were actively misleading — `Ruleset.cs` had the `[jobs]` table's documentation on `TrafficRuleset`, and
`WorldInvariants.cs` had *"Every Rule Instance is in exactly one queue"* on
`NoBuildingRunsRulesItsKindDoesNotDeclare`. All forty repaired: orphans moved to the member each
describes, superseded blocks deleted.

⚠ **The sweep found a second half nobody was looking for.** Six blocks had a `</remarks>` typed where a
`</para>` belonged, closing the comment early and stranding every paragraph after it **outside** the
remarks — a malformation visible only where the docs are rendered, which in this project is nowhere.
***A malformation nobody's tooling surfaces is a malformation nobody fixes.***
`DocCommentAttachmentTests` holds both halves and is **code against code**, on
`RefusalCountTests`' shape.

⚠ **Three deletions dropped a clause and were reviewed by hand rather than taken on trust.** One was
carried back verbatim — `BinTable.Create`'s *linking it in is `World`'s*, which its sibling
`RuleInstanceTable.Create` had kept. Two were confirmed genuinely superseded: an `EventWheel` sentence
offering a flat overflow list that [`adr/0056`](../docs/adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md)
had since **refused**, and a `Readouts` paragraph whose `adr/0006` argument had already moved to
`IndexList.Length`. ***A block left stacked on its replacement is not evidence that its author meant to
keep it***, and the reverse is not evidence either — each one has to be read.

**Filed as [`plans/0012`](0012-corpus-audit.md) Cause 6**, because it is not this milestone's defect:
it is a failure mode the corpus had no detector for, and thirty-one files predate this milestone.

## What this milestone must not do

- **Do not add an immigration rate, an arrival scalar or an attractiveness meter.** `adr/0023`'s first
  line. ⚠ **Two documents already read as having one** — `01 §5.4`'s *"Hinterland attractiveness drives
  arrivals"* and `04 §5`'s *"Attractiveness, and therefore immigration"* — and neither says it is
  shorthand.
- **Do not let a Lot read an aggregate demand figure.** `adr/0023`: *"RCI was a number that caused
  buildings. This is a comparison that causes people."*
- **Do not build the District Pool (12), the price surface (13) or the choice model (16)** — unless
  decision 1 says otherwise, in which case say so in the ADR and move the edge in `06`.
- **Do not author a price anywhere but the Hinterland.** `adr/0050`: the boundary is authored, the
  interior is derived.
- **Do not open an inflow without a sink.** Decision 3.

---

## Definition of done

[`CLAUDE.md`](../CLAUDE.md)'s list, refined per [`0003`](0003-build-plan.md) → *Definition of done*:

- `dotnet build` green with no GPU and no Godot; **the whole unfiltered `dotnet test` green**.
- **A Household exists in the world that the world did not start with**, arriving through the gate as a
  Trip, and a test that fails if the only door closes. 🔴 ⚠ **The criterion is the door and never the
  population** ([`adr/0128`](../docs/adr/0128-the-gate-ships-before-the-comparison-that-walks-through-it.md)):
  until 16 the door's only caller is a Command, so ***a test asserting the city grew would be asserting
  the Command it just issued.***
- **Money's supply is not constant over the run**, and `MoneyIsConserved` still holds as supply + flow.
  ✅ **DONE** — and ⚠ **without a flow term**, which is **F20**: the equality stayed exact.
- ~~**Rejected arrivals are counted with reasons**, and the reasons are distinguishable.~~ **MOVED TO 16
  with the comparison** (`adr/0128`) — nothing declines an offer nobody makes. ⚠ **Relocated, not
  discharged**, and it stays `adr/0023`'s.
- **The Pool does not grow without bound** over the acceptance run — decision 3's sink, asserted.
  ✅ **DONE** — `ArrivalLongRunTests.The_pool_does_not_grow_over_the_run`, and the give-up channel is
  asserted separately because the drift claim alone is satisfied by a city that houses everybody.
  ⚠ **What the bound MEANS is F31** and it is not what the number looks like: the Pool's ceiling is
  `inflow × gives_up_after_days`.
- **A shipped Ruleset contains an Outside Connection**, and the acceptance run uses it. ✅ **DONE** —
  `rulesets/crowded.toml`, and the run asserts all four gates stand and admit.
- Every number that reached a Ruleset has a row in `0002` §D1 with a machine, **a world** and **a
  quantity**, and ⚠ **the world is checked for whether it can occur** — `adr/0052` does not ask, and
  milestones 7 and 9 both paid for that.

---

## What scoping found

### F1 — the assessment: no document names a gate on milestone 11, and the one that appears to is a citation about a milestone that did not exist

`plans/0000`'s per-milestone table gives it `—` in the *Blocked on* column under a header reading
*"There is no red gate left."* `plans/0003`'s gate board holds no Phase 2 rows. `plans/0002` §A is empty
and has been since 2026-08-14. **The only text in the corpus naming milestone 11 as blocked** is
`05 §6`'s threading policy, recorded in three documents as *"gates milestones 10 and 11"* — written when
**there was no milestone 11**, and where old-10 is now **8**, which session K found unblocked. `06`
carries the warning in writing: *"the stale citation has stopped being self-evidently stale. **It is
still wrong**"*, and states what §6 still owns *"gates nothing in 6–24."* ***A citation is falsified by
a renumber into truth-shaped ambiguity, and the renumber is what makes it dangerous rather than what
fixes it.***

### F2 — the milestone's central mechanism is specified in terms of a milestone five positions downstream

Decision 1. Three documents specify arrival as the choice model; the choice model is 16; `06`'s graph
runs 11 → 16. ***A dependency stated inside an ADR is invisible to a dependency graph assembled from
milestone rows.*** `06`'s permutation note says slots 9, 11, 12, 13 and 14 *"admit exactly one
arrangement. Nothing here was a preference"* — that derivation used four edges and this is a fifth.

### F3 — the Pool has no door, and the invariant that closes it is the corpus disagreeing with itself

`World.Unplace` refuses an unhoused Household, so the Pool holds only Households the city has already
housed; `CONTEXT.md` → Unplaced Pool describes four entry routes entering *"on equal terms"*, of which
**immigrants are the first**. Neither is wrong about the build — the invariant describes what exists and
`CONTEXT.md` describes what is meant.

### F4 — the three §D2 numbers name a ratifier that is the wrong half of a split this corpus already recorded as paid

Decision 6. `plans/0012` records session K item 2 as *"PAID: Save/load is **8**, the Outside is **14**"*
— and the §D2 rows were mapped onto **8**. ***A correction that fixes the document it was written against
does not fix the rows that had already copied the number***, and the copies here sit in the ledger whose
job is to hold them.

### F5 — a §D2 row's stated derivation basis was refused by a later ADR

Decision 6. *"Derive it from the unlock rule, not a feel"* against `adr/0090`'s *"The map is open — no
unlock, no serviceability gate, no boundary."* ***A refusal deletes a derivation without touching the
number that was going to use it.***

### F6 — `06` places two mechanisms at this milestone that its own risk row does not carry

Decisions 4 and 5: Settlements, and Shipments *"Placed: 11, behind 12"* where 12 runs after 11. Both come
from the inventory table rather than from the milestone table. ***An inventory is a view over the
milestone table, and the 2026-08-18 permutation reached one of them.***

### F7 — the anchor has no consumer inside this milestone, which is milestone 9's shape one milestone later

The named risk's second half is *no price has an anchor*. The District Pool is 12 and the price surface
is 13. **So the Hinterland's authored prices will be read by nothing in the tree the day they ship**,
which is exactly why `w₂` and `w₃` are unratified today: `adr/0125` — ***a ratifier that needs a consumer
nobody built is not reachable***. This milestone must write its §D1 rows knowing that, not discover it
in task 4.

### F8 — freight's Stress weighting is load-bearing on `adr/0088` and is unset

Session J's own *what this did not close*: *"`plans/0002` design fork 14, and **`0088`'s replacement
friction depends on it entirely — a weighting of zero deletes the argument**."* `adr/0088` is the record
that makes edge choice cost *congestion rather than distance*; at a weighting of zero, choosing a far
edge costs nothing at all and the decision it describes evaporates.

### F9 — the Hinterland's field list is not the same in three documents

`adr/0023` gives population with composition, rent, wage and a price per Good; `CONTEXT.md` adds service
levels and a commute figure; `01 §3` adds depth, recovery rate and **favoured Goods** — which `adr/0088`
and `CONTEXT.md` both put on the **building kind** instead. ***A field list assembled by reading three
documents is a union, and the object has to be one of them.***

### F10 — `05 §7` no longer specifies the Outside, and that is a closure rather than a gap

Session J closed both halves of `05 §7` and the section is now purely the save format. So there is no
technical specification to scope against, and there does not need to be: `adr/0088`'s *no new table, no
new column, no new mechanism* is the whole of it.

---

## Where this sits

**After** milestone 10 — money exists and is conserved, and this milestone gives `MoneySupply.Issued`
its second writer. **Before** 12, 13, 15, 16 and 19, each of which reads something authored here. It is
the anchor under every price, wage and rent in the design, which is why the 2026-08-18 economic reorder
moved it from **14** to **11**.
