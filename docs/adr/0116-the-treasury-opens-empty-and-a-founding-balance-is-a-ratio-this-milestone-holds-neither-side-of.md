# The treasury opens empty, and a founding balance is a ratio this milestone holds neither side of

**Milestone 10's treasury opens at zero, no key authors it, and that is derived rather than chosen — an empty treasury is what makes `02 §4.2`'s exhaustion branch reachable on the first Tick.** The **founding balance** is a different quantity and is deferred: its two ends are both denominated in what things cost, and nothing costs anything yet. **Gift or loan stays open, and this ADR shows that staying open is free rather than asserting it** — because *a debt is not negative money*, both branches need the same representation. `SOLVE THE ACTUAL PROBLEM` `PLAYER GOVERNS` `HONEST DEGRADATION`

## Why

### Zero is what the circuit needs, so there is nothing to choose

[`plans/0033`](../../plans/0033-conserved-money-and-the-treasury.md) task 5 is a tax sweeping Households into the treasury and a transfer paying it back out. **Tax flows in before the transfer pays out**, so the circuit runs on an empty treasury from Tick 0 and never needs an opening stock.

It is better than merely sufficient. [`02 §4.2`](../02-simulation-model.md) specifies the exhaustion case — a Policy paying out of a treasury that runs dry *"pays whom it reaches and reports where it stopped"* — and that branch is **only reachable from a treasury that can be empty**. Opening at zero reaches it on the first sweep instead of after a long run somebody has to construct. Slice 10 task 11's rule applies directly: ***a baseline records what a run did, so a change that narrows what the run reaches is invisible in it by construction.*** An opening stock large enough to be comfortable would have hidden the branch the milestone exists to demonstrate.

So the value is **derived from the mechanism it feeds** and opens no [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) number. That is the second decision running in this milestone whose answer was *there is no number here*, after [`0115`](0115-moneys-unit-is-fixed-by-the-smallest-fraction-the-design-multiplies-by.md).

⚠ **This value sits inside the range of legitimate answers for the quantity it is not, and that is the trap session F named.** A treasury holding zero looks exactly like a founding balance of zero, which [`01 §3`](../01-player-experience.md) calls *"a game that cannot start"* — and ***a placeholder whose value sits inside the range of legitimate answers cannot announce itself***. **What distinguishes them is not the number but the missing consumer**: this milestone has no `Govern`, no construction cost and no player, so there is nothing a founding balance would be funding. The guard is therefore a **sentence in the fixture's own header**, on `congested.toml`'s precedent, and not a distinguished value. ***Where two quantities share a range, only their consumer tells them apart.***

### The founding balance is a ratio, and this milestone holds neither side of it

`01 §3` states both ends and both are relative: *"**It must be enough to get started and not enough to win.** … a balance is too small if the player cannot reach a first housed Household, and too large if a city grows to self-sufficiency without the player having made a single spatial decision."*

Reaching a first housed Household costs money. Growing to self-sufficiency costs money. **Neither price exists**: there is no construction cost, no wage, no price surface and no gate, and [`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md)'s three prices for infrastructure are specified and unbuilt. A figure chosen here would be a numerator with no denominator — `plans/0012` **Cause 5**'s ***a number that is one half of a ratio says which half*** — and it would then be quoted as *the founding balance* by everything downstream.

Under [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) the missing prices are **unbuilt**, so they are evidence of nothing and the question is not *should the balance compensate*. It is: **the number arrives with the prices it is a ratio against.**

`01 §3` already names the ratifier and the two refuting observations, in the document, and no ledger carried it — `plans/0012` **Cause 1** in its one-copy form, *a fact with no second copy at all*. The row is filed in [`plans/0002`](../../plans/0002-open-questions.md) §D2 and owned by the first playable build, not by this milestone.

### Gift or loan is free, and the check is what makes that a decision rather than a hope

`01 §3` leaves the axis open deliberately, as *"the natural axis for a difficulty setting"*. An axis left open is only free if **both branches need the same representation**, and that is checkable:

| Branch | What it needs | Whose milestone |
|---|---|---|
| **Gift** | an opening balance | nobody's — it is a world-creation value |
| **Loan to be serviced** | the same opening balance, plus a principal and a servicing draw | whichever milestone builds **borrowing**, which is not this one |

Borrowing is **already settled and is not a new mechanism**: [`04 §5`](../04-economy-and-goods.md) puts *borrow* among `Govern`'s three financial levers, and [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) as corrected by `adr/0035` makes it *"a player action, not an automatic overdraft."* **A loan founding balance is therefore the game opening with that lever already pulled** — the same machinery, pre-applied — rather than a second kind of money.

The decisive step is arithmetic rather than schedule. **A debt is not negative money.** [`0003`](0003-deterministic-integer-simulation.md) says so in as many words: *"The design genuinely never holds negative money — … the treasury **empties** and its Rules **wait** …, and borrowing is an explicit player action that **adds** money."* A principal is an obligation, not a balance, so it never enters the conservation sum and **task 4's invariant reads identically under either branch**. That is the whole check, and it comes from an ADR written about signedness for entirely unrelated reasons.

***An axis left open is free only when both branches need the same representation, and that is checkable rather than assumed.***

### What settling it found is larger than what it settled

⚠ **`01 §3` quotes half of a sentence whose other half reverses it, and attributes it to the wrong entry.** It writes: *"budget failure is absent, for the reason `CONTEXT.md` → Resource already gives: a deficit becomes a debt burden and never a stop. You can overspend in the first ten minutes and you will feel it; you cannot lose."*

The sentence is under `CONTEXT.md` → **Money**, not → Resource, and it reads in full: *"a deficit becomes a debt burden, never a stop — **but it is a player action, never an automatic overdraft, so the treasury genuinely empties and the Rules that could not draw simply wait.**"* The dropped clause is the one `adr/0035` §3a wrote **specifically to correct that reading**, and `adr/0024` carries the amendment: *"'A deficit becomes a debt burden' **reads as an automatic overdraft**, and an automatic one deletes a decision the player should be making."*

So the conclusion `01 §3` draws is the reading the corpus refused, and the correction **landed in two documents and missed the third** — `plans/0012` **Cause 2**. It is also **Cause 5's reading half on a new object**: the rule is *quote the sentence, never the digits*, and here what was dropped was the second half of the sentence itself. ***A caveat can be a clause of the sentence it qualifies, and a half-quote is the one form of miscitation that reads as a faithful one.***

⚠ **And the damper cannot reach a command.** `adr/0035` §3a specifies the unfunded state completely — *the Rule that could not draw waits and subscribes to the Bin that was short* — and **the first ten minutes are made of commands, not Rules.** `Simulation.ApplyConnect` either throws on a malformed payload or returns silently when nothing changed; there is no refusal path, no return value and nowhere for *you cannot pay for this* to go. A Rule waits; **a command cannot wait, so it must be refused, and a refusal is a stop** — in exactly the region `01 §3` promises there is none.

Under `adr/0070` this is **undesigned** rather than unbuilt: no document states what an unaffordable player action does, and it is in neither of [`06`](../06-roadmap.md)'s two inventories. It is filed to `plans/0002` §C rather than answered here, because the likely shape — the shell asking `Core` whether the action is affordable before it ever reaches the Input Log — is a **sim/render boundary** decision (`05 §2`) and not a treasury one. ***Specifying what happens when a Rule cannot pay is not specifying what happens when the player cannot.***

## Consequences

- **The treasury Bin opens at level zero and nothing authors it.** No `[treasury]` table, no key, no `adr/0052` number. Task 5's circuit fills it and drains it.
- **The fixture Ruleset's header states that its treasury opens empty and why**, because the value is inside the founding balance's legitimate range and only a sentence separates them.
- ⚠ **No document may call this the founding balance**, and this ADR is the reason a later reader will not: they are different quantities with different owners, and the shared value is a coincidence of the milestone's shape.
- **Task 4's conservation constant is the Households' opening money alone.** The treasury contributes nothing to it, which makes the equality tighter rather than looser.
- ⚠ **Task 9 is unblocked and its exact-equality assertion is unaffected.** `plans/0033` recorded decision 3 as blocking task 9; it also blocks **task 4**, which nothing said — the invariant's anchor is a founding-balance question, and it resolves to *zero contribution* rather than to a term.
- **`01 §3` is corrected in place**, restoring the dropped clause and the right `CONTEXT.md` entry, with the superseded wording kept.
- **Two rows are filed rather than answered**: the founding balance in `plans/0002` §D2, owned by the first playable build with `01 §3`'s own ratifier; and the unaffordable command in §C, owned by `04`/`05`.

## What would trigger revisiting

- **A price landing in this milestone after all** — if decision 4 puts Upkeep here with a counterparty, the treasury acquires a consumer and an opening stock becomes arguable. It would still be a demonstration value, not a founding balance.
- **Borrowing being built.** That is when the loan branch acquires a principal and a servicing draw, and when *gift or loan* stops being free. The check in this ADR should be re-run then rather than assumed to have held.
- **Any conserved quantity that a debt *would* enter.** The neutrality argument rests entirely on a principal not being money. A design that let an obligation be traded, defaulted on, or held as an asset by another actor would break it.
- **The first playable build.** It supplies both halves of the ratio, and the deferred row becomes choosable on the day it does.
