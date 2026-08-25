# A Business runs Rules, and its Rules live as long as its tenancy

**A Rule Instance's subject widens from *premises or Household* to *premises, Household or Business*,
and a Business's Rule Instances and Bins are created when it takes premises and destroyed when it loses
them — exactly as a Household's live and die with its tenancy. A trade declines by going short of
**money**, which is `Blocking.Supply` on its balance Bin, which is bankruptcy under
[`adr/0137`](0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md)
and is therefore visible in `Evidence`.** `LEGIBLE CAUSE` `UNIQUE INDIVIDUALS`

🔴 **This is the work milestone 27 task 9 built and reverted**, and it is taken up here because
***nothing was ever decided against it***: `plans/0041` **G39** records the revert as a **sizing**
failure, not a design one — *"nothing was decided wrongly … what it cost was sizing."* Under
[`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) it is **unbuilt**, and only
*refused* would have been evidence.

---

## Why

### A shop nobody buys from is immortal, and that is a deliberate property of the pressure clock

Session W twice asserted that a Provider's decline would come nearly free — that a shop with no
customers fails its own Rule and condemnation does the rest. **It is wrong, and the build says so
precisely.** `RuleEngine.Stop` clears the failure-pressure clock for **any** blocking reason that is not
`Blocking.Supply`, and `RuleInstanceTable.StarvedSince` explains it:

> ***"Only `Blocking.Supply` sets it. A Rule stopped on `Blocking.Space` has a Bin that is full, which
> is what a well-supplied Building with nobody to sell to looks like, and `02 §5.9` names input
> starvation as the source."***

And `Blocking.Space`'s own definition names our exact case — ***"an output Bin that was full … a bakery
whose shelf of bread nobody has bought."*** ⚠ **So unsold stock is not decline pressure, on purpose.**
A trade cannot be made to churn by failing to sell; ***it has to go short of something it consumes.***

### The only thing it can go short of is money, and money is what makes it bankruptcy

A trade that cannot sell earns nothing. Give it a Rule that **consumes money** — an upkeep, a rent, any
recurring cost — and an empty balance is `Blocking.Supply` on a money Bin, which is:

- **pressure**, so `condemn_after` on the kind applies and the premises churn;
- **bankruptcy** rather than starvation, which is `adr/0137`'s own distinction — *"Pool Bin short is
  input starvation, money Bin short is bankruptcy, two Bins and two blame reasons"*;
- **the world `plans/0037` task 10 has been waiting for** — *"bankruptcy distinguishable from starvation
  in `Evidence` on a world that produces both"*, whose absence is the second of milestone 26's two
  stated gates.

***One mechanism answers the decline question, the diagnostic requirement and the missing world.***

### Which is impossible today, because nothing can hang a Rule on a Business

`RuleInstanceTable` has exactly two subjects: `Building` — *"the premises this Rule runs on, set for
every row, tenant's Rules included"* — and `Household`, a `HandleColumn<Household>`. **There is no
Business.** So the money Rule above cannot be armed, and the cheap alternatives all fail on the same
wall:

- A **Policy** sweeping Businesses exists ([`adr/0149`](0149-a-business-is-a-population-a-policy-sweeps-and-a-readout-names-every-entity-it-reads-against.md))
  and **cannot** produce this: `Readouts.cs` records that a Rule whose apply count is read off the Bin
  it spends is ***"unfailable by construction"***, so ***"a levy on holdings never joins a wait list and
  never reports bankruptcy."***
- A **balance-empty departure bound** — a Business at zero for *N* Days leaves, reusing
  [`adr/0142`](0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md)'s
  sink — was session W's cheap recommendation and is **refused below**.

### The obstacle that crashed task 9 is a design question, and milestone 25 already answered its shape

Task 9's column crashed with `StaleHandleException` on a **building** handle at `RuleEngine.Fire`,
because a Rule Instance names premises unconditionally and
[`adr/0142`](0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md)
makes **unpremised a legitimate steady state** with `Businesses.Building` severable. ***So the real
question was never plumbing: it is what a Business's Rules are when it has no premises to run them
on.***

✅ **Milestone 25 answered exactly this for the other Occupant kind and the answer transfers unchanged.**
A Household's Rule Instances and Bins are created on move-in and destroyed on eviction — *"a tenant's
Rule Instances and Bins live exactly as long as the tenancy"* — which is the collection `rulesets/evicted.toml`
exists to exercise and which a 131,072-Tick run showed **flat**. **A Business's do the same**: an
unpremised Business runs no Rules, holds only its money
([`adr/0144`](0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md)),
and is already bounded by its give-up bound while it waits. ***The stale handle cannot arise, because
the row does not outlive the premises it names.***

⚠ **This makes the work a second application of a shipped pattern rather than a subsystem invention**,
which is the specific thing task 9 could not have known — *it built the column before the tenancy rule
existed to copy.*

## Rejected

**A balance-empty departure bound instead of Rules.** Cheap, touches no engine, reuses the emigration
sink, and produces the churn. **Refused because bankruptcy would stop being a Rule failure and would
therefore not appear in `Evidence`** — and `plans/0037` task 10's acceptance criterion is bankruptcy
*distinguishable from starvation in `Evidence`*. ***It satisfies the mechanism and fails the diagnostic,
which in this project is failing.*** `adr/0137` exists to make the wait list carry the blame, and a
bound carries none.

**Defer decline and ship the Provider immortal.** Honest, and `adr/0006` does not forbid it — shops are
bounded by permitted land rather than growing without limit. Refused because the demonstration would
then show the exact failure `rulesets/minimal.toml`'s header predicted — ***"until the city is all
offices"*** — and a milestone whose demonstration exhibits the flaw it was meant to retire has not
retired it.

**Make `Blocking.Space` accumulate pressure too.** It would make an unsold shop decline with no new
subject at all. **Refused**: `02 §5.9` names input starvation as the source and
`RuleInstanceTable.StarvedSince` states the exclusion deliberately, so this is amending a decided thing
to avoid an undecided one. ⚠ **It would also silently change every existing Building**, since a full
output Bin is reachable on shipped Rulesets today.

## Consequences

- 🔴 **This is milestone-sized work and should be scheduled as such.** `RuleEngine.Fire`, evidence,
  `on_fail`, the wake targets and every local Bin lookup resolve a Building from the instance — *"the
  column was the visible tenth of a subsystem"*. **Session W does not schedule it**; it establishes that
  it is unrefused, necessary for W-Q2, and the cheapest route to two of milestone 26's obligations.
- ✅ **Milestone 26's second gate is discharged by this rather than by a separate world.**
  [`adr/0163`](0163-demand-for-a-shop-is-elapsed-unserved-need-in-reach-and-building-claims-it.md)
  established that the bankrupt world is 26's **output**; this names the mechanism that produces it.
  ***A trade with a money upkeep and no customers in reach is the world `0037` task 10 asked for.***
- ⚠ **W-Q2's remaining content is one authoring decision, and only now is that true.** *What does a
  trade consume such that failing to get it should kill it* — a money term, whose magnitude is
  hash-bearing and owed a §D row **when it is written**, which is not here.
- 🔴 **`RuleInstanceTable.cs:92` is now wrong in a new way and is owed to
  [`plans/0012`](../../plans/0012-corpus-audit.md).** It says *"A Business gets its own column when a
  Business runs a Rule, **which is milestone 27**"* — and milestone 27 closed without it. ⚠ **`plans/0041`
  **G39** proposed the check that catches exactly this**: *a doc comment naming a future milestone is a
  citation, and a citation to a closed milestone is checkable.* ***It has now become checkable.***
- ⚠ **A third subject makes `World.FindLocalBin`'s binary a ternary**, which `plans/0041` **G10** already
  identified. **This is milestone 25 task 2's shape a second time**, and that task's own finding was
  that the decomposition split what the governing record had declined to split — ***worth re-reading
  before this is decomposed.***

## What would trigger revisiting

- **A cheaper route to bankruptcy-in-`Evidence` being found.** The refusal above turns entirely on the
  diagnostic; anything that puts a money-short Business on a wait list without widening the subject
  would reopen this.
- **`plans/0037` task 10's acceptance criterion being narrowed.** If *bankruptcy distinguishable from
  starvation in `Evidence`* is ever dropped, the balance-bound alternative becomes correct and this ADR
  is superseded rather than amended.
- **The widening turning out to cost more than a milestone.** Task 9's estimate was wrong by a
  subsystem; ***this ADR's is an estimate too***, and if a survey finds the blast radius larger again,
  the fork is to schedule the Provider immortal at tier 1 and take decline later — which is the second
  rejected option, revived on evidence rather than on cost-aversion.
- **Wages arriving (`adr/0026`, milestone 15).** A trade with revenue changes what *going short of
  money* means, and the money term's magnitude — deliberately unwritten here — should be chosen against
  a world where a shop can earn rather than one where it cannot.
