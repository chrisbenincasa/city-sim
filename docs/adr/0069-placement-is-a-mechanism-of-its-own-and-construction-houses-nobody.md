# Placement is a mechanism of its own, and construction houses nobody

**`02 §5.2` step 2 — Household placement — is a mechanism distinct from construction: a sampled pass in
Phase 6, running *ahead* of the Zone Rules' own sample, that drains the Unplaced Pool into vacant
declared capacity in Buildings that already stand. It is blind today, on
[`adr/0054`](0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md)'s existing
reasoning and at its site rather than at a new one. A Zone Rule raises a Building and houses **nobody**;
placement fills it over the following Days.**
`EMERGENCE` `LEGIBLE CAUSE` `HONEST DEGRADATION`

## Why

**The mechanism was invisible, and two ledger entries named a number in its place.** `World.Place` has
exactly one caller in the tree, inside `ZoneRuleEngine.Create`, at the instant of construction. Of
`02 §5.2`'s six steps only **step 5, development**, exists at all — so nothing takes a Household out of
the Pool and puts it into a Building that already stands, and the Zone Rule has been doing placement's
job as a side effect, one Household deep.

That is what the *five-sixths homeless* equilibrium is. It was filed twice —
[`0002`](../../plans/0002-open-questions.md) §B and §C — as a question about **occupancy**, and §B states
outright that *"the number that settles it is an occupancy declared in a `[[kind]]`"*. **It is not, and
no number is**: declare three and each demolish-and-rebuild cycle still evicts three and re-houses one,
because the only door into a dwelling is its own construction. **Both filings named a number because the
missing mechanism was invisible to them**, which is why the entry could not be typed once — the two
sections were not disagreeing about the *type*, they were agreeing about the wrong *subject*. This is
[`plans/0012`](../../plans/0012-corpus-audit.md) *Cause 1* on a third axis: a mechanism's absence read as
a design constraint, and the constraint then generating positions.

**It introduces no new blindness anywhere.** `adr/0054` already established that draining the Pool
without an acceptance test is acceptable in this build, and stated exactly why — *"acceptance needs rent,
a commute and a tolerance; a Household that would refuse this dwelling is a thing this build cannot
express, and pretending otherwise would put a number in a file that nothing had measured."* This moves
that same draw to a second site. It does not weaken the argument and does not extend it.

**Separating them is what makes the eventual upgrade land in one place.** `02 §5.2` step 2b is a hard
filter — *affordable? at least one reachable job in budget?* — and it needs a price surface and a Commute
Budget, which are the choice model's and milestone 9a's. When it arrives it replaces the blindness
**inside the placement pass**, rather than being retrofitted into `ZoneRuleEngine.Create`, where it has
no business being and where `02 §5.8`'s *never resolve a route inside the choice loop* would meet it in
the wrong phase.

**The ordering is not a detail, and it makes the create predicate better than it is today.** `02 §5.6`
records that creation *"drains the signal that authorised it, so no Ruleset can build past its demand
however wide its sample"* — a self-limiting property that plainly cannot come from a construction step
that houses nobody. **The ordering supplies it, and supplies it more honestly.** Placement runs first, so
a Household still in the Pool when a Zone Rule samples is a Household **the standing stock could not
house** — which is the *residual* `§5.2` calls the demand signal, in its own words. The create predicate
stops being a statement about population and becomes one about **vacancy**: a developer does not build
while there are empty flats.

That is strictly stronger than what shipped. Today's predicate reads a Pool that construction drains one
Household at a time, so a wide sample can build ahead of demand by up to the sample size within a single
trigger. Post-placement, the Pool it reads is a true residual and cannot.

**Its shape is the one the design already specifies, and it is not a new Rule family.** `§5.2` puts the
loop on *"a slow cadence — a matter of Days rather than a Tick"*; `§5.3` makes sampling **a behaviour
model rather than an optimisation**, which is the swept shape
[`adr/0033`](0033-two-rule-families-scheduled-and-swept.md) already has. Nothing here needs a third
family, and `adr/0033`'s warning about moving a mechanism between families for performance is not
engaged: this mechanism has never had a family.

## Consequences

- **Phase 6 gains a step, ahead of the Zone Rules.** `02 §1.1` says the phase ordering *"is not an
  implementation detail — it is the determinism contract"*, so this is hash-bearing by construction and
  the ordering is the decision rather than a scheduling convenience.
- **`ZoneRuleEngine.Create` stops calling `World.Place`, and `PurposeTag.PoolDraw` moves with it.** The
  create predicate still reads `UnplacedPool.Count`; it no longer draws from it.
- ~~**The demolish-and-rebuild cycle balances with nothing tuned**, because eviction and re-housing use
  the same door. That is what closes the five-sixths equilibrium, and it closes it without choosing a
  number.~~ **Struck by the build; see *What building it found* below. Both halves were wrong — the
  equilibrium does not close, and three numbers had to be chosen.**
- **A Building may stand empty, and that is legal.** `HouseholdHomeExists` was already qualified by
  `adr/0054` to *a Household is housed or is in the Pool*; nothing in the invariant set constrains a
  Building from the other side.
- **Vacancy becomes a quantity the city holds.** `CONTEXT` → Frontage lists four `Evidence` answers for
  why a *Lot* is vacant; this is the first time an empty **dwelling** is a thing with a count, and it is
  what a later acceptance filter will be scored against.
- **What this does not do**, deliberately: no acceptance filter, no sampler bias, no scored choice, no
  `μ`. Those are `§5.4`'s and 9a's, and this ADR would be trespassing if it named them.
  [`adr/0054`](0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md) declined the
  same trespass for the same reason.
- **`02 §5.6`'s self-limiting note is superseded and needs amending in place**, since the property now
  comes from the ordering rather than from creation. → [`0012`](../../plans/0012-corpus-audit.md).
- **The pass's pacing is derived rather than chosen, if
  [`adr/0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)'s
  precedent holds**: author a revisit period over the Pool and derive the count, in which case `0002` §D
  gains no row. The precedent is named here and the derivation is owed to whichever slice builds it.
  **Half right.** The `sample` is derived, and that half held. But the *duration* it is derived from is a
  free number and so is `candidates`, so `0002` §D gained **three** rows rather than none.

## What building it found

**Four corrections, recorded here rather than amended away, because three of them are this ADR being
wrong about what it was about to build.**

**1. The equilibrium does not close, and saying it would was the same error this ADR was written to
name.** *Five-sixths homeless* was 83%; it is now **53%**, and the residue is not a mechanism gap — it
is `rulesets/minimal.toml` demolishing every dwelling it raises, which that file's header states at
length and on purpose. **What the pass actually fixes is vacancy**: before it, **45% of the housing
stock stood empty** while 70% of the population queued; after it, **10%**, which is the floor a city
that is continuously building carries. So the acceptance test asserts **vacancy and not homelessness** —
`PlacementLongRunTests` — because *everybody is housed* is a property of a Ruleset's **balance**, and
the shipped Ruleset explicitly declines to have one. **Predicting a content outcome from a mechanism
change is `adr/0070`'s error running forwards instead of backwards.**

**2. The draw is over Lots, not over Buildings.** The first implementation sampled
`Buildings.Rows.SlotCount`, which is a **recycling** table: under the shipped Ruleset roughly 55% of
Building slots stand freed at any instant, so three candidates bought about **1.3** real looks and
lowering the demolition rate would have silently raised the effective candidate count. A Lot is a place
in the city and the Lot table's slot count is the size of the city, so `candidates` means what the file
says. A look landing on a vacant Lot **found nothing**, which is a thing that happens to somebody
looking for somewhere to live.

**3. Three numbers, not none.** `interval`, `revisit_ticks` and `candidates` are all hash-bearing and
all now in [`0002`](../../plans/0002-open-questions.md) §D2. `revisit_ticks` shipped at **8192** — one
Day, copied from `adr/0059`'s derived default — and that value left **45% of the stock empty**, because
a Day is the cadence at which the *development industry surveys the city* and a family without a home
looks more often than that. It is **1024**, eight occasions a Day, chosen against that measurement and
unratified.

**4. The Census gained a fourth metric family, and writing it exposed that the third has no test.**
`considered` and `placed` are two flows for the reason `evaluations − due` are: a queue being looked at
and not housed is a city out of dwellings, and a queue not being looked at is a mechanism that has
stopped, and one counter cannot tell them apart. Nothing in the suite reads a `ZoneCounter` back
through a `Census` — `adr/0064`'s id-29 shape, a block written and never read — so the placement family
ships with one and the Sweep family's gap is filed.

## What would trigger revisiting

- **The choice model arriving** (`02 §5.4`). That is when the blindness ends, and this is the site it
  lands on — the whole point of separating the two.
- **Immigration** ([`adr/0023`](0023-immigration-arrives-through-the-gate.md), milestone 9a). The Pool
  stops being a subset of a population fixed at world creation, the give-up bound becomes load-bearing,
  and a placement pass draining a Pool that is being refilled is a different mechanism from one draining
  a Pool that is not. This is `adr/0054`'s own named trigger, inherited.
- **A Ruleset whose content makes the residue interesting.** The equilibrium above is measured against
  a fixture that demolishes its whole housing stock on purpose, so *53% homeless* says almost nothing
  about the design. The first Ruleset that models a city is what makes the number mean something, and it
  is also what would ratify the three `0002` §D2 rows.
- **The residual lagging badly enough that Zone Rules still overbuild.** If a slow placement cadence
  means the Pool the create predicate reads is stale by more than a trigger interval, the cadence becomes
  load-bearing and wants a number and a ratifier rather than a derivation.
- **Placement proving to want a Household's own Event Wheel entry** rather than a sweep. A Household that
  is looking is an entity with something to do, which is the Wheel's own criterion
  ([`adr/0056`](0056-the-event-wheel-is-two-levels-ticks-and-days.md)); the sweep is chosen here because
  `§5.3` makes sampling behavioural, and if that argument weakens the scheduled family is the fallback.
