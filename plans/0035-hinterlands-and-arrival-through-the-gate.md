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

**Eight decisions were owed. One is settled; seven remain and five come before any task.** They are §*Open decisions* below.

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

### 2. Is an arriving Household created into the Pool, and what does that do to the Pool's meaning? Typed *arguable*

`World.CreateHousehold` demands a dwelling; `World.Unplace` refuses an unhoused Household by invariant.
So **the Pool cannot today contain a Household that has never lived here**, and arrival needs a second
door — `World.Arrive`, creating unhoused straight into the Pool. ⚠ **The invariant is not an obstacle to
route around; it is the corpus stating what the Pool means**, and `CONTEXT.md` → Unplaced Pool already
says the opposite: *"immigrants, existing Households that decided to move, Households the city generated
itself … and Households evicted. **All four enter on equal terms**."* ***A door the design describes and
an invariant refuses is a disagreement, not a defect***, and which one moves is the decision.

### 3. Does the give-up rule ship here? Typed *arguable*, and `CONTEXT.md` has already answered it

`CONTEXT.md` → Unplaced Pool: *"the day immigration arrives, that reason evaporates and Departure becomes
load-bearing. **Whoever builds the gate owes the give-up rule in the same milestone.**"* `06` schedules
Departure at **19**. ⚠ **This is [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md)
territory and not a nicety**: a Pool with an inflow and no sink is a collection that grows with elapsed
time, and this milestone would be the first to open one.

### 4. Do Settlements ship here? Typed *arguable*

`06` places *Settlements — commute-shed components, merge and split* at **11**; **11's own risk row never
mentions them**. And the algorithm is open: `adr/0020` is amended by S2 R1 on evidence — union-find
computes **weak** connectivity where the corpus needs **strong** — and `RoadConnectivity.cs:34` records
that disagreement in the build. ***A row placed at a milestone by a table is not a row the milestone's
risk statement carries***, which is `06`'s inventory drifting against `06`'s own table.

### 5. Do Shipments ship here? Typed *arguable*, and the corpus contradicts itself

`06` inventory: *"Shipments … ✅ **Placed: 11**, behind 12"*. **11 runs before 12.** A row cannot be placed
at 11 and behind 12, and `06` warns two paragraphs above the table that the 2026-08-18 permutation was
applied mechanically. ***A permutation applied to a cell that names two milestones can satisfy one of
them.***

### 6. What ratifies the three §D2 numbers, and against what world? Typed *measurable*

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

### 7. Does money cross the gate in this milestone, and what happens to the exact equality? Typed *arguable*

[`0033`](0033-conserved-money-and-the-treasury.md) wrote milestone 10's conservation assertion
anticipating this: *"Write it that way and let milestone 11 add the term."* So the shape is decided and
the timing is not. ⚠ **Beside it sits a question that is explicitly blocked on this milestone** —
*where does a departing actor's balance go?* — which [`0002`](0002-open-questions.md) types *arguable*
and says is either a **gate crossing** (this milestone's machinery used a second time) or an
**inheritance**.

### 8. What does an import payment pay to? Typed *arguable*

`RulesetLoader.cs:1538` records that the money-balance refusal **over-refuses an import payment by
design**, because no scope can name its counterparty. `Scope.Pool` is the market and it is **milestone
12**. So either this milestone names a counterparty scope for the gate, or a Rule cannot buy anything
from the Outside until 12 and the anchor stays unobservable for two milestones.

---

## Tasks

⚠ **Provisional, and deliberately unnumbered against a schedule.** Six of the eight decisions above come
before the first task that composes, and decisions 1, 4 and 5 change *which of these tasks exist*.

- **The Outside Connection kind** — a `[[building]]` kind, edge-constrained placement, an Access Point,
  and `min(declared ceiling, Segment capacity)` with **which one binds** reported.
- **The Hinterland** — one per edge, authored, in the units a District exposes. ⚠ **Its field list differs
  in three documents** (F9) and the milestone must settle one.
- **The arrival door** — `World.Arrive` into the Pool, and decision 2's invariant.
- ~~**The comparison**~~ — **struck by decision 1**; it is milestone 16's. What ships here is the **route**: a Trip originating at an Outside Connection, and a Command that starts one.
- **Rejected arrivals, with reasons** — `adr/0023` makes this a *required deliverable*, not a readout:
  *"Without this the anchor is felt rather than observed, and the whole legibility argument collapses."*
- **Money crosses the gate** — `MoneySupply.Issued`'s second writer, and conservation as supply + flow.
- **A Ruleset with an Outside Connection in it** — ⚠ **and this is the task milestone 9 proves must be
  scheduled rather than assumed.** Its F17: the producer shipped *correct and unobservable* because no
  shipped world exercised it, and the fix was a ninth Ruleset. **Every one of the nine says in its own
  header that it models no city.**
- **Something to look at** — a runner mode.
- **The long acceptance run** — ⚠ with a **sink**, per decision 3, or `adr/0006` fails by construction.

---

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
