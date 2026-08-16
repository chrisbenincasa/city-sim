# 0028 — Evidence, the accumulators

`06` milestone **6**. The brief.

---

## Status

🟡 **SCOPED 2026-08-16, not started. Ungated** — it is the first row of the re-derived Phase 2 spine
and no session, spike or milestone stands in front of it.

**Four decisions were settled with the user in the room on 2026-08-16**, before any task was written,
and they are recorded under *What was settled before scoping* below. **Three remain open** and are
listed under *Open decisions this milestone owes*; none of them blocks task 1.

⚠ **A fifth task and a fourth open decision were scoped here on 2026-08-16 and removed the same day,
before any code.** Attributing budget refusals to the Building they were aimed at is Evidence-shaped,
but the **trailing window** it needs is a property of the **decline model**, which is milestone **17** —
and the derivation this brief recommended for that window is refused by name by
[`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md).
Both moved to 17. ***Evidence reports pressure and does not produce it***, and the line between the two
is the line between this milestone and that one. Reasoning under *What this milestone must not do*.

⚠ **Two things to read before touching this milestone.** Its position in [`06`](../docs/06-roadmap.md)
is argued from **cost of delay** rather than from a dependency, which is unique in that table — so
nothing downstream will fail if it slips, and that is exactly why it slipped for the life of the
project. And the 2026-08-15 corpus sweep found [`01 §6`](../docs/01-player-experience.md) and
[`00-vision`](../docs/00-vision.md) **each naming the other** as the document that schedules Evidence,
with no milestone building it — [`0012`](0012-corpus-audit.md) *Cause 1* with the two copies pointing
at each other rather than drifting apart, which is why no reader of either ever noticed.

---

## Why this milestone exists, in one paragraph

[`02 §9`](../docs/02-simulation-model.md)'s general rule is that **every aggregate figure must be able
to name its constituents**, and today **not one can**. The Census is city-wide by construction — its
store is sample-major over a fixed metric set (`Census.cs:141`) and **no structure in it can name an
entity**. The condition behind a demolition is computed, is correct, and is discarded on the next line:
`ZoneRuleEngine.cs:291-298` says so in its own comment, *"nothing consumes it: `01 §5`'s notification
surface does not exist, and the row it would be copied off is freed on the next line."*
[`adr/0097`](../docs/adr/0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md)'s
reach-failure count is **decided and unbuilt**. And the specimen sentence `CONTEXT.md` uses to define
what `LEGIBLE CAUSE` means — *"abandoned: 74% of work trips exceeded commute budget over 30 days"* —
is unproducible in exactly one of its five clauses ([`0012`](0012-corpus-audit.md)). This milestone
builds the place those answers go.

---

## The named risk

**That Evidence is retrofitted.** `02 §9` prices it *"cheap if designed in and expensive if
retrofitted"*, and every milestone below this one — the District Pool, Money, land value, the Provider
List, the price surface, the residential choice model, Needs, Departure, Life Stages — adds a mechanism
that would each have to be **reopened** to emit into it. Fifteen reopenings against one.

⚠ **The risk is not symmetric with the others in `06`'s table, and the difference matters when this
milestone is under time pressure.** Every other Phase 2 row retires a risk that *fails loudly* — a
scope the engine refuses, a field nothing sets, a conservation law that breaks. This one retires a risk
that fails **silently and later**: nothing breaks if Evidence is absent, the city runs, the tests pass,
and the cost is paid fifteen times by other people. ***A risk that nothing will report is the one a
schedule discounts***, and that is the whole reason this row is third rather than sixteenth.

---

## What was settled before scoping — the grilling of 2026-08-16

Recorded here because the reasoning is load-bearing and two of the four reversed an earlier
recommendation on evidence found mid-session.

### D1 — the axis is **who reads it**

The corpus held two decisions about Evidence pointing opposite ways, neither citing the other. The
Census is emphatically **outside** the World — *"It is owned by whoever runs the session, never by the
World… Putting it on the World would also have made it state — something the State Hash and the save
would each need an answer for"* (`Census.cs:36-42`). `adr/0097` puts an Evidence datum **inside** the
hash, as a saved count on the Citizen.

**What separates them is the reader.** A number the simulation reads is state whatever it is called;
a number only a human reads is instrumentation. That axis is already written down in this project —
`ColdPathAttribute` grants its exception to a type when *"no code path from `step()` reaches it"*, and
names `Evidence` as a passing case.

### D2 — Evidence is an **assembler**, not a store, and Core assembles

Most of `02 §9` needs no accumulator: a Building's occupants and Bin levels are live state, a shortage
is a scan for unmet Rules, *which Trips are on this Segment* is a Traveller scan, and all of it is
legal on a cold path where a human is waiting. **The residue that genuinely accumulates is events whose
subject has left or whose moment has passed.**

Core owns the assembly, because only Core can re-run a Zone Rule predicate or walk an intrusive list.
The host owns every word, per
[`adr/0002`](../docs/adr/0002-simulation-is-an-engine-agnostic-library.md). Core therefore returns a
**structured id-and-number result per question**, and designing those result types is a real cost of
this milestone rather than an afterthought.

### D3 — the accumulator is **inside the World**, on `RulesetTrailTable`'s pattern

⚠ **This reverses the session's own earlier recommendation, which was to keep counts inside and named
constituents outside. Two findings killed it.**

**It put a shared mutable buffer where the determinism machinery is blind.** `step()` is entirely
serial today — zero threading constructs in `src/` — but `TickPhase.cs` deliberately encodes two
functions, `Phases.Permission[]` (what the design allows) against `Phases.Runs()` (what this build
does), and **Move and Layers are `Parallel` in Permission**. Departures and Trip Fates are produced in
Move. An unhashed sample buffer written there is exactly the *parallel loop accumulating into shared
state* the architecture bans — and because it is unhashed, **lint 4 cannot see it**: thread-count
equivalence compares State Hashes, so the buffer would scramble and every test would stay green.
⚠ S5's clean 2-thread scaling result does **not** cover this case, and `spike-results` says why: it
held *by construction, because the Lane pass is wholly Lane-local*, and is explicitly **not a
discharge**. ***The good number came from there being nothing shared.***

**And for the canonical case there is no entity to hang a count on.** A departed Household is gone.
The project has exactly one precedent for a tally that outlives its subjects, and it is a better answer
than the split: **`RulesetTrailTable`**.

| Property | `RulesetTrailTable` | Why it transposes |
|---|---|---|
| Fixed-size `[Table]`, capacity `Retained + 1` | `:87` | The `adr/0006` sink is the cap itself |
| All six columns `Rows.Saved`, `Touch.Cold` | `:87-99` | Survives reload; costs nothing per Tick |
| **Aggregate row at slot 0** — the oldest entry folds its counts in and its identity is dropped | `:184-197` | ***Attribution decays to magnitude***, which is `02 §9`'s *"a bounded sample of them rather than only totals"* implemented |
| Dense and chronological by slide-down copy, **never a ring** | `:131-139` | *"index order is hash composition order, so a ring buffer with a cursor would make two worlds that survived the same transitions hash differently"* |
| A filter at the write door — a transition that cost nothing is never recorded | `:163-166` | *"an all-zero entry would push a real one out of a window sized for diagnosis"* |
| No handle in, no handle out | `Rules.cs:39-48` | Entries slide, so a handle to one would go stale |

### D4 — the bound is hash-bearing, and it is a `const`

Everything above is saved, so the retention bound is hash-bearing and `adr/0052` applies. It is a
**`const` rather than Ruleset data**, on the trail's own argument transposed one step: that file says a
*designer* must not be able to shrink the window that records their own edits, *"on the authority of
the same file whose adoption the history is about"*. Here it is a **player** who must not be able to
shrink the window that explains their own city.

**Named ratifier** — machine, world and quantity, per
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
as amended: **the first real diagnosis that had to reach past the window**, on a city that has actually
declined, measured as *how far back the answer was*. Refuting readings in both directions: a window
never filled means it is too large, and a diagnosis that ran out of entries means it is too small.

---

## Tasks

**Tasks 1, 2 and 4 are the milestone.** Task 3 is a cheap repair the grilling turned up that belongs
here because it is the first producer; 5 and 6 are the standing obligations.

### Task 1 — the Evidence trail

The `RulesetTrailTable` pattern, generalised to more than one event kind: a fixed-size `[Table]` in
`World._tables`, all-`Saved`, all-`Touch.Cold`, entries dense and chronological, an aggregate row at
slot 0 allocated at world creation so no reader needs a liveness branch, and a filter at the write
door. **Appended to the table list**, never inserted — appending is the one edit that moves no row
relative to another.

⚠ **Open decision 3 lands here**: one window shared by every event kind, or one per kind. Do not
choose it by taste; see below.

### Task 2 — keep the abandonment reason

`ZoneRuleEngine.cs:291-298` names this gap in its own comment and has been waiting for somewhere to put
it. Copy `RuleInstanceTable.Reported` and the Tick into the trail **before** `World.DestroyBuilding`
frees the row. This is `02 §9`'s hardest requirement — *"For a **Lot**: why it is vacant. Not 'vacant'
— *why*"* — reaching the one case where the answer is genuinely not recomputable, because the entity
holding it is about to cease existing.

### Task 3 — `adr/0097`'s reach-failure count

Decided, specified, and built by nothing: `grep` finds no `ReachFailure` symbol in `src/` or `tests/`.
A saved count on the Citizen, incremented when the Road Graph cannot deliver a candidate inside the
Commute Budget, **reset on success**. ⚠ **Whose it is, is open decision 4** — the ADR names milestone
19's Departure as its consumer, and `UnplacedTable.cs:9-14` routes the give-up counter to the same
place.

### Task 4 — the assembler

The cold query surface: one Core-side entry point per `02 §9` question, returning a structured
id-and-number result the host renders. Live state is read, predicates are re-run, the trail is read,
and the three are composed into one answer. `[ColdPath]` where a result type needs it. **No strings.**

### Task 5 — something to look at

A runner mode. The obvious shape is `--evidence`, printing what the trail holds and expanding one
aggregate into its constituents, which is the milestone's whole claim rendered in text. It would be the
**ninth** runner mode and the third that steps the world.

### Task 6 — the long acceptance run

100,000+ Ticks with no collection and no magnitude trending upward. ⚠ **The trail's own discipline is
what is being tested here**: entry count is monotonic to the cap and then constant, so the assertion is
a **slot high-water mark that saturates**, not a live count. 5c task 8 found that distinction the hard
way and its record carries the reasoning.

---

## What this milestone must not do

- **Not the drill-down graph.** It is Phase 3 and is named there. This row owns the accumulators.
- **Not trajectory detection or notification.** Phase 3, and its indicators fall out of milestones
  **10**, **15**, **19** and **20** — none of which exist.
- **Not store what a click can recompute.** The default is the assembler; the trail is the exception
  and needs an argument each time, namely *the entity holding this answer will not exist when the
  question is asked*.
- **Not put a shared mutable accumulator in a phase `Permission[]` marks `Parallel`.** If a trail write
  must happen in Move, it goes through a serial commit point, and the reason is written at the site.
- **Not attribute Trip failures to the Building they were aimed at, and not choose a window.** Both
  moved to milestone **17** on 2026-08-16, and the reasoning is the sharpest thing this scoping
  produced. `CONTEXT.md` names **three** sources of Failure Pressure — Trips failing, Rules reaching a
  reporting terminal, conditions below tolerance — and **only the second is built**: `starved_since` is
  started solely by `Blocking.Supply` (`RuleEngine.Stop:598-607`). Making the first one count is a
  **decline** decision, and ***Evidence reports pressure and does not produce it***.
  ⚠ **And the window derivation this brief recommended is refused by name.** It proposed reporting the
  rate over `kind.CondemnAfter × rule.Rate`, which `ZoneRuleEngine.Condemn` already computes.
  [`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)'s
  revisit triggers forbid exactly that: *"not bolted onto `0053`, whose predicate is about Bins and
  whose `CondemnAfter` is denominated in a **Rule's rate**. Two pressure sources sharing one threshold
  would make the number mean two things."* A Ruleset halving a Rule's rate would silently halve the
  Evidence window. ***A derivation that reuses a constant inherits every decision that constant is
  already carrying***, which is `adr/0094`'s `Speed.PerKilometrePerHour` and `02 §2.1`'s Cell/Chunk
  split on a third axis. 17 is the right home either way: `06`'s own inventory parks a **sibling window
  question** there — `01 §6`'s sustained-detection duration, *"derived from the time contagion takes to
  reach a neighbour"* — so the two belong together.
- **Not build a general Trip→Building link.** A `Handle<Building>` column on `TripTable` is a different
  mechanism with a different cost, and 17's counter will not need one either: `TripEngine.Start` holds
  `toBuilding` and `purpose` as live parameters at the line that reaches the verdict.

---

## Definition of done

The four cumulative obligations from `CLAUDE.md`, plus:

- Every aggregate the trail produces can be expanded into named entities, and **a test asserts the
  expansion**, not merely the total. A count that agrees with its constituents' length is the check.
- The trail's cap holds across a 100,000-Tick run — **slots saturating**, live count flat.
- `RebuildDerived` is unaffected: the trail declares no derived column, so there is nothing to rebuild
  and nothing that could rebuild to a different value.
- The State Hash moves, deliberately, once, with a commit whose subject says why
  ([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md):
  moving it costs nothing while nobody is carrying a save, and citing hash movement as a reason to
  defer is itself a defect). All three golden baselines re-recorded.
- `--evidence` prints something a human can read that no other runner mode can produce.

---

## Open decisions this milestone owes, before the task that needs them

### ~~The window~~ — **MOVED TO MILESTONE 17, 2026-08-16.** Not this milestone's

*Kept as a struck row rather than deleted, because the reason it moved is the boundary between this
milestone and the decline model and a future reader will otherwise re-derive it here.* See *What this
milestone must not do*. The live question — how a trailing-window rate is denominated without
authoring a decay rate `adr/0053` deleted, and without welding to a constant `adr/0079` forbids
reusing — travels with it.

### 1. The bound's value — owed before task 1

The trail's own `Retained = 16` is *"the smallest window that outlives a balance sitting's worth of
remove-watch-restore cycles"* and is explicitly a guess with a named ratifier. **Do not copy the digits**
— that number was derived from a *designer's working habit*, and this window is sized by a **player's
diagnosis**, which is a different quantity that happens to share a unit ([`0012`](0012-corpus-audit.md)
*Cause 5*). The value waits on decision 2.

### 2. One window, or one per event kind — owed before task 1

If demolitions, departures and budget refusals share one window, a busy demolition period evicts the
departure history that explains it. If each gets its own, the cap is per-kind and the table is
`kinds × Retained`. **Recommendation: one per kind**, because the failure mode of sharing is that the
*correlated* events crowd each other out, and correlated events are precisely the ones a diagnosis
needs together.

### 3. Whether task 3 belongs to this milestone or to 19 — owed before task 3

`adr/0097` names milestone 19's Departure as the count's consumer, and `UnplacedTable.cs:9-14` routes
the give-up counter and Departure to the same place, warning that naming them earlier *"would be the
trespass the ADR was written to avoid."* **Recommendation: build it here, read it there.** The count is
an Evidence accumulator by `02 §9`'s Citizen row, and the standing rule is that the producer ships with
the mechanism that makes it legible, not with the one that eventually consumes it — which is
`adr/0069`'s finding, that a mechanism with one caller inside another mechanism's predicate is a
mechanism nobody built.
