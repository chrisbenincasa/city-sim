# 0021 — Trips, Legs and the pedestrian layer (milestone 5b)

> The slice brief for [`06`](../docs/06-roadmap.md) milestone **5b**, *Trips, Legs and the pedestrian
> layer* — and, ahead of it, the brief for the session that gates it.
> Decisions to be built: [`adr/0008`](../docs/adr/0008-walking-is-a-simulated-leg.md) (the gate),
> [`adr/0005`](../docs/adr/0005-two-fidelity-tiers.md),
> [`adr/0072`](../docs/adr/0072-the-mode-mask-is-saved-on-the-arc-and-the-segments-is-derived.md),
> [`adr/0071`](../docs/adr/0071-travel-time-is-sub-tick-and-q16-16-is-a-scale-rather-than-a-meaning.md).
> Design realised: [`03 §3.7`](../docs/03-agent-architecture.md), `CONTEXT.md` → Trip, Leg, Traveller,
> Access Point, Trip Fate, Severance.
>
> **This is a planning document and therefore cites rather than owns**
> ([`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)). Every
> figure below names its owner. If this document and its owner disagree, the owner is right.

## Status

**🔨 IN FLIGHT 2026-08-11. Tasks 1–3 are built; the slice is not done.** What exists is the
**structural half** — the vocabulary type, the three tables and the walk Leg resolved end to end —
and it stops deliberately short of everything that moves the State Hash. **1,084 tests green against
5a's 1,060, and neither golden baseline was re-recorded**, because nothing is registered with `World`
yet. See *What tasks 1–3 built* below for the record, and what remains.

**✅ UNBLOCKED 2026-08-11. Session F has run — one sitting, all seven decisions — and 5b's second gate
is discharged.** Both gates are now clear: **D** (the traffic model, 2026-08-10) and **F** (the Leg
model, 2026-08-11). The slice below may be started.

F produced an amendment and three ADRs:
[`adr/0008`](../docs/adr/0008-walking-is-a-simulated-leg.md) amended in place (four consequences and the
revisit trigger), plus
[`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md),
[`adr/0075`](../docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) and
[`adr/0076`](../docs/adr/0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md).
`CONTEXT.md` gained **Address** and **Node** and had six entries amended. **It closed neither §B-16 nor
§B-17**, which was the constraint it was booked under. Full record in *What session F decided* below.

*Original status, kept because the gate's history is the reason the brief was written:* ~~**⚠ BLOCKED.
Not started, and it must not be started.** Milestone 5b is gated on session **F** —
[`adr/0008`](../docs/adr/0008-walking-is-a-simulated-leg.md), *walking is a simulated Leg* — which has
not run. [`0000`](0000-board.md)'s argument-track table carries row **F** unstruck against *milestone
5b*, and [`0002`](0002-open-questions.md) §F states the obligation in the strongest form it uses
anywhere: "Makes 5b **the irreversible milestone**, so it is owed **before** the Leg model is built."
`CLAUDE.md`'s standing rule is that **a gated slice must not be started before its gate clears.**~~

**This document exists in two halves for that reason.** *Session F's brief* below is the gate and **has
run**; *The slice* below that is the build, and it is now available.

### ⚠ 5b has two gates, one of them cleared, and reading the first as the second is how this brief nearly got written wrong

**Session D also gated 5b, and D has run.** The board's D blockquote says so in its own words — *"the
only thing that can replace it is Trip generation, which is milestone 5b, which D gates"* — and
`CLAUDE.md` repeats it. That sentence is true and it is not the whole gate. The gate table lists **two**
sessions against this milestone:

| | Session | What is missing | Unblocks | State |
|---|---|---|---|---|
| ~~**D**~~ | ~~`03 §5` — the traffic model~~ | ~~the most detailed unargued design in the project~~ | 5b, 5c, 6, 7a | **RUN 2026-08-10** |
| **F** | [`adr/0008`](../docs/adr/0008-walking-is-a-simulated-leg.md) — walking is a simulated Leg | *"Written from research"*, never argued | **5b** | **🔴 OPEN** |

D cleared the traffic model. F is the Leg model, and nothing has touched it.

**This is [`plans/0012`](0012-corpus-audit.md) *Cause 1* on the gate rather than on the fact, and
[`0020`](0020-the-road-graph.md) had already written the warning three paragraphs into its own status
section** — about the S2 harness, *"a deletion blocked twice for unrelated reasons is exactly the kind
of row that gets struck for the wrong one"*. Same shape, one milestone later, and the document carrying
the warning is the one this brief was modelled on. **Keep the two apart**: 5b's traffic-model gate is
discharged and its *Leg-model* gate is not.

---

## Session F's brief — the gate

**F is one sitting, and it does not get a plan document of its own.** [`0017`](0017-session-d-the-traffic-model.md)
is the precedent and it states the criterion: a session earns a brief when it is *more than one sitting*,
which is the same test that gives a slice a plan. F is not, and the reason is the next paragraph.

### Session D task 0 already ran F's typing pass, and that is most of the work

`adr/0043` says a claim a measurement could settle must not be settled by argument, and `0002` §F turns
that into F's instruction directly: *"`adr/0016`, `adr/0009` and `adr/0008` each read as decided, each
carry a quantitative claim, and none has a number… **Type them before grilling them** — if the refuting
number and the machine can be named, they are measurable and **a session must not close them.**"*

**That typing has happened.** Two §B rows are stamped *"NEW, from session D task 0"* and both are
`adr/0008`'s:

| `0002` | The claim, as `adr/0008` states it | The refuting number | Machine |
|---|---|---|---|
| **§B-16** | *"pedestrian networks do not saturate at this scale"* — asserted in **three** places (`adr/0008`, `03 §3.7`, `CONTEXT.md` → Fidelity) and measured in none | peak pedestrian density per block face at 1M, against the density at which walking speed falls | **5b** |
| **§B-17** | *"Trip object count roughly triples"* — the number the Trip table is to be **sized** on | mean Legs per Trip | **5b** |

So **F may not close either of them**, and a brief that asked it to would be setting the session up to
break the rule that booked it. What F is left with is the *arguable* residue — and the residue is the
part that is genuinely irreversible, which is the right division.

### ⚠ What F must decide first: `adr/0008` asks for a structure `adr/0072` rejects by name

**This is the finding that makes the gate obviously real work rather than a formality, and it is exactly
what a grilling session exists to catch.** `adr/0008`'s second consequence reads:

> *"**The road network needs a pedestrian layer**: **sidewalk edges alongside street edges**, and
> crossing edges at junctions. This is real work, not a free consequence."*

Every other document in the corpus says the opposite. `CONTEXT.md` → Segment: *"**Walking does not add
Segments.** The mode mask is *an edge property, not a second edge set*, so a Street's footway is **the
same Segment with the foot bit set**, and the pedestrian network is a **subgraph** rather than an
addition."* `adr/0072`, shipped yesterday, quotes `CONTEXT.md` → Road Graph as categorical — *"the same
structure, tagged by which modes may traverse them — **not two parallel networks**"* — and names the
three things one graph buys, all three of which splitting it would lose: one Epoch, one revalidation
path, and **a multi-Leg Trip routed by a single mode-aware search rather than stitched across two
structures.**

**`adr/0008` predates the mode-mask decision and has never been amended.** Taken literally it instructs
5b to build the thing `adr/0072` refuses. Nobody has noticed because nobody has built a Leg. **F must
amend `adr/0008` in place** — the corpus's own form, a banner and not a deletion — and the amendment is
small: *sidewalk edges alongside street edges* becomes *the foot bit on the Street's Arcs*, and the
argument the ADR was making is untouched, because the argument was never about edge sets. It was about
Buildings not sitting on the road graph.

**And the amendment decides the revisit trigger's cost.** `adr/0008`'s one trigger names the coarser
topology as *the mitigation to reach for first* — *"one pedestrian edge per block face rather than per
street segment — **not** deleting the Leg."* Under the mask reading that mitigation is **not available as
a tuning change**: it is a change to which Arcs exist, which is a graph change and hash-bearing. F should
either design the coarse form now and leave it unbuilt, or record that the trigger's stated mitigation
has become expensive and say what replaces it. **A revisit trigger whose mitigation no longer exists is a
trigger nobody can act on**, which is the failure `adr/0073` was written about one layer down.

### What else F must decide

Everything below is *arguable* under `adr/0043`: no measurement settles it, and each is a choice the Leg
model bakes in on its first line.

**2. What a Leg *is*, as a field set.** **No document in the corpus states one.** `CONTEXT.md` → Leg
defines a Leg by its mode-homogeneity and by the `walk → drive → walk` minimum; it names no fields. Mode,
endpoints and a cost are implied by every consumer and written down by none. This is the largest hole F
closes, and it propagates: `03 §4` invariant 3 already owes *"write down what is discarded when a
Traveller leaves a Microscopic segment — anything not enumerated is a bug"*, and that enumeration cannot
be written against a structure nobody has specified.

**3. The pedestrian access point, and whether it is a second Access Point or a second offset.**
`CONTEXT.md` → Access Point says every Building has a **pedestrian** and a **vehicle** one, that *"an
Access Point is an offset along a Segment, never a node"*, and that the consequence is a query shape:
*"a routing query is therefore `(Segment, offset) → (Segment, offset)` rather than node-to-node, **which
is the query shape everything downstream must be measured on**."* `adr/0008` says the two *"are usually
the same place and occasionally are not, and the distinction is what lets parking later become a real
location without restructuring anything."* Whether that is two rows, two columns or one row with a flag
is F's, because milestone 8 inherits it — and `adr/0009`'s superseding note has already cashed the split:
*"a **District is bounded by where transport can be ignored**; a **shed is bounded by where transport
must be measured**, because per `adr/0008` the walk Leg is its entire output."*

**4. Whether the Commute Budget is genuinely one currency across modes.** `adr/0008` asserts it —
*"walking minutes and driving minutes are the same currency and both count against it"* — and derives it
from the SC4 rule that the routed quantity must be the scored quantity. It is stated nowhere else and has
never been examined. The obvious objection is that people do not value the two equally; the obvious
answer is that a weighting is a tuning number rather than a structure. **F should say which, because a
per-mode weight is hash-bearing and `adr/0052` wants it named on the day.**

**5. Is a walk Leg *always* Statistical, or *almost* always? The corpus says both.** `CONTEXT.md` →
Fidelity is categorical — *"a **walk Leg is always Statistical**… there is nothing a second tier could
find"* — while `adr/0007` says *"walk Legs resolve statistically **almost always**"* and `adr/0008` says
*"Statistical **approximately always**… they **almost never** enter the expensive regime."* **The
difference is a whole mechanism.** Categorical means no promotion path for a foot Segment need exist at
all, and 5b builds nothing; probabilistic means one must, and 5b owes a hole for it. The underlying
claim is §B-16 and is unmeasured, but **which reading the structure takes is not the measurement** — it
is F's, and it should be settled toward the categorical reading with `03 §3.7`'s transit trigger as the
one thing that reopens it.

**6. Where a walk Leg starts and ends when there is no parking.** `adr/0008` is explicit that this
becomes an open question rather than a non-question: *"the walk Leg has to start and end somewhere.
Deferring a real parking model is defensible; pretending the question does not exist is not."* Parking is
milestone 8. F must name the placeholder and its retrofit cost, **not** invent a parking model —
`adr/0070` applies, the absence is *unbuilt*, and it may not generate a compensating design position.

**7. The Trip Fate set, whether it is closed, and what *stranded* means.** `CONTEXT.md` gives four —
*completed*, *no route found*, *exceeded commute budget*, *stranded* — and the corpus has refused a fifth
**three times on the same ground**: `adr/0067` (*"the Trip completed; what failed is the purchase"*),
`CONTEXT.md` → Parking Shed (*"deliberately no *no parking* Trip Fate"*), and → Diversion (*"there is no
Trip Fate for a lost driver"*). **Three refusals is a pattern worth promoting to a rule**, and F is where
it gets one. ⚠ **And *stranded* is glossed twice with different entry conditions**: `CONTEXT.md` → Trip
Fate says *the network changed mid-journey*, and `CONTEXT.md` → Diversion says a Traveller *"is
**stranded** when no arc out of the node it stands on reduces the straight-line distance to its Target."*
Whether that is one Fate with two entry conditions or two Fates wearing one name, **the corpus does not
say** — and 5b writes the enum.

### What F must hand forward rather than settle

- **§B-16 and §B-17** — routed to 5b above. F may sharpen the instrument; it may not report a number.
- **Whether transit is ever built.** `03 §6.4` already settles the part that matters — *"a bus is a Leg
  type inserted into machinery that already handles Legs"* — and `03 §3.7` records the one trigger that
  reopens walking's single fidelity: *"a stop is a queue with a capacity, and a platform is the one
  pedestrian context where the no-saturation argument genuinely fails."* That is a recorded trigger, not
  an open question, and F should leave it alone.

### Four corpus defects F should collect on the way past

None blocks the session; all four are in the documents F will be reading, and `adr/0073` says a finding
about a document you do not own gets routed rather than worked around.

- **`adr/0041` still says the travel-time matrix is District-granular, and it is unbannered.** *"The
  matrix remains District-granular; only *attribution* leaves."* `adr/0047` reversed that —
  *"the travel-time matrix's granularity is the routing partition, not the District"* — and `CONTEXT.md`
  → District carries the reversal. `adr/0041` has an amendment block that does not touch the bullet.
  **Do not cite that sentence**; it is 5c's foundation and it is wrong in the ADR a reader would reach
  for first.

- **`CONTEXT.md` has no `Node` entry, and `Node` is load-bearing.** It appears in the route cache key
  *(origin node, destination node, variant)*, in the Rejoin Target, in the Sight Horizon's floor
  (*"its next branching node"*), in `adr/0040`'s pathfinding cluster, and in the definition of Segment
  and Access Point — both of which define themselves *against* it. 5a shipped `RoadNodeTable`. `CLAUDE.md`
  is explicit: *"if a concept needs a name that isn't in `CONTEXT.md`, add it there first."* This is
  `0012` *Cause 1* with the copy count at **zero**.
- **No design document owns Severance.** `06` says *"the payoff is Severance, argued in `adr/0014` and
  `03 §3.7`"* — but `§3.7` argues *walking's fidelity* and the one-graph decision, and cites Severance as
  evidence **for** that decision by quoting `CONTEXT.md` back at itself. The definition exists only in
  `CONTEXT.md`. Under `adr/0042` a design document owns and a planning document cites; **Severance is
  cited by two and owned by none**, and it is this milestone's stated payoff.
- **`docs/movement-primer.md` says the Microscopic Cap counts Segments.** `adr/0062` settled that it
  counts **Vehicles**, and the primer was written in the *same commit* that changed the unit. The primer's
  header disclaims authority, which covers it — but *"the fourth copy drifted inside one commit"* is
  exactly the failure its header says it exists to avoid, and it is the cheapest possible sighting of
  `0012` *Cause 1*.

---

---

## What session F decided — the record

**Ran 2026-08-11, one sitting, seven decisions, and it closed no measurable claim.** The brief's own
constraint was that D task 0's typing pass had already routed §B-16 and §B-17 to milestone 5b, so F may
sharpen the instrument and may not report a number. It did not.

| | Decision | Outcome | Record |
|---|---|---|---|
| **1** | The amendment `adr/0072` forces | **Split, not substituted.** The *sidewalk edges* half is refused and becomes the foot bit on the Arc; the *crossing edges* half **stands literally** as foot-only Segments. *"Real work, not a free consequence"* is **true and already discharged — by 5a** | `adr/0008` |
| **1b** | The revisit trigger's mitigation | **Already spent.** *One edge per block face* is what the graph is. The lever is the **search**, not the topology: cache the walk route, or straight-line plus a detour factor | `adr/0008` |
| **2** | What a Leg is | **A three-way split.** Trip owns purpose and Fate, **Leg is the plan**, **Traveller is the cursor**. A Leg stores a **cost, never a path**; Legs are created **eagerly** | [`adr/0075`](../docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) |
| **3** | The pedestrian Access Point | **Two Addresses on the Building**, pedestrian and vehicle, two saved columns, **equal by construction** today | [`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md) |
| **4** | Is the Commute Budget one currency | **Yes, literally — clock minutes, no per-mode weight.** Distaste for walking belongs to the choice model, on a Provider List entry's mode | `adr/0008`, `CONTEXT.md` |
| **5** | Always or almost always Statistical | **Categorical.** 5b builds no foot promotion path and owes no hole for one | `adr/0008` |
| **6** | The no-parking placeholder | **The vehicle Address, as 5c's *sole* path and never a fallback**, plus the milestone-8 prohibition, plus the direction of error | `adr/0008` |
| **7** | The Trip Fate set | **Closed at four**, on a two-clause rule. *Stranded* is the Fate; the lost-driver condition is renamed **a Rejoin is abandoned** | [`adr/0076`](../docs/adr/0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md) |

### The four findings that outlive the session

**1. The brief was wrong about the size of the amendment, in both directions at once.** It proposed
substituting *sidewalk edges alongside street edges* with *the foot bit on the Street's Arcs*, which
silently drops the *crossing edges at junctions* clause — and that clause is **correct**, is what
`CONTEXT.md` → Segment already calls *"the small set of foot-only Segments… the edges Severance turns
on"*, and is **already built**: `RoadGenerator` keeps every `foot_crossing_every`-th severed crossing as
a `RoadKind.FootPath` and lays standalone foot paths. So one half was over-corrected and the other
under-corrected, and the cause is the same in both: **the consequence names two claims in one breath**,
so a reader who evaluates the sentence evaluates whichever half is salient. *An `and` in a consequence is
two consequences, and it should be written as two.*

**2. A revisit trigger can be spent before it is written.** `adr/0008`'s only stated mitigation — *one
pedestrian edge per block face rather than per street segment* — was not made expensive by `adr/0072`;
it was **already the state of the graph**, because `CONTEXT.md` → Segment fixes a Segment at roughly a
block-length link. Nobody spent it; it was never available. This is not `adr/0073`'s failure (a
workaround removing the pressure that would have fixed the source) but its neighbour: **a trigger whose
mitigation was already the status quo on the day it was written**, and no amount of later diligence would
have caught it, because the only way to notice is to price the mitigation against the current graph
rather than against the graph the ADR imagined.

**3. A placeholder whose value is inside the range of legitimate answers cannot announce itself — and
this one paid the player for the shortage.** The obvious no-parking placeholder makes a flanking walk Leg
**zero-length**, which is also what a Building with its own garage genuinely produces. So the hole is
indistinguishable from a modelled answer, which is the `pool` precedent inverted. **And the failure mode
is not merely silence**: if the vehicle Access Point were ever reachable as a *fallback* from an exhausted
Parking Shed, a full car park would cost **less** than an empty one — the driver arrives at the door
instead of parking three blocks away — so the player is rewarded for under-building parking, and the
reward grows with the scarcity. `CONTEXT.md` → Parking Shed already forbids it (*"scarcity **widens** the
shed"*), and F wrote the prohibition into `adr/0008` because **F is the last document before the code
exists.** *The general form: check a placeholder's value against the range of legitimate answers, and
check the incentive it creates before it becomes a fallback.*

**4. Side of street was nearly surrendered by collapsing two questions into one.** F's first draft of the
amendment concluded that one graph meant giving up which side of a Street a place is on. It does not:
**whether side is *modelled* is independent of whether it is *in the graph*.** Four rungs exist and only
the fourth — two footway edges per Street — is refused. `adr/0074` takes the second, at one saved bit and
one cost term. **The corpus had already asked for it and nobody had noticed**: `adr/0009`'s deferred table
lists *"allow or ban street parking per Road Segment **side**"*, so side of a Segment was a latent concept
with nowhere to live, and parking was always going to be the milestone that demanded it.

### What F handed forward, and what it deliberately did not choose

**Handed forward, untouched:** §B-16 (*do pedestrian networks saturate*) and §B-17 (*mean Legs per Trip*),
both *measurable*, both machine **5b**. `adr/0008`'s amended revisit trigger names §B-16 explicitly and
says the ADR **may not be cited as having a number**.

**Two new §D2 rows, and one existing row corrected.** The **crossing cost** and the **Commute Budget** are
new, hash-bearing, and unset with named ratifiers. **`walk_speed_kph` is not new** — it shipped with 5a
inside the free-flow speeds row — and F found its **ratifier misordered**: that row names 5c's travel-time
matrix, but a walk Leg needs no matrix, no cache and no VDF, so **5b is the first thing that produces a
walk time a person can call implausible**. Corrected in place. *The row bundled three numbers because they
share a source and then gave them one ratifier because they shared a row, which is the granularity defect
`plans/0012` names.*

**One §D row that was refused rather than opened.** A per-mode weight in the Commute Budget would have
been hash-bearing and would have owed a row. F refused it on `adr/0008`'s own ground, so the section got
**smaller** by an argument — the same direction as `adr/0059`.

**Three hash-bearing numbers named in advance rather than discovered.** `adr/0069` shipped needing three
its ADR predicted none of, and the board records that. 5b's are the crossing cost, the Commute Budget and
`walk_speed_kph` — all three written down before a line of code, which is the correction that episode
asked for.

**Six corpus defects routed to [`0012`](0012-corpus-audit.md)**, one of them paid in the sitting
(`CONTEXT.md` → **Node**, because an Address is defined as *never a Node* and a definition cannot rest on
an undefined term). Two were found by F rather than by the brief: `adr/0025` says a Building holds *one*
Access Point where `CONTEXT.md` says two, and `adr/0007` carries the same *almost always* hedge F settled
in `adr/0008` — routed to session **E**, which owns it, per `adr/0073`.

---

## The slice — ~~everything below is BLOCKED on the section above~~ **AVAILABLE 2026-08-11**

### Why this slice, and why now

**Because it is the only thing that can repair either half of [`0013`](0013-tick-budget.md), and it
repairs them in opposite directions.** The ledger's two movement rows are both half-priced and
half-priced in different halves:

| Row | Unit | Multiplicand | What 5b supplies |
|---|---|---|---|
| **Routing** | **measured** — ~9.4–10.5 ms, a *maximum* over five pinned captures | **guessed, and the wrong event** — 16 Trip starts, where R6.3 showed the expensive event is a **diversion** | the multiplicand |
| **Microscopic Lane model** | **measured** — 27.4–29.3 ns a Vehicle (S5 L5) | **none at all** — the Microscopic Cap is unset and its demand half is 5b's | the multiplicand |

`0013` says it in one sentence: *"routing needs 5b's Trip generation to fix a multiplicand, and the Lane
model needs 5b's stress counts to acquire one."* Routing carries **60–67 of the ledger's ≥114 points at
4×** — without it the ledger reads 42–48% and fits with room — so the question *does the simulation fit*
is a statement about one row, and that row's multiplicand is 5b's.

**And the code holds four named holes waiting on it by name.** `RoadSegmentTable.Fidelity` is
`adr/0007`'s hole and its docstring says why: *"Fidelity follows Stress, Stress needs volume, and volume
is written by Trips — 5b."* `RoadGraph.RebuildDerived` repeats it at the write site. Tick **Phase 4
(`Move`)** is an empty method whose own remarks reserve it — *"Lanes advance Vehicles; Statistical trips
check arrival. Empty until Phase 2 of the roadmap."* And `LineSourceQueries` — Noise and near-road
pollution — is a named hole whose source term is traffic volume, which cascades into Desirability and
the land-value target that `LayerCellTable` is already diffusing toward and **nothing writes**.

**Under `adr/0070` every one of those is *unbuilt*, not *refused*.** None may generate a compensating
design position, and the answer to all of them is the same: build the mechanism.

### What this slice is

**Trips that exist, hash, save, complete, fail with a recorded reason, and can be walked.** The city
gains people going places on foot, and the first thing it can say is *these people cannot reach those
shops*.

| In | Why |
|---|---|
| `Trip` and `Leg` as typed tables, Legs as an **intrusive index list** off the Trip | `CLAUDE.md`; `CONTEXT.md` → Trip is *"an ordered sequence of Legs"* |
| A **pedestrian and a vehicle Access Point** per Building, each an **`Address`** — `(Segment, offset, side)`, `(derived AND rebuilt)` | `CONTEXT.md` → Access Point; `adr/0008`'s third consequence; `adr/0074`, `adr/0078` |
| The **walk Leg, resolved end to end** — `distance / speed` over the foot subgraph | `03 §3.7`: for a walk Leg this *"is not an approximation, it is the exact answer"* |
| A **Trip generator** — one, named, and argued | Nothing generates Trips; see *Decisions this slice must close* |
| **Trip Fate**, recorded and reported | `CONTEXT.md` → Trip Fate; `02`'s per-Tick assertion *no Trip without a Fate* |
| The **Commute Budget**, as Ruleset data | `CONTEXT.md` → Commute Budget; `adr/0015`, and it is hash-bearing |
| Tick **Phase 4** doing something | The phase exists and is empty |
| **Volume** on the Segment, incremented on entry and decremented on exit | `03 §3.3` and `adr/0041`; it is what unblocks Stress and therefore Fidelity |
| `--trips` in the headless runner, and a **Severance demonstration** | `06` rule 2: there is something to *look at*, and Severance is this milestone's stated payoff |
| Census metric family, invariants, State Hash coverage, a long run | The definition of done |

| Out | Owner |
|---|---|
| Routing on the vehicle graph, the travel-time matrix, the route cache | **5c** — and S2 has measured all of it |
| Lanes, IDM, Overlaps, the Microscopic tier | **6** — and S5 has priced the kernel |
| Stress *thresholds* (`T_high`/`T_low`), promotion, demotion, hysteresis | **7a**; 5b supplies the volume they read and chooses none of them |
| Parking, Sheds, the shed query | **8** |
| Transit | Unmilestoned, and `03 §6.4` says a bus is a Leg type inserted later |
| Jobs, wages, the labour market | Unmilestoned — **and this is why the generator is a decision, not a task** |

### Tasks

**1. Access Points.** Every Building gets a pedestrian and a vehicle Access Point, each an **Address** —
~~`(Handle<RoadSegment>, offset)`~~ **`(Handle<RoadSegment>, offset, side)`**, per
[`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md).
~~Saved and hashed — a Building's front door is a property of the city, not a cache.~~
⚠ **CORRECTED 2026-08-11 by
[`adr/0078`](../docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md),
which 5a-bis shipped after this line was written: an Access Point is `(derived AND rebuilt)` on the
Epoch, and it is not saved.** The offset makes this the query shape `CONTEXT.md` says *"everything
downstream must be measured on"*, so getting it wrong here mis-measures 5c and 8 as well.

**The correction is narrower than it reads, and the original's *reason* survives intact.** A front door
is still a property of the city rather than a cache — what is saved is the **Lot's position and its
side**, which is a place on the ground and is exactly what a Building's front door is. What is *derived*
is the pair `(Segment handle, offset)`, and that pair is a function of the graph, so saving it is not an
option rather than a choice: `BulldozeStreet` **frees the Segment row**
([`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)),
so a saved handle would outlive its target and the next lay would recycle the slot underneath it. ***A
saved handle to a row a player can destroy is a dangling pointer with a save file behind it.***
`Frontage.Locate` runs the derivation backwards from the saved position, and the two sets are disjoint —
a Lot on a horizontal Street has `north ≡ 0 (mod block_tiles)`, one on a vertical Street the reverse —
so the position names at most one lattice edge and nothing is lost by not storing it.

**`Address` is a named value type and not a tuple spelled out at each site**, which is what makes
milestone 8 a one-endpoint swap rather than a restructure. **It exists as of 5a-bis** —
`Borough.Core.Space.Address`, with `Address.None` as the *no front door* value `adr/0079` requires — so
this task **consumes** the type rather than introducing it. **Side** is left or right of the Segment's
forward direction — fixed A→B by its endpoints, so it needs no geometry — and it exists so that a walk
between two Addresses on the same Segment and opposite sides pays a **crossing cost**. That cost is
`[trips]` Ruleset data, hash-bearing, and **5b must not choose its value**: it is a new `0002` §D2 row
with a named ratifier.

**The two Addresses are written equal by construction**, and a docstring must say what makes them
diverge — ~~5a-bis's subdivider,~~ milestone 8's parking, `03 §6.6`'s freight. ⚠ **The subdivider is
struck from that list because it shipped and does not do it**: `LotSubdivider` derives **one** Address
per Lot, from the Lot's own position and side. That is not an omission — a second Address needs a second
saved fact, and under `adr/0070` inventing one for a consumer that does not exist is the position this
slice may not take. So *the two are equal* stops being an interim simplification and becomes **the
built behaviour**, and the divergence list is genuinely two entries, both in later milestones.
⚠ **And the vehicle Address is never a fallback from a failed Parking Shed query**, which is milestone
8's rule written now: an exhausted Shed **widens**, because a full car park must not cost less than an
empty one.

~~**⚠ This task's shape depends on whether 5a-bis has landed, and it should have.**~~ ✅ **IT HAS —
2026-08-11, [`0022`](0022-the-lot-subdivider-and-build-road.md), all seven tasks.** So the branch this
paragraph hedged against is closed: **the nearest-Segment-by-construction fallback is dead and must not
be written.** Every Lot in a world with `[roads]` and `[lots]` carries a real, derived Address, and
`Invariant.VacantLotHasFrontage` is the whole-world check that says so. What this task actually owes is
therefore **smaller than the paragraph below predicted**: not an assignment, but *lifting a Lot's
Address onto the Building that stands on it*, and deciding nothing except what a Building with
`Address.None` does — which `adr/0079` already answers, *the Trip ends **no route found***.

*Original hedge, kept because it is the record of a dependency that was real:*
[`0022`](0022-the-lot-subdivider-and-build-road.md) is **ungated and available now**, while this slice
waits on session F — so the natural order is 5a-bis first. It produces **frontage**, and `CONTEXT.md` →
Frontage puts the Access Point downstream of it: *"subdividing consumes frontage — narrow terraced Lots
eat the available street edge to **buy one Access Point each**."* Run in that order, this task inherits
real Access Points and the walk Leg's origin stops being a placeholder. Run without it, the assignment is
nearest-Segment-by-construction and must say so in a docstring — every Access Point it invents is one the
subdivider would have derived.

**2. `Trip` and `Leg` tables.** ~~`Borough.Core` — namespace is F's to name, since F names the Leg.~~
**F named the Leg and left the namespace to the build**, which is the smaller decision.
[`adr/0075`](../docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) fixes the field sets and the
split is **three-way, not two**: a **Trip** carries a **Trip Purpose**, origin and destination
**Address**, a Leg **head index**, a Fate and **the index of the failing Leg**; a **Leg** carries mode,
two Addresses, a `TravelTime` (`adr/0071`, Q16.16 Ticks) and a `next` index — **the plan**; and a
**Traveller** carries the Citizen, the Trip, which Leg it is on and that Leg's arrival Tick — **the
cursor**. Per-field `saved AND hashed` / `derived AND rebuilt` throughout, which is what allocates the
column and what closes the hash's coverage hole. **A Leg stores a cost and never a path**, and every Leg
of a Trip is created **at Trip creation** — the first is what keeps the table small enough for
`adr/0008`'s *"roughly triples"*, and the second is what makes §B-17 countable at all.

⚠ **Spell the Trip's purpose `TripPurpose` and never abbreviate it.** `PurposeTag` is the counter-based
RNG tag policed by `BOR0801`–`BOR0803`; two unrelated concepts one word apart.

**⚠ This table needs a sink and `adr/0006` is not satisfied by *"Trips are transient"*.** A completed
Trip's Fate must reach the Census **before** the row is freed, or the only durable record of a failure is
gone; and the Fate counters are *flows*, not levels, so they follow slice 7 task 9's precedent — read as
a sum and a peak over the interval, and the reading drains them. `CONTEXT.md` → Traveller is the
constraint that makes this safe: *"a Traveller is a view, not an owner"*, and **conserved quantities live
on the Citizen record, never on the embodiment.**

**3. The walk Leg, resolved.** A mode-aware search over the foot subgraph — `TravelMode.Foot` on the
**Arc** (`adr/0072`), which is what makes this a subgraph query rather than a second network. Cost is
`distance / speed` and `03 §3.7` is unusually strong about why that needs no second tier: pedestrian
networks do not saturate, so the cheap answer is the *exact* answer. **This is the property that makes 5b
buildable before 5c**: the walk Leg needs no travel-time matrix, no route cache and no VDF, and it is
half the Legs in the city.

**4. The Trip generator.** One, chosen in *Decisions* below. It must be a real mechanism rather than a
sampler — `adr/0069`'s lesson from placement is that a **number** does not settle what a **mechanism**
settles — and it must be able to fail, because a Trip that cannot fail measures nothing.

**5. Volume on the Segment, and Phase 4.** Increment on entry, decrement on exit, per `03 §3.3` and
`adr/0041` — *"a Traveller contributes congestion to exactly the Segments it experiences congestion on"*,
and no `in_flight[origin][dest]` counter, which `adr/0041` deleted. The ADR fixes both the disposition
and the check: volume is **`(saved AND hashed)`** hot per-Tick state on the Segment table, and *"a new
invariant belongs with the definition of done: **summed Segment volume equals the number of in-flight
vehicular Travellers, every Tick**."* **Only vehicular Legs increment** — *"walk Legs still contribute
nothing"*, because `CONTEXT.md` → Fidelity keeps pedestrians out of Stress entirely. Phase 4 is
*permitted parallel* and this build runs it serially; `Phases.Runs` already states that permission is an
upper bound. **Do not write `Fidelity`.** Volume is 5b's; the threshold that reads it is 7a's.

**6. Trip Fate, the Census family and the Commute Budget.** Four Fates, `02`'s *no Trip without a Fate*
as an `O(1)` write-site invariant, and the Budget as `[trips]` Ruleset data — hot-reloadable and
**hash-bearing**, so it owes a §D row and a named ratifier the day it is written (`adr/0052`).

**7. `--trips`, and the Severance demonstration.** `--zones` and `--roads` set the precedent, including
**refusing rather than degrading** when the Ruleset declares nothing. The picture worth printing is not a
Trip count: it is a city where a neighbourhood cannot reach its shops on foot, and the same city with a
crossing added. ~~5a's acceptance test already found the shape this must respect — **Severance is a
property of the grid's fineness relative to the barrier**, and at 512-Tile blocks or two Arterials the
crossing dial does nothing at all.~~

> **⚠ CORRECTED 2026-08-11, and this task got a great deal easier and one degree harder.** The struck
> sentence has the direction backwards; the sweep is in [`0020`](0020-the-road-graph.md)'s amendment.
> Three things this task can now assume rather than discover:
>
> **The demonstration Ruleset exists** — `rulesets/severance.toml`, characterised over eight seeds, where
> the shipped `minimal.toml` strands **zero** walkable nodes on seven of eight. This task does **not**
> need to choose `[roads]` values, which §D1 forbids it from doing; it needs to load a file.
>
> **The `--roads` half of the picture is already correct.** `RoadConnectivity.StrandedOnFoot` is the
> measurement and the runner prints it, so `--trips` is not the first honest Severance instrument — it is
> the *second*, and it should agree with the first or say why.
>
> **The harder degree: `--trips` is the first thing that can measure the half nobody can measure yet.**
> Everything shipped measures **disconnection** — *can a pedestrian get there at all*. The half that
> decides whether a Building declines is **detour** — *how much further*, which is a Trip cost and needs
> the search this slice builds. So the picture worth printing is a **cost distribution over a Ruleset
> pair**, not a component count: the same city with and without crossings, and the difference in what a
> walk costs. That is also the only thing that can ratify the Commute Budget, which is a percentile of
> exactly that distribution.

**8. Invariants, hash and the long run.** 100,000 Ticks, no collection and no magnitude trending
(`adr/0006`), and — the standing warning from slice 10 task 11 — **assert that the branches under test
were actually reached.** A baseline records what a run *did*, so a change that narrows what the run
*reaches* is invisible in it by construction. For this slice that means asserting that a Trip failed, and
that a walk Leg was severed, not merely that Trips ran.

### What tasks 1–3 built — the record

**Built 2026-08-11, in a worktree beside 5a-bis, and deliberately stopping at the hash.** Tasks 1, 2
and 3 are the part of this slice that adds structure without adding behaviour to the Tick, which is
what makes them runnable in parallel with [`0022`](0022-the-lot-subdivider-and-build-road.md).

| Built | Where | Against |
|---|---|---|
| **`Address`**, and `StreetSide` | `Core/Space/Address.cs` | `adr/0074`, `CONTEXT.md` → Address |
| **`TripFate`** (closed at four) and **`TripPurpose`** | `Core/Movement/` | `adr/0076` |
| **`TripTable`**, **`LegTable`**, **`TravellerTable`** | `Core/Movement/` | `adr/0075` |
| **`WalkRouting`** + `WalkScratch` — the walk Leg, resolved | `Core/Movement/` | `03 §3.7`, `adr/0072` |
| 24 tests | `tests/Borough.Tests/Movement/` | the definition of done's first four bullets |

**What is *not* done, and it is the majority of the slice**: task 1's Building assignment (the
Access Points themselves — this built the *type*, not the per-Building rows), task 4 (the generator),
task 5 (volume and Phase 4), task 6 (the Census family and the Commute Budget as Ruleset data),
task 7 (`--trips`) and task 8 (the long run). **No `[trips]` Ruleset table exists yet**, so the
crossing cost is a required parameter with no default and nothing has chosen one.

**Three findings, and two of them are about the tools rather than about movement.**

**⚠ An `Address` is passed as one value and stored as three columns, and the hash is why.** A column
of `Address` would fold the Segment handle's **slot index**, which is identity: two runs building the
same city with different allocation histories would disagree. `HandleColumn` exists precisely to fold
the target's monotonic id instead, so every table holding an Address declares a `SavedHandle` plus an
offset plus a side and assembles the struct at the boundary. **This is the shape milestone 8's parking
Bin and 5a-bis's frontage-derived Access Points must both use**, and getting it wrong is invisible
until two saves disagree.

**⚠ `BOR0901` caught a defect on the first build that no reviewer would have been looking for.**
`TripTable` was written holding a `LegTable` reference so that `LegList` could be a property — storage
in a `[Table]` that is neither saved nor derived. The analyser named it, and the fix is the shape
`World.Occupants` already had: compose the list at the call site from the two tables that own its
columns. **This is `05 §4`'s lint 7 earning its keep in the negative direction** — the cost of the
rule is one awkward signature, and it was paid by a build rather than by a save/reload divergence.

**⚠ And a test passed while demonstrating nothing, which is slice 10 task 11's warning arriving in a
unit test.** A path's cost is the **sum of per-Arc floors** rather than the floor of the total
(`adr/0071`: *rounding is floor*, per division) — so an `n`-Arc path can sit up to `n` raw units below
a single division over the same distance. The test asserting that bound was written against the
standard fixture, where 32 Tiles at 5 km/h gives a fractional part of 0.306: **three of them sum to
0.918, the floors agree, the gap is exactly zero and both assertions were vacuously true.** Rewritten
against a 31-Tile chain, where the gap is 1 and the inequality is strict. *A bound that is never
approached is not a bound anybody has checked*, and the general form is the one this corpus keeps
re-deriving: **a test records what a run *did*, so a fixture that cannot reach the interesting case
makes the assertion invisible rather than false.**

**One thing task 1 must inherit rather than invent.** The Building-side Access Point is still
unbuilt, and the note in *Tasks* above stands: run after 5a-bis and it inherits real frontage; run
before and every Access Point it invents is one the subdivider would have derived. **The type is now
ready either way**, which is the half that had to exist before the ordering mattered.

### Decisions this slice must close

**1. What generates a Trip — and the corpus has no answer, which is a finding rather than an oversight.**
**No document defines a Trip generator.** Not `03`, not `movement-primer`, not `CONTEXT.md`. What exists
is a scatter of generators owned by other decisions: the **commute** (`CONTEXT.md` → Provider List — *"a
Provider List entry carries its Mode; how I get to work is decided when the job is taken"*), **shopping**
(`adr/0067` — *"a Household visits one provider per shopping occasion"*), **school** (`adr/0032`, and
*"roughly +50% on the commute peak"*), **dispatch** (`adr/0030`), **immigration** (`adr/0023`), **Office
export**, and **freight** (`03 §6.6`).

**The obvious choice is the commute and it is unavailable: there are no jobs.** No Office, no wages, no
labour market — `06`'s no-milestone table lists them as settled-and-unplaced. Under `adr/0070` that
absence is **unbuilt**, so it may not generate a compensating design position, and *"give every Household
a synthetic workplace"* is exactly such a position.

**The candidate that survives is shopping, and it is the one already specified down to its fields.**
[`adr/0067`](../docs/adr/0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md)
shipped the mechanism whole: *"a Household travels to a shop on its Provider List, and finding the shelf
empty is a **transaction** outcome recorded on the Household… **A failed occasion costs one Trip, not
`N`**"*, selected by a **cursor** that advances on failure and resets on success, and *"the Household
gains three small fields and no collection"*. It accepts the consequence in its own words — *"every
Household's shopping is now Trip generation, so `04 §6` is a **load on milestone 5b**"*. Buildings with
Bins exist, Households exist, placement exists. **This is the rarest starting position a task in this
project has had: a generator whose design is settled, whose failure mode is designed, and whose cost is
already booked to this slice.**

Three constraints come with it and none is negotiable. `adr/0032`: **"A Household chooses providers and
modes. It never chooses an itinerary"**, and *"mode is an attribute of a Provider List entry, not a
per-Trip decision"* — so the generator picks a destination, never a route. `adr/0025`: a Building *"may
hold Bins, one Access Point, one Parking Shed. **It may never hold a Need, money, a Provider List, or a
Trip**"* — Trips belong to Households. And `adr/0009`'s superseding note gives the general form:
**"movers choose; Rules transform"** — nearest-first selection among nearby options belongs to something
that moves, never to a Building's Rule, which is what keeps this out of the Rule engine and in Phase 4.

*Arguable* under `adr/0043`; a sitting may take it, and this brief recommends it — while noting that
`0002` §B-1, *shopping occasions per Household per Day*, is the multiplicand, is **5b's own**, and is the
number `adr/0067` says *"could force a choice between the Evidence chain and the Tick budget"*. **The
mechanism may be chosen now and the rate may not.**

**2. Where 5b's Leg model stops and 5c's routing begins — and the honest line is the mode, not the
Leg.** 5b resolves **walk** Legs completely, because `distance / speed` is exact. It cannot resolve a
**drive** Leg, because that needs a cost function over a congested graph, which is 5c. The trap is to
conclude that 5b therefore ships single-Leg Trips — that is precisely the shape `adr/0008` exists to
forbid, and shipping it *"from the first line of code"* is the ADR's own wording. **Recommendation: the
multi-Leg structure ships whole and the drive Leg ships as a named hole that throws**, on slice 6's
`pool`-scope precedent — an absence that announces itself rather than degrading into a plausible answer.
A walk-only Trip is then a real Trip with one Leg, not a degenerate model.

**3. Whether the Trip table is sized on a number that does not exist yet.** `adr/0008` says *"the Trip
table must be sized for this rather than for a Leg-per-Trip assumption"*, and §B-17 is the measurement
that has never been taken. `0002` §D1 already carries **table sizing ratios** as a *live inconsistency*
rather than merely an unratified number — `World` allocates 225 Lots and 150 Buildings per 1,000 while
the populator builds 120 of each. **Do not add a fourth guessed ratio to that row.** Size the Trip table
from what the generator actually produces at a measured rung, and record the rung.

~~**4. Whether a per-mode weight exists in the Commute Budget.**~~ **CLOSED by session F: it does not.**
The Budget is **clock minutes, one currency**, and `adr/0008`'s claim is built literally — so the ADR has
the consequence it can be held to. The objection was heard and refused on the ADR's own ground: a weight
would make the **scored** quantity differ from the **displayed** one, which is the SC4 unlearnability
failure the Budget exists to prevent. **Distaste for walking belongs to the choice model**, on a Provider
List entry's mode (`adr/0032`), one layer up from the cost function. **This slice therefore builds no
per-mode weight and owes no §D row for one** — the section got smaller by an argument rather than larger,
which is `adr/0059`'s direction. *The Commute Budget itself is still a §D2 row and 5b must not choose it.*

**5. ⚠ What a failed Trip does to the option that produced it — and 5b is the first thing that can hit
this.** The corpus holds the same sentence as both a rule and a named defect. `adr/0032` says
re-evaluation is *"an Event Wheel countdown, or immediate on a failed Trip."* `adr/0047` says that is
broken: *"`adr/0017` re-evaluates 'immediately on a failed Trip' against the same information, which
still says the same wrong thing — so a Household can choose, fail, re-evaluate and **choose the same
unreachable option for ever**."* `adr/0017` records it as **owed and unsettled** — *"what that memory
*is* — a per-Household demotion, a cooldown, or Habit's own weight moving — is unsettled… **Recorded
here rather than invented**."*

**This slice builds the failing Trip, so it meets the defect before anybody has picked the mechanism**,
and a severed neighbourhood is the sharpest possible instance: the destination is permanently
unreachable, so the loop is infinite rather than merely wasteful. **Recommendation: build `adr/0067`'s
cursor and nothing more.** It already advances on failure and resets on success, which is a demotion in
everything but name and is the narrowest thing that stops the loop. **Do not invent the general
mechanism here** — it is `adr/0017`'s to settle and `02 §9` wants a diagnostic with it. Route it, and
say in the code that the cursor is standing in.

**6. What Trips do to `adr/0053`'s failure pressure, which predicted this slice by name.** *"When Trips
arrive in milestone 5b, pressure will be integrating **three** signals of different shapes, and
*duration of the worst one* may stop being the right composition."* A Rule failure is subscription-driven
and its pressure is a duration; **a failing Trip is an event per attempt**. 5b does not have to fix the
composition, but it must not silently feed a third shape into a mechanism whose ADR says the composition
may break — so either the Trip signal stays out of Building pressure in this slice and says so, or
`adr/0053` gets its amendment. **The first is smaller and this brief recommends it.**

### ⚠ What this slice must report and must not choose

~~**5b is the named ratifier for six numbers at once**~~ — **nine, after session F, and that is the
largest concentration of `adr/0052` debt ever pointed at a single slice.**

⚠ **And three of the nine are different in kind, which is the distinction to hold.** The original six are
numbers 5b **ratifies for somebody else** and must not choose; F's three are numbers **5b itself needs in
order to run at all** — a walk Leg cannot be costed without a speed, a crossing cannot be priced without
a term, and a Trip cannot fail without a Budget. **Those three must be chosen, and each is hash-bearing
and owes a named ratifier on the day it is written.** Naming them here rather than discovering them at
the write site is the correction `adr/0069` asked for, which shipped needing three its ADR predicted
none of.

| `0002` | Number | What 5b is asked for |
|---|---|---|
| §D1 | **`walk_speed_kph`** — *needed, not ratified for another* | **Not new** — it shipped with 5a inside the free-flow speeds row. **Session F found its ratifier misordered**: that row names 5c's travel-time matrix, but a walk Leg needs no matrix, no cache and no VDF, so **5b is the first thing that produces a walk time a person can call implausible** |
| §D2 | The **crossing cost** — *needed* | **NEW, session F** ([`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md)). Report the walk-Leg cost distribution at zero and at a candidate value. **Look for the derivation first** — a crossing is a real duration, and *half a signal period* is a property of a junction rather than a preference |
| §D2 | The **Commute Budget** — *needed* | **NEW, session F.** It is a percentile of a Trip-cost distribution and is meaningless before one exists, so 5b must produce the distribution before anybody picks the value. **One number and not one per mode**, because F refused the per-mode weight |
| §D2 | **`T`**, the Habit staleness bound | a steady-state `P(stale)` and a Trip start rate — *"what turns `T` from a period into a bill"* |
| §D2 | **`k`**, Habit Route variants | R8's concentration column **and** the route cache's hit rate, which move in opposite directions |
| §D2 | The **Rejoin crossing budget** | rejoin success on real demand, since R6.4.2 measured it on an invented draw |
| §D2 | The **Aggravation threshold** | switch rate against variant occupancy, over a run long enough to reach a first switch |
| §D2 | **Habit refresh cadence** | R8.5's instrument re-run on a variant-supplied route set — the ratification withdrawn 2026-08-10 |
| §D2 | The **Microscopic Cap** | the demand half — how many Vehicles a real city stresses at once |

**And seven `0002` §B rows name 5b as their machine**, which is a different obligation: §B forbids any
document citing them as decided, so these are measurements 5b **owes** rather than numbers it must
resist choosing. §B-1 *shopping occasions per Household per Day*; §B-3 *does the route cache actually
work* — the hit rate, and therefore routing's whole Tick budget, which `adr/0047` calls *"the only
exit"*; §B-13 *how many Segments are stressed at once at 1M*; §B-15 *does the Statistical tier cost
"about 1% of a core"*, which `adr/0005` states as the reason storage was never the problem and which is
**not a row in `0013` at all**; §B-16 and §B-17 above; and §B-20, `05 §5`'s Chunk partition under
**mobile** entities — re-owned from S0b, which *"ran and could not reach it: nothing in the world
moves."*

**Five of the six §D2 numbers are gaps rather than debts** — nothing is built on them, so nothing
accretes — and that is the only reason this is manageable. The failure mode is named in `0002` itself and it is not
hypothetical: *"a number becoming a decision by being the only number in the room is a habit this corpus
has already recorded."* It has happened to the Microscopic Cap's supply half within the last two days.

**So the rule for this slice is: report the distribution, name the rung, and choose nothing.** Four of
the six also want 5c or 6 to exist before they mean anything — a rejoin budget is meaningless without
diversion, and diversion needs routing. **5b that reports six numbers has succeeded; 5b that sets six
numbers has quietly made six decisions nobody argued.**

### Definition of done

`CLAUDE.md`'s cumulative list, plus:

- Trips and Legs are in the State Hash, and replay equivalence holds across a world that travels
- Every Trip that ends has a Fate, asserted at the write site, and every Fate reaches the Census
- A walk Leg's cost is `distance / speed` over the foot subgraph, checked against a hand-built fixture
- **A neighbourhood severed by an Arterial produces *no route found* on foot, and adding a crossing
  fixes it** — with the unsevered variant kept in the suite watching itself pass
- Segment volume rises and falls with Travellers, and is zero at the end of a run in which everyone arrived
- `--trips` prints Trips, Fates and a severed component, and refuses rather than degrading
- 100,000 Ticks with no collection and no magnitude trending, **and an assertion that a Trip failed**
- §B-16 and §B-17 are reported with their rungs named, and no §D2 number is chosen

**Risk retired:** the irreversible one. After this slice a Trip is a sequence of Legs, a car commute
cannot be spelled as one Leg because the structure does not permit it, and walking has a cost the
Commute Budget scores. `06`'s statement of the risk is what to check the result against — *"a single-Leg
Trip model propagates into Lot valuation, cost functions and every balance constant, and is what
Citybound could never undo."*
