# A Business is an Occupant with its own balance, and a Building never holds money

**A Business is an entity: a row in a `BusinessTable`, an Occupant of a Building, holding its balance as a column on its own row exactly as a Household does. A Building never holds money.** Milestone **10** creates the table and the balance and **nothing else of a Business** — no inputs, no outputs, no employment, no market behaviour, all of which belong to the milestones that already own them. `UNIQUE INDIVIDUALS` `LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM`

## Why

### The two ADRs that appeared to disagree never did, and the build is what disagrees with both

Scoping milestone 10 ([`plans/0033`](../../plans/0033-conserved-money-and-the-treasury.md)) recorded a contradiction: [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md)'s *"**Businesses hold money.** Closes the open question of whether they hold only Bins. They hold a balance"* against [`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s *"[a Building] may hold Bins, one Access Point, one Parking Shed. It may **never** hold a Need, **money**, a Provider List, or a Trip."*

**That reading was wrong, and `CONTEXT.md` had already settled it.** → Occupant: *"A Household or a **Business**. What fills a Building."* → Business: *"The commercial or industrial economic actor **occupying** a Building."* A Business is not a Building; it is one of the two things that occupy one. Both ADRs are correct as written and neither needs an amendment.

**What the contradiction was actually with is the build.** There is no Business in `src/` — no table, no occupant kind, and the word survives in two doc-comments. `BuildingTable.OccupantHead` links `HouseholdTable.DwellingNext`, so a Household is the only Occupant that has ever existed, and the Bins that make an economic actor an economic actor hang off a **Building handle** (`BinTable.Owner`, `BinTable.cs:59`). So a Business's money had nowhere to go except the one place `adr/0025` forbids by name — and it had nowhere to go because **the actor it belongs to does not exist**.

⚠ ***An apparent contradiction between two documents can be a contradiction between both of them and the build***, and the tell is that neither document is wrong when read on its own. [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) governs the repair: the Business is **unbuilt** rather than refused, so *should a Building hold money instead* is a void question and the answer is to build the Business.

### `adr/0025`'s clause is load-bearing here rather than incidental, so amending it was the wrong exit

The cheap exit was a money Bin on the Building with an amendment banner on `adr/0025`. It is wrong on that ADR's own test — *"a Building field that would have to be averaged across its Occupants is a Cohort forming"*.

`BuildingTable.OccupantHead` is the head of a **list**. A Building may therefore hold several Occupants, and a money Bin on the Building is not *an* actor's balance but a sum over however many are in it — which is an average wearing a total, and it is [`0005`](0005-two-fidelity-tiers.md)'s deleted Cohort re-entering at exactly the place `adr/0025` predicted it would. **The clause is not a stylistic preference about where fields go; it is the Cohort prohibition applied to the container.**

### A balance is a column on the actor, because that is already how the one existing actor holds one

`HouseholdTable` declares `Money` and `Savings` as `Saved<Money>` **columns** (`HouseholdTable.cs:44-45`), not as Bins. A Business's balance takes the same shape for the same reason, and the symmetry is the point: `adr/0024` says *"every actor needs a balance, which is one integer per Household and per Business — trivial against records that already exist"*, and it is trivial in exactly the way that sentence claims only if both actors spell it the same way.

### The occupant list stays homogeneous, on a precedent the build already set twice

A Business as an Occupant does **not** mean one polymorphic list holding two row types. `BuildingTable` already carries two homogeneous intrusive lists on the same row — `OccupantHead` into `HouseholdTable.DwellingNext`, and `WorkerHead` into `CitizenTable.WorkerNext` — so a Business list is the **third axis on the precedent of the second**, and every list stays typed. This keeps `CONTEXT.md` → Occupant as a *concept spanning two lists* rather than a discriminated union in a codebase where lint 7 forbids reference types in simulation state.

### Building only the balance is what keeps this from becoming another milestone

A Business, fully modelled, *"consumes inputs, produces outputs, employs Citizens, and offers Goods or services to the market"* — which is commercial and industrial placement (`06` milestone **13**), the labour market (**15**), and the Pool as a market (**9**). Milestone 10 needs exactly one property of a Business: somewhere for conserved money to sit that is not a Building. **It builds that and stops**, and the entity is available for the milestones that need the rest.

## Consequences

- **`BusinessTable` exists, with a `Money` column and a Building handle**, and `BuildingTable` gains a `BusinessHead`. Both are new saved columns, so the State Hash moves and the three golden baselines re-record.
- ⚠ **`06` gains a mechanism it never listed.** *Business* appears **nowhere** in that document — not in the milestone table and not in either inventory of unscheduled mechanisms — despite being defined in `CONTEXT.md` and given a balance, a margin and a bankruptcy diagnosis by `adr/0024` and [`0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md). ***An inventory row naming a mechanism that acts on an entity reads as scheduling the entity*** — *Commercial and industrial placement* is placed at 13, and it places Buildings rather than creating actors. This is the fifth recorded blind spot in that table and the first where the mechanism is fully **designed**.
- **`adr/0024`'s *Businesses hold money* becomes buildable**, and with it `adr/0050`'s margin — *"it buys inputs at Pool prices, sells outputs at Pool prices, and the difference is a margin nobody had to invent a mechanism for"* — and the two-Bin failure surface that makes bankruptcy *"a distinct diagnosis from input starvation"*.
- **Neither `adr/0024` nor `adr/0025` is amended.** Both were correct. `plans/0033`'s decision 1 is corrected in place rather than struck, because the wrong reading is the instructive part.
- ⚠ **How many Businesses occupy one Building is *undesigned*** (`adr/0070`), and nothing in the corpus states it. It does not block this decision — a balance on the actor is right at any cardinality, which is the property a Bin on the Building lacks — but **it blocks the first money term a Rule fires on a workplace**, because `local` money must resolve to *an* actor and a list does not name one. Recorded rather than answered.

## What would trigger revisiting

- ⚠ **The balance turning out to be unreachable by a Rule in the shape chosen here.** This ADR puts a balance on the actor and does **not** decide how a Rule term reaches it — `BinTable.Owner` is a `HandleColumn<Building>`, so no Household, Business or treasury can own a Bin today, and `RuleEngine.Bin` resolves `Scope.Local` through `World.FindBin(buildingSlot, resource)`. [`0065`](0065-a-bin-holds-a-long-and-unbounded-names-a-ceiling-whose-approach-is-a-defect-rather-than-a-refusal.md) left exactly this open — *"what this does not settle: whether money belongs in a Bin at all"* — and it is milestone 10's open decision 6. **If that decision widens `BinTable.Owner` rather than giving money its own term kind, re-read this ADR's *a balance is a column* against it**: the two are compatible but they are not the same claim, and one spelling would make a Business's balance a Bin the Business owns.
- **A second Occupant kind arriving that is neither a Household nor a Business.** Two homogeneous lists is the right shape for two; at four it is a table of lists and the polymorphic question reopens honestly.
- **A Building genuinely needing a balance of its own** — as distinct from its Occupants' — for something like a body corporate or a shared service charge. `adr/0025`'s Cohort test is the thing to re-run, not this ADR.
