# 0037 — Goods between Buildings: the District Pool

**`06` milestone 12.** Ungated. The milestone that makes `Scope.Pool` resolve, and with it the first
Goods chain that crosses an ownership boundary.

---

## Status

🟢 **SCOPING STARTED 2026-08-21. The decision 1 sitting ran 2026-08-22 and no task has begun.**

⚠ **One sitting has run and it settled less of decision 1 than it changed about it.** It produced
[`adr/0132`](../docs/adr/0132-the-district-is-derived-and-a-ward-is-what-the-player-draws.md) and
[`adr/0133`](../docs/adr/0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md),
which between them **close decision 1's player-arm sub-question by removing the player arm**, add a
sub-question to decision 4, and change what decision 3 owes. **Decision 1's substance — the derivation
itself — is still open**, and decisions 2 and 5 to 9 are untouched.

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

## Open decisions this milestone owes — **ALL NINE STILL OPEN**

Typed under `adr/0043`. **These want a sitting, and several are entangled** — 1 governs 2 and 3.

⚠ **The heading said *NONE SETTLED* and that is no longer the useful statement.** The 2026-08-22 sitting
settled **parts** of 1, 3 and 4 without closing any of the nine: `adr/0132` removed decision 1's player
arm, and `adr/0133` added decision 4's haulage sub-question and changed decision 3's obligation from a
ceiling to possibly a curve. ***A decision list tracks decisions and a sitting moves clauses***, so read
each entry rather than this heading.

### 1. What derives a District from road topology and land use? — *arguable*

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

**What is open is the algorithm, and the two named inputs already cut the field:**

| Candidate | Against it |
|---|---|
| Connected component of the Road Graph under a distance bound | Uses **topology only** and never land use. `RoadConnectivity` is a union-find with no diameter bound, so on `rulesets/bordered.toml`'s 535,817 Segments the whole lattice is **one** component and the bound is the entire mechanism. A component is a set of *nodes* and a District is a set of *Cells* |
| A fixed partition over Cells | Uses **neither** input. Boundaries are arbitrary and invisible, so two Buildings either side of a line do not pool and nothing can show the player why. It also cannot express *"more appear as it outgrows the pooling radius"* by growing — only by lighting up more tiles |
| Growth seeded at Buildings, bounded by the pooling radius, merging on contact | **Uses both**, and is the only candidate that reproduces *the count is physics* rather than approximating it. The costs are real and belong in the sitting: placing one Building can **merge two Districts and change what pools city-wide in one Tick**, and it needs a rebuild-on-load story that `DerivedRebuildAuditTests` is the only thing that would ask for — decision 2 |

⚠ **`RoutingPartition` is not the answer and reusing it is a regression**, not a shortcut:
[`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md) detached the District from
travel-time matrix granularity on purpose. ⚠ **`02 §2.1` still said otherwise until 2026-08-22** — the
strike is recorded there and in [`0012`](0012-corpus-audit.md) — so ***the document a scoping reader
would open for the space hierarchy was, until this sitting, telling them to re-attach it.***

### 2. Does a District ship as a saved entity, or as derived state? — *arguable*

Consequential for `05 §4` and for saves. ⚠ **If derived, it must be rebuilt on load**, and
`DerivedRebuildAuditTests` is the only thing that would ask — the milestone-7 `car_park.segment_next`
lesson. ⚠ **District extent decides Goods pooling, which is a change to the city**, so whatever this
is, it is **hash-bearing**.

### 3. What ratifies the District's extent? — *measurable*, and it needs a `plans/0002` §D row

`CONTEXT.md`'s **128 Cells** is labelled a *"working anchor… a starting point rather than a
derivation"*, with *"what actually pools convincingly is a playtesting question"*, and its ceiling
argument is that a District *"can only be as large as the area within which ignoring transport is a
defensible simplification."*

Under [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
a hash-bearing number is **chosen with a named ratifier or not at all**, and a ratifier must name a
machine, **a world** and **a quantity**. ⚠ **This number has none today and is about to become
hash-bearing.** It cannot arrive without a §D row.

### 4. Where does a price come from at 12? — *arguable*

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

### 5. Does Upkeep ship at 12? — *arguable*

`adr/0117` says **12 is the earliest and not necessarily the right one**, and its own trigger list
includes *"milestone 12 shipping without `Scope.Pool` resolvable from a Segment — then the placement is
wrong and Upkeep moves again."* Grounds 2, 3 and 4 are all open. ⚠ **The default answer is no**, and
what this milestone owes is to say so deliberately rather than by omission, and to move it.

### 6. Does the `adr/0088` `min()` ship here? — *arguable*

`min(declared ceiling, Segment capacity)` plus the **which-of-the-two-binds** readout, relocated from
11 by [`0035`](0035-hinterlands-and-arrival-through-the-gate.md) decision 9 on the ground that *"the
second term follows freight to 12."* ⚠ **Whether freight itself is in this milestone is decision 8**,
so this one is downstream of it.

### 7. What does *"subject to connectivity"* mean for a Pool draw? — *arguable*

`CONTEXT.md` → District Pool: *"Goods moving between Buildings within a District pass through the Pool
instantly, **subject to connectivity**."* `Scope.Pool`'s doc says the scope *"requires road
connectivity"*. `RoadConnectivity.cs` exists. **What has never been said is what a disconnected
Building does** — fail on the Pool Bin, be outside the Pool entirely, or be in a District of one.

### 8. Does a second `[[building]]` kind ship — a Provider that is not a dwelling? — *arguable*

`06`'s obligations table places *"a Ruleset that models a city"* here and says **12 is the first row
whose mechanism cannot run on a single-kind Ruleset** — *a shopping occasion needs somewhere to shop*.
⚠ **Every shipped Ruleset's emitting kind is `dwelling`**, and `rulesets/crowded.toml`'s header already
records why: industrial demand is **unbuilt**, so the only demand signal that exists is demand for
homes. A market with one kind of participant is a market in name.

### 9. How is bankruptcy told from starvation, and does anything need building for it? — *arguable*

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
- **Conservation holds across a trade**, asserted rather than argued: Goods and money both, over a long
  run, with `adr/0024`'s equality exact.
- **Bankruptcy and starvation are distinguishable** in `Evidence` on a world that produces both.
- There is something to *look at* — a runner mode showing a Pool with stock, a price, and a Building
  that could not afford it.

---

## What scoping found

*(Filled as the sitting runs. The three preconditions above were found before it started, which is why
they are in the survey rather than here.)*
