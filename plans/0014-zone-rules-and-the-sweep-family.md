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

**Task 1 shipped. Grilled.** The gate is clear and slice 7 — its only dependency — closed with task
10a.

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
- **Next: task 5**, the trigger in Tick phase 6.

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

### 5. The trigger, in Tick phase 6 — and what it does *not* share with a Bin Rule

`Simulation.Growth` stops being empty. The Zone Rule evaluates **and acts** inside phase 6.

**This is `adr/0033`'s observable difference, made concrete, and it is the first time the claim has
been checkable.** A Bin Rule proposes in Tick phase 2 and is settled in phase 3 by a counter-based
shuffle, because two Rules may contend for one Bin. A Sweep Rule has no such split: it acts where it
runs. So the two families differ in **when their effect becomes visible within a single Tick** — which
is exactly the class of difference the ADR asserts and `05 §4` says makes a migration a design change.

Contention between two Zone Rules over one Lot is resolved by **scan order** and nothing else, because
`02 §5.5`'s bid-price contest needs prices. `02 §4.2` supplies the mitigation that *does* exist —
**rotate the scan start per trigger** — and the reasoning is the wait list's: a fixed order privileges
the same low-index Lots for the life of the city, and no player could see why. Rotation is required
here, not optional, and it is cheaper to write once centrally than to argue about later.

### 6. Create — a Building on a vacant, permitted Lot somebody wants

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

### 7. Demolish — and failure pressure is a duration, not a tally

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

### 8. Eviction — where the Households go, and it is settled

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

**No emigration, no refusal reasons, no give-up counter, no rejected-arrival taxonomy.** All 9a's.

### 9. The tripwire — constant cost regardless of Zone size

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

### 10. The `slots` half of slice 5 task 7's trend assertion

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

---

## Acceptance

- `dotnet build` and `dotnet test` green with no GPU and no Godot.
- A Ruleset naming an undeclared kind, an unpaintable permission bit, or a zero sample size is
  **refused with a file, a line and a rule name**, and the previous Ruleset stays live.
- Replay equivalence holds over a session in which Buildings are created and demolished: two runs, one
  log, identical hash traces. This is materially stronger than slice 7's, because row *allocation
  order* now depends on the free list, and the free list depends on demolition order.
- The tripwire of task 9 is published as a break-even with its denominator measured twice.
- A 100,000-Tick run in which **no collection and no magnitude trends upward** — both halves, for the
  first time. `slots` flat against a moving `live`; failure pressure bounded by its decay.
- `02 §2.2`'s *a Lot is either vacant or holds exactly one Building* is registered as a whole-world
  invariant.
- There is something to look at: `--zones` or equivalent printing the Lot grid by permission and
  occupancy, in the register of `--layer pollution`. A city that visibly fills in and thins out.
- Every unratified number this slice chose is in [`0002`](0002-open-questions.md) §D **with a named
  ratifier** (`adr/0052`) before it closes. At minimum: the **sample size**, the **trigger interval**,
  the **failure-pressure threshold** and its **decay rate**.

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
