# 0045 — The amnesty

**Active. Start here; this page is the board while amnesty runs. Next: row 29.**
Updated 2026-09-10. Amnesty ends at the prose/code ratio target, never on a date.

## Status

The recent capability work is complete: housing consequences (16), shopping (20), daily schedules
and care (19), Business insolvency (27), the choice model with incumbent and Outside
alternatives (28), and education changing a working life (29). Their findings and the earlier completed rows are preserved in
[the historical queue](0045a-amnesty-history.md#the-queue). Completion is scoped to those rows;
follow-ups below remain open.

At `36ff4fc`, before this tracking edit, the test's counting rules measured **2,156,100 prose
words / 52,107 source lines = 41.38 words per line** (integer reading **41**). The ceiling is
now **41**, lowered from 52; the exit target remains **30**. `CorpusBudgetTests` owns both checks.
The numerator includes Markdown under `docs/` and `plans/`, plus C# comment lines under `src/`
and `tests/`. The denominator includes nonblank, non-comment C# lines throughout `src/`, including
the shell. Both comparisons use integer division. Moving findings into history does not remove
them from the count.

## Standing orders

1. **No new ADRs.** The test ceiling is 171, including the two pre-amnesty records merged later.
2. **No new entries in `plans/0002` §A–§F.** Its word ceiling remains 153,786. Keep deferred
   questions in the queue below; decisions needed for active work belong in that item's plan.
3. **Keep lowering the ratio ceiling as the integer reading falls.** Prose and code comments
   count wherever the test includes them; adding simulation can support explanatory prose.
4. **`adr/0043` and `adr/0052` remain suspended.** Choose tuning by taste, mark it `PROVISIONAL`,
   and open no ratification row or new ADR.
5. **A session ending without a change under `src/` is not committed.**

`CorpusBudgetTests.The_amnesty_has_not_yet_earned_its_end` goes red when the exit target is
reached. That failure reports completion; lifting the orders is an explicit follow-up.
Changing a restriction requires changing its test in a commit explaining why.

## The queue

The table's position is the order; numbers are permanent identities. Rows 29 and 30 retain their
identities; 31, 32 and 33 are new. Row 29 is done, 32 is scoped, and 33 needs a design pass
before it can be scoped. The roadmap supplies
capabilities; this page selects the work; each item's plan owns its detailed scope and findings.
The old [board](0000-board.md) is suspended. Read the relevant roadmap, code and gates when taking
up an item, not the whole historical corpus. Expanded scopes do not automatically clear gates.

| Row | Capability and scope | State / completion evidence |
|---|---|---|
| **29** | **Education changes a Citizen's working life.** Connect actual attendance, Skill Tier, employment eligibility and access to different earnings, through the school-to-work transition and Household consequences. Include experience progressing Tier 1 → 2 and the schooling requirement for Tier 3. | **Done 2026-09-09.** [0071](0071-education-changes-a-working-life.md) owns the sixteen decisions and the eight findings. Attendance is counted per level per child, a childhood is scored 70/30 against Days attended and the Household's Education depth, and the score sets the Skill Tier at formation; experience carries Tier 1 → 2 and never past it; `[[business]] requires_tier` refuses below the credential and `CitizenTable.Employment` now names the reason; earnings are the posted wage times a tier percentage times an experience premium. A university is entered by a Household, public places first and a private college on overflow, tuition per Day with drop-out on default. `rulesets/schooling.toml` and `--school`'s pipeline panel are the demonstration. 🔴 **The unsuccessful path is the one that surprised**: every Household in that world is destitute by Day 3, so nobody can buy a private degree and the split is proved by `SchoolingTests` rather than by the world — [0071 F1](0071-education-changes-a-working-life.md). |
| **31** | **The city attracts people without injected arrivals.** Connect Hinterland population stock, who presents themselves, the existing choice model, gate throughput, admission and placement. Include depletion and replenishment, with population accounting and changing city conditions. | **Queued for scoping.** Continues row 28's explicitly deferred stock half; [0068 D6](0068-the-choice-model.md#decisions) owns the gap. Demonstrate arrivals changing as opportunity and the Outside change, without repeated player or Input Log requests supplying the flow. |
| **32** | **The city spends money and the player decides on what.** Connect the treasury, a service that costs money to run, the funding lever and the consequence of withdrawing it. Include an opening balance, a cost on placing a service, and money conserved across both. | **Scoped 2026-09-09; not started.** [0070](0070-the-city-spends.md) owns the scope and the three findings. Watch a school be funded, staffed and paid, then lose its funding and close. |
| **33** | **The city's income grows with the city, and the player sets the rate.** Connect a tax base that responds to the city's condition, a rate the player sets rather than a quantum, and the consequence of setting it wrong. Include a levy that reads something other than a standing balance, and a budget the player can read income and expenditure off. | **Phase A shipped 2026-09-10; B, C and D open.** [0072](0072-city-income.md) owns fifteen design decisions, seven more taken while building, and fifteen findings. **A Citizen's earnings are taxed at the payday**, not a standing balance: three marginal bands per Day, four rates the player sets from the Policies panel, a 32-Day schedule history so a late wage is taxed at the Day it was earned, and one till debit against two credits that sum to it. `rulesets/taxing.toml` and `--income` are the demonstration. 🔴 **What surprised was the budget rather than the tax**: two separate paths paid the treasury and no flow counted either, so **89% of its income was unattributed** — the fix is worth less than the test now asserting that the balance equals income less expenditure. ⚠ **Not closed.** Business profit tax on accrual accounting (D8, D15) and the charge/relief/subsidy catalogue (D9–D14) remain, and the first needs per-Business revenue and expense state that does not exist anywhere. |
| **30** | **A player diagnoses decline, intervenes and watches the result.** Connect sustained failure sources, their consequences, Evidence and an available player action. Preserve the original Trip-failure and below-tolerance scope; include recovery and persistent failure. | **Expanded; queued for scoping and gate review.** Carries milestone 17's failure-source residue and builds on [0064](0064-the-information-interface.md#first-diagnosis-interaction--2026-09-08). Watch an intervention remove a cause and produce recovery, or expose a remaining cause that prevents it. |

**29's boundary:** `CitizenTable.SkillTier`, `CivicEngine`, `EmploymentEngine`, `WorkSchedule` and
`WageEngine` are the starting points. Use `CONTEXT.md` → Skill Tier and Schooling, and `adr/0104`,
to settle how the existing individual school visits feed qualifications at Household formation.
Keep credential requirements as eligibility filters. Include meaningful job access and earnings
consequences in the same demonstration; a saved tier and a filter alone do not close the row.
The expanded schooling levels, experience and job-transition details belong in its scoping pass.

**31's boundary:** `Simulation.ApplyArrive` and `PlacementEngine` own the built comparison and
admission path. Scope the stock's composition, drawdown, recovery and relationship to Departures
before implementing its generator. Keep the gate's capacity distinct from willingness to arrive.
The stock must be saved, hashed and bounded. This row does not require a new Shock system or an
Intensity Dial; changes to existing city conditions must suffice for the demonstration.

**32's boundary:** `PolicyEngine`, `WageEngine`, `World.CreateBuilding`'s trade instantiation and
`Simulation.ApplyService` are the starting points. A Policy sweeps a whole table today, so funding a
service needs it to name a trade first. Keep catchment staffing out: a school employs what its floor
area divides into, and `adr/0026`'s demand-determined version stays unbuilt. The placement cost adds
an exit door to the money supply and must keep `Invariant.MoneyIsConserved` green. Opening balance and
funding level are provisional under standing order 4. A treasury that merely fills does not close the
row; the player must be able to spend it and see what spending bought.

**33's boundary:** `PolicyEngine.SweepMembers`, `Readouts.Declared`, `PolicyTable.Govern` and
`Ruleset.EmigrantBalance` are the starting points. What is unbuilt, so the row can say which of it it
builds:

- **A Building population.** `02 §4.2` names three populations a Policy may sweep;
  `PolicyEngine.SweepMembers` throws on `building` and the loader refuses it by name, because the
  predicate that selects the rows does not exist. ⚠ `occupancy` is declared readable against a
  Building and nothing else, so ***the one scalar suited to a property tax sits behind the one
  population a Policy cannot reach***.
- **A Readout that is not a standing balance.** `Readouts.Declared` holds `Occupancy` and `Balance`.
  No earnings, output, sales or land-value scalar exists, so no levy can respond to the city
  prospering.
- **A rate.** `PolicyTable.Govern` edits the transfer quantum and the panel says so; `percent` is
  Ruleset-only. `04 §5`'s lever is *set tax rates*.
- **Borrowing.** `04 §5`'s third lever and `adr/0024`'s damper against a seizing economy.
  `adr/0035` settles that it is a player action and never an automatic overdraft. Nothing is under it.
- **Capital expenditure and upkeep.** `adr/0035` prices both — construction against Lane-Tiles and
  discrete pieces, upkeep as construction cost ÷ effective life drawn per Day. Nothing draws
  either, and `04 §5` records that both were absent from the whole corpus.
- **A balance of payments.** `adr/0024` makes imports and exports the endgame;
  [`0070` F3](0070-the-city-spends.md) found no import path — a Hinterland price seeds a Pool's
  price and no Bin receives a payment. Until money crosses the gate, revenue can only move what
  arrivals carried in.
- **A budget the player can read.** The Census carries `ToTreasury` and `FromTreasury`; the console
  shows a balance. Nothing states income against expenditure per Day.
- **The regressive floor.** `FloorDiv(balance × percent, 100)` collects nothing below
  `100/percent`, `PolicyEngine` counts it as `Floored`, and nothing acts on the count —
  `adr/0115`.

**Refused, and this row must not build either.** There is no annual budget cycle: `adr/0010` gives the
game no calendar and revenue accrues per Day. There is no maintenance funding slider: `04 §5`
refuses a slider whose only sensible setting is *as high as affordable*.

⚠ **Scope the tax base before writing a task list.** Which base the city taxes — a standing
balance, a flow of earnings, or a property's value — decides which of the above is on the critical
path. `04 §5`'s second-order claims are arguments with nothing under them: a residential rate as a
fertility decision, and a rate as a velocity control. Standing order 1 forbids a new ADR, so that
argument belongs in this row's own plan. ***The list above is a survey and not a task list*** — a
levy that scales with one thing the city does, a rate the player sets, and a consequence they can
watch, closes the row.

**30's boundary:** `TripEngine`, `RuleEngine` and the existing decline mechanisms produce the
causes; Evidence reports them. Specify durations and attribution without substituting event counts
for elapsed failure or welding a Trip window to a Building's condemnation setting. Reuse the supply
diagnosis interaction and keep its remaining interface tasks in `0064`; do not rebuild that work.
Scope at least one additional failure source through consequence and intervention before coding.
Counting failures or adding an explanation label alone does not close the row.

## Definition of done, amended

**Done means you watched it and were surprised.** Each item spans a trigger, a decision or response,
consequences, and something the player can observe or influence. Ship the Ruleset demonstration,
inspection and relevant replay/save checks with the mechanism. Record what watching exposed in the
item's own plan. Keep small implementation tasks inside the capability; do not promote every missing
reader, defect or panel into a new top-level row. Existing milestone gates still apply.

## Follow-ups attached to their owners

These are retained obligations, not competing top-level priorities. Historical completion does
not discharge them. Check their owners when the relevant work is taken up.

| Follow-up | Owner / occasion |
|---|---|
| Choice stickiness can make alternatives impossible at the arithmetic horizon | [0068 D5](0068-the-choice-model.md#decisions); address when extending the choice path in row 31. |
| Driven hover evidence needs deterministic aiming without acting | [Historical aim findings](0045a-amnesty-history.md#what-the-aim-found); check the current drive channel before row 30's observation work. |
| Splitting `Main` removed file-size pressure without replacing it | [Historical row 26](0045a-amnesty-history.md#the-queue); revisit when extracting shell responsibilities. |
| School matching and care costs remain unmeasured | [0013](0013-tick-budget.md); owned performance work, not a claim that these costs passed a budget. |

## Owed when the freeze lifts

Transfer still-open questions to `0002` when standing order 2 lifts. A trigger reached earlier is
handled in the relevant active plan under amnesty. Each row keeps its question, type and trigger;
[the original discussion](0045a-amnesty-history.md#owed-when-the-freeze-lifts) preserves the arguments.

| Question | Type | Trigger / owner |
|---|---|---|
| Should children pair into one Household at formation, or each form their own? | Arguable | A mechanism distinguishing two adults in one Household; [0046](0046-life-stages-and-a-self-generating-population.md), decision 3. Recheck that trigger during row 29 scoping. |
| What is the natural playing speed, and what does a player do at each rung? | Arguable | A game with distinct phases to play; `01-player-experience.md`. |
| What may the ground communicate beyond a readout? | Arguable | Ground markings that imply a claim; [0049](0049-visuals.md), “What the ground may say”, F46–F52. |
| What does a Business's founder lose in bankruptcy? | Arguable | Founding common enough to watch; [0065](0065-business-insolvency.md) and `adr/0146`. |

The uniform-lattice question was answered and became completed row 25; its tombstone remains in
history. The Lot-count question and choice-model follow-ups remain with their owners above.

## Keeping this page small

Keep only status, standing orders, the next few capability rows and links to outstanding obligations.
When a row closes, move its scope and findings to its own plan and leave a short linked completion
entry. Preserve identities and source evidence. Add no session narrative here or to the frozen
[history](0045a-amnesty-history.md). This page is the sole active priority queue during amnesty;
[the roadmap](../docs/06-roadmap.md) supplies the larger capabilities and retains its milestone identities.
