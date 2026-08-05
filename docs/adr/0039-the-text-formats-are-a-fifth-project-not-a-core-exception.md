# The text formats are a fifth project, not a Core exception

**The Input Log's on-disk codec lives in `Borough.Formats`, a fifth runtime project that references `Borough.Core` and is referenced by both shells. `05 §1`'s project count moves from four to five. `Borough.Core` gains no filesystem and no human-readable string; the shells gain no private copy of a format they must agree on.**

`LEGIBLE CAUSE`, `FAST ITERATION`

The Input Log is the artefact a bug report is made of, and [`adr/0007`](0007-stress-driven-simulation-detail.md)'s deletion of the camera input plus [`adr/0037`](0037-the-world-is-single-buffered-and-hazards-are-per-table.md)'s deletion of the double buffer have between them made it the *entire* mechanism of crash forensics. Slice 5 settled that its encoding is line-oriented text. That decision immediately raised a question the corpus had no answer for: **text spells verbs in words, and [`adr/0002`](0002-simulation-is-an-engine-agnostic-library.md) says the core returns ids and numbers while the shell owns every string a human reads.**

## Why

### Two codecs for one format is the failure the format exists to prevent

The obvious answer is to put the codec in `Borough.Headless` and let `Borough.Godot` grow its own when it needs one. It obeys every existing rule and needs no argument, which is what makes it attractive and what makes it wrong.

**A log written by the game must replay in the headless runner.** That is not a nice property of the format — it is the format's entire purpose. A player hits a bug, the game writes a log, the log is attached to a report, and `Borough.Headless --strict` reproduces the failure Tick for Tick. Agreement between the writer and the reader is therefore the one property that cannot be allowed to drift.

Two independent implementations is the arrangement most likely to make it drift, and the drift is silent in the worst way: a log that parses cleanly and replays to a *different* city produces a State Hash divergence with no cause, which is precisely the diagnostic dead end this whole slice exists to abolish. Duplication is normally a tidiness complaint. Here it attacks the requirement.

A round-trip conformance test would police it. But a test that must be remembered is a weaker guarantee than a structure in which there is only one implementation to be wrong.

### `Borough.Core` would need an exception, and the precedent is worse than the duplication

The argument for putting the codec in the core is better than it first looks, and it is worth writing down because it will be made again.

It runs: `plans/0003` establishes that **both the save serialiser and the State Hash are generated from the one field declaration** — and that declaration is in `Borough.Core`, so *the core already owns a serialiser*. `adr/0002`'s rule is aimed at Readouts, meaning strings resolved through the Ruleset for a human panel. A format keyword like `zone` is fixed by the format version, is not resolved through anything, is not localisable, and is not a Readout. So the rule does not engage and no exception is needed.

**The reason to reject it is where the precedent stops carrying.** A save is an *array dump* — it contains no words at all, so it is not evidence that the core may write words. Accepting the argument means accepting a case-by-case judgement about which strings are "really" Readouts, and the leak vector `adr/0002` actually names is not `using Godot;` but *a method that returns a formatted string because a panel wanted one*. Every such method will arrive with a reason as good as this one.

There is direct evidence about which way this goes wrong. In slice 3, `Math.Abs(int)` was a genuine exception to the no-`Math` rule — exact integer arithmetic with no intrinsic to vary — and writing the replacement rather than the exemption surfaced a real defect that the exemption would have preserved. **The absolute rule was cheaper to obey than to argue with, and obeying it paid.** That is this decision's evidence, from this codebase, about this class of judgement call.

### The fifth project is a smaller change than the count suggests

`05 §1` calls the project split *the architectural decision*, so moving four to five is not bookkeeping. But note what already sits beside it: `Borough.Analysers` is a fifth project excluded from the count on the stated grounds that it is *a build-time input rather than part of the runtime architecture*. `Borough.Formats` cannot claim that exemption — it ships — so the count genuinely becomes five, and it is stated here rather than smuggled in as a footnote.

The layering is one-directional and shallow:

```
Borough.Core  <-  Borough.Formats  <-  Borough.Headless
                                   <-  Borough.Godot
```

`Borough.Core` does not reference it, which is what preserves *the headless runner must never require Godot* and the core's freedom from the filesystem. Nothing in `Borough.Formats` can reach the running simulation, because the simulation cannot name it.

### What the project owns is a boundary, not a bag

**`Borough.Formats` owns the artefacts that spell things in words.** Today that is the Input Log; tomorrow it is the crash artifact, which is the log plus a header, and any diagnostic dump a human is expected to read.

**It does not own the save.** `05 §7` makes a save an array dump generated from the field declaration, and the declaration is in the core — so the save serialiser stays there, where the thing it is generated from lives. A project that accumulated "all the I/O" would be a bag rather than a boundary, and the line that keeps it a boundary is *does a human read the tokens*.

## Consequences

- **`05 §1` states five runtime projects.** `Borough.Analysers` remains outside the count, and the reason is now stated by contrast rather than in isolation: it does not ship.
- **Both shells depend on `Borough.Formats`,** and neither may parse or emit a log itself. A shell that hand-rolls a log line is the defect this decision exists to prevent, and it is visible in a project reference rather than only in review.
- **The format version is `Borough.Formats`' to bump**, and it must be bumped whenever a field is added to `Command`, to `WorldConfiguration`, or to the header. A log outlives the build that wrote it.
- **The extension is `.borough`.** Not `.inputlog` and not `.log`: the repository's inherited .NET `.gitignore` ignores both, and the golden-hash baseline is a *committed* log. That a template ignore rule could have silently prevented the project's most important regression artefact from ever being tracked is recorded in `plans/0008`.
- **One more project to keep building without Godot installed.** `dotnet build src/Borough.Formats` joins the boundary check, and it is cheap: the project references only the core.
- **A cost worth naming:** for as long as `Borough.Godot` remains a stub, this project has exactly one consumer, and it will look like over-structure to anyone reading the repository cold. It is justified by a second consumer that does not exist yet. If that consumer never arrives, the justification never arrives either — see below.

## What would trigger revisiting

- **`Borough.Godot` never ships, or never writes a log.** The entire argument is that two shells must agree on one format. If only one shell ever writes logs, the duplication risk is hypothetical and this project is structure bought for nothing — fold it into `Borough.Headless` and delete this ADR's premise rather than its conclusion.
- **A format arrives whose tokens must be resolved through the Ruleset.** That would be a Readout in a file, and it belongs to the shell that has the Ruleset, not here. The boundary in *what the project owns* would need restating rather than stretching.
- **The save turns out to need a text form** — for diffing a divergence, most plausibly. That would put a words-spelling format on the field declaration's side of the line and force a genuine choice between moving the save serialiser here and admitting the boundary is not where this ADR draws it.
- **The count moves again.** Three projects would be a merge and six would be a proliferation; either is a reason to re-read `05 §1`'s claim that the split is the architectural decision, rather than to amend the number a third time.
