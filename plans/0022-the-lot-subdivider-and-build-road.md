# 0022 — The Lot subdivider and `build_road` (5a-bis)

> The slice brief for **5a-bis**, the follow-on [`0020`](0020-the-road-graph.md) scoped and named and
> which nothing then scheduled. It has **no milestone in [`06`](../docs/06-roadmap.md)** and it is not in
> that document's *Mechanisms with no milestone* table either — [`0012`](0012-corpus-audit.md) flags the
> gap and this brief is half of what closes it.
> Decisions built: [`adr/0014`](../docs/adr/0014-grid-streets-with-freeform-arterials.md) (the frontage
> rule), [`adr/0025`](../docs/adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md) (the
> subdivide/stack trade), [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md) (the Epoch,
> finally driven), [`adr/0039`](../docs/adr/0039-the-text-formats-are-a-fifth-project-not-a-core-exception.md) (the log format).
> Design realised: [`02 §2.2`](../docs/02-simulation-model.md), `CONTEXT.md` → Frontage, Lot, Access Point.
>
> **This is a planning document and therefore cites rather than owns**
> ([`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)). Every figure
> below names its owner. If this document and its owner disagree, the owner is right.

## Status

**✅ DONE 2026-08-11.** All seven tasks. The five decisions closed first and produced three ADRs —
[`adr/0077`](../docs/adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md),
[`adr/0078`](../docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md),
[`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)
— **and three of the five recommendations in this document were wrong**, which is recorded in place
rather than silently edited, because a brief quietly corrected to agree with what was built stops being
evidence about what was predicted. **1,073 tests green; all three golden baselines re-recorded.**

**The slice's sharpest finding is about the re-record and not about Lots.** Zoning a Tile now zones the
**block** it falls in, and eight of the golden session's eleven `zone` commands named Tiles 0–31 —
which is *one* block, and one the populator had already carved. A straight re-record would have turned
eight commands into no-ops and **retired the verb from the baseline while producing a full set of freshly
correct hashes**. That is slice 10 task 11's finding on its second outing in three slices, so it is now a
test — `GoldenSessionCoverageTests` asserts what the session *reaches* rather than what it hashes to.
**A hash test structurally cannot see this**, and the second sighting is what makes it a standing rule
rather than an anecdote.

**Three more outlive the slice.** A **guard that covers one of two identical files is worse than no
guard**: `The_golden_ruleset_is_the_one_the_session_names` checked `minimal.toml` and not its twin, so
`TunedRulesetHash` was a literal nothing held to a file — and the catalogue pairs a *stated* hash with a
*loaded* one, so the stale number resolved to the new content and nothing anywhere noticed. It had been
wrong since the file was last edited. **A derived structure that caches a Ruleset value reads as
*absent* rather than as *stale* before its first rebuild**, and absent is the state every guard is
written against: `RoadGraph`'s constructor never called `RebuildDerived`, so a replayed world with
`[roads]` and no generator run refused every `connect` with *"this world has no Street lattice"*. And
the 100,000-Tick run found that **the lay/bulldoze cycle closes but is not synchronous** — a Building
standing on the bulldozed face survives (`adr/0079`), so its Lot's freeing is deferred to whenever the
Zone Rules condemn it. The first draft asserted equality across cycles and failed at 140 against 138;
*a test that demands synchrony is testing the Zone Rule's cadence while claiming to test the
subdivider.* It also surfaced a real defect — a Lot vacated by demolition while it had no frontage was
never freed, because re-subdivision runs on a **road edit** and a Lot can be vacated on any Tick.
`World` frees it at the demolition site now, and `Invariant.VacantLotHasFrontage` is true continuously
rather than only immediately after an edit.

*Original status:* **NOT STARTED. Ungated, and available now.** Nothing in the argument track names it, its two design
inputs (`02 §2.2` and `adr/0014`) are settled and detailed, and milestone 5a shipped the Street network
it was waiting for. **It went on [`0000`](0000-board.md) as *Do these next* row 6 on 2026-08-11** — until
that day it existed only as prose inside a closed slice's *What this excludes* section, which is a
reasonable place to record a decision and a poor place to keep a task.

**It does not contend with session F**, which is the other available row, and 5b wants it done first —
see *What 5b inherits from this* below.

---

## Why this slice, and why now

**Three claims in the corpus are currently true by accident, and 5a shipping is what made two of them
false.**

**1. *Every Building is on the Road Graph by construction* — true because there was no Road Graph.**
`02 §2.2` records the admission in its own text, and `0012` restates the general form: *"a precondition
stated in a design document is a hypothesis about the build until something enforces it. **Nothing has
ever refused a Lot for want of frontage.**"* This is not a tidy-up. `CONTEXT.md` → Frontage leans on that
claim to **delete the utility network entirely** — *"because every Lot has frontage by construction,
every Building is on the Road Graph, which is what lets Utilities ride it with no second network to
draw."* A load-bearing simplification is resting on a vacuous truth, and as of 5a the truth is no longer
even vacuous: there is a Road Graph now, and nothing connects Lots to it.

**2. `adr/0025` rejected a road-derived density cap on the ground that the simulation teaches the lesson
instead — and the mechanism that teaches it is this one.** The ADR is emphatic: *"a road-derived cap
would **pre-empt the lesson the engine exists to teach**"*, and *"a player who draws sparse streets does
not get a refusal — they get **dead block interiors** and a funnel through one Access Point, and both of
those are consequences that explain themselves."* Today the player gets **neither the refusal nor the
lesson**, because block interiors do not exist: a Lot is painted wherever the `zone` command names, and
frontage is not consulted. `02 §2.2` calls this out as the design's whole point — *"this is how bad
street layouts punish the player **mechanically** rather than through a penalty number."* `LEGIBLE CAUSE`
is doing no work at all until the subdivider does.

**3. The Epoch has never been driven by anything a player did.** 5a task 3 built `adr/0012`'s per-Segment
invalidation contract — the mechanism every later consumer inherits, and the one S2 R5 measured at 96%
retention against a single counter's 9%. But the generator runs at world creation and **nothing in a
running world edits the graph**, so the Epoch is exercised by unit tests and by nothing else. `adr/0012`'s
contract is about *road addition* specifically — *"a stored route may be wrong about a Segment that was
**added**"* — and **the project has never added one after startup.** Putting road edits in the Input Log
is what makes replay drive the Epoch, which is the only way it meets the determinism harness rather than
a hand-written fixture.

**And the two halves are one slice for a mechanical reason, not for convenience.** `02 §2.2`'s last line:
*"Re-subdivision happens when the street network changes, and must preserve existing Buildings — only
vacant land re-parcels."* The subdivider needs an edit signal; the edit signal is the Epoch; the Epoch
needs an editor. **Neither half is testable alone**, and a subdivider that only ever runs once is a
subdivider whose hardest requirement is never exercised.

**Why it is not part of 5a.** [`0020`](0020-the-road-graph.md) took the decision and gave the reason: 5a
retires a risk about *the graph's uniformity* and this retires a different one about *Lots being honest*;
folding them doubles the slice and couples two acceptance tests that fail for unrelated reasons. It also
recorded that `Simulation.cs:311` already states the trade correctly — *"painting a **region** of Lots
would stand in for more of 5a than painting one does, and every Lot it invented would be one the real
subdivider would have refused."*

**`0012` carries two unchecked boxes this slice discharges** — `06` owes the mechanism a home, and
`02 §2.2` owes the same *this does not exist yet* note that `rulesets/minimal.toml` gave its own
emptiness. The sibling entry immediately below them in that file, `02 §5.2` step 2, was *"the Lot
subdivider entry above wearing a different mechanism — settled design, specified in full, owned by
nobody"*, and it was closed by building it (`adr/0069`). Same shape, same remedy.

---

## What this slice is

**Lots that a street layout can refuse, and a player who can move the streets.**

| In | Why |
|---|---|
| **Frontage**, derived from the Road Graph per Lot | `CONTEXT.md` → Frontage: *"the geometric precondition for a Lot existing at all"* |
| A **subdivider**: zoned land → parcels with frontage, land without it left unlotted | `02 §2.2`; it replaces `LotTable.Create`'s one-per-command painting |
| **Re-subdivision on the Epoch**, preserving standing Buildings | `02 §2.2`; and it is what exercises `adr/0012` |
| `CommandKind.Connect` — **applied**, not thrown | `01 §2`'s fifth verb; `Simulation.cs:332` |
| Road edits in the **Input Log**, so replay drives them | `adr/0039`; the Epoch's only honest test |
| A **vacancy reason** of *no frontage* | `CONTEXT.md` → Frontage: *"one of the four reasons a Lot is vacant"* |
| `--zones` showing dead block interiors | `06` rule 2: something to *look at*, and it is the whole point |
| Invariants, hash coverage, a long run | The definition of done |

| Out | Owner |
|---|---|
| Trips, Legs, Access Point **consumers** | 5b, and it is gated on session F |
| Routing, the travel-time matrix | 5c |
| The `pool` scope | Blocked on conserved Money and prices, which have no milestone — **not on this** |
| Terraforming, procedural terrain | `adr/0021`, unmilestoned |
| The Junction piece **library** as content | `adr/0014` — *"content with three faces each"*; this slice needs the topology fragment, not the meshes |
| Density bands as gameplay | `adr/0025`; this slice supplies the frontage arithmetic its trade rests on and no bands |

---

## Tasks

**1. Frontage, derived and rebuilt.** A Lot's contact with a Street it can take access from. **Derived**,
on the Epoch — a Lot does not save its frontage any more than an Arc saves its cost, because both are
functions of the Segments. `adr/0014`'s asymmetry is the whole rule and it is already in the graph: only
Streets grant frontage, **Arterials grant none**, and `RoadKind` distinguishes them. `CONTEXT.md` is
explicit that this is arithmetic and not policy — *"**frontage is arithmetic, not a rule.** Block
geometry decides how much land is in a parcel and whether it touches a street at all."*

**2. The subdivider.** Zoned land carved against the Street network into parcels. `02 §2.2` gives three
rules and this slice builds all three: frontage on at least one Street; depth and width targets varying
by density band, *"and the subdivider fits what it can"*; and **land that cannot be given frontage stays
unlotted and undevelopable.** The third is the one that matters and the one to write the test against.

**⚠ This replaces the `zone` verb's meaning, and it is the largest hash-moving change since the Ruleset
arrived.** `Simulation.cs:321` currently creates exactly one Lot at the command's coordinates;
`SyntheticCity.cs:126` creates a grid of them. Both become callers of the subdivider, both baselines
re-record, and `--zones` starts printing a different city. Budget the re-record, and read slice 10 task
11's warning first: **a baseline records what a run *did*, so assert that the refusal branch was actually
reached** — a golden session in which no Lot is ever refused for want of frontage covers half the
mechanism while every hash moves.

**3. Re-subdivision on the Epoch.** `02 §2.2`: *"re-subdivision happens when the street network changes,
and must preserve existing Buildings — only vacant land re-parcels."* This is the hard half. A standing
Building pins its Lot; the vacant land around it re-parcels; and a Lot that loses its frontage entirely
while occupied is a case the design does not describe — see *Decisions*.

**4. `CommandKind.Connect`, applied.** Declared at `Command.cs:18`, throws at `Simulation.cs:332` beside
`Service` and `Govern`. What it *is* is decision 1 below, and it is not a small question.

**5. The Input Log, and whether the format version bumps.** See decision 2. Whatever the answer, it is
taken deliberately: `InputLogCodec.cs` already states the rule and the cost.

**6. `--zones`, showing the thing.** The precedent is `--roads` and `--zones` itself, including refusing
rather than degrading. What is worth printing here is **a block interior with no Lots in it**, and the
same block after the player runs a street through it. That picture is `adr/0025`'s rejected road-derived
cap, shown working the way the ADR said it would.

**7. Invariants, hash and the long run.** ~~Every Lot has frontage, checked whole-world~~ ⚠ **every
*vacant* Lot has frontage** ([`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)
— the original is false under this document's own decision 4); a Building's Lot
survives re-subdivision, checked at the write site; 100,000 Ticks with road edits in the log, no
collection and no magnitude trending (`adr/0006`), **and an assertion that a re-subdivision preserved an
occupied Lot** — because that is the branch a run without demolition never reaches.

---

## Decisions this slice must close

**1. ⚠ What a road-drawing command *is* — and the corpus specifies it nowhere.** [`0020`](0020-the-road-graph.md)
says so plainly: `01 §2` counts `Connect` among the player's five verbs, the corpus calls road editing
*the player's core verb*, and **nowhere specifies its command surface.** *Arguable* under `adr/0043` — no
measurement settles it — so this slice may take it, and it must take it first, because decision 2 falls
out of it.

The shapes worth weighing, and they differ by more than ergonomics:

| Shape | Payload | Fits today's `Command`? |
|---|---|---|
| One grid-snapped Street Tile per command, painted like `zone` | `(east, north)` + kind | **Yes** |
| One Segment per command, endpoint to endpoint | two coordinate pairs + kind | **No** |
| An Arterial spline | many control points | **No**, and not in one command at all |
| A Junction piece | `(east, north)` + piece id + rotation | Fits if the piece id borrows the `zone` slot |

`adr/0014` is what makes the first row plausible rather than a fudge: **Streets snap to the grid**, so a
Street edit genuinely is a Tile-sized act and the Road Graph *"falls out of the Tile grid directly"*.
Arterials are the freeform ones and they are *"deliberately rare"*.

**Recommendation: Streets in this slice, Arterials and Junction pieces named as a hole that throws.** It
keeps `Command` at twelve bytes, it keeps the log format at version 1, and it exercises the Epoch just as
hard — an added Street is exactly `adr/0012`'s unsound edge. Splines want a command shape nobody has
argued and they are the rare case; deferring them is `adr/0070`-clean because the absence would be
*refused-for-now with a named successor*, not silently missing.

**2. Whether the Input Log format bumps to version 2 — and two remarks in this project already
disagree.** `Command.cs` and `InputLogCodec.cs` both claim the verbs' arrival is free:

> *"All four verbs are encoded, though only Zone is applied… the log format has their slot today, so the
> artefact a bug report is made of does not change shape when they arrive — **and this format version
> does not have to be bumped for their arrival.**"*

And thirty lines below that, the same file states the rule that would falsify it:

> *"**What would bump it is a change to a line that already exists**: **a sixth field on a command**, a
> second number on `citizens`, a different meaning for `seed`. Those an old reader parses happily and
> gets wrong, which is exactly the case a version exists for."*

**Both cannot be right, and which one is depends entirely on decision 1.** A verb whose payload fits
`(east, north, zone)` costs nothing; a verb needing a second coordinate pair is *a sixth field on a
command* by the codec's own definition, and the bump *"would cost every log ever written — including the
committed golden baseline."* **Nobody could have caught this**, because no verb has ever needed a payload
the four existing fields could not carry, and `Command`'s docstring pins the struct at *"twelve bytes,
fully defined"* with no padding, checked by arithmetic.

⚠ **Whichever way it goes, the two remarks get reconciled in this slice.** The claim *"does not have to
be bumped for their arrival"* is either narrowed to *"for Zone, Service and Govern"* or struck. A
sentence that is true of three verbs and asserted of four is `0012` *Cause 1* waiting to be quoted by the
next reader.

**3. Where the Access Point lives, and who assigns it.** `CONTEXT.md` → Frontage says subdividing
*"consumes frontage — narrow terraced Lots eat the available street edge to **buy one Access Point
each**"*, which puts the Access Point downstream of this slice. `adr/0025` says a Building *"has one
vehicle and one pedestrian Access Point"* and, four lines later in its invariant, *"it may hold Bins,
**one Access Point**, one Parking Shed"* — loose shorthand for the pair rather than a contradiction, but
worth tightening while somebody is here. **Recommendation: this slice produces frontage and the Access
Point's *location*; session F decides its *shape*** (the pedestrian/vehicle split is F's decision 3 in
[`0021`](0021-trips-legs-and-the-pedestrian-layer.md)), **and 5b consumes it.** Build the column, let F
say whether it is one row or two.

**4. What happens to an occupied Lot that loses its frontage.** `02 §2.2` says re-subdivision *"must
preserve existing Buildings"* and says nothing about a player bulldozing the only Street a Building
fronted. The three candidates are: the Lot keeps its Access Point and the Building is stranded but
standing; the Building starts accumulating failure pressure and declines through `adr/0053`'s existing
machinery; or the edit is refused. **Recommendation: the second** — it needs no new mechanism, it is
`LEGIBLE CAUSE` behaving exactly as `adr/0025` describes, and *refusing the edit* is the road-derived cap
that ADR rejected, wearing a different hat. *Arguable*; take it here.

**5. Lot depth and width targets by density band.** `02 §2.2` requires them and gives no values. **Two or
more hash-bearing, world-creation numbers**, so under `adr/0052` each owes a *named* ratifier and a
trigger on the day it is written, in `0002` §D. **Look for the derivation before reaching for a value** —
that is tau's precedent and `adr/0059`'s, and the corpus has twice found there was no number to choose.
The obvious candidate derivation is `block_tiles` and the density band's occupancy, both of which are
already Ruleset data.

---

## What the decisions closed

**One sitting, 2026-08-11, five decisions, three ADRs — and the sitting's own finding is that a brief
is evidence about what its author could see, not a specification.** Three of the five recommendations
above were wrong, each in a different way, and all three failures are of a kind no amount of care in
writing the brief would have caught.

| | Decision | Outcome |
|---|---|---|
| **1** | What a road-drawing command is | **Recommendation refused.** [`adr/0077`](../docs/adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md): one **Segment**, not one Tile. Streets only, Arterials and Junction pieces refused by name |
| **2** | Whether the log format bumps | **Version 1 stands**, and the two remarks are reconciled — but *not* for the reason given above |
| **3** | Where the Access Point lives | **Not this slice's to take.** Session **F** closed it |
| **4** | An occupied Lot that loses its frontage | **Recommendation refused.** [`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md): the Building stands and its Address becomes a named absence |
| **5** | Lot depth and width | **One number, and it was already in the corpus.** [`adr/0078`](../docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md). Depth deleted rather than chosen |

**Decision 1 — a quotation can be accurate and its conclusion false.** The recommendation of *"one
grid-snapped Street Tile per command"* rests on two correct quotations from `adr/0014` — *Streets snap
to the grid*, and the Road Graph *"falls out of the Tile grid directly"*. But `RoadGenerator` puts nodes
**only at grid intersections `block_tiles` apart**, so one Street Segment spans a whole block face and
**there is no such thing as a Street Tile in the graph 5a built**. The brief reasoned from a design
sentence without checking what was built under it — [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s
shape on a third axis, since `0070` governs absences and this is a **presence** misread.

**Decision 2 was right and its stated reason was not, which is the more useful half.** The brief says
the version turns on whether the payload needs a second coordinate pair. It does not need one — but not
because a Tile-sized edit is small. It is because **an origin plus an orientation names an adjacent pair
uniquely**, the grid spacing being Ruleset data, so the far endpoint is *derived rather than carried*.
Had the recommendation been implemented as written it would have reached the same version by a route
that stops working the moment `block_tiles` is not the node spacing.

**Decision 3 was closed by a session that ran after this brief was written, and the brief says so
without knowing it.** *"It does not contend with session F"* reads as independence and was a
**dependency that had already been discharged** — F settled the Access Point's shape completely in
[`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md) and
`CONTEXT.md` → Address. ***A brief's open question can be closed between its writing and its execution,
and the brief will not say so*** — which is an argument for reading a plan's *status* against the board
before taking its decisions, not for writing briefs differently.

**Decision 4 — citing a mechanism is not checking what it is keyed on.** The recommendation is *decline
through `adr/0053`'s existing machinery*, on the explicit ground that **"it needs no new mechanism"**.
`ZoneRuleEngine.Condemn` walks a Building's **Rule Instances** and asks `IsStarving`; a Building whose
Street was bulldozed **starves nothing**, so routing the case there needs a second pressure source and a
threshold — exactly the new mechanism the recommendation claimed to avoid. Same shape as
[`adr/0064`](../docs/adr/0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)'s
finding, one level up: the sentence was about the design and the answer was in the code.

**Decision 5 — the third time this corpus has looked for a derivation and found the number already
written down.** The brief predicts *"two or more hash-bearing world-creation numbers"*. It is **one**,
and it is `CONTEXT.md` → Address's *"five Buildings share a Segment"* — the premise of the decision that
keeps an Address off a Node, and therefore of the ~30,000-Segment figure the whole routing budget is
priced against. 5a's graph gives **33,024** Street Segments against that *"~30,000"* **by construction
rather than by arrangement**, so the Lot count is **165,120** — against `World`'s independently-chosen
225 per 1,000 Citizens, or 225,000 at 1M. *Two figures that never met, agreeing within a quarter.*
Depth is **deleted**: a Lot has no extent in `LotTable`, so a depth would be a hash-bearing number
chosen for a consumer nobody has designed.

⚠ **And this document contradicts itself, eight paragraphs apart.** Decision 4 asks what happens to an
occupied Lot that loses its frontage; the *Definition of done* asks for **"Every Lot has frontage,
checked whole-world"**. That invariant is **false** under every answer to decision 4 except *refuse the
edit* — a preserved occupied Lot is a Lot without frontage by construction. The definition of done is
the half that is wrong, and it is corrected below rather than deleted, because *an invariant that fails
on the correct behaviour is worse than no invariant*: it is the tier that gets disabled to ship.

## What 5b inherits from this

**5b wants this done first, which makes the ordering convenient rather than merely non-conflicting.**
[`0021`](0021-trips-legs-and-the-pedestrian-layer.md) task 1 currently has to assign Access Points
*nearest-Segment-by-construction* and say so in a docstring, precisely because the subdivider does not
exist. If 5a-bis lands first, 5b inherits **real** Access Points derived from frontage, and the walk
Leg's origin stops being a placeholder.

It also sharpens 5b's headline test. Severance is *"a part of the city made unreachable **on foot**"* —
and 5a's own acceptance work found that Severance is **a property of the grid's fineness relative to the
barrier**, so the crossing dial does nothing until there is enough Arterial per unit of grid. A player
who can *draw* an Arterial through a neighbourhood is the demonstration; a generator that places eight of
them at world creation is a fixture.

---

## Definition of done

`CLAUDE.md`'s cumulative list, plus:

- Lots are generated from zoned land and the Street network, and **`02 §2.2`'s opening sentence becomes
  true of the build** — with `0012`'s two boxes struck and `02 §2.2`'s *does not exist yet* note removed
  rather than amended
- **Land that cannot be given frontage stays unlotted**, with a test that watches a block interior stay
  empty and the same interior fill after a Street is run through it
- A road edit arrives through the Input Log, replays identically, and **bumps the Epoch of the Segments
  it touched and no others**
- ~~Every Lot has frontage, checked whole-world~~ ⚠ **CORRECTED by [`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md):
  it is **every *vacant* Lot has frontage**, whole-world, plus a write-site check that a Lot is only
  ever preserved without frontage when it is occupied. The original wording is false under decision 4
  and was written against a subdivider that only ever ran forwards
- Re-subdivision preserves every standing Building, asserted whole-world —
  `SimulationTests.A_building_survives_losing_its_street_and_a_vacant_lot_beside_it_does_not` at the
  write site, `LotLongRunTests` whole-world after 195 road edits
- ~~*No frontage* is reportable as a vacancy reason~~ ⚠ **SUPERSEDED by
  [`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md).**
  The consequence of an Address that does not exist is a **Trip Fate** — *no route found*
  ([`adr/0076`](../docs/adr/0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md),
  a set closed at four) — and there is no vacancy-reason mechanism to report into. Adding one here
  would have been a second, parallel way of saying the same thing, in a slice with no Trips in it.
  This is `adr/0070` in its usual direction: the line was written against a reporting channel nobody
  has built. **It belongs to 5b and is on that slice's inheritance list**
- `--zones` shows a dead block interior, and refuses rather than degrading
- 100,000 Ticks with road edits in the log, no collection and no magnitude trending, **and an assertion
  that both the refusal and the re-subdivision branches were reached**
- The two Input Log remarks agree with each other and with the code

**Risk retired:** that a design document's precondition is a hypothesis about the build. After this slice
frontage is enforced rather than assumed, so *every Building is on the Road Graph* is true **by
construction** rather than by there being no Road Graph — and `CONTEXT.md` → Frontage's deletion of the
utility network is standing on something. The secondary risk is `adr/0012`'s: an invalidation contract
that has never been driven by a player is a contract nobody has tested.
