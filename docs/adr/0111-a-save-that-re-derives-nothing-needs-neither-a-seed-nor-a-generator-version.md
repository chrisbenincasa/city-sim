# A save that re-derives nothing needs neither a seed nor a generator version

**The version-1 save header carries eight fields and a generator version is not one of them.** Magic, the
format version, a native-order byte-order sentinel, the **world key**, the Ruleset content hash, and the
**four world-creation constants that live in the binary rather than in a table** — `TICKS_PER_DAY`,
`WHEEL_SIZE`, `CellGrid.WorldCells`, `CellGrid.TilesPerCell`. [`0086`](0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md)'s
**table of three is amended to two numbers and a class**: the generator version and the world seed are
**one requirement rather than two**, and neither has a reader until something regenerates from a seed.
**A placeholder version number is worse than an absent one, because an absent one refuses and a
placeholder agrees.**

`HONEST DEGRADATION` `SOLVE THE ACTUAL PROBLEM`

## Why

### The seed and the generator version are one requirement, and two documents split them

[`0086`](0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md) puts the
generator version in a table of three version numbers. `05 §7`'s deleted header listing put the world
seed in a list of five fields. They were written as separate requirements and they are not: **a seed is
consumed by exactly one thing**, and in this build that thing is `WorldKey.FromSeed`, whose every call
site — six shells, `Replay`, and the tests — derives a key and discards the number. **`World` does not
retain the seed at all.** The only artefact that keeps one is the Input Log, which writes `seed 0x…`
because a replay *re-derives*: it re-runs `SyntheticCity` and `RoadGenerator` from that number every time
it is read.

A save does not. `Rows.Restore` reads saved columns back into their slots and `World.RebuildDerived`
recomputes the derived ones from them; **no generator is called on either path**, and the roads, Lots and
Buildings a generator once laid are in the file as columns like everything else. So the failure
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) names — *seed 42 produces
different terrain and every existing save silently loads the wrong world* — **cannot occur in a version-1
save**, and the number that would announce it has nothing to be compared against.

That is not an argument that it never will. `0021` is explicit that saves become *seed + edits*, with an
untouched Chunk regenerating on load; the day that ships, the save re-derives and needs both. **They
arrive together, because the seed is only ever needed by the thing the generator version versions.**

### A placeholder agrees where an absence refuses, and that inverts the guard

The obvious cheap move is to write `generator_version = 1` now and bump it later. It is not neutral, and
the reason is `0021`'s own repair policy: the generator version has no migration chain and no degradation
function — **it is pinned**, so a mismatch is a refusal rather than a repair.

Follow that through. A build that grows a terrain generator must refuse every save written before it:
those saves describe a city with no landscape, and regenerating one underneath them is precisely the
*city floating over a landscape that moved* the pin exists to prevent. **If the header carries no
generator version, the format version delivers that refusal for free and for the right reason** — the
declaration set moved, the file is version 1, this build reads version 2. **If the header carries
`generator_version = 1`, the terrain build compares 1 against 1, agrees, and loads the save.** The field
added to prevent the failure is the field that permits it.

***A version number for a mechanism that does not exist has no value that could ever differ, so it cannot
refuse; and a reader who sees the field assumes something checked it.*** That is the shape open decision
5 was circling when it said both candidates *read as a guarantee* — the correct third answer is not a
better derivation, it is no field.

### The third number was a class, and four of its members were already asking for this

What `0086` reached for is real, and the generator version is the member of it that does not exist yet.
The class is: **a world-creation value that lives in the binary rather than in a table**. A saved column
carries its own value in the file and cannot disagree with the reader; a `const` is *supplied* by the
reader, so a save written under one and read under another is a body that parses perfectly, column for
column, and means something else.

Four such constants exist today, and each says so in the file that owns it:

| Constant | Its own file says | What a mismatch does |
|---|---|---|
| `Ticks.PerDay` | *"a world-creation constant baked into the save"* | every saved duration means a different length of time |
| `EventWheel.Size` | *"it is baked into the save"* | every armed Rule is in the wrong bucket |
| `CellGrid.WorldCells` | *"baked into the save, and never tuned"*, and `0021`: *"recorded in the save header"* | every Cell index means a different place |
| `CellGrid.TilesPerCell` | *"design constant. Never tuned"* | the same, one level down |

**Nothing had ever baked them, because there was no save.** Three of those four sentences have been in
the tree for the life of the project, describing a file that did not exist, and the fourth is in an ADR.
`plans/0012` **Cause 1** in its cheapest form: a fact stored only as a claim about somebody else's
artefact, where no check can reach it — and it is now `SaveHeaderTests`.

They are written **individually rather than folded into one number**, which is `0086`'s *do not compact*
applied to the header: a fold says *something differs* and four fields say *the Day moved*. The
difference is a reader's afternoon.

### The world key is in the header because the State Hash cannot notice its absence

`World.Key` is not a column and folds nothing. A loader that dropped it would restore every column
correctly, **hash identically at the instant of the load**, and then diverge on the next Tick, because
every draw takes the key as its first coordinate — and `RebuildDerived`, which *"takes no arguments and
must not start taking them"*, cannot reproduce the commute roster without it.

That is worth stating as a property of the test rather than of the header: **the round-trip form of the
Factorio test cannot catch a missing key, and only the run-N-more form can.** A field whose omission is
invisible to the hash is exactly the kind the header exists for.

### Five fields on the `Simulation` dissolve, and three had already answered

`plans/0002` carried `_phase` and `_inForce` as *"§7's unargued format half"*. Walking the rest of that
class settles all five, and the notable part is where the answers were:

- **`_phase`** — [`0087`](0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md)
  takes the copy at the end of phase 7, so it is `Commit` at every save. A value with one possible value
  is not state. ***The cadence decision answered the format question in another ADR and nothing
  recomputed it.***
- **`_inForce`** — it *is* the Ruleset content hash, which is a header field. Saving it as world state
  would be a second copy of a header entry.
- **`_opened`** — `true` in every save by the same argument as `_phase`: a world that has never stepped
  has never reached phase 7, so a save of an unopened world does not exist. ⚠ **It must still be
  *supplied* at load** — a fresh `Simulation` has it `false`, so the first step after a load would adopt
  whatever Ruleset it was handed as an *opening* rather than as a *transition*, writing no provenance
  trail entry and reporting no degradation. **Dissolving out of the save is not dissolving out of the
  loader.**
- **`_reloads`** and **`_degradation`** — both are already documented as per-run rather than per-world,
  in their own remarks: *"since this Simulation started"*, and *"the trail is world state because `05 §7`
  puts it in the save; this is a Simulation's, because a warning is about the run."*

`adr/0093`'s reading half, paying: three of the five were settled where the fields are, and the question
had been open in `plans/0002` for two milestones.

### The header parses on any host and the body does not, so the header says so

`plans/0012` item 5 found that the State Hash is not byte-order stable — `Column.FoldBytes` fixes the
order at the point of *combination* and the bytes it combines come from `MemoryMarshal.AsBytes`, so a
multi-byte field inside a row sits in the machine's order. Task 2's `WriteBytes` is the same call, so
**a save is not portable across byte orders either**, and this ADR cannot fix that.

It can stop it being discovered a hundred megabytes in. The header's own fields are written explicitly
little-endian so they always parse, and **one sentinel is written in native order** so a reader that gets
it back reversed refuses immediately and says why. ***A guard that cannot fix a defect can still refuse to
proceed into it.***

## Consequences

- **`0086`'s three-number table is amended in place**, and `05 §7`'s copy of it with it. What replaces
  the third row is the class and its four current members, with the generator version recorded as its
  unbuilt one.
- **`WorldKey` grows a second door.** `internal static WorldKey Restore(ulong)` reopens exactly the
  guarantee that type's private constructor exists to give — *a key can be obtained from a seed and no
  other way* — for one caller. Its own remark's *save the seed, not the key* is annotated rather than
  struck, because it is **true of the Input Log**, which is the artefact it was written for and the one
  that honours it today.
- **`World` still cannot produce a seed, and that is now a written precondition rather than an
  oversight.** The slice that makes a save re-derive must give `World` the seed and derive the key from
  it — a four-call-site change, and a hash move, because `default(WorldKey)` and `FromSeed(0)` are
  different worlds. **It is not deferred for the hash's sake** ([`0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
  forbids that): it is deferred because the value has no reader.
- **The Tick is not in the header**, though `05 §7`'s deleted listing had it. `0058` moved the Tick into
  `ClockTable`, so a header copy would be a second copy of a saved column. A save browser wanting to show
  *Day 412* without reading 131 MiB is the reason to reinstate it, and it would be an explicitly derived
  echo rather than an authority.
- **A refusal names its cause.** Five distinct refusals, each with a message that sends a reader
  somewhere: not a save, wrong format version, wrong byte order, and one per constant. `LEGIBLE CAUSE`
  applies to a loader as much as to a city.

## What would trigger revisiting

- **The first content re-derived from a seed at load time**, which `0021` makes terrain. That is the day
  the seed and the generator version both arrive, the format version goes to 2, and every version-1 save
  is refused — correctly, because it describes a city with no landscape.
- **A second `World` per process, or a world whose key changes.** The argument that the key is a header
  field and not a column assumes exactly one per world. Nothing threatens that today; the `WorldKey`
  parameter still threaded through nine mutators is the shape that would, and it is filed in
  `plans/0012`.
- **A world-creation constant becoming Ruleset data.** `Ticks.PerDay` is the candidate — `adr/0015` says
  it should be, and its own remark agrees. It would move from this header into the Ruleset content hash's
  coverage, which is a strictly better home, and this table loses a row.
- **A big-endian host mattering.** The sentinel refuses; it does not repair. If one ever has to read
  these files, the repair is at the point of *storage* and is `plans/0012` item 5's, not this ADR's.
