# Demand for a shop is elapsed unserved need in reach, and building claims it

**A Zone Rule raising a trade's premises builds when the total *elapsed* unserved need for a Good that
trade sells, within reach of the Lot, exceeds a threshold — and raising the Building **claims** that
need, so a second Lot cannot answer it twice.** Four forks are bundled here and every later tier
inherits all four: demand is **elapsed** rather than instantaneous, **in reach** rather than global,
**per Good** rather than undifferentiated, and **claimed** rather than free. `LEGIBLE CAUSE`
`EMERGENCE` `UNIQUE INDIVIDUALS`

**It is tier 1 of a four-tier ladder, and the other three are named here so they are not folklore.**
Tier 0 is what ships today and is being replaced; tier 2 is the tenant's own test and tier 3 the
developer's pro-forma, both **designed and deferred** on unbuilt dependencies named below.

---

## Why

### What was refused was a synthesised scalar, not demand modelling

The rule this design carries is *do not **add** a demand scalar*, and it has been read — in session W's
own opening, before it was checked — as *do not model demand*. That reading is wrong and the corpus
already says so. [`01 §`](../01-player-experience.md) at the RCI discussion: ***"`CLAUDE.md`'s rule is
do not **add** a demand scalar — this one is not added, it is **counted**."*** And the thing that makes
an RCI meter indefensible is named precisely: ***"An RCI meter is a synthesised scalar with no
constituents. Nothing in SC4 **is** the RCI value; it is a formula's output drawn as a bar, and it
cannot be interrogated when it is wrong."***

⚠ **So the test is constituents, not arithmetic.** A magnitude computed from named records each carrying
a reason is not an RCI meter however large the number gets, and
[`adr/0023`](0023-immigration-arrives-through-the-gate.md)'s bright line — ***"No Lot ever reads an
aggregate demand figure"*** — is satisfied **more** strictly by this decision than by what it replaces,
because a reach-bounded query over real rows is not an aggregate at all. ***The global count is the
thing that flirted with `adr/0023`; the per-record test is what keeps it honest.***

### `02 §5` already specified this, and the step is unbuilt rather than undesigned

[`02 §5.2`](../02-simulation-model.md)'s six-step loop, adapted from UrbanSim, states the mechanism in
its own step 3 — ***"Business placement: same shape; commercial seeks **unserved needs in reach**"*** —
and its closing paragraph states this ADR's whole thesis: ***"The residual is the demand signal — and it
is a list of specific frustrated Households with specific reasons, which the commercial and development
logic can **read directly**."*** 🔴 **Steps 1, 3, 4 and 6 are not built**, which `02 §5` says on its own
face. **Under [`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) this is *unbuilt*
and the answer is therefore build it**, rather than *undesigned*, which would have made this ADR a
larger act than it is.

### The boolean it replaces was never a principle, and its own author said so

`ZoneRuleEngine.Create`'s summary line states the create predicate as ***"vacant AND permitted AND
somebody in the Pool would take it"*** — a per-record acceptance test — and `CONTEXT.md` → Frontage
lists ***"no Household in the Unplaced Pool that would accept it"*** among the four documented reasons a
Lot is vacant. **What shipped is `UnplacedPool.Count == 0`**, and the same doc comment admits the gap
three paragraphs on: ***"The Pool is read as non-empty and drained blind. There is no acceptance test,
because acceptance needs rent, a commute and a tolerance."***

⚠ **Nobody decided a presence test was correct.** It is an honest stand-in for a test that could not be
expressed in 2026, and ***a stand-in inherited without being re-read becomes a principle by default***,
which is the failure this ADR exists to stop. **The session recommended keeping it for one exchange, on
the grounds that an impoverished signal cannot smuggle in a synthesised one, and the user refused it:
consolidating to a boolean discards the constituents that make the signal defensible in the first
place.**

### Milestone 26 produces the signal as a by-product, which is what makes tier 1 cheap

A Household's larder refills from nothing today — `rulesets/minimal.toml`'s `restock` is `inputs = []`,
producing four sundries against no draw. ***That absence is the shop.*** When `Scope.Pool` resolves and
`restock` draws from a market, a Household with no reachable seller **fails its Rule and starves**, and
starvation is already a first-class, interrogable state: a `Blocking` reason, a wait list, and a
`StarvedSince` clock. **So *unserved need in reach* needs no new record, no new column and no new
mechanism — it is a query over rows milestone 26 creates anyway.**

### Elapsed rather than instantaneous, because a flag blinks and a clock does not

Starvation clears the instant a Rule fires successfully — `RuleEngine.Fire` zeroes `Reported` and
`StarvedSince` together, and recovery is total. **A Household starving intermittently is therefore
invisible to whichever samples happen to catch it fed**, and a Zone Rule samples a *share* of Lots on a
cadence. ***A signal that flickers under sampling is a signal that reports a sampling artefact.***

✅ **`StarvedSince` is a timestamp rather than a flag, so the fix costs nothing**: `tick - StarvedSince`
is elapsed unserved need, and the **condemnation path already does this exact arithmetic** —
`ZoneRuleEngine.Worst` compares `elapsed` against `threshold × rate` per Rule Instance. ⚠ **This is
strictly more information rather than less**, which is the direction the whole argument runs: a
magnitude assembled from named rows, each still carrying its own reason.

### Claimed rather than free, because otherwise one hungry street gets five shops

**Several Lots are sampled in one pass and each would read the same starving Households.** Nothing tells
the second Lot that the first has just answered this demand, so ***an undifferentiated read overshoots
in proportion to how many Lots happen to be sampled together*** — a number that is a property of the
cadence rather than of the city.

`02 §5` carries both available answers and they are not equivalent. Step **2e** — ***"consume the
dwelling — capacity is real"*** — makes satisfaction subtractive, and step **5**'s ***"throttled by a
build rate"*** caps output regardless of signal. **Consume is taken and throttle is rejected**: a
throttle cannot tell *five shops for one hungry neighbourhood* from *five shops for five*, so it
suppresses the symptom while leaving the reading wrong. ***A claim makes the demand a stock that
answering it depletes, which is the same shape placement already has.***

## Rejected

**Inherit the Household pool's term unchanged.** Free, and what happens if this session decides nothing.
Refused because shops would then rise in proportion to demand for **homes** — so a city that housed
everybody would stop building shops however many were queuing for premises, and the signal would be a
proxy for the wrong quantity rather than a weak measure of the right one.

**A Household in reach, full stop — *"there are people here"*.** Buildable before the purchase and would
let the Provider ship first. Refused because it discards the reason, which is the property that
distinguishes a counted signal from a synthesised one. ***It is the boolean's mistake at a smaller
radius.***

**The full pro-forma now** — `02 §5`'s step 5 as written, `revenue(price) − cost > hurdle`. It is the
right long answer and is **tier 3**. Deferred rather than refused: it needs a land price surface, a
capital position and a bid contest, none of which exist, and
[`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) forbids
choosing its numbers by argument.

**A build-rate throttle instead of a claim.** Cheaper and hash-neutral. Refused above: it bounds the
work without correcting the reading.

## Consequences

- 🔴 **Tier 1 cannot ship before `Scope.Pool` does, so the Provider and the purchase land together.**
  This is a scheduling cost taken deliberately: the alternative was the *people in reach* proxy, which
  would have decoupled them by discarding the reason.
- 🔴 **Two hash-bearing numbers are owed, unset, and go to [`plans/0002`](../../plans/0002-open-questions.md)
  §D2** — the **build threshold** (how much elapsed unserved need raises a Building) and the **claim
  amount** (how much a raised Building subtracts). ⚠ **Neither is chosen here and neither may be**:
  under [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) a
  ratifier names a machine, **a world** and **a quantity**, and the world is milestone 26's own
  demonstration Ruleset, which does not exist yet. ***§D2 rather than §D1 because they are unset rather
  than in use.***
- 🔴 ~~✅ **W-Q2, the decline Rule, becomes nearly free and stops being a separate invention.** A shop
  with no customers in reach sells nothing, earns nothing and fails its own Rule — and failing a Rule is
  already what condemns a Building.~~ **THIS WAS WRONG AND WAS CORRECTED THE SAME DAY, IN THE SAME
  SITTING** ([`adr/0166`](0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md)).
  ***A shop nobody buys from is IMMORTAL.*** Its output Bin fills, so it stops on `Blocking.Space`, and
  `RuleEngine.Stop` **clears** the failure-pressure clock for every blocking reason that is not
  `Blocking.Supply` — deliberately, because `RuleInstanceTable.StarvedSince` records that a full Bin
  ***"is what a well-supplied Building with nobody to sell to looks like"*** and `02 §5.9` names **input
  starvation** as the source. ⚠ **A trade cannot be made to churn by failing to SELL; it must go short
  of something it CONSUMES**, which is money, which is bankruptcy — and **nothing can hang a Rule on a
  Business today**, so `adr/0166` is the prerequisite. ***The error is left visible rather than deleted
  because it was made twice in one sitting, in the same direction: reasoning about a mechanism from what
  it plainly ought to do rather than from what it does*** — which is
  [`adr/0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) with no
  document to blame.
- ⚠ **The reach query is the dependency and its cost is *measurable* and UNMEASURED.** It runs per
  sampled Lot per trigger, over the walk search whose cost is `≈ 37 ns × distance²`
  ([`plans/0002`](../../plans/0002-open-questions.md)). **A row is owed in
  [`plans/0013`](../../plans/0013-tick-budget.md) on the day it is built**, and it must be a measured
  multiplicand rather than a guessed one.
- ⚠ **The existing housing rule is LEFT ALONE and is now known to be tier 0** — a documented stand-in
  rather than a principle. ***Whether housing is lifted onto the same ladder is a separate question this
  ADR does not answer***, and it is the larger one: the same argument applies to it word for word, and
  the reason it is not taken here is that housing works well enough to have no milestone pushing on it.
- ⚠ **This ADR settles one of session W's four questions.** The **land-use split** and **which Ruleset
  carries the Provider** remain open in [`0043`](../../plans/0043-session-w-the-provider-kinds-content.md).
  🔴 **The land-use split is the one with no mechanism under it at all** — `SyntheticCity` paints one
  zone bit, `LotTable.Create` is the sole writer of `Lots.Zone`, and a Ruleset naming an unpainted bit
  loads clean and builds nothing.

## What would trigger revisiting

- **A measured reach-query cost that does not fit its [`plans/0013`](../../plans/0013-tick-budget.md)
  row.** The query is per sampled Lot per trigger and the walk search is quadratic in distance; if it
  does not fit, the fork is a coarser reach unit rather than a return to the global read.
- **A run in which shops oscillate** — built, condemned for want of customers, rebuilt on the demand
  their own condemnation restored. That would say the claim amount and the threshold disagree, and it is
  the first thing milestone 26's acceptance run should look for.
- **Elapsed need dominated by a single pathological Household.** A Household starving for ever
  contributes unboundedly, so if one row can outweigh a neighbourhood the magnitude needs a per-row cap
  — which would be a fifth fork and is deliberately not taken pre-emptively.
- **Rent, or any price for premises, coming to exist.** That makes **tier 2** buildable and moves the
  tenant's choice off first-fit.
- **A land price surface and a capital position coming to exist.** That makes **tier 3** — `02 §5`'s
  pro-forma — buildable, at which point tier 1's threshold is superseded rather than retuned.
- **Housing being lifted onto this ladder.** If tier 0 is replaced for dwellings too, the *claim*
  semantics must be reconciled across both, because two Zone Rules claiming from different pools against
  one Lot sample is a case this ADR has not reasoned about.
