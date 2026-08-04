# The core's language is a separate decision, and it is C#

**`Borough.Core` is written in C# on .NET. This is a decision in its own right, argued on its own merits, and it is *not* a consequence of [`0001`](0001-godot-and-csharp.md)'s choice of Godot as the host.** The two were welded together in one ADR whose entire argument was about the shell; this one separates them and settles the half that was never argued.

The finding that decides it:

> **All three serious candidates — C#, Rust, C++ — converge on the same code.** A deterministic integer simulation over struct-of-arrays tables addressed by handles looks nearly identical in all three. Since the code converges, the language is chosen on the *surrounding* factors — shell language, tooling, enforcement mechanism, developer fluency, iteration speed — and C# wins all five.

## Why this is a separate ADR at all

`0001` is titled *"Godot 4.7 with C#"* and its **Why** section defends exactly one thing. `Control` nodes for data-dense UI, `MultiMeshInstance3D`, `RenderingServer` as an escape hatch, MIT licence, no vendor risk — every argument is about the **shell**. So is every entry in its Rejected list. Not one line addresses `Borough.Core`, and yet three decisions came out of it: the host, the core's language, and the core's *runtime* — the last never named at all.

**This is the corpus's own failure mode one level up.** [`0034`](0034-fields-are-sorted-by-source-geometry.md) established that *a constant welded to two decisions is governed by whichever of them is louder*. Here it is a **decision** welded to two others. The loud one had an argument, a rejected list and four revisit triggers; the quiet ones rode along on it and now sit under `05 §3`, `05 §4`, and the threading policy.

**And [`05 §4`](../05-technical-architecture.md)'s State Hash rule is the test that separates them.** The host is hash-preserving by construction — that is [`0002`](0002-simulation-is-an-engine-agnostic-library.md)'s entire job. The core's language is not: integer overflow semantics, shift behaviour on negative operands, division rounding, and the hash function behind `hash(seed, entity, tick, purpose)` are all language-defined, and every one appears in the determinism rules. Under the project's own rule, `0001` is an optimisation-class decision and this one is a **design** decision. They cannot share an argument.

## Why C#

### The constraints on the core are not C#'s constraints

The obvious objection is that the core bans most of what a C# developer reaches for, so C# is a square peg. The objection dissolves on inspection: **not one of the bans exists because of C#, and Rust would relax none of them.**

| Rule | Why it exists | Relaxed by Rust? |
|---|---|---|
| No `float`/`double` | [`0003`](0003-deterministic-integer-simulation.md) — cross-platform, cross-JIT reproducibility | **No.** Same problem, same ban |
| No `Dictionary`/`HashSet` iteration | Randomised hashing → non-reproducible order | **No.** Rust's `HashMap` is randomised by design |
| No stdlib RNG | Undocumented algorithm stability | **No.** Same discipline |
| Struct-of-arrays by hand | Cache behaviour; [`0004`](0004-typed-tables-over-ecs.md) rejects a table framework outright | **No.** Identical, and the codegen version was already rejected |
| Handles, never references | Determinism and stable identity | **No — Rust *forces* it.** Index handles are the standard Rust answer for graph-shaped state |
| Intrusive index lists, no per-entity collections | No allocation, deterministic iteration | **No.** This is idiomatic Rust, not a C# workaround |

The style is what a deterministic integer simulation looks like in **any** language. It is not C# fighting its grain.

**The lint audit is the concrete measure of grain-fighting.** Of the six CI rules this project enforces (`05 §4` plus the sixth added below), **exactly one is C#-specific** — *no reference types in simulation state*. The other five are needed identically in Rust.

**Nor is it true that none of C#'s features are used.** The core uses C#'s systems dialect, which exists for precisely this work: `Span<T>`, `ref` returns and locals, `readonly struct`, `in` parameters, `stackalloc`, the `unmanaged` generic constraint. It also uses real generics for **typed handles**, so a `Handle<Citizen>` cannot index the Building table — a class of bug `0004` names specifically and which an untyped id leaves open.

### The arithmetic says the performance question is not close

Per-Tick cost at the 1M target, sized against `05`'s own budget (order-of-magnitude estimates, to be confirmed by spike **S4** and then **S0**):

| Work | Scales with | Per Tick at 1M |
|---|---|---|
| Event Wheel wakeups | *activity*, not population ([`0006`](0006-no-collection-grows-with-elapsed-time.md)) | ~10³ |
| Microscopic car-following | the **Microscopic Cap**, a fixed world constant (`03 §3.9`) | bounded by construction |
| Statistical travellers | nothing — arrival is a wheel entry | ~0 |
| Map Layer diffusion | 128×128 Cells, separable, integer, staggered over 32–64 Ticks | ~10³ amortised |
| Sweep Rules | population, but amortised and staggered | ~10² comparisons |

That is on the order of **10⁴ units of work against a 15.6 ms budget** at 4× speed — roughly an order of magnitude of headroom. The honest language penalty for C# against Rust or C++ on tight integer loops over arrays is **1.2–2×**, not 10×. **Measuring a 5× separation is theatre**, which is why this ADR is decided by argument with a measured tripwire rather than by a bake-off.

**Two things make the headroom robust rather than lucky.** The Microscopic Cap is what keeps the only continuously-scaling cost bounded as the city grows — the strongest argument for the Cap anyone has made, and it is not currently in `03 §3.9`. And `02 §1.2` mandates wall-clock dilation rather than Tick skipping, so an overrun degrades the **speed ladder** rather than breaking anything. The failure mode of "the language was 2× slower than hoped" is *"4× feels like 2×"*, which ships.

### C# is the better determinism substrate, and nobody had written that down

C# **fully specifies** integer overflow (wraps), shift masking, and division truncation toward zero. C++ makes signed overflow undefined behaviour, so identical source can produce different results at different optimisation levels — a determinism nightmare in a project whose primary oracle is a State Hash. Rust is as good as C#; C++ is materially worse. Since `0003` bans floats outright, the hardest cross-platform reproducibility problem is already gone, and on what remains **C# ties the best candidate and beats the third**.

### The GC is a discipline risk, not a language risk — and the risk is not where it looks

The GC does not scan the interior of arrays of unmanaged structs; it traces the *reference graph*, which under `0004` is a few hundred array references rather than a million objects. All the tables live on the LOH, which is where long-lived non-compacting arrays want to be. The design already dodges the main failure mode.

**But the tables were never the risk.** The risk is the three derived structures the design grew around them, all variable-length, all per-entity:

| Structure | Where | Count at 1M |
|---|---|---|
| Wait list per Bin | [`0033`](0033-two-rule-families-scheduled-and-swept.md), `05 §3` | one per Bin — **hundreds of thousands** |
| Cached Parking Shed per Building | `05 §3`, [`0009`](0009-parking-is-modelled-supply-never-search.md) | one per Building — ~10⁵ |
| Event Wheel buckets | `05 §9` | 8192, churning every Tick |

As `List<T>` these are on the order of **a million long-lived managed objects**, every one traced on every full collection — the exact failure the layout was supposed to avoid, relocated. `05 §3` said these structures *"must be named or they will be discovered."* They were named; their **representation** was not. The rule in Consequences below closes that.

### What C# buys that the alternatives do not

- **One language across the boundary.** `0001` puts ~60% of the work in the shell, and Godot's C# support is **first-party**. A Rust core means two toolchains, and `gdext` is community-maintained — reintroducing at the seam exactly the vendor risk `0001` bought Godot to avoid.
- **The enforcement mechanism this whole plan depends on.** Roslyn analysers are first-party and cheap to write. Custom lints in Rust mean `dylint` or a custom driver. There is a real irony here worth stating: **C# makes cheap to enforce the discipline that Rust would make unnecessary** — and given that five of the six lints are needed in Rust anyway, cheap enforcement is worth more than one structural guarantee.
- **`BenchmarkDotNet`, xUnit, `dotnet-trace`** — `05 §1` already assumes all three.
- **Fast edit–compile–run**, which is `FAST ITERATION` and the failure that took Citybound.
- **Developer fluency.** Java transfers the language; **C++ transfers the idiom**, and the idiom is what the kernel is made of. Learning Rust concurrently would spend the project's scarcest resource on the wrong problem.

### The constrained zone is small, stable, and not where the work is

The objection *"this will be arcane and easy to break"* describes code that churns. This kernel is the opposite:

| Zone | Style | Churn |
|---|---|---|
| Hot kernel — tables, Event Wheel, Lane queues, Rule execution, wait lists | fully constrained | **write once, then rarely.** Order of 10³ lines |
| Rest of `Core` — Ruleset loading, save serialisation, world generation, `Evidence` assembly | ordinary C#, outside the Tick | moderate |
| `Tests`, `Headless` | ordinary C# | high |
| `Godot` — UI, panels, overlays, streaming | full C# and full Godot | highest |

And [`0015`](0015-all-tuning-data-is-hot-reloadable.md) puts all balance content in data rather than code, so **the loop iterated on daily never touches the kernel at all.**

## Rejected

**Godot shell + Rust core via GDExtension.** The serious alternative, and `0001` never actually considered it — it rejected **Bevy** and the rejection got filed under *Rust*. Testing `0001`'s four anti-Bevy arguments against this configuration: *no editor* — Godot is still the editor; *immature UI toolkit* — `Control` nodes, untouched; *pre-1.0 migration tax* — that was Bevy's, not Rust's, stable since 2015; *long compile times* — the only survivor, and much weaker against a dependency-light core than against Bevy's dependency graph, and further weakened because `0015` makes the balance loop recompile nothing. **Three of the four dissolved**, which is the third time this corpus has found that shape.

Rejected anyway, on: two toolchains for one developer over five years; `gdext` as community-maintained vendor risk where Godot's C# is first-party; an FFI wall landing on `visible_agents`, which `0002` already names as the boundary's most likely failure point and which is called per frame; and developer fluency, which `0001` itself names as the binding condition (*"the Rust fluency is the binding one"*) and which is not met.

**C++.** Worse than both on determinism — undefined signed overflow is disqualifying for a State-Hash-oracle project — and worse than C# on tooling, iteration speed and shell integration. Its only advantage over C# is the GC, which Rust also provides without the determinism cost.

**Leaving the decision welded inside `0001`.** Rejected because an unargued yes and an argued yes are different objects when the thing downstream is a five-year solo build, and because `0001`'s revisit triggers cannot fire on a decision `0001` does not discuss.

## Consequences

- **`0001` is amended to drop C# from its claim.** It now decides the host only. Its GDExtension escape hatch survives, and is owned by this ADR.
- **A sixth CI lint: no reference types in `Borough.Core` simulation state.** A Roslyn analyser asserting that every table row type and every derived structure satisfies the `unmanaged` constraint. This single check enforces the GC property, the determinism property and the portability property at once — a fourth instance of *the honest model and the cheap one keep coinciding*.
- **Every variable-length collection in the core is an intrusive index list** — a head index on the owner, a `next` index on the element, both in flat arrays. Never a per-entity collection object. This is what makes the lint above satisfiable for wait lists, Parking Sheds and wheel buckets, it gives `0033`'s round-robin drain its deterministic order for free, and it survives a port unchanged.
- **The exceptions to that rule are owed and not yet enumerated.** The likely candidates are the **Ruleset interpreter** — loaded from data at runtime, therefore variable-shaped, and hot-reloadable per `0015` — and the **`Evidence` surface** (`02 §9`), which is cold and UI-facing. Both look legitimately excepted, but [`0031`](0031-one-resource-abstraction-and-depth-not-count.md) found its real axis by enumerating exceptions rather than granting them one at a time, so these are to be listed deliberately before the analyser ships.
- **Spike S4 is created** — a six-kernel microbenchmark measuring the machine's response to the shapes this design makes, at target row counts, with no simulation in it. It runs **before S0**. It is a tripwire confirming this argument, not a gate replacing it. **S4 has since run and no row fired** ([`spike-results`](../spike-results.md)): every shape the design commits to lands between **1.02× and 1.42×** of its hand-computed ideal, and K6's GC tail is recorded against the revisit trigger below. Two of its findings bear on this ADR beyond the trigger — `checked` costs **27%** rather than the "cheap" [`0003`](0003-deterministic-integer-simulation.md) asserted without arithmetic, and the intrusive-index-list rule in the consequence above is what keeps the GC tracing a few hundred array references instead of 1.56M objects, which K6 measured as the difference between 6.984 ms and 100.200 ms on the desktop, and 5.626 ms against 206.977 ms on the M4 Pro.
- **The escape hatch should be assumed unused.** A solo developer three years in with a large C# core will not rewrite it in Rust. GDExtension is a genuine backstop and it is not a plan. The consequence is that investment goes into the discipline that makes C# succeed rather than into speculative portability — which costs nothing, because they are the same investment.
- **The standing caveat in `plans/0002` is discharged.** *"Treat any argument that reaches for a C#-specific fact as suspect"* was correct while this was unargued. It no longer applies.

## What would trigger revisiting

- **A sustained run hitching with the hot tables already pure unmanaged structs.** That is the discipline holding and the runtime hitching anyway, and it is the one outcome that genuinely flips this decision. It arrives **before the core is written**, which is the point.

  **Evaluated by S4's K6 on two machines, and it did not fire on either** ([`spike-results`](../spike-results.md)). Across **6,062,762 iterations** and all four `ServerGarbageCollection` × `ConcurrentGarbageCollection` configurations on each host, the unmanaged arm exceeded the 15.6 ms budget **zero** times; its worst single iteration was **6.984 ms** on the desktop and **5.626 ms** on the M4 Pro. The measurement has a positive control rather than being an absence of evidence: the counterfactual arm — the same data held as ~1.56M linked objects, taking identical churn — reached **100.200 ms** and **206.977 ms**, 6.4× and 13.3× past budget, with 18 over-budget iterations against the unmanaged arm's zero.

  **The margin widens on the faster machine, which is the opposite of the expected shape and is worth stating.** Churn is per iteration, so the M4 Pro's 2.4× faster iteration puts **2.5× more allocation pressure per second** through the same 1.56M-object graph. The intrusive-index-list rule is therefore worth *more* where the machine is quicker, not less — the arms separate by 13.7–36.8× there against 2.2–19.7× on the desktop.

  **The trigger is restated, because K6 showed that the statistic it originally named cannot detect the failure it describes.** It read *"a p99.9 pause beyond 15.6 ms"*. A ten-minute run is ~438,000 iterations and a gen2 stall is a handful of events; p99.9 discards the top 438 samples and p99.99 the top 44, so the run whose worst iteration was 100.200 ms read **2.462 ms at p99.9** — and in half the GC matrix p99.9 ranked the rejected design *above* the chosen one. **The second machine makes the point without needing the inversion**: there, p99.9 separates the two arms by at most **2.3% in either direction** while max separates them by up to **36.8×**. The statistic is blind on one host and anti-correlated on the other. **The trigger is now: any single Tick exceeding 15.6 ms during a 10-minute sustained run, with the over-budget count and the maximum both reported.** A rare, large, correlated pause is precisely what a high quantile smooths away, and a trigger that cannot fire is not protecting anything.
- **The Microscopic Cap landing high.** The headroom estimate above assumes a Cap in the low thousands of vehicles. The Cap's real value is still unset (`03 §3.9`; ledger #13). If the traffic model needs an order of magnitude more to be honest at 1M, the headroom becomes ~1× and the language returns to contention. **This decision is currently downstream of an unset constant**, which is stated here rather than discovered later.
- **The sixth lint being disabled or waived** — same shape as `0002`'s trigger on the boundary check, and for the same reason: it is the point at which this ADR stops describing the system.
- **Not general slowness.** A profile showing the core is slow is an invitation to look at ledger **#29** (the per-Tick full-state copy, which is language-independent), at the Cap, and at routing — in that order — before it is an argument about the language.
