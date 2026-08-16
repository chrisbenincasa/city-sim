# 0006 — Slice 3: the analysers

> Slice 3 of [`0003-build-plan.md`](0003-build-plan.md). Governed by
> [`05 §4`](../docs/05-technical-architecture.md),
> [`adr/0002`](../docs/adr/0002-simulation-is-an-engine-agnostic-library.md),
> [`adr/0003`](../docs/adr/0003-deterministic-integer-simulation.md),
> [`adr/0036`](../docs/adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md).

**`05 §4`'s rules are enforced mechanically because they fail silently and are violated by
accident.** Three of them exist today as reflection tests from
[`dev-environment.md`](../docs/dev-environment.md) A5, which cover *state* and not *arithmetic*,
`Dictionary` enumeration, or the `unmanaged` constraint. This slice writes the analysers that close
the difference.

**Risk retired.** That a rule stated in a document never becomes a rule the compiler knows. The
project's whole posture is that these constraints are cheap to hold and impossible to retrofit — *a
single `using Godot;` added under pressure would not be noticed for months, and by then it would not
be one.* The same sentence is true of a `double` in a cost function and of a `List<T>` on a Bin. An
analyser is the only thing that notices in the month rather than in the year.

**Why here and not later.** An analyser written before the first table shapes the code; an analyser
written after ten thousand lines exist condemns it, and the response to a lint that condemns working
code is to waive the lint. `adr/0002` names *either CI check being disabled or waived* as its own
revisit trigger, which is the corpus admitting this failure mode in advance.

---

## Gate

**None.** All seven rules are stated. What is missing is a project, not a decision.

## Prerequisites

Slice 2, weakly — rule 2 and the division/shift lints have something to point at once the substrate
exists, and rule 7's `unmanaged` constraint has nothing to check until slice 4. The analyser project
itself depends on neither.

---

## The seven rules, and what each needs

`05 §4` enumerates seven. **`adr/0036` calls the same rule its sixth and
[`0002`](0002-open-questions.md) calls it the seventh** — one rule with three counts across three
documents. Fix the count in `adr/0036` while this slice is open; a checklist that cannot agree on its
own length stops being checked.

| # | Rule | Today | This slice |
|---|---|---|---|
| 1 | **No Godot reference from `Borough.Core`, transitively** | reflection test, A5 | keep. It works, and the transitive case is what a reference test is good at |
| 2 | **No floating-point types in simulation state or arithmetic** | reflection test over *state* only | **analyser.** The rule was widened deliberately — the old wording permitted `int r = (int)(a * 1.5f)`, a float temporary that is *exactly as non-deterministic* as a stored one, via x87 80-bit intermediates, FMA contraction and differing SIMD widths. A reflection test over fields cannot see a temporary |
| 3 | **No `Dictionary`/`HashSet` enumeration; no `System.Random` anywhere** | nothing | **analyser.** Note the shape: hash maps *may* be built and looked up, they may **not** be walked. That is a call-site rule, which is what an analyser is for and what a reflection test structurally cannot express |
| 4 | **Thread-count equivalence** | n/a | **Not writable yet, and that is correct.** Phase 1 is entirely single-threaded; the test is written when the first parallel phase lands, not before |
| 5 | **Replay equivalence** | n/a | slice 5 |
| 6 | **Save/reload equivalence** | n/a | milestone 8 |
| 7 | **No reference types in simulation state** — every table row type and every derived structure satisfies `unmanaged` | nothing | **analyser.** Bites from slice 4 onward |

---

## Tasks

### 1. The analyser project

A Roslyn analyser project, referenced by `Borough.Core` as an analyser and not as a dependency —
`adr/0003` requires any core *dependency* be argued explicitly, and an analyser is a build-time
input rather than a runtime one, which is the distinction worth stating in the csproj comment.

Each diagnostic needs an id, a category and a **message that names the rule and the document**. A
diagnostic reading `CS9001: float in Core` teaches nothing; one reading `no float arithmetic in the
core — 02 §8 rule 1; use Q16.16 or an integer` teaches the rule to whoever hit it, which is the only
time anybody reads a design document about arithmetic.

### 2. Rule 2 — no floating-point arithmetic

Over the `Borough.Core` assembly. Fields, locals, parameters, return types, **and expressions** —
the temporary is the case the reflection test misses and the case that motivated widening the rule.

Flag alongside it, from the same ADR's banned-construct table:

- `Math.*` in any form. The replacement is the tabulated `exp`/`log` from slice 2.
- Raw `/` and non-constant `<<`, which must go through slice 2's stated-rounding and range-checked
  helpers.
- `DateTime`, `Stopwatch`, `Environment.TickCount` — the library has no clock; it has a `u64` Tick
  counter.
- `Guid.NewGuid()` and default `object.GetHashCode()`.

### 3. Rule 3 — no hash-map enumeration, no `System.Random`

- `foreach` over a `Dictionary` or `HashSet`, and `.Keys`/`.Values`/LINQ over one. `Dictionary<string,T>`
  iteration order differs between runs of the same binary because .NET randomises string hashing per
  process — the failure is per-*process*, so it will not reproduce for whoever is debugging it.
- `System.Random`, anywhere in `Core`. The .NET implementation changed in .NET 6, which is the
  concrete demonstration that a shared stream is not a format.
- Build-and-look-up remains legal. The diagnostic must say so, or it will be worked around rather
  than obeyed.

### 4. Rule 7 — no reference types in simulation state

Assert that every table row type and every derived structure satisfies the `unmanaged` constraint.

**Rule 7's actual target is not the tables.** Arrays of unmanaged structs are opaque to the GC, so
the tables were never at risk. The risk is the three derived, variable-length, per-entity structures
— the **per-Bin wait lists**, the **cached Parking Sheds**, and the **Event Wheel buckets** — which
as per-entity collection objects would be on the order of a **million long-lived traced references**
at the 1M target. That is the GC risk in its entirety, and it is why the companion rule is that
**every variable-length collection in `Borough.Core` is an intrusive index list**: a head index on
the owner, a `next` index on the element, both in flat arrays, never a per-entity collection object.

**Two exceptions are owed and have never been enumerated:** the **Ruleset interpreter** and the
**`Evidence` surface**. `adr/0036` says they must be *listed deliberately before the analyser ships*
— which is this slice. They sit on the hot/cold axis `adr/0002` already established: the hot path
allocates nothing and holds no references; the cold path may do both, because it runs on a click.
Encode the exception as that axis rather than as a list of type names, or the list becomes a place to
put anything inconvenient.

### 5. `purpose_tag` uniqueness, at build time

Over slice 2's central enum. **Nothing at runtime can catch a reused tag** — the correlation it
creates between two decisions is invisible, produces no exception, and looks like a plausible city.
A build-time check over the enum is the only possible detector, which is why the corpus states it as
build-time rather than as a test.

Slice 2 left a unit test as a stopgap. Replace it and say in the commit why a test was not enough.

### 6. Prove each analyser fails

For every diagnostic, commit a test that **writes the violation and asserts the diagnostic fires**.
`dev-environment.md` A5 already insists on this for the Godot guard — *`Core_does_not_reference_Godot`
has been seen to fail when Godot is actually used from `Borough.Core`* — and the reasoning
generalises. A guard nobody has watched fail is a guard nobody knows is wired up.

---

## Acceptance

- `dotnet build` fails, with a specific and readable diagnostic, on each of: a `double` local in
  `Core`; a `Math.Exp` call; a `foreach` over a `Dictionary`; a `System.Random` construction; a
  `List<T>` field on a row struct; a duplicated `purpose_tag` value.
- `dotnet build` **succeeds** on: a `Dictionary` built and looked up but never walked; a reference
  type in a documented cold-path exception.
- Every diagnostic message names its rule and the document that states it.
- `dotnet test` green, including the deliberate-violation tests.
- `adr/0036`'s lint count is corrected to agree with `05 §4`.

## Decisions owed by this slice — settled

- **The rule-7 exception boundary.** Settled as the **hot/cold axis** and recorded in `adr/0036`,
  which owed exactly this enumeration. The two candidates it named — the Ruleset interpreter and the
  `Evidence` surface — are not two special cases but one: *a type is entitled to the exception when
  no code path from `step()` reaches it*. Encoded as `[ColdPath("why")]` with a **required** reason,
  at the declaration rather than in a list, because a list is a place to put anything inconvenient
  and it lives in a file nobody edits while writing the type.

- **The rule is opt-out, not opt-in** — a decision the plan did not anticipate needing. Every struct
  in `Borough.Core` is checked; the alternative, a `[SimulationState]` marker checked only where
  applied, makes a forgotten marker a silent exemption that reports nothing anywhere. Opt-out also
  means the rule bites slice 4's rows the moment they are declared, with nothing to remember, which
  removes the plan's own reservation that rule 7 "has nothing to check until slice 4".

- **Raw `/` is an error, not a warning.** As recommended, and for the reason recorded rather than the
  one that motivated the question: a lint that is *sometimes* right gets suppressed at the site where
  it was right. The helper is only load-bearing if it is the only spelling. The known cost is a
  `const` that wants a division — `IntegerMath.FloorDiv` is not constant-evaluable, so such a
  constant must be written as a literal. Nothing in the core needs one today; if that changes, the
  cheap answer is a constant-folding exemption and it should be argued then rather than pre-granted.

- **Constant shift counts stay legal**, which `05 §4` already implied by banning the *non-constant*
  `<<` specifically. The hazard is masking, a count written as a literal or a `const` is visible and
  cannot vary, and `Randomness.Mix` is built out of exactly those.

- **`SortedDictionary` and `SortedSet` are not banned.** Their order comes from a comparer rather
  than from a hash, so walking one reproduces. The *interfaces* are banned, because an
  `IDictionary<,>` hides which of the two it is holding.

---

## What it produced

**Twelve diagnostics in `src/Borough.Analysers`, every one an error.** Ids follow `05 §4`'s lint
numbering — `BOR02xx` is lint 2, `BOR03xx` is lint 3, `BOR07xx` is lint 7, `BOR08xx` is the
`purpose_tag` row of the same section's banned-construct table, which is stated as build-time but is
not one of the seven. Roslyn's own release tracking (`AnalyzerReleases.*.md`) keeps the id ledger, so
renumbering one is visible in a diff.

**It found a real violation in the ~700 lines that already existed**, which is the argument for
writing analysers before the code rather than after it, made concrete: `Tiles.Magnitude` called
`Math.Abs`. The ban read over-broad there — `Math.Abs(int)` is exact integer arithmetic with no
intrinsic to vary — but obeying it rather than arguing with it surfaced the actual defect underneath:
`Math.Abs(int.MinValue)` throws, and `Tiles.Magnitude` was propagating that without saying so. The
replacement is `IntegerMath.Abs`, which states the edge case. **The absolute rule was cheaper to obey
than to argue with, and obeying it paid.**

**Nothing else in the core moved.** `Borough.Core` builds clean under all six analysers, which is a
weak signal at this size and the right one to record anyway: the lints were written early enough that
they shape the code instead of condemning it, which was the stated reason for scheduling this slice
before the first table.

**The stopgap `PurposeTagTests` is deleted**, as `plans/0005` asked. Why a test was not enough is in
`PurposeTagAnalyser`'s own doc comment, and it is not the usual argument about tests running late:
every other rule here describes a defect that *something* could eventually observe, and a reused
`purpose_tag` has no runtime symptom at all — it produces a city that is entirely plausible. A test
runs at the wrong *kind* of moment, not merely a late one.

**A review pass found the rules half-enforced, and the gap had one shared cause.** The first cut
asked whether a type's `SpecialType` was `Single`, `Double` or `Decimal` — a check shaped like the
*keyword* when the rule is about the *arithmetic*. That left four doors open, each easier to walk
through than writing `double`: `List<double>` and `Func<double, double>` hide it in a type argument,
`double?` hides it in `Nullable<T>`, and **`Vector2` hides it in two `float` fields while being
`unmanaged`, so lint 7 waved it through as well.** `Vector2` is the one that would actually have
happened: it is what somebody writing a position reaches for, and this is a Godot project. The fix is
one predicate — `CoreConventions.CarriesFloatingPoint`, recursing through arrays, type arguments and
the fields of a struct, stopping at classes because following a class's fields reaches floating point
from nearly every type in .NET. Five more holes came out of the same pass: lifted operators
(`int? / 2` has no `OperatorMethod` *and* no integer `SpecialType`, so it passed both guards),
`string.GetHashCode` and `System.HashCode` (both seeded per process — **the rule was contradicting
itself**, since randomised string hashing is the stated reason `BOR0301` exists), `FrozenDictionary`,
a `Dictionary` subclass, a child namespace inheriting the arithmetic exemption, and a struct nested
inside a `[ColdPath]` one taking the exemption without arguing for it.

**One diagnostic was instructing an action it forbade.** `BOR0301` said *"use a sorted array"*, and
every route from a hash map to one — `.Keys`, `.OrderBy(…)`, LINQ — is itself reported. Rather than
carve out `OrderBy`, which only restores determinism when the sort key is total and an analyser
cannot check that, the message now says the right thing: **order comes from the ordered source — the
Ruleset's document order — and the map stays a lookup index.** A lint whose remedy is illegal gets
suppressed rather than obeyed, which is the failure this slice's own negative tests exist to catch.

## What this slice deliberately does not do

Rules 4, 5 and 6 — thread-count, replay and save/reload equivalence. Each needs machinery that does
not exist, and rule 4 in particular **must not** be written speculatively: Phase 1 is single-threaded,
so a thread-count equivalence test today would assert a property against no parallelism and pass
vacuously forever.
