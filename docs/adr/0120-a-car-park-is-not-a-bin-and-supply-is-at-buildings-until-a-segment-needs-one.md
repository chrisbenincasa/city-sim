# A Car Park is not a Bin, and supply is at Buildings until a Segment needs one

**A `Car Park` is its own type and its own table: a Building's parking provision, located by an
`Address`, with a capacity derived from the Ruleset in force and a saved occupancy.** It is **not** a
`BinTable` row, and `CONTEXT.md` gains the term rather than `Bin` being stretched to cover it. **In this
milestone the only Car Parks are held by Buildings**; Road Segments hold none, and the structure
forecloses none, because a Car Park is located by an Address and an Address is already
`(Segment, offset, side)`.

Guiding concepts: `LEGIBLE CAUSE`, `SOLVE THE ACTUAL PROBLEM`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
The type question is a vocabulary question and the scope question is a question about what a milestone
costs; neither names a number.

## Why

### Four structural mismatches against one shared word

[`0009`](0009-parking-is-modelled-supply-never-search.md) says *"parking is a Bin"*, and lists among its
reasons *"it reuses machinery that already exists"*. The machinery does not fit, in four independent
places:

| | A `BinTable` Bin | Parking needs |
|---|---|---|
| **Location** | `Handle<Building>` only (`BinTable.cs:59`) | An **Address** — the value every *where is it* query in this project takes |
| **What it holds** | A `ResourceId` from the Ruleset's `[[resource]]` list | Vehicles, which are not a Good and are on no `[[resource]]` list |
| **Waiting** | Two wait lists, Supply and Space | **Nothing** — `0009`'s own superseding note turns on *"nothing about parking ever waits"* |
| **The type's scope** | `CONTEXT.md` reserves it for **Goods and Money** by name | A third thing |

**The fourth is the one that decides it**, because it is already written down and was written for this
exact reason: `CONTEXT.md` → Supply and Space says *"the bound vocabulary is general even though the
`Bin` type is reserved for Goods and Money ([`0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md):
a Bin has a consumer and occupancy has none)"* — and in the sentence before it uses a full Parking Shed
as its worked example of something with a **ceiling** that is **not** a Bin. ***The corpus had already
put parking outside the `Bin` type while calling it a parking Bin*** — the distinction existed and the
word did not.

**And `0068`'s test settles it in one line.** A Bin has a **consumer**; a parked car has a **holder**,
like a job and unlike a Good. That is the same test that made `[[building]] jobs` a dismissal rather
than a drain, and it puts parking on the jobs side of it.

### The one sentence pulling the other way, and why it loses

`CONTEXT.md` → Address ends *"a Building's Access Point is a Building's Address, a Leg runs from one
Address to another, and **a parking Bin will have one**"* — future tense, and the only sentence in the
corpus reading as though the two are one type.

It loses on its own tense. **It is a promise about a thing that did not exist, made by an entry about
`Address`**, and what it is actually asserting is that *the located parking thing takes an Address* —
which this decision keeps in full. What it is not asserting is that the located parking thing is a
`BinTable` row; that reading is available only because there was no other word. **The sentence is
repaired rather than deleted**, and it says `Car Park` now.

### The name was chosen against two collisions and one register

*Parking space* is refused: **`Space` is a bound** in this project — `capacity − level`, the thing that
refuses a Rule that wants to put something somewhere — and a term whose head word already means *the
room left in it* would be quoted meaning the other thing within a milestone. That is `CONTEXT.md`'s own
***name a bound, never a level*** arriving from the other side.

*Parking lot* is refused: **`Lot` is a unit of land** that holds exactly one Building, and a Car Park
sits **on** one.

*Parking bay* was considered and refused on scale: in ordinary British usage a bay holds one car, and
this row holds a capacity, so the entry would have had to override its own word in its first line.

**Car Park** collides with nothing in `CONTEXT.md`, is British, scales from a driveway to a
multi-storey without straining, and names the object `0009`'s player-tool table already describes
placing — *"a Building that is mostly a large parking Bin"*.

### Segments are omitted, not foreclosed, and the omission is filed rather than absorbed

`0009` says parking is *"a Bin held by Buildings **and by Road Segments**"*, so street parking is half
the supply model as designed. It is not built here, and the reason is cost rather than doubt: it needs
**content and a second balance pass** — residential provision against kerb capacity, which `0009` says
in its own words *"must be balanced separately"* — inside a milestone that already owes a shed radius
and a per-kind capacity, both hash-bearing and both unratified.

**Nothing structural is spent by waiting.** A Car Park is located by an Address, an Address is
`(Segment, offset, side)`, and a Segment-held Car Park therefore needs **no new column and no new
mechanism** — only rows, and a number. The one thing that would have foreclosed it is locating a Car
Park by `Handle<Building>`, which is exactly what the `BinTable` shape would have done. ***The decision
that keeps the type separate is the same decision that keeps the Segment case open.***

⚠ **The player tool stays refused and is a different object.** *Allow or ban street parking per Road
Segment side* is a **seventh verb** against a list
[`0091`](0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) fixed at six
with the user in the room. Street parking's **capacity** is authorable with no verb at all, so the two
halves are separable and only the verb is refused.

⚠ **The omission is filed to `06` on the day rather than recorded here**, because this milestone is
about to make *parking exists* true and **a half-built supply model reads as complete from outside** —
`plans/0000`'s *a partially-shipped milestone reports as shipped*, which cost this project the District
Pool for forty inventory rows.

## Consequences

- **`CONTEXT.md` gains a `Car Park` entry** and the `Parking Shed` entry becomes *the set of **Car
  Parks** within acceptable walking distance*. The `Address` entry's future-tense clause is repaired.
  **`Bin` is unchanged**, which is the point: one meaning per term, and the reservation it already
  states now has a named thing sitting outside it.
- **Every field's disposition follows from a rule already written**, and none of them is a new choice:
  capacity is `(derived AND rebuilt)` from the Ruleset in force (`0068`, `0064`), occupancy is
  `(saved AND hashed)`, and ~~the Address is derived from what owns the row. **Whether the Address is a
  stored derived column or resolved through the owner at shed-rebuild time is task 1's question**, not
  this decision's — it is a caching choice with no observable behaviour on either side.~~ **the Address
  is three `(saved AND hashed)` columns on `LegTable.From`'s pattern** — a `Severable` handle to the
  Segment, an offset and a side.

  > ⚠ **CORRECTED 2026-08-18 by task 1, hours after this ADR was written, and the correction is about
  > this ADR's own reasoning rather than about the columns.** *A caching choice with no observable
  > behaviour on either side* was wrong twice. **`Address.cs` forbids one of the options outright**: an
  > Address may not be a single stored column, because a saved slot index folds *the entire demolition
  > history of the city* into the State Hash and two runs building the same city would disagree — so a
  > table persisting a place declares a handle plus plain columns and assembles the struct at the read
  > boundary. **And the disposition is what decides this ADR's own foreclosure claim.** A Building-held
  > Car Park's Address is *derivable from its owner*; a **Segment-held one's is primary** — it is where
  > the player put it — and a column is declared once, so a **derived** Address would have forced the
  > Segment case to bring a second column and *"needs no new column"* would have been false. Saved keeps
  > it true.
  >
  > ***A question deferred as an implementation detail was load-bearing for the paragraph that deferred
  > it***, and the tell was available: this ADR argues that locating a Car Park by `Handle<Building>` is
  > the mismatch that would have foreclosed Segments, and then offered *resolve through the owner* as a
  > free alternative — **which is that same location, one indirection later**. `plans/0012` **Cause 2**,
  > inside a single document, between two sections written in one sitting.
  >
  > **What it costs is a staleness the design already models**: a bulldozed Street severs the handle and
  > the Address reads `Address.None`, which is
  > [`0079`](0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)'s
  > existing shape rather than a new failure mode — *a hole the Trip model reports*.
- ⚠ **The over-capacity rule is *dismissal*, and it is stated here rather than inherited.** A Ruleset
  reload lowering `[[building]] parking` leaves Car Parks holding more than the new ceiling. `0068`
  **evicts** and [`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)
  **drains**, and they differ on whether the quantity has a consumer — so parking takes `jobs`'
  **dismissal**: the overflow holders' columns are cleared and the occupancy decremented **in the same
  act**, or the conservation sum breaks on a reload. **A Citizen holding nothing releases nothing**, which
  is already a legal state — it is what a walker and a first-ever driver are in.
- **No wait list, no `on_fail` chain, no subscription, and no fifth Trip Fate.** A Car Park that is full
  is not a Bin that is full: nothing sleeps on it, because the shed widens instead. This is what makes
  the type separation cheap rather than duplicative — the Bin's two most expensive features are the two
  parking has no use for.
- **`[parking]` and `[[building]] parking` are the Ruleset surface**, and `RulesetLoader`'s section
  refusal (`RulesetLoader.cs:383-389`) stops being right about parking. Task 2's.

## What would trigger revisiting

- **A second thing needing an Address-located ceiling** — a loading kerb for freight, a taxi rank, a
  charging point. If a third such type appears, the right move is a shared *located capacity* type and
  `Car Park` becomes a kind of it, rather than a fourth table with the same four columns.
- **Street parking arriving with a shape this decision did not anticipate.** The claim is that a
  Segment-held Car Park needs no new column. If it turns out to need one — a side, a length, a
  per-Segment ban bit — then the foreclosure argument above was wrong and the omission cost something
  after all, which is the reading that would reopen decision 4 rather than merely schedule it.
- **`Bin` acquiring an Address for its own reasons.** If Goods delivery ever needs a Bin located on the
  network rather than at a Building, three of the four mismatches survive and one falls; the decision
  should be re-read then rather than assumed to still hold on four legs.
