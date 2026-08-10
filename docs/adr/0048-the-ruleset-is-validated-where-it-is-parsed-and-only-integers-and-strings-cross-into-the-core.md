# The Ruleset is validated where it is parsed, and only integers and strings cross into the core

**`Borough.Formats` parses the Ruleset, resolves every name to an id, and runs every refusal; `Borough.Core` receives a validated, string-free table of integers.** The parser is **Tomlyn**, and it is not a core dependency, so [`0003`](0003-deterministic-integer-simulation.md)'s exception machinery does not apply. **A tuning number reaches the simulation as a bare TOML integer or as a quoted decimal string** converted by our own routine — never through a library's `double`. `LEGIBLE CAUSE` `FAST ITERATION` `SOLVE THE ACTUAL PROBLEM`

## Why

### `02 §4.3` says TOML *"requires no parser"*, and that is false

.NET has no TOML reader. Something is taken or something is written, and the sentence is why the dependency sat unnamed through six slices while three documents referred to it. **Tomlyn** is taken: 5.3M downloads, actively maintained, and — the property that actually decides it — a syntax tree carrying source spans, which is what [`0015`](0015-all-tuning-data-is-hot-reloadable.md) already requires when it promises *"a file, a line, and a rule name."* [`0018`](0018-prefer-off-the-shelf-infrastructure.md) demands a written exception naming a property no library provides before anything is built by hand. There is no such property here, so writing one is not available.

### The float hazard is real, and it is invisible

[`0003`](0003-deterministic-integer-simulation.md) bans `float` and `double` from simulation state and arithmetic. TOML has a native float type, and every library will parse `decline_rate = 0.15` into a `double`. Three spellings were available:

| | The designer writes | Exposure |
|---|---|---|
| decimals, converted in `Formats` | `decline_rate = 0.15` | a `double` exists for microseconds, and the conversion must be pinned by hand |
| fixed-point integers | `decline_rate = 9830` | none |
| **decimals as strings** | `decline_rate = "0.15"` | **none** |

**The middle one is correct and defeats the ADR it serves.** `0015`'s acceptance test is *"changing a production ratio and seeing the effect must take seconds"*, and a designer computing Q16.16 in their head is not that. It also fails the reason the Ruleset exists at all: the balance surface is meant to be **enumerable and readable**, and `9830` is neither.

**The first one is rejected on the shape of its failure rather than on its likelihood.** A `double`-mediated conversion is right on both machines almost always, and when it is not, the symptom is a State Hash divergence between two developers with no diff to look at — the class of bug `05 §7` says costs days. Quote marks make the hazard structurally impossible instead of carefully managed, which is the same move `0034` made when it split the Cell out of the Chunk: remove the welding rather than document it.

So the library only ever hands us **strings, integers and booleans**. Its float path is never on the path, and an unquoted decimal is a **named refusal** rather than a coercion.

### The validator cannot live in the core, and the argument is `adr/0002`

A cycle check walks `on_fail` names. A `fills` check compares a declared resource against a Bin's. Both need names, and both must report one — *"rule `bake_bread` has a cycle"* is the whole deliverable, because `0015` makes **error-message quality part of the deliverable, not polish**. [`0002`](0002-simulation-is-an-engine-agnostic-library.md) forbids the core from producing a string a human reads.

**A validator that cannot name what it rejected is not a validator.** So it sits with the parser, where the names are, and the core receives ids.

The cost is drift: `Formats` could accept a Ruleset `Core` cannot run. That is answered the way this project answers drift elsewhere and not with a second validator — **the interpreter refuses an id it does not know** rather than trusting the table. One assertion, not a duplicated rule set.

### What this does to `adr/0003`'s owed exception

`0003` requires any **core** dependency be argued explicitly, because a determinism liability entering the core is not recoverable. Tomlyn enters `Borough.Formats`, which already declines to load `Borough.Analysers` — its own project file says so — because its job is to write the strings the core's lints forbid.

**So the exception is not owed, and something narrower is.** What matters was never which assembly holds the parser; a nondeterministic parse poisons the simulation from any distance. What matters is the **values crossing the boundary**, and the rule is one line: *nothing but integers and strings crosses from the parser into the loader.* Under that rule the exposure is a digit-to-integer conversion and our own decimal routine, both of which are ours and both of which are testable.

## Rejected

**Decimals converted through the library's `double`.** Rejected on failure shape, above. It reopens if the conversion is ever proven bit-identical across platforms *and* the proof is mechanised — but the proof is the expensive part and the quote marks are free.

**Fixed-point integers in the file.** Correct, zero-exposure, and it attacks `0015`'s acceptance test directly. Rejected on the ADR it serves.

**Validation in the core.** Fails `0002` at the exact point that matters, since the validator's entire output is human-readable.

**Splitting refusals between the two projects** — structural in `Formats`, semantic in `Core`. No principle decides which check goes where, and [`0045`](0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md) already established that its two refusals are *"one walk at load"* — the same walk. Two error surfaces where `0015` promises one.

## Consequences

- **Three refusals, not two.** `0045` handed this session the `on_fail` **cycle check** and the **`fills` check**. The decimal rule generates a third: **an unquoted decimal is refused by name**, never coerced. All three are one load-time walk in `Borough.Formats`, on the error surface `0015` specifies.
  - **There are now eleven at load and a twelfth on reload, and this bullet is the count of record.** Slice 7 added an **unbalanced money** refusal (`adr/0024`, when the Resource family was taken out of order) and an **unterminated chain** refusal (task 8, enforcing `0045`'s own reporting terminal). Slice 10 task 3 added three over `[[zone_rule]]`: an **undeclared kind**, a **permission bit no `zone` verb can paint**, and a **sample of zero**. The number is stated here, in `0015`, and in `0003`'s gate board, and all three had drifted apart by task 8 — which is `plans/0012`'s *Cause 1* in the ADR that owns the surface.
  - **Slice 8 task 3 added three more of that same class, over `[layers]`**, and they are what a Map Layer's version of *loads clean and does nothing* looks like: a **period of zero**, which freezes the field; an **offset outside its period**, which has no solution to `tick % period == offset` so the Layer is never recomputed; and a **decay that rounds to never**, where a duration shorter than the cadence it is counted in divides to a tau of 0 and 0 is the sentinel for *never* — a file that reads *fades in a minute* and behaves as *never fades*. The last of the three exists only because the tau is a **derivation** rather than a literal, which is `plans/0015`'s decision owed 2; a literal would have had no second number to be inconsistent with.
  - **The twelfth runs on a second entry point and is the first refusal that is not a property of the file alone.** `0015`'s world-creation constants are read from the Ruleset and frozen when a world is created, so *whether a value changed* is only knowable against the world it would be applied to. `RulesetLoader.Reload(text, file, frozen)` takes the world's frozen constants — a handful of integers, which cross the boundary under this ADR's own rule — and refuses a change. **It compares the effective value, not the authored one**: the kernel is written in metres and used in Cells, and two authored figures that round to one Cell count build the same kernel, so refusing between them would refuse a reload that reinterprets nothing. That comparison *is* `0015`'s membership test rather than an approximation of it.
  - **The argument of that entry point is one number where the category enumerates four, and that is a finding.** See `plans/0012` — `0015` says its world-creation members *"live in the Ruleset like everything else and are read from it"*, and after slice 8 task 3 that is true of the kernel radius alone. `TICKS_PER_DAY`, `WHEEL_SIZE` and the Cell are `const`s no Ruleset can state, so a refusal over them would be a check with nothing on the other side of it. Each joins the signature on the day it becomes authorable.
  - **What is *not* counted, and the line is worth stating because it is where the number would drift next.** A duplicate `[layers]`, an unknown section name and a duplicate `[[resource]]` name are all refused and none of them is in the count: they are ambiguities a designer sees immediately. The count is of refusals that catch a Ruleset which would otherwise **load clean and misbehave in silence**, which is what every member of it has in common and what makes it worth stating at all.
  - **The three added by slice 10 are one class rather than three, which is why they were worth stating separately.** Each describes a Zone Rule that loads clean, triggers on schedule for ever, and builds nothing — the same symptom from three causes, and a symptom no author would recognise as a Ruleset defect. That is the `apply = {min=1,max=4}` behaving as `{1,1}` failure generalised: a silent narrowing is indistinguishable from a quiet design decision, so the loader has to be the thing that tells them apart.
- **`02 §4.3`'s *"requires no parser"* is struck.** It is the sentence that hid the dependency.
- **`Borough.Formats` gains a package reference and stays lint-free by design.** Nothing changes in `Borough.Core.csproj`.
- **The core's interpreter carries one cheap assertion** — an unknown id is refused — and no defensive checking beyond it.
- **`CONTEXT.md` → Ruleset gains the boundary**: it is validated where it is parsed, and refused rather than warned about.
- **A Ruleset is TOML-shaped rather than idiomatic TOML**, and that is the price paid. Anybody reading `= "0.15"` and reaching for the quotes should find this ADR.

## What would trigger revisiting

- **A tuning number that cannot be expressed as an integer or a decimal string.** A curve, a table of pairs, an expression. TOML arrays cover more of this than it first appears; something that genuinely does not fit is the trigger for the DSL already parked in [`deferred.md`](../deferred.md), not for loosening this rule.
- **Tomlyn becoming unmaintained, or its integer parsing proving culture-sensitive.** The second is testable today and should be tested when the loader is written rather than assumed — under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) that claim is **measurable** and the machine that settles it is a unit test with a non-invariant culture set.
- **The drift assertion firing in practice.** If `Formats` repeatedly accepts what `Core` refuses, the single-validator claim is wrong and the rules want a shared declaration rather than two implementations.
- **A designer study finding the quote marks are a real obstacle.** This decision rests on them being trivial. That is an assumption about people, not about machines, and the acceptance test is `0015`'s: does changing a ratio still take seconds.
