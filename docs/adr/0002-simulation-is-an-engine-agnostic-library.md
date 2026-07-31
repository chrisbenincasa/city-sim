# The simulation is an engine-agnostic library

**`Borough.Core` is a pure class library holding zero engine types. It has no reference to Godot, no concept of a frame, and no concept of a camera. The engine is a rendering and UI shell that calls into it.**

Its public surface has **two flavours of query, split on the cadence of the caller**:

```
hot    step(inputs)                   advance exactly one Tick
       visible_agents(aabb, alpha)    interpolated transforms inside a box the host supplies
       layer_cells(aabb, layer)       overlay values, one per Cell
       chunk_aggregates(aabb)         cached per-Chunk population, pollution, land value, employment

cold   inspect_*(handle)              02 §9's Evidence requirements, per entity kind
       expand(aggregate)              an aggregate's constituents, as a bounded sample
       preview(command)               legality and cost of a command not yet issued
       drain_notifications()          the outbound queue, emptied by the host
       series(metric, window)         aggregate history for panels and the headless runner
```

**Persistence is not a query and does not sit on this axis.** Save, load, the migration chain, Ruleset load, and the crash dump return bytes rather than answers, touch the whole world rather than a bounded sample, are versioned, and — in the async save's case — run concurrently with a Tick. They are specified in [`05 §7`](../05-technical-architecture.md) and `05 §8`, and forcing them into a query flavour would only make the flavours mean less.

> **This ADR previously claimed the surface was *"roughly two methods."* That was wrong, and the way it was wrong is instructive.** It sized the boundary against a **renderer** when the actual consumer is an **inspector**. [`02 §9`](../02-simulation-model.md) is explicit that Evidence is *"a constraint on the simulation"* rather than a UI concern, `00-vision`'s Pillar 4 calls it *"load-bearing in a way the others aren't,"* and [`0001`](0001-godot-and-csharp.md) selected Godot largely **for** the drill-down. Roughly twenty entry points are required by the corpus and the largest family was the one this document did not mention. Worse, its own second revisit trigger — *"a required feature that cannot be expressed through `step` and `visible_agents`"* — had **already fired** when it was written, and could not fire again because nobody had connected the two documents. The fix is not more methods; it is naming the axis those methods sort on.

## Why

**The library never knows what a camera is.** The host passes an AABB and an interpolation alpha; it gets transforms back. Nothing about where the player is looking, or how fast frames are arriving, or whether anything is being drawn at all, crosses into simulation state. This is what keeps the Input Log — `(world seed, configuration, Ruleset content hash, player commands per Tick)` — a complete description of a session. If the camera could reach the simulation, replay would require recording the camera, and the Log would stop being small enough to attach to a bug report.

**Fixed sim Tick, interpolated render** is the mechanism that lets the two rates decouple, and it must be built first because everything else assumes it. The precedents are unambiguous about how much slack this buys: Cities: Skylines 1 simulates car movement at **4 Hz** and interpolates between two sim frames, and nobody notices. Factorio goes further still, *extrapolating* robot positions from a known spline and velocity while updating them once per **20 Ticks**. Render smoothness is a presentation problem with a presentation-layer solution; paying for it in simulation rate is a category error.

**The headless runner is the reason the boundary earns its keep on day one rather than in year three.** `Borough.Headless` fast-forwards thousands of Ticks per second with no renderer attached, which makes balance testing a batch job: run a Ruleset change across a hundred simulated Days, diff the outcomes, keep or discard. A tuning loop that requires launching a game window and waiting for wall-clock time is a tuning loop that stops happening — which is precisely how Citybound's simulation became unbalanceable. `FAST ITERATION`

**This is also the payoff that survives [`0036`](0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md).** This ADR historically justified itself on three things: engine reversibility, headless testing, and GPU-free determinism in CI. `0036` says to **assume the escape hatch is never used**, which weakens the first. The other two are undiminished and they alone justify the boundary — worth stating, because a payoff nobody re-checks after its neighbour changes is how a discipline outlives its reason.

### The split is on the caller's cadence, not the data's location

**A cold query reads hot data all the time.** `02 §9` requires a Building's *"Bins with current levels,"* which is as hot as state gets. The flavour is decided by **how often the host calls**, never by which table the answer comes from. Conflating the two is the failure mode this section exists to prevent: it would argue an Evidence query into the hot path because of what it reads, and rebuild the entire inspector at the wrong cadence.

**This is the third application of an axis the project already uses.** `05 §3` splits **tables** hot/cold; `0036` uses the same line to decide where the no-reference-types lint applies; this splits the **API**. One axis, three levels, and the third one arrived by noticing that the two exceptions to `0036`'s lint — the Ruleset interpreter and the Evidence surface — were both on it. *Repeated exceptions on one axis mean the axis is the abstraction* ([`0031`](0031-one-resource-abstraction-and-depth-not-count.md)).

### The rules, by flavour

| | Hot | Cold |
|---|---|---|
| Pull-only; the library never calls out | ✓ | ✓ |
| Zero engine types | ✓ | ✓ |
| Returns ids and numbers; **the shell owns every string a human reads** | ✓ | ✓ |
| Allocation-free; no reference types (`0036`) | ✓ | **✗ — freely allowed** |
| Fixed-size flat spans of value types | ✓ | ✗ |
| Bounded sample (`02 §9`, [`0006`](0006-no-collection-grows-with-elapsed-time.md)) | n/a | ✓ |
| Results tagged with the generation they were computed against | **open** | ✓ |

**The last row is deliberately unresolved and it is not a hot/cold question.** A cold result is a snapshot the UI holds across Ticks, so it must carry the generation it was computed against or its staleness is silent — the same argument `05 §3` makes for generational handles, one level up. Whether the *hot* path needs the same depends on whether the simulation owns a thread, which `05 §6` has never stated. Recorded as open rather than guessed; see the session-nine brief.

**Cold queries are serviced at a Tick boundary**, and this is not a latency concession — it is a correctness requirement created by [`0037`](0037-the-world-is-single-buffered-and-hazards-are-per-table.md). With one live world state there is no free-floating Past to read, so a drill-down landing mid-Tick would observe a torn world and could display a number that never existed — a `LEGIBLE CAUSE` defect long before it is a threading bug. The host asks; the answer is produced during a serial phase. Worst case is one Tick — 62 ms at the reference rate, on a click. Same shape as `05 §6` step 3's deterministic-order application of pathfinding results.

**And the boundary's own payoffs moved with it.** This ADR historically leaned on the Past/Future double buffer for *"safe parallel reads, since any thread may read the Past without coordination."* `0037` traded that property knowingly, for a 50–150× reduction in per-Tick cost. It is replaced by three specific channels: the **renderer** reads a published transform history, the **saver** reads its own copy taken at save time, and **nothing outside a phase reads the live state**. Asynchronous saves are unaffected; crash forensics is *strengthened*, since a panic emits the last checkpoint plus the Input Log and replays into the failure rather than dumping a dead world.

### The boundary is a membrane, and it was guarded in the direction nothing crosses

One CI check asserts no Godot reference from `Core`. That guards **engine → simulation**, which is the direction with no traffic on it. The other direction is deliberate and constant: `02 §9` requires that *"accumulators keep entity references (or a bounded sample of them) rather than only totals"* — **simulation state shaped by a UI requirement**, argued on `LEGIBLE CAUSE` grounds and correct.

So the real leak vector was never `using Godot;`. It is `Core` growing a method that returns a formatted string, a colour, or a tree node because a panel wanted one. Hence the second enforcement below, which is the checkable form of *the shell owns every string a human reads*.

**And [`0007`](0007-stress-driven-simulation-detail.md) is what keeps the hot path honest.** Because Fidelity is driven by network Stress rather than camera position, rendering is free to be as camera-driven as it likes — frustum culling, distance LOD, sampled pedestrians, skipped Chunks — with no possibility of affecting simulation state. A Traveller on a stressed Segment offscreen is still microscopically simulated and simply not drawn. `0007` removes the camera from the simulation; this ADR removes the simulation from the camera.

## Rejected

**A push or subscription feed for Evidence and notifications.** The obvious shape for *"tell the UI when something changes,"* and it is how engine types get into `Core` — a callback needs a target, a target has a type, and the type is the shell's. Notifications are instead an **outbound queue the host drains**, which is per-frame in cadence but bounded in volume, so it is a queue drain rather than a query. The library still never calls out.

**Letting the shell read simulation structures directly** to avoid marshalling. It would work, it would be fast, and it would delete the boundary — after which the CI check passes while the property is gone. If marshalling is genuinely dominant the answer is a tighter interchange format, and if that is exhausted the trade is re-argued explicitly rather than eroded.

**A third flavour for persistence.** Considered and rejected above: it would exist to hold one already-specified family and would dilute what the other two flavours mean.

## Consequences

- **Two CI checks, not one.** (1) `Borough.Core` has no reference to Godot, transitively — one `using Godot;` added under deadline pressure silently converts a swappable shell into a permanent dependency, and it would not be noticed for months. (2) **No human-readable strings returned from `Core`** — ids and numbers only, resolved to display text by the shell through the Ruleset. This is the enforceable form of the membrane rule, it is the same analyser `0036` already requires, and localisation forces it anyway.
- **Cold-path methods are added deliberately, with a documented reason each, and that is now the expected activity rather than an exception.** `02 §9` is the specification of what they must answer; this document is the specification of how they are shaped.
- **The renderer needs its own state, and that state is disposable.** Per-Chunk MultiMesh buffers, interpolated transforms, cached overlay textures and mesh instances live entirely in `Borough.Godot`, are rebuilt from a snapshot, and nothing in the shell survives a reload.
- **Overlay reads are hot in cadence but slow in content.** Map Layers update every 32–64 Ticks (`05 §9`), so the shell caches them and invalidates on a generation or epoch rather than re-pulling per frame. Stated here because it is the one hot-path call whose cost the naive implementation gets badly wrong.
- **`visible_agents` interpolating between two sim states is served by a published transform history**, one generation deep and well under a megabyte — not by a second world ([`0037`](0037-the-world-is-single-buffered-and-hazards-are-per-table.md)). What remains **open** is whether that history is the library's or the shell's; the `alpha` parameter currently implies the library's, and the answer is downstream of whether the simulation owns a thread (`05 §6`).
- **Some duplication is accepted.** Vector and geometry types will exist on both sides of the boundary, and conversion happens at the shell. This is the price of the property, and it is small.
- **The State Hash and the Factorio save/reload test live in `Borough.Tests` and run without an engine.** Determinism is testable in CI on a machine with no GPU, which is only possible because the core has no display dependency.

## What would trigger revisiting

- **Snapshot marshalling showing up in profiles.** Note that this ADR previously assumed the risk was on `visible_agents`; with the cold path named, **the more likely site is a drill-down** — an `expand()` over hundreds of Households, on a click, on the frame the player is already judging responsiveness by. The mitigation is a tighter interchange format, not letting the renderer reach into simulation structures.
- **A required capability that cannot be expressed as a pull returning ids and numbers.** Restated so that it can actually fire — the old form named `step` and `visible_agents`, which `02 §9` had already exceeded on the day this was written. Replay scrubbing and persistent selection remain the likely candidates. The response is to widen the surface deliberately, never to relax the zero-engine-types rule.
- **Either CI check being disabled or waived.** That is the point at which this ADR stops describing the system, and it should be treated as a defect rather than a process detail.
- **The cold path acquiring a per-frame caller.** If a panel starts polling `inspect_*` every frame, the axis has been violated rather than the rule — and the fix is on the shell side, by caching against a generation, not by promoting the method.
