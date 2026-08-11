# 0014 — Slice 10: Zone Rules, and the second Rule family

> Slice 10 of [`0003-build-plan.md`](0003-build-plan.md). Roadmap **milestone 3c**, the Sweep half —
> slice 6 was the Layers half.
> Governed by [`02 §4.2`](../docs/02-simulation-model.md), [`02 §5`](../docs/02-simulation-model.md),
> [`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md),
> [`adr/0025`](../docs/adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md),
> [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md),
> [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md).

**A Zone Rule fires on a time trigger, samples a small random set of Lots, tests real simulation
state, and creates or demolishes a Building on those that qualify.** This slice builds the second of
`02 §4`'s two execution models — the one the corpus spent years treating as an anomaly bolted onto
the side of the Rule engine — and with it the first thing in this project that makes **rows churn**.

**The risk it retires** is the one `06` names for milestone 3c: *that growth cost scales with Zone
size rather than staying constant*. `02 §5.7` asserts constant cost and credits GlassBox for it;
nothing has ever measured it here, and it is measurable, so under [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
it must not be closed by argument. Task 9 is that measurement.

**The second risk, unnamed by `06` and arguably larger:** that `adr/0033`'s central claim — the two
families *differ in observable behaviour, not merely in cost* — has never been testable, because only
one family exists. It is load-bearing enough that `CLAUDE.md` forbids moving a mechanism between
families for performance. Building the second family is what puts it at risk of being wrong.

---

## Status

**All eleven tasks shipped. The slice is closed twice** — once at task 10, and again at task 11, which
reopened it after S0b found a defect the closed slice had shipped. The gate was clear and slice 7 — its
only dependency — closed with task 10a.

- **Task 1** — the Lot's permission set at full width. Its second half, *a `zone` verb that paints an
  area*, was **cut**: see *The second collision*.
- **Grilled before task 2**, because decision 1 shapes a table rather than a predicate. Produced
  [`adr/0053`](../docs/adr/0053-failure-pressure-is-a-duration-not-a-tally.md),
  [`adr/0054`](../docs/adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md)
  and [`adr/0055`](../docs/adr/0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md),
  settled three of the four owed decisions, and **deleted one of the four unratified numbers by
  deriving it away**. Tasks 4, 6, 7 and 8 are rewritten; task 7's planned mechanism was found to
  invert severity and would have shipped.
- **Task 2** — the derived Lot→Building reverse index, and `02 §2.2`'s invariant checkable for the
  first time in both tiers. It caught six existing fixtures on the day it landed.
- **Task 3** — the `[[zone_rule]]` table and its three refusals. The loader's count of record goes
  from five to eight, corrected in `adr/0048` and `adr/0015`.
- **Task 4** — `PurposeTag.ZoneRuleSample` and `ZoneSample.Draw`. Sampling without replacement turned
  out to need **no retry budget**, which would have been a fourth hash-bearing number.
- **Task 5** — `ZoneRuleEngine` and Tick phase 6. `adr/0033`'s observable difference is checkable for
  the first time. Found `02 §4.2`'s *rotate the scan start* **inapplicable rather than merely
  unnecessary**, and its stagger too: both are mitigations for a cost that grows with the population,
  which is a sweep's shape and not a sample's. `adr/0055` had inherited the error and is struck.
- **Task 6** — the create predicate, and the Unplaced Pool it reads, which task 8 was to have built.
  The Pool is a **table** because a member is drawn by position and a derived list would come back in
  a different order after a reload. **Found: the growth cycle cannot be entered from a standing
  start** — a populated city has no vacant Lot and an empty Pool, so nothing this project can build
  today exercises creation. Task 7 is what supplies both.
- **Task 7** — demolish, and the cycle closes. The mechanism needed **two amendments to `adr/0053`**,
  both found by the code rather than by argument: the signal is a Rule asleep short of an **input**
  and never a reporting terminal, and the clock lives on the **Rule Instance**. It also found a defect
  that had been latent since slice 4 and a live one it introduced itself. `rulesets/minimal.toml` now
  declines and rebuilds, and all three baselines moved.
- **Task 9** — the tripwire, and it fired at **1.56× against a bound of 2×**. `02 §5.7`'s *constant
  cost regardless of Zone size* is **false in the letter and true in the substance**: the sweep is
  `O(sample)` exactly, and what grows over three orders of magnitude of Zone is the **memory
  hierarchy**, not the algorithm — against a control rung that moved **989×** on the same data. It is
  the **third sighting of scatter ×1.5**, after task 10a's findings 42–43, and the first with nothing
  else moving beside it.
- **Task 8** — eviction, and closing it was an audit rather than a change. The qualification had
  already shipped in task 6, as a **new** invariant rather than an amended one — which left
  `HouseholdHomeExists` **reported by nothing**, the only orphan among 26 members. Bannered and
  `[Obsolete]`, and the id retired rather than reused, because a crash artifact carries the number.
- **Task 10 — the slice is closed.** The `slots` half of slice 5 task 7's trend assertion is
  discharged: five of six tables are **dead flat** across 100,000 Ticks of continuous demolition and
  rebuilding, which is `adr/0006`'s collection half checked for the first time. The sixth, the
  Unplaced Pool, is a **running maximum** and gets a structural ceiling plus a convergence *rate*
  rather than a fitted window. **The finding is what the numbers sit on**: the city settles at ~60 of
  121 Lots built and ~300 of 360 Households homeless, because demolition evicts a Building's whole
  occupancy and creation rehouses exactly one — **a Building has no occupancy at all**, filed to
  `0002` §B. **⚠ The headline is amended below**: homelessness here is the fixture's own arithmetic, and
  the finding is the **vacancy** beside it — half the places that existed stood empty.
- **Task 11 — `revisit_ticks`, and the slice is closed a second time.**
  [`adr/0059`](../docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)
  shipped: a `[[zone_rule]]` authors a **duration** and `ZoneRuleDefinition.SampleFor` derives
  `ceil(Lots × interval ÷ revisit_ticks)` per trigger. At 1,000,000 Citizens the shipped Ruleset now
  raises **2,898 Buildings** where it raised **none**, and the Tick got **cheaper** while doing 117×
  the Lot evaluations. Three findings outlive it, below.

---

## Gate

**Cleared**, and the distinction matters: [`0003`](0003-build-plan.md)'s gate board records slice 10
as *"waits on slice 7, which is a dependency and not a gate"*. There is no ungrilled design standing
in front of this slice. `adr/0033` settled the family in full, and `02 §4.2` is the section it wrote.

What is *not* cleared, and is not a gate either, is `02 §5` — the growth model. See the next section,
because it is the whole shape of this slice.

---

## Prerequisites

Slices 4, 5, 6 and 7, all closed. Specifically: typed tables with the per-field declaration and row
freeing (`Rows.AllocateSlot` / `Rows.FreeSlot`), the eight Tick phases with **Growth (phase 6) still
empty and labelled for this slice**, the Ruleset loader and its five refusals, `World.CreateBuilding`
and `World.DestroyBuilding`, and the Census.

Three pieces are already in the tree with this slice's name on them, written by earlier slices as
debts rather than as guesses:

| Where | What it says |
|---|---|
| `Simulation.cs:288` | Tick phase 6 — *"Zone Rules sample Lots; Buildings with accumulated failure decline. Serial. Slice 10, which is gated behind the Rule engine: a Zone Rule is a Sweep Rule."* |
| `Simulation.cs:156` | *"Slice 10 gives Lots a permission column and Zone Rules to sample them. Until then a Zone paints one Lot, and the permission set narrows to the Lot's zone byte."* |
| `Command.cs:101` | The `zone` verb carries a **`ushort` permission set**; `LotTable.Zone` is a **`byte`**. The command is being narrowed on the way in |

---

## The collision this plan found, and it inverts the slice's name

**`02 §5` describes growth, and almost none of what it describes exists.** Its causal chain is:

> Households can't find housing → they stay in the pool → demand/supply in that submarket exceeds 1 →
> prices rise → the pro-forma flips positive → a developer builds → supply appears → prices relax.

Of the seven links, **the Unplaced Pool does not exist** (`06` mechanism 2: *"9a has the Pool and
Departure but nothing says where Households come from"*), **there is no price surface**, **no land
value**, **no construction cost**, **no private capital** (`0002` §C lists *where private capital
comes from* as open), and **no bid-price contest** to resolve two uses wanting one Lot (`02 §5.5`).
`02 §5.6`'s construction trigger — *local price × buildable capacity versus cost* — has **no term in
it that this build can evaluate.**

**Decline is a different story, and this is the finding.** `02 §5.9` sources failure pressure from
three things:

| Source | Available today? |
|---|---|
| Trips to or from it failing | **No.** No Trips, no Legs, no roads |
| Rules repeatedly hitting their terminal fallback | **Yes.** `on_fail` chains and reporting terminals shipped in slice 7 task 8 |
| Local conditions below occupants' tolerance | **Partly.** Map Layers are real; *tolerance* is not authored anywhere |

**So one of the three is fully available, and it is the one that is native to the Rule engine this
slice extends.** A Building whose Rules keep reaching a reporting terminal is a Building that is
starving, and the terminal already *records the condition* — that is what `adr/0045` made it for.
Decline reads a quantity the previous slice built. Growth reads six quantities nobody has built.

**The slice is named for growth and only decline is honestly expressible.** That is the same shape
slice 7 task 10 hit when it asked for a production chain and found `pool` was a named hole
([`0011`](0011-rule-engine-bins-and-rules.md) finding 34) — and it was found the same way, by
planning rather than by building, which is the second time that has paid.

**The response is not to skip creation**, because a slice that only frees rows cannot discharge the
obligation it inherited: `slots` flat against a falling `live` is not the churn the trend assertion
tests for (see task 10). The response is the one task 10a already modelled — **build the structure,
and make the content's absence loud**:

- The Zone Rule's **structure** — trigger, sample, predicate, act — is this slice's, in full.
- The Zone Rule's **create predicate** is deliberately trivial: *the Lot is vacant and its permission
  set admits the kind*. It is not `02 §5.6`'s pro-forma and must never be mistaken for a draft of one.
- Every term of the real predicate is named in the Ruleset comment as belonging to Phase 2, exactly as
  `rulesets/minimal.toml` names why it declares no Good.

**What this buys is not a lie about the economy — it is churn.** Rows created and freed in a running
city, on a cadence, is the only thing that tests row recycling, handle generations, stale-handle
detection, and `DestroyBuilding`'s teardown under repetition. None of that has ever run. That is worth
a slice on its own, and it is why this one is worth building before the economy exists rather than
after.

---

## The second collision, found on the first sitting: Lots are generated, and the generator is 5a's

**`02 §2.2` is unambiguous — *"Lots are **generated, not painted**"*.** The subdivider carves zoned
land against the Street network into parcels with frontage, and `CONTEXT` → Frontage calls that
contact *"the geometric precondition for a Lot existing at all"*, with land the network cannot reach
staying unlotted. **`06` puts the Road Graph and Streets at milestone 5a, which is Phase 2.** This
slice is 3c, which is Phase 1.

**So the roadmap placed the subdivider after the slice that was supposed to make the `zone` verb
honest**, and no spelling of task 1's second half is available here. A Zone command that emits Lots is
standing in for 5a whatever shape it takes; painting a rectangle stands in for *more* of it than
painting one Lot does, and every Lot it invents is one the real subdivider would have refused for
want of frontage.

**What dissolves it is that this slice never needed the verb.** `SyntheticCity` already creates Lots
— 225 per 1,000 Citizens — so a world entered through `populate` has a Lot population in quantity.
The tripwire's Zone-size sweep, the churn run and the trend assertion all sample *that*, and a Zone in
this build is anyway not an entity but **the set of Lots whose permission set carries a bit**, which
is what task 3's `[[zone_rule]]` names. Nothing downstream asks the verb for an area.

The verb keeps its single Lot, the permission set widens, and **the subdivider stays visibly unbuilt**
— filed below rather than half-built, because a half-built subdivider is the one shape that would stop
anyone noticing it is missing.

---

## Tasks

### 1. The Lot's permission set

`LotTable`'s `Zone` byte becomes a **permission set** matching `Command.Zone`'s `ushort`, discharging
the narrowing `Command.cs:101` records.

**The other half of this task — *and a `zone` verb that paints an area* — is cut**, and the reason is
a roadmap ordering rather than a difficulty. See *The second collision* above. `CommandKind.Zone`
keeps creating exactly one Lot, which is no more fictional than it is today and materially less
fictional than a rectangle of them would be.

**A permission set is a set, and the distinction is `adr/0025`'s.** Density is a *cap*, so a Lot's
permission is *which forms may be built here*, not *which form is here*. One bit per admitted kind is
the cheap spelling and the honest one; a single enum would re-introduce the "zone type" the design has
been avoiding since `02 §2.2`.

Saved and hashed — it is player intent, and `05 §4` says a different permission set is a different
city.

### 2. Which Building is on this Lot — the derived reverse index — **done**

**The relation was one-directional.** `BuildingTable` declared `Lot`; `LotTable` declared `East`,
`North`, `Zone` and no Building handle. So *"is this Lot vacant"*, which is the first question a Zone
Rule asks and the one it asks most, had no answer without scanning Buildings — `O(Buildings)` per
sample, which would have destroyed the constant-cost property task 9 measures before task 9 could
measure it.

`LotTable.BuildingSlot`, declared **`Derived`** so it stays out of the State Hash and out of the save,
rebuilt in `World.RebuildDerived` alongside the four indices already there. Confirmed by the golden
baselines not moving.

**Three things the implementation turned up:**

- **It is a slot plus one, not a handle, and not a raw slot.** A `DerivedHandle` would be a
  construction cycle — `BuildingTable` already takes `LotTable` to address its own `Lot` column. And
  the `+1` encoding is mandatory rather than cosmetic: `IndexList` documents at length that slots are
  zero-filled on growth and on free, so a `-1` sentinel would make every freshly allocated Lot read as
  holding *Building slot 0*. Vacancy decodes to `Rows.NoSlot` for free — stored zero, minus one.
- **`BuildingTable.Create` was the only table door in the project that wrote cross-table state.**
  `HouseholdTable` and `CitizenTable` expose no `Create` at all; `World` is their only door. Building's
  did, and it wrote the forward handle while leaving the reverse end to whoever called it — which is
  the arrangement that made `02 §2.2` unenforceable for four slices. It now takes the `LotTable` and
  writes both ends. It could not simply *hold* the table: **`BOR0901`** forbids a `[Table]` type from
  holding anything but declared columns and its own `Rows`, so the analyser chose the shape.
- **The invariant caught six existing tests immediately**, all of them fixtures that had built a
  half-written relation. That is the check earning its place on the day it was added rather than in a
  hypothetical future.

**`02 §2.2`'s *a Lot is either vacant or holds exactly one Building* is now checked in both tiers.**
`LotIsNotAlreadyBuiltOn` at the write site, one comparison, where the second Building would go up.
`LotHoldsExactlyOneBuilding` whole-world, walking **both directions** — because each sees a failure the
other structurally cannot. Walking Buildings catches an index left stale by a demolition that freed the
row without vacating the Lot; walking Lots catches an index pointing at a slot since freed *or
recycled into an unrelated Building*, which reads as perfectly valid from the Building side.

### 3. The Zone Rule in the Ruleset, and its refusals — **done**

A `[[zone_rule]]` table, read by `Borough.Formats.RulesetLoader` on the same walk as the other five,
reaching `Borough.Core.Rules.ZoneRuleDefinition` as ids and integers and never a string (`adr/0048`).
It declares a **kind**, a **zone** bit, an **interval** and a **sample**.

**The loader now runs eight refusals, and the count is corrected in `adr/0048` and `adr/0015`** —
`0012` records that this exact number has drifted between three documents before.

Three new, and they are **one class rather than three**: each describes a Zone Rule that loads clean,
triggers on schedule for ever, and builds nothing.

- a Zone Rule naming a **kind the Ruleset does not declare**
- a Zone Rule naming a **permission bit no `zone` verb can paint** — checked against
  `LotTable.ZoneBits` rather than a literal, so widening the column cannot leave the parser refusing
  bits that have become paintable
- a **sample size of zero**, which is the `apply = {min=1,max=4}` behaving as `{1,1}` defect
  (finding 19) arriving in the second family

A fourth bounds check rides along, on **interval**, for the Event Wheel reason `rate` already has. It
is spelled `interval` rather than `rate` deliberately: sharing the word would invite the reading that
a Zone Rule is armed *per Lot*, which is the Bin Rule shape it is not.

**Two things the implementation turned up:**

- **A Zone Rule needs no id, and giving it one would have invented a reference nothing holds.** A Bin
  Rule is named by an `on_fail`; a kind is named by a Rule. Nothing ever names a Zone Rule — it is
  only iterated — so it is a span in declaration order, which is also `02 §4.2`'s tie-break between
  two Rules contending for one Lot.
- **`BOR0204` caught `1 << Zone` on the first build.** The permission bit is a variable shift count,
  and C# masks it against the operand width, so an out-of-range bit would have wrapped to a valid one
  rather than throwing. The loader refuses such a bit; `IntegerMath.ShiftLeft` is the second side of
  that check, and the analyser is what insisted on it.

**No `[[zone_rule]]` is added to `rulesets/minimal.toml` yet**, so the golden baselines do not move.
The trigger that would run one is task 5.

**The condemnation threshold is not here.** It belongs on `[[building]]`, not on the Zone Rule: under
`adr/0055` any Zone Rule may sample any Lot, so a threshold declared per Zone Rule would make a
Building's mortality depend on which Rule happened to look at it — and `adr/0053` makes pressure a
property of the Building. Task 7.

### 4. The sample — and it is a new `purpose_tag` — **done**

`PurposeTag.ZoneRuleSample`, the third member and the **first belonging to the Sweep family**. Distinct
from `RuleSettleOrder` for a sharper reason than usual: both are *which of these do we act on*, so
sharing a tag looks harmless — and would mean the Lots a Zone Rule sampled correlated with which Bin
Rule won a contested draw on the same Tick. That reads as a lucky District, never as a defect.

**The population is every Lot** (`adr/0055`). The permission bit is a term in the create predicate,
never a filter on what is drawn from.

**The finding: sampling without replacement needed no retry budget, and a retry budget would have been
a fourth hash-bearing number.** The obvious implementation draws until it has `k` usable Lots, which
needs a bounded attempt count — and how many attempts you allow changes *which* Lots get built on, so
it is hash-bearing, so `adr/0052` would demand a named ratifier for a quantity nobody has ever wanted
to think about.

**`ZoneSample.Draw` makes exactly `k` draws and discards the ones that land badly**, which costs
nothing and *is already the model*: a draw landing on a freed slot, or on a Lot this trigger has
already seen, is a parcel the developer looked at and could not use. So the sample size is how many
Lots are **evaluated** — which is the quantity `02 §5.7` names and the one task 9 holds fixed — and how
many turn out usable is a property of the city rather than a number in a file. Cost is `O(sample)`
exactly, with no dependence on the Lot count in either direction.

Duplicates are discarded rather than counted twice, which is `02 §5.3`'s one criticism of UrbanSim.
The scan that finds one is linear over what has been drawn so far: a set would allocate, hash, and be
walked in an order `05 §4` lint 3 bans, for a handful of Lots.

**`02 §4.2`'s *rotate the scan start per trigger* turns out to be unnecessary here**, and not by
oversight. The mitigation exists because a fixed scan order privileges the same low-index Lots for the
life of the city. A sample keyed on the Tick has no fixed order to privilege anything — which the
coverage test asserts directly.

Nine tests, including allocation-free on the hot path and *many triggers reach every Lot* — a coverage
check rather than a distribution test, aimed at the family of defects that make a sampler concentrate
(a modulus against the wrong bound, a coordinate that does not vary, a dropped mixer). Each of those
would otherwise look like a city that simply grew slowly.

### 5. The trigger, in Tick phase 6 — and what it does *not* share with a Bin Rule — **done**

`Simulation.Growth` stops being empty. `ZoneRuleEngine` triggers on `tick % interval`, samples, and
forks each sampled Lot to the mechanism its occupancy selects. **A second engine class rather than a
mode of `RuleEngine`**, which is `adr/0033` honoured in the layout: a single class branching on family
would make moving a mechanism between them look like a flag.

**`adr/0033`'s observable difference is now checkable, and one test is the whole of it.** A world
running one Zone Rule and nothing else does real work in phase 6 while the Bin Rule engine's due and
evaluation counters stay at zero for the entire run and `RuleInstances` holds no rows — no wheel
entry, no subscription, no proposal that phase 3 could refuse. That boundary is what a migration
between families would cross.

**`02 §4.2`'s *rotate the scan start* is not the mitigation here, and the section is corrected.** Task
4 had already found rotation unnecessary for the sample; building the trigger found the stronger
statement — **it is inapplicable**, because a sampler has no scan to start, *and* the bias it exists to
fix is absent. A Policy rotates because a treasury **is exhausted**, so a fixed order permanently
excludes the tail of a population; nothing a Zone Rule contends for is exhausted, and two Rules
overlap at all only about `sample² ÷ Lots` of the time. Contention is settled by **declaration
order**, which is not new hash-bearing surface — the Rule's index is already a coordinate of its draw.
`adr/0055` had carried the same over-generalisation into a consequence line and is struck through.

**No stagger either, and that is the same correction one level up.** §4.2's three cheapness mechanisms
— low frequency, stagger, Chunk partition — are all mitigations for a per-trigger cost that *grows
with the population*, which is a sweep's shape and not a sample's. Staggering a Zone Rule would be
armour against the cost task 9's tripwire exists to prove absent, bought with a second hash-bearing
coordinate in the trigger.

**Phase 6 after phase 5 is a decision, now stated where the code is.** A Zone Rule's predicates read
Map Layer values, so growth this Tick sees the diffusion this Tick. Reversing them would make growth
lag the environment by a Tick on the 1-in-64 Ticks a Layer moves — a difference no readout could
explain.

`ZoneActivity` carries three flows: **triggers**, **vacant** and **occupied**. The last two are two
different mechanisms rather than a breakdown — a vacant Lot is a candidate for creation, an occupied
one is a Building whose failure pressure is *read* (`02 §5.9`: *"sampling reads that duration; it never
produces it"*) — and their **sum** is task 9's quantity. There is deliberately no peak equivalent of
that sum: the busiest Tick of one flow and of the other need not be the same Tick, so adding the peaks
would report a burst that never happened. `RuleFlow.Fold` moved onto the type so both engines share one
definition of the peak, which is the half that drifts silently.

**No `[[zone_rule]]` in `rulesets/minimal.toml` yet.** A trigger that fires and changes nothing is
content that does nothing; it earns its place in task 6, and the baselines move then. Twelve tests.

### 6. Create — a Building on a vacant, permitted Lot somebody wants — **done**

`World.CreateBuilding` already gives a Building its kind's Bins and Rule Instances with an arming
stagger, so this task is the *decision* and not the construction.

The predicate is **vacant AND permitted AND a Household in the Pool would take it**. The third term
arrived from the grill and is not a softening of the second: `CONTEXT` → Frontage lists the four
`Evidence` answers for why a Lot is vacant, and *"no Household in the Unplaced Pool that would accept
it"* is one of them, **beside** *no capital* rather than downstream of it. So consulting the Pool is a
documented vacancy reason and not a draft of `02 §5.6`'s pro-forma, which needs prices, capital and a
bid this build has none of. See [`adr/0054`](../docs/adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md).

**The permission bit is a term here and nowhere else.** It does not filter the sample population —
[`adr/0055`](../docs/adr/0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md),
because a Rule that only looked at Lots carrying its own bit would make repainting a preservation
order.

The Ruleset comment says in its own words what is still missing: no price surface, no capital, no bid.
`rulesets/minimal.toml`'s header is the model for the register — it says *"it models no city, and it
is not the beginning of one"*, and that sentence is why nobody has mistaken it for content.

**Construction time is deliberately not built** (`02 §5.7`'s second pacing mechanism). A Building
under construction occupies its Lot and produces nothing, which needs a state a Building does not
have; it is Phase 2's, with the derelict flag slice 8 owes.

**Task 8 came forward into this task, because the third term needed something to read.** The Unplaced
Pool did not exist, so it is built here: `UnplacedTable`, `World.Unplace` and `World.Place`, plus the
derived reverse index on the Household. Task 8 keeps the eviction *verb* and its Ruleset-facing half;
what moved is the structure.

**The Pool is a table and not a list threaded through the Households, and save/reload is the reason.**
The cheaper shape is an intrusive list rebuilt from *which Households have no dwelling* — no new table
and no new saved state. But a member is chosen by **position**, and a rebuilt list is in slot order
while a live one is in arrival order, so the same save would rehouse a different Household after a
reload. That is a divergence only the save/reload test could see, and not having it is cheaper than
detecting it.

**Live Pool rows are dense, which is bought and not given.** `Leave` moves the last member into the
vacated position and frees the *last* slot; the free list is LIFO, so the slot freed is the one the
next `Join` takes back and the live range never holes. Freeing the middle slot instead would let a
draw over the count name a dead row — so a Lot that passed every term would silently not be built on,
at a rate set by how much the Pool had churned. **That failure reads as a city that grows slowly**,
which is the family `ZoneSampleTests` was written against on the sampling side. An invariant asserts
density rather than this paragraph being trusted.

**Who moves in is drawn, not taken from the front.** `02 §8` rule 5's wording covers Phase 3's
contested intents and this is neither, since the drain is blind — but its *reason* reaches exactly: a
Pool that never fully drains is what a housing shortage **is**, and under any fixed order the same
Households would stay unhoused for the life of the city with nothing to explain why.
`PurposeTag.PoolDraw` is the fourth tag, keyed on the **Lot's monotonic id** rather than its slot,
per the same rule's footnote about recycled indices.

**Two invariants were qualified rather than one, and the suite found the second.** `adr/0054`
predicted `HouseholdHomeExists`; it did not predict `EveryoneIsInExactlyOnePlace`, whose occupant
count reports any Household in no Building's list — which is every member of the Pool. Both are now
two-sided: a Household housed *and* in the Pool is as much a violation as one that is neither, and
the second is the corruption that would let somebody be housed twice.

**The finding that outranks the code: neither task 6 nor task 7 can run in a real world alone.**
`SyntheticCity` creates exactly one Lot per Building and houses every Household, so a populated city
has **no vacant Lot and an empty Pool** — two of the three terms are false everywhere, permanently.
What makes vacant land is the Lot subdivider, which is milestone 5a's and has no Phase 1 milestone
(`plans/0012`); what makes a Household seek a home is demolition, which is the next task. **The growth
cycle closes on itself and cannot be entered from a standing start.** That is now a test on
`Populate` rather than a note, so if it ever stops being true the golden trace needs rereading.

Consequently **no `[[zone_rule]]` goes into `rulesets/minimal.toml` here either** — one would fire on
schedule and provably build nothing. The baselines moved anyway, and only because the world gained a
ninth table; the fold is unchanged and `World.HashSeed` is untouched.

**Two things a review of the shipped code changed, both about failing loudly rather than late.**
Density is inherited from `Rows`'s free list being **LIFO**, which nothing in `Rows` promises — so the
failure mode is somebody else's edit to a different type. `Join` now returns the position it landed on
and `World.Unplace` checks it at the write site, so an allocator change fails on the first eviction
instead of leaving the city under-building for a whole run and reporting it once at the end. And
`World.Place` is keyed on the **Household handle** rather than the position drawn: `Leave` swaps the
last member into the vacated slot, so a caller holding two positions and using both would house
somebody who was never drawn — and because the whole mechanism *is* a draw, that reads as a
legitimate outcome. The reverse index makes the lookup free, so the safer shape costs nothing.

**A third finding went to `0002` rather than into the code: a Building has no capacity.** Not in
`BuildingTable`, not in `KindDefinition`, not in the Ruleset — and the two populators already disagree,
`SyntheticCity` housing 3 Households per Building against growth's 1. Nothing breaks today because an
occupant list is unbounded and nothing reads its length, which is exactly why it is worth recording:
the first mechanism that reads occupancy inherits a world whose halves were built to different numbers.

Twenty-two tests: eleven on the Pool, eleven on the predicate with each of the three terms removed on
its own.

### 7. Demolish — and failure pressure is a duration, not a tally — **done**

**The grill rewrote this task.** [`adr/0053`](../docs/adr/0053-failure-pressure-is-a-duration-not-a-tally.md)
owns the reasoning; what changes here is that the planned mechanism — *pressure is a count of terminal
firings* — was found to **invert severity** and would have shipped.

Under [`adr/0045`](../docs/adr/0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md) a failed Rule
does not retry; it subscribes and sleeps. So a comprehensively starved Building emits **one** terminal
report and then nothing, while an intermittently supplied one wakes and fails repeatedly. A tally
condemns the second and spares the first.

So: a Building carries **the Tick it began failing**. Pressure is `now − since`, derived on read,
reset whenever a Rule fires successfully. Three consequences:

- **The decay rate is deleted before it was ever chosen.** Nothing accumulates, so nothing can run
  away, and `adr/0006`'s magnitude half is satisfied structurally rather than by a number somebody
  tuned. Four unratified numbers became three.
- **The threshold is authored in *missed firings*, not Ticks.** A Rule fires every `rate` Ticks when
  healthy, so silence of `N × rate` is `N` missed firings. In Ticks, a Ruleset that halved every rate
  would silently double every Building's lifespan.
- **One threshold, not two.** `02 §5.9` has *loses occupancy and quality*, then *abandoned*. Only the
  second ships: the first is half-expressible because **quality does not exist anywhere in the tree**,
  and it is purely additive later.

`RuleInstanceTable.Reported` needs no change — it is already a `ConditionId` **level**, which is the
right shape. The condition current at the crossing is retained so the demolition has a sentence behind
it, per `02 §5.9`'s refusal of the sad-face icon. Ours is a duration and a condition where §5.9's
worked example is a proportion over a window; that is a weaker instrument of the same shape, and the
window version is not attempted rather than approximated.

**Buildings do not shrink** (`adr/0025`). No downgrade in this slice.

#### What building it changed — **done**

**`adr/0053` needed two amendments and both were found by writing the code, not by arguing it.** The
ADR is the grill's largest finding and it was still wrong twice, in the two places an argument could
not reach.

- **The signal is `Blocking.Level`, never a reporting terminal.** `Reported` is set only where an
  author has written an `on_fail` chain, and `minimal.toml` has none — so under the only Ruleset that
  exists no Building could ever have been condemned, and the task would have shipped a mechanism that
  provably never fires. **The obvious repair is worse**: the one Rule that fails in that file is the
  *producer*, failing on **headroom**, which is the healthy surplus steady state slice 7 built on
  purpose. A terminal there condemns the whole city for being well supplied. `02 §5.9` already said
  *input starvation* and `RuleEngine.Check` already separated the two by name; nothing had joined them.
- **The clock is on the Rule Instance.** The threshold is in missed firings and a `rate` belongs to a
  Rule, so a kind running Rules at 8 and 32 Ticks means two different things by *three missed
  firings* — a Building carrying one clock has to pick one arbitrarily. Two Rules that went short at
  different moments are two durations, and the Building's pressure is the longest, computed at the
  sample and stored nowhere.

**The condition behind a demolition is available and deliberately not kept.** Nothing consumes it —
`01 §5`'s notification surface does not exist and the row is freed on the next line — so storing it
would be a column nothing reads. That is `0011` finding 39's shape and it is recorded rather than
built.

**Two defects, and the interesting one had been latent since slice 4.** `Rows.Allocate` hands back a
recycled slot **without clearing any column**, and has never promised to; until this task nothing in a
running simulation ever freed a row, so it cost nothing. Demolition makes it reachable in two places
at once: a rebuilt Building would open with the condemned one's **Bin contents** — goods from nothing,
reading as a generous city — and its Rule Instances would inherit the condemned one's **starvation**,
condemning it on the Tick it was raised at an age it had not lived. Both doors now write the columns
they do not read. It is task 6's Pool-density finding one level down: *a borrowed guarantee nobody
wrote down*.

**The live one is the better story.** `LotTable.BuildingSlot` is **plus-one encoded**, so that a
zero-filled row reads as vacant rather than as holding the first Building in the city, and its own
doc-comment says *"use `BuildingOn` rather than reading this directly; the encoding is not meant to
travel."* The condemn branch read it raw and **demolished the Building on the next slot**. Nothing
about the symptom said so: Buildings declined, Lots cleared, Households pooled, the census was
plausible and the long-run assertions passed. The only wrong thing was **which house fell down** —
which is invisible in every aggregate and would have been found, if at all, by a player.

**A handle column can now declare that its target may be freed underneath it** — `Reference.Required`
against `Reference.Severable`, beside `Disposition` and `Touch`. Demolition is the first thing in this
project that can free a row another table holds a handle to, and **exactly one column needs it**:
`citizen.workplace`, which is somebody else's Building. Clearing it would need a Building-to-workers
reverse index that does not exist and belongs to the labour system rather than to Zone Rules, and a
workplace that no longer resolves **is** the job no longer existing. The declaration goes at the field
because `02 §10`'s walk is driven by the columns for a stated reason — a list of fields shares its
blind spot with the bug it exists to find — and the whole-world tier caught this on the first run.

**`rulesets/minimal.toml` declines now, and the content is honest about being total.** A `repairs`
Good that nothing produces, a Bin, an `upkeep` Rule that goes short on its first firing and sleeps for
ever, `condemn_after = 4`, and **the project's first `[[zone_rule]]`**. Every dwelling therefore falls
down, which is total rather than selective because there is one kind and it has one behaviour — a city
where some Buildings thrive and others do not needs Buildings that differ, which needs content, which
the file's first line says it is not. All three baselines moved.

**Slice 7's long-run assertion lost its premise, and that is the correct outcome rather than a
regression.** `RuleLongRunTests` asserted *exact equality* across the tail, earned by a Ruleset whose
period was known, and stated its premise plainly: *no Building created or demolished*. A
`[[zone_rule]]` falsifies it. The premise did not turn out to be wrong — it **expired**, and the
replacement is bounded rather than exact because the churn is aperiodic: the sampler is the pacemaker
and is in phase with nothing. What is still exact is better than what was lost: **`slots` does not
move while `live` does**, which is `adr/0006`'s collection half reaching the one place nothing has
ever been able to check it. That is task 10's assertion arriving early, because a long-run test left
asserting `LiveCount == SlotCount` after demolition exists is red rather than merely weak. Task 10
still owns the sharper form — a reading interval that is a whole number of the Zone Rule's trigger
period, so the assertion is not about the sampling phase.

Eleven tests, of which the load-bearing one is *running out of headroom does not start the clock*:
every other test in the file passes with the wrong signal, and the city that results merely falls down
everywhere at once.

### 8. Eviction — where the Households go, and it is settled — **done, mostly inside tasks 6 and 7**

**No longer a hole.** [`adr/0054`](../docs/adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md)
settles it: the Occupants move to a minimal **Unplaced Pool** with `Money` and `Savings` intact, and
task 6's create predicate drains it.

Three findings from the grill made the choice cheap:

- **Eviction is free.** Moving a Household touches `Dwelling` and the occupant list; it does not touch
  `Money` or `Savings`, so *"Households keep what they own"* is what not writing to those columns
  already means.
- **Destroying them is a money leak**, which `adr/0024` forbids and which this project has already
  paid for once — slice 7 took the Resource family out of order *"to stop a money leak six slices
  old"*.
- **The Pool needs no Departure yet.** Nothing creates a Household after world creation, so the Pool
  is a subset of a fixed population and cannot grow with elapsed time. `adr/0006` holds for a reason
  that has nothing to do with the mechanism the design intends — **and that reason expires the day
  immigration arrives**, which the ADR records as the named trigger.

**One invariant is qualified, not deleted.** `WorldInvariants` reports `HouseholdHomeExists` when
`Dwelling` fails to resolve. The claim becomes *a Household is housed **or** is in the Pool*, and a
Household that is neither is still a violation — which is the check worth keeping.

**Eviction itself shipped with task 7**, because `DestroyBuilding` cannot leave Households pointing at
a Building it is about to free and there was nothing to defer. It is three lines and a paragraph: the
Occupants are peeked rather than popped, since `Unplace` removes each from the list itself, and it
runs first while the Building is still whole because `Unplace` reads the dwelling handle to find the
list it is leaving. What remains of this task is the invariant qualification above.

**No emigration, no refusal reasons, no give-up counter, no rejected-arrival taxonomy.** All 9a's.

#### What closing it found — **done**

**The qualification had already shipped, and not where this task said it would.** Task 6 needed the
Pool for its create predicate, so it added `HouseholdIsHousedOrInThePool` as a **new** member with
both directions of the exclusive-or and two tests, rather than amending `HouseholdHomeExists` in
place as planned. That is the better outcome — the two claims are different claims, and the old name
says the wrong one — but it left the old member **reported by nothing**.

**One orphan, and it is the only one.** An audit of all 26 members against every `Invariant.X`
reference in `src/` found `HouseholdHomeExists` alone at zero call sites; every other member is
reported at least once. **An enum member nothing can report reads as a check that exists**, which is
the corpus's recurring failure mode arriving in a new place — a green mark that is not evidence a
claim was examined. It is now bannered and `[Obsolete]`, so a call site that tried to report it would
be a build warning rather than a silent lie.

**Retired in place, never renumbered.** The id travels: a violation reaches a human through a crash
artifact carrying the number, so letting a later invariant inherit 3 would make every artifact
written before that day say something false. A banner costs nothing and a reused id cannot be
un-reused — which is `CLAUDE.md`'s *superseded documents get a banner, never a deletion*, applied to
an identifier for the first time.

### 9. The tripwire — constant cost regardless of Zone size — **done**

**The milestone's own risk, stated as a number in advance** (`PROCESS.md` → *Tripwire*).

Sweep Zone size across at least two orders of magnitude with the sample size fixed, and measure
per-trigger cost. `02 §5.7` claims it is flat. **Publish the break-even, never a multiple over a
guessed denominator**: the wire is *the per-trigger cost at the largest Zone divided by the cost at
the smallest*, and the claim survives at 1.00× and fails above some stated bound.

Two failure modes the harness must not have, both from S2's findings list, both free to avoid here:

- **A zero that cannot move is not evidence** (R3's detour column). If the ratio reads 1.00× at every
  rung, pair it with a rung expected to be non-zero — a deliberate scan instead of a sample — or the
  column is indistinguishable from an instrument that is not wired up.
- **A denominator measured once has no error bar, and measured first has a systematic one** (R3's flat
  search). Measure the smallest rung twice, at both ends of the sweep, and publish both.

Both counters reach the Census as a **flow**, read as a sum and a peak over the interval, per slice 7
task 9's second metric family.

#### What it measured — **done**

`ZoneRuleBenchmarks`, four rungs, sample fixed at 16, half the Lots built on. Release, same machine,
one run. `ZoneRuleBenchmarkFixtureTests` holds the arrangement to the engine's own counters: every
Tick triggers, both branches of the sample are entered at every rung, and **nothing the benchmark
times can change the world it times** — which is what makes an invocation repeatable, and is checked
rather than asserted in prose.

| Lots in the Zone | sweep | denominator (256 Lots, re-timed at this rung) | scan control |
|---|---:|---:|---:|
| 256 | 488.1 ns | 474.7 ns | 371.4 ns |
| 2,560 | 490.6 ns | 485.1 ns | 3,668.5 ns |
| 25,600 | 596.1 ns | 474.4 ns | 36,138.6 ns |
| 256,000 | 740.3 ns | 475.2 ns | 367,418.8 ns |

Allocated is `-` at every cell, which is `ZoneSample`'s allocation-free claim measured rather than
asserted.

**The wire: 1.56×, against a bound of 2×.** 740.3 ns at 256,000 Lots divided by the 475.2 ns the
smallest Zone cost *at the same rung* — both ends of the division timed on one machine in one run,
so nothing in the figure came from somewhere else. Taken instead against the sweep's own bottom rung
it is 1.52×; the two agree to within the harness's noise, and the smaller is not the one published.

**The two guards both held, and both were worth having.**

- **The control moved 989× across a 1,000× Zone** — 371.4 ns to 367,418.8 ns, linear to 1.1%. So the
  instrument can move, and a flat column is a fact about the sweep rather than about the harness. R3's
  detour column is why this rung exists and it earned its place: without it, 1.52× would be
  indistinguishable from a benchmark timing an empty method.
- **The denominator drifted 2.3% across the whole sweep** — 474.7, 485.1, 474.4, 475.2 — against R3's
  flat search, which read 1,401,307 ns first and 477,609 ns last. That spread *is* this harness's
  error bar, and it is small enough that 1.56× is a measurement rather than a mood.

**The claim in `02 §5.7` is false in the letter and true in the substance, and the difference is not
the algorithm.** Sampling is `O(sample)` exactly — the sweep does the same sixteen draws, the same
sixteen liveness tests and the same sixteen dispatches at 256,000 Lots as at 256, and the control
rung shows what a size-dependent sweep would have looked like. What grows is the **memory
hierarchy**: sixteen random rows scattered over a 256,000-row Lot table miss where sixteen rows in a
256-row table hit. The staircase is visible in the column — ×1.005 from 256 to 2,560, then ×1.215 and
×1.242 as the table leaves each level of cache — and it has a floor, because DRAM is the last rung.
Zone size is not the variable; **working-set size is**, and it is bounded by the map.

**It is the third sighting of the same factor, which is why it is worth writing down.** Task 10a's
findings 42–43 attributed the Rule engine's laboratory-to-city gap to terms ×1.84, **scatter ×1.49**
and population ×1.14. This is scatter measured again, in a different mechanism, alone and with
nothing else moving: **×1.52**. A synthetic benchmark that touches a small table is measuring a
different machine from the one the simulation runs on, and the size of that difference is now known
twice.

**A number, not an argument, for `plans/0013`.** One Zone Rule triggering costs 740 ns at the largest
Zone measured. `rulesets/minimal.toml` triggers on a 32-Tick interval, so one Zone Rule amortises to
**23 ns a Tick**; sixteen of them all triggering on the same Tick — the worst alignment a Ruleset can
author — is **11.8 µs, or 0.08% of a 15.6 ms budget**. The multiplicand is *how many Zone Rules a
real Ruleset declares*, and per `0013`'s own organising column that number is **guessed**: the sweep
family has never met a content author. What the tripwire retires is the fear that the multiplicand
might be *Lots*.

**What would move it.** A per-Lot cost that grows with the map rather than with the Zone — a Lot row
widening past a cache line, or the sample gaining a term that reads a neighbour. Both would show as
the sweep column steepening while the control column stays where it is, which is the one shape this
harness distinguishes.

### 10. The `slots` half of slice 5 task 7's trend assertion — **done**

**Inherited, and this slice is the only thing that can discharge it.** `0003`'s gate board states the
reasoning: the Rule engine allocates no rows — a Rule Instance's life is its Building's — so no
Ruleset can make a table's slot count trend. What churns rows is Buildings arriving and being
demolished, *and that is this slice*.

Over the tail of a 100,000-Tick run with a Zone Rule creating and demolishing: `slots` must be flat
against a `live` that moves. A rising `slots` against a bounded `live` is **freed rows not being
reused** — the table growing to the high-water mark of a cycle rather than to the size of the city —
which is `adr/0006` in the one place nothing has ever been able to check it.

Slice 7 shipped the **flow** half of this assertion and shipped it stronger than asked, as exact
equality over a whole number of periods. The same discipline applies: choose a reading interval that
is a whole number of the Zone Rule's trigger period, or the assertion is about the sampling phase.

#### What it measured — **done**

`ZoneRuleLongRunTests`, 100,000 Ticks, 1,000 Citizens, `rulesets/minimal.toml` in force, read every
**2,048 Ticks — derived from the Ruleset rather than written down**, as 64 of the `[[zone_rule]]`'s
own 32-Tick period. Retuning the file therefore moves the reading interval with it, instead of
leaving an exact assertion quietly measuring a sampling phase. It bought exactly what it was meant
to: **64 triggers in every one of the 45 tail readings, asserted as equality**.

**Five of six tables are dead flat, and the sixth is flat for a different reason.**

| | `slots` | `live` across the tail |
|---|---:|---|
| Lots | **121**, never moves | 121 — nothing creates or destroys a Lot |
| Buildings | **121**, never moves | **54 → 78**, oscillating |
| Households | **360**, never moves | 360 — nothing destroys a Household |
| Bins | **242**, never moves | 108 → 156 |
| Rule Instances | **363**, never moves | 162 → 234 |
| Unplaced Pool | **300 → 312**, then flat | 282 → 306 |

`slots` for Buildings equals the Lot count and never exceeds it across a run that demolished and
rebuilt continuously, which is `adr/0006`'s collection half **checked for the first time**: every
freed row was handed back out rather than appended beside. The same holds for the Bins and Rule
Instances that hang off a Building's life.

**The Pool is a running maximum, and that is a different claim needing a different assertion.** Its
slot count is the largest number of Households ever homeless *at once*, so it legitimately creeps
while the run is still finding the largest eviction cohort it will see: 300, 304, 305, 309, 311, 312,
then 312 for the last 37,000 Ticks. Two things are asserted instead of flatness. The **ceiling**,
which is the population, because a Household is in the Pool at most once — a *structural* bound, and
it is why task 8's third finding said `adr/0006` holds here for a reason that has nothing to do with
the sink. And **convergence as a rate**, ≤1/32 growth in the high-water mark across the back half of
the tail.

**The rate is there because the alternative would have been fitted to the data.** The plateau arrives
at tail reading **26 of 45** — just past the midpoint — so a *flat after reading N* assertion would
have had an N chosen because the run has that shape. A rate has no such freedom, it is the same form
slice 7's flow half already uses, and the thing it is guarding against is not subtle: a Pool that
failed to recycle would allocate thousands of rows over 100,000 Ticks and cross the 360-row ceiling
almost immediately.

#### The finding underneath the numbers: the city drains into the Pool, and it is not a leak

> **⚠ AMENDED 2026-08-11 — this section records the right observation under the wrong headline, and the
> headline travelled.** *Five-sixths homeless* is `1 − capacity ÷ population` over a fixture that
> condemns every dwelling **on purpose** (`minimal.toml`'s `upkeep` draws on a Resource nothing produces),
> so the homelessness figure is fixed by knobs this file chose and carries no information — it would have
> read much the same at any occupancy. **The number that is evidence is the one this section states
> second: nearly half the *declared places that existed* stood empty while the Pool held 300 people.**
> Places existing that nobody can reach is not producible by any balance of rates. It was a missing door
> — `02 §5.2` step 2 — built as
> [`adr/0069`](../docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md),
> after which vacancy is **10%** and homelessness is **53%**, the residue being this fixture's arithmetic.
> The general form is in [`0018`](0018-session-n-the-bin-the-pool-and-the-economy.md) → *What shipping
> task 2 found*, finding 1: **a long run reports the fixture's arithmetic in every quantity the fixture
> determines.**

**In steady state ~60 of 121 Lots hold a Building and ~300 of 360 Households are homeless.** Nothing
trends, every table recycles, and the run passes — so this is not `adr/0006`. It is arithmetic:
**demolition evicts a Building's whole occupancy and creation rehouses exactly one Household.**
`SyntheticCity` puts three Households in each Building; `ZoneRuleEngine.Create` draws one member of
the Pool and places it. Every demolish-and-rebuild cycle therefore nets **+2** into the Pool until
eviction and rehousing balance, which they do — at a city five-sixths homeless.

**The two halves of the model disagree about what a Building holds, and neither is wrong on its own.**
The populator's three-per-Building is a sizing ratio; `Create`'s one is `adr/0054`'s *drained blind*
— there is no acceptance test, no rent and no tolerance, so placing more than one would be inventing
a capacity nobody has measured. What is missing is that **a Building has no occupancy at all**: a Lot
has a permission set and a Building has a kind, and how many Households either admits is a number the
Ruleset cannot express. That is `02 §5.4`–`§5.6`'s Phase 2 hole seen from the inside, and this is the
first run that could see it.

**It is filed rather than fixed, and the reason is `adr/0043`.** The claim *a Building should house
N Households* is **measurable** — the number that settles it is an occupancy declared in a Ruleset and
the machine is this run — so no session should settle it by argument, and today there is nothing to
declare it in. What can be said without a number: **the equilibrium is an artefact of the mismatch
and not of the mechanism**, so it will move when occupancy exists and no tuning done before then is
worth keeping.

---

## Acceptance — **met**

- ✅ `dotnet build` and `dotnet test` green with no GPU and no Godot. **683 tests.**
- ✅ A Ruleset naming an undeclared kind, an unpaintable permission bit, or a zero sample size is
  **refused with a file, a line and a rule name**, and the previous Ruleset stays live. Task 3.
- ✅ Replay equivalence holds over a session in which Buildings are created and demolished: two runs,
  one log, identical hash traces. Materially stronger than slice 7's, because row *allocation order*
  now depends on the free list and the free list depends on demolition order — and the golden session
  has churned 121 → 103 → 121 Buildings since task 7 re-recorded it.
- ✅ The tripwire of task 9 is published as a break-even with its denominator measured twice.
  **1.56×**, denominator re-timed at every rung, drift 2.3%.
- ✅ A 100,000-Tick run in which **no collection and no magnitude trends upward** — both halves, for
  the first time. Task 10 for the collections; the magnitude half is **structural rather than
  measured**, and saying so is the point: failure pressure is a duration whose clock is cleared by
  firing and whose holder is demolished at the threshold, so there is no accumulator to bound. That
  was `adr/0053`'s whole reason and it is why the *decay rate* row in the table above is struck.
- ✅ `02 §2.2`'s *a Lot is either vacant or holds exactly one Building* is registered as a whole-world
  invariant. `LotHoldsExactlyOneBuilding`, both directions, plus the `O(1)` write-site half.
- ✅ There is something to look at: **`--zones`**, printing the Lot grid by permission and occupancy
  before and after a run, with what the sweep did. It refuses without `--ruleset` rather than
  degrading, because an unchanging grid would read as a broken mechanism instead of as a file that
  declares no `[[zone_rule]]`. At 10,000 Citizens a city of 1,201 Lots visibly thins from solid to
  824 built and 377 vacant.
- ✅ Every unratified number this slice chose is in [`0002`](0002-open-questions.md) §D1 **with a named
  ratifier** (`adr/0052`): the **sample size**, the **trigger interval** and the **condemnation
  threshold**, bounded by task 10's run and selected by the first playable build. The fourth, the
  **decay rate**, was derived away rather than chosen.

---

## Decisions owed — three settled by the grill, one deliberately left

**Grilled before task 2 was written**, on the reasoning that decision 1 shapes a table rather than a
predicate and discovering it mid-task 7 would be expensive. It produced three ADRs and deleted one of
the four unratified numbers.

**~~1. Where the Households from a demolished Building go.~~ SETTLED** →
[`adr/0054`](../docs/adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md).
Branch (c), and cheaper than the plan feared: eviction is free because it never touches `Money` or
`Savings`, and the Pool needs no Departure because nothing creates a Household after world creation.
**The bound is real and its reason is not the design's** — that is the ADR's load-bearing half.

**~~2. Whether a Zone Rule may demolish a Building it did not build.~~ SETTLED** →
[`adr/0055`](../docs/adr/0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md).
The question decomposed into four sub-decisions of which only one was open. Repainting does **nothing
immediate** (`adr/0025`, plus `01 §5` listing rezoning and clearance as *separate* recovery levers),
which forces the rest: the permission bit scopes the create predicate, the sample population is every
Lot, and nothing is ever immortal.

**~~3. Pressure as a count of terminal firings, or weighted by condition.~~ SETTLED, and the question
was wrong** → [`adr/0053`](../docs/adr/0053-failure-pressure-is-a-duration-not-a-tally.md). Both
branches were tallies, and a tally **inverts severity** under `adr/0045`'s subscription model: a
comprehensively starved Building emits one event and sleeps, an intermittently supplied one emits many.
Pressure is a **duration**. This is the grill's largest finding and it would have shipped.

**4. Whether the create predicate's absence needs a name in the Ruleset schema.** Still open, and
deliberately. `rulesets/minimal.toml` handles this in prose. A second file doing the same suggests the
pattern wants a first-class spelling — or that prose is the right answer twice. Do not decide this on
one instance.

### The numbers, after the grill

Four became three, and the one that went was **derived away rather than chosen** — the third time that
has worked here, after tau and the arming stagger.

| Number | Standing |
|---|---|
| ~~**Failure-pressure decay rate**~~ | **Deleted.** A duration does not accumulate, so there is nothing to decay and `adr/0006`'s magnitude half holds structurally |
| **Condemnation threshold** | Authored in **missed firings**, not Ticks — dimensionless, and immune to a Ruleset that retunes every `rate` |
| **Sample size** | Its usual justification — *sample N, take the best* — has nothing to rank until `02 §5.4`'s choice model exists. Today it buys throughput, not choice |
| **Trigger interval** | `02 §4.2` names it Ruleset data. Composes with sample size into one rate; they differ only in burstiness |

All three go to [`0002`](0002-open-questions.md) §D **D1** with task 10's 100,000-Tick run as a
**bounding** ratifier — the values must sit where `live` neither collapses to zero nor stays flat — and
the first playable build as what selects within that band. The row says *bounds* rather than *picks*,
because pretending a pacing number was settled by a benchmark is exactly what `adr/0052` exists to stop.

---

## Owed to other documents, not questions

Per `PROCESS.md`: a correction goes to [`0012`](0012-corpus-audit.md), not to `0002`.

- **`06:108` contradicts `06:57`.** The no-milestone table says *"Policy as a Sweep Rule, and the
  Sweep Rule family entire — a milestone. **3a/3b are Bin Rules only**"*, while milestone **3c** in
  the same document is *"Map Layers and Zone Rules"*. A Zone Rule is a Sweep Rule. The row justifies
  itself by naming 3a and 3b and skipping 3c. **Policy** genuinely has no milestone; *the family
  entire* is over-stated, and this slice is the counter-example.
- **The Lot subdivider has no milestone.** `06`'s *Mechanisms with no milestone* table does not list
  it, and 5a is *"Road Graph and Streets"* — which names the graph and the geometry risk and never
  names the thing that turns zoned land into parcels. `02 §2.2` describes it in full, `adr/0014` and
  `adr/0035` both reason from its frontage rule, and nothing builds it. Until it exists **every Lot in
  this project is painted**, which is the sentence `02 §2.2` opens by denying, and slice 10 is the
  slice that made that visible without being able to fix it.
- **`02 §5.3` and `§5.7` use "sampling" for two different populations** with two different actors, and
  only §5.3 carries a number. See task 4.
- **`02 §5.7`'s *constant cost regardless of Zone size* is over-stated, and the correction is worth
  more than the claim was.** Measured at **1.56×** over a 1,000× Zone (task 9). The algorithm is
  `O(sample)` exactly and the section is right about *why*; what it does not say is that the cost is a
  function of the **working set** rather than of the Zone, so it grows until the Lot table leaves
  cache and then stops. Stated as written, the sentence would be falsified by a benchmark and the
  reader would draw the wrong conclusion from that. **Amend, do not strike.**

---

## What this slice deliberately does not do

- **No price surface, no land value, no capital, no bid contest, no pro-forma.** `02 §5.4`–`§5.6`
  entire. Phase 2.
- **No subdivider, and therefore no Zone verb that paints an area.** `02 §2.2` entire. It needs the
  Street network, which is milestone 5a's. See *The second collision*.
- **No construction time and no derelict state.** Both need a Building state that does not exist;
  the derelict flag is slice 8's.
- **No upgrade or downgrade.** `adr/0025`: the density ladder is walked at construction only.
- **No Policy.** The other instance of the family, and it needs conserved Money (`adr/0024`,
  `adr/0031`), which has no milestone. The family's *machinery* is built here; its second instance is
  not. Note the asymmetry `02 §4.2` insists on — a Zone Rule **samples**, a Policy **sweeps**, and
  anything reaching for sampling to make a Policy affordable has confused a behaviour model with an
  entitlement.
- **No Chunk partition or stagger on the sweep.** `02 §4.2` names them as the cost controls; task 9
  measures whether they are needed rather than assuming. Adding them first would make the tripwire
  unmeasurable.

---

## The correction S0b found — task 11, and it reopens a closed slice

**Added 2026-08-10, after S0b.** The slice is closed and met its acceptance; this is a defect it shipped
and the tripwire is what hid it. Settled by
[`adr/0059`](../docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md);
the numbers behind it are in [`spike-results`](../docs/spike-results.md) → S0b, findings 1–3.

**The defect in one line: `sample` is an absolute count of Lots per cycle, so the quantity the city
actually feels — a fraction of the city per cycle — is inversely proportional to the size of the city.**
A Lot is visited once per **0.12 Day at 1,000 Citizens and once per 117 Days at 1,000,000**. At target
scale the shipped Ruleset builds **nothing** in 2,000 Ticks, and it is not the create predicate:
`created` equals `vacant` exactly in every interval of every capture, so the Pool and the permission bit
have never once declined a candidate.

**Three things about how this got through are worth more than the fix.**

- **Task 9's tripwire is what hid it, and it was a good tripwire.** It measured `O(sample)` independence
  from Zone size, proved `02 §5.7`'s stated claim, and passed. **A test that confirms the stated claim
  while the unstated one fails is worse than no test** — the same shape as `0013`'s *right by
  cancellation*. What went unstated: the mechanism is scale-free in **cost** and its **time constant is
  the city**.
- **`adr/0052` was followed to the letter and still admitted it.** `0002` §D named the ratifier *in
  advance* — task 10's 100,000-Tick run — and it ran, bounded `sample = 4` against *"54–78 Buildings of
  121, oscillating, nothing trending"*, and passed. **A ratifier that runs at one city size cannot catch
  a number that is absolute.** This is the strongest argument in the corpus for a third question beside
  *measurable/arguable* and *who ratifies it*: **what does this number scale with?** *Nothing* is a
  claim, and here it was false twice in one file.
- **`0002` §D already had half the answer and stopped one step short.** Its row says sample and interval
  *"compose into a single rate; `N/I` is what the city feels"*. Correct, and `N/I` is still absolute —
  what the city feels is `N ÷ (I × Lots)`.

### Sequencing — this waits for slice 8

**Do not start task 11 until slice 8 lands.** It is hash-bearing and moves the three golden baselines,
and slice 8 task 10 is *the golden session reloading*, which re-records the same files. Two re-records
against one mechanism is how a baseline stops being evidence of anything.

### 11a. `revisit_ticks` in the Ruleset, and the refusals

`[[zone_rule]]` gains `revisit_ticks` and **loses `sample`**. Optional, defaulting to `Ticks.PerDay` —
derived, so no `adr/0052` ratifier is needed (`adr/0059`). Refusals, on `RulesetLoader`'s existing
pattern:

- **Zero, or absent with no default reachable** — refuse. Today's refusal 8 (*a sample of zero evaluates
  no Lots*) is the same check one level up and its message is re-pointed rather than deleted.
- **Shorter than `interval`** — refuse. It would round the derived sample against a cycle that cannot
  deliver it, which is `pollution_decay_ticks`'s *a duration shorter than the cadence* refusal exactly,
  and that one is worth copying because it was the one slice 8 task 3 found by reasoning rather than by a
  test failing.
- **`sample` still present in a file** — refuse **by name**, with a message pointing at `adr/0059`. A
  silently ignored key is how a designer's tuning stops taking effect without anything saying so, and
  every Ruleset on disk today carries one.

### 11b. The derivation, and where it is read

`sample = ceil(Lots × interval ÷ revisit_ticks)`, spelled through `IntegerMath` — there is no `/` in
`Core` outside `Arithmetic`, and the ceiling matters because flooring gives **zero** for any city smaller
than `revisit_ticks ÷ interval` Lots, which is 256 Lots at the defaults and therefore covers the golden
fixture and every test in the suite. **Flooring here would make the mechanism silently stop existing on
small worlds**, which is the defect being fixed wearing the opposite sign.

Read from `_world.Lots.Rows.SlotCount` in `ZoneRuleEngine.Sweep`, per trigger. **`SlotCount` rather than
`LiveCount`** for the reason `ZoneSample.Draw` already draws against it: the draw is over slots and
discards ones that are not live, so a denominator of live rows would systematically over-sample.

### 11c. Retire the `O(sample²)` duplicate scan

`ZoneSample.Draw`'s `Contains` walk is quadratic and justified by *"a sample is a handful of Lots"*. The
premise is gone. At a one-Day revisit at 1M it is ~110,000 comparisons a trigger, amortised to ~3,400 a
Tick — **affordable today and quadratic in a quantity now proportional to the map**, so it is replaced
rather than carried on the strength of one measurement.

The replacement may not be a `HashSet` (`05 §4` lint 3 bans walking one, and it would allocate). Two
candidates, and the choice wants a number rather than an argument: a **stamp array** indexed by Lot slot
holding the trigger ordinal that last touched it — `O(1)` per draw, one `int` per Lot, allocated once and
therefore `adr/0006`-safe by the same argument as the scratch buffer — or **accepting duplicates** and
letting the discard rule absorb them, which `ZoneSample`'s remarks already argue is *the model* rather
than a defect, and which would delete the problem instead of solving it. **Prefer the second if the
measured duplicate rate is negligible**, because it removes code. At a one-Day revisit the sample is
0.4% of the Lot table, so the birthday-collision rate is small and this is measurable in one run.

Note `02 §5.3`'s standing criticism of UrbanSim — *it samples with replacement and double-counts an
alternative's weight* — is why accepting duplicates needs the measurement rather than a shrug. The
defence is that a duplicate here costs a wasted evaluation and **not** a doubled weight, because the
create predicate is a boolean with no score; that stops being true the day `02 §5.4`'s choice model
arrives.

### 11d. The scratch buffer's bound, restated

`ZoneRuleEngine.Scratch`'s remark says it is *"bounded by the Ruleset rather than by elapsed time"*. It
becomes bounded by the **Lot count**, hence by the map. Still `adr/0006`-safe — a map does not grow with
elapsed time — but the **stated reason changes**, and a remark that is true by accident is how the next
person gets it wrong. Rewrite it; do not leave it.

### 11e. Re-record the three baselines, and say why in the message

`session.borough`, `session-trace.txt` and `world-hash.txt`, per the procedure in
`tests/Borough.Tests/Golden/README.md`. **`World.HashSeed`'s version byte is not bumped**: the fold is
unchanged and this is a behaviour change, which is precisely what that byte exists to distinguish a
regression *from*. Under `CLAUDE.md`'s own test — *a change is an optimisation if the State Hash is
unchanged and a design change otherwise* — this is a design change, deliberately.

### Acceptance for task 11

- **The scale test, which is the point**: a run at 1,000 and a run at 1,000,000 Citizens produce the
  **same occupancy at the same τ**, and the per-Lot revisit period is within one interval of
  `revisit_ticks` at both. S0b's collapse table is the shape to reproduce.
- **A 1M run creates Buildings.** `zones created` is non-zero within a few thousand Ticks, which is the
  observation that opened this and the cheapest possible regression test for it.
- **`created` still equals `vacant`** wherever the Pool is non-empty — the create predicate was never
  the defect and must not become one.
- **The Tick price does not move measurably** at 1M against S0b's 8.72 ms, which is the cost claim this
  fix leans on and the one thing that would invalidate it.
- **Every Ruleset on disk loads**, or is refused **by name** for carrying `sample`. Silence is failure.

### Task 11 as built — **done**, and what it found

**Acceptance, against the list above.**

| Claim | Result |
|---|---|
| The scale test | **Met, and asserted rather than captured.** `The_revisit_period_is_the_same_at_every_city_size` runs 2,048 and 204,800 Lots — a 100× span — and both observe a per-Lot revisit period of **exactly 8,192 Ticks**. It is an equality rather than a tolerance because the fixture's Lot counts divide exactly; see below on why that is not a dodge |
| A 1M run creates Buildings | **Met.** `--zones --citizens 1000000 --ticks 2000`: **2,898 raised**, against S0b's **0**. 63 triggers, 469 Lots a trigger, one visit per Lot every 8,192 Ticks |
| `created` still equals `vacant` | **Met, exactly** — 2,898 evaluated vacant, 2,898 raised. The create predicate has still never declined a candidate at scale |
| The Tick price does not move | **Met, and it moved the *other* way.** Same binary, same world, two Rulesets differing only in `revisit_ticks`: 960,008 (which reproduces the retired `sample = 4` at this Lot count) ran 2,000 Ticks in **26.3 s and 26.2 s**; the shipped 8,192 ran **24.0 s and 24.4 s**. **117× the Lot evaluations for 8% less wall clock**, because 25,213 demolitions take that many Buildings' Rule Instances out of the Rule engine. S0b saw the same sign at a smaller magnitude |
| Every Ruleset on disk loads, or is refused by name | **Met.** Both files carry `revisit_ticks`; refusal 10 fires on a `sample`, by name and with the ADR in the message |

**Finding 1 — the acceptance criterion this list was written with is not satisfiable, and the reason is
the ceiling that had to be there.** *"The per-Lot revisit period is within one interval of
`revisit_ticks` at both"* cannot hold at 1,000 Citizens: 132 Lots at an interval of 32 want a sample of
**0.52**, the ceiling gives **1**, and the city is therefore surveyed roughly **twice a Day** rather
than once. That is not a rounding error to be tightened — flooring is what gives **zero** below
`revisit_ticks ÷ interval` Lots and deletes the mechanism on every small world, which is 11b's own
argument. **So the derived sample is exact only where `Lots × interval` divides by `revisit_ticks`, and
below that it errs toward doing the work.** The criterion should have said *the error is bounded by one
Lot a trigger and its sign is toward surveying*, which is a statement a small city can satisfy.

**Finding 2 — the duplicate scan never bought coverage, and that is what retired it rather than the
measurement.** 11c asked for a number before choosing between a stamp array and accepting duplicates,
and the number is small and **scale-free**: the duplicate rate depends on `sample ÷ lots`, which
`adr/0059` makes exactly `interval ÷ revisit_ticks` — a property of the *file* and not of the city — so
one measurement settles every city size at once, and at the shipped `32 ÷ 8192` it is ~0.2%.
`Duplicates_are_negligible_at_the_shipped_revisit_period` holds it. **But the argument that actually
decides it is structural**: deduplicating *within* a trigger never made a trigger reach more Lots, since
the same slots come up either way and the scan only skipped the second look. It bought avoiding a
doubled **evaluation**, where `02 §5.3`'s criticism of UrbanSim is about a doubled **weight** — and the
create predicate is a boolean with no score. **The day `02 §5.4`'s choice model arrives that stops being
true**, and the answer then is the stamp array rather than the scan.

**Finding 3 — the golden session stopped covering the create branch, and nothing would have said so.**
At 132 Lots the derived sample is **1** where `sample = 4` was four, so over the session's eight
triggers it condemned and never once landed on a Lot demolition had cleared: **0 raised against 3
before.** Every hash still moved, every test still passed, and the committed trace quietly covered half
the mechanism. The session was lengthened **256 → 2,048 Ticks** at a cadence of **64** (holding the
trace at thirty-two samples) with the reload moved to **1,024**, which gives 12 raised and 49 condemned;
and `The_golden_session_raises_buildings_as_well_as_condemning_them` is the line that makes the
lengthening mean something rather than being a number somebody once chose. **The general shape is
`0012`'s *Cause 1* in a fixture rather than in a document**: a baseline records what a run *did*, so a
change that narrows what the run *reaches* is invisible in it by construction.

**Two smaller things.** `IntegerMath` gained a `CeilDiv(long, long)`, because `Lots × interval` reaches
40 bits at a large map and a long interval while the answer fits an `int` comfortably. And
`ZoneRuleBenchmarks` now inverts the derivation per rung to hold its sample at 16 — the tripwire's
question is unchanged, and the fixture having to work at it is `adr/0059` in miniature: the benchmark
was measuring scale-freedom in **cost** and passing, while the mechanism's time constant was the city.
