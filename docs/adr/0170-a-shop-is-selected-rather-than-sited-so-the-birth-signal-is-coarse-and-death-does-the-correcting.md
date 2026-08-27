# A shop is selected rather than sited, so the birth signal is coarse and death does the correcting

**A Zone Rule raising a trade's premises is a *generate-and-test* mechanism and not a site-selection
one. The demand signal decides **whether and roughly where**, bankruptcy decides **which survive**, and
the design's accuracy therefore lives in the death half rather than in the threshold.** This settles
[`0163`](0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md)'s reach
fork as **the District**, and reframes that record's oscillation trigger: ***churn is the mechanism
until it fails to converge, rather than a failure on sight.***
`EMERGENCE` `LEGIBLE CAUSE` `HONEST DEGRADATION`

**Taken by the user on 2026-08-26, during milestone 26 task 6**, against a session recommendation that
had argued the reach unit on precision grounds alone. The user's question — ***"why should the game
optimise where shops are placed? if the mechanisms work, the shops that emerged within range of the
households that need them are the ones that survive"*** — is the whole of it, and what this record adds
is the three conditions that have to hold for it to be true.

---

## Why

### Precision on the birth signal is worth less than it looks, and the reason is structural

`0163` replaces `UnplacedPool.Count != 0` — demand for **homes** proxying for demand for **shops** —
with elapsed unserved need summed over a reach. The open question was the reach unit, and it was being
argued as *which boundary most accurately describes who a shop can serve*.

***That framing assumes siting is the mechanism.*** If instead the city **overbuilds slightly and prunes**,
the birth signal only has to be right about *whether* and approximately *where*; the threshold becomes
an entry cost rather than a forecast, and being wrong costs a shop's lifetime rather than a permanent
misallocation. **A cheaper signal with a working reaper beats an expensive signal without one**, and
milestone 26 had been building the expensive signal first.

### The reach unit is the District, and the argument is a decision rather than an absence

`RuleEngine.MarketRow` resolves a purchase **premises → Lot → Cell → District** and stops. ***A
Household buys only from sellers in its own District*** — not *prefers*, *only*. So a District is
exactly the set a shop can serve, and no smaller unit describes anything the economy does.

⚠ **This is [`0013`](0013-goods-are-pooled-within-a-district-and-shipped-between.md) DECIDED, with
[`0022`](0022-land-is-a-stock-the-city-spends.md)'s reason underneath it** — a District can only be as
large as the area within which *ignoring transport* is a defensible simplification. Under
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) that is a **refusal**, which is
evidence, and not a gap.

⚠ **The check that matters: the walking-radius alternative is BUILDABLE TODAY and is still refused.**
`ParkingShed.Nearest` is a production-wired, radius-bounded ball on the Road Graph, and
`EmploymentEngine` already does box-then-walk against the Commute Budget. ***So this is not chosen
because the alternative is missing.*** It is refused because a radius would count Households a shop can
**walk to** while ignoring Households it **will serve** — a demand signal finer-grained than the
mechanism it feeds, which is `0070`'s error running backwards.

**Two practical consequences fall out and neither was the reason:** the District answer is **identical
for every Lot in a District**, so it is computed once per District per trigger rather than once per
sampled Lot; and the **claim has somewhere to live**, because `Space.DistrictPoolTable` is already keyed
`(District, Good)` and already carries `Price`, `Rate` and `Consumed`. `plans/0044` open decision 1 —
*"the build has nowhere to subtract from"* — is answered by the reach unit rather than by a new table.

### 🔴 Three conditions, and only one of them held on the day this was decided

**Selection is not free. It requires all three, and naming them is most of this record's value.**

| | Condition | Standing on 2026-08-26 |
|---|---|---|
| **1** | **Death fires for the right reason** | 🟡 **Half.** Milestone 26 task 7 built the levy, so a shop with no customers goes broke — but the verdict was **mis-aimed**, see below |
| **2** | **Birth costs something** | 🔴 **No.** `World.CreateBuilding` moves no money, a Building holds none ([`0113`](0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)), and `provisioned.toml` has no founding channel — ***raising a shop costs nobody anything*** |
| **3** | **Position affects survival** | 🔴 **No.** Under District pooling a shop at the far edge earns exactly what one beside the houses earns |
| **4** | **The city over-supplies** | 🔴 **No, and this one is SELF-INFLICTED** — tier 1 is the mechanism that stops it. Added 2026-08-26 after building tier 1; see below |

**🔴 Condition 4 was NOT in this record when it was written, and it is the one that bit.** Selection
needs something to select *from*. A shop with no competitor sells all it makes and pays its levy for
ever, so ***a city that builds only what demand justifies prunes nothing.*** **That is precisely tier
1's job**, which makes the birth rule and the death rule pull against each other by construction — and
one world cannot demonstrate both. Measured on `provisioned.toml` at 2,000 Citizens: under tier 0 the
city raises **20** shops against **~18** vacant trade Lots and the weakest two go broke; under tier 1 it
raises **4**, and **none declines over 131,072 Ticks** at any threshold or cooldown tried.

⚠ **The repair is a second Ruleset and not a tuning**: `rulesets/oversupplied.toml` is
`provisioned.toml` with the two tier-1 keys deleted, so ***the diff is the whole demonstration***, and
milestone 26 task 7's decline test moves to it. ⚠ **Do not read this as tier 1 being too tight.** It
is a *correct* birth rule meeting a city whose supply ceiling is **the Lot count** rather than demand:
~237 Households consuming 4 sundries every 32 Ticks want ~30 shops' worth, one `stock` Rule makes 1 a
Tick, and there are ~18 Lots. ***The over-supply that selection needs was an artefact of a signal that
did not look.***

**Condition 2 is why the threshold survives.** In a real selection model the entry cost is capital at
risk. Here there is none, so ***the threshold IS the entry cost***, standing in for a capitalisation
band that is `plans/0002` §D2 and belongs to milestone 27. **It may be loose; it may not be removed** —
without it nothing restrains birth and the city churns shops for ever, which is
[`0006`](0006-no-collection-grows-with-elapsed-time.md) arriving as a Rule Instance and Bin cycle.

**Condition 3 is why this selects on COUNT and not on PLACE.** Too many shops in a District and the
marginal ones starve; that is real emergence and it is what ships. ⚠ ***What it does NOT deliver is the
sentence that motivated it*** — *shops near the households that need them* — because range does not
exist in the economy. **That needs shopping to be a Trip with a Provider List**
([`0066`](0066-the-provider-list-is-an-intrusive-index-list-and-its-ruleset-length-is-a-cap-rather-than-an-allocation.md)),
which is `0163`'s tier 2/3 and **supersedes District pooling rather than extending it.** ***It is named
here so it does not become folklore.***

### 🔴 The reaper is mis-aimed, and it was found by measuring rather than by reading

**`ZoneRuleEngine.Worst` filters on `RuleInstances.Household[instance] != tenant` and on nothing else.**
A Business's Rule Instance leaves `Household` **unset**, so it matches the *premises* call
(`tenant: default`) and its failure pressure is counted as **the building's**. ⚠ **The method's own
doc comment says the opposite** — *"THE PREMISES' OWN RULES ONLY, which is `adr/0141`"* — and that
sentence was true when it was written, because no Business ran a Rule until milestone 26 task 1.

🔴 **Measured on `rulesets/provisioned.toml` at 2,000 Citizens over 24,576 Ticks: 20 shops raised,
2 demolished, and 2 is exactly the number that go broke.** The attribution is airtight because a
`shopfront` runs exactly two Rules and `stock` has `inputs = []` — ***a Rule with no inputs can never
be `Blocking.Supply` and therefore can never starve***, so the levy is the only thing on that kind
capable of setting `StarvedSince`.

***So a broke shop demolishes its own premises instead of ending its tenancy and leaving the building
for another trade.*** That is [`0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)'s
*what changes is what dies* — the record that fixed exactly this for Households, in its own words *"ONE
STARVING TENANT CONDEMNED THE OTHER'S SHOP"* — arriving one subject late. **The verdict is corrected to
end the tenancy**, and the premises stand.

⚠ **A peer session reported this as *nothing consults a Business's Failure Pressure*, and that is half
right in the way that matters**: nothing consults it as a **tenancy**, and something consults it as
**premises**. ***A pressure routed to the wrong verdict is not an inert pressure***, and the two have
opposite repairs — one adds a walk, the other narrows a filter, and doing only the first would leave
every broke shop demolishing its building *and* ending its tenancy.

### Where a turned-out Business goes, and why it is not a new mechanism

**`Entities.UnpremisedTable` already exists and is already bounded.** A Business orphaned by
`World.DestroyBuilding` joins it, waits, and leaves the city through `World.Depart` **with its money**,
subtracted from `MoneySupply.Issued` — [`0142`](0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md),
whose own words are ***"the money is neither destroyed nor confiscated; it is exported."***

⚠ **Destroying the Business row instead was considered and refused**, and the reason is
[`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) rather than tidiness: a
destroyed row takes its balance with it, which is ***a leak with extra steps***. The pool's exit already
solves this and the eviction path simply joins it. **So the reaper's repair adds no table, no column and
no sink.**

## Rejected

**Keep arguing the reach unit on precision.** What the session was doing. Refused because the accuracy
it was buying is delivered by the reaper at a fraction of the cost, and because ***a threshold tuned
against a world with no death in it is tuned against a world that cannot correct it.***

**Drop the demand threshold entirely and let selection do everything.** The pure form of the user's
proposal, and the tempting one. Refused on **condition 2**: birth is free today, so there is no brake
except the threshold, and a selection model with a free birth is not selection — it is churn with a
survivor bias. ***It becomes available the day a Business is capitalised***, which is the revisit
trigger below.

**Treat `0163`'s oscillation trigger as a bar on churn.** It reads *shops built, condemned for want of
customers, and rebuilt on the demand their condemnation restored* as a failure. Refused as written:
**that cycle is the mechanism**, and what distinguishes health from failure is whether it **converges**.
The claim is the hysteresis that decides which, so the trigger is re-aimed at *non-convergence* rather
than at *movement*.

## Consequences

- **`0163` tier 1 ships with the District as reach**, and its threshold is deliberately **coarse**. The
  two `plans/0002` §D2 numbers move to §D1 when chosen, and their ratifier is now *does the shop count
  converge*, not *is the threshold accurate*.
- **The claim writes to `DistrictPoolTable`**, keyed `(District, Good)` — the row that already exists.
- 🔴 **A second shipped Ruleset, `rulesets/oversupplied.toml`**, because condition 4 and tier 1 cannot
  hold in one world. ⚠ **It is a demonstration of the DEATH half and its numbers ratify nothing** —
  a city at its Lot ceiling is not a city in equilibrium. Its header says so.
- ⚠ **The threshold SATURATES and the cooldown is the only live dial.** Measured: `build_threshold_days`
  at 1, 2 and 4 gives the identical shop count, because unserved demand on this world is **bimodal** —
  a District is either serving or wholly unserved, so any threshold in the band lands in the same gap.
  `cooldown_days` 0 → 11 shops, 1 → 4, 2 → 2. ***So the number `0163` argued about is not the number
  that decides anything here***, and `plans/0002` §D1 records both with that caveat attached.
- 🔴 **`ZoneRuleEngine.Worst` is narrowed and `Condemn` gains a second walk.** Both halves ship together
  for the reason above. ✅ **`Worst`'s doc comment is CORRECTED IN PLACE rather than filed** — the
  sentence *"the premises' own Rules only"* was true when it was written and is now a paragraph saying
  so, which is what [`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
  asks of a description that went wrong about its **trigger**.
- ⚠ **With ONE District, the reach fork buys nothing** — *in reach* degenerates to *anywhere*, which is
  the tier-0 signal it replaces, correctly denominated. **The elapsed, per-Good and claimed forks still
  earn their keep.** ***This is a real hole in the early game and it is recorded rather than papered
  over***: the user raised it, and no mechanism closes it until a city has two centres.
- ⚠ **No District exists for the first `[districts] revisit_ticks`**, so no demand and no shops until
  then. It is the same cold start that was evicting Households on `provisioned.toml`.

## What would trigger revisiting

- **A Business being capitalised** — milestone **27**, `plans/0002` §D2's capitalisation band. That
  supplies condition 2, at which point the threshold may genuinely loosen toward nothing and this
  record's *may be loose, may not be removed* is re-opened.
- **Shopping becoming a Trip** — `0163` tier 2/3 with a Provider List. That supplies condition 3, makes
  position affect survival, and ***retires the District as a reach unit rather than ratifying it.***
- **A run in which the shop count does not converge.** Not churn — *non-convergence*. The claim and the
  threshold disagreeing is the diagnosis, and it is what milestone 26's acceptance run looks for.
- **A Lot supply that stops binding.** Condition 4's measurement was taken on a city at its **Lot
  ceiling**, so tier 0's over-supply and tier 1's restraint were being compared against a wall rather
  than against demand. A larger lattice, or a trade zone with more frontage, re-opens whether the two
  still pull against each other — and it is the cheapest of these triggers to pull.
- **A second trade kind.** Selection on count assumes the survivors are interchangeable; two kinds
  competing for the same Lots makes *which* survives a question this record has not reasoned about.
- **The early-game hole biting.** If a one-District city visibly stifles, the fork is a **finer reach
  unit for a young city only**, which is a special case this record deliberately does not take
  pre-emptively.
