# Every tuning constant lives in reloadable data, and the binary is a stable interpreter

**Every economy constant, production chain, Household behaviour parameter, traffic parameter, and Zone Rule lives in data files that can be reloaded into a running game.** The compiled binary is a stable interpreter for the Ruleset and nothing else. A tuning number written as a `const` in simulation source is a defect, not a shortcut.

This is filed as an ADR because it is a **project-survival** requirement wearing the costume of a convenience. It is the requirement most likely to be quietly skipped — the reload path is always the thing that can be added later — and it is the one most likely to be fatal.

## Why

Citybound is the proof, and the evidence is unusually direct because its author wrote it down himself.

Anselm Eickhoff's warm rebuild took **60–120 seconds**, in a core he described as *"highly interdependent and thus nearly impossible to break up into crates."* Every balance question — is this production ratio right, is this commute budget too tight — cost two minutes to ask, and two minutes to ask again after changing one number. His final devblog concedes he had *"been abandoning the simulation aspect for a while"* in favour of procedural architecture and UI.

That sentence is the entire argument. Those were the parts of his project with a fast iteration loop, and effort migrates to where the loop is fast — not by decision, but by attrition. **The simulation did not become unbalanceable because balancing it was hard; it became unbalanceable because tuning it was slow, so tuning stopped happening.**

For a game whose entire value is emergent behaviour, an unbalanceable simulation is not a rough edge, it is a terminal condition. `EMERGENCE` is only worth anything if it can be steered; emergence that cannot be steered is indistinguishable from noise, and the difference between the two is discovered exclusively by tuning.

GlassBox reached the same conclusion from the opposite direction. **Hot-reloading everything was its stated reason for existing** — rules were data precisely so that designers could retune a running city rather than wait on an engineer. We are adopting its production model wholesale (`docs/02-simulation-model.md` §4); adopting the reason it was built that way is not optional extra credit.

The acceptance test, stated plainly because it is checkable rather than aspirational:

> **Changing a production ratio and seeing the effect must take seconds, not a rebuild.** `FAST ITERATION`

A secondary benefit is worth naming, because it is the one that would justify the decision even if iteration speed were free: forcing every constant into the Ruleset makes the balance surface **enumerable**. The set of things that can be tuned is a file listing rather than an archaeology exercise across the source tree, and a bug report can carry the exact Ruleset that produced it.

## Rejected

**Constants in code now, reload later.** The retrofit is not a file-format change — it is re-plumbing every reader of every constant, once there are several hundred of them and each has grown a call site that assumes a compile-time value. Built on day one it costs a day; retrofitted it competes with actual game work and loses, which is precisely how Citybound got where it got. The roadmap therefore places the Rule engine third in Phase 1, **with reload working from the start**.

**Code hot-reload** (C# hot reload, edit-and-continue) is not a substitute and is not pursued. It is unreliable across exactly the changes worth making, and unnecessary here: behaviour lives in the Ruleset, so the interpreter itself changes rarely. [`0001`](0001-godot-and-csharp.md) already rejected Bevy in part for long compile times attacking this same requirement — the answer there and here is to stop needing the compiler, not to make it faster.

**A purpose-built DSL for the Ruleset.** Format is **TOML**, per `docs/02-simulation-model.md` §4.3, and the DSL is parked in `docs/deferred.md` with a recorded trigger. That entry is not relitigated here. Note only that this ADR is indifferent to the format: the Ruleset is an input to a stable interpreter, so swapping TOML for something better later touches the loader and nothing else.

> **`02 §4.3`'s reason for TOML was false, and [`0048`](0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md) replaces it.** It said TOML *"requires no parser"*; .NET has no TOML reader, and that sentence is why the dependency stayed unnamed through six slices. The parser is **Tomlyn**, in `Borough.Formats`, chosen on a property this ADR itself requires — source spans, so a refusal can name *a file, a line, and a rule name*. The conclusion above survives: the format is still swappable and still touches the loader and nothing else.

## Consequences

- **Reload is a simulation event, not a side channel.** It is swapped at a phase boundary and recorded in the Input Log, so replays remain exact. The Ruleset's content hash feeds the State Hash — otherwise two runs of the same log against different data would diverge with no indication why.
- **Reload semantics must be defined for every removal**, not just additions. Bins whose resource no longer exists are dropped with a logged warning; Buildings whose kind no longer exists are marked derelict rather than deleted. Silent deletion during a balance session is how a save gets quietly corrupted.
- **Bad data fails loudly and locally.** A malformed Rule reports a file, a line, and a rule name, and the **previous Ruleset stays live** rather than the game dying. Error-message quality is part of the deliverable, not polish — a reload loop that punishes typos with a crash is a reload loop that stops being used.
- **Every new system arrives with its parameters in the Ruleset.** "Where does this number live" has exactly one answer, permanently.
- **There is a second category: world-creation constants.** These live in the Ruleset like everything else and are *read* from it, but they are **fixed when a world is created and baked into the save**, and a reload that changes one is refused rather than applied. The test is whether existing simulation state was recorded in units of the constant. `TICKS_PER_DAY` is the clearest case — every event already sitting on the Event Wheel was scheduled in Ticks against an assumed Day length, so changing it mid-save silently reinterprets every pending Life Stage countdown and decline window ([`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md)). `WHEEL_SIZE` and the **Cell** are in the same category for the same reason, one structural and one spatial. **The membership is enumerated, and the enumeration is the point** — a category with unlisted members is a set of exceptions wearing a category's name. The full list is `TICKS_PER_DAY`, `WHEEL_SIZE`, the **Cell** (`adr/0034`), and the **industrial pollution kernel radius** (`adr/0044`), which passes the test because a Layer's stored values were accumulated in units of it. Note the diffusion **cadence** fails the same test and is therefore ordinary hot-reloadable Ruleset data — the two numbers arrived together and separate here.

  > **Corrected by [`0034`](0034-fields-are-sorted-by-source-geometry.md).** The spatial member is **Cell size**, not Chunk size — the Cell is the grid Map Layers and Sealing are recorded in, which is what makes it satisfy this category's own test (*existing simulation state was recorded in units of the constant*). Chunk size fails that test and belongs to the profiler; it stays pinned only because a save is a sequence of Chunk records, which is a format constraint rather than a semantic one. This is a **named category with an enumerated membership**, not a set of exceptions — the moment it becomes a place to put anything inconvenient, this ADR has been defeated.
- **The validator this ADR implies has been specified**, in [`0048`](0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md): it lives with the parser, because *"error-message quality is part of the deliverable"* means its whole output is human-readable strings and `adr/0002` forbids the core from producing one. It runs **five** refusals in one walk at load — `0045`'s `on_fail` cycle check and `fills` check, its **unterminated chain** check, an **unbalanced money** check per `adr/0024`, and an unquoted decimal, which is refused rather than coerced because a library's `double` must never reach a tuning number.
- **This ADR's ordering claim is retired rather than re-grounded.** *"The roadmap therefore places the Rule engine third in Phase 1"* cited `06`, which cited this ADR back — one unargued claim counted twice. **Slice 6 then falsified it and cost nothing**, because `LayerRuleset` arrived as a constructor argument rather than a `const`. **The no-`const` rule above is the mechanism; the milestone order was a proxy for it.** What replaces the claim is checkable: slice 8 is not done until the Layer cadence and rates load from a file.
- **The headless runner is the real iteration loop.** Reload makes single-change tuning fast; headless fast-forward makes parameter sweeps possible at all. The two are the same investment.
- **The game becomes moddable almost by accident**, which the vision lists as an anti-goal at launch. That is a reason to keep the Ruleset format clean rather than a reason to obstruct it.

## What would trigger revisiting

- **A parameter that genuinely cannot be data** — something that determines a table width, an array stride, or a memory layout fixed at startup. Distinct from the world-creation category above, which *is* data and is merely frozen per-world. These are expected to exist and to be few. Each gets a written exception naming why, and requires a restart rather than silently ignoring a reload. A growing list of exceptions is the signal that the boundary has been drawn in the wrong place.
- **Reload cost becoming visible in the tick**, or the Ruleset growing large enough that swapping it stalls a running city. The answer is incremental reload of changed files, not abandoning reload.
- **The seconds test failing for reasons other than compilation** — for instance, a ratio change whose effect takes ten thousand Ticks to become observable. That is a different problem with a different fix (faster fast-forward, better instrumentation), and it must not be allowed to discredit this decision by association.
