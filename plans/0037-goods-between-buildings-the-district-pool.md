# 0037 — Goods between Buildings: the District Pool

**`06` milestone 12.** Ungated. The milestone that makes `Scope.Pool` resolve, and with it the first
Goods chain that crosses an ownership boundary.

---

## Status

🟢 **SCOPING STARTED 2026-08-21. The scoping sitting ran 2026-08-22, settled decisions 1, 2, 4, 5, 6, 8
and 9, and the milestone was DECOMPOSED INTO TEN TASKS the same day. TASKS 1 AND 2 SHIPPED
2026-08-22** — `rulesets/twinned.toml` and `[[lattice]]`, the first world this build can generate with
**two centres** in it; and the Building-density field, which 🔴 **turned out to be already built**
(`BuildingResidency`, 5b-bis) so that task 2 was a name and a measurement rather than storage.
**TASK 3 SHIPPED 2026-08-22** — `DistrictTable`, `DistrictCellTable`, `DistrictResidency` and
`Space.DistrictWatershed`: a two-descent persistence watershed over the density field, clipped to the
**Foot** road component, run once from `SyntheticCity.PopulateInto`. ⚠ **Nothing reads a District yet** —
`Scope.Pool` still throws, and the Pool Bins are task 5. **Findings F1–F17** are below, under *What
building it found*.

⚠ **Open: 3, 7 and 10** — and **10 did not exist until decomposition wrote it**, which is the argument
for decomposing rather than sitting again. ***Ordering the work asked what each task needed and found a
question seven decisions had not***: the Pool is the counterparty on both sides of a trade, the two
sides happen at different Ticks, and nothing says where the money is in between.

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

## Open decisions this milestone owes — **OPEN: 3 AND 10**

Typed under `adr/0043`. **Settled: 1, 2, 4, 5, 6, 8, 9** — each closed in place, with the question as
first written kept beneath it as `Na`, because ***the original wording is how a later reader checks
whether the answer addressed the question asked.*** ⚠ **10 is new**, found by decomposition on
2026-08-22 rather than by a sitting.

⚠ **This heading has now been wrong three times — *NONE SETTLED*, *ALL NINE STILL OPEN*, and *SEVEN OF
NINE*, the last of which went stale within the hour when decomposition added a tenth decision.** Each
time because a count sits at the top of a list that changes underneath it. ***A count is a fact that
drifts***, which is why `CLAUDE.md` tells you to count the ADRs rather than quote a total. **The heading
now names the open ones instead**, which is the thing a reader actually wants and does not go stale
silently — a settled decision leaving it is a visible edit. **Read each entry.**

**What the two survivors actually need is not another sitting:**

- **3 is an obligation, not a fork.** `adr/0052` does not forbid choosing a hash-bearing number; it
  requires **naming what would ratify it**. The four District numbers get chosen, go to `plans/0002`
  **§D1** — *in use and unratified, which is the debt* — with the ratifier named. ⚠ `adr/0134` puts that
  ratifier at **milestone 15**, so the numbers are unratifiable now and that is **not** a reason to
  withhold them.
- **7 is largely pre-answered.** `adr/0134` makes the **road component constitutive** of a District, so a
  Building not on the component is not in the District, and *"subject to connectivity"* mostly falls out.
  What survives is the disconnected Building's own fate, which is a small question.

***So the next step after this document is task decomposition and not another argument.*** ✅ **DONE
2026-08-22 — see *Tasks* below, ten of them.** ⚠ **It changed the first task**: this paragraph said
`DistrictTable` and the watershed, and the world with two settlements in it has to come **first**,
because on every world this build can currently generate the derivation produces one District and is
untestable. ***That is milestone 11 task 3's lesson arriving before the milestone rather than during
it.***

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

### 7. ✅ SETTLED — a Building's District is its **Cell's** District, and the clip is the whole answer

**Settled 2026-08-22 with the user in the room**, before task 5. *"Subject to connectivity"* is
**already spent**: `adr/0134`'s watershed clips the flood to the **Foot** road component, so every Cell
in a District is foot-reachable from its centre and ***a District is connected by construction.*** There
is nothing left for a Pool draw to check, and `DistrictResidency.Of` — a Building's Cell, then the index
— is the whole lookup.

⚠ **The question survives as one about GRANULARITY, and the answer is the Cell.** A Cell's component is
read off the first Building in it that has an Address, so a Building with **no** Address rides on its
neighbours' reachability. 🔴 **That is a real state and a designed one**: `adr/0079` keeps a Building
standing when its last Street is bulldozed, so its Lot has no frontage and nobody can reach it — and it
keeps trading with the Pool. **Accepted**, because the alternative costs a per-Building check on the
hot path to correct a 128 m discrepancy inside one Cell, and because the two candidates that would act
on it are both worse: *fail on the Pool Bin* livelocks against the wait list, and *a District of one*
does not fit a membership table keyed by Cell.

**The original entry:**

### 7a. What does *"subject to connectivity"* mean for a Pool draw? — *the question as first written*

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

### 10. 🔴 NEW, found in decomposition 2026-08-22 — **what holds the Pool's money, and may it go negative?** — *arguable*

**Found by reading `BinTable` and `BinOwnerKind` while ordering the tasks, and it blocks task 7.** It is
not a ninth decision rediscovered; **nothing in this document or in `adr/0050`, `adr/0135` or `adr/0114`
asks it.**

🔴 **The Pool is the counterparty on both sides, and the two sides happen at different Ticks.**
`adr/0135`: a Provider *"draws inputs from the District Pool and **sells its output into it**."*
`adr/0050`: *"the counterparty is already implied by the scope"*, and *"every case reachable today —
local, Pool, import — has exactly **one** counterparty."* So a Provider is paid **by the Pool** on
deposit and a consumer pays **the Pool** on draw — and a deposit at Tick 100 against a draw at Tick 500
means ***the Pool holds money in between.*** Under
[`adr/0024`](../docs/adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) that money
cannot be nowhere.

⚠ **And there is no matching alternative available.** Paying the Provider directly at the moment of the
draw would need to know *whose* units were drawn, and a Bin is fungible by construction — `04 §4`'s
*"the Pool is just a Bin per Good per District"* is exactly the statement that units carry no
provenance.

**The sub-questions, and they are small:**

1. **Does the District get a money Bin?** It is the obvious answer and it costs a **fifth
   `BinOwnerKind`** doing double duty — Goods *and* money — plus a case in `MoneyLedger`'s switch
   (`MoneyLedger.cs:122`), which today resolves `Treasury / Household / Business`.
2. **May it go negative, and what happens when it cannot pay a Provider?** A Pool that buys before it
   sells is a market-maker carrying inventory risk. ⚠ **A negative money Bin is a new thing in this
   build** — and *the Pool cannot pay* is a **third** failure to tell apart from starvation and
   bankruptcy, which lands directly on
   [`adr/0137`](../docs/adr/0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md).
3. **Or is settlement deferred — the Provider paid on draw out of the draw's own payment**, with unsold
   stock simply unpaid? That keeps money conserved with no Pool balance at all, and it makes a
   Provider's revenue depend on somebody buying, which is arguably the more honest economics.

⚠ **Front-runner: (3), and it is not obviously right.** It needs no fifth owner kind, no negative
balance and no `MoneyLedger` case, and *a Provider is paid when its output sells* is a sentence a player
can be told. What it costs is that the Pool must remember **how much it owes per Good**, which is a
column, and that a Provider's deposit becomes a **consignment** rather than a sale — a word `CONTEXT.md`
does not have.

⚠ **This is typed *arguable* and therefore wants the user in the room**, but it is one question with
three named candidates and it blocks exactly one task. ***It should be taken at the head of task 7 and
not in a sitting of its own***, which is what `adr/0043` permits: no measurement settles it, and the
corpus has no prior answer to look up.

---

---

## Tasks

**Decomposed 2026-08-22, after seven of nine decisions closed.** Ordered by what the next task needs.
⚠ **Three entries are blocked on something other than code.** Task 5 on **decision 7** and on
~~🔴 **[`plans/0003`](0003-build-plan.md) queue item 14**~~ — ✅ **settled 2026-08-22 in a commit of its
own, as its own entry required.** The invariant narrowed to the head of the wait list on
[`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)'s
own argument, and it moves no hash. Task 7 on **decision 10**, which decomposition itself found.
⚠ **Decisions 7 and 10 are small and neither wants a sitting.**

⚠ **Read the ordering claim before trusting the order.** Tasks 1 to 4 build **a boundary**, and tasks 5
to 8 build **a market inside it**. ***The market half is the milestone's named risk and the boundary half
is its precondition***, so an eye on schedule should be on tasks 1–4 finishing rather than on 5–8
starting.

1. ✅ **SHIPPED 2026-08-22 — a world with two settlements in it** — the generator places two separated
   lattices, and a Ruleset authors them. **`rulesets/twinned.toml`**, `[[lattice]]`, and the two are
   **joined by a Street corridor**: `adr/0134` rejected splitting on road components, so a world in two
   components would let component labelling pass for a watershed. ⚠ **The key is `[[lattice]]` and not
   `[[settlement]]`** — see **F2**. Findings **F1**–**F6** below.
   ⚠ **This is FIRST and not last, and milestone 11 task 3 is why**: *"`SyntheticCity`
   places gates, so there is a world with a door in it — milestone 9's **F17** is why this is a task and
   not an assumption."* ***The derivation is unobservable and untestable on every world this build can
   currently generate***, because the count follows centres and there is one. **Make the gap
   unambiguous** — the prominence threshold is not chosen until task 3, so the world must be one that any
   sane threshold splits, not one that calibrates it.
2. ✅ **SHIPPED 2026-08-22 — the Building-density field on the Cell grid** — what the watershed reads
   ([`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)).
   🔴 **IT WAS ALREADY BUILT.** `BuildingResidency` has cached its own per-Cell list lengths since
   **5b-bis**, sized against `CellGrid.WorldCellCount` exactly as this entry predicted whatever held it
   would be, because the job search needed a fair draw over a box. **What task 2 added is a name** —
   `BuildingResidency.Density` — **and the evidence that the field has the shape the watershed needs.**
   ⚠ **It is flat on all nine existing Rulesets and has texture on task 1's world**, which is the whole
   reason for the order — and *flat* turned out to be **literal**. Findings **F7**–**F10** below.
   ⚠ **No smoothing**, and that is a decision against a measurement rather than an omission (**F8**).
3. ✅ **SHIPPED 2026-08-22 — `DistrictTable` and the first derivation** — the watershed,
   prominence-seeded, clipped to a road component. `Space.DistrictWatershed` is the operator;
   `DistrictTable` holds a centre apiece and `DistrictCellTable` the extent, one row per built Cell;
   `DistrictResidency` is the dense Cell→slot index. `(saved AND hashed)` and **not** `Derived`, per
   decision 2 — so `DerivedRebuildAuditTests` does not apply and nothing is owed a rebuild. **Runs once,
   from `SyntheticCity.PopulateInto`** (**F15** — not from the command that calls it): no re-evaluation
   yet, therefore no persistence, hysteresis or damping, which is task 4.
   ⚠ **`DistrictId` did not ship and nothing is missing** — the identity is `Handle<District>`, which is
   the project's only one (**F11**).
   ⚠ **ONE of the four numbers was chosen here and one §D1 row was owed**, not four: the prominence
   threshold, `[districts] prominence_percent = 50` in `rulesets/twinned.toml`. The hysteresis band and
   the cadence-with-its-Cell-bound belong to task 4 and stay §D2 gaps; the haulage curve's extent scale
   belongs to task 7. **The row is written and names milestone 15** — `adr/0052`, and decision 3.
   🔴 **The threshold is not consulted on any world that exists and the tests say so out loud** (**F17**).
   ⚠ **The extent is BUILT CELLS ONLY** — empty ground is in no District, because it holds no Building,
   no Bin and nothing to pool. ⚠ **The clip reads the FOOT subgraph**, which is the weaker of the two,
   and **no shipped Ruleset exercises it** — `twinned.toml` is deliberately one component (**F1**), so
   it is held by an in-code fixture with a cut corridor.
   🔴 **The State Hash moved**, and every golden trace and the world baseline were regenerated with it
   ([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)).
   ⚠ **`RoutingPartition` is not this and reusing it is a regression** ([`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md)).
   Findings **F11**–**F17** below.
4. ✅ **SHIPPED 2026-08-22 — re-evaluation: persistence, hysteresis, damping and the cadence.**
   ***This is the task that earned decision 2's answer***: all three consult the previous extent, which
   is precisely why a District is saved rather than rebuilt. `[districts]` goes from one key to four —
   `revisit_ticks`, `hysteresis_percent`, `migrate_cells` — and `DistrictWatershed.Evaluate`
   **reconciles rather than replaces**, on one path with the first evaluation as its degenerate case.
   ⚠ **Identity travels through the CENTRE Cell**, first claimant in Cell-index order; a District no
   basin claims is **destroyed**, which at task 5 becomes `adr/0024`'s problem and is deliberately left
   to it. The cadence runs **last in phase 6**, after the Zone Rules, because that is the line after
   which the Building count is settled for the Tick. Findings **F19**–**F23** below.
   🔴 ⚠ **THIS ENTRY WAS WRONG ABOUT THE CELL BOUND AND THE STRUCK WORDS ARE THE CORRECTION.**
   ~~*it is a work bound*~~ — it is not.
   [`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)
   and [`0002`](0002-open-questions.md) §D2 both say it bounds **how far a boundary may move per
   evaluation**, and they say it in the same words: *"a boundary migrates by at most a bounded number
   of Cells per evaluation, so it never jumps."* ***A work bound and a change bound are different
   numbers and would have been different code***, and this entry's own next clause — *"must not size a
   District from a profiler"* — is the argument against the reading it had just given. Filed in
   [`0012`](0012-corpus-audit.md). ⚠ **Only a Cell moving from one LIVING District to another counts
   against it**: new ground is growth, and a Cell whose District is being destroyed has nowhere to stay.
5. ✅ **SHIPPED 2026-08-22 — Pool Bins, a Bin per Good per District.** `BinOwnerKind.District` is the
   **fifth** member; `Space.DistrictPoolTable` is a saved join naming which Bin belongs to whose Pool,
   because `BinTable.Owner` cannot hold a District and the alternative — a derived list — is only
   derivable when the element names its owner (**F25**). Every Pool Bin is **unbounded**, on an argument
   that is not money's: there is no shed, and `adr/0135`'s falling price is what discourages selling
   into a full Pool. `World.FitDistrictPools` adds and never removes, after the watershed and at every
   Ruleset swap. A dying District's stock goes to whoever took its **centre Cell**, which is identity's
   own rule used for succession (**F29**). ⚠ **No money Bin**, because decision 10 is open and a Bin
   opened before it is answered would *be* the answer. ⚠ **No lookup index, and that is deliberate**
   (**F26**) — every read is cold while `Scope.Pool` throws. 🔴 **The State Hash moved and all three
   golden baselines were re-recorded**: `DistrictPools.Rows` is appended to `World._tables`, and an
   empty table still folds its allocator ([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)).
   🔴 **Task 5 also found a defect it does not own** — a Ruleset that inserts a `[[resource]]` crashes
   the swap on the treasury — routed to [`0003`](0003-build-plan.md) queue item **15** (**F27**).
   ⚠ **`Scope.Pool` STILL THROWS**, exactly as this entry said it would. *The original entry:*
   🔴 **`BinOwnerKind` has four members and none is a
   District**, and `BinTable.Owner` is a `HandleColumn<Building>` bound to `buildings.Rows` at
   construction (`BinTable.cs:60`), so ***a District-owned Bin cannot address its owner through
   `Owner` and must not try.*** **Use the Household/Business shape instead** — the owner row holds the
   Bin handles and the Bin leaves `Owner` unset, which is what `BinTable.Create(BinOwnerKind, …)` already
   exists for and what its doc-comment already explains. A fifth `BinOwnerKind` is needed; widening
   `Owner` is not. ⚠ **`Scope.Pool` STILL THROWS after this task.** A Bin without a settled purchase is
   the wider Bin lookup the milestone must not ship, and the `throw` is the thing preventing it.
   **Blocked on decision 7** — what a disconnected Building does — which `adr/0134` largely pre-answers
   by making the road component constitutive.
   ~~🔴 ⚠ **AND BLOCKED ON [`plans/0003`](0003-build-plan.md) QUEUE ITEM 14, which says so itself and which
   this document did not list.**~~ ✅ **SETTLED 2026-08-22, ahead of this task and in its own commit.**
   ⚠ **The fork read as symmetric here and was not**: making the drain skip is the behaviour `adr/0063`'s
   *Atomic servings* section refuses **by name**, so ***different cities* overstated the choice** — one
   candidate was already refused by the ADR that owns the mechanism, and the sitting's work was finding
   that out rather than choosing. **The invariant narrowed to the head of the list and moves no hash**,
   and the narrowing turned up a second half nobody had reasoned to: a woken waiter records no claim
   anywhere, so the drain's guarantee is true of an *instant* and `RuleEngine.AccumulateClaims` derives
   the difference. *The original entry, kept because the blocker was real:* `World.Drain` and `Invariant.WaiterIsBlockedByTheBinItNames` contradict
   each other today, and a committed test asserts the state the invariant calls a violation. It is
   **latent only because no shipped Ruleset puts two Rules on one Bin** — `BinWaitListTests`' own header:
   *"under `local` scope no two Rules share a Bin they do not both own."* ***`Scope.Pool` is precisely the
   mechanism that ends that***, so this task is what makes a committed contradiction reachable.
   **Its own entry says it must be settled before the Pool ships, not after**, and that the repair is a
   design question that must not be taken inside another item's commit: either the invariant narrows to
   the drain's guarantee, or the drain stops starving small waiters, and ***those are different cities.***
   ⚠ **A fourth precondition, and it was sitting in a ledger that names this milestone by number** —
   which is decision 6's finding pointing the other way: *a mechanism placed by a different document is
   not a suspicion*, and neither is a blocker filed in one.
6. **The price — `[[hinterland]] price` per Good, and the tâtonnement**
   ([`adr/0135`](../docs/adr/0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md)).
   Per Good per District, damped, from Pool level against recent consumption, on a `Ticks.PerDay`
   boundary, **bounded above by the Hinterland's price**. ⚠ **No milestone-18 dependency and no wheel** —
   `World.cs:1073` already floor-divides by `Ticks.PerDay`. ⚠ **Before the purchase, because a purchase
   settles at a price**; and authoring the import price **repairs `adr/0045`'s running ladder**, which is
   unordered without it, rather than filling a gap.
7. **The purchase — and `Scope.Pool` stops throwing.** Good one way, money the other, settled atomically
   with the Rule. 🔴 **Blocked on decision 10**, above: *who holds the Pool's money between a Provider's
   deposit and a consumer's draw.* ⚠ **The engine's term resolution is 1:1 and a purchase is 1:2** —
   `RuleEngine.Bin` returns **one** slot (`RuleEngine.cs:801`) and a Rule waits on **the one Bin it was
   short of**, so the money leg needs a Bin to subscribe to that no term names. **Both halves of decision
   9 land here** ([`adr/0137`](../docs/adr/0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md)):
   `RuleEvidence` gains the blocking Bin, **and** the money check produces a verdict **naming the money
   Bin** rather than failing without subscribing. ***The second half is the one that gets skipped***,
   because nothing about the money leg is authored and so nothing about it is prompted.
8. **The Provider kind and the market Ruleset** — a second `[[building]]` kind that draws inputs from the
   Pool and sells its output into it. 🔴 **It costs three CONTENT decisions and `rulesets/minimal.toml`'s
   own header enumerated them before anyone asked**: a **second `[[zone_rule]]`** to raise it, a **second
   decline Rule** so the city does not fill with offices, and a **land-use split**. ⚠ **Not in
   `minimal.toml`** — that file's first header line is that it makes no content decisions.
   ✅ **`02 §4.3`'s bakery loads and runs here**, which is the named risk stated as an artefact.
9. **Something to look at** — a runner mode showing a Pool with stock, a price that moves, and **a
   Building that could not afford it**. ⚠ **The third clause is the one that would be dropped**, and it
   is the only one that shows the market having a consequence.
10. **The long acceptance run** — conservation across trades with `adr/0024`'s equality **exact**, no
    collection or magnitude trending at steady state (`adr/0006`), and **bankruptcy distinguishable from
    starvation in `Evidence` on a world that produces both**. ⚠ **That last clause needs a world where a
    Building genuinely runs out of money**, and no existing Ruleset produces one — ***milestone 11's F25
    was exactly this***: task 9 was specified against a world that did not exist and task 8 had to ship
    it. **Check for it at task 8 rather than at task 10.**

**Struck by the decisions, and listed so nobody re-adds them**: Shipments and `adr/0088`'s `min()`
(unplaced — `adr/0138`), Upkeep (unplaced — `adr/0136`), a haulage term on a Pool draw (`adr/0135`, a
deliberate *no* at 12), any player-drawn District boundary (`adr/0132` — the pen went to a **Ward**, which
has no milestone), and a District extent ceiling (`adr/0134` — nothing forces a split).

⚠ **What this list does not contain, stated because its absence is a decision**: freight. So **import is
priced and not embodied**, and a distant gate costs nothing at 12 — see decision 6.

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
- **Must not let Upkeep in by the back door** because the counterparty arrived. ⚠ ~~Three of its four
  grounds are open.~~ **Upkeep is UNPLACED**
  ([`adr/0136`](../docs/adr/0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md))
  — read its blocker list there rather than a count here, because ***a count in prose is a fact that
  drifts*** and this one already has: the counterparty ground and ground 3 both closed on 2026-08-22
  while the sentence still said three were open.

---

## Definition of done

`CLAUDE.md`'s cumulative list, plus:

- `02 §4.3`'s **own bakery example loads and runs** — the corpus's worked example stops being
  unloadable, which is the named risk stated as an artefact.
- A Ruleset in `rulesets/` demonstrating a chain that **crosses the ownership boundary**, with its
  header saying what it exists to show.
- 🔴 **A Ruleset authoring TWO SEPARATED SETTLEMENTS, so a SECOND DISTRICT EXISTS** — added 2026-08-22 by
  [`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md).
  ⚠ ~~*and one real inter-District Shipment happens*~~ **STRUCK the same day by
  [`adr/0138`](../docs/adr/0138-freight-is-unbuilt-so-the-min-follows-it-and-neither-is-at-twelve.md)**:
  Shipments are **unplaced**, so this bullet asked for a demonstration of a mechanism the milestone no
  longer ships. ***A Definition of done written against another document's placement inherits that
  placement's mistakes***, and this one was written and invalidated within the same sitting.
  **What survives is the whole reason the bullet exists**: without two settlements this milestone ships
  **one District on every world**, and a boundary is the thing a District *is*. ***The count follows
  centres, so a world with one centre cannot demonstrate the mechanism*** — and no amount of running the
  existing Rulesets longer produces a second one.
- **Conservation holds across a trade**, asserted rather than argued: Goods and money both, over a long
  run, with `adr/0024`'s equality exact.
- **Bankruptcy and starvation are distinguishable** in `Evidence` on a world that produces both.
- There is something to *look at* — a runner mode showing a Pool with stock, a price, and a Building
  that could not afford it.

---

## What scoping found

*(Filled as the sitting runs. The three preconditions above were found before it started, which is why
they are in the survey rather than here.)*

---

## What building it found

### Task 1, 2026-08-22 — **F1** to **F6**

**F1 — the two lattices had to be JOINED, and the alternative would have shipped the rejected
mechanism under the chosen one's name.** A world in two road components splits under
`RoadConnectivity` alone, so task 3 could implement component labelling, pass every test this task
writes, and be `adr/0134`'s explicitly refused *split only where the road graph disconnects*. The
generator therefore lays a **corridor of Street Segments** between consecutive Lattices — Street and
not Arterial, because an Arterial ramp carries cars and not feet, which would make the world one
component for driving and two for walking and hand task 3 a fork about which one the clip reads.
**Measured on `twinned.toml` at 1,000 Citizens: one component in each mode, 109 of 109 nodes.**
***The only thing that can find this world's boundary is the density field, which is the thing under
test.***

**F2 — the key is `[[lattice]]`, and `[[settlement]]` would have authored a contradiction.**
`CONTEXT.md` → Settlement is a **derived** commute shed, and *"connectivity is transitive, so a
contiguously-developed lattice is one Settlement however large the graph"*. 🔴 **Whether these two
Lattices are one Settlement or two is decided by a key in a different table**: over the corridor's
7,680 m, ~9 clock minutes by car and ~92 on foot against a 50-minute ceiling, and `twinned.toml` states
no `[households]`, so nobody drives. ***A term naming a derived thing cannot be borrowed for an authored
one, because the derivation may depend on something the author did not write.*** `CONTEXT.md` gains a
**Lattice** entry; the loose prose in this document and in `adr/0134` is filed in
[`0012`](0012-corpus-audit.md). ⚠ **It was caught by opening `CONTEXT.md` before writing the key and
would not have been caught after** — the vocabulary rule works by being upstream of the code.

**F3 — a Lattice's Lots do not stop at its extent, because a Segment has two sides.** ⚠ **MEASURED, not
reasoned.** Restricting subdivision to each Lattice's own block box is necessary — a map-wide walk would
carve Lots along the corridor and fill the saddle — and a box of exactly *n* blocks **moved every golden
trace**. The block sitting beyond a Lattice's east edge still has that edge's Node column of vertical
Segments as its **west face**, and a face is all `SubdivideBlock` needs. `GoldenSessionCoverageTests`
named it exactly: *"carved 118 Lots where 117 were expected"*. The box is **n + 1** blocks.
***A generator's output does not end where its input does.***

**F4 — the overlap refusal has to be drawn where the LAND contends, not where the roads do.** A Lattice
paves what its share needs, so a large enough city makes two of them collide, and the generator refuses
rather than laying one over the other. Drawn at the Nodes, the refusal left a band in which the two
Lattices' **Lots** contended while their roads did not: measured at **344,000** Citizens on
`twinned.toml` the split came out **20,545 / 20,736** where the construction says 20,641 / 20,640 — the
first Lattice walked the shared blocks and took what the second needed. ***A world going quietly
lopsided is worse than one that throws***, so the test widened by one block, which is F3's block.

**F5 — `twinned.toml` has a measured population ceiling and it is loud.** **341,000 Citizens lays;
342,000 throws.** Above it the answer is to move the origins apart — the map is 16,384 Tiles a side and
this file uses 2,048 of it. ⚠ **Stated in the file's header** rather than left for somebody to discover
at 400,000.

**F6 — Arterials are laid per Lattice, so the combination is refused at load.** A file saying
*`arterial_count = 4`* with two Lattices would mean *four in each*, and which of the two readings the
designer meant is not recoverable from the file. Refused rather than divided or doubled. ⚠ **A second
combination is refused at generation for the same shape of reason**: a Ruleset declaring a gate kind
paves the lattice to the map's **boundary**, which leaves no ground for a second Lattice and no gap
between them. ***A world with two centres AND a door needs the extent to stop being all-or-nothing***,
and nothing needs it yet.

### Task 2, 2026-08-22 — **F7** to **F10**

**F7 — the field already existed, and the task was a name and a measurement rather than storage.**
`adr/0134` specifies *a watershed over a Building-density field on the Cell grid* and names
`LayerCellTable`, `LayerDiffusion` and `SeparableKernel` as the machinery that exists. **None of those
is where the field was.** `BuildingResidency` — milestone 5b-bis, built for `adr/0081`'s job search
because a fair draw over a box needs a count per Cell rather than a walk per Cell — holds
`int[CellGrid.WorldCellCount]` of exactly this, maintained at the write site and rebuilt with the
index. ***A value per Cell is a field whatever the consumer that first wanted it was called.*** Task 2
adds `BuildingResidency.Density`, so the watershed reads a field rather than a 1×1 box query — the same
array read, and only one of them says what it is. ⚠ **The ADR pointed at the wrong three files and was
right about the mechanism**, which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
working exactly as written: *a description of the build is where to look, and never what you found.*

**F8 — the field is not smoothed, and the measurement is what decided it.** A kernel would introduce a
**radius**, which is a fifth hash-bearing number where `adr/0134` enumerates four and owes `plans/0002`
§D rows for those. ⚠ **MEASURED before deciding** ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)):
the generator lays Lots uniformly, so a Cell inside a lattice holds **exactly 10** Buildings, its west
edge 7 and its east edge 5. ***The field is not noisy, it is flat*** — and smoothing is what you reach
for against noise. The maxima form **one plateau component per lattice** with nothing spurious to
filter. **Revisit trigger is named and it is a test**:
`BuildingDensityFieldTests.The_field_is_flat_inside_the_built_area` goes red the day the generator stops
laying Lots uniformly, which is milestone **15**'s agglomeration and is the day the argument above
stops holding.

**F9 — `twinned.toml` has a LOWER size bound as well as an upper one: 2,000 Citizens.** ⚠ **MEASURED.**
At 1,000 each lattice holds ~61 Buildings over ~11 Cells, does not fill its own ground, and its ragged
edge carries maxima of its own — **four** plateaus rather than two. From 2,000 up it is exactly two at
every size measured to 64,000. ⚠ **The watershed would still find two at 1,000**, because prominence
discards a plateau whose saddle to a higher one is barely below it — ***the field counts candidate
centres and the watershed counts accepted ones***, and the distance between those two numbers is what
the prominence threshold is for. **Both halves are committed as tests** and the floor is in the file's
header, because a reading taken at 1,000 would look like a defect in task 1.

**F10 — density counts every Building, including an Outside Connection, and nobody has decided whether
it should.** Whether a gate is a *concentration of activity* is a question no document asks. ⚠ **It is
inert rather than open**: no shipped Ruleset has both a gate kind and a second lattice, and the
generator **refuses** that combination (**F6**), so there is no world in which the answer is
observable. ***Filed here and not in [`0002`](0002-open-questions.md), because nothing is blocked on
it*** ([`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) — the world
that would pose the question is **unbuilt**). It becomes a question on the day a Ruleset wants both.

### Task 3, 2026-08-22 — **F11** to **F17**

**F11 — there is no `DistrictId`, and the plan naming one was the plan naming a shape the project does
not have.** This task's line asks for *"`DistrictTable`, `DistrictId`, and the first derivation"*. What
shipped is `DistrictTable` and `Handle<District>`. ⚠ **The project has exactly one identity for a table
row** — a handle over a monotonic never-reused id, which is what `Rows.SavedHandle` folds into the State
Hash — and a second spelling of *which District* would be a second thing to keep in step across a save,
a reload and a task-4 re-evaluation, for no capability. ***A name in a plan is a request for a
capability, not for a type***, and the capability was already there.

**F12 — a persistence watershed needs TWO descents and one cannot be made to do.** The first asks which
maxima are real: when two basins meet at level *h*, the lower peak dies and its **prominence** is its own
height minus *h*. The second repeats the descent and refuses exactly one merge — the one joining two sets
that each already hold a surviving peak — and that refusal *is* the boundary. ⚠ **The second flood cannot
be folded into the first**, because whether a merge should be refused depends on a prominence that is not
known until the whole descent has run. The cost is one extra pass over the built Cells, which is nothing;
the alternative was a boundary drawn from a prominence computed so far.

🔴 **F13 — *unlabelled* is not an equivalence class, and comparing two road-component labels for equality
made the clip silently bridge the thing it exists to separate.** `RoadConnectivity.Unlabelled` is `-1` and
means **unknown**: a Cell answers it when nothing in it stands on a Street the connectivity pass reached.
The first version merged two Cells when `component[a] == component[b]`, so every unlabelled Cell in the
world was mutually connected to every other one — ***unknown behaving as the largest component there
is***. A hand-built fixture with two islands in it came out as **one** District with every assertion about
the density field passing. The rule that shipped is `Reaches(a, b) => a != Unlabelled && a == b`, so an
unlabelled Cell becomes a District of its own. ⚠ **That answer is strange and somebody will notice it,
which is the point** — the alternative was a merge nobody would. **The general form is worth keeping**:
*a sentinel that compares equal to itself is a sentinel that has quietly become a value.*

**F14 — a Cell must report its own road component, not its lowest-slot Building's.** `Frontage.Locate`
gives no Segment to a Lot sitting exactly on an intersection, because `CONTEXT.md` → Address is emphatic
that an Address is **never a Node**. The first version read the component off the first Building in the
Cell's list, and in the fixture above that Building was the one on the corner in **all eight** Cells — so
every Cell reported *unknown* while every Cell was squarely on a Street. It now walks the Cell's Buildings
until one has an Address. ⚠ **This is `adr/0093` from the writing end**: the defect was not in what the
component means, it was in **which symbol was asked**.

🔴 **F15 — the derivation belongs to `SyntheticCity.PopulateInto`, and hanging it off `CommandKind.Populate`
attached it to one CALLER of the thing that builds the city.** The first version called
`World.EvaluateDistricts()` from `Simulation` immediately after the command applied. It was defensible —
a District is world state and state changes belong in the log — and it was wrong: **most of the suite
populates a world by calling `PopulateInto` directly**, so most of the suite got a city whose Ruleset
said *two Districts* and whose tables were empty. ***The rule is that a derivation hangs off the
mechanism, never off one of its callers***, and the test for it is to ask what the second caller gets.

**F16 — a table whose writer is gated on a Ruleset key is, to a fixture, a table with no production
writer.** `FactorioTests.Every_saved_column_reaches_the_file_and_no_other_one_does` names **eleven**
unreachable saved columns and needed a **fifth** world to reach them. ⚠ **This is the fourth time that
test has needed a new fixture for the same reason** — its own remarks already say *a table with no
production writer needs a fixture named for it or its columns are carried by the format and checked by
nothing* — and the new half is that *gated on a key one shipped file states* counts as **no** writer.
🔴 **It found F15 as well**, which a format test has no business finding: the eleven columns were
unreachable because the derivation never ran, not because the writer was wrong. ***A coverage assertion
caught a wiring defect, and it caught it because it pins its residue by name rather than tolerating a
count.***

⚠ **F17 — the threshold is unratifiable on every world that exists, and the SUITE says so rather than
only [`0002`](0002-open-questions.md).** `Two_lattices_are_two_districts_at_any_threshold` is a `[Theory]`
at **1, 50 and 100** and gets **two** every time; `One_lattice_is_one_district_at_any_threshold` is the
same shape and gets **one**. The reason is measured and is `twinned.toml`'s header: the density field is
**flat**, so the two concentrations never touch at a saddle and the threshold is never consulted. ***A
test that cannot tell 1 from 100 has not ratified 50***, and writing it as a theory is what makes that
visible where somebody is looking. §D1 carries the row and names **milestone 15**.

**F18 — the watershed's cost is below the instrument's noise floor, and the suite-duration finding
beside it belongs to somebody else.** ⚠ **MEASURED.** World creation, three runs each, Release:
`minimal.toml` (no `[districts]`, so `Evaluate` returns before it collects anything) **2.13–2.23 s**;
`twinned.toml` (two Districts derived over a full 262,144-Cell scan) **2.14–3.06 s**. ***The two bands
overlap, so what this says is that the derivation is too cheap for this instrument to see and NOT that
it is free.*** No `plans/0013` row is owed either way: [`0013`](0013-tick-budget.md) prices a **Tick**,
and this runs once at world creation. 🔴 **What the gate DID find is not this milestone's**: the
assertion tier ran **3 m 02 s over 1,974 tests** against `CLAUDE.md`'s recorded **42 s over 1,690**.
**It was checked against task 3 rather than blamed on it** — the reading is unchanged either side of
**F15**, which moved the derivation from a call site most fixtures miss to one they all take — and then
**routed to [`0012`](0012-corpus-audit.md) on the day**, which is
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md).
⚠ **It is filed and not fixed, because the replacement is a capture**: today's readings were taken with
edits happening alongside them, so they are **upper bounds**, and ***an upper bound is exactly what must
not be pasted into a table a reader will treat as the working loop's cost.***

### Task 4, 2026-08-22 — **F19** to **F23**

✅ **F19 — hysteresis needed no new quantity, because the flood already computes one.** The band wants
*how decisively the field favours the new District*, and the obvious implementations all invent
something: a dwell counter, a distance to each centre, a re-run of the flood per District. **None was
needed.** Reachability along Cells of at least a given density is symmetric, so ***once two basins have
met at level h, everything either of them gains below h is reached by both at exactly the same level***
— which makes *contested* mean precisely *acquired at or below the touch level*, and makes the margin
`winLevel − min(winLevel, touchLevel)`. It costs **one scalar per basin** and **one array**, and the
behaviour falls out rather than being tuned in: a Cell deep in a basin has a large margin and follows
the field, a Cell on the boundary has a margin of zero and never moves. ⚠ **`adr/0134`'s *never on a
tie* is therefore exact rather than a figure of speech**, and the ties are the majority of a boundary
rather than a rare coincidence.

🔴 **F20 — this plan's own task 4 entry called the Cell bound a WORK bound, and `adr/0134` and
[`0002`](0002-open-questions.md) §D2 both say it bounds how far the boundary MOVES.** Those are
different numbers and would have been different code — a work bound makes the flood incremental and its
answer depends on where it stopped; a change bound runs the whole flood and applies at most *N* of its
conclusions. ***One is a profiler's number and the other is a designer's***, which is exactly the
distinction the entry's **next clause** was drawing: *"must not size a District from a profiler."* **The
entry contained its own refutation one clause later.** ⚠ **What made it visible was reading the ADR
before the plan** — a task entry is a pointer to a specification and not one, which is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
reaching a document rather than a symbol. Struck in place above and filed in
[`0012`](0012-corpus-audit.md).

✅ **F21 — reconciliation is one path and the first evaluation is its degenerate case, and the
regression suite is the evidence.** A world with no Districts reconciles against nothing: every Cell is
joining its first District, so nothing is migrating, so neither the band nor the bound has anything to
hold. ***A separate first-run path would have been a second implementation of the same reconciliation,
differing only in what it had not been told about*** — and the day it drifted, nothing would have said
so. **All twenty-two of task 3's tests passed against the new path with no edit**, which is what that
claim looks like when it is true.

**F22 — a snapshot taken before a mutation must be iterated at its own length, not the table's.**
`dying[]` is sized from `SlotCount` before any District is created, because `Create` may recycle a slot
a previous evaluation freed — and the destruction loop then walked the table's *current* `SlotCount`,
which had grown. It threw on the first fixture that opened a District. ⚠ **The bug is not the array
length; it is that the loop's bound and the array's came from the same expression evaluated at two
different times.** The fix names `standing` in both places.

**F23 — new ground must not spend the migration bound, and the fixture proves it by setting the bound
to one.** A Cell joining its *first* District is growth, not migration; counting it would freeze a
growing city's boundaries against a budget its own construction was spending, which is the opposite of
what damping is for. **A Cell whose District is being destroyed moves for free too**, because the
alternative is membership of a row that will not exist — and that one is a correctness requirement
rather than a preference. ⚠ **THE GOLDEN HASHES DID NOT MOVE**, unlike task 3's: the cadence adds a
branch to phase 6 that reads a Ruleset key **no golden session states**. ***A mechanism gated on a key
is a mechanism no baseline covers***, which is the same sentence `FactorioTests` needed a fifth world
for at task 3 (**F16**), arriving here as a reason nothing broke.

🔴 **F24 — destroying a District opened a hole the State Hash structurally cannot see, so it needed an
invariant rather than a test.** A handle column folds the target row's **monotonic id**, and
`Rows.TryIdAt` returns *false* for a handle whose target has been freed — which folds as **zero**. So a
membership row left naming a destroyed District is a dangling reference that **replay equivalence,
thread-count equivalence and save/reload equivalence all agree about**: ***two runs reproduce the same
wrong answer***, which is `adr/0004`'s divergence-not-a-crash arriving from the direction where nothing
even diverges. `Invariant.ADistrictCellNamesALiveDistrictAndBuiltGround` is registered end-of-run, and
what it really asserts is an **order** — membership released before the row it names — which is the kind
of thing that is right when written and wrong three mechanisms later. ⚠ **Its second half is not
redundant with the first**: a Cell that stopped holding Buildings and kept its row names a live District
perfectly well, and puts **empty ground inside a Pool**, which is the abstraction `adr/0013` is a lie
without rather than a simplification. **Three tests write both violations and watch them fire**, and a
third runs the reconciliation and watches one clear.

### Task 5, 2026-08-22 — **F25** to **F29**

✅ **F25 — the Pool hangs off the OWNER row, and which shape that is was decided by what a load can
recover rather than by taste.** `BinTable.Owner` is a `HandleColumn<Building>` bound to `buildings.Rows`
at construction, so a District cannot go in it, and this plan already said not to widen it. What it did
not say is which of the two remaining shapes to use, and they are not equivalent. **A Building's Bins
hang off a `Derived` list that `World.RebuildDerived` re-threads from the Bins' own owner column** — it
can, because the element names its owner. **A Pool Bin's row names nothing**, so there is no column to
re-thread from and a derived list would come back empty. `Space.DistrictPoolTable` is therefore
**saved**, and it is the only statement anywhere of which District owns which Bin. ⚠ ***The difference
between the two shapes is what is RECOVERABLE and not how much care was taken***, and a derived list is
only derivable when the element names its owner.

✅ **F26 — no lookup index shipped, and `Scope.Pool` still throwing is the argument.** The obvious
companion to the join is a dense `(District, Resource) → Bin` index, on `DistrictResidency`'s model. It
is not here. **Every read of the table today is cold** — opening a Pool, and retiring one — because
nothing can resolve a `pool` term at all, so an index would be sized against a table nothing walks and
measured by nothing. ***An index built before the hot path exists is an index whose shape is a guess
about a caller nobody has written.*** Task 7 owes it, on the Tick a pool term resolves for the first
time.

🔴 **F27 — a Ruleset edit that INSERTS a resource crashes the swap, and it crashes on the treasury.**
Found by a test of this task and it is **milestone 10's defect, reproduced on `rulesets/minimal.toml`
with no `[districts]` in it at all**. `RulesetMigration` maps Resources **by name** and says why in its
own words — *"the map exists because an id is not an identity"* — and `World.Migrate` applies that map
by walking `Buildings.Rows`. **That is the whole of where it is applied.** A `ResourceId` is the
declaration's *position*, so an insert renumbers everything after it, and a Treasury, Household,
Business or Pool Bin keeps the outgoing file's id. `World.RebuildCapacities` throws
***"bin 0 holds a non-conserved Resource and is owned by Treasury"*** — bin 0 being the treasury's
money, whose id was `money` and is now `repairs`. ⚠ **The migration is right and its REACH is short**,
which is the opposite of the defect it looks like. Routed to [`0003`](0003-build-plan.md) queue item
**15** under [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)
and expressly not fixed here; the test appends its new Good instead, which is a legitimate fixture and
not a workaround, because the test is about fitting a Pool. ⚠ **Task 5 widens the blind spot without
widening the defect**, and the wider half is the worse one: a Pool Bin's ceiling derives from its
**owner** rather than its Resource, so it does not throw — ***it comes back holding a stock of whatever
the id now names, silently.***

🔴 **F28 — `Invariant.CrossTableHandleResolves` already covers every dangling saved handle, and task
4's invariant did not know it.** That walk is **column-driven** — *"every column is asked whether it
dangles, so a handle column added in a later slice is covered the day it is declared"* — and it is
registered **first**. So a test that frees a District and asks the registry gets `CrossTableHandleResolves`
and not the District member, which is how this was found: task 5's version of that test failed, and task
4's identical-looking one passes because ***it calls the check directly rather than through the
registry.*** ⚠ **What is left to the per-table member is the UNSET handle**, which is not dangling and
which the generic walk is right not to report. Both members now say so at the declaration. ***A test
that trips two checks tests whichever is registered earlier***, and the isolation is the point rather
than the tidiness.

✅ **F29 — succession reuses identity's rule, and the symmetry is the argument rather than a
coincidence.** A dying District's stock goes to whoever now owns its **centre Cell** — the same one Cell
pass 1 reads to decide which District inherits a basin. `adr/0134` makes a District *be* its centre, so
the row that inherited the centre is the row that inherited the District, and any other rule would make
identity and succession disagree about the same Cell. ⚠ **A District can still die with NO heir** —
demolish its ground and the centre stops being built — and that destroys Goods, which `04 §2` forbids.
`Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool` is what makes it a failure instead of a leak. **It
cannot fire today and the reason is exact rather than lucky**: `Scope.Pool` throws, so every Pool is
empty at every moment and *no heir* and *nothing to hand over* coincide. ***It is the day task 7 opens
the scope that it becomes a real constraint***, and the test that watches it fire has to deposit by
hand to get there.

