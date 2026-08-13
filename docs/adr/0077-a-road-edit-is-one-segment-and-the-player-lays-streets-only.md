# A road edit is one Segment and the player lays Streets only

**`CommandKind.Connect` carries one Street Segment between two adjacent grid intersections — an origin, an orientation and whether it is being laid or bulldozed — and Arterials and Junction pieces are refused by name with 5a-bis's successor written beside the refusal.** The payload fits the four fields `Command` already has, so **the Input Log format stays at version 1**, and the two remarks that disagreed about whether it would are reconciled by the rule the codec itself states: what bumps a version is *a sixth field on a command*, and this is not one.

Guiding concepts: `PLAYER GOVERNS`, `LEGIBLE CAUSE`, `SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md). No measurement distinguishes the command shapes: every one of them produces the same graph by a different number of keystrokes, and what they differ in is which edits remain expressible and what a log costs. That is a question for a sitting, and [`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md) decision 1 took it.

## Why

**The corpus counts road editing among the player's five verbs, calls it *the player's core verb*, and specifies its command surface nowhere.** [`plans/0020`](../../plans/0020-the-road-graph.md) recorded that gap rather than filling it, on the ground that milestone 5a retires a risk about *the graph's uniformity* and a command shape is a different question. `Connect` has been declared at `Command.cs:18` and throwing at `Simulation.cs:332` since slice 5.

**The brief's own recommendation was wrong about the graph, and the error is instructive rather than embarrassing.** [`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md) proposes *"one grid-snapped Street Tile per command, painted like `zone`"*, and grounds it in [`0014`](0014-grid-streets-with-freeform-arterials.md)'s *"Streets snap to the grid"* and *"the Road Graph falls out of the Tile grid directly"*. Both quotations are accurate and the conclusion does not follow. **`RoadGenerator` puts nodes only at grid intersections `block_tiles` apart**, and one Street Segment spans an entire block face — 32 Tiles in the shipped Ruleset. There is no such thing as a Street Tile in the graph 5a built. A per-Tile command would either have to split Segments, which is the thing [`0074`](0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md) and `CONTEXT.md` → Address refuse for the ~30,000-Segment figure, or accumulate 32 commands into one edge and leave 31 of them meaning nothing.

**The unit of a road edit is therefore the unit the graph already has.** A Segment between two adjacent intersections is what the generator lays, what the Epoch is per, what severance frees and what frontage is measured against. Choosing anything else would make the player's verb and the simulation's structure differ, which is the seam every later consumer would have to know about.

*A quotation can be accurate and its conclusion false, because `adr/0014` describes the design and `RoadGenerator` is the build.* This is [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s sibling on a third axis: not an absence reasoned from, but a **present** design sentence reasoned from without checking what was built under it.

### The payload, and why twelve bytes still hold

`Command` is `(kind, zone, east, north)` — two `ushort`s and two `Tiles`, twelve bytes with no padding, checked by arithmetic. A Segment edit needs:

| | Field | Where it goes |
|---|---|---|
| The origin intersection | `east`, `north` in Tiles, snapped down to the grid | the two `Tiles` fields, unchanged in meaning |
| Eastward or northward | one bit | the payload word |
| Lay or bulldoze | one bit | the payload word |
| Which kind of road | refused above `Street` | the payload word |

**The `zone` field is the verb's payload slot and always was**, which `Command`'s own docstring says in its opening line — *"a verb, a place, and the verb's payload"*. It is named for the only verb that had one. Reading it as *the permission set* rather than *the payload* is what made the second coordinate pair look necessary.

**The other end of the Segment is derived, not carried**, and that is the whole of why this fits. An origin plus an orientation names an adjacent pair uniquely, because the grid spacing is `block_tiles` and the Ruleset states it. Carrying both endpoints would be carrying a fact the world already holds, at the price of a format version.

### The version, and the two remarks that could not both be right

`Command.cs` and `InputLogCodec.cs` both claim the unapplied verbs arrive free:

> *"the log format has their slot today, so the artefact a bug report is made of does not change shape when they arrive — **and this format version does not have to be bumped for their arrival**."*

Thirty lines below, the same file states the rule that would falsify it:

> *"**What would bump it is a change to a line that already exists**: **a sixth field on a command**, a second number on `citizens`, a different meaning for `seed`."*

**Both cannot be right in general, and which is right was never a property of the verbs — it was a property of a decision nobody had taken.** A verb whose payload fits `(east, north, payload)` costs nothing; a verb needing a second coordinate pair is *a sixth field on a command* by the codec's own definition, and the bump *"would cost every log ever written — including the committed golden baseline"*. **Nobody could have caught this before now**, because no verb has ever needed a payload the four fields could not carry, so the first sentence had never been tested against the second.

The first claim is therefore **narrowed rather than struck**: it is true of `Zone`, `Service`, `Govern` and — as of this decision — `Connect`, and it is true *because* each of their payloads was made to fit, not because declaring a verb early makes its arrival free. A sentence that is true of four verbs and asserted of *any* verb is [`plans/0012`](../../plans/0012-corpus-audit.md) *Cause 1* waiting to be quoted by the next author, who will be reaching for it precisely when their payload does not fit.

### Why Streets and not Arterials

[`0014`](0014-grid-streets-with-freeform-arterials.md) makes Streets grid-snapped and Arterials *"deliberately rare"* freeform splines meeting at authored Junction pieces. A spline is many control points and is not one command at all; a Junction piece needs a piece **library**, which `0014` calls *"content with three faces each"* and which this slice needs the topology fragment of and not the meshes.

**Refusing them is [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)-clean because the refusal is named with a successor**, so the absence is *refused-for-now* rather than silently missing — a distinction that ADR exists to make, and the one that decides whether a later sitting may reason from it. `RoadKind.Arterial` in a `Connect` payload throws with the reason and the owner, exactly as `CommandKind.Connect` itself has thrown since slice 5.

**And the Street alone exercises the Epoch as hard as anything would.** An added Street is precisely [`0012`](0012-routing-intent-lives-in-the-agent.md)'s unsound edge — the case its contract is *boundedly wrong* about — and a bulldozed one is the case it must **never** be wrong about. Arterials would add reach, not rigour.

### Bulldozing is required, not optional

[`plans/0022`](../../plans/0022-the-lot-subdivider-and-build-road.md) names only laying, and lay-only makes two of its own requirements unreachable. [`0012`](0012-routing-intent-lives-in-the-agent.md)'s contract has **two halves** — never wrong about a removal, boundedly wrong about an addition — and an editor that only adds exercises one of them. And the slice's decision 4, *what happens to an occupied Lot that loses its frontage*, has no way to happen at all: nothing else in the simulation frees a Street Segment after world creation.

## Consequences

**`Connect` is applied rather than thrown**, for `RoadKind.Street` only, and the payload word is decoded by a small named type rather than by bit arithmetic at the call site.

**The Input Log format stays at version 1**, so every log ever written — including both committed golden baselines — still reads. The codec's `connect` line spells the payload in words, as `zone` does.

**`Command.Zone`'s documentation is corrected to say it is the payload slot**, with the permission set as one verb's use of it. This is the narrowing that made the second coordinate pair look necessary, and leaving the name while fixing the meaning is cheaper than a rename that would move every call site for no behavioural gain.

**Both remarks in `Command.cs` and `InputLogCodec.cs` are reconciled**: the *free arrival* claim names the four verbs it is true of and states that it holds because each payload was fitted, not because early declaration confers it.

**A road edit bumps the Epoch of the Segments it touched and no others**, which is the first time anything in a running world has driven [`0012`](0012-routing-intent-lives-in-the-agent.md)'s contract. Until now the Epoch was exercised by unit tests and by nothing else, because the generator runs at world creation.

**`RoadGraph.RebuildDerived` is now called on a running world**, where it was previously a load-and-reload path. It remains wholesale: an incremental rebuild is an optimisation of a call whose cost nobody has measured, and the first honest measurement is a player editing roads at a rate no session has yet produced.

## Amended 2026-08-12: the edit unit is a run of Segments, and the bulldoze half leaves

**The unit of a road edit becomes one *run* of Segments along an axis — a click and a drag — rather than one Segment.** The origin, the axis and the kind are unchanged; what is added is a **count**, and it fits: `ConnectPayload` uses bit 0 for the axis, bit 1 for the action and bits 8–15 for the kind, so **bits 2–7 are free**. Six bits is a **63-Segment** run, which at the shipped `block_tiles = 32` is **8.06 km of street in one command**. **The Input Log format stays at version 1** and every log ever written still reads, which was the property this decision was taken to protect and is protected again by the same argument.

**The trigger that fired is not the one this ADR wrote down.** *What would trigger revisiting* predicts a `block_tiles` small enough that one Segment is a fiddly edit; `block_tiles` has not moved. What changed is the **volume of road the player lays**: under [`0090`](0090-the-generator-makes-land-and-the-player-makes-every-road.md) the generator lays none at all, and under [`0089`](0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) the map is 65.5 km across, so a player builds every metre of a network the generator used to supply on a map sixteen times the area. One Segment per command was a natural unit for *editing* a road network and is an unusable one for *drawing* one. ***A revisit trigger names one way a decision can fall, and a decision usually falls another way*** — the sibling of session F's finding that a trigger can be spent before it is written.

**A run is still not a spline.** Every Segment in it is grid-snapped and on one axis, so the graph invariant this ADR rests on is untouched: what reaches the Input Log is a contiguous set of lattice edges, and the far endpoint is still derived from the origin, the axis and `block_tiles` rather than carried. Arterials remain **refused by name**, and the freeform case is unchanged by this amendment.

**The Streets-only restriction is discharged rather than overturned.** `01 §2` already tags it as an ⚠ *as built* caveat with its successor written beside the refusal, which is what `adr/0070`'s discipline asks for and is why nothing here has to reopen it.

**`ConnectAction.Bulldoze` leaves, because [`0091`](0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) makes `Demolish` `01 §2`'s sixth verb** and gives removal one spelling rather than two. That frees bit 1 as well, which would take a run to **127** Segments if anybody ever wants it — recorded rather than spent. **`Demolish`'s own payload is owed and has not been fitted against this ADR's test**, and it is harder than `Connect`'s because it addresses two object types; it joins `Service` and `Govern` in the last revisit trigger below.

## What would trigger revisiting

**A player wanting to draw a Street that is not on the grid.** The grid-snapped Street is [`0014`](0014-grid-streets-with-freeform-arterials.md)'s decision and this ADR inherits it; if that one is reopened, this one follows and the command grows a second endpoint — and with it the version bump this decision avoided.

**Arterials becoming ordinary rather than rare.** The spline command shape is deferred, not refused on its merits. If a design turns up in which the player draws Arterials as often as Streets, the *one Segment per command* unit stops being the natural one and a stroke-based command wants arguing on its own.

**A block_tiles small enough that one Segment is a fiddly edit.** The unit here is a block face, and the shipped value makes that 32 Tiles ≈ 128 m, which is a city block. At a much finer grid the player would be laying dozens of Segments to draw one street, and the command would want a run rather than an edge.

**Any verb whose payload does not fit sixteen bits beside a coordinate pair.** That is the case the format version exists for, and this decision is evidence about how to take it rather than a claim that it will never arrive: fit the payload if fitting it is honest, and bump if it is not. `Service` and `Govern` have not been examined against this test and should be, before either is built.
