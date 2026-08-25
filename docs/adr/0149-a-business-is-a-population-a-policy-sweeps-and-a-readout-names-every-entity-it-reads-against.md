# A Business is a population a Policy sweeps, and a Readout names every entity it reads against

**Two decisions, because milestone 27 task 9 is blocked on both and neither is worth an ADR alone.**

**First: `sweeps = "business"` is accepted, and a Policy sweeps every live Business exactly as it
sweeps every live Household.** A balance is a Bin whoever holds it
([`adr/0114`](0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md)), so what
`PolicyEngine` needs from a subject is a slot count, a liveness test and a balance Bin — and a Business
has all three. ⚠ **A Bin Rule armed on a trade was built first and is WITHDRAWN**; the measurement is
below.

**Second: a Readout declares the SET of entities it can be read against rather than one.**
`Readout.Balance` is readable against a **Household and a Business**; `occupancy` only against a
Building. The loader's equality becomes a membership test, and `Readouts` gets a third entry point on
the rule it wrote for itself.
`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `HONEST DEGRADATION`

⚠ **Both holes were found by [`plans/0041`](../../plans/0041-the-business-is-a-thing-the-city-contains.md)
before the task started** — **G22** and **G23** — and both were left explicitly unowned: *"unassigned by
tasks 6, 7, 8 and 9 alike; it is the first thing task 9 needs and it is nobody's."* ⚠ **G22's own answer
is the half this ADR withdraws.**

## What was built first, and what refuted it

**The plan's pointer was a third subject on the Rule Instance** (**G10**), and the build had written the
pointer itself: `RuleInstanceTable.cs` — *"A Business gets its own column when a Business runs a Rule,
which is milestone 27."* So the first implementation followed it. A `[[rule]]` gained
`trade = "<name>"`, `BusinessTable` gained `RuleHead`/`RuleTail`, `World` gained `ArmTrade`, and
`RuleEngine.Band` dispatched on the instance's subject.

**It loaded. It crashed on the Tick it fired.**

```
Borough.Core.Tables.StaleHandleException: handle {index 0, generation 0} into table 'building' is stale.
   at Borough.Core.Rules.RuleEngine.Fire(RuleVerdict verdict, Ticks tick)
```

⚠ **`RuleEngine` resolves a Building from the Rule Instance at `Fire`, not only at `Band`.** The
Building-centricity is not a branch near the top that a subject column redirects; it runs through
evidence, through `on_fail`, through the wake targets and through every local Bin lookup. **A
Business-subject Bin Rule is a subsystem, and the column was the visible tenth of it.**

That is [`adr/0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
working exactly as written. The sentence in `RuleInstanceTable` told the truth about *where to look*;
what is in there was never its claim to make, and **G10 read a location as an estimate.**

## Why a Policy is the right family and not the affordable one

⚠ **This matters, because [`adr/0033`](0033-two-rule-families-scheduled-and-swept.md)
forbids picking a family for cost.** Bin Rules and Sweep Rules differ in *observable behaviour*, so
this has to be right on the behaviour or it is not allowed at all.

**It is right on the behaviour.** `02 §4.2`: a Policy is an **entitlement** — it reaches every member
of its population, and paying a random subset would be a defect rather than a model. A levy on a
Business is that sentence. It is not a shop *doing* anything; it is the city reaching every shop.

**And the thing a Bin Rule would model does not exist yet.** A Bin Rule is a trade consuming inputs and
producing outputs on a rate — flour in, bread out — and a Business at milestone 27 holds **no stock**:
its only Bin is money, `Scope.Pool` throws, and nothing it could buy or sell has a counterparty. ***A
Bin Rule over a Business would today be a Bin Rule with one Bin and no terms***, which is a Policy with
extra machinery.

**So the family follows the mechanism**: what exists is a balance and a population, and a population
with balances is what the Sweep family is. A trade that *works* is milestone 26's, and it will want a
Bin Rule, and it will want the Building-centricity unpicked first.

## Why a Readout belongs to entities, plural

`Readouts.ScopeOf` returned **exactly one** `ReadoutScope` per Readout id and `RulesetLoader.ReadApply`
tested it with an **equality**. `Readout.Balance` was declared `Household`-scoped. ***A Business has a
balance too, so one declared scalar belongs to two entities and the declaration could not say so.***

**Membership rather than equality, and the check keeps its exact purpose.** What it exists to refuse is
*a Rule naming a real quantity with no row here to read it from* — a question about whether **this
entity has the scalar**, which a set answers and a single value cannot. `occupancy` stays Building-only
and refuses everywhere else, unchanged.

⚠ **A second Readout named `business_balance` was the obvious alternative and is worse.** It is two
names for one scalar, and it makes an author pick the *name* by knowing which entity their Rule is
attached to — which is the scope declaration doing its job twice, in the file, by hand.

**Three entry points, not one switch**, which is `Readouts`' own rule and it wrote it before there was
anything to apply it to: *"a single method taking an `(entity kind, slot)` pair would be two switches
wearing one signature and would let a Building slot be read as a Household."*

⚠ **The refusal message's sentence is built in `Borough.Formats` and not beside the predicate.** It was
written in `Borough.Core` first and `BoundaryTests` refused it under
[`adr/0002`](0002-simulation-is-an-engine-agnostic-library.md): the shell owns every string
a human reads. What crosses the boundary is `Readouts.Scopes` and the predicate.

## Consequences

- **`sweeps = "business"` stops being refusal 61's second half.** The refusal's own sentence — *"A
  Business has a balance and no pass that moves it"* — was the specification of what closes it, and
  **`adr/0048`'s count does not move**: one `Refuse(` call site served both populations and now serves
  `building` alone.
- ⚠ **`sweeps = "building"` is untouched and is NOT the same absence.** A Business population is every
  live row; a Building population is whichever rows a predicate selects, and the predicate does not
  exist. That one is a missing *mechanism*, this one was a missing *loop*.
- **`rulesets/levied.toml` ships** — `founded.toml` plus five lines. 🔴 **Its levy passes over a large
  part of what it sweeps, on purpose**: `adr/0148` gives every dwelling an instantiated shop whose
  balance opens at zero, and only a *founded* shop holds money. Measured at 2,000 Citizens over 6,144
  Ticks: **302 live Businesses, 177 holding money, 125 holding nothing.** A reader taking that gap for
  a defect has read `considered` as `eligible`. ⚠ **The ratio moves with the run and is not a property
  of the file** — founded shops accumulate while instantiated ones come down with their premises — so
  the test asserts the *shape* and never the ratio.
- **Not hash-bearing for any standing world.** No golden Ruleset sweeps Businesses, and `PolicyEngine`'s
  Household path is unchanged line for line.
- ⚠ **`RuleInstanceTable`'s promised `Business` column is NOT delivered and its comment is now wrong
  about the milestone.** Filed to [`plans/0012`](../../plans/0012-corpus-audit.md) rather than edited
  away, because the sentence is right about everything except when.
- **A trade holding STOCK stays *unbuilt* rather than refused**
  ([`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)). It needs a ceiling, and
  `adr/0141` puts capacity on the premises because `RebuildCapacities` reads `(building kind, Resource)`
  — while ***a Business can be unpremised as a steady state*** (`adr/0142`). **Money dodges it by being
  unbounded.**

## Rejected

**A Bin Rule armed on a trade.** Built, run, crashed, withdrawn — `RuleEngine.Fire` resolves a Building
from the instance. ⚠ **Not rejected on cost**, which `adr/0033` would forbid: rejected because a Bin
Rule models a trade consuming and producing, and a Business at milestone 27 has one Bin and nothing to
put in it.

**A third `BinTenancy` member.** It never arose. The enum answers *whose level is this* for a Bin the
**Building kind** declares; a Bin a trade declares is the trade's by construction, so the question does
not exist. **G22 read the enum as unable to express a distinction that lives one level up**, in which
declaration the Bin came from.

**A second Readout, `business_balance`.** Two names for one scalar, and it moves the scope decision out
of the simulation and into the author's head.

**Sweeping only *premised* Businesses.** A tempting filter, and wrong: `adr/0142` makes unpremised a
legitimate steady state, and a levy that skips the homeless shops would tax a shop for having a
landlord. The population is the population.

**A shipped Ruleset that levies at `household_levy`'s 10%.** A founded shop holds 400 and nothing
refills it, so 10% a Day strips it before it departs — and the file would demonstrate a levy emptying
its own subjects rather than a Rule reading one.

## What would trigger revisiting

- **A Business that earns.** Milestone 26. Then a trade wants a Bin Rule, and the Building-centricity
  in `RuleEngine.Fire` is the work that was priced here and deferred.
- **A Readout that is genuinely one entity's and is wrongly readable elsewhere.** The set makes
  over-permission possible in a way the single value did not; a Readout added carelessly to two sets is
  a Rule reading a quantity that means something different on each, which no test can catch.
- **A Business Rule needing to read its PREMISES' Bin.** `local` means *the subject's*, and a shop
  drawing on the building's supply is a sentence somebody will want. It is a term, not a scope.
- **A third population with a balance.** Two branches in `PolicyEngine` is a shape that stops paying at
  three, and it is the point at which the subject wants to be a resolved `(table, slot)` pair rather
  than a switch — which is the collapse `Readouts` argues against, arriving somewhere the argument may
  not hold.
