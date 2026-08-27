# A decline threshold is a duration, and the premises and the tenant get one each

**A Ruleset authors `condemn_after_days` and `tenancy_ends_after_days` — two independent durations in
Days, on the `[[building]]` kind — and `RulesetLoader` converts each to Ticks at the parse site. This
supersedes [`adr/0053`](0053-failure-pressure-is-a-duration-not-a-tally.md)'s
choice of *missed firings* as the authored unit and finishes
[`adr/0141`](0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)'s split, which
separated the verdict and left both verdicts reading one number.**
`LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM`

⚠ **The old key is refused by name at the parse site**, because the two units differ by the Rule's own
`rate` and a file that kept `condemn_after = 4` would load clean and decline **sixteen times too
slowly** on the shipped `upkeep`. ***A silent unit change is `plans/0012` Cause 5 arriving in content
rather than in prose.***

---

## Why

### The authored unit hid the felt quantity, and the tree proves nobody could see it

`adr/0053` chose the firing count for a reason that is correct as far as it goes: a Rule fires every
`rate` Ticks when healthy, so `N` missed firings is dimensionless and **immune to a Ruleset that
retunes every rate and would otherwise have silently retuned every Building's lifespan.** That property
is real. It is also the wrong half of the problem.

🔴 **`condemn_after = 4` against `upkeep`'s rate of 16 is 64 Ticks — forty-five in-world minutes — and
nothing on the page said so.** It stood at **4** in all eighteen shipped Rulesets and **no author had
ever written a different value.** That was read once as
[`adr/0164`](0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md)'s *would a
designer ever set this?* answered by the tree, and the reading was wrong in an instructive way: it is
not a key nobody wants, it is **a key nobody could read**. A designer writing `4` had no way to know
they had written three quarters of an hour, and the value that got shipped everywhere was whatever the
first author happened to type.

⚠ **The corpus already had the rule and had applied it everywhere else.**
[`adr/0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md) and
[`adr/0130`](0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md) say
***author the duration and derive the count***, in as many words, and `0130`'s reason is the mirror
image of `0053`'s: *authoring the count would make the felt quantity move whenever a cadence was
retuned.* `gives_up_after_days`, `collapses_after_days` and `revisit_ticks` all follow it.
`condemn_after` was the single key that went the other way, and it went that way first.

**So the repair is not new reasoning; it is the corpus's own rule reaching the one key it had missed.**
A Day is the coarsest unit that makes the mistake unwritable: the shortest decline a Ruleset can now
author is 2,048 Ticks, and forty-five minutes is not expressible at all.

⚠ **`adr/0053`'s property is not discarded, it is relocated.** Retuning a cadence still must not move a
lifespan — and now it cannot, because the lifespan is stated directly and the cadence is not in it.
What *is* lost is the ranking's independence from rate, and that is kept deliberately: see
*Consequences*.

### One threshold could not express a world the design already describes

`adr/0141` split the **verdict** — a tenant's Failure Pressure ends the tenancy, the premises' condemns
the Building — and stopped at the number. `ZoneRuleEngine.Condemn` read `condemn_after` once and used
it for both subjects. The engine's own doc comment defended this, and it was **nearly right**:

> *"a tenant has no kind to declare its own — a Household never will, and a Business gets one at
> milestone 27 — so a second threshold would be a number with nowhere to be authored."*

The premise holds and the conclusion does not. A tenant threshold has nowhere to be authored **on the
tenant** — so it is authored on the **premises** kind, which is `adr/0141`'s own answer to the identical
problem about Bin capacity: *a shop holds what fits in the shop, and what is in it is the shopkeeper's.*
It is a property of the **lease** rather than of the tenant, and it is the one thing `0141` left
unassigned.

🔴 **The cost of the missing number was invisible until decline was stripped out of the shipped
worlds.** Removing `condemn_after` from every file that demonstrates something else also removed every
tenancy that ends — and ***`rulesets/evicted.toml`, the one shipped world whose entire purpose is a
tenancy that ends, silently stopped ending any***, with every test green except its own. **A key doing
two jobs is not a tidiness defect; it is a world the Ruleset cannot describe.**

## Consequences

**The Ruleset states Days and `KindDefinition` holds Ticks.** `RulesetLoader.InTicks` multiplies once,
which is [`adr/0048`](0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md)'s
division doing what it is for. ⚠ **A fixture built in code may still hold a sub-Day duration**, and
nearly every decline test does — one that had to run a whole in-world Day to watch a Building fall down
would be a four-minute assertion.

**Two refusal sites are added** ([`adr/0048`](0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md)'s count 175 → 177) and the old key is refused by name with
its unit change spelled out.

⚠ **The threshold and the ranking are now denominated differently, and that is deliberate.**
`ZoneRuleEngine.Worst` still cross-multiplies against each Rule's `rate` when choosing *which*
condition to name in the trail. They answer different questions: *should this be condemned* is about how
long the thing has been broken, and a wall clock is the honest unit; *which condition do we name* is
about severity, and a Rule due every 8 Ticks silent for 100 is more starved than one due every 32
silent for the same 100. Collapsing both onto one unit would make the reported cause depend on cadence,
which is `plans/0012` **Cause 5** waiting to happen in the Evidence panel.

**Two demonstration worlds become writable that were not.** `rulesets/declining.toml` states the
premises threshold alone; `rulesets/evicted.toml` states the tenant's alone. ***Neither could be written
before the split.*** ⚠ **`rulesets/diagnosed.toml` must carry `declining.toml`'s exact value**, because
it is that file plus one `on_fail` key and `EvidenceDumpTests` asserts every count agrees — a differing
threshold makes them two cities and the test compares worlds instead of columns.

**Measured, at 10,000 Citizens over 65,536 Ticks on `declining.toml`:** 602 built, 385 abandoned, 387
vacant — **39% blight against the old unit's 65%**, with live stock **313 → 602**. ⚠ **Quote the
population with the census**: the 65% figure was taken with `--citizens` unset, which `Options.cs`
defaults to 10,000, and was written up as 1,000 in three documents (`plans/0012` Cause 5).

### 🔴 Neither number could be ratified when this was written, and `Scope.Pool` has since SPLIT the reason rather than removing it

**No shipped world can express balance → unbalance → balance.** `plans/0002` §D1 records both
thresholds as unratified with a **prior condition** attached rather than a ratifier alone: *a world in
which a Building can recover.*

`RuleEngine.Stop` zeroes `StarvedSince` on any blocking reason other than supply, so **the recovery path
has always existed** — a Building whose supply returns is healed completely and immediately, with no
decay term and no second mechanism. It had never been reachable, because no shipped Ruleset could put
anything into `repairs`. `rulesets/maintained.toml` now does, and it is one Rule long.

⚠ **But it lands on the opposite pole rather than in the middle.** A premises Rule needing a scarce
input has five places to draw on and every one is shut — ⚠ **`pool` was reopened the same day and the
row below says how; read the amendment under this table before quoting the sentence you are reading**:

| Source | Status |
|---|---|
| `local` — another premises Bin | **Circular.** The chain must bottom out in a no-input Rule, and that Rule never fails |
| `pool` — the District market | 🟢 **SHIPPED 2026-08-26, and it answers the tenant and REFUSES the premises.** A pool input expands 1:3 — Good from a seller, money from the buyer's purse, money to the seller's till ([`adr/0167`](0167-a-purchase-picks-its-seller-by-a-draw-and-waits-on-the-market-rather-than-on-a-shop.md)). A **tenant** can therefore fail on a shortage it does not control. A **premises** Rule with a pool term ***throws*** at `RuleEngine.Buy` — *a Building never holds money, so this is the landlord shopping* — which is [`adr/0113`](0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md) arriving as a runtime refusal rather than a gap |
| `global` — the treasury | **Money-family Resources only**, and [`adr/0113`](0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md) says a Building never holds money — so a premises Rule cannot buy anything |
| `map` — a Layer read | **Write-only by construction.** A Layer cell has no capacity to exceed, so a map term can never fail and no Rule ever waits on one |
| the tenant's Bins | **Refused at the parse site.** A Rule with two owners has no subject to run on, and *a term crossing an ownership boundary is a trade, which is `pool`* |

***So a premises Rule chain today is always-succeeds or never-succeeds, and there is no authorable
middle.*** `declining.toml` is one pole — every dwelling condemned with probability 1, the threshold
setting only how long it takes — and `maintained.toml` is the other, at zero condemnations over 32,768
Ticks with the worst Building in the city at one missed firing.

⚠ **Under [`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) every one of those five
is *unbuilt* rather than *refused*, so none of it is evidence about the design** — and the answer to
*given scarcity does not exist, should the threshold compensate?* is **build scarcity**. ***A threshold
measured in a world where failure is certain, or impossible, is measuring a stopwatch and not a
design.*** Routed to `plans/0002` as a question against Phase 2 rather than worked around here, per
[`adr/0073`](0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md).

### 🟢 AMENDED 2026-08-26, the same day, when `Scope.Pool` landed — and the two thresholds part company

**The paragraphs above were written against a build in which `pool` threw for everybody.** Milestone 26
task 4 shipped it, and the row that changed did not change symmetrically. ***One threshold got its
middle and the other got a refusal***, so *"neither number can be ratified"* is no longer one sentence
about two numbers.

**`tenancy_ends_after_days` now has a middle, and it is the one this ADR predicted.** A tenant's Rule
with a `pool` input fails on `Blocking.Supply` when the market is short of the Good, and
`RuleEngine.Stop` arms `StarvedSince` on `Supply` and on nothing else — so the tenant's Failure Pressure
clock now runs on **scarcity the tenant does not control, varying between Buildings and over time**,
which is the *partial answer that would count* named in `plans/0002` §A verbatim. ⚠ **What is still
missing is a WORLD and no longer a mechanism**: `rulesets/provisioned.toml` is the only file with
sellers in it, and it must both run and go **short**. A market that always clears measures nothing, for
the same reason `maintained.toml` measures nothing.

🔴 **`condemn_after_days` did not get a middle; it got a REFUSAL, and that is a harder answer than
waiting.** A premises Rule with a `pool` term throws at `RuleEngine.Buy`, which spells the reason out:
*a balance is a Bin belonging to a Household or a Business, and a Building never holds money, so this
is a PREMISES Rule with a pool term — the landlord shopping.* That is
[`adr/0113`](0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)
being enforced rather than a gap being left.

⚠ **So the premises row moves from *unbuilt* to *refused*, and under `adr/0070` that is the one
classification which IS evidence.** The four remaining sources are unchanged — `local` circular,
`global` money-family, `map` write-only, the tenant's Bins refused at the parse site — and with `pool`
now closed *by decision* rather than by absence, ***there is no route by which a premises Rule can fail
on anything outside its own Building, and the design says so on purpose.***

***This stops being a question about when scarcity ships and becomes a question about what a premises
Rule is for.*** Three readings are available and this ADR picks none of them: that a Building's decline
should be driven by its **tenant's** state rather than by its own Rules failing, which would make
`condemn_after_days` a reading of the lease; that the premises need a non-money input a landlord could
plausibly hold, which needs a Resource family that is neither Good nor money; or that `adr/0113` is
right and **a premises threshold measured on Rule failure is measuring the wrong thing entirely**. It
is an argument, it is now unblocked, and it is not a measurement — [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
does not reach it, because the number that would refute it cannot be produced by any world the build
can express.

## What would trigger revisiting

- ~~**`Scope.Pool` shipping**, which is the first time a premises Rule can fail on something outside the
  Building. That is the world both thresholds are owed a measurement in, and it is the trigger that
  matters — everything below is smaller.~~ 🟢 **FIRED 2026-08-26 and it was HALF right** — see the
  amendment above. It gave the tenant threshold its middle and refused the premises one outright, so
  what replaces it is two triggers rather than one.
- **A world in which `rulesets/provisioned.toml`'s market goes SHORT.** That is what
  `tenancy_ends_after_days` is owed a measurement in, and it is now content rather than engine. ⚠ **The
  file does not run at all as of this merge** — `adr/0164` left it with no inflow to its Unplaced Pool,
  so no shop is ever raised and there is no seller; a market that never opened cannot be short.
- **An argument about what a premises Rule is for**, which is what `condemn_after_days` is now blocked
  on. Not a measurement, and no world settles it.
- **A Business becoming a tenant with its own kind.** `adr/0141`'s second namespace would then have
  somewhere to author a tenant threshold, and *the premises own the lease* is worth re-arguing on the
  day it stops being the only available home.
- **A second Failure Pressure source arming.** `RuleEngine.Stop` arms on `Blocking.Supply` alone, where
  `CONTEXT.md`:296 names three. A threshold denominated against one source may not be denominated
  correctly against three, and the two that are missing — Trips failing, and conditions below tolerance
  — are not supply-shaped.
- **The first threshold shipping** — `CONTEXT.md`:296's *loses occupancy and quality*, which is
  `plans/0046` task 3. A decline chain with an intermediate stage a Building recovers from changes what
  the second threshold is measuring, because it would no longer be the only thing standing between
  healthy and abandoned.
- **Anything making a Day the wrong grain.** The unit is chosen so 45 minutes is unwritable; if a
  designer ever has a real reason to author a sub-Day decline, the floor is the thing to re-argue and
  not the direction.
