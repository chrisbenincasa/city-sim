# A standing cost needs a counterparty, so a trade pays rates until there is a supplier to pay

**A Business carries one recurring money cost, and it is a **levy to the treasury** — a `local` money
input and a `global` money output on a Rule owned by the trade. ***The counterparty is chosen because
one is structurally required and not because rates are the truest cost***: `adr/0024` conserves money,
so a cost paid to nobody is refused at load. **Cost of goods to a supplier is the successor**, and it
is named here so that the stand-in has an end rather than a tenure.**
`LEGIBLE CAUSE` `HONEST DEGRADATION` `SOLVE THE ACTUAL PROBLEM`

**Taken by the user on 2026-08-26 during milestone 26 task 7**, from three offered counterparties, as
*rates now, goods later*. The level is a separate decision and a separate ledger row — see
*Consequences*.

---

## Why

### A shop that nobody buys from is immortal, and that is the actual problem

A trade's Rules are its own as of
[`0166`](0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md), and the shipped
`provisioned.toml` grocer had two: it restocks from the Hinterland and it sells. Neither can end it.

`RuleEngine.Stop` **clears the failure-pressure clock for every blocking reason but `Supply`**, and it
does so deliberately — [`0053`](0053-failure-pressure-is-a-duration-not-a-tally.md)'s pressure is
*going short*, and a full Bin is what a well-stocked Building with nobody to buy from looks like. So an
unsold shop stops on `Blocking.Space`, its clock resets every time, and ***it stands full and solvent
and dead for the life of the city.***

⚠ **This is not a gap in decline; it is decline working on the wrong verb.** The build can already end
a tenant that goes short. What it had no instance of was a trade that *consumes* anything, so there was
nothing for it to go short **of**. ***The missing mechanism was a cost, not a threshold.***

### Money is conserved, so the counterparty is a structural requirement and not a modelling choice

[`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) conserves money, and
`RulesetLoader`'s **refusal 4** is where that becomes a rule an author meets: it rejects any `[[rule]]`
whose money terms do not sum to zero, in its own words — *"every money term needs a counterparty. **A
cost paid to nobody is a leak, not a cost.**"* ⚠ **Quote the refusal and not the record**: the sentence
is the loader's message, cited by
[`0117`](0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md), and `0024` argues
conservation without ever putting it that way. A `local` money input with no matching output is
therefore refused where the Ruleset is parsed
([`0048`](0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md)),
and `Simulation.CheckEndOfRun` folds every balance against `MoneySupplyTable.Issued` and fails a world
that has invented or destroyed a unit.

**So the question was never *should the cost have a counterparty*. It was *which of the three that
exist can receive one today*.** Three were considered and the field is small because most of the
economy is unbuilt:

| Counterparty | Standing |
|---|---|
| **A landlord** — the premises' owner charging its tenant rent | ***Unbuilt.*** A Building holds no money at all ([`0113`](0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)) and no table names a landlord. It is the most truthful cost a shop has and it has nowhere to be paid |
| **A supplier** — cost of goods, paid to whoever sold the stock | ***Buildable and not yet built.*** `restock` draws from the Hinterland, which is a price ceiling rather than an actor, and a Hinterland holds no balance. This is the successor |
| **The treasury** — a levy, paid to the city | ***Built.*** `Scope.Global` resolves to it, `taxed.toml` and `levied.toml` already move money through it, and it is the only money-holding actor a shop can reach today |

### The honest objection, and why it does not change the answer

⚠ **Rates are the *least* explanatory of the three.** A player told *this shop closed because it could
not pay its rates* learns almost nothing about the city; told *it could not pay for its flour*, they
learn that the shop was buying more than it sold. `LEGIBLE CAUSE` prefers the second, and this record
ships the first.

**What makes that acceptable is not that rates are good enough — it is that the alternative is
`adr/0070`'s error.** *An unbuilt mechanism is not a design constraint*, and its converse holds too:
**the answer to *given a supplier does not exist, should the shop pay nobody?* is neither *pay nobody*
nor *wait* — it is *build the supplier*, and until then, pay the actor that does exist.** A cost paid
to the wrong counterparty is a wrong *attribution*; a cost paid to none is a **leak**, and the second
is the one that corrupts every other number in the world.

🔴 ⚠ **The real risk here is tenure, and it is stated because the user stated it when taking the
decision**: *"later" is how a stand-in becomes a principle, which is the failure*
[`0163`](0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md) *was
written to stop.* A levy that works is a levy nobody revisits. **That is what the revisit trigger below
is for, and it names a milestone rather than a condition, precisely so that it comes due whether or not
anybody is dissatisfied.**

## Consequences

- **A trade's Rules gain their first money term, so `RulesetLoader.ApplyTenancies` routes one to the
  Business.** The tenancy is derived from a Rule's **local** terms matched against the kind's declared
  Bins, so declaring `{ resource = "money", owner = "business" }` on the kind is what makes the levy
  the Business's rather than the premises'. ⚠ **A kind that declares the Rule and not the Bin gets a
  Rule that fails against the wrong subject**, which is a load-time shape and not a runtime one.
- **Bankruptcy becomes observable, and it is the world
  [`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md) task 10 has been
  waiting for.** [`0137`](0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md)
  gave `RuleEvidence` the blocking Bin at task 5; this supplies the first Rule in any shipped world
  that can block on a **money** Bin. ***A field that distinguishes two failures is untested until a
  world produces both***, and `provisioned.toml` now does.
- 🔴 **The level is a separate decision and it is hash-bearing.** `[[rule]] rates`' `rate` and `amount`
  each get a `plans/0002` **§D1** row under [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).
  ⚠ **The first value shipped was wrong in the way that is hardest to see**: at ~6% of a measured
  median shop's revenue **no shop in the world ever blocked on money at all**, so the mechanism was
  present, correct and **unobservable** — the same failure shape as milestone 9's land-value producer.
  It was found by running it, not by reading it.
- 🔴 **A shop can now go broke and STILL cannot be turned out, which this record does not fix and must
  not be read as fixing.** `ZoneRuleEngine.Condemn` walks `World.Occupants` — the Households in a
  Building — and a Business occupies through `World.BuildingBusinesses`, which nothing walks; `Worst`
  is typed `Handle<Household>`. ***So the pressure this levy creates accumulates against a threshold no
  code consults for a Business.*** ⚠ **What a broke shop's eviction should DO is the undecided half and
  it is not plumbing**: `Unplace` sends a Household to the Unplaced Pool, and whether a Business goes to
  `UnpremisedTable` or is destroyed decides **whether its capital survives**, which is an `adr/0024`
  question. Filed in `plans/0002` §A, owned by milestone 26.
- **It is a levy on capital rather than on revenue, exactly as `levied.toml` already is**, because a
  shop's income is sales and sales are the thing under test. ⚠ **So a number out of a run of
  `provisioned.toml` ratifies nothing about taxation**, and the file's header says so.
- **`adr/0163`'s bid contest is untouched.** This record is about a trade's *outgoings*; what raises a
  shop in the first place is that record's question and milestone 26 task 6's work.

## What would trigger revisiting

- **Milestone 27, which capitalises a Business.** `BusinessTable.Balance`'s missing *capitalisation
  band* is a `plans/0002` **§D2** row already owned by that milestone, and a shop that opens with money
  is a shop whose decline curve is a different shape — so the level here is re-derived on that day
  whether or not it is reopened.
- **A supplier that can hold a balance.** The moment `restock` pays an actor rather than a ceiling —
  a Hinterland with a purse, or a second Building kind selling to the first — cost of goods exists and
  ***this record's stand-in is retired rather than ratified***, in the same way
  [`0167`](0167-a-purchase-picks-its-seller-by-a-draw-and-waits-on-the-market-rather-than-on-a-shop.md)'s
  draw is retired by per-seller prices.
- **A landlord.** [`0113`](0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)
  keeps money off the Building on purpose, so rent needs an owner entity that does not exist. If one
  ever does, rent is a better cost than rates for the same reason cost of goods is: ***it names
  somebody the player can see.***
- **A second recurring cost.** One standing cost is a levy; two is a cost *structure*, and the question
  of which costs a trade carries stops being answerable one Rule at a time.
