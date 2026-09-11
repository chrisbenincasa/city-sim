# 0072 — City income

Amnesty row 33. Design session opened 2026-09-10. **Design in progress; implementation not started.**
This plan owns the decisions and remaining design questions. Implementation follows a completed
design and scope; row 32 supplies the expenditure side of the budget.

## Decisions

### D1 — Tax individual earned income and Business profit

Accepted by the user on 2026-09-10. Both taxes are in scope, with rates controlled by the player.
Citizen income tax applies to the individual's earnings and reduces the money reaching their
Household. Business profit tax applies to revenue less allowable business expenses, including
wages. A loss-making Business owes no profit tax. Neither tax uses a standing money balance as
its base; sales alone are not Business profit.

Real-world grounding: [Finnish municipal earned-income taxation](https://www.vero.fi/en/individuals/tax-cards-and-tax-returns/tax_card/tax-rate-and-income-ceiling/tax-bases/)
and [HMRC's deduction of qualifying business expenses from taxable profit](https://www.gov.uk/corporation-tax-rates/corporation-tax-expenses).
These are mechanism references, not a choice to reproduce either jurisdiction's tax code.

### D2 — Citizen income tax has three marginal bands

Accepted by the user on 2026-09-10. A tax-free allowance is followed by a middle taxable band
and an upper taxable band, each with a separate player-controlled marginal rate. Each rate
applies only to earnings within its band. Crossing a threshold does not reprice earlier earnings
or cause a downward jump in take-home income. Bands describe portions of individual earned
income, not Citizen wealth classes, Household balances or Skill Tiers.

No numerical thresholds or rates have been chosen; the session's worked numbers were
illustrations only.

### D3 — The player controls both thresholds and both taxable rates

Accepted by the user on 2026-09-10. The four Citizen income-tax controls are the tax-free
allowance, the threshold at which the upper band starts, the middle marginal rate and the upper
marginal rate. D4 sets the time basis; constraints on these controls remain open.

### D4 — Assess individual earnings per Day, independently of payday

Accepted by the user on 2026-09-10. Thresholds are denominated in earnings per Day. Combine
earnings attributable to the same Citizen and Day across employers. Paying several Days' wages
together does not treat the lump as one Day's earnings or change the total tax solely because
of payday timing. Daily fluctuations can change the total liability relative to steady earnings
with the same multi-Day total; no multi-Day averaging was selected.

### D5 — Withhold on actual wage payments and conserve Money

Accepted by the user on 2026-09-10, with an explicit requirement that no Money be lost.
The employer splits each gross payment between the Household and treasury; their credits sum
exactly to the employer's debit. Gross payment discharges wages, including the withheld portion.
Unpaid wages generate no collectible Citizen income-tax debt. Calculate withholding against
cumulative gross payments attributed to the earning Day, sharing its bands across employers
and instalments. Later payments collect only the additional tax: no repeated allowance.

For illustration, with a daily allowance of 50 and a 20% middle rate, a payment of 40 followed
by 60 for the same Day transfers 40 then 50 to the Household and 0 then 10 to the treasury.
Assessments and wage obligations do not themselves move or create Money. Partial payments,
multiple employers, rounding and save/reload must preserve both payment accounting and Money
conservation. Fractional tax must not disappear through splitting payments; the exact remainder
representation and settlement at death or closure remain design work.

### D6 — Use the earning Day's tax rules

Accepted by the user on 2026-09-10. Rates and thresholds are those in force when wages were
earned, including when payment arrives late. Player changes apply prospectively, without
repricing earlier earnings. The precise effective boundary for a change remains to be settled.
Retaining the necessary history through late payments and save/reload is an implementation
obligation; a bounded retention or arrears-forfeiture policy must not silently change this decision.

### D7 — Citizen marginal rates cannot decrease with earnings

Accepted by the user on 2026-09-10. The upper marginal rate must equal or exceed the middle
marginal rate. Equal rates permit an allowance followed by a single effective taxable bracket;
a higher upper rate permits stronger progression. A lower upper rate was considered as a
possible incentive for high earners to locate in the city, but that possibility did not justify
the control without established consequences supporting the strategy. Numerical bounds and
threshold validation remain to be settled; this decision fixes the ordering of the rates.

### D8 — Business profit tax has two marginal bands

Accepted by the user on 2026-09-10. Private Businesses share two marginal profit bands across
trades. The player controls the lower rate, the profit threshold and the upper rate; the upper
rate must equal or exceed the lower. Only profit above the threshold faces the upper rate.
There is no separate tax-free band, although setting the lower rate to zero can provide one.
No numerical settings or Business assessment period have been chosen.

The mechanism gives relief to modest profits, which must not be labelled small Business relief:
a large employer with thin margins can also benefit. The real-world reference is
[HMRC's small-profits rate and marginal relief](https://www.gov.uk/guidance/corporation-tax-marginal-relief);
the two-band schedule is a game simplification, not a reproduction of that calculation.

### D9 — Design targeted charges and incentives as a broader policy capability

User direction on 2026-09-10: pollution charges are one starting example, and clean energy can
receive incentives or relaxed payments. The design must cover the broader system containing
both, rather than treating these as two isolated features or an exhaustive list of policies.
Each policy has a separate player lever. This expands the design remit; it does not settle which
examples ship in row 33 or authorise implementation of every possible policy.

The proposed pollution example charges attributable emissions, independently of profit tax.
Its detailed base, collection, consequences and inclusion in implementation remain open.
Clean-energy support is an example to design, not a claim that its eligible activity or response
already exists. The distinction between reducing tax owed and paying a subsidy must be explicit:
the former forgoes revenue, while the latter spends treasury Money. Neither may create Money.

### D10 — Support charges, tax relief and explicitly funded subsidies

Accepted by the user on 2026-09-10. The targeted policy system supports three distinct tools:
charges transfer Money from the liable payer to the treasury; tax relief reduces tax owed;
subsidies transfer Money from the treasury to the eligible recipient and require funding.
Relief alone cannot pay a Business that owes no tax. A payment beyond the tax otherwise owed
must be represented and funded as expenditure, not hidden as negative revenue or created Money.

The common design questions are eligibility, the activity or quantity determining the amount,
player controls, interaction with other policies, and budget presentation. Support for all three
tools does not select individual policies, amounts or implementation scope.

### D11 — Targeted policies come from an authored catalogue

Accepted by the user on 2026-09-10. The player chooses from defined policies and adjusts each
policy's relevant controls. Eligibility, qualifying activity and the type of effect belong to
the policy definition; the player does not construct predicates or tax formulas. The catalogue
can grow to cover further policies without requiring a general-purpose tax editor.

Example controls proposed for later design are a price per unit emitted for a pollution charge,
a relief level for clean-energy tax relief, and a payment per unit plus funding allocation for a
clean-energy subsidy. These examples do not yet settle eligibility, units, caps or initial content.

### D12 — Subsidies share a capped daily allocation proportionally

Accepted by the user on 2026-09-10. A subsidy has a player-controlled support rate and funding
ceiling per Day. If qualifying claims exceed the available allocation, recipients share that
allocation in proportion to their claims. Processing order must not determine who receives
support. Unpaid portions create no debt: support is explicitly subject to funding. The panel
shows qualifying claims and actual payments, and any Business response must account for
funding availability rather than assume the headline rate is guaranteed.

For illustration, claims of 200 and 100 against an available allocation of 150 receive 100 and
50. Integer remainder allocation must conserve Money and be deterministic. How the treasury
reserves available funding across multiple policies and other expenditure remains open.

### D13 — Tax relief reduces qualifying profit tax, not targeted charges

Accepted by the user on 2026-09-10. The standard Business tax-relief mechanism is a
player-controlled percentage reduction of qualifying profit tax, calculated after applying the
normal profit bands. A 25% relief against a qualifying tax bill of 40 reduces it to 30; no
profit-tax liability means no benefit from this relief and no payment to the Business.

Targeted charges remain separate: profit-tax relief does not reduce a pollution charge or other
targeted charge. A Business may qualify for clean-energy support while still owing charges for
pollution it emits. Eligibility and whether relief applies to all or part of a Business's profit
tax remain open, as does the interaction between overlapping reliefs.

### D14 — Overlapping reliefs add, capped at qualifying tax

Accepted as a starting point by the user on 2026-09-10; **PROVISIONAL**. Relief percentages
applying to the same qualifying portion of profit tax add together, capped at 100% of that
portion. Application order cannot change the result, and relief cannot create a payment.
Two applicable 25% reliefs against qualifying tax of 40 save 20 in total. Reliefs covering
different activities apply only to their respective qualifying portions; attribution remains
to be designed before implementation.

### D15 — Business profit uses simplified accrual accounting

Accepted by the user on 2026-09-10. Sales count when Goods or services are delivered. Stock
bought for resale becomes an expense when sold; unsold stock retains its purchase cost. Gross
wages count as an expense when earned, including the portion later withheld for Citizen income
tax. Delaying payment does not change when these revenues or expenses enter profit.

For illustration, a shop buys 100 units for 100, sells 40 for 80 and incurs 20 in gross wages:
profit is 80 minus 40 minus 20, or 20. Remaining stock carries a cost of 60. Deducting the full
purchase immediately would instead report a loss of 40 and overstate profit on later sales.

Accounting records do not create or move Money. Profit and cash available to pay bills are
distinct, so collection must explicitly handle a profitable Business that lacks cash. Production
costs, inventory cost allocation across purchases, capital purchases, losses and the Business
assessment period remain to be designed. D5 still governs Citizen withholding on actual payment;
recognising the employer's wage expense does not itself collect Citizen income tax.

## Remaining design branches

- Numerical bounds for Citizen rates and threshold validation.
- Targeted policy structure: eligibility, activity bases, charges, tax relief, subsidies, stacking,
  funding limits and player controls; choose initial examples and implementation scope.
- Catalogue presentation, activation and effective dates.
- Taxable earnings beyond wages, job changes and attribution of partial payments to earning Days.
- Business revenue and deductible expenses; accounting period, losses, inventory and capital.
- Effective boundary for rate changes, fractional settlement, insufficient funds and closure.
- Public employers, transfers, founding capital and other receipts that are not trading income.
- Consequences through spending, employment, Business decisions and the Outside comparison.
- Treasury income and expenditure presentation, individual inspection and attribution.
- Relationship to existing Policies, geographic overrides and the row 32 spending mechanism.
- Demonstration, acceptance scenarios, replay/save obligations and implementation scope.

No options in this list are settled by their inclusion. Tuning choices remain PROVISIONAL under
amnesty; no new ADR or implementation is authorised by this document.

---

## Implementation, opened 2026-09-10

Worktree `worktree-row-33-city-income`, against `main` at `2461d57`. Read back from the symbols
before any task started; five of the design's premises moved.

### F1 — a Citizen holds one job, so D4 and D5's cross-employer clauses reach nothing

`CitizenTable.Workplace` is a single severable handle (`CitizenTable.cs:231`), and
`WageEngine.Pay` walks one employer's worker list. There is no second employer to combine
earnings from. Under `adr/0070` that absence is *unbuilt*, not refused, so the per-Day
accumulator D5 requires is built for instalments and **not** for multiplicity. A second
employer, if one is ever built, meets an accumulator already keyed by Citizen and Day.

### F2 — a Day can be paid twice, and that is the accumulator's whole job

`Pay` advances `LastPaidDay` by whole Days covered — `FloorDiv(due, rate)` at
`WageEngine.cs:381` — while depositing `due` in full. A part payment therefore hands over money
for a Day it leaves claimable, and the next payday pays that Day again. ***Without a cumulative
accumulator the allowance is granted twice for one Day***, which is exactly what D5 forbids.

### F3 — D6's history is already bounded, by a rule written for another reason

D6 asks for the earning Day's rates through arbitrarily late payment, and warns that a bounded
retention must not silently weaken it. It does not: `Pay` caps the window at
`trade.PayPeriodDays` and **forfeits** the remainder (`WageEngine.cs:326`), so no payment ever
reaches an earning Day older than one pay period. A ring of schedules `maxPayPeriodDays + 1`
deep is exact rather than lossy, and it is saved and hashed like any other world state.

### F4 — there are two payment paths and only one decomposes into Days

The flat path pays `days * rate`, uniform per Day. The `WorkSchedule.Runs` path pays down
`CitizenTable.EarnedWage`, a single accrual with no per-Day breakdown, and sets
`LastPaidDay = today` whether or not the till covered it (`WageEngine.cs:376-379`). ⚠ **On that
path an underpaid Citizen's residue is carried forward and would otherwise be taxed against
today's Day rather than the Day it was earned.** Decision: attribute every payment uniformly
across the window it closes, on both paths, and tax each Day of that window separately.

### F5 — no Business records revenue or expenses

`BusinessTable` carries `Balance`, `ShortPaydays` and stock Bins, and nothing else fiscal.
There is no sales figure, no cost of goods and no expense total anywhere in `Borough.Core`.
D15's accrual accounting is therefore new state rather than new arithmetic, and it is the
largest single piece of this row.

## Phases

Ordered by dependency. Each ships with its tests before the next starts.

| Phase | What | Decisions |
|---|---|---|
| **A** ✅ | Citizen income tax withheld at the wage payment; three bands; four player controls; the earning Day's schedule | D1–D7 |
| **B** ✅ | Business revenue, expenses and profit on a simplified accrual basis; two profit bands; collection | D1, D8, D15 |
| **C** ✅ | The targeted policy catalogue — charges, relief and funded subsidies, with proportional rationing | D9–D14 |
| **D** | The budget the player reads income and expenditure off, the demonstration Ruleset and the acceptance run | — |

### Phase A tasks

1. **A tax schedule, and a Ruleset that states one** — `[income_tax]` with an allowance, an upper
   threshold and two marginal rates, refusing an upper rate below the middle rate (D7) and a
   threshold below the allowance. Integer arithmetic only.
2. **A per-Citizen, per-Day gross accumulator** — saved and hashed, one `(Day, gross)` pair per
   Citizen. Only the boundary Day of a payment window can be paid twice (F2), so one pair is exact.
3. **Withholding at the payment** — `WageEngine.Pay` splits its single deposit into a Household
   credit and a treasury credit summing to the unchanged till debit. Money conservation holds by
   construction rather than by a later correction.
4. **The player sets the four controls** — a governed tax schedule edited by a new Command, saved
   and hashed, carried across a hot reload the way `PolicyTable.Adopt` carries a governed amount.
5. **The earning Day's schedule** — a bounded ring, `maxPayPeriodDays + 1` deep (F3), so a rate
   change applies prospectively and a late payment is still taxed at the Day it was earned.
6. **Tests** — withholding arithmetic across the bands, no repeated allowance across instalments,
   money conserved, replay and save/reload equivalence, and the hashes re-recorded.

### F6 — the money file pays nobody, so it cannot demonstrate the tax

`rulesets/taxed.toml` is the one shipped world whose Households hold anything, which is why the
schedule is authored there. ⚠ **It pays no wages at all** — a wage is refused at load until
milestone 15 (`adr/0026`) — so nothing in it is ever withheld from. Its two `[[policy]]` sweeps are
transfers rather than earnings. ***The row's demonstration therefore needs a world that both pays a
wage and holds a treasury***, and no shipped file is both. Phase A gains a task for it.

### F7 — withholding is invisible to the budget the row has to deliver

`PolicyActivity.ToTreasury` is folded in `PolicyEngine.Move` and nowhere else, so money the employer
withholds reaches the treasury without appearing in any flow the Census carries. The treasury
balance would rise against a reported income of zero. `PayrollReading.Withheld` carries the figure
out of the payroll sweep; the Census has to fold it beside `ToTreasury` or the budget is wrong in
the one direction this row exists to make readable.

## Decisions taken while building

### D16 — the employer withholds; the treasury never collects from a Household

One debit against two credits summing to it exactly: the till gives up the gross, the Household
receives the gross less tax, the treasury receives the tax. The alternative — paying the Household
in full and levying afterwards — collects nothing from a Household that has already spent it, and
it is the standing-balance levy this row exists to replace. Money is conserved by construction
rather than by a later correction.

### D17 — a payment is spread across the Days it closes, a full Day's rate at a time

Thresholds are per Day (D4), so a week paid together must not read as one enormous Day and receive
one allowance. The walk gives each earning Day a full Day's rate until the money runs out, which
mirrors exactly how `WageEngine.Pay` already advances `LastPaidDay` over the Days it covered. The
accumulator ends the walk on the part-paid boundary Day — the only Day that can be paid again.

### D18 — the schedule history is 32 Days deep, and a longer pay period is refused

`IncomeTaxTable.Retained` is a `const` on `RulesetTrailTable.Retained`'s grounds: the row count is
fixed for the life of the world, so a hot reload must not be able to demand a deeper history than
the table was built with. ⚠ **The matching loader refusal is not yet written** — a Ruleset stating
`[income_tax]` beside a trade whose `pay_period_days` exceeds the ring would silently lose the
oldest Day's schedule. Phase A task 8.

### D19 — a player's rate change takes effect from the start of the next Day

D6 forbids repricing earnings already made, and a Day part-earned is already being made. Setting
all four controls on one Day accumulates into a single ring entry, because the ring is indexed by
the effective Day and replaces rather than appends.

### D20 — a hot reload of the authored schedule is not a player rate change

`ScheduleFor` falls through to `Ruleset.IncomeTax` for any Day the player never governed, so
reloading a file with different `[income_tax]` numbers changes what an unpaid past Day would be
taxed at. ⚠ **Accepted rather than solved.** A reload is a designer's act during authoring, not a
player's fiscal decision, and D6 governs the latter. Determinism is unaffected: the reload is in
the Input Log and a replay reproduces it.

### Phase A tasks added

7. **A world that pays a wage and holds a treasury** (F6) — the demonstration Ruleset, and the
   `--income` reading over it. `taxed.toml` keeps the authored schedule as content; it is not the
   world this is watched in.
8. **The loader refuses a pay period deeper than the ring** (D18).
9. **The Census folds `PayrollReading.Withheld`** (F7), so income is readable against expenditure.

### D21 — the rates must be raised from the top down, and the refusal says so

D7 constrains the pair, so from a world that levies nothing — every control at zero — raising the
middle rate first is refused for exceeding an upper rate of zero. ⚠ **Accepted rather than
smoothed.** The alternative is lifting the upper rate to match, which moves a control the player
did not touch, and a tax schedule is the last place to do that quietly. The refusal names the
constraint and the panel has to state the order; both checks are composed against the *resulting*
schedule, so the four controls are settable in any order that is legal at every step.

### D22 — the demonstration world is `shopping.toml` with a schedule over it

`rulesets/taxing.toml`, and it is `shopping.toml` verbatim plus one `[income_tax]` table. That file
is the only shipped world that both **pays a wage** and holds a **treasury Bin for money**, which is
exactly the pair an income tax needs and which no other shipped file has together (F6). Its
schedule is scaled to its own `wage_per_day` of 4096 rather than to `taxed.toml`'s numbers: an
allowance of 1024, a threshold of 3072, and rates of 20 and 40 percent. ⚠ **Both bands are reachable
by the only wage the file pays**, which is deliberate — a threshold above the wage would make the
upper rate unobservable and the demonstration would fail quietly. The size is chosen to be visible
within one pay period, not to be plausible.

⚠ **`--money` cannot read it.** `MoneyDump` refuses a Ruleset with no `[[policy]]` and this world
has none, so the row still owes a reading of its own — income against expenditure per Day, which is
the budget clause of row 33 and Phase A task 9's other half.

## Findings from the integration tests

### F8 — F2 was wrong, and the accumulator's own case is unreachable on a fixed Ruleset

**A Day cannot in fact be paid twice while the Ruleset holds still**, so the per-Citizen accumulator
F2 was built for is written on every payday and read on none. The arrears cap gets there first: a
part payment leaves `LastPaidDay` at least one Day behind the payday, the gap to the next payday is
therefore `period + 1`, the cap fires, and it moves the clock **past** the part-paid Day. The walk
starts the Day after it. Changing employer does not open the case either — `World.Employ` resets
`LastPaidDay` to the hire Day.

The one shipped opening is a **hot reload that re-times the pay period**, which puts a second payday
inside one window. `WageWithholdingTests` proves the branch through exactly that door.

⚠ **The columns stay, and the reason is not that the branch is reachable.** Without them, D5's *no
repeated allowance* would be guaranteed by the arrears cap — a rule written for `adr/0006`'s sink and
for nothing to do with taxation. ***A correctness property resting on an unrelated rule is one edit
away from silently doubling every Citizen's allowance***, and nothing would fail. Two hashed columns
is the price of the guarantee being stated where it is relied on.

### F9 — a part payment pays for the same Day twice, and it is not this row's defect

`WageEngine.Pay` floors `covered = FloorDiv(due, rate)` to zero on a sub-Day payment, so the money
is handed over and the Day stays fully claimable. On a fixed Ruleset the arrears cap forfeits it and
nothing is visible. Where two paydays fall inside one window — the reload case above — the worker
receives `part + rate` for one Day's work. ⚠ **The tax is correct and the wage is not**: the walk
assesses the whole `part + rate` as one Day, which is what was actually paid.

**Pre-existing, and older than this row.** Under `adr/0073` it belongs to the code that owns it;
recorded here because this row is what made it visible, and routed to `plans/0003`.

### F10 — the treasury Bin can never be missing, so `Pay`'s guard is dead

`World.FitTreasury` opens a Bin for every conserved Resource, `Ruleset.IsConserved` is exactly the
money family, and it runs in the constructor and on every `Adopt`. A till is a money Bin by
construction, so it always finds a treasury Bin to withhold into. The `NoSlot` branch is unreachable
today; the test asserts the positive claim instead, so the day something makes it reachable the
assumption is named rather than silently relied on.

### F11 — the treasury had a third income path and no counter saw it

Measured on `taxing.toml` over 24,576 Ticks: the treasury closed at **1,621,850**, of which tax
withheld was 180,058 and **1,441,792 was 88 firings of a Bin Rule whose output names
`scope = "global"`**. So 89% of the budget's income was unattributed and its balance column could
not be explained by the flow columns beside it.

⚠ **The same defect class as F7, arriving through a different door.** Fixed by counting a global
term at `RuleEngine.Fire` — not at `Touch`, which accumulates a scratch delta for every *evaluated*
Rule including blocked ones and every rung of a failed chain, and would have counted evaluations
rather than movements. Reported as two new counters rather than folded into `ToTreasury`, because
each `MoneyFlow` carries a `Peak` beside its `Sum` and **two peaks cannot be added**: adding invents
a Tick on which both engines peaked, and taking the larger under-reports the Tick on which both
moved money. Residual after the fix is **zero**.

***The test is worth more than the fix***: the change in the treasury balance across an interval must
equal income less expenditure across the same interval. Nothing asked that before.

### F12 — a dead Household's estate reaches the treasury uncounted, on ten shipped worlds

`World.Dissolve` passes a dissolving Household's balance to the treasury and no flow counts it. It
cannot fire on `taxing.toml`, which declares no `[[life_stage]]`, so this row's identity holds
exactly there — but ten shipped Rulesets declare both life stages and a money Resource, and the
budget under-reports income on every one of them. ⚠ **Left unbuilt rather than widening this row**,
and named in the code so it is not rediscovered by arithmetic. It wants its own row.

### F13 — the demonstration world could not pay the wage it posts

1,719 Citizens employed produced **82 wage payments** over 24,576 Ticks, against **40,326,483 owed
and uncovered** across 273 short paydays; Households fell from 13.9M to 100,675 by Day 9. The wage
is 4,096 a Day and a shop's only income is what a Household spends. ⚠ **The withheld column was
intermittent because the payroll barely ran, and read as the tax misfiring.** The file also declared
no `[[policy]]`, so its expenditure column was structurally zero — ***a reading of income against
nothing is not a budget***, which is the clause of row 33 this world exists to satisfy.

## What watching it exposed

`plans/0045`'s amended Definition of done: **a milestone is done when you have watched it happen and
something surprised you.** Two things did, and neither is about the tax.

### F14 — the wage was the weakest lever on whether payroll runs

Halving `grocer wage_per_day` from 4,096 to 2,048 bought four points of payroll coverage. What bought
the rest was **deleting a trade with no revenue at all** — the dwelling's `shop` held roughly 1,500 of
1,719 jobs and 173 of 195 live Businesses closed holding nothing — and **quadrupling the Household
endowment** so a shop had custom to take. ⚠ **A trade with no revenue cannot make payroll at any
wage**, and the shortfall scales with the wage while the *failures* do not, because a fixed subset of
grocers has no customers whatever their payroll. That reproduces `insolvent.toml`'s P1 independently,
over a fourfold sweep.

### F15 — quadrupling the money supply went partly into the price, not into runway

Single-variable control, the endowment band alone reverted: the sundries price falls 100 → 60 over
four moves and the Districts trade 5,278 a Day. With the band as it now stands the price **never
leaves the 100 ceiling** — zero moves in twelve Days — and they trade 22,447 a Day. ***Richer
Households bought more, faster, and at a higher price.*** So demand in this file is
**money-constrained rather than need-constrained**, and an endowment is partly a transfer to the
market rather than to the Household holding it. The assumption going in was that an endowment was
inert with respect to price; it is not.

⚠ **This bears on every world-creation endowment in the corpus, not on this row.** Recorded here
because this row measured it; it belongs to whoever owns `[households] opening_balance_min`/`max`.

### The reading, and the hand check

`--income --ruleset rulesets/taxing.toml --citizens 2000 --ticks 24576`: withheld 145,656; nothing in
by Policy; 1,884,160 in by Bin Rule; 1,485,312 out by Policy; treasury closes at 544,504. At 2,048 a
Day the schedule takes 204 in the middle band and 204 in the upper — **408 a Day, 19.9% effective**
— and every non-zero reading divides by it exactly: 357 Citizen-Days, 357 × 408 = 145,656. Nothing
grades the wage in this file, so 2,048 is the only rate there is.

⚠ **At a smaller sizing one reading does not divide**, and that is the mechanism rather than a
defect: a part-paid payday closes a short Day, which is taxed in a lower band.

## Phase A — done

All nine tasks. The tax is withheld at the payday, the player sets four rates from the panel, the
earning Day's schedule survives a late payment, the budget states income against expenditure and
**the treasury balance is exactly explained by the flows beside it**. Phases B, C and D remain.

## Phase B decisions

### D23 — Business profit is assessed per Day

Accepted by the user on 2026-09-10. The same cadence as the Citizen tax, and for the same reason
D4 gives: it needs no calendar, which `adr/0010` refuses the game outright. A Business that has a bad
Day owes nothing for it. ⚠ **A trade with lumpy sales pays more over a span than a steady one with
the same total** — the identical wrinkle D4 already accepted on the Citizen side, and it is accepted
here rather than smoothed.

### D24 — stock carries its total cost, and a sale expenses its share

Accepted by the user on 2026-09-10 as a weighted average. **Implemented as a total rather than an
average**, which is the same arithmetic without the second column: a stock Bin carries what its
contents cost, a purchase adds the payment to it, and selling `q` of `n` units expenses
`FloorDiv(cost × q, n)` and deducts it. ***One saved column per Bin and no collection***, where FIFO
would need a per-purchase queue on every Bin of every Business — a variable-length collection under
`adr/0036`, with a sink of its own. What is given up is being able to say which purchase a sale drew
down, which nothing asks.

### D25 — a short till pays what it has and the rest is forgiven

Accepted by the user on 2026-09-10. D5 already settles the identical question on the Citizen side:
unpaid wages generate no collectible tax debt. Carrying the shortfall would need a saved arrears
column **and a sink**, or it is a magnitude trending upward at steady state, which `adr/0006` forbids
outright. No collection may push a till negative.

### D26 — a loss is simply an untaxed Day

Accepted by the user on 2026-09-10. D8 already says a loss-making Business owes no profit tax; this
adds nothing to it. No carry-forward, so no saved carried-loss column and no bound for it. ⚠ **A
Business that loses money one Day and profits the next pays in full on the profitable Day**, and that
is the consequence of the daily cadence rather than a separate decision.

### D27 — both schedules live in one ring, stamped independently

One row per effective Day carries the earnings schedule and the profit schedule together, because
D19's *from the start of the next Day* is the same sentence for both and a second table would be a
second copy of that rule. ⚠ **The two halves stamp separately.** With a shared stamp, governing an
earnings rate in a world whose Ruleset authored `[business_tax]` would zero every profit band on the
way past — no refusal, no diagnostic, just a tax that stopped collecting. A recycled slot clears the
*other* half's stamp only when the row lands on a different Day; clearing it on every write wipes the
schedule just set.

🔴 **This was a real defect and a test written straight after the code caught it**, which is the
second such catch in the row.

## Phase B findings

### F16 — a Rule's `Touch` moves no money, and reading it as though it did inverts the books

`RuleEngine.Touch` stages a scratch delta during Phase 2's `Check`, which runs for every due Rule,
for every rung of a failed chain, and again for Phase 3's re-check — and can end in `Stopped` having
moved nothing. **The write is `Fire`.** Recognising revenue at `Touch` would have counted
evaluations rather than sales, over-reporting every blocked Rule and every chain rung.

⚠ **The same misreading arrived twice in one day from opposite directions** — F11's treasury
accounting hit it too — which makes it a property of the file rather than of either task.
`RuleEngine.Buy` resolves a `pool` input into the seller's stock down, the buyer's purse down and
***the seller's till up***: the seller is the one making the sale. Two named doors rather than one
with a flag, so a call site cannot get the direction wrong silently.

### F17 — a bought Good does not land in a Bin by itself

`Buy` debits the seller and nothing else. A Rule that means to *hold* what it bought says so with a
matching `local` output; no such term means the Good was **consumed**, which is a production cost
and D15 files that as still to be designed. ⚠ **Reading the buyer's Bin list instead would put the
cost onto stock it never bought.**

### F18 — the demonstration world's stock is free, so it shows the tax and not the accrual

In `taxing.toml` the pool buyer is a **Household** — the dwelling's sundries Bin is owned by its
occupant, so `restock` is a larder being filled and not a shop being supplied. The grocer's own
stock arrives from an input-less Rule, so ***its cost of goods is a genuine zero*** and its profit
is revenue less wages. **Not a defect**: the file demonstrates the profit tax without demonstrating
D15's stock-cost path. The header has to say so, or a reader infers a mechanism was exercised that
was not.

### F19 — the Day's figures roll on the next Day's first write, and payroll is that write

A Business's revenue and expense stand until something recognises against a later Day. On a
Day-boundary Tick the payroll sweep accrues each employer's wage bill before anything else, so
***Day N's profit must be assessed before it or the assessor reads zeroes and the city is never
taxed***. Silently — there is no error and no diagnostic, only a column of nothing.

⚠ **Held by a test rather than by a comment.** A comment saying *this must run first* is exactly the
kind of thing that survives the edit which breaks it.

### F20 — the governing panel no longer fits a screen, and a driven run cannot scroll it

With three tax blocks the profit note and its status line sit below the fold at 1600×1400, even
against `minimal.toml`, which is the shortest the panel gets. The block is reachable by scrolling and
the sentences that would have to go are the ones that stop a player mis-reading a dial, so the notes
stand. ⚠ **`ui scroll` addresses the inspector and not this panel**, so nothing below the fold can be
asserted against by a driven run. Separately, the tool palette overlaps the panel's heading and
Policy banner; pre-existing, and worsening as the panel grows.

## Phase B — done

Shipped 2026-09-10. Three saved columns on a Business, one on a Bin, a profit schedule sharing the
earnings ring, three player controls with their own refusals and their own panel block, a sweep at the
head of the Wake phase, a ninth `--income` column and a sixth money-flow counter. `rulesets/taxing.toml`
gained `[business_tax]`. All four golden artefacts re-recorded; no Ruleset content hash moved. The
working lane is 3,265 green.

## Phase C findings, before any code

### F21 — neither policy the design names as an example has anything to levy on

The survey that opened Phase C went looking for a per-entity quantity a charge could price or a
subsidy could pay against, and found that **D9's two worked examples both rest on mechanisms that do
not exist**.

| what D9 and D11 assume | what is there |
|---|---|
| attributable pollution per Building | **does not exist.** `RuleEngine.Emit` computes the emitter's own figure and passes it straight to a Map Layer Cell, which is shared by many Lots and decays. Nothing is written back |
| an energy or fuel Good | **does not exist.** Across every shipped Ruleset the Resources are `money`, `sundries`, `repairs`, `shop`, `grocer`, `dwelling`, `sewage`; the one `utility` is sewage |
| per-Business output or throughput | **does not exist.** A Rule Instance keeps no firing count; a Business keeps money figures and `short_paydays` |
| a way to aim a Policy at some Buildings and not others | **does not exist.** A Policy sweeps its whole subject population unconditionally |

What does exist per entity is a trade's kind id, a Building's kind id, and Phase B's per-Day revenue
and expense. ⚠ **Under `adr/0070` that makes all four *unbuilt* rather than *refused*, so none of them
is evidence for narrowing the design** — the answer is to build the cheapest one rather than to
redefine the policy around its absence.

### F22 — a Policy sweeps three phases after emission is written

`PolicyEngine.Sweep` runs in phase 6; a Rule's map output is written in phase 3. So on a Day-boundary
Tick a charge reading *the Day's emission* reads a **partially accumulated** Day, with no error and
nothing to notice it. ⚠ **This is F19 arriving a second time from a different direction**, which makes
it a property of the phase order rather than of either task.

## Phase C decisions

### D28 — a Policy may name the trade it applies to, and absent means all of them

The catalogue's eligibility, and the narrowest form that D11 needs. `[[policy]] trade = "grocer"`
restricts the sweep to Businesses of that kind; omitting the key keeps today's behaviour, which is
every member of the subject population. ⚠ **Eligibility is a property of the policy definition and
not of the player**, per D11 — the player moves the amount and never the predicate.

⚠ **This is also the one genuine overlap with queue row 32**, resolved here rather than there: a
policy that can name which trade it sweeps is what targeted spending needs, and it is a smaller thing
than the spending mechanism that wanted it.

### D29 — a Building's emission is attributed to the Building that made it

Chosen by the user on 2026-09-10 over charging a flat rate per trade, and over charging a share of
revenue. A charge on a flat rate per trade cannot be reduced by emitting less, and a charge on
revenue is a second tax on turnover wearing a charge's name; ***neither prices the thing the city
wants less of***, which is the whole of D9's example. The figure already exists at the write site and
is discarded there, so the build is a column and a line rather than a mechanism.

### D30 — a charge reads a Day the column has SETTLED, never a Day still being written

F22's answer, and it is deliberately not F19's answer. Phase B held its assessment's position with
two tests because a Business keeps only the Day it is accumulating; ***a position held by a test is a
position that can be moved***. A Building keeps a third column instead — what it emitted over the
whole of the previous Day — so a charge reads a complete Day from any phase, and the phase order
stops being load-bearing. ⚠ **The asymmetry with `BusinessTable` is real and is not an oversight**:
the profit assessment is committed and tested where it stands, and retrofitting it is a change to a
working mechanism rather than a finding.

### D31 — a shared allocation is apportioned by largest remainder, ties to the lower slot

D12 requires that processing order not decide who is paid, that no Money be created or destroyed, and
that a replay reproduce the payments exactly. Largest remainder gives all three: floor each claim's
exact share, then deal the leftover units one apiece to the largest fractional remainders, breaking a
tie on the **larger claim**, and only then on the lower slot. ⚠ **The scan start rotates on a
`Randomness.Draw`**, so an apportioner that deals its leftovers in scan order would satisfy every
other requirement and silently violate this one. Held by a permutation test rather than by reasoning.

🔴 **The brief for this said *ties to the lower index* and that was wrong.** An index is deterministic
within one run and still lets the scan decide: the same two claimants arrive in a different order on a
different Tick, and the unit follows the arrival. Ordering on the claim first makes the answer a
property of the claims. ⚠ **Two claimants holding an IDENTICAL claim remain separable only by
position** — one unit cannot go to both and nothing else tells them apart — so order independence is
exact per claimant while claims differ, and holds for the multiset of payments unconditionally. That
limit is the pigeonhole rather than a weak rule, and it is stated where the code is.

⚠ **The implementation bisects for the remainder threshold rather than repeatedly scanning for the
next largest.** Leftover units are bounded by the claimant count, so the obvious form is quadratic and
a subsidy claimed by a few thousand recipients would cost tens of millions of operations a Day.

### D32 — a subsidy's claim is a rate per worker employed

Chosen by the user on 2026-09-10 over a share of revenue and over a flat amount per Business. A flat
amount makes proportional rationing arithmetically identical to an equal split, so ***D12's mechanism
would ship exercised by no shipped world***; a share of revenue pays the most to the Businesses that
need support least. Headcount already exists per Business, varies widely, and makes the pot running
short visible in the panel on the Day it happens. **PROVISIONAL**, no ratifier.

### D33 — a charge rides the existing `[[policy]]`; relief and a subsidy cannot

A charge is a transfer of Money from a liable payer to the treasury on a derived quantity, which is
what `[[policy]]` already is — it needs a new `Readout` and D28's eligibility and nothing else.
**Relief moves no Money at all**: it reduces a tax bill, so it has no transfer to declare and its
effect is only visible inside the profit assessment. **A subsidy has a pot**, and `PolicyEngine.Move`
pays each member in full or not at all and abandons the rest of the sweep when the treasury runs
short — which is exactly the *processing order decides who is paid* that D12 refuses. ***So the split
is not a matter of taste***: two of the three tools are outside what a transfer can express.

### Phase C tasks

1. **A deterministic apportioner** — largest remainder over a span of claims, zero allocation, safe
   to a claim total of 3,037,000,499 and refusing rather than wrapping past it. ✅
2. **A Building's own emission** — three saved columns and one line at the write site, plus a
   `Readout` so a `[[policy]]` can price it. ✅
3. **The catalogue in the Ruleset** — `tool`, `trade`, `ceiling` and `relief_percent` on
   `[[policy]]`, with the refusals for every illegal combination of them.
4. **Eligibility in the sweep** — a Policy naming a trade reaches that trade, and a relief or a
   subsidy is not swept as a transfer at all. ✅
5. **Relief inside the assessment** — taken off after the marginal bands, added across overlapping
   reliefs, capped at the whole bill. ✅
6. **The subsidy sweep** — claims gathered, the pot taken as the smaller of the ceiling and the
   treasury, apportioned, then paid. ✅
7. **The player's second control** — a `Fund` verb for the ceiling, refused against a Policy that
   pays nobody and against a negative ceiling. ✅
8. **The panel, the counters and the reading** — three tools in the governing panel, Money flows the
   budget can be read off, and a demonstration world that exercises all three. ✅

### F23 — the charge D9 exists for was unauthorable, and nothing said so

A Building holds its emission, because `RuleEngine.Emit` attributes a firing to the Building the
Rule Instance stands in. A `[[policy]]` cannot sweep Buildings: there is no predicate that selects a
Building population and the loader refuses `sweeps = "building"` by name. 🔴 **So a Building-scoped
Emission Readout was one nothing could ever be charged on**, and the only quantity a charge could
reach was a balance — ***a wealth tax wearing a charge's name***.

⚠ **Every piece was built and tested and the assembly was still impossible.** The column, the write
site, the Readout and the loader all did what they were asked; the gap was between two of them and
lived in neither. It was found by the shell agent laying out the panel, which is three removes from
any of them.

### D34 — a Business reads the emission of the premises it occupies

`Readout.Emission` is readable against a Business as well as a Building: it resolves the Business's
premises and returns that Building's previous complete Day. An unpremised Business — one in the
Unplaced Pool — reads zero rather than throwing, because a trade with no Building has fired no Rule
and emitted nothing.

⚠ **A Household is deliberately not admitted.** A dwelling emits and its occupant did not decide to,
in any sense this design has settled. That is `adr/0070` *undesigned* rather than *refused*, and it
is the reason the scope is widened by one entity rather than to everything that tenants a Building.

## What watching Phase C exposed

### F24 — the subsidy made the payroll WORSE, and the phase order is why

Adding `employment_support` alone moved the demonstration city from 701,144 wages paid across 115
payments with 64,808 uncovered, to **689,348 across 112 with 76,604 uncovered**. The city paid its
grocers 170,304 to employ people and ***11,796 less reached the people they employed***.

The cause is not the subsidy. A Bin Rule fires in phase 3 and a payday runs in phase 6, so the
`rates` Rule reaches the till first: it fired twice more than it otherwise would have, taking
32,768 — a fifth of the whole subsidy — straight back to the treasury, and three paydays that would
have been met were not. 🔴 ***A subsidy paid into a till is not a subsidy paid to a worker***, and
nothing in the design had said which of the two it was.

⚠ **It is a finding about where money lands and not a defect in any of the three tools.** D10's
sentence is that a subsidy *transfers Money to the eligible recipient*, and the recipient here is a
Bin that the city's own charges empty first.

### F25 — the charge falls on the shops with custom, not on the shops that emit

Every standing shopfront in the demonstration emits the same amount a Day. The charge collects from
**8 to 15** premises' worth over the last seven Days against **22 live grocers**.

`PolicyEngine.Move` pays in full or not at all, so a till that cannot cover the whole bill pays
***nothing*** and is counted `Unaffordable`. ⚠ **So the shops that pollute for free are exactly the
shops that are doing badly**, which inverts the charge: a trade too poor to pay is a trade the city
stops charging. This is the all-or-nothing transfer arriving on a tool it does not suit — the same
property D33 kept a subsidy away from — and it is the one thing in Phase C that should be built
differently. ***A charge should take what the till has, on D25's own reasoning for a short profit
tax***; it does not, and no test asserts that it should.

### F26 — the subsidy's scratch was sized once and a save is what outgrows it

`SubsidyEngine` sized three scratch arrays from the Business table's capacity **at construction**
while the gather walks the live slot count. A `Simulation` rebuilt on a reloaded world is sized to
the rows that world had when it was written and then watches it grow, so the first Day a claim list
is longer than the world used to be is an index past the end.

⚠ **It did not fire from a fresh world at any size tried**, up to 6,000 Citizens over 40,960 Ticks.
***It is specifically a save-and-reload hazard***, which is the shape `adr/0112`'s Factorio test
exists for and which no amount of running forward would have found. The scratch is per-sweep and a
sweep runs once a Day, so sizing it once was a defect wearing an optimisation's clothes.

### F27 — charging the city for its pollution changed nothing else either

Against the same file without the charge: withheld identical, profit tax identical, rates identical,
public works identical, and the Businesses' money falls by **exactly** what was collected, to the
unit. The entire second-order effect is 1,000 of payroll and one extra short payday.

🔴 **This is the row's own earlier finding arriving on the lever that was supposed to be different.**
A profit tax is a pure extraction in this city because a grocer has no investment to forgo; a
pollution charge was meant to be the counter-example, because a Business could emit less. ***It
cannot.*** Nothing in the Ruleset connects emission to a decision, so the charge is a second
extraction with a different name on it. ⚠ **The mechanism is right and the world cannot answer it**,
which is `adr/0070` *unbuilt* rather than a design fault — and it is the clearest statement yet of
what this city still lacks.

### F28 — a charge and a transfer share one accumulator

`PolicyEngine.Move` folds both into one `ToTreasury` flow, so the reading cannot separate what was
charged from what was transferred. In the demonstration the charge is the only inbound Policy, so
the column *is* the charge — ⚠ **but that is a property of that world and not of the reading**, and
a second inbound transfer would silently merge into it. Open.

## Phase C — done

Shipped 2026-09-11. A catalogue of three targeted tools on `[[policy]]`, eligibility by trade, a
Building's own emission attributable to it, a deterministic apportioner, relief inside the profit
assessment, a rationed subsidy sweep, a second player verb for a funding ceiling, four panel forms
and a tenth `--income` column. `rulesets/taxing.toml` grew from six numbered changes to ten and
exercises all three tools, with the subsidy rationed on nine Days of twelve. Twelve new loader
refusals; the count of record moved 297 → 309. All four golden artefacts re-recorded; no Ruleset
content hash moved. The working lane is 3,361 green.
