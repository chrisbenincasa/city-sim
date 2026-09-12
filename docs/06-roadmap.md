# Capability roadmap

[The backlog](../plans/0000-board.md) chooses current work. This page describes longer-term
capabilities and their dependencies; it does not maintain status or assign a second sequence.
Read [PROCESS.md](../PROCESS.md) for how work is planned and validated.

## Direction

The city should consist of people whose choices have visible consequences, an economy whose
Goods and Money can be followed, and interventions whose effects the player can explain.
Each capability should connect a trigger, a response, a consequence and something observable.
Prefer completing those connections over adding another isolated table or panel.

The historical project Phases remain useful orientation: experiments (0), simulation foundations
(1), interacting city systems (2), and presentation (3). Presentation is developed alongside the
simulation so a capability can be watched. It is not held until the rest of the simulation is done.
Existing milestone numbers are historical identities, not an instruction to start them in order.

## Capability families

| Family | Outcome to develop | Dependencies and scope boundaries |
|---|---|---|
| Population and housing | A city attracts people, accommodates changing Households and loses them for explainable reasons | Life Stages, housing alternatives, the Outside population account, admission and placement. Row 31's active plan owns its scope. |
| Public finance | The player raises income, funds Services and sees who benefits or loses | Income and expenditure must explain the treasury balance; costs need a counterparty or an explicit conserved exit. Row 32 owns current spending work. Borrowing is a player decision, not an automatic overdraft. |
| Decline and recovery (17) | Diagnose sustained failure, act on its cause and observe recovery or a remaining obstruction | Distinguish Trip failures, failed Rules and below-tolerance conditions. Evidence reports causes; it does not create them. Row 30 owns the next scoping pass. |
| Development and prices (13) | Goods prices, construction costs and available land change what gets built | Scope private capital, Materials use, the development calculation, density caps and commercial/industrial placement together where they depend on one another. |
| Work, Services and qualifications (15) | Access to Services changes participation, earnings and Household choices | Attendance, staffing, wages and credentials must produce consequences. Office and agglomeration require a separate scope; their absence does not justify inventing a substitute. |
| Freight and Outside trade | Shipments move Goods between Districts and through gates, with payment and congestion | Scope cargo, Vehicles, counterparties and destinations. The gate throughput minimum involving road capacity becomes meaningful when cargo actually travels; a price ceiling alone does not embody trade. |
| Driver behaviour and traffic (21) | Drivers respond to congestion and the network develops observable queues | Scope Habit, Sight, Temperament and diversion before tuning their parameters. Free-flow route choice with congestion paid only on entry does not supply that feedback. Mode choice and Transit need their own design. |
| Fidelity and audit (22–23) | Bounded detailed traffic agrees with the coarse simulation within explicit tolerances | Detailed traffic precedes promotion/demotion; promotion machinery precedes rotating audits. Camera position must not choose fidelity. Measure queue preservation, stressed Vehicles and divergence on the actual mechanism. |
| Terrain, extraction and hazards (24) | Ground and hazards create constraints the player can respond to | Height requires a usable Terraform interaction and price. Shocks and the Intensity Dial need a scope distinct from static terrain. Tie extraction, regeneration and hazard response to real stocks and Trips. |
| Services beyond attendance | Health, dispatch and Service variants change the city rather than merely adding coverage | Define Incident response and what successful containment changes. Revisit childcare, elder care and death care through their effects on labour, housing, land and Trips. |
| Information and presentation | The player can inspect a cause, compare outcomes and notice deterioration | Extend the existing interface. Trajectory detection, Pins, overlays, empty-Lot diagnosis and modelled markings need meaningful underlying quantities. Measure rendering and panel costs when their scale becomes relevant. |
| City-scale performance | Representative cities meet the chosen simulation and frame budgets | Attribute costs before optimising. Use actual Trip frequency, due Rules and developed land; do not price a million people by an unsupported multiplier. Preserve determinism and behaviour when classifying a change as an optimisation. |

These families are candidates for scope, not claims that every part is unbuilt. Check code and
current worktrees before proposing a task. Existing gameplay refusals, including the absence of
an annual budget cycle and a maintenance slider whose only useful setting is maximum, continue
to apply unless explicitly reconsidered.

## Additional candidates and deferrals

[0074](../plans/0074-the-systems-nobody-named.md) defines additional systems and their attachment
points: death care, childcare, elder care, segregation, public opinion, homelessness, road pricing,
cycling, waste disposal, evening destinations, district heating, land banking, street lighting and
content destinations. A definition is not a commitment to build it.
[Deferred work](deferred.md) retains deliberate deferrals, retrofit costs and revisit triggers.
The backlog indexes these candidates, including work needing scope or design and deferred work.
An entry makes the candidate discoverable; it does not commit to implementation or a milestone.

## Historical sequence and evidence

The original milestone tables, numbering migrations, dependency discussion and scope findings
remain available in [the pre-migration roadmap](https://github.com/chrisbenincasa/city-sim/blob/be07abc3b90a8102cc77ac131fd8852fbf853596/docs/06-roadmap.md).
That version explains old citations; it does not schedule current work. Measurement evidence is
indexed in [spike results](spike-results.md), with current Tick-cost investigations in
[0013](../plans/0013-tick-budget.md). Completion is recorded by Git and PRs.
