# A save has no schema of its own and the field declaration is the format

**Nothing authors a save layout.** The file is the per-field declaration read out in the order the State
Hash already folds it: for each table in `World._tables` declaration order, the allocator's four scalars,
then every column whose disposition is `Saved`, over the full slot range. `05 §7`'s hand-written `header
/ global / chunks / tables` listing is **deleted** rather than corrected — it is a second copy of a fact
the declaration owns, and it had already drifted. The header carries **three** version numbers, the save
is **slot-exact**, and a migration is a pure function over **saved columns only**.

`FAST ITERATION` `SOLVE THE ACTUAL PROBLEM`

## Why

### The format was decided by slice 4 and nobody moved the document

`05 §7` was written before typed tables existed and describes a file somebody would sit down and lay out.
Since [`0003`](0003-deterministic-integer-simulation.md)'s per-field declaration shipped, that act is not available:
declaring a field through `Rows.Saved`, `Rows.Derived` or `Rows.SavedHandle` is what **allocates** it, so
there is no way to have a field that the declaration does not classify, and `BOR0901` is a build error on
storage in a `[Table]` type that is not a declared `Column`.

`World.HashState` is eleven lines and it is the whole specification:

```
hash = HashSeed
for each table in _tables, in declaration order:
    fold _slotCount, _liveCount, _freeHead, _nextId
    for each column with Disposition.Saved, in declaration order:
        fold slots [0, _slotCount)
```

**The save's content set is exactly the hash's coverage set** — same tables, same columns, same slot
range — because both are the same question asked once. `05 §4` already states the composition order
(*tables in declaration order, arrays in index order*) and a test already holds it. Writing a second
ordering for the file would be writing a second answer to a settled question, and the two would drift the
way the layout listing did.

### The listing had drifted, which is the evidence rather than the complaint

The section says the file is `global` (*"the Event Wheel, Road Graph + Epoch, District definitions, the
travel-time matrix (or a rebuild flag)"*), then `chunks` (*"per-Chunk: Tiles, Lots, Buildings, Map Layer
cells, Lanes"*), then `tables`. Against the code:

- **Lots and Buildings are flat tables**, not per-Chunk blocks, and have been since slice 4.
- **The Road Graph is three typed tables** with declared columns (5a), so it is not a `global` blob.
- **The travel-time matrix is derived**, so *"or a rebuild flag"* is not a choice the file gets to make —
  a derived column is rebuilt, always, and the flag is the disposition.
- **Trips, Legs and Travellers** are named in `tables` and are not in `World._tables` at all yet.

Not one of those is a bug. Each is the declaration being right and the prose being a stale transcription
of it, which is [`plans/0012`](../../plans/0012-corpus-audit.md) *Cause 1* on a document that had no
reason to hold the copy in the first place. ***A layout listing beside a field declaration is a
transcription with no reader and a guaranteed decay rate.***

### The save is slot-exact, and this is the part that would have been got wrong

`Rows.Fold` folds `_freeHead` and `_nextId`, and `Column.Fold` folds `[0, _slotCount)` — every slot,
including recycled ones holding a dead row's residue. So:

- **The free list and the id counter are saved state**, not bookkeeping to rebuild. A loader that
  recomputes the free list by scanning for dead rows produces a different `_freeHead` and a different
  hash.
- **Compaction on save is forbidden.** Dropping dead slots and renumbering is the obvious size win and it
  changes `_slotCount`, every live row's slot, and the residue in between. Under `05 §4` that is a
  **design change**, not an optimisation, however it was motivated.

Neither follows from *"a save is array dumps"*, and both follow from the hash. This is the declaration
paying for itself a second time: the first payment was that the hash cannot have a coverage hole, and the
second is that the file cannot either.

The one place the two diverge is a **handle** column, where the hash folds the target row's monotonic
never-reused id and the file must store the handle, because a load has to restore the same slots. That is
not an inconsistency — it is the hash being deliberately blind to slot recycling, which is the property
that lets two identical cities agree. **A save round-trip must preserve the hash and need not preserve
the bytes**, and that difference is exactly what the reload test measures.

### Three version numbers, and the format one versions the declaration set

[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) requires three, each for a
different reason; `05 §7`'s header lists *"magic, format version, Ruleset version, world seed, Tick"*,
which is two; and `06-roadmap.md` has carried *"milestone 8 owes the three version numbers where it
currently implies one"* as an unpaid item. Settled here:

| Number | Versions | Mismatch means |
|---|---|---|
| **Format version** | the **declaration set** — which tables exist, which columns, which disposition | run the migration chain forward from the recorded version |
| **Ruleset content hash** | the content the Rules are made of | `05 §7`'s two policies, already closed: lenient in play, refused on an unaccounted mismatch in replay |
| **Generator version** | the procedural generator's output for a given seed | the terrain moved under the city. `0021` calls this the same class of failure as `System.Random` changing under `0003` |

They are three because they fail in three different ways and are repaired in three different ways. The
format version is the only one with a migration chain; the Ruleset hash has a degradation function and a
provenance trail; the generator version has **neither**, which is why `0021` pins it rather than
migrating it.

### A migration is a function over saved columns, and most schema changes need none

Because a derived column is rebuilt on load rather than read from the file, the migration surface is the
**saved** set only. Three cases fall out and only one of them costs anything:

| Change to the declaration | Migration needed |
|---|---|
| Add, remove or alter a **derived** column | **None.** It was never in the file and is rebuilt from what is |
| Add a **saved** column | A default-filling step, which is the trivial migration |
| Remove or reinterpret a **saved** column | A real migration, and the only kind that can lose information |

Moving a field from saved to derived is the middle column read the other way and is the shape the corpus
has taken twice already — [`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)'s
Bin capacity and [`0078`](0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md)'s
frontage. Both moved a number off the row and onto the Ruleset in force, and neither would need a
migration written for it: the column stops being read and the rebuild supplies it. `05 §7`'s rule that
migrations are *never rewritten to skip steps and never deleted* stands unchanged and now applies to a
much smaller set of things.

### The reload test still belongs in CI and it no longer finds what the section says it finds

`05 §7` says the Factorio test *"finds **unsaved state** — a cached value, a dirty flag, an accumulator, a
lazily-built index that was never written to the file and never restored."* Every item on that list is
now either a declared `Saved` column, in which case it is in the file by construction, or a declared
`Derived` one, in which case it was never meant to be. **The bug class the section names has been made
unrepresentable** by `BOR0901` and the declaration, and the test is not thereby pointless — what it
catches has changed:

> **A derived column that does not rebuild to the value it had.**

That is a live class with a sighting already. 5a-bis found that *a derived structure caching a Ruleset
value reads as **absent** rather than as **stale** before its first rebuild*, and absent is the state
every guard is written against. A reload lands a world in precisely that pre-rebuild state, so the test
is now the cheapest instrument for the one failure mode the declaration cannot rule out. It should be
described as measuring the **rebuild**, not the write.

## Consequences

- **`05 §7`'s file listing is deleted and replaced by the rule**, with the hash's fold as the statement
  of it. The section keeps its migration-chain paragraph, its whole Ruleset-versioning subsection, the
  provenance trail and the hash-broken mark, none of which this touches.
- **`06-roadmap.md`'s owed item is paid.** Milestone 8 carries three version numbers and the table above
  says what each does on a mismatch.
- **The save has no size decision in it, and its size is already measured.** S0a reports **85.98 MiB** of
  tables at 1M, which is the saved-and-derived total; the file is smaller by the derived columns. Nothing
  here may be argued against a guessed size — that is `0043`, and the number exists.
- **A test is owed that the file's column set equals the hash's.** The rule above is only worth having if
  something enforces it; the obvious form asserts that a save's declared content matches
  `Rows`'s `Saved` set table by table, which is cheap and structural. It belongs with milestone 8 and
  is named here so it is not discovered later as a gap.
- **`adr/0037`'s consequence for *when* a save is taken is not settled here.** `05 §7`'s async-save
  paragraph rests on a structure that ADR deleted, and it is
  [`0087`](0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md)'s.

## What would trigger revisiting

- **A save the declaration cannot describe.** The whole argument is that every piece of world state is a
  declared column. If something arrives that must persist and cannot be a column — an interned string
  table for names is the candidate, since `Core` returns ids and never strings — then that thing needs an
  authored layout and this ADR governs only the rest of the file.
- **The file needing to be readable without the binary that wrote it.** Everything above ties the format
  to a build's declaration set. A tool that inspects saves independently would need a written schema,
  which is a real cost and is the strongest argument against this decision; it is accepted because the
  crash artifact and the Input Log, not the save, are what `05 §8` gives an external reader.
- **Compaction becoming necessary rather than tempting.** If dead-slot residue ever dominates the file,
  the repair is a hash-bearing change to slot recycling itself, argued as a design change under `05 §4`
  — not a quiet compaction step in the writer.
