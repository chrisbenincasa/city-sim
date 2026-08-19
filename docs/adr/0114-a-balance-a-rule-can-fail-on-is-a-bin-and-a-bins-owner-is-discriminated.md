# A balance a Rule can fail on is a Bin, and a Bin's owner is discriminated

**Money an actor holds lives in a Bin, not in a column, and `BinTable.Owner` becomes a discriminated handle — Building, Household, Business, or the treasury.** The reason is not storage: it is that a Bin is the only thing a Rule can **blame** and the only thing a blocked Rule can **wait on**. **`HouseholdTable.Savings` is deleted**, because it stores a stock where every design sentence describes a threshold. `LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM` `HONEST DEGRADATION`

## Why

### The failure surface is what a Bin is for, and it is denominated in Bin slots throughout

[`0065`](0065-a-bin-holds-a-long-and-unbounded-names-a-ceiling-whose-approach-is-a-defect-rather-than-a-refusal.md) left this open in as many words — *"what this does not settle: whether money belongs in a Bin at all"* — and treated it as a question about representation. It is not. Read `RuleEngine.Check`:

- terms accumulate into `_touchedBin[]` / `_touchedDelta[]`, keyed by **Bin slot**, deduped by slot identity (`RuleEngine.cs:876-893`);
- affordability tests read `Bins.LevelAt(bin)` and `Bins.Capacity[bin]`;
- failure returns `RuleVerdict.Stopped(instance, rule, bin, blocking)` — **the blame target is a Bin slot**;
- the sleeper is queued on that Bin's `SupplyHead`/`SupplyTail` (`BinTable.cs:63-66`), and `World.Drain` wakes it on a **write to that Bin**.

A balance held in a column is therefore unreachable in four separate ways at once: a Rule short of money cannot say what stopped it, has no list to join, is woken by nothing, and reports no `Blocking`. ***A balance a Rule can fail on is a Bin, because the failure surface is what a Bin is for.***

This is decisive for [`0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md), whose entire diagnostic payoff is the two-Bin split — the Pool empty is *input starvation*, the buyer unable to afford it is **bankruptcy** — *"two Bins, two blame targets, two sentences from Evidence"*. With money in a column there is one blame target and the second sentence cannot be written. And it is what [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) is buying: *"bankruptcy becomes a distinct diagnosis from input starvation."*

**The corpus already said so and the build was the outlier.** `CONTEXT.md` → Bin: *"Bins live on Buildings, on Districts (as Pools), and globally (the treasury)"* and *"the Household's Money is a `long` and money is a Resource held in a **Bin**"*. [`04 §2`](../04-economy-and-goods.md): *"**Money is a Resource too, and its Bin is unbounded.**"* `HouseholdTable.Money` as a `Column<Money>` disagreed with all of it.

### A money Bin cannot be owned by a Building, and that is what forces the discriminator

`BinTable.Owner` is a `HandleColumn<Building>` (`BinTable.cs:59`) — typed, saved, and a Bin's only ownership. Keeping it is not available:

**An unhoused Household holds money.** `World.EvictToPool` deliberately does not touch `Money` or `Savings` ([`0054`](0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md)), `CONTEXT.md` → Eviction says an evicted Household *"arrives with its Money and Savings intact — losing a dwelling is not losing what you own"*, and `adr/0024`'s destitution argument turns on a Household that *"cannot afford to move"*. A Household in the Unplaced Pool owns no Building and must still hold a balance, so a Building-owned Bin cannot express it.

**And the treasury is nobody's Building at all** — `RuleEngine.cs:813-817` refuses `Scope.Global` on exactly that ground: *"a city-wide Bin is one no Building owns, and where it would live is an entity decision."* This ADR is that decision: the treasury is a Bin whose owner kind is *treasury*.

### One discriminated column beats a table per owner, and the argument is where the tag is paid

The alternative is a Bin table per owner type, keeping every handle homogeneous. It does not remove the discriminator — **it moves it into the hot path**. `_touchedBin` is a flat `int[]` of slots and `Touch` dedupes by comparing that `int`; with several Bin tables it becomes a `(table, slot)` pair, compared on every term of every evaluation, inside the loop `02 §4`'s evaluation counter exists to price. One saved byte per Bin row is paid once at rest; a widened key is paid per term per evaluation for ever.

It also duplicates the wait-list machinery — `SupplyHead`, `SupplyTail`, `SpaceHead`, `SpaceTail`, `Drain`, `Requirement`, `BinStillBlocks` — which is the thing a Bin *is*. ***Splitting a table to keep a handle typed duplicates whatever the table was for.***

### Savings is a threshold that was stored as a stock

`HouseholdTable.Savings` is a second `Saved<Money>` column whose whole justification is its own summary line, *"Money set aside."* Nothing in the design asks for a second account:

- `adr/0024`: a Household *"holds a **reserve sized by its Life Stage** — a Young Household saving toward forming a family needs a deeper buffer than an Empty Nest"*, and *"**saving has a purpose and therefore a ceiling**."* A reserve *sized by* something is a target.
- Its own revisit trigger: *"examine the **savings buffer**, since that is where **velocity** is set."* A buffer that sets velocity is a propensity to spend.
- `CONTEXT.md`: *"Savings drain, discretionary spending stops first, the housing search widens, and destitution is the terminal condition"* — which is the narrative of **one** balance falling.

So a Household has one pool. What varies is how much of it the Household will spend, which is a **reserve level** derived from the Ruleset in force through the Household's Life Stage — [`0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s disposition rule on a sixth axis — consulted by the spending decision. *Savings draining* becomes an **observable** of the balance falling below that reserve, which is [`0102`](0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md)'s *a comparison the Household re-runs* rather than a new mechanism.

⚠ **The reserve is not built here.** Nothing spends discretionary money yet — the Provider List and the shopping occasion are milestone **14** and Life Stages are **20** — so choosing a reserve size now would be a hash-bearing number with no consumer and no ratifier, which is what [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) exists to refuse. It arrives with the mechanism that reads it.

⚠ **The direction of the error is worth keeping.** `adr/0054`, `adr/0068` and `CONTEXT.md` → Eviction all write *"Money **and** Savings"* as a pair — correctly, because by then two columns existed. ***A threshold stored as a stock reads as a second account, and every document that later names the pair inherits it.*** This is [`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) running from the code **into** the documents rather than the other way round, which that ADR does not consider: its whole subject is prose being wrong about the build.

## Consequences

✅ **BUILT 2026-08-19** — [`plans/0031`](../../plans/0031-conserved-money-and-the-treasury.md) tasks **1** (the discriminator and the treasury) and **4c** (the actors). Three amendments this ADR owes to its own build:

⚠ **It underdetermined how a Bin names an actor, and the consequence below is written for a shape that was not built.** *"Two owners of different kinds may share an id, so the kind is part of the value"* presumes **one** handle column addressing four tables. Task 1 kept `BinTable.Owner` typed to `Building` and folded the kind as a separate column — satisfying the hash consequence a different way — on the ground that *"the treasury has no owner row to point at in any case"*. That ground holds for a **singleton** and expired the moment a Household needed naming. Task 4c settled it the third way: the link is a saved `HandleColumn<Bin>` **on the actor**, so `BinTable` is untouched, a rebuild has nothing to do, and the lookup is O(1). ***A Building holds many Bins because its kind declares many Resources; an actor holds one because money is one Resource*** — the asymmetry is cardinality, not carelessness.

⚠ **A balance is conditional on the Ruleset in force, which this ADR does not say and which is its largest practical consequence.** A Bin exists only for a declared Resource, so a Household in a world whose file names no money **cannot hold any** — where `HouseholdTable.Money` held it whatever the file said. All five shipped Rulesets therefore gained a money `[[resource]]` block, and every code-built fixture that endows anybody had to start declaring one. ***Making a quantity conditional on the Ruleset turns every fixture's Ruleset into a statement about what that fixture can test.***

⚠ **An actor holds at most one Bin, not one per Resource.** The final consequence below says *"at most one Bin per Resource, since `FindBin` is keyed (owner, resource)"*; a single saved handle is tighter than that, and `World.TryMoneyResource` **throws** on a second conserved Resource rather than balancing on the first — which would leave every actor holding one currency and none of the other while every conservation sum still added up, because money in a Resource nobody can hold is money nothing can lose. That is this ADR's third revisit trigger being demanded rather than defaulted.


- **`BinTable.Owner` gains an owner kind**; `Building`, `Household`, `Business` and `Treasury` are the four. `World.FindBin` takes an owner rather than a Building slot, and `Scope.Global` resolves to the treasury Bin instead of throwing.
- **`HouseholdTable.Money` and `.Savings` are both deleted**; a Household's balance is its money Bin. The State Hash moves and all three golden baselines re-record.
- **`adr/0065`'s open question is closed**, and closed on a ground that ADR did not consider — it weighed width, arithmetic and denomination, and the answer came from the wait list.
- **`adr/0050`'s bankruptcy diagnosis becomes buildable**, which is the milestone's reason for being.
- ⚠ **The hash folds the owner kind as well as the owner id.** A handle column folds the target row's monotonic never-reused id; two owners of different kinds may share an id, so the kind is part of the value or two distinct Bins fold identically.
- ⚠ **`MoneyIsRepresentable` must be rewritten rather than retargeted.** It sums `Households.Money` and `.Savings` by slot (`WorldInvariants.cs:621-640`); both columns are gone and the sum is now over money Bins.
- ⚠ **An actor may hold at most one Bin per Resource**, since `FindBin` is keyed `(owner, resource)`. This is what makes Savings-as-a-second-money-Bin unavailable, and it is a constraint rather than a coincidence.

## What would trigger revisiting

- **A profile showing the owner-kind branch in `FindBin` costing anything measurable.** The claim here is that one saved byte at rest beats a widened key in the dedup loop. It is **measurable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) — the number is the change in the cost of **one evaluation**, read against whichever unit is current when it is taken — and nothing has measured it, because no Ruleset moves money yet. ⚠ **Do not price it against a remembered evaluation cost.** The corpus carries several and they are not the same quantity: the flat synthetic unit, and the in-situ unit a real world produced, which came in several times higher and is attributed in `plans/0011`'s findings. ***A unit cost is a hypothesis until a real world has produced one***, and a money term will change the working set that argument turns on.
- **The treasury's wait list showing up in a profile.** [`0033`](0033-two-rule-families-scheduled-and-swept.md) already names the response and it is not this ADR: *"give Districts their own budget Bins, not to reintroduce polling."*
- **A genuine second balance on one actor** — an escrow, a debt account, a restricted fund. It would need a second Resource in the money family, and `adr/0031`'s family table lists no member for money at all, so the first one is a decision rather than a detail.
- **The reserve turning out to need saved state** when the spending mechanism is built. This ADR asserts it is derivable from Life Stage and the Ruleset in force; if a Household's reserve has to *remember* something no function of its stage recovers, that is [`0101`](0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md)'s *a value drawn once is derivable and a value measured once is not*, and a column comes back — but as a reserve, never as a second pile of money.
