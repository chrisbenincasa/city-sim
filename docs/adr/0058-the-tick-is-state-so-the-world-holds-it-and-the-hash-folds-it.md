# The Tick is state, so the World holds it and the hash folds it

**The current Tick is a saved, hashed column on a one-row table in the `World`, not a private field on
the `Simulation`. A Simulation is the loop; the Tick is state. Nothing takes a Tick as a parameter in
order to find out what time it is — the invariant tiers read it from the world they are checking, and
the State Hash folds it like any other saved column.** `LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM`

## Why

### Three mechanisms paid for the old arrangement before this was written

`Simulation._tick` was a `private ulong`, so the Tick was reachable only by being handed round.

- **Slice 8's `World.Adopt`** had to take the Tick **as a parameter** to compute an arming stagger, for
  no reason other than that the world it was migrating could not say what time it was.
- **Slice 9 added three checks over the Event Wheel and every one is relative to *now*** — the
  double-arm refusal, the period bound, and `Unlink`'s removal check. The wheel could not reach a `now`
  except through whoever called it, so each check rested on a property nobody had written down.
- **The end-of-run invariant tier spent both 100,000-Tick acceptance runs stamping every violation
  `Tick 0`.** Each run called `CheckEndOfRun()` on a **freshly built `Simulation` over the already-run
  world**, because the helper that ran the world never handed its own back — and a new Simulation
  honestly reports a Tick of zero.

**The third is the argument, and what makes it one is how long it survived.** It was invisible for
exactly as long as no invariant was relative to the Tick: the stamp was decorative, and a decorative
field agreeing with nothing is not checkable by anything. The first check that read it failed
immediately. **The tier where the Tick is the only temporal context a crash artifact carries is the tier
that had it wrong**, in the runs `CLAUDE.md` names as the ones that surface these bugs.

A number every caller has to be right about is a number no caller should be able to supply.

### Saved implies hashed, and there is no third box to put it in

`adr/0003`'s field declaration has two categories: `(saved AND hashed)` or `(derived AND rebuilt)`. A
Tick is not derived — nothing reconstructs it — so it is saved, and saved means folded. **That pairing
exists so the hash cannot have a coverage hole**, and exempting the clock would open exactly the hole
this decision is about: a save that restored the wrong Tick would agree on the hash while the world's
clock was wrong, which is the `Tick 0` failure arriving through the save instead of through a
constructor.

Invariant 6, the Factorio test, needs the Tick to survive a save. It could not have, because a private
field on the loop is in no field declaration at all.

### The one-row table is precedented, not invented

`WheelBucketTable` already allocates a fixed number of rows up front and frees none, and it is a
`[Table]` rather than a bare array because storage beside the columns is what `BOR0901` is an error for.
`ClockTable` is the same shape with one row. No new declaration kind, no new analyser rule.

## Consequences

- **Every hash in the project moved at once, on account of the composition rather than the city**, so
  `World.HashSeed`'s version byte is bumped to `02` — which is precisely the case that byte exists to
  distinguish from a regression, and the procedure in the golden baseline's `README` is what caught that
  it applied. The three baselines are re-recorded in the same commit and nothing else is.
- **Within-run flatness is gone: every sample of every hash trace now differs from the one before it.**
  *"An idle Tick changes nothing"* was always slightly false — time changed — and it is now false in a
  way a trace can see. **Cross-run comparison is untouched**, because two runs of one log carry the same
  clock, so replay equivalence, save/reload equivalence and the bisection property all stand exactly as
  before.
  - The claim that sentence was reaching for **moved to where it can be stated honestly**:
    *a Tick with no commands changes nothing but the clock*, asserted over every table's fold except the
    clock's. A replay trace was only ever a proxy for it. `Core` gained no second published fold to
    serve it — a second canonical hash would be an API a test wanted and a thing to keep in step for
    ever.
- **`RunStaggered`, `RunEndOfRun` and `CheckEndOfRun` no longer take a Tick.** The bug class is
  unrepresentable rather than fixed: there is nowhere left to pass a wrong one. `InvariantRegistry`
  takes the world in its constructor and reads the Tick from it.
- **The stored Tick is the *next* Tick to run, not the last one run.** That was `_tick`'s convention and
  it is kept deliberately, so this is a relocation and not a redefinition — but the convention now
  reaches the hash and the save, and it is stated on `ClockTable` rather than left to be re-derived from
  where an increment sits. It is why the Event Wheel's period bound is half-open at the bottom.
- **`World.Advance()` is `internal`**, so the Tick loop is the only thing that may move time. Reading it
  is free.
- **`_phase` and `_inForce` stay on the Simulation**, and that is a scope decision rather than an
  oversight. `_inForce` is the Ruleset in force, whose save semantics are `05 §7`'s unargued format
  half; moving it would prejudge that. `_phase` only matters if a save can happen mid-Tick, which
  nothing has decided. Both are in the same position the Tick was, and
  [`plans/0002`](../../plans/0002-open-questions.md) records that rather than this ADR settling it.

## What would trigger revisiting

- **A saved field that genuinely should not be hashed.** This decision leans on `(saved AND hashed)`
  having no exceptions. The first legitimate exception reopens the pairing rather than the clock, and
  `adr/0003` is where it would be argued.
- **A second clock.** `adr/0010` forbids a second time base and `adr/0056` rests on there being exactly
  two levels of one; a mechanism denominated in something else would make `ClockTable` a table with more
  than one column and worth re-reading.
- **The within-run flatness idiom being missed in practice.** If *the state stopped changing* turns out
  to be a thing people reach for often — in a long-run balance run, say — the answer is a named
  clock-excluded fold in `Core` with a stated purpose, not an exemption from the hash.
- **A save format that stores the Tick outside the table dump.** The save is generated from the field
  declaration; if `05 §7` later puts session metadata in a header, the Tick would be recorded twice, and
  one of the two would be the one that drifts.
