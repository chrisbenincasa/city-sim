# Scratch is a third Disposition, because Derived is a claim a scratch column does not make

**`Disposition` gains a third value, `Scratch`: storage that is neither saved, hashed, nor rebuilt,
and whose content between the phases that use it is meaningless by declaration. It carries an
obligation the other two do not — *a scratch column must be written before it is read within the phase
that uses it, and nothing outside that phase may read it* — and that obligation is asserted rather
than assumed: filling every scratch column with garbage at a phase boundary must not move the State
Hash trace.** The rebuild audit skips these columns **by declaration**, and there is no exemption list
anywhere.

Guiding concepts: `HONEST DEGRADATION`, `SOLVE THE ACTUAL PROBLEM`.

This is an **arguable** claim under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
It fixes no number. What it is settled by is reading `Derived`'s stated contract against one column
that does not satisfy it, and reading [`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)'s
sibling question — *where does an exemption live* — which this corpus has answered once before in the
same file.

## Why

[`0003`](0003-deterministic-integer-simulation.md)'s field rule is that every field in a table is
declared once as `(saved AND hashed)` or `(derived AND rebuilt)`, and `Disposition` welds the two
halves of each pair together so that *saved but not hashed* — the state with no detector — is
unrepresentable. `BOR0901` makes storage in a `[Table]` type that is neither a build error. The result
is that a field is in the file by construction or was never meant to be, and there is no third case.

**There was a third case, and it had been declared as the second one for the life of the column.**
`layer_cell.pollution_pass` is the horizontal pass's intermediate in the separable convolution: it is
written and read entirely inside one call to `LayerDiffusion`, and it survives to the next only
because a column is storage rather than a local. Nothing rebuilds it. Nothing should.

### `Derived` is a claim, and this column does not make it

The enum says so at the declaration: choosing `Derived` *"is a claim that the field is a pure function
of saved state, and the claim is checkable: rebuild it and assert it matches."* That is not a loose
description of *not saved*. It is a specific assertion about recoverability, and it is the assertion
`World.RebuildDerived` exists to honour.

`pollution_pass` is a pure function of nothing. Its content between two diffusions is meaningless —
its own declaration has said so since slice 6. So declaring it `Derived` was **not a weaker version of
that claim but a false one**, and the falseness had no consequence only because nothing had ever
checked the claim for all columns at once. Milestone 8 task 1 is what checks it.

***A disposition set that forces a third kind of field to pick one of two is a declaration with a
hole, and the hole shows up as an exemption in somebody else's test.***

### A third value rather than a second axis, and rather than a modifier on `Derived`

A second axis in the shape of `Touch` or `Reference` would narrow `Derived` — *derived, but this kind
of derived* — and narrowing a claim this column never made is the wrong operation. `Disposition`'s own
question is **what is this column for**, and *scratch* is a third answer to that question rather than a
qualification of the second.

**It does not reopen what the welding closes.** The forbidden state is *saved but not hashed*; scratch
is neither saved nor hashed, so the pair stays welded, the enum stays one enum, and the transitive
proof `Disposition`'s remarks rest on — *save/reload equivalence proves hash completeness* — is
untouched, because it quantifies over saved fields and scratch is not one.

### The alternative was an exemption list, and this corpus refused that once already, one file below

The rebuild audit is driven by the column declarations. So is `02 §10`'s every-handle-resolves walk,
and that walk needed exactly one column exempted — `CitizenTable.Workplace`, whose target may be freed
underneath it. The exemption became `Reference`, an axis declared **at the field**, and the reason is
written in `Declaration.cs` where it still is:

> That walk is driven by the columns for a stated reason — **a list of fields shares its blind spot
> with the bug it exists to find** — so the one column that is allowed to dangle has to say so where
> it is declared, in the same place and the same spirit as `Disposition`.

The rebuild audit is a declaration-driven walk with one exempt column. That is the same shape, in the
same file, and an exemption list inside the audit is the arrangement that reasoning refused.

### The standing objection is refutable, and the experiment has already been run

[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s neighbourhood supplies the
objection: **one column is not a general mechanism**, and deciding a taxonomy on a single instance is
how a taxonomy grows a member nobody needed. That is a real rule and it is worth stating that it does
not bind here — not because the argument is weak, but because it is a **prediction**, and this
repository has run the experiment once.

`Reference` was introduced for one column and says so: *"Slice 10 is what made this necessary, and only
one column needs it."* It is now **seven columns across five tables** — `LegTable` ×2, `TripTable` ×2,
`RouteHopTable`, `CondemnationTrailTable`, `CitizenTable` — reached within two milestones of the axis
being added. The only evidence available about what happens to a declaration axis added for a single
instance in *this* codebase points the other way from the objection, and the sentence carrying the
prediction is now wrong by 7× and filed to [`plans/0012`](../../plans/0012-corpus-audit.md).

***A count stated as a fact about the build ages against the build, and it is read by exactly the
person deciding whether to add another axis.***

### What makes this better than an exemption is that it is an obligation

An exemption says *do not look at this one*. It is unfalsifiable by construction: the audit skips the
column, and whether skipping was safe is a claim nobody can check.

`Scratch` says *it cannot matter what is in it*, and that is checkable. Fill the column with garbage at
a phase boundary and the State Hash trace must be unchanged. If a phase ever reads scratch before
writing it, the fill changes what that phase computes, the hash moves, and the test fails.

That closes the hazard the exemption would have left open, and it is a real hazard rather than a
tidiness argument: **scratch content becoming a hidden input to the simulation is unhashed state
affecting behaviour**, which is the divergence class `Disposition` exists to close, arriving through
the one column declared not to matter. `HONEST DEGRADATION` is the tag because the failure mode being
refused is a silent one.

## Consequences

- **`Rows.Scratch<TField>()` is the third declaration door**, beside `Saved` and `Derived`. There is no
  `ScratchHandle`: a handle whose target may be recycled is not something a phase-local intermediate
  should be holding, and `DerivedHandle` already has zero call sites.
- **`Rows.Fold` needed no change**, because it filters on `Saved` rather than against `Derived`. A
  third disposition was invisible to the State Hash by construction, which is why this decision moved
  no baseline. The two behavioural readers of `Disposition` in the whole tree were that filter and the
  footprint report.
- **The footprint report counts three ways**, where it counted saved against everything-else. An
  `else` there would have silently folded scratch into the derived column, and the saved figure is what
  the save's size will be read off — so the other two have to be told apart where somebody can see
  them.
- **The rebuild audit skips `Scratch` and asserts the scratch set by name.** A second scratch column
  declared later fails that assertion rather than being silently unexercised, which forces its author
  to extend the garbage fill. That is the exemption list's job done by a test that fails when the list
  would have gone stale.
- **A column that is genuinely recoverable and merely happens to have no rebuild yet is `Derived` with
  a missing rebuild — a defect — and not this.** The two are distinguishable by asking whether any read
  of the column outlives the phase that wrote it, and that question has an answer in the code rather
  than in the author's intent.
- **`05 §4` invariant 7's shape is unaffected.** Scratch storage is still `unmanaged`, still declared
  once, still allocated by the declaration. This adds a disposition, not an escape hatch.
- **⚠ It creates a way to dodge the rebuild audit, and the obligation is the whole defence.** An author
  whose derived column fails the audit can make the failure go away by redeclaring it `Scratch`. What
  stops that is not the type system: it is that `Scratch` is then subject to the garbage fill, which a
  genuinely-derived column with real readers will fail. **The cost of the wrong choice is a different
  red test, not a green suite** — which is the property worth having, and it is worth saying plainly
  that it is a property of the tests and not of the declaration.

## What would trigger revisiting

- **A scratch column whose phase cannot be named.** The obligation is phrased *within the phase that
  uses it*; a column written in one phase and read in another is not scratch under this decision, and
  if one is genuinely wanted then the boundary this rests on is wrong and the decision should be
  reopened rather than the column stretched to fit.
- **The garbage fill becoming impractical.** It is one typed `Span.Fill` today because there is one
  scratch column. At five or ten, doing it column by column stops being reasonable and it wants the
  type-erased byte accessor milestone 8 task 2 builds — which is a change of mechanism, not of
  decision, but it is the point at which somebody should check that the assertion still runs at all.
- **`Scratch` reaching a table that is `Buffering.TwoCopies` for a reason other than diffusion.**
  `LayerCellTable` is the only double-buffered table and its scratch column is inside a synchronous
  phase. A scratch column in a table written by a genuinely parallel phase raises a question this
  decision does not answer: whether the garbage fill can even be placed somewhere meaningful when the
  phase's internal ordering is not fixed.
- **The count going the other way.** If `Scratch` is still exactly one column after several more
  milestones have added tables, `0070`'s objection was right and the honest response is to record that
  — the axis stays, because removing it would restore a false declaration, but the *precedent* this
  decision sets for adding an axis on one instance should stop being cited.
