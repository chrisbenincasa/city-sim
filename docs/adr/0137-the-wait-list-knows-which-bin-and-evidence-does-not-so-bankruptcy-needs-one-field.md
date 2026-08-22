# The wait list knows which Bin and Evidence does not, so bankruptcy needs one field

**[`0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)'s
claim that bankruptcy and starvation are distinguishable *"rather than needing a mechanism"* is true of
the wait list and false of the build.** `RuleInstanceTable.WaitingOn` records the Bin that stopped a
Rule; `RuleEvidence` does not carry it, and `Blocking` is only `Nothing / Supply / Space` — so a
Business short of flour and a Business short of money surface **identically**, as `Blocked = Supply`.

**Two things ship with the purchase at 12.** `RuleEvidence` gains the blocking Bin, and **the purchase's
money check must produce a verdict naming the money Bin** rather than failing the draw without
subscribing to anything. The shell then classifies by the Resource's **family**, so `Core` keeps
returning ids and never strings.

`LEGIBLE CAUSE` `HONEST DEGRADATION` `SOLVE THE ACTUAL PROBLEM`

## Why

### The claim was about the build and nobody had read the build

`adr/0050`: *"the distinction falls out of the wait list rather than needing a mechanism"* — a Pool Bin
short is input starvation, a money Bin short is bankruptcy, two Bins and two blame targets.
[`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md) flagged it correctly as
*"a claim about the build [that] should be read off `Evidence` before being believed"*, and named it **the
one most likely to be assumed**. Read:

| Where | What it holds |
|---|---|
| `RuleInstanceTable` | `Building`, `Rule`, `NextTick`, **`WaitingOn`**, `Blocked`, `Reported`, `StarvedSince`, `QueueNext`, `RuleNext` |
| `RuleEngine.Stop` | writes it — `_world.Subscribe(instance, bin, blocking)` |
| **`RuleEvidence`** | `RuleId`, `LastRan`, `Succeeded`, **`Blocking Blocked`**, `ConditionId Reported`, `StarvedSince`, `Rate`, `MissedFirings` — **no Bin, no `ResourceId`** |
| `Blocking` | `Nothing / Supply / Space`, and both cases are **`Supply`** |

***The wait list is not a reader.*** `Evidence` is — `02 §9`'s whole point, and the only pure per-entity
read the shell may enumerate. The information exists in the world and stops one layer short of anybody
who could use it.

⚠ **This is milestone 11 task 8's defect, one subsystem over**: *"`PlacementCounter.Departed` reached no
instrument — **a flow that reaches no instrument is a flow nobody can read**"*, found because the
milestone's Definition of done is *there is something to look at*. **Two instances make it a shape rather
than an incident**: a column is written, a consumer is assumed, and nothing connects them, because
*writing* the column feels like finishing the work.

### The second half is not a field, and it is the one that could be missed

Under `adr/0050` a purchase **has no money term at all** — *"there is no Ruleset syntax for the payment"*,
the price is emergent and the counterparty is implied by the scope. The wait list keys on a Bin named by
a **term**. So a Pool draw that fails for want of money has, by default, **no term and therefore no Bin to
subscribe to**, and the cheapest implementation returns *insufficient funds* and subscribes to nothing.

***A distinction Evidence cannot recover is one no Evidence change fixes.*** So the requirement is on the
purchase itself: **synthesise the buyer's money check, and fail it as a verdict naming the money Bin**,
exactly as a term-named shortfall does. That costs nothing extra — 12 is implementing the purchase
regardless — but it has to be written down, because nothing about the money leg is authored and so
nothing about it is prompted.

### Why the classification belongs in the shell

A Bin's Resource has a **family**, and `monetised.toml` is the first file to declare `family = "money"`.
So *money Bin* is a Ruleset fact, not an engine fact, and resolving it is the shell's job under
`CLAUDE.md`'s rule that **`Core` returns ids and numbers, never human-readable strings**. `RuleEvidence`
carrying the Bin — or its `ResourceId` — is sufficient and is the whole change.

## Rejected

**A third `Blocking` value, `Funds`.** Tempting and wrong. `Blocking` distinguishes wait lists **by what
wakes them**, in its own words: *"Woken by a withdrawal, and never by a deposit, **which is why the two
lists cannot be merged**."* A money shortfall is woken by a **deposit**, so it *is* `Supply`. A third
value would be a **domain classification wearing a wake rule's clothes**, and it would put a question
about Resources into an enum about scheduling.

**Store a `WasMoney` flag on the Rule Instance.** Redundant, and against
[`0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)'s
direction: a shortfall is **derived from the Bin when the drain asks** rather than stored. `WaitingOn`
already determines the answer; a second column would be a cached derivation that can disagree with its
source.

**Author two `on_fail` conditions and read `Reported`.** It works, and it makes the distinction **authored
content**: every Ruleset would have to remember to, and `diagnosed.toml` is the only shipped file
authoring an `on_fail` chain at all. `adr/0050` wants it structural, and ***a distinction that depends on
an author remembering is a distinction most Rulesets will not have.***

## Consequences

- **`RuleEvidence` gains one field**, on the **cold path** — `Evidence` runs on a click, not in `step()`,
  so `adr/0036`'s `unmanaged` rule is not in play and the record struct may grow freely.
- **`plans/0037` decision 9 is settled and its work lands with the purchase**, not as a separate task.
  ⚠ **It is still the cheapest decision in that milestone** — the correction does not make it expensive,
  it makes it *exist*.
- **`adr/0050` gets a banner.** Its claim is **corrected rather than reversed**: the distinction does fall
  out of two Bins, and it needed a mechanism after all — the mechanism being *telling somebody*.
- **Filed to [`plans/0012`](../../plans/0012-corpus-audit.md)**: an ADR stated as settled a property the
  build does not have, and no mechanical check could reach it, because the corpus's checks are all
  document-to-document.
- ⚠ **A possible check, named and not built.** `DerivedRebuildAuditTests` exists because a column can be
  declared and never rebuilt. **This is its sibling: a column can be written and never read by any
  `Evidence` surface.** Two sightings now. A test that walks the tables and names columns no `Evidence`
  reader touches would have caught both — ***but a column nobody reads yet is not a defect, so such a
  check needs a way to say "deliberately", and that is the hard half.***

## What would trigger revisiting

- **A third instance of write-without-reader.** Two is a shape; three means the check above is owed
  rather than merely available.
- **The shell wanting more than the Bin.** If classifying by family turns out to need the *amount* short,
  `adr/0063` refuses to store it and the shell must derive it from the Bin — and if that proves
  impossible from `Evidence` alone, that ADR's *derived rather than stored* meets its first real test.
- **A money Bin being short for a reason that is not bankruptcy.** The mapping *money Bin short →
  bankruptcy* is an interpretation, not an identity; a Rule that fails on the treasury's Bin is the city
  being broke, which is a different sentence about a different actor.
