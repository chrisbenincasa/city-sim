# Upkeep has three blockers landing at three times, so it has a queue and not a milestone

**Upkeep is struck from milestone 12 and placed nowhere.** It moves to [`06`](../06-roadmap.md)'s
mechanisms-with-no-milestone table with its three surviving blockers enumerated, each naming what would
clear it. ***A mechanism whose blockers land at three different times does not have a milestone; it has a
queue, and pinning it to the earliest one is how it acquires a third move.***

**And one of [`0117`](0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md)'s four
grounds is settled here rather than deferred again: ground 3's *money or Materials* is a false binary.**
[`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md) §3 already answers it — *"the formulation
exists to keep the only authored number a **duration**"* — so the authored figure is the **design life
in Days**, the Materials quantity derives from the Segment, the money derives from the market price, and
Upkeep is a **purchase**.

`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `HONEST DEGRADATION`

## Why

### `adr/0117`'s revisit trigger has already fired, and it fired predictively

Its trigger list names this exact case: *"milestone 12 shipping without `Scope.Pool` resolvable from a
Segment — then the placement is wrong and Upkeep moves again."* Milestone 12's scope is now settled —
[`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md) decisions 1, 2, 4 and 8,
across [`0134`](0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)
and [`0135`](0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md) — and
**none of it attaches a Rule to a Segment**. ***So the placement is known wrong before the milestone
starts, and a trigger that can be evaluated against a scope rather than against a shipped build should
be.*** Waiting for 12 to close would buy nothing but a stale row in the interim.

### Twice placed, twice moved, and both placements were made against one blocker

Upkeep went to **10** against the Household balance sheet and moved. It went to **12** against the
counterparty and is moving. Each placement was correct about the blocker it was reasoning from and silent
about the others. **Pinning it to 21 against the Lane would be the same move a third time** — and
`06`'s milestone 21 is itself marked *"⚠ Position provisional — session G moves it"*, so the pin would
inherit a provisional position. ***A placement made against the blocker its author happened to be holding
is not a schedule, it is a note about that author.***

### The three blockers, and they land at three different times

| Blocker | What clears it | When |
|---|---|---|
| **Construction cost's *quantity*** | `adr/0035` denominates money in **Lane-Tiles**, so the count needs the Lane | **Milestone 21**, whose own position is provisional |
| **Design life** | `adr/0035`: *"not authored by hand… derived from the **share of a mature city's budget** Upkeep should occupy"* | **A mature city.** The treasury shipped at 10, so the *budget* half now exists; the *mature* half does not |
| 🔴 **The actor** | *"Upkeep's subject is a Segment, its payer is the treasury and its counterparty is a market — three different things — and the engine has no Rule whose subject is not its payer"* | **No milestone at all.** Nothing on the roadmap builds a Rule whose subject is not its payer |

⚠ **The third has no home, and that is why this is an unplacing rather than a re-placing.** A mechanism
blocked on something nothing is scheduled to build cannot be given a date by choosing one.

### What today discharged, which is more than the count suggests

- **Ground 1 — the counterparty — is fully discharged.** `Scope.Pool` is a market, and `adr/0135` gives it
  a Provider and a moving price, so *"bought from local Processing it becomes local wages"* has a
  mechanism rather than a promise. ⚠ **Whether *Materials specifically* have a local seller is content in
  12's new Ruleset**, so the discharge is of the mechanism and not yet of the instance.
- **Ground 2's *money* half is discharged.** It read *"no Ruleset key anywhere authors a cost of
  anything"*, and after `adr/0135` none needs to: `adr/0035` makes infrastructure money *"a transfer, not
  a sink"*, so construction money buys Materials **at the market price**. ***The blocker was an authoring
  gap and the answer was a market, not a key.*** What survives is the quantity, above.

### Ground 3 is settled here because it depends on nothing unbuilt

`adr/0117` left it to *"whoever builds Upkeep"* on the ground that *"settling it decides whether the
authored quantity is money or Materials, and that is the mechanism rather than its schedule."* **The
premise is right and the binary is wrong.** `adr/0035` §3: *"The formulation exists to keep the only
authored number a **duration**. A flat §X per Lane-Tile per Day is a magnitude constant, which this
project distrusts; a design life is scale-free and means the same thing in a village and a metropolis."*

So the answer is **neither**: a designer authors a **duration**, the Materials fall out of the Segment,
and the money falls out of the price. That resolves `adr/0117`'s *"specified with a transfer's syntax and
a purchase's semantics"* in favour of **purchase**, which is what `adr/0035`'s title claimed and its
formula obscured. ***A ground that turns out to be answerable from a document already cited is a ground
that was deferred for being adjacent to hard ones***, and this one was carried three ADRs on the strength
of the company it kept.

## Rejected

**Pin it to 21.** The third repetition of the pattern above, against a milestone whose own position is
provisional.

**Ship a reduced Upkeep at 12** — money only, no wear, with an authored §-per-Segment cost. It is the
shape `adr/0035`'s title refuses (*"never by a budget"*), the magnitude constant §3 distrusts by name, and
a hash-bearing number with no ratifier under
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md). ***Three
independent refusals of the same shortcut is the corpus working.***

**Leave the row at 12 and let it lapse when 12 ships.** This is the failure the sitting that produced this
ADR spent its day correcting in three other documents. A row that goes stale on a known date is a row
somebody should move on the day they learn the date.

## Consequences

- **`06`'s Upkeep row moves out of milestone 12** into the mechanisms-with-no-milestone table, carrying
  the three blockers above. **Its named-risk sentence for 12 loses nothing** — 12's risk was always
  `Scope.Pool`, and Upkeep was a passenger.
- **`adr/0117` is superseded in two places and stands in the rest**: its *placed at 12* is superseded by
  this ADR, and its ground 3 is settled rather than open. Its grounds 1 and 2 are updated by the discharge
  above. It gets a banner; it is not rewritten.
- 🔴 **The actor problem is promoted to a blocker in its own right and needs a home.** *A Rule whose
  subject is not its payer* is engine design that no milestone owns, and Upkeep is only its first
  customer — Policy scoped to a Ward will want the same shape. **It should be watched for a second
  claimant**, because a shape wanted by two mechanisms is a shape somebody should design deliberately.
- **No number is chosen here**, so no `plans/0002` §D row is owed. ⚠ **The design life is a duration and
  will owe one on the day it is written**, and `adr/0035` already names its derivation, so the §D entry
  when it comes is a ratifier and not a guess.

## What would trigger revisiting

- **The Lane landing** (milestone 21, or wherever session G moves it), which clears the quantity blocker
  and leaves two.
- **A mature city existing**, which makes design life derivable rather than invented.
- **Anyone building a Rule whose subject is not its payer**, for any reason. That clears the blocker with
  no home, and it is the one most likely to be cleared by a mechanism that is not Upkeep.
- **A second claimant for that Rule shape.** Two customers make it a design task rather than a blocker,
  and it should then get a milestone of its own rather than waiting for whichever mechanism needs it
  first.
