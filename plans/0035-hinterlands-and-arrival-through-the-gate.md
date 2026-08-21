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
5. **Money crosses** — the arriving balance drawn from the Hinterland, `MoneySupply.Issued`'s second
   writer, and `Invariant.MoneyIsConserved` rewritten as **supply plus flow**.
6. **The move-in Trip** — gate → dwelling, on placement, carrying real congestion.
7. **The unhoused Departure** — the duration bound, the derived occasion count, the dwellings-considered
   record, and a Departure that leaves through a gate
   ([`adr/0130`](../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)).
8. **Something to look at** — a runner mode showing arrivals, the Pool, departures and the money flow.
9. **The long acceptance run** — ⚠ **on a world where arrivals outpace housing**, because that is the
   only world in which the give-up bound and `adr/0006` can be read at all.

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
kind carrying `arrivals_per_day = 12` and **four `[[hinterland]]` blocks** — and
`SyntheticCity.RaiseGates`, which raises **one Outside Connection on every map edge the lattice
actually reaches**. **7 new tests**; the assertion tier is **1,854 green**. **No State Hash moved**:
the new pass returns 0 on a Ruleset that declares no gate kind, so the other nine files walk exactly
the Lots they always did.

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
  `GatePlacementTests.Only_the_edges_the_lattice_reaches_get_a_gate` asserts the **set** rather than a
  count, so it goes red in either direction and reopens the two rows rather than letting them settle.

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
- ~~**Rejected arrivals are counted with reasons**, and the reasons are distinguishable.~~ **MOVED TO 16
  with the comparison** (`adr/0128`) — nothing declines an offer nobody makes. ⚠ **Relocated, not
  discharged**, and it stays `adr/0023`'s.
- **The Pool does not grow without bound** over the acceptance run — decision 3's sink, asserted.
- **A shipped Ruleset contains an Outside Connection**, and the acceptance run uses it.
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
