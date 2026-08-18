# Upkeep leaves milestone 10, and its blocker is a Rule with no actor

**Upkeep is struck from milestone 10 and placed at 12, the District Pool.** Four things block it and each is independently sufficient, but the one nobody had written down is the fourth: [`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md) names Upkeep's **Rule family** and never names **what the Rule is attached to**, and a Segment is not an actor in any sense the engine has. `SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `HONEST DEGRADATION`

## Why

[`06`](../06-roadmap.md)'s milestone 10 row says it *"carries the Household balance sheet, Upkeep (`adr/0035`) and Policy's spend (`adr/0033`)."* The Household balance sheet and Policy's spend stay. Upkeep does not, and it is not close.

### 1. The loader already refuses it, by name, and has since slice 7

`RulesetLoader`'s **refusal 4** rejects any `[[rule]]` whose money terms do not sum to zero: *"every money term needs a counterparty. **A cost paid to nobody is a leak, not a cost.**"* Its own remark names Upkeep's case among the two it deliberately over-refuses:

> *"A wage — a Business paying the Household that works there — and **an import payment** both have real counterparties that **no scope can currently name**, so both would be refused. Neither is writeable anyway, and a refusal that says so is better than a leak that does not."*

`adr/0035` sends Upkeep's money to exactly there — *"bought from local Processing it becomes local wages; imported, it leaves through the gate"* — so Upkeep in milestone 10 is not awkward, it is **unloadable**. [`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) running in the direction it is meant to: the mechanism was opened, and the answer was already in it.

### 2. Neither term of the formula exists, and one of them is forbidden to authors

`construction cost ÷ effective life`:

| Term | State |
|---|---|
| **construction cost** | **No Ruleset key anywhere authors a cost of anything.** `adr/0035` denominates it in **Lane-Tiles**, on the ground that [`0016`](0016-the-lane-is-the-entity-not-the-car.md) *"already makes the Lane the entity, so no new quantity"* — and the Lane is milestone **21** |
| **design life** | `adr/0035`: *"**not authored by hand.** They are derived from **the share of a mature city's budget** Upkeep should occupy"* — and there is no budget and no mature city |

So one term has no unit and the other has a **derivation whose inputs do not exist**. Choosing either here is precisely what [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) refuses, and [`0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)'s direction says the answer is to wait for the derivation rather than to invent a value beside it.

### 3. The shape question is settled in general and unapplied to Upkeep

[`plans/0011`](../../plans/0011-rule-engine-bins-and-rules.md) finding 6 settled that the two money mechanisms are **different shapes rather than two spellings of one**: an explicit money term is a **transfer** — a tax, a wage, a subsidy — which names both ends and balances inside its own atomic Rule; [`0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)'s payment is a **purchase**, whose price is emergent, whose counterparty is implied by the scope, and which **has no syntax at all**.

⚠ **Upkeep is specified with a transfer's syntax and a purchase's semantics.** The formula is an **authored money amount**; the money goes to a supplier at a market price. It cannot be both, and `adr/0035`'s own title says which one it is — ***infrastructure is priced by what it consumes, never by a budget***. ***A document can state its principle in its title and contradict it in its formula, and the formula is what a builder implements.***

This is not settled here. It belongs to whoever builds Upkeep, because settling it decides whether the authored quantity is money or Materials, and that is the mechanism rather than its schedule.

### 4. The blocker nobody wrote down: an Upkeep Rule has no actor

`adr/0035` places Upkeep *"Mechanically … a **Sweep Rule** ([`0033`](0033-two-rule-families-scheduled-and-swept.md)) on the staggered slot `05 §9` already reserves — sweeping a population, which is that ADR's own discriminator."* That sentence names the **family** and the **cadence**. It never names **what the Rule is attached to**.

Every scope in the engine is resolved through one:

- `RuleEngine.Bin(World world, **int building**, in BinRef reference, RuleId rule)` — the signature itself;
- `Scope.Local` — *"Bins on the **Building** running the Rule"*;
- `Scope.Pool` — *"Bins on the **Building's** District Pool"*;
- and [`0114`](0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md) enumerated the owner kinds a Bin may have — **Building, Household, Business, treasury** — with no Segment, correctly, because **a Segment holds no money and never will**.

Upkeep's *subject* is a Segment, its *payer* is the treasury, and its *counterparty* is a market. **The subject, the payer and the counterparty are three different things and the engine has no Rule whose subject is not its payer.** Task 5's tax is the near miss that shows the gap is real rather than pedantic: it sweeps Households, and a Household is a payer *and* the subject, so it needs nothing new.

***Naming a Rule family says how often a Rule runs and never what it is attached to.*** That is `adr/0093`'s class on a new axis — its subject is a description being wrong about a **trigger**, and this one is a description being **silent about a subject** while looking complete, because a family and a cadence are what a reader checks for.

⚠ **This is the ground that survives the deferral.** Milestone 12 supplies the market and does not supply a Segment an actor. Placing Upkeep at 12 buys it a counterparty and leaves this open, so it is named here rather than assumed to dissolve.

### The schedule was already right and the table disagreed with it

`06`'s dependency graph carries the edge **Money → Upkeep, Policy, private capital**. Its milestone table then placed Upkeep **inside Money's own row**. ***An edge from A to B says B comes after A, and putting B inside A's row reads the edge as "at the same time".***

**Third sighting of a mechanism hidden inside another milestone's row**, after the District Pool inside milestone 3a — invisible to an inventory of unscheduled work because 3a reports as shipped — and milestone 10 having been two milestones wearing one number. In all three the row was true and the placement was the lie.

## Consequences

- **`06` milestone 10's row loses the Upkeep clause; milestone 12's row gains it**, with the two number preconditions and the actor question written beside it. The inventory row *Infrastructure pricing, Upkeep, design life, wear* moves from **Placed: 10** to **Placed: 12**.
- **No renumbering.** Upkeep moves into an existing row rather than becoming one, so `PROCESS.md`'s *inserting renumbers the unshipped tail* does not fire.
- **`adr/0035` gains a note** recording that it specifies a family and a cadence and no actor, and that its formula and its title disagree about the denomination.
- ⚠ **12 is the earliest and not necessarily the right one.** It is where the *counterparty* becomes nameable — `Scope.Pool` is the only market spelling the enum has. Ground 2 may bind later: design life needs a mature city's budget, which needs **15**'s wages. **Whoever picks Upkeep up re-checks grounds 2, 3 and 4 before starting**; only ground 1 is discharged by arriving at 12.
- ⚠ **There is no import scope**, though `adr/0050` lists *"local, Pool, import"* as the cases reachable today. Four scopes exist — `Local`, `Pool`, `Global`, `Map` — so an import payment has no spelling in any milestone until somebody adds one, which is **11**'s business. Recorded here because it was found while checking whether 11 could host Upkeep, and it is a fact about the enum rather than about Upkeep.
- **Milestone 10 keeps everything else `06` assigned it.** Only the row that could not load leaves.

## What would trigger revisiting

- **A construction cost landing before 12.** `adr/0091`'s compulsory-purchase price is the likeliest candidate for the first authored cost in the project, and it would give ground 2 half an answer.
- **A Rule whose subject is not its payer being built for some other reason.** That discharges ground 4, and Upkeep should be re-read against it the same day rather than waiting for its own milestone.
- **The transfer-versus-purchase shape being decided for Upkeep specifically.** If the authored quantity turns out to be **Materials**, `adr/0035`'s formula needs rewriting and this ADR's ground 3 becomes an amendment to it rather than a caveat.
- **Milestone 12 shipping without `Scope.Pool` resolvable from a Segment.** Then the placement is wrong and Upkeep moves again, and it should move to whichever milestone gives its subject an actor, not to the next one along.
