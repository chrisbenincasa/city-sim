# We buy infrastructure off the shelf and build only the thing that is the game

**A standing bias against writing bespoke frameworks, runtimes, allocators, serialisation layers, geometry kernels, and renderers.** Where a competent library exists we use it, including when it is uglier than what we would write ourselves.

This ADR is not here to be agreed with once and filed. **It exists to be re-read at the exact moment building something from scratch has started to feel reasonable** — because the feeling is the symptom, and it never arrives alone. It arrives with good arguments attached, a clear picture of the design, and the pleasant sensation of a problem that is finally tractable.

## Why

Anselm Eickhoff spent 2014–2020 solo on very nearly this game, and **the thesis survived**: roughly 400,000 individually simulated cars on a single core, with a real production economy underneath. Microscopic simulation was never what defeated him. What he wrote instead of the game was:

| Library | What it was |
|---|---|
| `kay` | bespoke actor runtime |
| `chunky` | bespoke allocator |
| `compact` | compact-memory trait |
| `kay_codegen` | build-time code generation |
| `descartes` | geometry kernel |
| `monet` | renderer — written **twice** |
| `michelangelo` | procedural geometry library |

Ten-plus libraries, three engine rewrites, zero shipped games. All are now dead code with no external users.

**The sharpest fact is that the bespoke runtime's central promise was never collected.** Kay's entire tax — a `Compact` constraint on every type in the simulation, generated code in every module, nightly-only compiler features, and a codebase contributors could not enter — was paid to buy transparent multi-core parallelism. The README's `- [ ] multiple cores` checkbox is **still unticked**, and the runtime is `Rc`/`RefCell` throughout. He bought distribution across **machines**, which he never needed, and never got parallelism across **cores**, which he did.

That is the characteristic shape of the failure and it generalises: bespoke infrastructure charges its tax **immediately and continuously**, and pays its benefit **conditionally and later**. The tax is certain, the benefit is a bet, and the bet is placed at the moment you understand the problem least. Note also that the tax was paid on *every* line of simulation code written afterwards, which is why it compounds while the benefit waits.

**8,100 GitHub stars produced approximately zero engineering help**, because *"the idiosyncrasy of the codebase made it hard for others to contribute more than housekeeping."* Popularity does not convert into contribution when contributing means first learning a private runtime, a private allocator, and a private build step. Bespoke infrastructure spends optionality about who else can ever touch the project — including, on a multi-year solo project with gaps of weeks, the version of yourself who comes back cold.

And then the ending, which is the part worth re-reading. He correctly diagnosed his bottleneck as *"the friction within and between [my] tools"*, chose to fix the tools rather than the game, and discovered that the tools were a better business. **Citybound was outcompeted by its own yak-shave.** The diagnosis was right. The response was to escalate the thing being diagnosed.

## The test to apply

> **Is this thing the game, or is it scaffolding around the game?**

Scaffolding is bought. The game is built.

**One exception is already taken, deliberately:** the simulation core itself is bespoke — typed tables with generational handles, a deterministic Tick, integer maths, the Rule interpreter. That passes the test. A general-purpose ECS or engine runtime cannot promise bit-exact reproducibility or total control over iteration order, and those properties *are* the product here: they are what make replay, State Hash divergence bisection, and save-as-input-log possible. See [`0004`](0004-typed-tables-over-ecs.md).

The test can be applied dishonestly — with enough motivation, anything can be argued to be "the game". Two guards against that:

1. **Name the specific property no library provides**, in one sentence, in writing. "Nothing quite fits" is not such a property.
2. **The scope must be bounded and shrinking.** A component with a fixed surface is a component. A component that keeps acquiring features is becoming a platform, and `monet` being written twice is what that looks like from the inside.

## Rejected

**"Writing it myself will be faster than learning the library."** For the first week, usually true. The comparison being made is writing versus learning; the comparison that matters is writing versus learning, *plus* maintaining, debugging, documenting, and absorbing the years of edge cases the library already ate.

**"Nothing off the shelf fits exactly."** Nothing ever does. Bending our requirements to fit an adequate library is nearly always cheaper than bending a library to fit our requirements, and far cheaper than owning one. [`0001`](0001-godot-and-csharp.md) is the live example: `MultiMeshInstance3D` shares one AABB across all instances, which does not fit — so we chunk the city. We do not write a renderer.

**"It will be fun, and I will learn a lot."** Both true, and neither disputed. This ADR does not deny the value of building a routing protocol or an allocator from scratch; it declines to charge that value to this game's budget. That is a different project, and it should be honest about being one.

## Consequences

- **Off the shelf means fewer things we own, not more things we depend on.** Prefer boring, widely used, stable libraries with permissive licences; prefer the standard library to a dependency; vendoring a small single-purpose library is fine and sometimes better than taking a large one.
- **Some performance is left on the table, on purpose.** A general library will lose to a specialist one somewhere. That is repaid with a profile showing where, not pre-empted with a rewrite.
- **Godot's warts become our warts** ([`0001`](0001-godot-and-csharp.md)), and the escape hatch when they bite is GDExtension for the simulation core — not a custom engine. The escape hatch exists precisely so that "the engine is limiting us" never becomes an argument for writing one.
- **A bespoke component requires a written exception in this file**, naming the property no library provides and the condition under which the component would be deleted. An exception that cannot be written down is not an exception, it is an urge.
- **The Ruleset's TOML format is this decision in miniature.** A purpose-built DSL would be more readable and is parked in `docs/deferred.md` for exactly this reason; TOML requires no parser, no error-message engineering, and no maintenance, and it satisfies [`0015`](0015-all-tuning-data-is-hot-reloadable.md) today.

## What would trigger revisiting

- **A specific, profiled bottleneck a specific dependency is provably causing** — with a benchmark, and scoped to replacing that one component behind its existing interface. Never a general sense that the stack is heavy.
- **A dependency dying** — unmaintained, relicensed, or incompatible with a .NET version we need. The options are then fork (cheap, bounded) or replace with another library, in that order. Writing from scratch is third of three, not first of one.
- **A genuine second candidate for "this thing is the game."** [`0004`](0004-typed-tables-over-ecs.md) is the precedent and this list should stay very short forever. If it reaches three or four entries, the test is being applied dishonestly and the right response is to re-read the table at the top of this file.
