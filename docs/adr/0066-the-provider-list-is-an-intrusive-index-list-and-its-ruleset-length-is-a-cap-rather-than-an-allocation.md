# The Provider List is an intrusive index list, and its Ruleset length is a cap rather than an allocation

**A Household's Provider List is an **intrusive index list** — a head on the Household, a `next` on the
entry, both in flat arrays, with entries drawn from one shared pool sized to what the city's Households
actually know. [`adr/0017`](0017-agents-satisfice-they-never-optimise.md)'s Ruleset length is a **cap
enforced at insert**, never an allocation reserved per Household. `S0a`'s 104-byte inline row model is
superseded, and every figure derived from it — including *the Provider List is ~21% of the entire
world* — is an **upper bound measured on a structure this project's own rule forbids**, not a
measurement of the design.**

Settled by session **N**, [`plans/0018`](../../plans/0018-session-n-the-bin-the-pool-and-the-economy.md)
task 5, which was arguing `04 §6` and found this underneath it. **Two claims, typed apart under
[`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md), and the
separation is the point**: *the Provider List is a variable-length collection and therefore an index
list* is **arguable** and is settled here; *an index list is smaller than the inline array* is
**measurable**, depends on steady-state occupancy against the cap, and is **routed rather than
asserted**.

`BOUNDED KNOWLEDGE` `SOLVE THE ACTUAL PROBLEM`

---

## Why

### The rule already existed, and this is the only collection that ever escaped it

`CLAUDE.md` and `05 §4` state it without qualification:

> Every variable-length collection in `Borough.Core` is an **intrusive index list** — a head index on
> the owner, a `next` index on the element, both in flat arrays. Never a per-entity collection object.

Every collection in the code obeys it: `BuildingBins`, `Occupants`, the two Bin wait lists, the
Unplaced Pool, and — on the Household table itself — `MemberHead`/`MemberTail`. **The Provider List is
the only per-entity collection in the design that has ever been modelled as an inline fixed array**,
and it was modelled that way in a **spike's footprint estimate**, not decided in a document. This is
therefore closer to an excavation than to a decision, which is the shape
[`adr/0051`](0051-industrial-pollution-is-a-stock-the-environment-absorbs.md) turned out to have: the
rule had been written down all along and one structure was built past it.

**Nothing in code has to change, because the Provider List has never been built.** That is the whole
reason to settle it now rather than when it is load-bearing.

### The figure that escaped with it is one of the corpus's headline numbers

S0a's footprint model reports the Provider List at **104 bytes, 47% of the Household row and roughly
21% of the entire world**, and concludes that *the design's memory is dominated by what Households
know*. The give-away that it is pricing declared capacity rather than knowledge is in the same
paragraph: *"every entry is ~4.5 MiB at 1M"*. **A column is a flat array over the table's whole slot
count — declaring a field is what allocates it — so an unset entry costs exactly what a set one does.**
Eight entries are reserved for a Household that knows one shop.

So the number is real, and it is a measurement of **the inline representation**, not of the design.
Under an index list the same knowledge costs a head per Household plus one pooled entry per provider
actually known, and the *"21% of the world"* conclusion may not survive at all — which matters, because
that figure is quoted as evidence about the shape of the design, and it has already propagated into a
code comment on `Entities.cs`.

### The trade-off is locality against footprint, and it does not obviously go one way

**An inline array is contiguous.** A short walk over eight adjacent entries is one or two cache lines;
an index list chases a `next` through a shared pool and scatters. This project has measured that
penalty three times and it is not small — **×1.49** in the Rule engine's attribution, **1.56×** on the
Zone Rule tripwire, and a third sighting in `0011` — so *scatter ≈ 1.5* is a known constant here rather
than a worry.

Three things put the decision on the index list's side anyway.

**The walk is not hot.** A Provider List is consulted on a *shopping occasion*, not on a Tick — task 5
settles one Trip per occasion — so it is nearer the cold path than the Rule engine's inner loop. S5
sharpened this the same day: scatter cost **1.00×** on an arithmetic-bound kernel, so *scatter ≈ 1.5*
is a property of memory-bound walks and not a tax on everything.

**A cap is not an allocation**, which is [`adr/0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s
shape exactly: density caps what may be *built*, it does not reserve it. `adr/0017`'s length is the
same kind of quantity — the most a Household may know — and reserving it for every Household turns a
behavioural bound into a memory bill.

**And the inline model makes the cap doubly load-bearing for no reason.** Under it, raising the cap
from 8 to 12 costs ~18 MiB at 1M whether or not a single Household learns a twelfth shop. Under an
index list the cap costs nothing until knowledge is acquired, which is the honest coupling: *memory
follows what the city knows*.

### What is measurable here, and is therefore not settled here

**The index list is smaller only if Households know fewer providers than the cap.** Inline costs
`N × entry`; a list costs `head + actual × (entry + next)`. At `04 §6`'s own *"the two or three places
it knows"* against a cap of 8 the list wins comfortably; at **full occupancy it loses**, because it pays
a `next` per entry and a head per owner that the inline array does not.

So *the representation is cheaper* is a claim about **steady-state average occupancy against the cap**,
the refuting number exists, and the machine is an `S0a`-class footprint capture once Households acquire
providers — `06` milestone 5b or 9. **It is routed to `0002` §B and is not asserted here.** The
structural argument stands without it: an inline array is the wrong *kind* of thing for a
variable-length collection in this codebase regardless of which is smaller this year, because the rule
exists so that footprint follows content rather than declaration.

---

## Consequences

**A correction is owed to `docs/spike-results.md`**, and it is a correction rather than a retraction:
the 104 bytes, the 47%, the 21% and *"a tuning knob controls a fifth of the world's footprint"* are all
true **of the inline model**, and the model is superseded. The same figure sits in a comment on
`Entities.cs` and travels with it. Filed to [`0012`](../../plans/0012-corpus-audit.md).

**S0a's largest structural conclusion is in question.** *Households are the largest table in the world,
and it is not close* — 75.2 MiB against Citizens' 53.4 MiB — rests on a 219-byte Household row of which
104 bytes is this list. Remove the reservation and the ordering may reverse. **The conclusion is not
retracted here**, because that too is the measurable claim above; it is flagged so that nobody quotes
the ranking as settled.

**The cap gains a place to live and loses its second job.** `adr/0017`'s length stays a Ruleset
constant, enforced at insert, and it stops being a memory parameter — so a designer tuning how much a
Household may know is no longer also tuning a fifth of the world's footprint. It remains **unset**, and
it is a `0002` §D2 row that has never existed.

**A tail is needed as well as a head, or insertion order is wrong.** Appending is what `IndexList`
already does for the Bin wait lists, and a Provider List is ordered by acquisition rather than by rank —
`adr/0017` forbids ranking — so the same shape applies.

**Eviction becomes explicit rather than implicit.** Under the inline array, learning a ninth provider
overwrote a slot and the policy was invisible; under a cap enforced at insert, the Household must
*decide what to forget*, and that decision is now a named hole rather than an accident of layout. It is
not settled here and belongs with whatever specifies Provider List maintenance — which the corpus has
never written.

**What this does not decide:** the cap's value, the entry's contents, how a provider is learned, how one
is forgotten, and whether an entry carries per-provider state such as a last-failed timestamp. That last
one is task 5's live question and this decision changes its cost — under a list, per-provider state is
paid per **entry that exists** rather than per reserved slot on every Household.

---

## What would trigger revisiting

- **A measurement showing steady-state occupancy near the cap.** Then the list pays a `next` and a head
  for nothing, and the inline array is both smaller and faster. This is the named measurable above, and
  it reverses the decision on a number, which is the correct way to reverse it.
- **A Provider List walk turning out to be hot.** If shopping occasions are frequent enough that the
  walk lands in the per-Tick set, *scatter ≈ 1.5* applies to it and locality may outweigh footprint.
  The instrument is the same `S0a`-class capture plus the Tick budget.
- **A requirement to scan the list in a sorted order.** `adr/0017` forbids ranking today; if that is
  ever relaxed, a contiguous array is the structure a sort wants and this should be re-derived rather
  than amended.
- **The entry growing large.** The crossover between the two representations moves with the entry size;
  a fat entry favours the list further, a 4-byte entry favours the array.
