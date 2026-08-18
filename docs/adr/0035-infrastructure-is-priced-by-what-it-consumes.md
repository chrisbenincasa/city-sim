# Infrastructure is priced by what it consumes, never by a budget

**Infrastructure is paid for three times — in Money, in Materials, and in Land — and none of the three is a limit anybody authored.** Money is a *transfer* rather than a sink, because [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) conserves it. **Upkeep is `construction cost ÷ effective life`**, where the life is a duration shortened by the traffic the Segment carries. And an Arterial's largest price is the **land it sterilises**, which is what retires this project's one remaining proposal to cap a player action with a budget.

## Context

Two things collided.

**`0014` left a loose end:** *"Nothing zones onto an Arterial… but it means Arterial corridors need something to be made of."* Nothing in the corpus answered it.

**And the corpus had no cost model for infrastructure at all.** `04 §5`'s budget had tax rates, service funding and borrowing. The words *maintenance* and *upkeep* appeared in no design document and in none of the preceding 34 ADRs, and no document said that building a road costs money. This is the failure mode session six named — *an unratified number is more dangerous than an open question* — one step worse, because there was no number to ratify.

## Decision

### 1. Three prices, all mechanisms

| Price | Unit | Why it is not authored |
|---|---|---|
| **Money** | Lane-Tiles, plus discrete Junction pieces | `0016` already makes the Lane the entity, so no new quantity |
| **Materials** | as any construction | already the growth brake in [`0022`](0022-land-is-a-stock-the-city-spends.md) |
| **Land** | Sealing, plus the strip an Arterial sterilises | falls out of `0022` and of `0014`'s own frontage rule |

### 2. Money spent on infrastructure is a transfer, not a sink

Conservation means a § spent on a road becomes somebody's income. Construction consumes Materials; bought from local Processing the money becomes local wages, imported it leaves through the gate. **Public construction is therefore a stimulus or a leak, and which one is decided by the Materials chain rather than by the spending.**

*A city that builds while importing all its Materials is exporting its own stimulus.* This is a new and **economic** argument for a domestic Materials chain, where every previous argument for it was logistical.

### 3. Upkeep is design life consumed by wear

Every piece of Infrastructure has a **design life**: a duration in Days at zero traffic. Upkeep drawn per Day is `construction cost ÷ effective life`, and traffic consumes the life faster — a **base term** plus a **wear term** reading the volume the Segment already tracks.

The formulation exists to keep the only authored number a **duration**. A flat *§X per Lane-Tile per Day* is a magnitude constant, which this project distrusts; a design life is scale-free and means the same thing in a village and a metropolis. It is also how real infrastructure budgets amortise.

It is legible as a **pipeline rather than a bill** — *"this corridor has 40% of its life left; at current freight volume it needs rebuilding in 120 Days"* is a reading of present state, the same move that made `Schooling`'s lag legible.

Mechanically it is a **Sweep Rule** ([`0033`](0033-two-rule-families-scheduled-and-swept.md)) on the staggered slot `05 §9` already reserves — sweeping a population, which is that ADR's own discriminator. It needs no Event Wheel entry per Segment.

> **⚠ AMENDED 2026-08-18 by [`0117`](0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md): this sentence names a family and a cadence and never names what the Rule is attached to, and that is the blocker.** Every scope in the engine resolves through a Building — `RuleEngine.Bin(World, **int building**, …)` in its own signature, `local` as *"Bins on the **Building** running the Rule"*, `pool` as *"Bins on the **Building's** District Pool"* — and [`0114`](0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md) enumerates a Bin's four owners as **Building, Household, Business, treasury**, with no Segment, correctly, because a Segment holds no money and never will. **Upkeep's subject is a Segment, its payer is the treasury and its counterparty is a market — three different things — and the engine has no Rule whose subject is not its payer.** ***Naming a Rule family says how often a Rule runs and never what it is attached to.***
>
> **⚠ And this section's formula disagrees with this ADR's title.** *"`construction cost ÷ effective life`"* is an **authored money amount**, which [`plans/0011`](../../plans/0011-rule-engine-bins-and-rules.md) finding 6 classes as a **transfer** — both ends named, balancing inside its own atomic Rule. But §2 above sends the money to a *supplier at a market price*, which is [`0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)'s **purchase**, and a purchase **has no syntax at all**. It cannot be both, and the title says which: ***infrastructure is priced by what it consumes, never by a budget***. Whether the authored quantity is Money or **Materials** is left to whoever builds this, because it decides the mechanism rather than its schedule. ***A document can state its principle in its title and contradict it in its formula, and the formula is what a builder implements.***
>
> **Neither is a reason to withdraw anything here.** §3a's degradation chain, the wear weighting and the *pipeline rather than a bill* reading are untouched, and the road-side inputs they need — Segment volume, capacity and free-flow speed — all shipped in milestone 5c. **What moved is the schedule**: Upkeep was assigned to milestone 10 and is now **12**, where `Scope.Pool` first gives its payment a counterparty. Three of its four blockers survive that move.

**Remaining life is stored as accumulated wear, an absolute count — never as a fraction of design life.** [`0015`](0015-all-tuning-data-is-hot-reloadable.md)'s test is whether existing state was recorded in units of the constant: a stored fraction would be silently rescaled by a Ruleset edit, and design life would become a world-creation constant. Stored as wear, it stays hot-reloadable.

### 3a. Upkeep is drawn automatically, and the unfunded state is reachable

**There is no maintenance funding lever.** `04 §5`'s financial levers are deliberately few, and a slider whose only sensible setting is *as high as affordable* is not a decision. The draw is automatic.

**But borrowing is a player action, not an automatic overdraft** — which corrects the reading of [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md)'s *"a deficit becomes a debt burden."* The damper exists and must be **reached for**; an automatic one would delete the decision. So the treasury genuinely empties, and then:

| Step | Mechanism it uses |
|---|---|
| the maintenance Bin Rule cannot draw | it **waits and subscribes to the Bin that was short** — `0033`, unchanged |
| life is not renewed, wear accumulates | already tracked |
| **capacity and free-flow speed fall** | both are Road Graph edge attributes the VDF already reads |
| the player is told why | `on_fail` — *"maintenance failed 40 times in 30 Days: treasury empty"* |

Degradation is therefore **not a system**. It is what a Rule not running looks like, and a decayed road is simply a slower road with less capacity. The player's exits are borrow, tax, cut services, or prune; refusing all four is a supported outcome with visible consequences. `NO VERDICT`

### 3b. Overfunding is the preservation curve, and it is a Policy

Pavement decays slowly and then rapidly. A cheap treatment in the slow phase restores condition; reaching the rapid phase requires full reconstruction at several times the cost.

| Rebuild at | Cost | Cycle | Amortised |
|---|---|---|---|
| 40% life remaining | ~0.2 × construction | 0.6 × life | **~0.33 C/L per Day** |
| 0% — reconstruction | 1.0 × construction | 1.0 × life | **~1.0 C/L per Day** |

So the attentive player genuinely pays less. This needs **no new lever, because it is a Policy** — parameterised, player-set threshold, sweeping a real distribution (remaining life across Segments), with a derived reference point shown beside it: *"rebuilding at 40% costs §12k/Day; at 10% it costs §9k/Day now and §31k/Day in 200 Days."*

**It is District-overridable, and that is what makes it a decision rather than a wealth readout.** The cheap strategy requires capital sooner and more often, so a player who commits to it **cannot commit everywhere** and must choose which corridors to preserve. The alternative — defer, and bet on having the money when the bill lands — is an equally coherent play style rather than a mistake. Global-only scoping would have left the player choosing *how much* and never *where*, which is the objection this scoping answers.

**The bet is settled by the demographic transition.** Deferring works while revenue grows, and revenue growth slows at the crossover where internally-generated Households overtake arrivals. *A city that defers maintenance is betting on continued immigration* — a coupling between two systems designed years apart for unrelated reasons.

**And the liability must be told, not shown.** A decayed road has a visual signature; an *accumulated deferred liability* does not, and thread D's rule applies — **announce what the city cannot show.** Condition belongs on the map; the size of the bill coming belongs in a readout.

### 4. Arterial corridors are made of Processing industry, and of nothing

`0014`'s loose end resolves with no new mechanism, because three existing rules meet:

- Land with **no Street frontage** is unlotted and undevelopable. That is `02 §2.2`'s stated behaviour and `CONTEXT` → Frontage already names it — *a dead block interior*, arriving from a different cause.
- Land with frontage on a **parallel Street** is ordinary, but under [`0034`](0034-fields-are-sorted-by-source-geometry.md) it is provably the noisiest and dirtiest in the city, so it is the cheapest by bid price.
- **Processing industry is located by reachability of inputs and buyers** (`CONTEXT` → Zone), and a corridor is maximum reachability. It bids high on land whose only defect is the thing it does not care about.

So the corridor fills with Processing and warehousing, and nobody authored *"industry goes by the highway."* Access runs ramp → local Street → Lot, so the premium concentrates **near Junctions** rather than along the corridor — which is what real logistics geography looks like, and which makes **ramp placement a land-use decision**, the second instance of `0014` §19's point about Junction pieces being planning choices.

### 5. `0014`'s "probably budgeted" is retired

That ADR's first consequence reads *"Arterials must be genuinely rare, and probably budgeted."* A budget is an authored cap whose only justification is *otherwise players lay too many*, which fails this project's own test: **would you have written it without knowing about the exploit?**

Three mechanisms now restrain Arterials without one. They **seal** land, they **sterilise** the strip they cannot give frontage to — ruinously downtown, cheaply in the periphery, which is both correct urbanism and self-limiting exactly where the junction combinatorics `0014` feared would arise — and they carry the **largest upkeep** in the network because they carry the most traffic. The player's counter-lever is to run a parallel Street and reclaim the strip, which is what a frontage road is.

## Consequences

- **Roads Seal.** Previously Sealing was only ever discussed through Buildings, leaving the road network invisible to Fertility. The verge stays *unsealed*, so an Arterial sterilises land for development without sealing it against recovery, and Woodland regrows there.
- **The Bill axis gains its first mechanism that responds to *use* rather than to *size*** — `01-player §5` had few.
- **The Bill converts into the Clock.** `01-player §5.1` names two pressure axes and treats them as independent; an unpaid bill lengthening every commute is the first mechanism that couples them. A fiscal crisis becomes legible on the map rather than only in a number.
- **Decline gains a spatial vector.** An abandoned District's roads decay, so it becomes harder to reach, so it worsens — `adr/0030`'s *neglect must not be containable*, arriving through infrastructure rather than crime. It is a **cycle rather than a spiral** for a reason that falls out of the wear model itself: **a collapsing city's roads decay slowest**, because wear scales with use.
- **Right-sizing the network is the real counter-play**, and it is another entry for ledger #4's endgame levers. Pruning your own work feels unnatural, and the lesson is precisely that the network was built past what the city could carry. It requires one thing to be a decision rather than a chore: **the Upkeep bill must decompose by utilisation** — *"§34k/Day, of which §12k is carried by Segments below 15% of capacity"* — or the player is deleting roads at random.
- **The cheap strategy is unavailable to a poor city.** Preservation needs capital sooner and more often, so a squeezed city defers and deferral compounds. This is `CONTEXT` → Destitution's shape at city scale — *every exit costs money you do not have* — and it is how real municipal deferred-maintenance crises actually work.
- **Transit's economic case strengthens with no rule written**, because fewer vehicle-km is less wear.
- **A tension in `0014` worth naming:** sterilisation rewards aligning Arterials to the grid, which is mildly at odds with their being freeform. The defence is that freeform buys curves and interchanges rather than misalignment, and that threading a highway to waste less land *is* the planning skill — but it is a real pull and should not be discovered by a player.
- **The endgame gains a second edge.** `0022`'s mature city has sealed its land and imports Materials — and now imports them partly to maintain the network whose upkeep has become its largest standing bill.
- ~~*what happens to Infrastructure whose upkeep goes unfunded*~~ — **settled in §3a**, and the deferral was wrong: it is not a decay system, it is a Bin Rule that did not run.
- **Design life values are the balance surface of the whole Bill axis** and are not authored by hand. They are derived from the share of a mature city's budget Upkeep should occupy — the same *sizing is a derivation, not a constant* move that produced the 1M target. The wear weighting by vehicle class is grounded separately: road damage scales superlinearly with axle load, so **freight dominates wear and commuters barely register**, which makes freight routing consequential and is the reason a Processing corridor is the most expensive road in the city to keep.
