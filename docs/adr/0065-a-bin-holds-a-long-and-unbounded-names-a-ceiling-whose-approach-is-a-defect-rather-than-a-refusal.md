# A Bin holds a `long`, and *unbounded* names a ceiling whose approach is a defect rather than a refusal

**A Bin's `level` and `capacity` are `long`, uniformly, for all three Resource families — and so is
every quantity on the write path that reaches them, or the narrowing merely moves from the column to
the argument. *Unbounded* is not a state: it is `long.MaxValue`, and it means a ceiling far enough away
that **approaching it is a defect rather than a refusal**. There is no special case — `Check` runs the
same headroom test on a money Bin as on any other — and a level climbing toward its ceiling is
[`adr/0006`](0006-no-collection-grows-with-elapsed-time.md)'s **magnitude** clause, caught by the long-run
acceptance test, never by a headroom subscription and never by a panic. If 2⁶³ is ever approached the
answer is **denomination**, not width.**

Settled by session **N**, [`plans/0018`](../../plans/0018-session-n-the-bin-the-pool-and-the-economy.md)
task 4, on the *comparison* half [`adr/0031`](0031-one-resource-abstraction-and-depth-not-count.md) left
open. Typed under [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
with a distinction worth stating: **`int` is *refuted*, by arithmetic already available, so no
measurement is being pre-empted**; whether 2⁶³ suffices is measurable and is deliberately given **no
§B row**, for the reason in *Consequences*.

`HONEST DEGRADATION` `LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM`

---

## Why

### The corpus had already chosen the width, in one place, and the Bin never got it

`Money` is a `long`:

```csharp
public readonly record struct Money(long Raw) : IComparable<Money>
```

`HouseholdTable` holds `Column<Money>` for both money on hand and savings. `BinTable` holds
`Column<int>` for `_level` and `Capacity` — and `adr/0031`'s central claim is that money is a Resource
**held in a Bin**. So one quantity has two representations inside one core, and a payment from a
Household into a Bin — which [`adr/0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)
makes of every cross-boundary trade — **narrows 64 bits to 32**.

**This is the corpus's recurring shape rather than a new discovery**: the right answer written down in
one place, the wrong one in another, and nobody deciding between them. `adr/0062` found a wrong unit in
three places at once; `BOR0207` is a lint for *a ratio pre-scaled by a large constant and divided in 32
bits*; `02 §4.3`'s worked example destroyed money for six slices. **The decision here is therefore not
*should the Bin widen* but *the Bin must agree with a type the project already chose*.**

### `int` is refuted by arithmetic, not by preference

The synthetic city runs three Households per Building and 120 Buildings per 1,000 Citizens — **360,000
Households at 1M**. A Bin aggregating the city's money overflows a signed 32-bit level at

> 2,147,483,647 ÷ 360,000 ≈ **5,965 units per Household**

Six thousand units each. Not a fortune, not a late-game figure, and *before*
[`adr/0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md)'s balance of payments
lets imported money accumulate. Spread across District Pools rather than one city Bin it softens to
roughly 119,000 per Household at twenty Districts — still a bound a real economy reaches, and the exact
figure waits on `pool`, which does not exist. **The direction does not depend on which**, which is what
makes this refutation rather than estimation.

**Unsigned was never the lever.** It buys one bit, and negative values must stay *representable* so a
debit can be refused rather than wrapped — which is what `Money.TryDebit` and `Money.IsNegative` are
for. A doubling against a shortfall of orders of magnitude is not a fix.

Under `long` the same arithmetic gives ~2.5×10¹³ units per Household before trouble, which is where the
question stops being interesting.

### Truly unbounded is architecturally unavailable, and the word was never true

Arbitrary precision means `BigInteger`, which is managed and allocates, and **lint 7** with
[`adr/0036`](0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) requires every struct in `Borough.Core` to be
`unmanaged`. There is no arbitrary-precision quantity in this design and there will not be while that
invariant stands.

So the honest framing is the one the sitting arrived at: **there is no unbounded — only a ceiling far
enough away that reaching it is a defect rather than a design limit.** That is a better sentence than
the word it replaces, because *unbounded* was never true: the row has always held `int.MaxValue`, and
`adr/0031` asked for an explicit unbounded precisely so *"a very large number pretending to be a bound"*
would not escape into things that divide by it. **It escaped anyway** — `BinTable.Create` writes
`capacity.Units` and drops `IsUnbounded`, so every consumer downstream sees only the sentinel. Naming
the ceiling honestly is what stops the next consumer being fooled by it.

### *Full* stays uniform, and the special case was a workaround for the width

The sitting first proposed that *full* is not a state an unbounded Bin can be in — `Check` skipping the
headroom branch entirely, an unbounded Bin's headroom wait list provably empty. **That position is
withdrawn, and the reason it was ever attractive is the reason to reject it now**: it existed to keep a
too-small sentinel out of the arithmetic. At `long.MaxValue` the ceiling sits some 10⁸× beyond any
plausible city total, so the branch never fires; skipping it buys nothing and adds a second code path
through the engine's hottest loop.

It also **removes its own objection**. Skipping the check moved the failure from *a rich seller sleeps*
to *a rich seller crashes*, because nothing then stood between an accumulating money Bin and `Deposit`'s
assertion. Keeping the test uniform leaves the guard in place as a backstop and lets the magnitude
clause be what notices first.

**And the absurdity the loader refuses by name stops being reachable.** `RulesetLoader` refuses an
authored money ceiling with the words *"a finite one would mean an actor too full of money to be paid —
a sale failing on headroom because the seller is rich."* Under `int` that state was representable
through the sentinel: `FloorDiv(int.MaxValue − level, delta) < floor` returns `Stopped(…, Headroom)`
and puts a Rule to sleep on a money Bin's headroom list. Under `long` it remains representable in
principle — which is why the ceiling is *named* rather than denied — and is unreachable by any economy
the game can produce.

### Three products, and only one of them was in danger

The widening was checked against the engine's arithmetic rather than assumed to be sufficient.

1. **`RuleEngine.Requirement`'s `floor × |net|` was genuinely unbounded.** Nothing constrains it — not
   the Bin, not the level, not the apply band. On overflow the requirement goes **negative**, so
   `requirement > remaining` is false, the drain wakes a waiter it cannot satisfy, and then *increases*
   its own budget by subtracting a negative. It is unreachable today only because the single declared
   Readout is `occupancy`. **This is a live latent defect that the widening closes**, and it is
   `adr/0063`'s own new arithmetic, four hours old.
2. **`Check`'s `delta × applications` is safe by construction, and looks like the dangerous one.**
   `applications` is the minimum over every touched Bin's `affordable`, and each `affordable` is
   `level / |delta|` or `headroom / delta`, so each product is bounded by that Bin's own level or
   headroom. Recorded here so that nobody widens it as a precaution and nobody narrows it as a saving.
3. **`Band`'s `readout × Percent` is the door money walks through, and `Readouts.Read` returns `int`.**
   `02 §4.1`'s own spelling is *"one unit of money applied income × 15 / 100 times"* — the design
   intends a **money** readout. Widening the Bin without widening the Readout would leave the identical
   32-bit contradiction one level up, which is how this class of defect propagates.

---

## Consequences

**The whole write path widens, not only the storage.** `BinTable.Move`, `World.Deposit`,
`World.Withdraw`, `LevelAt`, `HeadroomAt` and `Readouts.Read` all take or return `long`. Widening the
column alone would move the narrowing from the column to the argument, which is the same defect wearing
a signature.

**It moves the hash**, so it joins [`0003`](../../plans/0003-build-plan.md)'s hash-moving queue behind
[`adr/0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md).
The two touch the same two columns and should be one commit, which is the opposite of the queue's usual
rule — and it holds here for the reason the rule exists: `0064` changes a column's *declaration* and
this one changes its *width*, so a hash that moved for both is attributable to neither if they ship
apart and the trace is wrong.

**Memory is noise.** +8 bytes per Bin, roughly +8 MB at 1M Bins against S0a's 86 MiB of tables.

**Uniform across families, because the alternative re-creates what `adr/0031` removed.** A per-family
width needs a discriminated column or a second table, and `adr/0031`'s whole argument is that Good,
Utility and Money are three families of **one** mechanism — its four escapees each had bespoke
machinery and each was excepted on the same axis.

**A Bin's level stays a raw `long` rather than becoming `Money`.** A Bin holds any Resource, and the
level counts units whose meaning belongs to the Resource; `Money` remains the Household-side wrapper
where the non-negative-stock invariant lives. Stated so nobody unifies them later and gives a Timber
Bin a `TryDebit`.

**No §B row for *is 2⁶³ enough*, deliberately.** The claim is measurable — an economy run at 1M would
produce the number, `06` milestone 9's work — and it is given no ledger row because **a row nobody will
ever check is noise**, and `0002` §D's triage already found five entries that were not numbers at all.
The standing check instead is the one that already exists: the long-run acceptance test's *no magnitude
trending upward at steady state*, which is exactly what an approach to any ceiling looks like.

**And no *nearly full* threshold is invented.** A number like *90% of the ceiling* would be a chosen,
hash-adjacent constant with no ratifier available, which [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
forbids. The magnitude clause needs no threshold because it watches a **trend**, not a level.

**`adr/0031`'s comparison half is discharged, and its determinism half was never in danger.** The named
hazard was `level + delta > capacity`, which overflows against a sentinel and silently inverts. The code
has always written `capacity − level` through `HeadroomAt` and divided with `IntegerMath.FloorDiv`, so
nothing overflowed and nothing inverted. That half needed recording, not deciding.

**What this does not settle:** whether money belongs in a Bin at all, which is `adr/0031`'s claim and
not reopened here; and what a *fullness gauge* shows for a money Bin, which is a presentation question
with no presentation layer to hold it — though the derived unbounded flag `adr/0064` makes free is what
lets such a gauge refuse to answer rather than divide by the ceiling.

### Rejected: an unsigned quantity

One bit, and it costs the representability of a negative that `Money.TryDebit` relies on to refuse a
debit rather than wrap it.

### Rejected: skipping the headroom test for unbounded Bins

The sitting's own first position, withdrawn above: a second code path through the hottest loop, buying
a branch that never fires, and converting a remote subscription into a remote crash.

### Rejected: a 128-bit quantity, now or later

**Denomination is always available and is cheaper.** Money's unit is a Ruleset choice, so if 2⁶³ were
ever approached the answer is fewer units per dollar. Written down here so that no future argument
reaches for a wider integer, which would be a new arithmetic substrate rather than a tuning change.

---

## What would trigger revisiting

- **A Readout that returns a money magnitude against a large `Percent`.** The band product becomes
  `long × int` and is safe for any plausible holding, but it is the one product with no structural
  bound. If a Readout is ever declared whose magnitude is a stock rather than a count, re-derive it.
- **A real economy run whose money magnitude lands within a factor of ~10⁶ of the ceiling.** That is
  the denomination trigger, and the response is a Ruleset change rather than a code change.
- **Money leaving the Bin abstraction.** If `adr/0031`'s *one Resource abstraction* is ever reopened,
  the uniform width loses its justification and the Bin could narrow again.
- **A Resource family whose natural quantity is fractional.** Everything here assumes whole units; a
  fractional Resource would need Q16.16, whose effective range in 64 bits is a different calculation and
  would want this ADR re-derived rather than amended.
