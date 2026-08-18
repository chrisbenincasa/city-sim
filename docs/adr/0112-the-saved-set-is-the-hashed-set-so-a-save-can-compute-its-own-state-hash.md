# The saved set is the hashed set, so a save can compute its own State Hash

**A version-1 save carries the State Hash of the world it holds, and that number is folded from the
copy rather than from the world — so it costs the simulation thread nothing.** The saved set and the
hashed set are the same columns in the same order, because
[`0086`](0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md) made the
declaration the format and the declaration was already the hash. `SaveHash.Of(world, body)` reproduces
`World.HashState()` exactly, reading the world for its **schema** and never for a value; a load then
restores the columns, runs `RebuildDerived`, recomputes, and refuses a mismatch. **`05 §4`'s invariant 6
becomes a property of every load** rather than of the seven cases in the test suite.

`LEGIBLE CAUSE` `HONEST DEGRADATION`

## Why

### The claim this overturns is one this project made about itself, four days ago

Milestone 8 task 6 recorded that a save could not carry a verified hash, and
[`0087`](0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md) was
amended in place to say so. The reasoning was exact and the conclusion was too wide:

> *"`HandleColumn.Fold` folds the **target row's monotonic id**, which lives in another table and is not
> a function of the handle's bytes, so folding the copy produces a number that is not the State Hash. **A
> hash that folds a value the bytes do not contain cannot be computed from the bytes.**"*

Every clause of that is true. What does not follow is *therefore not from the copy*, and the reason is
one line of `Rows`:

```csharp
_id         = Saved<ulong>("id", Touch.Wake);        // Rows.cs:72
_generation = Saved<uint>("generation", Touch.Wake); // Rows.cs:73
```

The id a handle resolves to is **saved state**. It has to be — a load that did not restore it would hand
every reallocated slot a new identity and break every handle in the world. So the value is in the file.
It is in a *different table's block* of the same file, which is the whole of the difficulty and none of
the impossibility.

***A value absent from a column's own bytes can still be present in the copy.*** The sentence that was
written is about **a column**; the conclusion drawn from it was about **a file**. The test that pinned
it, `A_fold_over_the_bytes_is_not_the_state_hash`, is still true and still in the suite — it folds the
buffer flat, byte after byte, which is a thing nobody would ever want. It never tested the claim it was
read as supporting.

This is [`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) on its
own author's turf and with a new surface: **a test name is a description of the build**, it says which
symbol to read and never what is in it, and a negative test is the most quotable kind because it reads
as a closed door. The four sightings that ADR lists are an ADR summary, a plan's recommendation and two
doc-comments. This is the fifth, and the first where the description is a *test*.

### The two walks were the same walk all along, and nobody had put them side by side

```
World.HashState (World.cs:644)          SaveFile.WriteBody
  seed                                    —
  per table, in World.Tables order:       per table, in World.Tables order:
    slotCount, liveCount,                   slotCount, liveCount,
    freeHead, nextId                        freeHead, nextId
    each SavedColumn, [0, slotCount)        each SavedColumn, [0, slotCount)
```

**The save file is the hash's input, written down.** That is not a coincidence to be preserved by
vigilance; it is `0086` working — *a save has no schema of its own and the field declaration is the
format* — arriving at a consequence `0086` did not draw. Once the declaration is both the format and the
hash, the file and the fold cannot disagree about *what* is folded, only about *how*.

And the how differs in exactly one place: `HandleColumn`. Every other column's `Fold` is
`FoldBytes(storage)` over the same span `StorageBytes` hands the writer. One indirection, in one type.

### So the fold takes its bytes, and there is one of it

`Column.Fold` now takes a `ReadOnlySpan<byte>` and a `TargetIds`:

```csharp
internal abstract void Fold(ref ulong hash, ReadOnlySpan<byte> storage, in TargetIds targets);
```

The live path passes the column's own storage and `TargetIds.Live(targetTable)`. The save path passes a
slice of the copy and `TargetIds.Saved(generations, ids, slotCount)`, located in the same buffer. Both
call the same method. `Rows.FoldScalars` is shared the same way.

**Writing a second fold beside the first was the obvious shape and is the one to refuse.** Two
implementations of one rule that must agree for ever is [`plans/0012`](../../plans/0012-corpus-audit.md)
*Cause 1* built on purpose, and this corpus's whole experience is that the second copy drifts and the
drift is invisible until something reads both. One implementation against two sources costs a refactor
of `Column` and `Rows` and cannot drift at all. ***The abstraction to reach for is the one that makes
the duplicate impossible, not the one that makes it convenient.***

`SaveHashTests.A_fold_over_the_copy_is_the_state_hash` holds the seam honest at 0, 1, 64, 256 and 1,024
Ticks, and a second test asserts the number *moves* — without which the first passes for a fold that
returns a constant.

### A schema read is not a state read, and that is what makes it free

`SaveHash.Of` takes a `World`. It reads the table order, each table's saved column list, each column's
width, and which table a handle column points at. **All of it is fixed at `Rows.Seal` and never moves
again.** It touches no array a phase could be writing.

That is the property `0087` needs and the one its own sentence was reaching for. The hash is on the
movable side of the seam, with the serialise and the write:

```
simulation thread   copy the saved columns        ~10 ms at 1M, once per autosave
a thread's to take  fold the copy                 ~32 ms, blocking nothing
a thread's to take  write the header and body     seconds, blocking nothing
```

**So `0087`'s clause is honoured rather than overturned**, which is the outcome that was not available
when milestone 8 shipped. The alternative on the table was to fold the *live* world on the simulation
thread and amend that clause on the strength of its own arithmetic — 32.47 ms once per in-world Day is
0.03% of a Tick budget amortised, and the clause is a per-Tick argument (`0037`'s) applied to a
per-autosave event, so the amendment would have been sound. It is not needed. **It would also have put
a ~42 ms hitch on the simulation thread at 1M — the exact size `0087`'s own revisit trigger names as
visible**, reached by adding a feature rather than by growing the city, which is a bad way to spend a
trigger. ***An argument that a cost is affordable is not a reason to pay it when it can be moved.***

### What the hash buys, stated as the thing it catches

A load reads the columns, rebuilds every derived structure, recomputes, and compares. What that catches
is the case nothing else can: **a file whose allocator state is internally consistent and whose contents
are wrong.** `Rows.Restore`'s consistency walk checks generations, the free list and the id counter — so
a flipped byte in `id` or `free_next` is refused already, and a flipped byte in a Household's money is
not. The file is the right length, every table restores, the world loads, and it is a different city.

`05 §7`'s *hash-broken* mark now has something to compare against, and a save that reloads into a
different world says so at the load rather than at whatever downstream divergence eventually surfaces.

## Consequences

- **The version-1 header is nine fields and 60 bytes**, amending
  [`0111`](0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md) on the
  count and on nothing else. The format is unreleased and nobody is carrying a save
  ([`0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)), so a ninth
  field cost one edit; it is the last moment that was true.
- **Zero is not a sentinel and there is no unverified save.** A world can genuinely hash to zero, so a
  reader cannot read the field as *absent*. Every version-1 save carries one — which is only available
  because the field went in before release rather than being retrofitted as optional.
- **The copy is part of the format rather than an optimisation.** Before this, a save could have been
  streamed out of the live world and the copy existed to keep the write off the simulation thread. The
  hash is folded from the copy, so there is no longer a way to write a version-1 save without taking
  one. `0087`'s mechanism is now load-bearing twice.
- **The header is no longer written into the copy.** It carries a number that is a function of the body,
  so it cannot precede it. `SaveFile.WriteBody` is the copy and the body; `SaveFile.Write` puts a header
  in front. ***A header is a statement about the build and a copy is a statement about the world***, and
  the two were only ever adjacent.
- **A save is two hand-overs to its destination rather than one** — the header, then the body. Task 5's
  streaming property is untouched: no single hand-over is proportional to the file except the drain,
  which was already one call by design.
- **`Rows` gains `IsValidSlot`, `TryIdAt`, `IdColumn` and `GenerationColumn`**, all internal.
  `Rows<T>.IsValid` now delegates to `IsValidSlot`, so handle validity is stated once rather than twice.
  The two columns are found **by identity** and never by declaration position, so declaring a column
  above them cannot silently point the fold at the wrong bytes.
- **`World.HashSeed` is internal rather than private.** It is the hash's first mix and the save folder
  needs it.
- **This does not make a load cheap.** The load side recomputes from the *live* world it has just built,
  at 32.47 ms at 1M — which is free, because a load is not inside a Tick loop. Only the save side had a
  budget to protect.
- **Nothing threads yet.** Milestone 8 D4 keeps the write synchronous; what this decides is which side
  of the seam the hash sits on, not when the seam is taken.

## What would trigger revisiting

- **A column whose file bytes stop being its storage bytes** — compression, a variable-length encoding,
  or a per-column transform. `SaveHash` folds slices of the body on the strength of
  `StorageBytes(slotCount)` being what the writer hands over. A format that transforms on the way out
  would have to fold before transforming, which is still off the simulation thread and is a different
  arrangement of the same parts.
- **A save that compacts rows.** `0086` permits it — *a round trip must preserve the hash and need not
  preserve the bytes* — and `HandleColumn` folding the id rather than the slot is what makes it
  possible. A compacting writer would move slot indices, so `TargetIds.Saved` would resolve against the
  *compacted* generation and id arrays, which is correct by construction. Worth re-deriving rather than
  assuming, because it is the one case where the two sources describe different layouts of one world.
- **A handle column pointing at a table outside `World.Tables`.** `SaveHash` throws on it today. It
  would mean a table that is hashed and not saved, or saved and not hashed, which `0086` forbids in
  both directions — so the throw is the right answer until something argues otherwise.
- **The hash becoming per-Tick rather than per-save.** Everything here is affordable because it happens
  once per autosave. A design that hashed every Tick — for a networked lockstep check, say — puts the
  cost back on the simulation thread and none of this arithmetic survives.
