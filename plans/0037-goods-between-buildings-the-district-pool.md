# 0037 — Goods between Buildings: the District Pool

**`06` milestone 12.** Ungated. The milestone that makes `Scope.Pool` resolve, and with it the first
Goods chain that crosses an ownership boundary.

---

## Status

🟢 **SCOPING STARTED 2026-08-21. The scoping sitting ran 2026-08-22 and settled SEVEN of the nine
decisions — 1, 2, 4, 5, 6, 8 and 9. Two remain: 3 and 7. No task has begun.**

⚠ **This block is rewritten rather than appended to, because the version it replaces said "one sitting
has run and it settled less of decision 1 than it changed about it" and then six more decisions closed
under it.** ***A status paragraph written mid-sitting describes the sitting's first hour***, which is
[`0012`](0012-corpus-audit.md) **Cause 1** arriving inside a single document over a single day.

**Seven ADRs came out of it** —
[`0132`](../docs/adr/0132-the-district-is-derived-and-a-ward-is-what-the-player-draws.md) (the District is
derived; the player's object is a **Ward**),
[`0133`](../docs/adr/0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md)
(a Pool draw pays haulage — **structural**, and superseded in part),
[`0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)
(centre and basin),
[`0135`](../docs/adr/0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md) (a
Provider and a moving price),
[`0136`](../docs/adr/0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md)
(Upkeep unplaced),
[`0137`](../docs/adr/0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md)
(bankruptcy needs one field) and
[`0138`](../docs/adr/0138-freight-is-unbuilt-so-the-min-follows-it-and-neither-is-at-twelve.md) (freight
and the `min()` unplaced).

🔴 ⚠ **The sitting made 12 BIGGER and unplaced three things, and those are not in tension.** It added the
District — *the largest unscoped piece in the milestone, listed in no inventory anywhere* — a Provider
kind with three content decisions, the tâtonnement, the Hinterland price per Good, one `Evidence` field
and a new Ruleset. What it removed — **Upkeep, freight, the `min()`** — was never scoped here by anybody;
each had been **parked at 12 by a different document, against one blocker its author happened to be
holding**. ***Three mechanisms found parked on an unchecked assumption, in one day, by asking what 12
actually ships.***

What has run is the **blocker re-check** `06`'s milestone 12 row demands in its own last sentence, and
a **survey of what the build holds**. Both are below. ⚠ **The decisions in this document are open and
are not to be settled by this file** — they are typed *measurable* or *arguable* under
[`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
and wait for a sitting, which is how every milestone since 7 has been scoped.

🔴 ⚠ **The survey found three preconditions that no document listed as blockers, and two of them are
structural.** `06`'s row and `adr/0117` between them enumerate four grounds, **all four about
Upkeep**. Nothing anywhere enumerated what the *District Pool itself* needs. ***A milestone whose
named risk is a single `throw` reads as a milestone with a single obstacle***, and the `throw` is the
symptom rather than the work.

---

## The named risk, as `06` states it

> That the Rule engine ships a scope it refuses. `RuleEngine.cs:803` throws on `Scope.Pool` **by
> name**, so [`02 §4.3`](../docs/02-simulation-model.md)'s own worked example — a bakery drawing from
> the District Pool — is unloadable, and no chain in [`04`](../docs/04-economy-and-goods.md) can cross
> the ownership boundary it names.

It is **the only root with a consumer already in the build**: the chain machinery, the wait lists, the
atomicity over net deltas and the `on_fail` ladder all exist and all stop at this scope.

**The refusal is not a stub.** Its message is an argument, and the argument is the milestone's brief:

> *"NOTE (`adr/0050`): the Pool is a **MARKET**, not a wider Bin lookup. A pool term crosses an
> ownership boundary, so the Good moves one way and money the other at the prevailing price, settled
> atomically with the Rule. Implementing this as a Bin lookup ships an **unconserved economy**, and no
> refusal can catch that."*

⚠ **That last clause is the whole reason this milestone is dangerous rather than merely large.** Every
other invariant in this project fails loudly. This one cannot: a Pool implemented as a lookup produces
a city that runs, hashes, saves and reloads, and is wrong in a way no test asks about. ***The cheapest
possible implementation is the one the refusal exists to prevent.***

---

## The blocker re-check — ✅ RAN 2026-08-21

`06`'s row ends *"re-check all three before starting it."* It ran, and **the row it appears in was
wrong in two ways**, both struck in place and filed to [`0012`](0012-corpus-audit.md).

**First, the count was inverted.**
[`adr/0117`](../docs/adr/0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md)'s
consequences say *"only ground 1 is discharged by arriving at 12"* and *"whoever picks Upkeep up
re-checks grounds **2, 3 and 4**."* The row said *"three of its four blockers arrive here and one does
not."* **One arrives and three do not.** ***A summary that inverts its source's count fails in the
reassuring direction***, and this row is read at exactly the moment somebody decides what to scope.

**Second, and worse: the row had dropped a whole ground.** `adr/0117` has four *grounds* — the loader
refusal, **both missing terms as one**, the transfer-versus-purchase **shape**, and the missing actor.
`06` had four *blockers* — counterparty, cost, life, actor. The counts match and the sets do not.
Splitting `adr/0117`'s ground 2 into two freed a slot, and **ground 3 fell out of it silently**.
***A re-partition that preserves the count reads as a restatement and is a deletion***, and the
preserved count is what hides it: a reader checking four against four finds nothing wrong.

### What the re-check found, read off the build rather than off prose

| Ground | State at 12 | Evidence |
|---|---|---|
| **1 — a counterparty** | ✅ **Discharged by arriving here.** `Scope.Pool` is the only market spelling the enum has | `RuleEngine.cs:803`; [`0035`](0035-hinterlands-and-arrival-through-the-gate.md) decision 8 routed trade here explicitly |
| **2 — a construction cost** | 🔴 **Open.** No Ruleset key authors a cost of anything. `adr/0035` denominates it in **Lane-Tiles**, which is **21**. `adr/0091`'s compulsory-purchase price — the trigger `adr/0117` names as the likeliest first authored cost — **has not landed** | `rulesets/*.toml`; `adr/0091` |
| **2 — a design life** | 🔴 **Open.** *"Not authored by hand… derived from the share of a mature city's budget"*, and there is no budget and no mature city. Plausibly **15** | `adr/0035` |
| **3 — the shape** | 🔴 **Open, and this is the one `06` had lost.** Upkeep is specified with a **transfer's syntax** and a **purchase's semantics**; settling it decides whether the authored quantity is **money or Materials** | `adr/0117` §3, assigned to *"whoever builds Upkeep"* |
| **4 — an actor** | 🔴 **Open, confirmed in code.** `BinOwnerKind` is `None / Building / Household / Business / Treasury` — **no Segment**. Nothing under `Rules/` attaches a Rule to a Segment. `RuleEngine.Bin` still takes `int building` | `Rules.cs:42`; `RuleEngine.cs` |

**Upkeep's subject is a Segment, its payer is the treasury and its counterparty is a market — three
different things — and the engine has no Rule whose subject is not its payer.** Arriving at 12 buys the
third of those and neither of the other two.

### And one thing that looks like an omission and is a decision

`adr/0117` recorded that an import payment *"has no spelling in any milestone until somebody adds one,
which is **11**'s business."* The `Scope` enum today is still `Local / Pool / Global / Map`.

**That is not 11 forgetting.** [`0035`](0035-hinterlands-and-arrival-through-the-gate.md) decision 8
settled it as a deliberate **no**: naming a counterparty scope at 11 invents one a single milestone
before the real market supersedes it, and ***two scopes for one idea, one milestone apart, is how a
superseded mechanism acquires content.***

⚠ **The live consequence for this milestone.** `adr/0035` sends Upkeep's money to local wages **or** out
through the gate. **12 buys Upkeep the local-Processing counterparty and not the imported one**, so
even a fully-answered ground 2 and ground 3 would leave half of Upkeep's money path unspellable.

---

## What the build already holds — surveyed 2026-08-21

**Read this before scoping a task.** Everything here was read off a symbol, not off a description
([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)).

### What exists and works

| Thing | Where | Note |
|---|---|---|
| Bin Rules, atomicity over net deltas, the apply band | `RuleEngine`, `Rules.cs` | The transformation half is done and has been since slice 7 |
| The two wait lists and round-robin drain | `World.Drain`, `World.Subscribe` | ⚠ Carrying an open defect — [`0003`](0003-build-plan.md) queue **item 11**, whose hold released today |
| `on_fail` chains and the fallback ladder | `RuleEngine`, `rulesets/diagnosed.toml` | `adr/0045`'s ladder — *local → Pool → Shipment → import → terminal* — is **a price ladder**, and only its first rung resolves |
| Money as a conserved Resource, and a treasury | `MoneyLedger`, `TreasuryTable` | `adr/0024`, `adr/0114`–`adr/0116`. Milestone 10 |
| **Household and Business balances** | `World.OpenBalance`, five call sites | ✅ **Both owned.** See the correction below |
| Road connectivity | `RoadConnectivity.cs` | `Scope.Pool`'s own doc says the scope *"requires road connectivity"*, and this is what it would ask |
| An authored Hinterland, one per map edge | `[[hinterland]]`, `rulesets/bordered.toml` | Milestone 11. See the gap below |

### 🔴 Precondition 1 — **there is no District**

**`DistrictTable` does not exist. `DistrictId` does not exist.** Every occurrence of the word
*District* in `Borough.Core` is in a comment or a doc-comment; not one is live code.

`CONTEXT.md` defines it fully — *"a contiguous named region, either player-drawn or automatically
derived… the boundary within which Goods pool without physical transport"*, **a contiguous set of
Cells and never of Chunks**, with a working anchor of **128 Cells (2.10 km², ~1.45 km across)** and the
claim that *"the count is **physics rather than a design choice**."*

⚠ **So the scope is named `Pool` after an entity the build has never had.** `Scope.Pool`'s doc reads
*"Bins on the **Building's** District Pool"*, and there is no District for a Building to be in.
***A scope that resolves through an entity is blocked on that entity existing, and the `throw` says
nothing about it.*** This is the largest single piece of unscoped work in the milestone and it appears
in no inventory, no blocker list and no risk statement.

### 🔴 Precondition 2 — **the Hinterland carries no price**

`[[hinterland]]` as shipped carries **`edge`, `emigrant_balance_min`, `emigrant_balance_max`** and
nothing else.

[`adr/0050`](../docs/adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)
is explicit that *"prices anchor to the Hinterland, which gains **a price per Good** alongside the rent
and wage it already carries — one authored object per map edge, **bounding three markets**."*
`CONTEXT.md` → Hinterland calls it *"**the one authored anchor under every price in the design**"*.

**It has not shipped.** [`0035`](0035-hinterlands-and-arrival-through-the-gate.md) decision **6a**
settled that only Hinterland fields **with a consumer** ship at 11, and at 11 a price had none. **12 is
the consumer.** This is the board's *"the anchor ships unread"* arriving one milestone later, exactly
as predicted — and it means **`04 §4`'s price ceiling, which is what makes `adr/0045`'s ladder
monotone, does not exist yet.**

⚠ **Without the ceiling the ladder is not merely incomplete, it is unordered.** The rungs are in their
order *"because each one costs more than the last"*, and nothing today establishes that.

### ✅ A correction, made on the day — the seller side **does** exist

`BinOwnerKind.Household` and `.Business` both carried the doc-comment **"Declared and not yet owned"**,
citing `plans/0033` tasks 5 and 4b. **Both are stale by two milestones.** `World.OpenBalance` opens a
balance Bin for each, at five call sites, and milestone 10 shipped it.

**Corrected in `Rules.cs` on 2026-08-21**, under
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md).
⚠ **It is worth more than a tidy-up, because of which way it was wrong.** `adr/0050`'s decisive
argument is *"a Business with no margin is not an economic actor"* — the Pool is a market precisely so
a Business can buy at a price, sell at a price, and have the difference be a margin. A scoping session
reading those two comments would have concluded **the counterparties do not exist yet** and deferred
the milestone. ***A stale comment that says a thing is missing costs more than one that says it is
present, because the first is a reason to stop.***

🔴 ⚠ **AND THE CORRECTION WAS HALF OF ONE — found 2026-08-22, on the commit read-through.** The same
claim sat a third time, in `BinOwnerKind`'s own `<remarks>` three lines above the members: *"declared
and throw by name where they would be resolved — `plans/0033` tasks 4b and 5 bring them"*. The members
were corrected and the block above them was not, so the file said **owned** and **throws by name**
about the same two values, four lines apart. Neither is a throw in the build:
`MoneyLedger.cs:128`/`:132` and `World.cs:1607`/`:1608` both resolve them. ***A correction applied to
the sentence somebody quoted leaves the sentence they will quote next***, and the enum's summary block
is the one a reader hits first. Corrected in place; the surviving named hole is `Scope.Pool`'s, which
is this milestone.

---

## Open decisions this milestone owes — **SEVEN OF NINE SETTLED, 3 AND 7 REMAIN**

Typed under `adr/0043`. **Settled: 1, 2, 4, 5, 6, 8, 9** — each closed in place, with the question as
first written kept beneath it as `Na`, because ***the original wording is how a later reader checks
whether the answer addressed the question asked.***

⚠ **This heading has now been wrong twice — *NONE SETTLED*, then *ALL NINE STILL OPEN*** — and both times
because a count sits at the top of a list that changes underneath it. ***A count is a fact that drifts***,
which is why `CLAUDE.md` tells you to count the ADRs rather than quote a total. **Read each entry.**

**What the two survivors actually need is not another sitting:**

- **3 is an obligation, not a fork.** `adr/0052` does not forbid choosing a hash-bearing number; it
  requires **naming what would ratify it**. The four District numbers get chosen, go to `plans/0002`
  **§D1** — *in use and unratified, which is the debt* — with the ratifier named. ⚠ `adr/0134` puts that
  ratifier at **milestone 15**, so the numbers are unratifiable now and that is **not** a reason to
  withhold them.
- **7 is largely pre-answered.** `adr/0134` makes the **road component constitutive** of a District, so a
  Building not on the component is not in the District, and *"subject to connectivity"* mostly falls out.
  What survives is the disconnected Building's own fate, which is a small question.

***So the next step after this document is task decomposition and not another argument***, and the first
task is `DistrictTable` and the watershed, which everything else in 12 resolves through.

### 1. ✅ SETTLED — a District is a **centre and its basin**, watershed over Building density

🔴 ⚠ **REWRITTEN 2026-08-22. The first draft of this decision asked the wrong question, and the corpus
had answered the one it asked.** It read *"What is a District, in the build?"* and offered player-drawn
against derived as though the fork were live. **It is closed, and closed twice over.** `02 §2.1`:
*"**Settled: both.** Automatic by default, player-adjustable as an advanced action."* `plans/0002`
item 6: *"**Closed by thread B — both.**"* `plans/0026` calls it discharged twice over. ***A decision
list is where an unsettled question goes, so putting a settled one in it re-opens it by filing***, and
this milestone would have spent a sitting re-deciding a design question two documents had closed.
⚠ **How it survived is itself filed** — [`0012`](0012-corpus-audit.md), 2026-08-22: `02`'s **own**
§11 open-questions list still carried the fork, unstruck, ending *"leaning player-drawn"*, while §2.1 of
the same document closed it. Both are struck now.

**What is therefore settled and is not this decision's to revisit:**

- **Automatic by default.** The derivation is the mechanism; the player arm is an override on top of it.
- **Derived from road topology and land use** — `02 §2.1`'s own words, and the tightest specification
  the corpus gives. It names **two** inputs, and a candidate satisfying one is not a candidate.
- **Contiguous sets of Cells, never Chunks** (`CONTEXT.md`, `02 §2.1`), so a profiler cannot move what a
  District *is*.
- **The count is physics**: the early city has one District because the city *is* one neighbourhood.
- 🔴 ~~**The player arm is a LATE-GAME ADVANCED ACTION**~~ **THERE IS NO PLAYER ARM — settled
  2026-08-22 by [`adr/0132`](../docs/adr/0132-the-district-is-derived-and-a-ward-is-what-the-player-draws.md),
  in the sitting this decision was written for.** The District is derived and the player cannot move its
  boundary at any milestone. The pen went to a **Ward**: a named set of Cells the player draws, which
  `Policy` is scoped to and which has **no logistics consequence**. ⚠ **This decision asked whether the
  arm shipped at 12 and the answer is that the arm does not exist**, which is a better outcome than the
  deliberate *no* `adr/0117` would have settled for. ***The pooling boundary's extent is physics and a
  Policy scope is nothing but a choice, and `02 §2.1` stated both four lines apart without reconciling
  them.*** The exploit it permitted — redraw one enormous District, switch off freight — was already in
  `adr/0013`'s trigger list.
- ⚠ **And the derivation is now the only way a District comes into being**, which raises this decision's
  stakes rather than lowering them: there is no player arm left to fall back on if the derived shape is
  unsatisfying. ***A fallback to the player is how the first weld was justified.***

✅ **THE ALGORITHM IS SETTLED, 2026-08-22 —
[`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md).
A District is a concentration of activity and the ground that drains to it**: a watershed over a
Building-density field on the Cell grid, clipped to a road component, seeded only where a concentration's
**prominence** clears a threshold. Stability comes from **persistence** on seeds, **hysteresis** on
membership and **damping** on the cadence — the last borrowing `04 §4`'s own argument for prices, that
*"an undamped price signal produces the same oscillation pathology as undamped congestion feedback."*

🔴 **The extent ceiling is GONE and that is the substantive half.** Nothing forces a split; a District
appears when a **second centre** does. ***No algorithm can find a meaningful boundary in a featureless
city, because there is not one to find*** — a monocentric city, which is every world this build can
generate, has no natural internal line, so a hard radius does not discover a boundary, it manufactures
one somewhere on a featureless ring. `adr/0013` requires the player be able to see a boundary and
understand what it means, and ***a straight line through a neighbourhood is a boundary no explanation
can be attached to.*** The bound moves to `adr/0133`'s haulage charge, which is **promoted from candidate
to structural** by that move — see decision 4.

⚠ **Consequence for this milestone, stated plainly: 12 ships ONE District on every current world**, and
`Scope.Pool` resolves through it. **Inter-District Shipments are not demonstrable** without a Ruleset
authoring two separated settlements, and `06` places Shipments at 12. ***That is milestone 9's land value
repeating*** — a producer built, correct and with nothing to look at. **A two-settlement Ruleset is the
cheapest fix and belongs in this milestone's task list.**

**The field the decision was taken against, kept because the rejections are the argument:**

| Candidate | Why it lost |
|---|---|
| **Split only where the road graph disconnects** | Effectively never splits — `RoadConnectivity` labels a connected city one component, and `DerivedRebuildAuditTests` says so: *"a city in one piece labels every live node `0`."* So a connected city is **one District for ever**, which is `adr/0013`'s explicitly rejected *pool everything, city-wide* wearing a derivation: *"it deletes geography with it… industrial siting stops mattering."* ⚠ **Proposed in this sitting and withdrawn in it**, after the user asked why we would want to lose inter-District shipping |
| **An anchored tiling of Cells, clipped to road components** | Stable by construction, cheap, and it would have given plural Districts at 12. **Its boundaries are straight lines that cut through neighbourhoods** — precisely the boundary `adr/0013` requires a player be able to understand. ***Shipping a mechanism known to be wrong, to buy an earlier demonstration of a different mechanism, is paying in the pillar to buy a milestone*** |
| **Growth seeded at Buildings, bounded by the radius, merging on contact** | Uses both inputs, and **the bound is what kills it**: forcing a large monocentric city to split puts the line somewhere on a featureless ring, and the merge makes membership flicker as the city grows |
| **Overlapping per-Building pooling balls** | Removes boundaries entirely, and with them every problem here. Refused on the corpus's own terms: **there is no Bin**, so *"the Pool is just a Bin per Good per District"* fails, `04 §4`'s per-District price has nothing to attach to, and `Scope.Pool` has nothing to resolve to |
| **⭐ A centre and its basin — watershed, prominence-seeded, unbounded** | **Chosen.** Boundaries fall where two concentrations meet, which is where a human would draw one. The count follows the city's structure. ⚠ **It buys that by giving up early Districts** — see the consequence above |

⚠ **`RoutingPartition` is not the answer and reusing it is a regression**, not a shortcut:
[`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md) detached the District from
travel-time matrix granularity on purpose. ⚠ **`02 §2.1` still said otherwise until 2026-08-22** — the
strike is recorded there and in [`0012`](0012-corpus-audit.md) — so ***the document a scoping reader
would open for the space hierarchy was, until this sitting, telling them to re-attach it.***

### 2. ✅ SETTLED — a District is a **saved entity**, `(saved AND hashed)`

**Settled 2026-08-22 as a consequence of decision 1, not on its own**
([`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)),
and it lands the **opposite** way round from this entry's expectation. The three mechanisms that keep the
boundary from flickering — **persistence** on seeds, **hysteresis** on membership, **damping** on the
cadence — all consult the previous extent, so extent is **not a pure function of a world snapshot** and
cannot be rebuilt on load. `DistrictTable` and `DistrictId` become real rows; a District is created and
destroyed like any entity; `DerivedRebuildAuditTests` does not apply to it.

⚠ **Determinism is unaffected** — extent is still a function of the Input Log, so replay and save/reload
equivalence both hold. ***What history-dependence costs is recomputability from a snapshot, which is
exactly what "saved" buys.*** ⚠ **The original entry's warning still bites, just elsewhere**: the
milestone-7 `car_park.segment_next` lesson is about a column declared `Derived` that nothing rebuilds, and
the defence here is that nothing is declared `Derived` at all.

### 3. What ratifies the District's extent? — *measurable*, and it needs a `plans/0002` §D row

`CONTEXT.md`'s **128 Cells** is labelled a *"working anchor… a starting point rather than a
derivation"*, with *"what actually pools convincingly is a playtesting question"*, and its ceiling
argument is that a District *"can only be as large as the area within which ignoring transport is a
defensible simplification."*

Under [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
a hash-bearing number is **chosen with a named ratifier or not at all**, and a ratifier must name a
machine, **a world** and **a quantity**. ⚠ **This number has none today and is about to become
hash-bearing.** It cannot arrive without a §D row.

🔴 **RESHAPED 2026-08-22 by [`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md),
and the obligation grew from one number to four.** The 128 Cells is **no longer a maximum**: nothing
forces a split, and a large District is bounded by `adr/0133`'s haulage charge rather than by geometry.
So the anchor becomes **the scale at which carriage starts to bite** — ***a curve's parameter rather
than a ceiling, which is a different obligation and not a softer one.*** Beside it arrive three more,
all hash-bearing, all unset, all owed §D2 rows: the **prominence threshold** that decides when a
concentration is a centre, the **hysteresis band** that decides when a Cell changes District, and the
**re-evaluation cadence with its per-evaluation Cell bound**.

⚠ **None of the four is tunable on a world that exists**, and that is the finding rather than an excuse:
the Building-density field the watershed reads is **flat on every shipped Ruleset**, so a threshold over
it has nothing to discriminate. They wait on a city with texture, which is milestone **15**'s
agglomeration. ***A number chosen against a flat field would be ratified by a world that cannot tell it
from any other value***, which is `adr/0052`'s requirement that a ratifier name **a world** doing exactly
the work it was written for.

### 4. ✅ SETTLED — the price is a damped tâtonnement against an authored anchor

**Settled 2026-08-22 with the user in the room** — [`adr/0135`](../docs/adr/0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md),
taken jointly with decision 8 because **8 exists so that 4 has something to price**. 12 authors
`[[hinterland]]`'s **price per Good**; the Pool price is **per Good per District, damped tâtonnement**
from Pool level against recent consumption, recomputed on a `Ticks.PerDay` boundary, **bounded above by
the Hinterland's price**. **No haulage term at 12** — a deliberate *no*, `adr/0133`'s payee being
unsolved and the collapse it guards being a scale risk one small District does not reach.

🔴 **Two of this entry's three sub-questions were answered by reading rather than by arguing.**

- **The anchor is EXOGENOUS, and the tempting inversion is refused.** *"Do Hinterland prices emerge from
  the city's?"* was raised in the sitting. **No** — `CONTEXT.md` calls the Hinterland *"the one authored
  anchor under every price in the design"*, and `adr/0050` gives the reason rather than the rule: *"an
  emergent price needs an anchor or it can run away."* ***A ceiling derived from what it bounds bounds
  nothing.*** ⚠ **But Hinterland prices DO move** — `04 §4` open question 7 is closed, drift's
  *"mechanism is settled; the tuning is not"*, and `01 §5` makes a shock *"a movement in a Hinterland's
  authored figures, and nothing else."* **Authored and dynamic, but exogenous.** The one real
  city→Hinterland feedback is the **population stock**, which is labour and not prices.
- 🔴 **THE MILESTONE-18 DEPENDENCY DOES NOT EXIST**, and this entry raised it as a live worry. A Day
  boundary is already computed without a wheel: `World.cs:1073` floor-divides by `Ticks.PerDay`,
  `CommuteEngine.cs:103` takes `tick.Raw % Ticks.PerDay`. 18's wheel is so that *many* Day countdowns
  share a structure; one recompute per Good needs none of it. ***A scheduling dependency was inferred
  from a cadence's units rather than read off a symbol***, which is `adr/0093` from the wrong end.
- **Authoring the price repairs a built mechanism rather than filling a gap.** `adr/0045`'s ladder is
  **running**, and `adr/0050` calls it *"a price ladder, monotone increasing"* whose ordering *"'the
  Outside Connection price is a ceiling' is exactly what guarantees"*. With no import price the shipped
  ladder is **unordered** — a live defect, not an absence. ⚠ **And the ceiling is `Hinterland price +
  haul`**, so ***haulage priced into a ceiling was already this corpus's principle and `adr/0133`
  extended it inward rather than introducing it.***

**The original entry, kept because its sub-questions are what the answers answer:**

### 4a. Where does a price come from at 12? — *the question as first written*

`04 §4` wants prices **per-District**, recomputed **each Day**, *"not set by the player and not
authored in the Ruleset."* `adr/0050` anchors them to the Hinterland's price per Good, which does not
exist. Sub-questions, and they are separable:

- Does 12 author `[[hinterland]]`'s **price per Good**? (It is the only consumer, so probably yes.)
- Does 12 build the **per-District recomputation**, or does every trade clear at the Hinterland
  ceiling until there is a market worth clearing?
- ⚠ A Day is **2048 Ticks** and the Day wheel is **milestone 18**, which a parallel session is
  building. *"Recomputed each Day"* may have a scheduling dependency on work in flight elsewhere.
- 🔴 **NEW 2026-08-22 — does the price carry a HAULAGE TERM?**
  [`adr/0133`](../docs/adr/0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md)
  settles that a Pool draw stops being free: intra-District movement stays unsimulated — no Vehicle, no
  routing query — and **gains a charge**, because `adr/0013`'s case was a *simulation-budget* argument
  about query volume and never a claim that carriage is worth nothing. **What is left is a discontinuity
  with no physical referent**: 100 m across a boundary is a Shipment, 1.45 km inside is nothing. ⚠ **The
  form and value are UNSET and the leading candidate scales the charge with the District's own extent**,
  which would make the extent bound self-enforcing and ***materially changes decision 3*** — a curve with
  a ratifier rather than a ceiling with one, which is a different §D obligation. 🔴 ⚠ **A payee is a
  blocker on shipping it**: a cost with no counterparty destroys money and `adr/0024` forbids that, so
  this is `adr/0117`'s *charge with no actor* arriving on a second mechanism. **Whether the charge ships
  at 12 is this decision's to answer**, and the deliberate *no* is worth more than the omission.

### 5. ✅ SETTLED — **no**, and Upkeep is UNPLACED rather than moved

**Settled 2026-08-22 with the user in the room** — [`adr/0136`](../docs/adr/0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md).
This entry said *"the default answer is no, and what this milestone owes is to say so deliberately rather
than by omission, and to move it."* **The first half stands and the second is refused**: moving it is what
put it here.

🔴 **`adr/0117`'s revisit trigger fired PREDICTIVELY.** It names *"milestone 12 shipping without
`Scope.Pool` resolvable from a Segment"*, and decisions 1, 2, 4 and 8 are settled with **no Rule attached
to a Segment** — so the placement is known wrong **before the milestone starts**. ***A trigger evaluable
against a scope rather than against a shipped build should be evaluated there.***

🔴 **Twice placed, twice moved, and both placements were made against ONE blocker while others were
open** — 10 against the balance sheet, 12 against the counterparty. ***Pinning it to 21 against the Lane
is the same move a third time***, and 21's position is itself marked provisional. **Three blockers land at
three times**: the construction-cost *quantity* needs the Lane (**21**); *design life* needs a **mature
city**; and **the actor** — a Rule whose subject is not its payer — has **no milestone at all**, which
is why this is an unplacing and not a re-placing.

✅ **Two grounds moved on the day, and both moved because of THIS milestone's other decisions.** Ground 1
is fully discharged — `Scope.Pool` is a market with a Provider and a moving price. **Ground 2's *money*
half is discharged**: it read *"no Ruleset key anywhere authors a cost of anything"*, and after
[`adr/0135`](../docs/adr/0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md)
none needs to, because `adr/0035` makes infrastructure money *"a transfer, not a sink"* and construction
buys Materials **at the market price**. ***The blocker was an authoring gap and the answer was a market,
not a key.***

✅ **And ground 3 is SETTLED here, because it depends on nothing unbuilt.** `adr/0117` left *"money or
Materials"* to whoever builds Upkeep. It is **neither** — `adr/0035` §3: *"the formulation exists to keep
the only authored number a **duration**."* A designer authors the **design life**; the Materials fall out
of the Segment; the money falls out of the price. Upkeep is a **purchase**. ***A ground answerable from a
document already cited had been carried three ADRs on the strength of the company it kept.***

**The original entry:**

### 5a. Does Upkeep ship at 12? — *the question as first written*

`adr/0117` says **12 is the earliest and not necessarily the right one**, and its own trigger list
includes *"milestone 12 shipping without `Scope.Pool` resolvable from a Segment — then the placement is
wrong and Upkeep moves again."* Grounds 2, 3 and 4 are all open. ⚠ **The default answer is no**, and
what this milestone owes is to say so deliberately rather than by omission, and to move it.

### 6. ✅ SETTLED — **no**, and freight goes with it

**Settled 2026-08-22 with the user in the room** — [`adr/0138`](../docs/adr/0138-freight-is-unbuilt-so-the-min-follows-it-and-neither-is-at-twelve.md).
The `min()` does not ship at 12, **Shipments do not ship at 12**, and the two are **pinned to each other
rather than to a number**: *the `min()` ships in the milestone that ships Shipments*, and that milestone
does not exist.

🔴 **`adr/0088`'s own test gives the same verdict at 12 that it gave at 11.** Its 2026-08-20 amendment
moved the term because the second operand is `RoadSegmentTable.CapacityPerDay` — **whole Vehicles per
Day** — and nothing crossed at 11: ***"a term that is vacuous on the world the milestone runs on is not a
diagnosis."*** **`Shipment` appears once in `Borough.Core`**, a doc-comment on `ResourceFamily.Good` at
`Ruleset.cs:225`, with no table, no engine and no Vehicle. So nothing crosses a Segment carrying cargo at
12 either. ***The date moved twice and the vacuity did not move at all***, because the amendment reasoned
*the second term follows freight to 12* — **an assumption about where freight was rather than a check on
it**, which is `adr/0136`'s finding about Upkeep arriving a second time in one day.

🔴 **This entry's stated dependency was MISROUTED, and that is the part worth keeping.** It said *"whether
freight itself is in this milestone is decision 8, so this one is downstream of it."* Decision 8 asked
whether a **Provider** ships and was answered **yes** — but a Provider is an intra-District seller and
intra-District movement is **pooled**, which `adr/0013` defines *in opposition to* a Shipment. ⚠ **So
decision 8 answers nothing about freight**, and ***a decision routed to the wrong upstream reads as
answered the moment the wrong upstream closes***: a later reader checking only whether 8 was done would
have marked this settled without ever asking about a Vehicle.

🔴 **`06` placed Shipments at 12 and this document never scoped them** — nine decisions, three
preconditions, no freight anywhere. Neither document contradicts itself; ***a survey looks for what its
author suspects is missing, and a mechanism placed by a different document is not a suspicion.*** `06`'s
row is now **unplaced**.

⚠ **What 12 must say out loud, and this is the cost of the answer rather than a footnote to it: import is
priced but NOT EMBODIED.** 12 makes import real as `adr/0045`'s rung 4 with `adr/0135`'s ceiling — and
with no freight it arrives with **no Vehicle and no congestion**. `adr/0088` withdrew `CONTEXT.md`'s *"at
the cost of longer hauls"* and put **your own traffic** in its place, so between 12 and freight **a
distant gate costs nothing at all**. ***The thesis is inert at the milestone that first makes imports
real***, accepted deliberately and for one milestone's reasons. **Do not read a 12-era city as evidence
that gate placement is free.**

**The alternative was taken seriously and refused on size**: 12 could ship gate-freight, but that is
Vehicles, Stress under `adr/0007` and Trip Fates, landing on a milestone already carrying the District,
the Provider, the tâtonnement and a new Ruleset — and it would be **gate-only**, because `adr/0134` gives
one District per current world, so *between Districts* has nowhere to go.

**The original entry:**

### 6a. Does the `adr/0088` `min()` ship here? — *the question as first written*

`min(declared ceiling, Segment capacity)` plus the **which-of-the-two-binds** readout, relocated from
11 by [`0035`](0035-hinterlands-and-arrival-through-the-gate.md) decision 9 on the ground that *"the
second term follows freight to 12."* ⚠ **Whether freight itself is in this milestone is decision 8**,
so this one is downstream of it.

### 7. What does *"subject to connectivity"* mean for a Pool draw? — *arguable*

`CONTEXT.md` → District Pool: *"Goods moving between Buildings within a District pass through the Pool
instantly, **subject to connectivity**."* `Scope.Pool`'s doc says the scope *"requires road
connectivity"*. `RoadConnectivity.cs` exists. **What has never been said is what a disconnected
Building does** — fail on the Pool Bin, be outside the Pool entirely, or be in a District of one.

### 8. ✅ SETTLED — **yes**, a Provider ships at 12

**Settled 2026-08-22 with the user in the room** ([`adr/0135`](../docs/adr/0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md)):
*we need to build a two-sided market.* A Provider draws inputs from the Pool and sells its output into
it. 🔴 **The decisive argument is that one side is not a weak market but an UNDEFINED one** — with no
seller the Pool holds no stock and consumes nothing, so a price from *"Pool level against recent
consumption"* is computed from **two zeroes**. ***That is a stronger failure than the unobservability
this milestone has already accepted twice***: land value and inter-District Shipments are built, correct
and with nothing to look at; a one-sided market cannot produce a number that means anything.

🔴 ⚠ **It costs three CONTENT decisions, and `rulesets/minimal.toml`'s own header enumerated them before
anyone asked**: *"A second kind needs **a second `[[zone_rule]]`** to raise it, **a second decline Rule**
so that it churns rather than accumulating until the city is all offices, and **a land-use split**."*
⚠ **That header's first line is that the file does not make content decisions**, so the Provider does not
go in `minimal.toml` — it needs a Ruleset of its own, **which is the same file this milestone's
Definition of done already owes for the two-settlement demonstration.** ***The cheapest reading of this
milestone was one that never counted the Ruleset.***

**The original entry:**

### 8a. Does a second `[[building]]` kind ship? — *the question as first written*

`06`'s obligations table places *"a Ruleset that models a city"* here and says **12 is the first row
whose mechanism cannot run on a single-kind Ruleset** — *a shopping occasion needs somewhere to shop*.
⚠ **Every shipped Ruleset's emitting kind is `dwelling`**, and `rulesets/crowded.toml`'s header already
records why: industrial demand is **unbuilt**, so the only demand signal that exists is demand for
homes. A market with one kind of participant is a market in name.

### 9. ✅ SETTLED — **yes, something needs building**: one field, and one requirement on the purchase

**Settled 2026-08-22 with the user in the room** — [`adr/0137`](../docs/adr/0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md).
**This entry's suspicion was right and the reason was worse than expected.** `adr/0050`'s *"falls out of
the wait list rather than needing a mechanism"* is **true of the wait list and false of the build**.

🔴 **Read off the symbols.** `RuleInstanceTable` carries **`WaitingOn`** — the Bin that stopped the Rule
— and `RuleEngine.Stop` writes it. **`RuleEvidence` does not carry it**: it is `RuleId`, `LastRan`,
`Succeeded`, `Blocking Blocked`, `ConditionId Reported`, `StarvedSince`, `Rate`, `MissedFirings`. And
`Blocking` is only `Nothing / Supply / Space`, so **both cases are `Supply`**. A Business short of flour
and a Business short of money are **indistinguishable to every reader**. ***The wait list is not a reader;
`Evidence` is.***

⚠ **It is milestone 11 task 8's defect one subsystem over** — *"a flow that reaches no instrument is a
flow nobody can read"* — and **two instances make it a shape rather than an incident.**

**What ships, both with the purchase rather than as separate tasks:**

1. **`RuleEvidence` gains the blocking Bin** (or its `ResourceId`). Cold path, so the record struct may
   grow. The **shell** classifies by the Resource's **family** through the Ruleset, keeping `Core`
   returning ids and never strings.
2. 🔴 **The purchase's money check must produce a verdict NAMING the money Bin.** Under `adr/0050` a
   purchase has **no money term**, and the wait list keys on a Bin named by a term — so by default the
   money leg has nothing to subscribe to, and the cheapest implementation returns *insufficient funds* and
   subscribes to nothing. ***A distinction `Evidence` cannot recover is one no `Evidence` change fixes.***
   It costs nothing extra; it has to be **written down**, because nothing about the money leg is authored
   and so nothing about it is prompted.

⚠ **Still the cheapest decision in this milestone.** The correction does not make it expensive — it makes
it *exist*. Filed to [`0012`](0012-corpus-audit.md), and `adr/0050` carries a banner.

**The original entry:**

### 9a. How is bankruptcy told from starvation? — *the question as first written*

`adr/0050` claims the distinction *"falls out of the wait list rather than needing a mechanism"* —
Pool Bin short is **input starvation**, money Bin short is **bankruptcy**, two Bins and two blame
targets. ⚠ **That is a claim about the build and it should be read off `Evidence` before being
believed**, which is `adr/0093`. It is the cheapest decision here and the one most likely to be
assumed.

---

## What this milestone must not do

- ⚠ **Must not implement `Scope.Pool` as a wider Bin lookup.** It ships an unconserved economy, it is
  the failure mode the refusal was written to prevent, and **no test in this repository would catch
  it**. If the market cannot be built here, the `throw` stays and the milestone shrinks.
- **Must not author a price in a Rule.** `adr/0050`: there is no syntax for the payment, the price is
  emergent, the quantity is the term's `amount`, and *"offering syntax would only offer a way to get it
  wrong."*
- **Must not add a demand scalar.** The Unplaced Pool is the demand signal.
- **Must not size a District from a profiler.** Extent decides pooling, which is a change to the city
  and not an optimisation.
- **Must not let Upkeep in by the back door** because the counterparty arrived. Three of its four
  grounds are open.

---

## Definition of done

`CLAUDE.md`'s cumulative list, plus:

- `02 §4.3`'s **own bakery example loads and runs** — the corpus's worked example stops being
  unloadable, which is the named risk stated as an artefact.
- A Ruleset in `rulesets/` demonstrating a chain that **crosses the ownership boundary**, with its
  header saying what it exists to show.
- 🔴 **A Ruleset authoring TWO SEPARATED SETTLEMENTS, so a second District exists and one real
  inter-District Shipment happens** — added 2026-08-22 by
  [`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md).
  ⚠ **Without it this milestone ships one District on every world and `06`'s Shipments row has nothing to
  show**, which is milestone 9's land value repeating. ***The count follows centres, so a world with one
  centre is not a world that can demonstrate the mechanism*** — and no amount of running the existing
  Rulesets longer produces a second one.
- **Conservation holds across a trade**, asserted rather than argued: Goods and money both, over a long
  run, with `adr/0024`'s equality exact.
- **Bankruptcy and starvation are distinguishable** in `Evidence` on a world that produces both.
- There is something to *look at* — a runner mode showing a Pool with stock, a price, and a Building
  that could not afford it.

---

## What scoping found

*(Filled as the sitting runs. The three preconditions above were found before it started, which is why
they are in the survey rather than here.)*
