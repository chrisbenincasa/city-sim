# 0007 — Slice 4: typed tables and the field declaration

> Slice 4 of [`0003-build-plan.md`](0003-build-plan.md). Roadmap **milestone 2**. Governed by
> [`adr/0004`](../docs/adr/0004-typed-tables-over-ecs.md),
> [`adr/0037`](../docs/adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md),
> [`adr/0003`](../docs/adr/0003-deterministic-integer-simulation.md),
> [`05 §3`](../docs/05-technical-architecture.md).

**Every entity is a row in a hand-written struct-of-arrays table, addressed by a typed generational
handle.** There is no ECS: no component registry, no archetypes, no query planner, no scheduler.
This slice builds the table layer, the handle discipline, the intrusive-list pattern, and — the part
that is easy to skip and expensive to add — **the per-field declaration from which both the save
serialiser and the State Hash are generated**.

**Risk retired.** Three. That an ECS or a naïve object graph gets baked in, which `adr/0004` rejected
explicitly because the population is homogeneous and ECS earns its complexity through heterogeneous
composition. That a full-world double buffer gets baked in and quietly cancels the Event Wheel, which
is ledger #29 and which `adr/0037` closed on arithmetic — a copy doing 150× more work than the Tick
itself. And the subtlest: **that the State Hash has a coverage hole**. *A field that is saved but not
hashed is invisible to every tool in the project* — runs diverge on it, hashes agree, replay reports
success, and the save/reload test passes because the field *is* saved. The oracle certifies a
divergence it cannot see.

**This is why the slice comes before the Tick.** The hash is generated from the field declaration,
so the hash is a property of the table layer. Building milestone 1 first would mean building the hash
twice.

---

## Gate

**Cleared.** `adr/0004` was grilled session eight and its layout claim survived untouched; `adr/0037`
replaced the buffering strategy that had ridden into it; `05 §3` is worked.

**One crack is open and this slice must answer it for Phase 1** — ledger **#29b**, below.

## Prerequisites

Slices 2 and 3. Typed quantities are the row fields; rule 7's `unmanaged` analyser is what keeps the
rows honest from the first one.

---

## Tasks

### 1. The table primitive — small, and not a framework

`adr/0004`'s claim is that each table is **hand-written**. What is shared is a handful of helpers,
not a base class: a column array, a generation array, a free list, a count. Eight to fifteen entity
types, all known at compile time, is few enough that hand-writing each is cheaper than the
abstraction that would generalise them — and the abstraction is how an ECS gets in through the back
door.

- Structure-of-arrays: each field its own contiguous array, so a hot loop touching three fields
  touches three streams and not a stride. The named hot loops are Lane queues, Event Wheel buckets,
  layer diffusion and choice scoring.
- `Span<T>` stays useful over a column, which is most of the reason for the layout.
- Iteration in index order, always.

### 2. Typed generational handles

`Handle<T>` as `{ index: u32, generation: u32 }`. The index addresses the row; the generation is
bumped when a row is freed, so **a stale handle is detectably stale** rather than silently pointing
at whatever moved in.

- **Typed as well as generational.** `Handle<Citizen>` cannot index the Building table — the same
  move as slice 2's typed quantities, applied to identities. `adr/0004` made it for identities
  first; `adr/0003` moved it to quantities later.
- A resolve that validates the generation, and a debug path that throws rather than returning a
  neighbour's row.

### 3. The sort-key prohibition, and the monotonic id

**A handle index must never be used as a sort key in simulation logic.** This is a determinism rule
and not a style note: indices are recycled by the free list, so ordering by one means **an unrelated
demolition on the far side of the city can silently change who wins a contested draw downtown**.

- Where a stable per-entity key is genuinely needed, it is a **monotonic never-reused id carried as
  its own field** — a separate column, not the index.
- `02 §8` rule 5 settles the prompting case: Phase 3 intent ordering is a counter-based random
  shuffle, `hash(world_seed, tick, "settle_order", entity_id)`, reshuffled every Tick. Entity id was
  wrong twice over — *biased*, because the same Building wins every contested draw for the life of
  the city and no player can see why, and *not stable*, for the recycling reason above.
- A random tiebreak is also the more honest explanation: *two bakeries reached for the same six flour
  and one got it* is complete; *it has a lower table index* explains nothing.

### 4. The hot/cold split

Fields touched every Tick in one table; fields touched only on transactions or inspection in another,
keyed by the same handle. Household economics — income, expenses, savings, purchases made and missed
— goes entirely cold.

Use S4 task 2's recomputed schema rather than `05 §3`'s 40-byte Citizen figure, which is admitted
stale: session five added a schooling accumulator, experience and car ownership and none is reflected
in it.

### 5. **The per-field declaration** — the load-bearing task

> Every field in a table is declared once as either `(saved AND hashed)` or `(derived AND rebuilt)`,
> and the save serialiser and the State Hash are generated from that one declaration.

This is the one deliberate step onto the framework slope in the whole design, and its bounds are
stated: **a per-field flag, not a framework** — no reflection, no codegen required, no component-like
API.

Why it is structural rather than a test: a test for hash coverage has the same blind spot as the
thing it tests. With the single declaration, **save/reload equivalence transitively proves hash
completeness** — a field that is saved is hashed by construction, so the save/reload test cannot pass
while the hash is incomplete.

Composition order falls out free and must be written down as the rule it is: **tables in declaration
order, arrays in index order**, folded through the same `mix`.

### 6. The State Hash

Fold **values, never identity**, through slice 2's `mix`. A handle's index is identity; the thing it
points at is value.

- `HashState()` returns a `u64`.
- Tests that earn their keep: changing a hashed field moves the hash; changing a derived field does
  **not**; adding a field to a table without declaring it **fails to build** rather than silently
  falling out of the hash.

### 7. Intrusive index lists

**Every variable-length collection in `Borough.Core` is an intrusive index list** — a head index on
the owner, a `next` index on the element, both in flat arrays. Never a per-entity collection object.

It allocates nothing, traces nothing, gives `adr/0033`'s round-robin drain its deterministic order
for free, and survives a port unchanged. This slice builds the pattern and its tests; the three
consumers — wait lists, Parking Sheds, Wheel buckets — arrive in later slices and must not invent
their own.

### 8. `ResourceMap`

A **sorted array with binary lookup**, never a hash map. At the nine Resources of `adr/0031` this is
cache behaviour rather than an algorithmic choice, which S4's K4 measures directly. Deterministically
ordered, which is the actual requirement.

The `Bin` struct `{ resource, amount, capacity }` and its **wait list** belong to slice 7 — `02 §3.2`
omits the wait list that `02 §4.1` requires, and `§3` is the least-maintained section in that
document. Build the map here; build the Bin where its semantics are settled.

### 9. The buffering declaration

Per `adr/0037`: **a table is double-buffered if and only if a parallel phase both reads and writes
it.** Lane dynamics and Map Layer cells — roughly 2 MB — against everything else at 80–150 MB.

In Phase 1 nothing is parallel, so every table is single. Declare the property anyway, per table, so
that the rule is stated where a future table is added rather than inferred later. Map Layer cells
arrive in slice 6 and are the first `double`.

Record with it the reason it is safe: **the Past is not a second copy.** It is *the state as of the
start of this Tick*, and Phase 2 observes it because nothing has written yet. **Phase 2's
read-only-ness is therefore load-bearing** — a future decision to parallelise Decide must not also
make it mutating, or every entity table silently reclassifies.

### 10. The first tables

Thin. Citizen, Household, Building, Lot — enough to hash something and to prove create, free and
reuse. The schema fills in as milestones land; a wide table now is a wide table to migrate later.

### 11. Answer ledger #29b for Phase 1

`adr/0004` claims *tables partition naturally along Chunks*. True for **static** entities — Buildings,
Lots, Lanes — and **unargued for mobile ones**. Grouping Households or Citizens by home Chunk means a
Household moving house **relocates its row**, and a relocated row is worse than a stale one: the
generation counter detects use-after-free but **cannot detect *this handle now points at someone
else's valid row***.

The working answer for Phase 1, to be recorded rather than assumed: **rows never move, and the Chunk
partition is a separate index.** It costs an indirection on spatial queries and nothing on the hot
path, and it keeps handles meaning one thing. Note that `05 §5` role 3 leans on the other answer and
that S0 will measure this whether it means to or not.

---

## Acceptance

- `dotnet test` green; `dotnet build src/Borough.Headless` builds with no Godot.
- Create, free and reuse a row; the stale handle is **detected**, not silently resolved.
- A test that adds an undeclared field and **fails to build**.
- Hash moves on a hashed-field change; hash does not move on a derived-field change.
- Every row type satisfies `unmanaged`, by slice 3's analyser rather than by inspection.
- Table operations allocate nothing on the hot path, shown by a BenchmarkDotNet memory diagnoser.
- **Something to look at:** a headless dump of table row counts and the world's State Hash. It is a
  number and a list, and it is the first time the project has one.

## Decisions owed by this slice — settled

- **Ledger #29b's Phase 1 answer** — rows never move, and the Chunk partition is a separate index.
  Recorded in [`0002`](0002-open-questions.md), revisit trigger S0's measurement. The code enforces
  the half that is enforceable: `Rows` has no compaction, no relocation, and `SlotCount` is
  documented as *not* the live count precisely so that nobody adds one to close the gap.
- **The row schema is in code** rather than in a table in a document, which is where it stops going
  stale. The Citizen is **13 B per-Tick, 42 B wake, 7 B cold — 62 B total**, printed by the headless
  report at whatever population it is run at.
- **Chunk = Cell** stands as the provisional Phase 1 value and is untouched by this slice, because
  ledger #29b's answer removed the only reason a table would have had to know about it. There is no
  Chunk in `Borough.Core` yet, which is the cheapest possible form of *provisional*.

## What it produced

**Twelve files in `Borough.Core`, one analyser, seven test files, and a report.** `dotnet test` is
green at 241 tests; `dotnet run --project src/Borough.Headless` prints table counts, the footprint
split three ways, and a State Hash.

**The declaration mechanism is `Declare`-allocates plus one analyser, and the first half does most of
the work.** `Rows.Saved`, `Rows.Derived` and `Rows.SavedHandle` *are* what allocate a column, so a
column that was never declared has no storage and cannot exist. That closes adr/0003's field rule
from the inside, with no reflection, no codegen and no component API — the bound `adr/0004` set on
this being one deliberate step onto the slope. `BOR0901` closes the one remaining route, a bare array
written beside the columns, and it is an error rather than a warning for the same reason every other
diagnostic here is.

**A handle column folds the target's monotonic id, not the slot it addresses.** *Fold values, never
identity* is only a slogan until a handle column has to be hashed, and folding the raw
`{index, generation}` would have been cheaper and would have been correct for both uses the hash has
today — in replay and in save/reload the allocation history is identical, so indices agree. It was
rejected because it buys that correctness from a coincidence rather than from the definition. The
cost is one random access per handle per hash and a target table named in the declaration; what it
buys is a hash that a compacting save could not move by compacting.

**The generation array, the free list and the monotonic id go through the same declaration as
everything else.** They are the allocator's own state and they decide which slot the next entity
lands in, so a divergence in them is a divergence in the city — and the hash should report it on the
Tick it happens rather than ten thousand Ticks later when it finally reaches a Citizen. The
consequence is stated where it will matter: the save is a verbatim array dump, so slot layout is part
of the state, and a save that ever *compacts* moves the hash by doing so.

**A test caught the bug that would have made the whole generation scheme a no-op.** `FreeSlot` zeroed
every column and *then* bumped the generation — but the generation is a column, so the bump was
applied to zero and produced 1, which is this design's encoding for *live*. Every freed row would
have read as occupied and every stale handle to it would have resolved. It is exactly the defect the
generation counter exists to prevent, introduced by the mechanism meant to implement it, and it was
invisible in every test except the one that asserted a freed slot is not live.

**Intrusive lists encode "empty" as zero rather than as −1, and it is load-bearing.** Slots are
zero-filled on growth and zeroed again on free, so a `-1` terminator would read a freshly allocated
owner's head as *slot 0* — a new Building silently owning the first Household in the city. The stored
form is the slot index plus one.

**A finding that belongs to slice 7 was made here: a list whose order carries meaning cannot be
`Derived`.** A derived list must be reproducible by a rebuild, and a rebuild has only index order to
work from — so `Append` (arrival order) and a rebuild disagree the moment the free list recycles a
slot, and nothing reports it because derived fields are outside the hash by declaration. Hence
`InsertOrdered` for the derived occupant and member lists, and hence the corollary written down at
its declaration site: **a Bin's wait list is drained in arrival order, arrival order is not
recoverable from anything else, so the wait list is `Saved`.**

**Two deviations from this plan, both deliberate.** The allocation check is an xUnit test over
`GC.GetAllocatedBytesForCurrentThread` rather than a BenchmarkDotNet memory diagnoser: the diagnoser
*reports* allocation where the test *fails* on it, and the property will be regressed by a stray
closure in a year rather than today. And `Handle<T>` has no `None` property — the unset handle is
spelled `default`, which is the point being made rather than a concession to `CA1000`.

## What this slice deliberately does not do

No `step()`, no phases, no Tick counter — slice 5. No save serialiser: the **declaration** the
serialiser is generated from is the expensive part and it lands here; the serialiser lands at
milestone 8, when the tables have settled and a migration chain has something to be a chain of. No
Bins, no Rules, no Event Wheel.
