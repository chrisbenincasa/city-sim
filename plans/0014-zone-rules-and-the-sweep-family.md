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

**Not started.** The gate is clear and slice 7 — its only dependency — closed with task 10a.

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

## Tasks

### 1. The Lot's permission set, and a `zone` verb that paints an area

`LotTable`'s `Zone` byte becomes a **permission set** matching `Command.Zone`'s `ushort`, discharging
the narrowing `Command.cs:101` records. `CommandKind.Zone` stops creating exactly one Lot and paints
the region the command describes.

**A permission set is a set, and the distinction is `adr/0025`'s.** Density is a *cap*, so a Lot's
permission is *which forms may be built here*, not *which form is here*. One bit per admitted kind is
the cheap spelling and the honest one; a single enum would re-introduce the "zone type" the design has
been avoiding since `02 §2.2`.

Saved and hashed — it is player intent, and `05 §4` says a different permission set is a different
city.

### 2. Which Building is on this Lot — the derived reverse index

**The relation is one-directional today.** `BuildingTable` declares `Lot`; `LotTable` declares
`East`, `North` and `Zone` and **no Building handle**. So *"is this Lot vacant"*, which is the first
question a Zone Rule asks and the one it asks most, has no answer without scanning Buildings — which
would make the sweep `O(Buildings)` per sample and lose the constant-cost property task 9 measures
before task 9 could measure it.

Declared **`Derived`**, not `Saved`: it is recoverable from `BuildingTable.Lot` in one pass, so it
stays out of the State Hash and out of the save, and it is rebuilt in `World.RebuildDerived`, which
already exists and already does exactly this for other indices.

*`02 §2.2` states the invariant this index materialises — "a Lot is either vacant or holds exactly one
Building" — and nothing currently enforces it. It becomes checkable here, and belongs in the
whole-world tier.*

### 3. The Zone Rule in the Ruleset, and its refusals

A `[[zone_rule]]` table, loaded by `Borough.Formats.RulesetLoader` on the same walk as the other five
refusals, reaching `Borough.Core.Rules.Ruleset` as ids and integers and never a string (`adr/0048`).

It declares: the **trigger interval**, the **permission bit** it applies to, the **sample size**, and
the **kind** it builds. `02 §4.2` fixes the first as Ruleset data and not a scheduling knob — *"a
Policy paying daily is a different city from one paying weekly"* — which is what keeps it out of being
a `const` and inside `adr/0015`.

New refusals, in the same style as the existing five:

- a Zone Rule naming a **kind the Ruleset does not declare**
- a Zone Rule naming a **permission bit no `zone` verb can paint**
- a **sample size of zero**, which loads clean and sweeps nothing — the `apply = {min=1,max=4}`
  behaving as `{1,1}` defect (finding 19) arriving in the second family

### 4. The sample — and it is a new `purpose_tag`

`PurposeTag` gains a third member. It has `RuleSettleOrder` and `RuleArmingStagger` and the comment on
the second states the rule this obeys: two uses of randomness over the same row that answer *different
questions* must not share a tag, or the two decisions are correlated invisibly.

**Sample without replacement**, per `02 §5.3`'s implementation note — UrbanSim samples *with*
replacement and double-counts an alternative's weight, which the section calls out as a real if minor
defect. Copying the defect knowingly would be worse than copying it unknowingly.

**The sample size is hash-bearing, and `02 §5.3`'s `N` is not it.** §5.3's `N` is the number of
*dwellings a Household considers*; §5.7's is the number of *Lots a developer evaluates*. Different
population, different actor, and the *argument* for sampling transfers while the *number* does not.
The corpus has no number for the second anywhere. Under [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
it is written into `0002` §D on the day it is chosen, with a named ratifier — and *"a profile"* or
*"a future spike"* is not one.

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

### 6. Create — a Building on a vacant, permitted Lot

`World.CreateBuilding` already gives a Building its kind's Bins and Rule Instances with an arming
stagger, so this task is the *decision* and not the construction.

The predicate is **vacant AND permitted**, and the Ruleset comment says in its own words why it is not
`02 §5.6`'s: no price surface, no capital, no Unplaced Pool, no bid. `rulesets/minimal.toml`'s header
is the model for the register — it says *"it models no city, and it is not the beginning of one"*, and
that sentence is why nobody has mistaken it for content.

**Construction time is deliberately not built** (`02 §5.7`'s second pacing mechanism). A Building
under construction occupies its Lot and produces nothing, which needs a state a Building does not
have; it is Phase 2's, with the derelict flag slice 8 owes.

### 7. Demolish — from the one failure source that exists

A Building accumulates failure pressure from **Rules reaching a reporting terminal**, and past a
Ruleset-authored threshold it is demolished and its Lot returns to vacant (`02 §5.9`).

Two properties this must have, both from `02 §5.9` and neither optional:

- **The accumulated condition is retained and readable.** *"abandoned: 74% of work trips exceeded
  commute budget over 30 days"* is the section's example; ours is a terminal count, which is less
  interesting and the same shape. A demolition with no sentence behind it is the sad-face icon the
  section exists to refuse — `LEGIBLE CAUSE`.
- **Failure pressure needs a sink**, or `adr/0006` is violated by the mechanism built to prove the
  city is bounded. Pressure that only accumulates makes every Building eventually fall over, at a rate
  set by elapsed time rather than by conditions. It decays, and the decay rate is Ruleset data — the
  same shape `adr/0051` gave pollution, and found the hard way when a `map` emission accumulated with
  no sink for six slices.

**Buildings do not shrink** (`adr/0025`). There is no downgrade in this slice and there should not be
one: `02 §5.9` calls the earlier "declines a density level" wording physically incoherent, and the
density ladder is walked at construction only.

### 8. What happens to the Households in a demolished Building — and this one is a hole

**`World.DestroyBuilding` does not touch Occupants.** It frees the Building's Rule Instances, wakes
and frees its Bins, and frees the Building row. `BuildingTable` declares `OccupantHead`/`OccupantTail`
and `HouseholdTable` declares `Dwelling` — a handle at the other end of that list.

So demolishing an occupied dwelling leaves every resident Household holding a **handle to a freed
row**, which `Rows`' generation counter turns into a `StaleHandleException` on next access. Loud, at
least — but the correct behaviour is not *don't throw*, it is **the Households go somewhere**, and the
somewhere is the **Unplaced Pool**, which does not exist.

**This is slice 10's `pool`.** Options, none of them settled here:

- **(a)** Demolish only unoccupied Buildings. Cheap, honest, and it makes the demolish path unreachable
  in any populated world — `SyntheticCity` places Households in every dwelling — so it would ship a
  mechanism nothing exercises, which is the defect task 9's tripwire exists to catch elsewhere.
- **(b)** Destroy the Households with the Building. Wrong, and quietly: it is an unbounded population
  sink with no Departure record, and `06` mechanism 2 already owes a milestone for where Households
  come from and go.
- **(c)** Build the minimal Unplaced Pool this slice needs, and let `06` 9a generalise it. The shape
  slice 7 took for the Event Wheel (`0011` decision owed 1 branch (c)), which worked.

**Recommendation: (c)**, with slice 7's reservation attached — it must not quietly decide what `06` 9a
owns. The Pool's *semantics* — immigration, Departure, rejected-arrival reasons — are not this slice's.
A list of Households with no dwelling is.

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

## Decisions owed, found while planning

**1. Where the Households from a demolished Building go.** Task 8. Three branches, (c) recommended.
Arguable, and it is a fairness-and-bookkeeping question rather than a measurable one.

**2. Whether a Zone Rule may demolish a Building it did not build.** The Ruleset declares a Zone Rule
against a permission bit and a kind. A Building whose Lot's permission set has since been repainted by
the player is outside every Zone Rule's population, so nothing can ever demolish it — a Building
immortal by a paint stroke. Arguable, and it interacts with slice 8: a hot reload that removes a kind
already owes `02 §4.3`'s **derelict rather than deleted**.

**3. The relationship between failure pressure and the reporting terminal's recorded condition.**
`adr/0045` made the terminal *record why*; this slice makes something *read* it. Whether pressure is a
count of terminal firings, or is weighted by which condition, is a modelling decision with `LEGIBLE
CAUSE` consequences — the inspector sentence differs.

**4. Whether the create predicate's absence needs a name in the Ruleset schema.** `rulesets/minimal.toml`
handles this in prose. A second file doing the same suggests the pattern wants a first-class spelling —
or that prose is the right answer twice. Do not decide this on one instance.

---

## Owed to other documents, not questions

Per `PROCESS.md`: a correction goes to [`0012`](0012-corpus-audit.md), not to `0002`.

- **`06:108` contradicts `06:57`.** The no-milestone table says *"Policy as a Sweep Rule, and the
  Sweep Rule family entire — a milestone. **3a/3b are Bin Rules only**"*, while milestone **3c** in
  the same document is *"Map Layers and Zone Rules"*. A Zone Rule is a Sweep Rule. The row justifies
  itself by naming 3a and 3b and skipping 3c. **Policy** genuinely has no milestone; *the family
  entire* is over-stated, and this slice is the counter-example.
- **`02 §5.3` and `§5.7` use "sampling" for two different populations** with two different actors, and
  only §5.3 carries a number. See task 4.

---

## What this slice deliberately does not do

- **No price surface, no land value, no capital, no bid contest, no pro-forma.** `02 §5.4`–`§5.6`
  entire. Phase 2.
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
