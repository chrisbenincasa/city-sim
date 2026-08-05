# The simulation is deterministic integer arithmetic, from commit #1

**The simulation core computes in integers and Q16.16 fixed-point only, advances on a `u64` Tick counter, derives randomness by hashing rather than by drawing from a stream, and observes nothing — wall clock, camera, hash-map order — that did not arrive through the Input Log.** A CI lint enforces the banned constructs listed below. This is the first thing built: before rendering, before the Ruleset, before any entity exists.

## Why

Determinism is not a feature that can be added later. It is a property of every line of arithmetic in the codebase, and one stray `double` in a growth rule silently invalidates every replay, every State Hash comparison, and every bug report that depends on them. A project that retrofits determinism ends up auditing the entire simulation anyway — which is the work of writing it correctly the first time, plus the work of finding where it was written wrongly. There is no partial credit.

The payoff is disproportionate for a solo developer, because the bugs that will actually cost us weeks are emergent, intermittent, and surface after hours of play. Those are exactly the bugs that are unreproducible without determinism and mechanically reproducible with it.

**Integer and fixed-point throughout.** Money, Goods, population, Ticks and Tile coordinates are naturally integral. The only genuinely continuous quantity in the design is a sub-Tile position, which is Q16.16. Integer square root remains available where a distance is genuinely needed, since it is algebraic and exactly computable.

> **This ADR claimed *"zero transcendental functions — no `sin`, `exp` or `log` anywhere in the core"* and that is false.** It is repeated in `02 §1` and `05 §4`, and both are corrected. The design has needed `exp` since before this ADR was written:
>
> - **`02 §5.4`'s choice model is a softmax** — `P(i) = exp(μ·V_i) / Σ exp(μ·V_j)` — and `adr/0032` routes *every* Provider List choice through it: housing, jobs, schools, shops, modes. It is the most-executed decision function in the project, roughly 20 evaluations per Household per cycle.
> - **`02 §2.4`'s noise falloff is logarithmic** over 50–300 m, evaluated as a point-of-use query during decision scoring (`adr/0034`).
>
> **The contradiction was hard, not cosmetic: this ADR bans `Math.*`, so as written the choice model could not legally be implemented at all.** The resolution is the one this ADR's own revisit trigger already prescribes — *a tabulated or fixed-point implementation with defined rounding* — promoted from a contingency to a **required component of the core**, built alongside the fixed-point library in Consequences. `sin` genuinely is not needed.
>
> **And precision here is behavioural rather than cosmetic**, which is why it belongs in this ADR rather than in a maths utility. `adr/0005` and `adr/0017` both make **μ, the logit scale parameter, the thing that prevents stampedes**. A table whose resolution perturbs the effective μ is a hidden global constant tuning a system-wide outcome — the object `00-vision` pillar 1 exists to forbid. The table's resolution is therefore a **stated** figure, validated against the herding behaviour `0005` describes, not an implementation detail chosen by whoever writes it.

**Never iterate a `Dictionary` or `HashSet` in simulation code.** .NET randomises `string.GetHashCode()` per process, so iteration order over a `Dictionary<string,T>` differs between two runs of *the same binary on the same machine*. This is not an edge case, it is the documented default, and it produces divergence with no visible cause. The remedy is structural rather than disciplinary: dense arrays indexed by generational handles, per [`0004`](0004-typed-tables-over-ecs.md), which have exactly one possible iteration order. Hash maps may be built and looked up; they may not be walked.

**Counter-based RNG.** Every draw is `hash(world_seed, entity_id, tick, purpose_tag)`, not a read from a shared mutable stream. A shared stream couples every draw to every prior draw, so results depend on evaluation order and any reordering of updates changes the whole simulation. Hashing makes a draw a pure function of its coordinates. The consequence worth buying now is that agent updates can later be parallelised in any order, across any number of threads, with **bit-identical results and zero coordination**. That is nearly free today and pre-emptively eliminates the nastiest class of parallel-determinism bug. `System.Random` is banned outright: its algorithm changed in .NET 6 and Microsoft has never documented it as stable across versions.

### The hash function is normative, and it is a format rather than an implementation detail

`hash(world_seed, entity_id, tick, purpose_tag)` appeared in four documents and **the function was never named**. That is not a documentation gap: an Input Log plus a world seed reproduces a run only if the hash is **bit-identical**, so changing it invalidates every stored log, every State Hash baseline, and every bug report in flight. **It is the same category of change as a save-format change**, and `05 §7`'s version machinery applies to it.

The function is **SplitMix64's finalizer**, written out literally so it is re-implementable from this document alone — which matters because saves and Input Logs outlive binaries, whatever [`0036`](0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) says about the port never happening. All arithmetic is `u64` and **deliberately wrapping**; this is one of the two named exceptions to the overflow policy below.

```
mix(z):                                        // SplitMix64 finalizer
    z ^= z >> 30 ;  z *= 0xBF58476D1CE4E5B9
    z ^= z >> 27 ;  z *= 0x94D049BB133111EB
    z ^= z >> 31
    return z

GOLDEN = 0x9E3779B97F4A7C15

world_key(seed):                               // once, at world creation
    return mix(seed + GOLDEN)

draw(world_key, entity, tick, purpose):
    h = mix(world_key + GOLDEN + entity)
    h = mix(h         + GOLDEN + tick)
    h = mix(h         + GOLDEN + purpose)
    return h
```

> **Amended 2026-08-04, before the first Input Log existed.** As originally written the first round
> was `h = mix(seed + GOLDEN + entity)`, which **added two externally chosen coordinates together, so
> only their sum reached the hash.** `draw(seed=1000, entity=1)` and `draw(seed=1001, entity=0)` were
> bit-identical for every tick and every purpose: **rerolling the world seed by one produced the same
> world shifted by one entity rather than a new world.** Within a single world it was harmless, the
> seed being constant and `seed + entity` therefore injective in entity; the damage was across worlds.
> It was found by writing this function's first known-answer vectors, when two vectors differing only
> in which coordinate held the `1` produced the same value.
>
> **The fix folds the seed in its own round**, restoring four coordinates to four. It is free on the
> hot path: `world_key` depends on nothing that varies during a run, so it is computed once at world
> creation and stored, leaving `draw` at the same three mixes it always had.
>
> **What this does and does not buy, stated precisely.** It does not make the coordinates
> algebraically independent — two worlds still satisfy `draw_A(e) == draw_B(e + d)` for the constant
> `d = world_key(A) − world_key(B)`. What changes is that `d` is now a pseudorandom 64-bit value
> instead of the seed difference itself. With entity ids bounded around 2²⁴, the chance that two given
> worlds overlap anywhere is about **2⁻³⁹**, against a certainty before. The tick and purpose rounds
> never had this defect, because `h` is a hash output by the time they fold in.

It is chosen for four properties, in order: it is a **bijection**, so it has no collisions over the counter domain; it is **published and stable**, unlike `System.Random`; it has **no dependency**, which this ADR's own revisit trigger requires of anything in the core; and it is ~15 instructions, which is cheap enough not to matter beside the decision it seeds. *If profiling ever demands fewer rounds, that is a format change requiring a deliberate re-baseline, not a free optimisation.*

**`purpose_tag` is a compile-time integer constant drawn from one central enum, never a string.** A string would need string hashing, which rule 2 of `02 §8` bans outright; and a mistyped string collides silently, which `05 §4` already names as a failure that *"correlates two decisions invisibly."* **Uniqueness over that enum is a build-time check**, which costs nothing and is the only way this failure is ever caught.

### The State Hash's coverage was the oracle's blind spot

This ADR said *"hash the world every N Ticks"* and *"hash values, never identity."* It never said **which** values, and that omission has a specific consequence:

> **A field that is saved but not hashed is invisible to every tool in the project.** Two runs diverge on it; the hashes agree; replay reports success; the save/reload test passes, because the field *is* saved. The oracle certifies a divergence it cannot see.

Same species as the overflow finding below — a defect class the primary oracle is structurally blind to — and worse, because it silently degrades the tool everything else depends on. **The fix is structural rather than a test**, extending [`0004`](0004-typed-tables-over-ecs.md)'s *"the layout is the file format"* by one step:

> **Every field in a table is declared once as either `(saved AND hashed)` or `(derived AND rebuilt)`. The save serialiser and the State Hash are generated from that one declaration.**

A field then cannot be in one and not the other, the save/reload test transitively covers hash coverage, and two failure modes collapse into one already-tested property. **Composition order falls out for free** — tables in declaration order, arrays in index order, folded through the same `mix` above.

**What this cost:** it is a step onto the slope `0004` refused — *"a general table framework of our own… is the yak-shave version of this decision."* It is taken deliberately and kept small: **a per-field flag, not a framework**, with no reflection, no codegen required, and no component-like API. The justification is that the alternative is a defect class with no detector at all. See the note in `0004`.

**Time is a `u64` Tick counter and the library never sees a clock.** The API is `step(inputs)`; the host decides when to call it. Fast-forward, headless testing and replay then fall out for free rather than being built. `FAST ITERATION`

**Two more constructs are specified rather than banned, because C# defines them and the definition is surprising.**

**Integer division truncates toward zero**, so `-7/2 == -3` while `7/2 == 3`. That is deterministic and **asymmetric**: a quantity split among agents rounds differently either side of zero, producing a directional bias at every zero crossing. Division in the core therefore goes through a **stated rounding helper** — floor division by default — rather than through the `/` operator, and the choice is made once instead of at each site.

**Shift counts are masked**: `x << 32` on a 32-bit value is silently `x << 0`. `0036` counts fully-specified integer semantics as a point in C#'s favour, and this is the case where *specified* and *safe* diverge. Shifts by a non-constant amount go through a helper that asserts the range.

| Banned in the simulation core | Reason |
|---|---|
| `float`, `double`, `Math.*` — **in any arithmetic, not merely in stored state** | Rounding and intrinsic selection vary with JIT, target and optimisation level. **A float temporary whose result is cast to an integer is exactly as non-deterministic as a stored one** — x87 80-bit intermediates, FMA contraction, differing SIMD widths. `02 §8` rule 1 and `05 §4` both said *"in simulation state"* and both are corrected |
| `System.Random` | Algorithm changed in .NET 6; never documented as stable |
| `DateTime`, `Stopwatch`, `Environment.TickCount` | Wall-clock time is not an Input Log entry |
| Iterating `Dictionary` / `HashSet` | Per-process `string.GetHashCode()` randomisation |
| `Guid.NewGuid()`, default `object.GetHashCode()` | Identity that differs between runs |
| Parallel loops accumulating into shared state | Order-dependent results |

## Overflow, and why it needs a policy rather than a test

**Overflow is the one class of bug this ADR's own oracle cannot see.** Everything above rests on the State Hash: two runs of the same Input Log must agree, and where they diverge the first differing hash names the Tick. An overflow wraps *identically* in both runs. The hashes agree, the oracle reports success, and the city is wrong. Replay, divergence-bisection and the save/reload test are all blind here.

Determinism does not protect against overflow. It makes overflow **reproducible**, which is not the same thing.

### The arithmetic, before the policy

The instinct is to make everything `checked`. **The arithmetic says that charges the hot path to protect the column where the probability is zero.**

```
household income flow       ~10⁹   per income period (400k Households, 00-vision's §2,400)
total money stock in city   ~10¹⁰  savings + business balances + treasury
i64 max                      9.2 × 10¹⁸     →  headroom ~10⁹ ×
i32 max                      2.1 × 10⁹      →  already exceeded at target population
```

Money is conserved ([`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md)) and enters only through the gate, so overflowing i64 would require the money supply to grow a **billionfold**. It is not a long game; it is unreachable. **The width choice is decisive and the check is worthless** — and the same holds for every integer quantity in the design, since counts are bounded by 10⁶ against i32's 2.1×10⁹.

**The real hazard is not width, it is Q16.16's *range*** — ±32,768, which is small, and which a product of two moderate quantities exceeds immediately. That is a **format** problem, and widening does not dissolve it the way it dissolves the integer problem: fixed×fixed multiplication inherently needs a 2N-bit intermediate, so Q32.32 in i64 requires an i128 intermediate (`Math.BigMul` plus recombination, ~2–3× the multiply). Roughly the price of the `checked` branch it would replace.

**But the stated use of Q16.16 never multiplies at all.** `05 §3` names one: sub-Tile positions, updated as `position += velocity × ticks` — Q16.16 by *integer*, no intermediate widening, no shift. Positions are bounded by the map at 4096 against a ±32,768 format: 8× headroom, provably, forever.

Fixed×fixed appears in exactly one place, and it is the place with powers in it: **IDM car-following** (`(v/v₀)⁴`, `(s*/s)²`) and the **VDF** (`t₀(1 + α(v/c)^β)`, β ≈ 4). Squaring an absolute quantity blows any fixed format — two 200-Tile distances multiplied is 40,000.

### The rule, and it is prevention rather than detection

Every one of those terms is `v/v₀`, `s*/s`, `v/c` — **a ratio**. A ratio sits in roughly [0, 3] and its fourth power in [0, 81], against ±32,768. Three orders of magnitude of headroom, with no widening and no check.

> **Fixed-point multiplication operates on dimensionless ratios, never on absolute quantities.**

This is the project's oldest through-line — ***ratios are real; units are invented*** — arriving as an **arithmetic safety property** rather than a modelling one. It already carries `TICKS_PER_DAY` ([`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md)) and the Hinterland's domain-unit authoring ([`0023`](0023-immigration-arrives-through-the-gate.md)); this is its fifth instance and the first where it prevents a defect rather than clarifying a design.

### Typed quantities, which make the rule structural

A convention enforced by review fails silently, and this ADR's whole posture is that such rules are enforced mechanically. No analyser can tell a ratio from an absolute, because **the units are not in the type** — so they are put there:

> **Quantities in `Borough.Core` are distinct value types — `Money`, `Ticks`, `Tiles`, `Ratio` — as `readonly record struct` wrappers over their integer representation.**

This is [`0004`](0004-typed-tables-over-ecs.md)'s own argument moved from identities to quantities. It gave typed handles so *"a Citizen handle cannot index the Building table — a class of bug an untyped `Entity` id leaves open by construction."* The same move gives:

- `Tiles × Tiles` **does not compile**; `Ratio × Ratio` does. The ratio rule becomes structural rather than disciplinary.
- Adding a `Tick` to a `Tile` stops compiling.
- The non-negative-Money invariant lives in one place instead of at every call site.
- `balance - cost` returns something the caller must handle, so underflow is answered once at the type.
- **Zero runtime cost** — they erase to their underlying integer.

**This is also why Money stays signed.** The design genuinely never holds negative money — a destitute Household departs rather than borrowing, a bankrupt Business is a distinct diagnosis from a starved one ([#15](../../plans/0002-open-questions.md)), the treasury *empties* and its Rules **wait** ([`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md) §3a), and borrowing is an explicit player action that adds money ([`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) note). But **stocks are non-negative while every delta, flow and balance-of-payments figure is inherently signed**, so an unsigned type would protect the column with no bugs in it and arm the one with all of them: `balance - cost >= 0` is correct signed and catastrophic unsigned. The invariant belongs on the type's *operations*, not on its signedness.

### The policy

| | |
|---|---|
| **Integers** | **Wide by default.** Money and accumulators **i64**; counts and Bin levels **i32**; positions **i32** in sub-Tile units; Ticks **u64**. Width is free at runtime; checks are not, and width closes the entire integer column |
| **Q16.16** | **Positions only**, where the bound is the map and the multiply is by an integer. Provably safe |
| **Fixed × fixed** | **Dimensionless ratios only.** The primary mechanism |
| **Q32.32** | Available for a genuine product-of-absolutes, decided **per site with a written reason**, never as a default |
| **`checked`** | **Inside the fixed-point library only.** It now guards a *stated rule* rather than an unknown, which is the same posture as the CI lints below: the rule is the design, the check is what stops it being violated by accident. Ambient arithmetic in the core stays unchecked |
| **Detection** | **Conservation invariants are the money detector** and `05 §1` already lists them. They must run in the headless suite, which is where millions of Ticks actually elapse |
| **Prevention** | **No quantity accumulates without bound** — [`0006`](0006-no-collection-grows-with-elapsed-time.md) applied to magnitudes rather than to collections. Concretely, `0035`'s accumulated wear **caps at a stated multiple of design life**, beyond which more wear changes nothing. That is a design answer, not an arithmetic one |
| **Deferred** | **The fixed-point range sizing**, explicitly, to [`03 §5`](../03-agent-architecture.md). IDM and the VDF are the only fixed×fixed sites and the traffic model has never been grilled; sizing a format for maths nobody has argued would be inventing a number. Nothing in Phase 1 is blocked, because `05 §3`'s stated use is provably safe |

**One number is still asserted rather than measured:** that `checked` inside the fixed-point library is cheap. **S4's K1 gains a checked/unchecked pair** to settle it — one extra kernel variant, and it is the only claim here without arithmetic behind it.

## What this buys

Three tools that require determinism and cannot be approximated without it:

- **Save-as-Input-Log.** A session is `(world seed, configuration, inputs per Tick)`. A ten-hour session is kilobytes, because a player issues a handful of commands a minute, not a megabyte a second. Bug reports become *"attach your log"*, and the reported bug then reproduces exactly on our machine.
- **The State Hash as a bug oracle.** Hash the world every N Ticks. Two runs of the same Input Log must produce identical hash sequences, and when they diverge the first differing hash identifies the exact Tick the bug entered — turning "the economy goes wrong somewhere around hour three" into a single reproducible step.
- **The Factorio save/reload test.** Run N Ticks, save, reload, run M more. Separately run N+M Ticks in one process. Compare State Hashes. Any mismatch is *unsaved state* — a field the save format forgot. This class of bug is otherwise nearly impossible to find, because it manifests only as a game that is subtly different after loading, hours later and far from the cause.

## The camera-LOD problem, and how it disappeared

An earlier draft identified camera-driven level of detail as the most likely way determinism would be destroyed by accident: promoting whichever agents are near the camera makes simulation state depend on render state, so two players watching the same replay from different angles diverge into different cities. The proposed fix was to make the LOD focus point a recorded input — the camera *drives* the value, the simulation only ever reads what was recorded.

[`0007`](0007-stress-driven-simulation-detail.md) removed the problem rather than solving it. Fidelity is now derived from simulation state — Segment Stress — so there is no focus point, nothing to record, and no coupling to break. A replay reproduces the same Microscopic Segments without the Input Log ever mentioning which ones they were.

The specific mitigation is obsolete; the rule it was an instance of is what matters and survives intact. **Nothing may enter simulation state except through the Input Log.** The camera was the tempting case, and it will not be the last one — any future feature that wants to read render state, host state, or the wall clock is refused on exactly these grounds.

## Consequences

- **A lint runs in CI** over the simulation core from the first commit, failing the build on any banned construct. Added later, it becomes an audit; added first, it is a guardrail that never lets the debt accumulate.
- **A small, boring fixed-point library must exist** — multiply, divide, lerp, **and tabulated `exp` and `log`** — written once and tested exhaustively. Q16.16 multiplication needs a 64-bit intermediate, and overflow is a real hazard rather than a theoretical one.
- **The choice *algorithm* is deliberately not fixed here, and that is a scope decision rather than an omission.** `02 §5.4`'s **softmax stands as the recorded algorithm**. Two alternatives are noted as available, and the trigger for reaching for either is **how the housing market feels in play** — not a profile:
    - **Gumbel-max.** `argmax_i(μ·V_i + G_i)` with `G_i` drawn from a fixed Gumbel quantile table is *exactly* the same distribution as the softmax, needs no `exp` on the hot path, and is cheaper — one table lookup, one add, one compare per alternative. The uniforms come free from counter-based RNG above, and `adr/0027`'s Taste is another integer added to the same sum.
    - **Tabulated softmax** at a coarser or finer resolution, tuning where the herding threshold sits.

    **These are swappable because the interface is a scored candidate list in and one choice out.** What is *not* swappable, and therefore lives here, is the arithmetic substrate underneath all three.
- **Swapping the choice algorithm is hash-breaking but distributionally neutral, and that is a third case `05 §4`'s rule does not currently name.** Gumbel-max and softmax produce the same *distribution* and different *realisations*, so the State Hash changes while nothing about the design does. It is therefore not a free optimisation — every stored Input Log replay and every State Hash baseline is invalidated and must be re-taken. `05 §7` already covers the general form: *what genuinely defeats replay is a changed binary.* **Safe to do, deliberately, with a re-baseline — not silently.**
- **Rendering is exempt, and must be.** Interpolation between Past and Future, camera maths and mesh generation are all floating point. The boundary is one-directional: the host reads simulation state and never writes back.
- **The State Hash must hash values, never identity.** Memory addresses, object hash codes and allocation order must not reach it, or the oracle produces false divergence and stops being believed.
- **Determinism is tested continuously, not asserted.** CI replays a stored Input Log twice and runs the save/reload test. A determinism regression discovered by hand, months later, is a bisect over the entire history.
- **Multiplayer remains technically possible** without anything in the design being shaped by it. That is a side effect, not a goal.

## What would trigger revisiting

- **Not performance.** Integer arithmetic is expected to be faster than floating point here, not slower. If fixed-point ever did become the bottleneck, the answer is a different representation — Q32.32, or plain scaled integers — never a float.
- ~~**A feature that genuinely needs transcendental maths.**~~ **Already true when this was written — two instances, and the first is the game's central decision function.** The prescription survives and is now a standing requirement rather than a contingency: **a tabulated fixed-point implementation with defined rounding, in the core, from the start.** What is retired is the sizing — *"one instance is not [worth re-examining]"* was written on the assumption that transcendentals would be peripheral, and they are not.

    > **Third ADR in a row whose revisit trigger was written against its author's mental model rather than against the corpus.** `0002`'s had already fired; `0004`'s foreclosed the only ground its real defect could be attacked on; this one mis-sized the case it correctly anticipated. **A trigger must be checked against the documents that already exist, on the day it is written.**
- **A third-party dependency entering the simulation core.** Any library is a determinism liability, because its internal iteration order and numeric behaviour are outside our control and can change on a version bump. Dependencies in the core need this ADR cited against them explicitly.
