# The world is single-buffered, and concurrency hazards are per-table

**There is one live world state. A table is double-buffered if and only if a parallel phase both reads and writes it — which is two tables, not all of them.** The full-world copy that [`0004`](0004-typed-tables-over-ecs.md) described as *"a bulk copy of the hot columns"* every Tick is deleted. The three consumers that genuinely need a complete, stable snapshot each get one at their own cadence, and none of those cadences is per-Tick.

```
Lane dynamics        double-buffered   Phase 4 (Move) is parallel and crosses Lanes
Map Layer cells      double-buffered   Phase 5 (Layers) is parallel; also directional smear
everything else      single            written only by serial phases
```

## Why

### The copy was ~150 MB per Tick and nothing read it

At the 1M target the hot state is on the order of **80–150 MB**. An ordinary Tick writes about **1 MB** — roughly 660 Event Wheel wakes, ~23,000 travellers advancing, one staggered layer pass. The copy was therefore doing **150× more work than the Tick itself**, at 8–15 ms per Tick: 13–24% of the budget at the reference rate and **50–100% of it at 4× speed**.

Three things made that worse than a line item:

- **It defeated the Event Wheel.** [`05 §9`](../05-technical-architecture.md)'s premise is *"a Citizen at work for a third of a Day consumes zero CPU for a third of a Day."* A full-state copy touches every sleeping Citizen every Tick, copying a home address and a schooling level that will not change for hours of play. [`0006`](0006-no-collection-grows-with-elapsed-time.md)'s rule — cost proportional to activity, never to population — was violated by a structure two ADRs over, and neither document cited the other.
- **It was memory-bandwidth bound**, which is precisely the failure `05 §6` cites Factorio's electric-network attempt for. The document named the hazard and contained an instance of it.
- **It was language-independent**, so it would have survived every mitigation `0036` considered. It was found *while* sizing the language question, which is the only reason it was found at all.

### It was a naming artifact

[`0002`](0002-simulation-is-an-engine-agnostic-library.md) described Past/Future as *"two world states."* Once the words are **two worlds**, a full copy is implied by the vocabulary. But the property actually required is much narrower:

> **A parallel phase must not observe a partially-updated peer.**

That is a **per-table** property, not a world-level snapshot — and running it over [`02 §1.1`](../02-simulation-model.md)'s phase table sorts every table in one pass:

| Phase | Concurrency | Writes | Needs double buffering? |
|---|---|---|---|
| 0 Input | serial | commands | no |
| 2 Decide | **parallel** | **nothing** — *"output is a list of intents, never a mutation"* | no |
| 3 Settle | serial | most tables | no — a serial writer has no peer to race |
| 4 **Move** | **parallel** | Lane queues, and a Vehicle crossing a Junction reads **another Lane's** queue | **yes** |
| 5 **Layers** | **parallel** | Map Layer cells | **yes**, and already so per `05 §9` |
| 6 Growth | serial | Lots, Buildings | no |
| 7 Commit | serial | schedules, hashes | no |

**Phase 2's read-only-ness stops being tidiness and becomes load-bearing.** It was already stated as a hard property; it is now what permits every entity table to be single-buffered.

### Each of the three real consumers is better served by something cheaper

| Consumer | Needed | Now gets | Cadence |
|---|---|---|---|
| **Async save** (`05 §7`) | a stable full snapshot for seconds | **one real copy, taken at save time**, serialised off in the background | minutes |
| **Renderer** (`05 §2`, `§10`) | transforms at generations N−1 and N | a dedicated **transform history**, one generation deep, well under 1 MB | per frame |
| **Crash forensics** (`05 §8`) | a reproducible artifact of the failure | **the last checkpoint plus the Input Log since** | on panic |

**The crash case is the one that changes most, and it gets stronger rather than weaker.** [`0003`](0003-deterministic-integer-simulation.md) makes replay exact, and `05 §7` already observes that *"replay from save needs only the save and the log after it."* So a panic at Tick 5000 emits `(checkpoint @ 4096, log 4096..5000)`, which reproduces the crash on demand — using machinery that already exists, at no new cost. `05 §8`'s guarantee is met **more** strongly: instead of inspecting a dead world, you replay to Tick 4999 and single-step into the failure under a debugger.

### What "the Past" means now

The vocabulary survives and its referent changes, from a **storage fact** to a **phase-discipline fact**:

> **The Past is not a second copy.** It is *the state as of the start of this Tick*, and Phase 2 observes it because nothing has written yet. Phase ordering is the mechanism; two buffers were only ever one implementation of it.

`02 §1.1`'s semantics — decisions computed against a consistent pre-decision state, applied afterwards, contention resolved honestly in Phase 3 — are **entirely unchanged**. Only the mechanism moved.

## Rejected

**Keeping the full double buffer.** Its best case is that a predictable bulk cost is preferable to a distributed one you cannot profile — a real argument, and the reason to state it is that it is the argument *against* the undo journal below. It does not survive the arithmetic: 8–15 ms per Tick is not a cost to prefer, it is the largest single item in the Tick.

**An undo journal** — record each write's pre-image, roll back on panic. This was the first proposal and it is **unnecessary**: it exists solely to serve crash forensics, and determinism plus the Input Log already do that better for free. Rejecting it also deletes its one real cost, a **write barrier on the hot path** — the busiest path in the simulation, and exactly the kind of distributed cost the Factorio evidence warns against misreading.

**Copy-on-write by Chunk.** Plausible, and it leans on dirty tracking that already exists (`05 §5`, role 1). Rejected because it solves a problem that turns out not to exist once the hazard is classified per table, and because it depends on `0004`'s *"tables partition naturally along Chunks"* — which is true for static entities and **unargued for mobile ones** (see below).

## Consequences

- **`0004`'s block-copy consequence is struck**, and `0004`'s layout claim is untouched. The layout was never the problem; a buffering strategy had ridden into the design inside an ADR about something else — the third instance of that pattern after `0001` (the core's language) and `0002` (an unnamed query surface).
- **`0004`'s revisit trigger could never have caught this.** It reads *"**Not performance.** Hand-written struct-of-arrays is the layout an ECS exists to produce."* That is correct about layout and it made the buffering strategy — whose only defect is a performance one — unchallengeable on the only available ground. Same species as `0002`'s already-fired trigger.
- **`adr/0002`'s *"any thread may read the Past without coordination"* is traded, knowingly.** It is replaced by three specific channels: the renderer reads the **published transform history**, the saver reads **its own copy**, and nothing outside a phase reads the live state.
- **Cold queries are serviced at a Tick boundary.** With one live state, an Evidence drill-down landing mid-Tick would see a torn world and could display a number that never existed — a `LEGIBLE CAUSE` defect before it is a threading bug. The host asks; the answer is produced during a serial phase. Worst case is one Tick of latency — 62 ms at the reference rate, on a click. Same shape as `05 §6` step 3's deterministic-order application of pathfinding results.
- **Phase 2 must stay read-only, permanently.** `05 §6` defers parallel decision evaluation as a build-order item; if Decide ever becomes parallel **and** mutating, every entity table silently reclassifies. The rule at the head of this ADR is the check, and it needs running whenever a phase's concurrency changes.
- **The pathfinding worker pool survives on the Epoch, not on the buffer.** `05 §6` step 3's workers read the Road Graph across Tick boundaries — the one genuine cross-Tick concurrent reader. The Road Graph is mutated only in Phase 0 and Commit and already carries an **Epoch**, so this is unaffected. Worth naming because it is the case that looks like it should break and does not.
- **A crack in `0004` is flagged rather than closed.** *"Tables partition naturally along Chunks"* is true for **static** entities — Buildings, Lots, Lanes — and unargued for **mobile** ones. Grouping Households by home Chunk means a Household moving house **relocates its row**, and a relocated row is worse than a stale one: `05 §3`'s generation counter detects use-after-free but cannot detect *this handle now points at someone else's valid row*. Either rows never move and the Chunk partition is a separate index, or rows move and handles need indirection. `05 §5` role 3 leans on this and it is owed an answer.
- **`CONTEXT` → Past / Future is removed**, on the `Chunk` precedent from `0034`: it carries no player-facing meaning and `CONTEXT.md` is domain language. Defined in `05 §3` instead.

## What would trigger revisiting

- **A parallel phase gaining a write to a table currently classified single.** This is the rule failing, not the decision failing, and the response is to reclassify that table — never to reinstate the world-level copy.
- **Cold-query latency being felt.** One Tick is imperceptible at 16 Hz; if a panel ever needs a mid-Tick answer, the answer is a published snapshot for that specific data, not a return to a readable Past.
- **The transform history proving insufficient for interpolation.** `visible_agents(aabb, alpha)` needs generations N−1 and N live. If what the renderer needs turns out to be wider than transforms, the history widens — it does not become a world.
- **Not the copy's absence showing up in a profile.** There is nothing to show up. If the Tick is slow, the order of suspicion is the Microscopic Cap, routing, and the Sweep Rule schedule — in that order.
