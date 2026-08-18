# CLAUDE.md

Guidance for Claude Code working in this repository.

## What this is

A city-builder where the city is made of people you can actually meet, the economy is made
of Goods that actually move, and when something goes wrong the game can say exactly why.
Godot 4.7 is the host; the simulation is an engine-agnostic C# library.

## Current state

**Read [`plans/0000-board.md`](plans/0000-board.md) first on any cold start.** This section is a
pointer with just enough shape to orient; the board is the view and the slice plans are the record.
**This file does not store per-slice narrative** — it did, it became a third copy of the board and the
slice plans, and it was the copy that drifted (`plans/0012` *Cause 1*: every document that stores
per-slice status drifted, and the only large one that did not stores none).

✅ **Phase 2 is re-derived and the numbering is defined — session K, 2026-08-16.** `06`'s Phase 2 runs
**5a–5c frozen, then milestones 6 through 24**, and a **retired-numbering table** in that document makes
every citation written earlier still resolve — *old 6 → 21, 7a → 22, 7b → 23, **8 → 7**, 9a → 19,
9b → 20, **10 → 8***. [`PROCESS.md`](PROCESS.md) → *Numbering* now owns the scheme: **the integer is the
position**, a shipped milestone's number is **frozen**, inserting **renumbers the unshipped tail**, and
`-bis`, `K1`/`K2` and the sessions named *eight* and *nine* are recorded as **retired forms**. All forty
inventory rows are placed, the **dependency graph is written down** (six roots — Evidence, the District
Pool, Money, land value, the Provider List, the coarse Day wheel), and `06`'s five obligations have
**owners and triggers** instead of slots, which created sessions **R** (threading) and **T** (the target
speed). ⚠ **Three findings and one filing.** ***A partially-shipped milestone reports as shipped, so a
branch of it that throws is invisible to an inventory of unscheduled work*** — the **District Pool**
(`RuleEngine.cs:803` throws on `Scope.Pool` by name) was in none of forty rows because it lives inside
milestone 3a, which is done. **Milestone 10 was two milestones wearing one number** — Save/load in `06`,
*the Outside* on the board — so session J's clearance was recorded against the half it did not clear and
**the Outside was never scheduled**; it is now **14**. `adr/0041`'s **Segment volume row had shipped** in
5c and nobody struck it. And ⚠ **two of the six roots are half-built in a way no document says**:
`MapLayers.SetLandValueTarget` and `HouseholdTable.Money` are declared with **only test callers**, so the
two deepest debts are *producers* rather than subsystems. ⚠ ~~**21–23 are marked *position provisional***
— sessions E and G move them, and 5c prices congestion on entry to a Segment while routing on free flow,
which `adr/0046` refuses by name, so **congestion is a cost paid and never a cost avoided** and those
rows' acceptance criteria cannot be written yet.~~ ✅ **Session E ran 2026-08-16 and its half is
discharged: 22 and 23's positions are *derived*** — 21 + 1 and 22 + 1, with no other freedom, so the
cluster's position is **21's alone and 21's is session G's**. **The acceptance-criteria clause is
withdrawn**: `03 §5.1`'s three scenarios are constructed load profiles that read no route choice, and
that suite's own admission rule means one never could. ***A milestone's acceptance criteria and the
ratification of the numbers it produces are different things.*** What the missing feedback loop
genuinely costs is `T_high`/`T_low`'s **ratification**, which is measurable and was never a session's.
**Congestion is still a cost paid and never a cost avoided** — that half stands, and it is the driver
model's absence, behind **G**.

✅ **Session T closed 2026-08-16, the day K opened it — the target speed ([`plans/0027`](plans/0027-session-t-the-target-speed.md)),
three decisions and two ADRs, [`0105`](docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md)
and [`0106`](docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md).**
**The target is 4× at 1,000,000 Citizens — 15.6 ms on one core of the reference class — and no rung is
ever withdrawn**; a host that cannot sustain the chosen rung **dilates and says so**. The two-point form
the session recommended was refused with the user in the room: ***taking options away while a city
progresses is weird***, and the `Wait` step is hardest to close in exactly the city that would have lost
its fast-forward. **The Cap does not follow the target** — `adr/0096`'s revisit trigger fired, one of its
two legs fell, and the decision survived, because *a Cap and a bill are different objects*. ⚠ **The
ledger comes off worse than the decision.** `plans/0013` was **stale by ×4 on its own largest row**
against a correction it was carrying in its own sidebar (≥17.8 → **≥44–50 ms**); its **option set was a
ladder session P retired** (8× priced, 0.5× and 3× absent, quoted by three documents a month on); and
**no rung fits**, so the ledger could not have chosen one — picking the column under 100% would have
been picking where *the row known to be wrong is small*. ***A ledger says what a choice costs and never
which choice to make.*** ⚠ **Session R stops gating nothing**: 15.6 ms means 15.6 ms **of a core**, and
threading is the only named lever the size of the ~3× gap.

**Session P closed 2026-08-13 — the player model ([`plans/0025`](plans/0025-the-player-model.md)),
twenty-four decisions and seven ADRs, [`0090`](docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)–[`0096`](docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md).**
It grilled `01 §1`, `§3` and `§4`, which is **every remaining section of the document nobody had read**
while `02`–`05`, the ADRs and ~19,600 lines were built on them. The map starts **empty**; `Demolish` is a
sixth verb and clearing land is **bought rather than taken**; **1× is the design speed**; and two of `01
§4`'s markers were unsupportable for opposite reasons, giving the Commute Budget **three rungs** and the
Day **2048 Ticks**. ⚠ **Three of its decisions are decided and not built** — the clock, the rungs and
`adr/0089`'s map — and the list with owners is `plans/0025` → *What this session leaves*.

**Its sharpest finding is a rule about quotation, and the session produced it by committing the
error twice on consecutive turns.** ***A caveat attached to a number does not travel with it*** —
`plans/0012` **Cause 5**, the one failure in this corpus whose two copies **agree to the last digit**.
`adr/0094` built a 27–58× Microscopic Cap gap on a figure `plans/0013` disclaims **in the cell**; the
correction then quoted the *caveat* out of its context and was wrong in the other direction. Both are
withdrawn ([`adr/0096`](docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)),
and what survives is machinery: **check 6**, a **disqualifier registry** of seven figures that a test
holds every prose document to, and the rule ***carry the bill, not the percentage*** — a share of a Tick
budget is a measurement over a **product decision**, and `plans/0013`'s ~~≥229% / ≥114% / ≥57% / ≥29%~~
**283–318% at the settled target** is one bill — ~~≥17.8 ms~~ **≥44–50 ms**, re-summed 2026-08-16 —
read at several rungs. The registry caught its own author within the hour.

**5a-bis shipped 2026-08-11 — the Lot subdivider and the road editor
([`plans/0022`](plans/0022-the-lot-subdivider-and-build-road.md)), all seven tasks, 1,073 tests green
and all three golden baselines re-recorded.** Zoning a Tile now zones the **block** it falls in and the
block is carved against its four Street faces; `CommandKind.Connect` lays and bulldozes **one Street
Segment** and re-subdivides what it touched; frontage and the Access Point are `(derived AND rebuilt)`
on the Epoch; and a `[lots]` Ruleset table states **one** number, `lots_per_segment = 5`, taken from
`CONTEXT.md` → Address's own *five Buildings share a Segment*. Three ADRs:
[`0077`](docs/adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md),
[`0078`](docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md),
[`0079`](docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md).
~~**The code track now holds only 5b.**~~ ~~**It holds two rows as of 2026-08-12**~~ **It holds one:
milestone 5b-bis.** 5b closed the same day; see the next section.

**Milestone 5b shipped 2026-08-12 ([`plans/0021`](plans/0021-trips-legs-and-the-pedestrian-layer.md)) —
tasks 1, 2, 3, 5 and 7, 1,136 tests green, all three golden baselines re-recorded.**
`Borough.Core.Movement` holds the three tables, `WalkRouting` (a real Dijkstra over a binary heap), and
**`TripEngine` in Tick phase 4**, which had been an empty method since the Tick was written. A Trip enters
through the Input Log on `CommandKind.Populate`'s precedent
([`adr/0080`](docs/adr/0080-phase-4-does-not-wait-on-a-trip-generator-and-a-trip-is-entered-by-command.md)),
because **the §A sitting found that no milestone in Phase 2 ever produced a Trip destination**: all seven
generators the corpus names sit in `06`'s *Mechanisms with no milestone*, so tasks 4, 6 and 8 — each of
which measures a *distribution* — left for a new milestone **5b-bis**
([`adr/0081`](docs/adr/0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md)),
jobs and the commute. **5b-bis's first task is not the generator**: there is **no query from a place to the
Buildings near it** anywhere in `Borough.Core`, and all three candidate generators needed one.

**5b's sharpest finding is that one of its own consequences turned out unbuildable, and the reason everybody
had was not the one that binds.**
[`adr/0041`](docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)'s Segment volume
was expected to be *"nearly vacuous — `adr/0041` increments on **vehicular** Legs only, and 5b has none"*.
True, and it predicts that a vehicular Leg would fix it. **It would not**: that ADR's own amendment requires
*"a **next Segment** every Tick"*, and [`adr/0075`](docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md)
gives a Leg **a cost and no path**, with no next-hop table anywhere. Volume therefore waits on a **path
source**, which is 5c. ***A decision that removes a representation defers every decision that reads it***,
and neither ADR said so — `adr/0075` issued a write to `adr/0041` it did not know it was issuing, which is
`plans/0012` *Cause 2* running backwards. The columns and the conservation invariant exist; nothing
increments them. **Two smaller ones.** The State Hash **version byte's rule had been decided twice in
silence** — the clock bumped `World.HashSeed` and the five tables 5a and 5b appended did not, and the
distinction (*existing state re-composed* against *new state, which is a design change under `05 §4`*) was
written nowhere; *a precedent set silently is a precedent nobody can follow.* And a **vacuously-satisfied
invariant shipped on purpose**, against slice 5 task 7's precedent of withholding one: the difference is
between an assertion whose *shape* is wrong until the world changes and one that is **correct and
temporarily trivial**, which becomes load-bearing with no edit.

**The slice's sharpest finding is about the re-record, not about Lots — and it is the second sighting
in three slices.** Eight of the golden session's eleven `zone` commands named Tiles 0–31, which is
*one* block, and one the populator had already carved. A straight re-record would have turned eight
commands into no-ops and **retired the verb from the baseline while producing a full set of freshly
correct hashes**. That is slice 10 task 11's *a baseline records what a run did, so a change that
narrows what the run reaches is invisible in it by construction* — so it is a test now rather than a
paragraph: `GoldenSessionCoverageTests` asserts what the session **reaches**, which no hash test
structurally can.

**Three more outlive the slice.** **A guard that covers one of two identical files is worse than no
guard**, because the file it covers is evidence somebody thought about it: the ruleset-hash check
covered `minimal.toml` and not `minimal-tuned.toml`, so `TunedRulesetHash` was a literal **nothing held
to a file** and had been wrong since the file was last edited — the catalogue pairs a *stated* hash with
a *loaded* one, so a stale number resolves to the new content in silence. **A derived structure that
caches a Ruleset value reads as *absent* rather than as *stale* before its first rebuild**, and absent
is the state every guard is written against. And the 100,000-Tick run found the lay/bulldoze cycle
**closes but is not synchronous** — a Building on the bulldozed face survives, so its Lot's freeing waits
on the Zone Rules; *a test that demands synchrony is testing the Zone Rule's cadence while claiming to
test the subdivider* — plus a real defect: a Lot vacated by demolition while unfronted was **never
freed**, because re-subdivision runs on a *road edit* and a Lot can be vacated on any Tick.

**Session F ran 2026-08-11 and milestone 5b is unblocked — the movement track is open and no session gates
a slice any more.** F grilled [`adr/0008`](docs/adr/0008-walking-is-a-simulated-leg.md), *walking is a
simulated Leg*, in one sitting: seven decisions, an amendment in place, and three ADRs —
[`0074`](docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md) *side of
street is a property of the Access Point*,
[`0075`](docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) *a Leg is a plan and a Traveller is
a cursor*, [`0076`](docs/adr/0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md)
*the Trip Fate set is closed at four*. `CONTEXT.md` gained **Address** — `(Segment, offset, side)`, the
value every *where is it* query takes, of which an Access Point is a Building's — and **`Node`**, which was
load-bearing in five places with **no entry at all**. **It closed neither of the two measurable claims it
was forbidden to touch**, and it made §D *smaller* by refusing a per-mode Commute Budget weight.

**F's findings are about how a consequence is written, not about walking.** `adr/0008` asks for
*"sidewalk edges alongside street edges, **and** crossing edges at junctions"* — and the two halves have
**opposite fates**: the first is refused by `adr/0072`'s mode mask, the second is correct, is what
**Severance** turns on, and **5a already built it**. The brief proposed replacing the whole sentence,
which would have deleted the true half. ***An `and` in a consequence is two consequences.*** Three more:
**a revisit trigger can be spent before it is written** — *one pedestrian edge per block face* was already
what the graph is, so the ADR's only stated lever never existed and no later diligence could have caught
it; **a placeholder whose value sits inside the range of legitimate answers cannot announce itself**, and
this one *paid the player for the shortage*, since a zero-length parking walk is what a garage genuinely
produces and a vehicle Access Point reachable as a fallback from an exhausted Parking Shed makes a full
car park cost **less** than an empty one; and **side of street was nearly surrendered by collapsing two
questions into one** — whether it is *modelled* is independent of whether it is *in the graph*.

**Phase 2 has started: `06` milestone 5a, the Road Graph, shipped 2026-08-11**
([`plans/0020`](plans/0020-the-road-graph.md)). Nodes, Segments and a directed Arc adjacency are typed
tables in the State Hash; the Arc carries a **mode mask**, which is what makes `03 §3.7`'s **Severance**
a mechanism rather than a paragraph; a per-Segment **Epoch** implements `adr/0012`'s invalidation
contract; a generator ported from the S2 harness runs off a new **`[roads]`** Ruleset table; union-find
gives **per-mode connectivity components**, which is the deliverable the `pool` scope consumes; and
`--roads` prints the graph. **1,060 tests green, both golden baselines re-recorded.** Two ADRs:
[`adr/0071`](docs/adr/0071-travel-time-is-sub-tick-and-q16-16-is-a-scale-rather-than-a-meaning.md) and
[`adr/0072`](docs/adr/0072-the-mode-mask-is-saved-on-the-arc-and-the-segments-is-derived.md).

**The slice's sharpest finding is about porting, not about roads.** The spike's `Modes.cs` argues at
length that the mask must be **per direction** — one-way streets are the entire reason — and its
`RoadGraph.cs` then saves the *Segment's* mask and derives the Arc's from it, backwards from its own
argument. **The spike never generates a one-way street, so no measurement it ran could have noticed.**
*A benchmark cannot refute a claim about a case it never constructs*, which is `adr/0043` seen from
underneath: porting reasoning and porting code are different acts, and the second does not verify the
first. Two more outlive the slice. **Severance is a property of the grid's fineness relative to the
barrier** — ~~an Arterial destroys the Streets it runs over, so on a coarse lattice everybody walks
round the end of it, and the crossing dial does **nothing at all** until there is enough Arterial per
unit of grid.~~ **⚠ the finding stands and the *direction* was measured backwards on 2026-08-11**: a
240-configuration sweep puts severance **monotone increasing** in block size, so the shipped 32-Tile
lattice severs **0.0%** at every dial value and it is the **coarse** end where the dial bites. The
brief generalised from two observations differing in two variables and read the confound the wrong
way; *`foot_crossing_every` states a ratio and what reconnects a city is an absolute count of
crossings kept*. Three things fell out of that re-measurement, and each is larger than the correction:
**`--roads` announced Severance over a city that has none for the whole slice**, because its verdict
compared component *counts* and 65 of the shipped world's 66 foot components are motorway junctions —
`RoadSeveranceTests` had diagnosed this on day one and left the fix in the test file, so *a predicate
discovered in a test and left there is a correction with a blast radius of one call site*;
**`foot_paths_per_thousand_blocks` is a second, stronger dial nobody named**; and **every Severance
figure in the corpus was a single draw**, because `--roads` refused `--seed` — *a generator whose
output cannot be varied cannot be characterised*, and the guard that stopped it being varied was
written to refuse something else. `rulesets/severance.toml` is now the rung where the dial works, and
[`plans/0020`](plans/0020-the-road-graph.md)'s amendment carries the tables. And **a wholly-derived
table cannot join `World._tables`** — `Rows.Fold` folds the allocator's four scalars before consulting
any column's disposition, so such a table would hash its own rebuild count and two identical cities
would disagree.

**Phase 1's code is closed, and the Phase 1 code track is empty** — `0003`'s hash-moving queue emptied on
2026-08-10, **reopened the same day** with session N task 2's
[`adr/0068`](docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)
and [`adr/0069`](docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md).
A Building's occupancy is declared by its **kind** and derived from the Ruleset in force; an
over-capacity Building **evicts** rather than draining, because a Bin has a consumer and occupancy has
none; and **placement is a mechanism of its own** — `World.Place` had exactly **one** caller, inside the
Zone Rule's create predicate, so of `02 §5.2`'s six steps only step 5 existed and the city could house
somebody only by building them a house. **Both shipped 2026-08-11**, and item 5 was the first entry in
that queue that built a **mechanism** rather than correcting one — a `[placement]` table in the Ruleset,
a sampled Phase 6 pass ahead of the Zone Rules, and a fourth Census metric family.

**The finding is a vacancy of 45%, and the corpus spent a slice calling it *five-sixths homeless*
instead.** The defect was that **nothing in the simulation could move a Household into a Building that
already stood** — `World.Place` had one caller, at construction — so 45% of the housing stock sat empty
while 70% of the population queued outside it. **That is now 10%**, which is the floor a city that is
continuously building carries, and it is what `PlacementLongRunTests` asserts.

**The homelessness figure was mostly a property of the fixture, and quoting it buried the lede.**
`rulesets/minimal.toml` gives every dwelling an `upkeep` Rule drawing on a Resource **nothing in the file
produces**, so every Building is condemnable 64 Ticks after it is raised and the Zone Rule's one-Lot
sample rebuilds at the rate it demolishes. The city therefore holds **~60 of ~120 Lots** at equilibrium —
180 places for 360 Households — and homelessness is just `1 − capacity ÷ population` over knobs that file
chose on purpose, and says so in its own header at length. 83% became **53%** and the rest is arithmetic,
not a design problem. **Placement works**: `PlacementTests` drains a Pool to **zero** on a fixture that
does not demolish itself. *Everybody is housed* is a property of a Ruleset's **balance**, and no Ruleset
that models a city exists yet.

And the pass needed **three** hash-bearing numbers where its ADR predicted none: `adr/0059`'s precedent derives the *sample* and leaves
free the **duration** it is derived from, plus `candidates`. `revisit_ticks` shipped at one Day, copied
from that default, and one Day is how often the *development industry surveys the city* — a family
without a home looks more often than that; it is **1024**. **`adr/0070` runs forwards as well as
backwards**: a mechanism that does not exist cannot be predicted from outside either.

The rest of this section describes the state before those two. Slices 0–10 are **all done** — **slice 8, hot
reload, closed last**, and with it `adr/0015`'s acceptance test: *change a production ratio and see the
effect in seconds* reads **0.70 s**, against the 60–120 second warm rebuild the ADR was written on. **No
gate is red anywhere in the corpus.** Session **N** then put four items back on the code track — the
hash-moving queue in `plans/0003` — and **all four have shipped**. `adr/0063`: `adr/0033`'s
satisfiability invariant, specified in three documents and built in none, and the wake predicate it found
broken in the committed golden baseline within minutes. **Slice 10 task 11**, `adr/0059`'s
`ks`: a `[[zone_rule]]` authors a **duration** and the engine derives
`sample = ceil(Lots × interval ÷ ks)`, so at 1M the shipped Ruleset raises **2,898 Buildings**
where it raised none, and the Tick got **8% cheaper** doing 117× the Lot evaluations. And `adr/0064` +
`adr/0065`, **one commit** because they change the same two columns: a Bin's capacity is
`derived AND rebuilt` from the Ruleset in force rather than frozen when the Building was raised, an
over-full Bin **drains rather than clamps**, and a Bin's `level` and `capacity` are `long` — the corpus
was holding **two widths for one quantity**, since `Money` is a `long` and a Bin's level was an `int`, so
every payment into a Bin narrowed 64 bits to 32.

**That last commit's finding is an ADR being wrong about the code, and the cause is a missing test.**
`adr/0064` recorded a live defect on the ground that `RulesetLoader` refused nothing for a duplicate
`(kind, Resource)` Bin declaration. It has refused it since slice 7 task 8 — and that refusal was the
**one guard in the loader with no test**, so the suite you read to find out what the loader refuses did
not name it. **`plans/0012` *Cause 1* on a different axis**: not a second copy of a fact drifting from
the first, but a fact with **no copy at all**, re-derived wrongly from the shape of its absence. *A guard
with no test is invisible to every future reader, including the one about to decide it does not exist.*

**The finding task 11 leaves behind is about the baseline, not the sampler, and it generalises.** A
derived sample of 1 at the golden fixture's 132 Lots never lands on a Lot demolition has cleared inside
eight triggers, so the committed trace **silently stopped covering the Zone Rule's create branch** —
every hash moved, every test passed, and half the mechanism went uncovered. **A baseline records what a
run *did*, so a change that narrows what the run *reaches* is invisible in it by construction.** The
session is now 2,048 Ticks and a test asserts both branches ran.

**Spikes:** S4, **S0a**, **S0b** and S2 R0–R8 have all run, and **R7's tail closed 2026-08-11** — the
root `performance` capture, a canonical R8 re-capture, and the bookkeeping. The only act left of S2 is
**deleting its 33,000-line harness**, and R7 found the two gates holding it — session **M**'s
invalidation contract and the question R6.3 put in front of it — **both cleared elsewhere**, into
`adr/0012` and `adr/0061`, without either clearance reaching S2's plan. **That deletion is ⚠ STILL BLOCKED (2026-08-11), and it is
on its *second* gate rather than its first.** The 5a gate — `spikes/S2.Routing/Graph/` being the
reference implementation of milestone 5a, so the deletion waits for *the port to be done and nothing
to read the harness* — is **discharged**: 5a shipped, no project compiles against the harness, and the
only references left are two doc-comments naming file paths. **The gate now is that another session is
doing further research inside it**, so it is live work. Do not delete it, and **do not read the first
gate's clearance as the second's** — a deletion held twice for unrelated reasons is the row that gets
struck when the wrong one clears. It is **51 tracked C# files and 29,719 lines** (92 files and 42,914
lines counting the `results/` reports), not the 33,000 named above. **Sessions A, B, C, M, D, eight and nine are closed.**

**What runs today.** Typed tables with a per-field saved/derived declaration and a State Hash; a
deterministic eight-phase Tick; an Input Log that replays to identical hashes; a crash artifact that
replays back into its own crash; Map Layers with diffusion; build-time analysers that make the
determinism rules compiler errors; and **two Rule families** — Bin Rules moving Goods atomically with
wait lists and fallback chains, and Zone Rules building and condemning on a sampled Lot grid. **A Road
Graph** — Nodes, Segments, a directed Arc adjacency with a mode mask, Lots carved against Street faces,
and an Access Point on every Building. **Two of the eight Tick phases are still empty**, and there are
no jobs, money, traffic or renderer. **Movement is built and unwired**: `Borough.Core.Movement` holds
`TripTable`, `LegTable`, `TravellerTable` and a real Dijkstra, none of it in `World._tables` and
nothing in the Tick calling it — Phase 4 is still `_ = tick`. That is 5b task 5's whole job.

**The three numbers to hold in your head.** S0b measured a Tick *with work in it* at **8.72 ms at 1M —
55.9% of the budget at 4×**, and that is the only Tick figure ever taken from a real running city.
⚠ ~~[`plans/0013`](plans/0013-tick-budget.md) sums the ledger to **≥17.8 ms a Tick**~~ **RE-SUMMED
2026-08-16 by session T: the ledger is ≥44–50 ms a Tick, of which 37.6–42.0 ms — 85% of the whole bill —
is routing.** The routing row never took `adr/0094`'s ×4 (route searches fire per Trip, Trips are daily,
the Day went 8192 → 2048), and the correction was stated in `01 §1`, in this file, **and twice inside
`plans/0013` itself** — so ***a correction attached to a number does not travel with it any more readily
than a caveat does***, `plans/0012` **Cause 5**'s third form, and **the only file that was wrong is the
one that owns the sum**. It is still a row whose unit came off a synthetic harness and whose
multiplicand R6.3 found counts **the wrong event**. ⚠ ***Carry the bill, not the percentage***:
≥44–50 ms **on one core of the reference class** is a fact about the code, and **283–318% at 4×** is
that one fact over a product decision — now a **settled** one (`adr/0105`), so the target rung is
15.6 ms and the gap is **~3×**, which is a target with a number and not a defect. **The only named lever
that size is threading**, which is session **R**'s and is why R stopped being a row that gates nothing.
A percentage hides which side moved, which is how the figure below survived two changes to its own
denominator. And the correction with a known direction points *up*: a diverting Traveller
re-searching costs **861.87% of the 15.6 ms budget at 4×** at target scale — a bill of **134.135 ms**,
and carry the bill rather than the percentage, because `adr/0096` and `adr/0094` have since moved both
terms in opposite directions and it is now **right by cancellation** — which is a design question
(`03 §5`, session **D**) and not an algorithm.

**The direction of surprise has been consistent**: every time a fixture was replaced by a real world,
the number came in worse — the Rule unit by 2.8×, Trips/Tick by 32%, the Zone Rule `sample`
dimensionally wrong at scale, and every pre-S0a Tick figure taken over an empty world. **A unit cost is
a hypothesis until a real world has produced one.**

**Where the reasoning lives.** Five files, five questions, one each: [`0000`](plans/0000-board.md)
*what is next*; [`0003`](plans/0003-build-plan.md) *what is done*;
[`0002`](plans/0002-open-questions.md) *what needs answering*, every entry typed *measurable* or
*arguable* per `adr/0043`; [`0012`](plans/0012-corpus-audit.md) *what a document says wrongly*, which
is a correction and not a question; [`0013`](plans/0013-tick-budget.md) *what a Tick costs*. When the
board disagrees with any of them, they win, and **an open question is never written on the board** —
that is how it once held 63 of them while `0002` held none.

**A gated slice must not be started before its gate clears**, and several decisions on the critical
path are still open, so do not write implementation code beyond the current slice unless asked.

## The corpus, in numbers

Roughly **28,000 lines of prose** — `docs/` design documents, 75 ADRs and `plans/` — against **~19,600
lines of simulation** and **~17,000 lines of tests**, plus a **33,000-line spike harness** awaiting
deletion. The ratio is known, is on the board as a standing concern, and is why the board's rule is
*an argument session runs when something concrete is blocked on it, never because it is available.*

<details>
<summary><b>Historical: the per-slice narrative this section used to carry</b></summary>

Kept collapsed rather than deleted, and **every fact in it is also held by the board's *Done* section,
the slice plans and `docs/spike-results.md`** — which is precisely why it was removed: it was a third
copy with no unique content, and it drifted. Delete this block once you have satisfied yourself of
that; git holds it either way.

The repository is ~7,000 lines of design
documents and 54 ADRs, plus the first four slices of `plans/0003-build-plan.md` — the scaffolding,
spike S4, the arithmetic substrate, the analysers, and the typed tables with the per-field
declaration and the State Hash — and all eight tasks of slice 5: `step(inputs)` with the
eight phases, the command model and the Input Log, replay, the golden-hash baseline, the
headless runner with `Borough.Formats`, the three invariant tiers, the Census with
`series(metric, window)`, and the crash artifact. `dotnet run --project src/Borough.Headless` prints
a table report and a hash; `--log PATH --ticks N --hash-every N` replays a session and prints a hash
trace; `--census` adds what every collection did over the run; a panic writes a crash artifact that
the runner accepts back wherever it accepts a log.
**Slice 6, Map Layers ([`plans/0009`](plans/0009-map-layers.md)), is closed — all ten tasks — so
nothing in the code column stands in front of the Phase 1 gate.** `Borough.Core.Space` has the Cell
grid with the Cell and the Chunk as **two types**, the sparse `LayerCellTable` — the project's **first
`Buffering.TwoCopies`**, which made slice 4's declared-but-unimplemented double buffer real — the
separable integer convolution, the staggered schedule as a table, incremental re-diffusion that is
**bit-identical** to a full recompute, the three real Layers, **named holes that throw** where
Fertility, Desirability and the line-source queries will go, and `layer_cells(aabb, layer)`, the
project's first hot query — allocation-free and string-free, both checked. **Superposition is exact
over twenty sources and the in-place variant is kept in the suite watching itself fail.**
`dotnet run --project src/Borough.Headless -- --layer pollution` prints a field, which is the first
thing here that is not a number. **Slice 10 added the second: `--zones` prints the Lot grid before and
after a run of sweeping, so a city visibly thins out** — and it refuses without a `--ruleset` rather
than degrading, because an unchanging grid would read as a broken mechanism instead of as a file that
declares no `[[zone_rule]]`.
**Slice 10, Zone Rules, is closed — all ten tasks.** The second Rule family runs in Tick phase 6: a
Zone Rule samples Lots rather than sweeping them, builds on a vacant permitted one somebody in the
**Unplaced Pool** would take, and condemns an occupied one whose Building has been **starved of an
input** for longer than its kind allows — `adr/0053` making pressure a **duration**, amended twice by
the code itself. `adr/0054` sends a demolished Building's Households to the Pool with their money
intact; `adr/0055` scopes a permission set to what a Rule *builds* and never to which Lots it looks
at, so there is no immortality by paintbrush. **Four findings outlive the slice.** The growth cycle
**cannot be entered from a standing start**, which is why the shipped Ruleset makes dwellings decline.
The tripwire reads **1.56×** over a 1,000× Zone against a control that moved **989×**, so `02 §5.7`'s
*constant cost regardless of Zone size* is **false in the letter and true in the substance** — the
sweep is `O(sample)` exactly and the variable is the **working set**, which is the third sighting of
scatter ≈1.5 after `0011`'s findings 42–43. The 100,000-Tick run discharged `adr/0006`'s **collection**
half for the first time — five of six tables dead flat across continuous demolition, the sixth a
**running maximum** bounded structurally by the population — and found the city settles **five-sixths
homeless**, because demolition evicts a Building's whole occupancy and creation rehouses exactly one:
**a Building has no declared occupancy at all**, filed to `0002` §B rather than tuned. **Both halves are
built as of 2026-08-11** (`adr/0068`, `adr/0069`), and the filing was wrong **twice**: it named a number
where a mechanism was missing, and it led with the homelessness figure when the fixture fixes that by
construction. **The number that was evidence of a defect is the 45% vacancy** — places existing with
nobody able to reach them. And closing
task 8 was an audit rather than a change: **`HouseholdHomeExists` was reported by nothing**, the only
orphan among 26 invariants (**35 as of 5a-bis**), now bannered with its **id retired rather than reused**, because a crash
artifact carries the number. The slice's owed decision is settled *by measurement*: `adr/0044`
makes the diffusion cadence **hash-bearing**, the **sixth claim in the corpus measured false and the
first outside S2**. **That ADR then got its own second half wrong by argument** — it filed the cadence
as world-creation-fixed while citing `adr/0015` without running the membership test `adr/0015` states,
which the cadence fails and the kernel radius passes. Withdrawn and recorded rather than amended away;
**citing an ADR is not applying it**. On the parallel spike track, **S2 R0 through R5.5 are done** — the synthetic
Road Graph and the denominator, the travel-time matrix, which **carries the choice loop**, the path
source, which **revived the DSDV case rather than retiring it**, HPA\*, which **narrowed the
pathfinding cluster to 8 or 16 Chunks a side without closing it, weakened its own standing** to 2.63×
the flat search once a route has to come back with arcs, and found that **no cluster size fits routing
into the Tick budget** — which promotes R6's route cache from a tidy-up to load-bearing — and then
distance-vector, which is **out because it costs 2.13× the rebuild it exists to avoid** and is beaten
by a scheme the plan never named (dynamic subtree repair, **4.71 ms** against **234.74 ms**). R4 also
found that **S2's uniform origin-destination draw had been hiding a conclusion**: it is the
longest-trip distribution available, and on a local-trip draw a District-granular route's detour goes
from R2's 18.52% to **128.82%**, which under `05 §4` is a different city. The draw is now a swept
family, which is what makes R6 runnable at all. **R5, the edit storm, is done through R5.5** — it fired a
tripwire (a single-counter Epoch *is* a global flush), found **no Epoch rung both affordable and correct**
across the whole core verb because addition is monotone-*improving* and per-Segment structurally cannot
notice it, and then measured the way out: a **TTL rotation** at 0.40 forced refreshes per Tick clears the
wrongly-valid count 38 → 0 while retaining 97.08%. It also **retired the shared District route on a number**
and established that the two survivors are wrong in **different currencies** — structural against temporal —
which is session M's question and not a benchmark's. **The whole spike so far ran on a frozen cost basis**:
nothing in S2 has ever invalidated a route because a road got *busy*, only because one was bulldozed.
`adr/0046` settles the structure that fixes this — **Habit, Sight and Temperament**, which is `adr/0017`'s
satisficing rule reaching the one actor class nobody had applied it to — and sets no parameter.
**R8 then measured it and all three layers survive**: `03 §3.4`'s self-correction closes on the local
layers alone (Sight settles **42.62%** below a control under sustained demand), so **static Habit holds
and there is no refresh cadence to argue about**; the Sight Horizon's floor is **1 Segment**, derived
from the graph; Temperament damps by **92.28%** where a herd exists. **R8's largest finding is none of
those.** *The network runs out of routes, not road* — **87.25% of traffic on 1% of the carriageway,
90.87% of it empty, at 13% of holding capacity, with capacity confirmed realistic** — because one
free-flow tree per District means one route per (node, District) pair in the whole model. That is
**decision 11 on a different axis** and it is now the top item on the board. `spikes/S2.Routing/` compiles the arithmetic substrate in by source
and can name nothing else of `Core`, so it has changed no simulation code. Slice 5 task 7 shipped its instrument and **not** its trend assertion — nothing in
the world churned yet, so the assertion would have been vacuous. **Task 10a discharged it**, in the
flow half that is the only half the Rule engine could carry.

**Slice 7 task 9 gave the Tick its first measured price.** `02 §4`'s two counters exist, plus a third
— *due Rule Instances* — because `evaluations − due` is what chain walking costs and neither number
alone separates a bigger city from a less stable one. **The counter the section names could not have
failed the tripwire the section states over it**: an evaluation was a due Rule Instance, which a chain
walk does not move by one. An evaluation is now one atomicity check, and they reach the Census as a
**second metric family** — a table counter is a *level*, these are *flows*, so each is read as a sum
and a peak over the interval and the reading drains it. Measured: **82.84 ns an evaluation**, flat to
1.8% across two orders of magnitude, so **15.6 ms holds ~188,000**. A chain rung's marginal cost is
**53.6 ns**, two-thirds of the head that failed, which is the first evidence behind `02 §4`'s claim
that depth is not the cost driver — and it retires session B's withdrawn depth cap by pointing the
other way. **The uncomfortable half**: against `0002`'s own unratified 450 Rule Instances per 1,000
Citizens and `02 §4.3`'s rate of 8, a 1M city spends **~10.5 ms a Tick on the Rule engine alone — ~67%
of a 15.6 ms budget** — a whole-Tick figure that supersedes an earlier Phase-2-only estimate of 60%, and still a
floor. Filed to `0002` as **S0b's**, and summed with every other priced consumer in
`plans/0013-tick-budget.md`, **which prices the same rows against 1×, 2×, 4× and 8× — because the
speed multiplier is a product decision nobody has argued, and the whole simulation as priced fits at
2× and does not fit at 4×.**

**Slice 7 task 10a closed the slice, and the simulation does something for the first time.** A Ruleset
now reaches a `World` — `Replay.Start` takes one, the runner **loads** what it previously only hashed,
and `World.CreateBuilding` is the door that gives a Building its kind's Bins and arms its chain heads,
so `CONTEXT` → Bin's *"a Building is given exactly its kind's Bins when it is built"* has an
implementation rather than a test writing the loop by hand. The arming stagger was expected to be a
hash-bearing number needing a ratifier under `adr/0052` and **turned out to have no number to choose**:
a Rule re-arms at `+rate` for ever, so uniform over `[1, rate]` is the only offset that stays spread —
derived, not chosen, and `adr/0052` earning its keep in the negative direction. `rulesets/minimal.toml`
is the content and **says in its own header that it models no city**; the golden session **adopts it
and opens with `populate`**, so the committed trace covers the Rule engine, the Bins, the Wheel and the
populator for the first time, and all three baselines were re-recorded. **Two findings outrank
everything else.** The first Ruleset written **deadlocked in about two hundred Ticks** — every Bin
full, every Rule failed on headroom, every Rule subscribed, nothing left that could drain a Bin to
wake one — which turned the planning claim *sustained churn needs a sink* from an argument into a
measurement, unprompted. And **the shortage regime was not expressible**: a recorded shortfall is the
deficit at the instant of failure and the wait list wakes on the **arriving quantity**, so a consumer
short of three is never woken by three arrivals of one and both parties sleep for ever with the Bin
full. That is why the shipped Ruleset runs in **surplus** — the Rule that fails is the producer, on
headroom — and it was filed to `0002` §C as a fairness question rather than a bug, with **`pool` as its
trigger**. **`adr/0063` has since fixed it and the filing was wrong twice**: it is two bugs rather than
one question, and `pool` was not the trigger — the defect was live in the committed golden baseline all
along, on the headroom side. The flow half of slice 5 task 7's trend assertion ships as **exact equality across the
tail** rather than a trend line, because the Ruleset's period is known.

**Task 10a then made the first in-situ Tick capture possible, and every Rule-engine price in the
corpus turned out to have been taken in a laboratory.** A 1M world with a Ruleset in force costs
**~6.4 ms a Tick at 11,586 due Rules — 552 ns each, against a synthetic 198.3.** The gap is
attributed rather than shrugged at (findings 42–43): **terms ×1.84, scatter ×1.49, population ×1.14**,
a product of **3.13×** against an observed **3.70×**, with terms the largest axis and the one nobody
expected. Two consequences outrank the number. `0013`'s Bin Rule row was **right by cancellation** —
a unit cost 2.8× too low times a multiplicand ~5× too high — **which is worse than being wrong**,
because any change moving one factor without the other would have gone unnoticed. And the published
tripwire moved **3.3× onto the corpus's own worked example**: the engine fits below a mean Rule rate
of **~15.9 Ticks at 4×** where the retired wire said 4.8, and `02 §4.3`'s bakery runs at **8**. The
general lesson is now written into `0013`: **its organising column is *measured multiplicand* against
*guessed*, and it always assumed the unit side was solid — a unit cost is a hypothesis until a real
world has produced one**, and routing's 10.37 ms has never met a world either — and S2 R7 has since
found it is a **maximum with a 9.4–10.5 ms spread**, whose multiplicand R6.3 showed counts the wrong
event, in the row that carries most of `0013`'s sum.

**S0a is done and the Phase 1 gate is closed.** `CommandKind.Populate` fills a world through **Phase 0**,
so the population is in the Input Log and replay reproduces it by construction; `Borough.Core.Entities.SyntheticCity`
is the one populator, replacing two drifted copies that lived in the shells and were therefore outside
the arithmetic lints — moving it into `Core` made `BOR0203` fire three times. **The spike's largest
finding is what it was not looking for: run mode had never had a city in it.** `--citizens 1000000`
allocated capacity and stepped an empty world, so **every Tick figure in the corpus, slice 6's
100,000-Tick acceptance run included, was taken over nothing.** The numbers at 1M: **85.98 MiB** of
tables and ~94 MiB resident, linear to 343.91 MiB at 4M; an empty Tick is **0.112 ms**; **one State
Hash is 32.47 ms — 2.08 Tick budgets**, against a `05 §9` that does not mention it and that records
`adr/0037` deleting the full-world double buffer for costing *less*; and 100,000 Ticks run in **11.75 s**
with nothing trending. **The Decide guard was `O(world)`, on by default and had no runner switch** —
76.4 ms per Tick, 95% of a run — so `--no-decide-guard` now exists with the correctness check still the
default. **S0 also split while being run**: `plans/0002` names four clauses and three of them are slices
9, 7 and 10, so **S0b — the Tick with work in it — is not runnable, and it is the half carrying `06`'s
stated risk.** 1M is a spec for **row counts** and still a hope for **the Tick**. The capture is
`powersave` and owes a re-take, which is stamped rather than hidden.

**`plans/0000-board.md` is the first thing to read on any cold start** — a flat view of what is done,
what to do next and what is blocked. It is a *view*, and **four files answer four questions, one
each**: the board answers *what is next*; `plans/0003-build-plan.md` owns the slice order and its
gates and answers *what is done*; `plans/0002-open-questions.md` holds **every open question**, typed
*measurable* or *arguable* per `adr/0043` and grouped by what is blocked, and answers *what needs
answering*; `plans/0012-corpus-audit.md` holds corrections owed to documents, which are not questions; and
`plans/0013-tick-budget.md` answers *what a Tick costs*, which is the one thing none of the other four
can hold, because it is a property of the whole set of measurements rather than of any one of them.
**Its headline is that the two consumers with numbers already exceed the budget between them — and
that neither multiplicand has been measured**, which is what makes it a thing to watch rather than a
thing to fix today.
When the board disagrees with any of them, they win — and **an open question is never written on the
board**, which is how it once came to hold 63 of them while `0002` held none. **Slices 7, 8 and 10 have cleared gates; slice 9 is the
only red one**, on session C. A gated slice must not be started before its gate clears. The corpus is still being grilled and several decisions
on the critical path are open, so do not write implementation code beyond the current slice
unless asked.

</details>

## Repository map

| Path | What it is |
|---|---|
| `CONTEXT.md` | **The domain vocabulary. Authoritative.** Every term, with exactly one meaning |
| `PROCESS.md` | **The project vocabulary. Authoritative**, and the sibling of `CONTEXT.md` — slice, spike, gate, session. `CONTEXT.md` names the city; this names the calendar. Neither describes the other |
| `docs/00-vision.md` | Pillars, anti-goals, the argument against this design and the answer |
| `docs/01-player-experience.md` | Verbs, panels, notifications, overlays |
| `docs/02-simulation-model.md` | World model, Tick phases, Rule families, determinism rules, testing strategy |
| `docs/03-agent-architecture.md` | Movement, fidelity tiers, Trips and Legs |
| `docs/movement-primer.md` | **Orientation only, and it owns nothing.** The movement and routing model rebuilt from first principles in the order it was discovered — for paging the subsystem back in after time away. Stores **no status and almost no numbers**, which is what keeps it from becoming a fourth copy that drifts (`plans/0012` *Cause 1*). `03`, `CONTEXT.md` and the ADRs win against it always |
| `docs/04-economy-and-goods.md` | The five Goods, chains, Office |
| `docs/05-technical-architecture.md` | Project layout, sim/render boundary, data layout, threading, saves |
| `docs/06-roadmap.md` | **The phase model, the four pacing rules, and the risk each milestone retires. Nothing else** — it sequences work and never describes the simulation (`adr/0042`). Also names the mechanisms with no milestone yet |
| `docs/adr/` | **113** decision records, numbered to **`0114`** — `0028` is reserved and unwritten |
| `docs/deferred.md` | What is deliberately not being built, with retrofit costs and revisit triggers |
| `docs/references.md` | Reference games and prior art, with standing of each decision |
| `plans/0000-board.md` | **The board. Read this first on any cold start** — it opens with a **What is next** block, then done, unblocked, owed and blocked. A view over `0002` and `0003`, never a source, and **never the home of an open question**. ⚠ **A closed row leaves the board**, because a view that carries its own history stops being scannable: it has been cleared twice, on 2026-08-12 (999 lines) and 2026-08-15 (~400) |
| `plans/0000a-board-archive.md` | **An index, not a record.** One line per closed board row, naming the document that owns the full version. **Do not quote it** — a one-line summary is a caveat-free compression of somebody else's sentence, which is `plans/0012` **Cause 5** by construction. Follow the link |
| `plans/0002-open-questions.md` | ***What needs answering.*** One ledger, every entry typed *measurable* or *arguable* and grouped by what is blocked on it, with the session-by-session record archived beneath it |
| `plans/0003-build-plan.md` | The ordered slice ledger for Phase 0 and Phase 1, with a gate board. **Start here when picking up the *code* cold.** Supersedes `06`'s Phase 0/1 ordering |
| `plans/0026` | **Milestone 5c — statistical resolution and the travel-time matrix. ✅ DONE 2026-08-16: scoped 2026-08-14, tasks 1–7 shipped the same day, task 8 closed 2026-08-16.** ⚠ **Task 8's sharpest finding is about a doc-comment rather than about traffic** — the four slot high-water marks **saturate** (identical at 49 and 200 Days) where the file said they creep, and the figures it said it with were out by roughly **9× and 12×** with **nothing in this corpus able to catch them**, because the file was untracked until `82a1547` and a number in a doc-comment is invisible to every mechanical check the corpus has, all of which read one **document** against another. ***A measurement written into prose does not re-run itself when the mechanism underneath it moves*** — [`adr/0093`](docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) on the one input it does not cover: it governs a description of what the build **does** and repairs it with *name a symbol*, and a description of what the build **measured** is a number no symbol names. Its other three: the collection half is a **minimum** live count rather than a slot high-water mark, which is an **exact** leak test where a maximum is a statistical one and is available only because the commute empties the city once a Day; the flatness band was **transplanted from a quantity three times quieter** and was 1.25σ wide, failing one run in ten with nothing wrong under it; and the run **prints its series before it asserts anything**, because an acceptance run that only speaks on success is one you cannot use on the day it fails. Two ADRs, [`0098`](docs/adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md) and [`0099`](docs/adr/0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md). ~~**Task 8, the long run, is what is left**~~ ✅ **RUN AND CLOSED**, and it was the **named ratifier for four hash-bearing numbers** (`[traffic]`'s α, β and clamp, plus `[households] car_ownership_percent`) — so ⚠ **it must run against `rulesets/congested.toml`**, since `minimal.toml` states neither table and would ratify nothing, and ⚠ **over whole Days rather than a round 100,000 Ticks**, because every congestion figure this milestone published before task 8 was taken over 512 Ticks from Tick 0 with employment still ramping and one of them was an artefact of exactly that. **What the seven built:** `RoutingPartition` (**4 Cells, provisional and UNRATIFIED**, and it pays `adr/0040`'s owed Cells-not-Chunks correction); the matrix itself, partition × partition at free flow, refreshed on `RoadGraph.Version`; the path source, **opt-in so every walk pays nothing**; `RouteCache`, four-way set-associative and validated on each Segment's **handle and Epoch** — the **first reader the per-Segment Epoch has ever had**; the vehicular Leg; the volume-delay function, which **pays `adr/0041`'s volume debt, open since 5a**; and `--traffic`, the **eighth runner mode** *(ninth as of milestone 6 task 5)*, which needed a **fourth shipped Ruleset** before it had anything to look at. ⚠ **Three of its findings outrank the mechanism.** ***A premise licensing one quantity to stand in for another is itself a measurement, and a constant moved in another document can retire it silently*** — `adr/0041`'s *a vehicle crosses about one Segment per Tick* was stated as following from `TICKS_PER_DAY = 8192`, `adr/0094` made it ~4.6 without amending it, and the same expired sentence was load-bearing in **two** sites: the volume/capacity ratio (task 6, which came back ×1.0000 at every population) and `TripEngine.AdvanceTravellers`, where **a journey quoted at 2.10 Ticks took 11.66** and six tasks of measurement did not notice. ***A premise that expires retires every site resting on it, and finding one of them is not finding them.*** **A generated city cannot congest itself** — `v/c` peaks at 0.44 at 4,000, 16,000 and 64,000 Citizens alike, because the paved extent scales with **√population**, so ***the same number sizes both the demand and the supply***; congestion is something a **player** makes by laying too little road (`adr/0090`) and `CommandKind.Populate` cannot reach it. And ***where a route is stored is a different question from whether one can be produced*** — the brief named the wrong sentence of [`adr/0075`](docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md), since *a Leg stores a cost and not a path* is untouched and the **Traveller** row is what needed the edit. ⚠ **All three of the milestone's owed decisions are settled and two dissolved rather than closing**: the drive-Leg endpoint trap had **no ground** (a fallback from an exhausted Shed cannot occur because no Shed exists); a Leg's cost never goes stale because ***a plan does not age if nobody consults it twice***, which points `adr/0012`'s contract at the **route cache** where it was always aimed; and the matrix does not reject at all — the distance-reject is **inert** (the city is smaller than the Commute Budget) and the sound reachability reject **needs no matrix**, being `RoadNodeTable.FootComponent`, in the tree since 5a and **never read until now**. *Original row's scoping note, still true:* ⚠ **Scoping it corrected `06`** — that document said volume *"waits on a path source — which is 5c, **not on vehicles**"*, and `03 §3.3` plus `WorldInvariants.cs:103` make volume **vehicular**, so the path source is necessary and **not sufficient**. ***A precondition stated in the singular is a precondition nobody has finished counting*** — third sighting in three milestones, after 5b task 4's missing destination set and 5b's missing path |
| `plans/0004`–`0024` | One plan document per slice, spike **or session**: S4, the arithmetic substrate, the analysers, typed tables, the Tick and replay, Map Layers, S2 routing, the Rule engine, Zone Rules (`0014`), hot reload (`0015`), the Event Wheel (`0016`), **session D's brief (`0017`)**. **No slice is in flight**; `0015`, `0014` (task 11), `0020`, `0021` and `0022` are all closed. **`0017` is the first brief written for a *session* rather than for code** — D is more than one sitting, which is the same criterion that gives a slice a plan. **`0018` is session N's**, the Bin/Pool/economy cluster; tasks 1, 2, 3 and 4 are `adr/0063`–`0065` and `adr/0068`–`0070`, and **all have shipped**. **`0019` is S5's**, the Lane kernel — run, two tripwires fired, **nothing published**. **`0020` is the Road Graph (`06` milestone 5a) and is the first Phase 2 slice brief** — **built 2026-08-11, all seven tasks**, and it is the document that found no session gates 5a. Its close-out carries four findings and the reason the S2 harness is still on disk. **`0021` is milestone 5b — Trips, Legs and the pedestrian layer — and it is ✅ UNBLOCKED (2026-08-11)**: it had **two** gates, **D** and **F**, and both are now clear — F ran on 2026-08-11, one sitting, seven decisions. The document is **F's brief, F's record, and then the slice**. *(It was ⚠ BLOCKED until that afternoon, and this file's own "milestone 5b, which D gates" obscured the second gate — corrected rather than silently deleted, because a gate struck for the wrong reason is the failure `0020` warns about for the S2 harness.)* **⚠ RE-SCOPED 2026-08-12 by the §A sitting**: tasks **4, 6 and 8 left the slice** for a new milestone **5b-bis** (jobs and the commute), because each measures an origin-destination **distribution** and **all seven Trip generators the corpus names sit in `06`'s *Mechanisms with no milestone*** — so Phase 2 as sequenced never produced a destination and §A was **void as posed** under `adr/0070`. 5b's *irreversible* risk was retired by `adr/0075` and tasks 1–3, not by the generator. `adr/0080`, `adr/0081`. **✅ DONE 2026-08-12** on tasks 1, 2, 3, 5 and 7. Its close-out carries three findings, of which the load-bearing one is that **`adr/0041`'s volume attribution is not buildable against `adr/0075`'s Leg** — a cost and no path — so it waits on 5c's path source rather than on vehicles. **`0022` is 5a-bis — the Lot subdivider and `build_road` — and it is ✅ DONE (2026-08-11)**, all seven tasks. `0020` scoped it and nothing scheduled it; `06` now has a **5a-bis** row, and `plans/0012`'s two subdivider boxes are struck. Its close-out carries four findings, of which the load-bearing one is about **re-recording a baseline** rather than about Lots. **`0023` is 5b-bis — jobs, the commute and the first Trip generator — and it is ✅ DONE (2026-08-13)**, created by the §A sitting that closed 5b. **All eight tasks, 1,276 green.** ⚠ **Its milestone-level finding is that four of the eight found something about an *instrument* rather than about jobs**: task 3's crossing-cost derivation does not exist, task 6's histogram cannot ratify the number it is about, task 7's minute formatter had been dropping the sub-Tick fraction in every duration it ever printed, and task 8's peak test was measuring its own sample size — a missing derivation, a censored distribution, a lossy conversion and a small-sample maximum, four mechanisms with **one** consequence: a number came back and could not settle what it was taken to settle. ***`adr/0043` types a claim by whether a machine could produce the number, and every one of these produced it.*** **Task 8, the 100,000-Tick run**, closes the collection half — the three Movement tables joined `World._tables` in 5b and had never met a long run — asserting **slots** rather than live rows, because a live count is flat in a city that leaks. **The Commute Budget is binding hard**: `jobs beyond budget` never reaches zero and **4,561 of 10,000 hold a job against 9,608 posts declared**, so ~5,000 posts stand empty while ~5,400 Citizens have no work and **distance rather than supply is what separates them** — which is `adr/0095`'s cliff seen from outside, arrived at the same day by a different route. `trips no route` and `trips stranded` are **0 throughout**, so two of `adr/0076`'s four Fates are unexercised by any run. **Task 1, the spatial index**: `Space/BuildingResidency.cs`, a Cell → Buildings reverse index that did not exist and that all three candidate generators needed. **It is not a catchment** — a catchment is a *time* on the Road Graph — so the box supplies candidates and the walk decides acceptability, and merging the two stages would delete the Severance reading. **Task 2, `[[building]] jobs`**: `adr/0068`'s rule on a second axis, with the **worker list** (`BuildingTable.WorkerHead`, `CitizenTable.WorkerNext`) that `CitizenTable` had been saying since slice 10 does not exist and belongs to the labour system. Its finding is that **the key exposed a live defect nothing could previously report** — `SyntheticCity`'s `(i * 7)` workplace stride was 1,000 jobs no Ruleset granted, dismissed *en masse* by the first adoption after a ceiling existed: ***a quantity nothing can count cannot be contradicted***. The stride is **deleted here rather than in task 4**, so the baselines re-recorded once. **Task 3, the `[trips]` table**: the crossing cost is **30 s** and the Commute Budget stays **unset**, as the loader's one optional-key-inside-a-present-table — because ***a value that means "unset" must be outside the range of legitimate answers***, and every minute count is a legitimate Budget. Its findings are that **the derivation was looked for and there is none** (no signals, no Segment width — both candidates rest on unbuilt mechanisms, `adr/0070`), that ***a search for a derivation that fails is a result which has to be written down***, and that **a threshold read off a distribution its own presence shapes must be measured in the city that lacks it** — so *no ceiling* is a precondition of the ratifier. Only the two **Ruleset content hashes** moved: ⚠ **the golden session contains no `trip` command at all**, so the whole Trip model sits outside the committed baseline. **Task 4, the assignment pass**: `EmploymentEngine` in Tick phase 6 behind placement, a `[jobs]` Ruleset table, a four-counter `JobCounter` Census family, and the Commute Budget set at last — a Citizen with no Workplace draws candidates from a box around home and takes the first with a free slot it can **walk to inside the Budget**, which is `adr/0017` satisficing with a real filter for the first time. **The box is derived from the Budget and there is no radius key**: an unbounded draw is S2 R4's uniform O-D distribution, which R4 measured is *a different city*, so the loader **refuses `[jobs]` without a `commute_budget_minutes`**. Its findings: the **Cell-uniform draw is right on paper and unusable here** — 1,200 Buildings across 16,384 Cells means one look in four hundred finds anybody, so ***an argument about what a draw means is independent of whether the draw ever hits anything***; **a Budget chosen against the map is not thereby exercised by every world on it**, since 20 min is inert on the 1,000-Citizen golden fixture and binding at 10,000, which is slice 10 task 11 a fourth time and is now a test in both directions; and `[[building]] jobs` landed on the **dwelling** kind rather than a workplace kind, because a second kind needs a second `[[zone_rule]]` and a second decline Rule or the city fills with offices. **Task 5, the commute generator**: `CommuteEngine` in Tick phase 4, `TripPurpose.Commute`, and **`CommuteRoster`** — a `(derived AND rebuilt)` partition of the population by departure phase. **The occasion is a *phase*, not a schedule**, and the argument is two constants side by side: a commute recurs every Day and `EventWheel.Size` **is** a Day, so an armed Citizen never leaves its bucket and ***a bucketing on a constant is derivable rather than scheduled***. Its finding is that `commute_peak_factor` **was going to be a free hash-bearing number and is a restatement of a measured one** — ⚠ **and the key is RETIRED as of 2026-08-15 by [`adr/0101`](docs/adr/0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md), which replaced the whole departure model** — the peaking multiplier and the departure window are `TICKS_PER_DAY ÷ W` apart, so the file states the side S2 R7 measured and `JobRuleset.CommuteWindow` derives the other (`adr/0059` a fourth time). Two smaller: **the overlap guard is unnecessary because of a refusal written for another reason** — `[jobs]` cannot load without a Commute Budget, so an in-flight commute is bounded in minutes against a 24-hour Day; and **`TripPurpose.Commanded` was scheduled for deletion and is kept**, because `adr/0080` demotes it to *a test affordance rather than the only door* and its own rule became **checkable** on the day it stopped being vacuous. **Task 6, the Trip Census family and the cost distribution**: `TripCostBucket`, a **seventh Census family** and the corpus's first histogram — seven bands of clock minutes, filled at Trip *creation* so a Trip refused for its length is in the distribution with the cost that refused it — plus the four Trip Fates reaching `--census` at last. Its findings are that ***a Census family with no reader is a family nobody can see*** (5b built `TripCounter`, wired it through the Census, tested it and printed it nowhere, so for a milestone the only reader was the suite — `adr/0064`'s *a guard with no test* on a second axis, and both are **a fact with no copy at all**); and that ***a distribution censored by a number is not evidence about that number*** — lowering the Commute Budget **collapses** this histogram into its shortest band rather than growing a tail, because what a lower Budget removes is the *acceptances*, so the uncensored reading is `--trips`' and task 3 was right to insist on taking it first. The ladder is deliberately **not** denominated in the Budget: ***a ruler must not move with the thing it measures***, and the edges are an instrument's resolution rather than `adr/0052` numbers. **Task 7, something to look at**: `--commute`, the **seventh runner mode** and the **second that steps the world**, printing where people work against where they live **by block, before and after** — the before is the control, a city at Tick 0 in which nobody is employed anywhere. The quantity is a **balance rather than a count**, because a grid of worker counts is a grid of population and `--zones` already draws that. Its finding is that ***a defect that only shows on a value you happen to know is a defect that hides in every value you do not***: printing the Commute Budget beside the file's own `20` exposed `--trips`' minute formatter **dropping the sub-Tick fraction before converting**, so every duration that instrument has printed was short by up to one Tick — invisible as *19.9 min*, **7%** on the 2.5-minute band the crossing cost's ratifier is read off. Fixed at the source (`adr/0073`). And the picture makes task 4's `jobs`-on-`dwelling` choice **visible for the first time**: 222 blocks of 228 come out within a quarter of parity, because every Building has the same posts and the same occupants and **the city has no land use at all** | **`0024` is session J's** — `05 §7`'s format half, map size and Outside Connection layout, the three things that blocked milestone 8 — **run 2026-08-12 in one sitting beside 5b-bis, and ✅ DONE**. Four ADRs, [`0085`](docs/adr/0085-nothing-on-this-map-is-far-away-so-a-settlement-is-made-by-a-gap.md)–[`0088`](docs/adr/0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md). **It reversed nothing and replaced the reason for everything**: the map keeps 4096² **on density alone**, async saving keeps *no hitch* on a copy amortised over the autosave interval rather than a Past `adr/0037` deleted, and edge choice keeps its stakes with **congestion** in place of *longer hauls* — ***a decision given several grounds is load-bearing on whichever ones survive, and nothing recomputes that when one falls***. Its sharpest finding is that **S2 R1.5 had already measured the map's consequence in a column nobody read** (one Settlement holding all 121 Districts at every Budget rung from 40 Ticks up, against 171 for thirty minutes), so ***a measurement answers every question its numbers bear on, not only the one it was run for***. Two more: ***the obligation a deletion creates is a re-derivation, not a retraction***, the mirror of 5b's `0075`/`0041` finding and the half that would have deleted a correct decision; and `05 §3` **already held the replacement four sections from the paragraph contradicting it**, which is `plans/0012` *Cause 1* **inside one file** |
| `plans/0028` | **Milestone 6 — Evidence, the accumulators. ✅ DONE 2026-08-17: scoped 2026-08-16, all seven tasks shipped, ungated throughout.** Seven tasks — the seventh added while scoping task 4 — and **every open decision it owed is closed**. ⚠ **Task 7 found the brief wrong about its own shape, and the correction is the milestone's sharpest sentence.** It was scoped as *"task 2's situation verbatim"* — copy the fact into a trail before the row is freed — and it is not: task 2's subject is the **Building**, which is destroyed, so nothing is left to hang the fact on; a Trip's subject is the **Citizen**, who outlives the journey by design, and `02 §9` asks the question *of a Citizen*. ***What is freed is not always the subject, and it is the subject that decides the shape.*** The two are indistinguishable **from the freeing site**, which is why the brief read them as one. A trail would also not have scaled — a million Citizens end ~**1,000 Trips a Tick**, so a 256-entry window covers a quarter of one — where two columns on the Citizen (`LastTripFate`, `LastTripEndedDay`) are **exact for everybody for ever, with no window to size and no `adr/0052` number**. ⚠ **And it found `TripFate.Stranded` is written by no site in the build** — declared, documented, counted in the Census, produced by nothing (`adr/0070`'s **unbuilt**): ***a count that reads zero because nothing exercises it and one that reads zero because nothing writes it are different facts, and a Census cannot tell them apart.*** 5b-bis task 8 had read the zero and filed it as the weaker of the two. ⚠ **Task 6, the long run, found this milestone's two halves failing `06`'s obligation in opposite directions**: the **collection** cannot grow (the trail's 257 rows are allocated in its constructor, so the assertion is a regression guard rather than a discovery) and the **magnitude** must (the aggregate climbs 86 → 12,164 over 49 Days with no sink, because ***attribution decays to magnitude*** is the point) — so the run **states the exception** on the *who reads it* axis and asserts flatness over the **flow**, 251.4 condemnations a Day. ⚠ **Its sharpest finding is about the other magnitude, and it is a rule about documentation**: `CitizenTable.ReachFailures` is a saved per-Citizen count whose own doc-comment says its saturation is *"a wrap guard rather than a chosen bound"* — so the declared position is that it has **no bound** — and it satisfies `adr/0006` **only because `World.Employ` clears it**. ⚠ **That reset *is* named there, by symbol, and is described as an *attribution* rule**; what nothing said is that it is the bound. ***A sentence can name a mechanism exactly and still not state the property that mechanism is holding up*** — `adr/0093` governs a description being wrong about a **trigger**, and this is one being silent about a **consequence**. Removing it puts **3,868 of 4,000** Citizens on a history that can never be cleared. ⚠ **And the run shipped a fix ahead of itself** — the aggregate's count was an `int`, which wraps after ~**162 hours** of play at a million Citizens and then reports the city has *un-condemned* Buildings; ***a counter with no sink is denominated in the life of the world, not in the size of the city***, which is why it was missed, and the guard is a **compile error** rather than a test failure. ⚠ **Task 6 also retires one of the 256-entry window's two refuting readings and raises a new objection**: the window fills **inside the first Day** at 4,000 Citizens so *never filled* is out, *ran out mid-episode* is still unreadable because **no shipped Ruleset has an episode**, and the window is denominated in **events** while the argument for its size is about a **duration** — 256 entries is ~1 Day at 4,000 and **under five Ticks** at a million, so ***the unit a bound is written in is not the unit its argument is about***. **Original scoping note follows.** Six tasks; four decisions settled with the user in the room, three open. **The axis is *who reads it*** — a number the simulation reads is state whatever it is called, a number only a human reads is instrumentation, which is `ColdPathAttribute`'s own test (*"no code path from `step()` reaches it"*). So **Evidence is an assembler rather than a store**: most of `02 §9` is recomputable on a click, and the residue that genuinely accumulates is **events whose subject has left or whose moment has passed**. Core assembles and returns ids and numbers; the host owns every word. The accumulator is **inside the World** on `RulesetTrailTable`'s pattern — fixed-size, all-`Saved`, an **aggregate row at slot 0** so ***attribution decays to magnitude***, dense and chronological rather than a ring (index order *is* hash composition order), and a filter at the write door. ⚠ **Two findings reversed the session's own earlier recommendation**, which was to keep counts inside and named constituents outside: it put a **shared mutable buffer in the one place the determinism machinery is blind** — `Phases.Permission[]` marks Move and Layers `Parallel`, and lint 4 compares State Hashes, so an unhashed buffer would scramble with every test green — and for the canonical case (a departed Household) **there is no entity to hang a count on**. ⚠ **S5's clean 2-thread result does not cover that**, and `spike-results` says why: it held *by construction, because the Lane pass is wholly Lane-local*, and is explicitly **not a discharge** — ***the good number came from there being nothing shared***. ⚠ **Scoping it subtracted a task and moved it to milestone 17**: `CONTEXT.md` names **three** sources of Failure Pressure and **only one is built**, so attributing Trip failures to a Building is a **decline** decision — ***Evidence reports pressure and does not produce it*** — and the **window** it needs has two derivations, both refused (a decay rate by `adr/0053`, reusing `CondemnAfter × rule.Rate` by `adr/0079` by name, because ***a derivation that reuses a constant inherits every decision that constant is already carrying***). ⚠ **`02 §9`'s defect is a symmetric pair and only one side was repaired**: `adr/0097` fixed *jobs beyond budget* on the **Citizen**; `TripCounter.ExceededCommuteBudget` is the identical aggregate-with-no-constituents on the **Building** and nobody walked that axis. Task 2 is named in the build and waiting — `ZoneRuleEngine.cs:291-298` says the demolition's cause *"is available and is not kept… the row it would be copied off is freed on the next line"* |
| `plans/0012-corpus-audit.md` | The corpus audit's debt ledger. Delete it when everything in it is struck |
| `plans/0013-tick-budget.md` | **What a Tick costs.** One row per consumer, each citing its owner, and the column that is the point: whether the row's multiplicand was **measured or guessed**. A view, never a source |
| `docs/spike-results.md` | Recorded spike numbers and the decision each produced. S4, S2 R0–R8, **S0a and S0b** have all run |
| `docs/dev-environment.md` | Setting up a machine to work on this |
| `rulesets/` | **Ruleset content, in TOML.** Data the binary interprets, not source — hot-reloadable tuning under `adr/0015`. **Five** files: `minimal.toml`, the smallest Ruleset that makes Bins move, which says in its own header that it models no city; `minimal-tuned.toml`, **the same file with one number changed**, which the golden session **reloads into at Tick 128** so replay equivalence covers a transition, and which a test holds to a one-line difference; and `severance.toml`, **the same file with three `[roads]` numbers changed**, which exists because **`minimal.toml` cannot demonstrate Severance and that is measured** — the shipped 32-Tile lattice strands **zero** walkable nodes on seven of eight seeds. Its header carries the sweep that chose its rung and why sixteen Arterials rather than eight (a floor across seeds of 32.3% against a coin toss). It is a **demonstration, not a city** — a 256-Tile block is 1,024 m on a side. And `congested.toml`, **the same file with one number changed and two tables added** (`street_capacity_per_hour` 3600 → 400, plus `[households]` and `[traffic]`), which exists for `severance.toml`'s reason on a second axis: **`minimal.toml` cannot demonstrate congestion and that is measured** — the paved extent scales with √population, so `v/c` peaks at 0.44 at 4,000, 16,000 and 64,000 Citizens alike. ⚠ **Its header carries the capacity sweep that chose 400**, and the reason 400 rather than 200 or 900 is that it is the only rung where **both** the grid and the bill move: above it the extra dwell is real and *sub-Tick*, so the price changes and the per-Tick volume does not; below it every block clips at *over capacity* in both panels. It is a **demonstration, not a city** — at 400 Vehicles an hour a Segment holds **1.02** Vehicles where the shipped 3,600 holds 9.2, so ***you have to make the Street absurd before a generated city will congest it***. And `diagnosed.toml`, **the same file with one key added and one `[[rule]]` appended**, which exists for the same reason on a third axis: **`minimal.toml` cannot say *why* a Building fell down and that is measured** — the condemnation trail's condition column is `ConditionId.None` for **every entry at 512, 1,024, 2,048, 4,096 and 8,192 Ticks**, because **no shipped Ruleset authors an `on_fail` chain**, so the trail records when, where and what kind and never the one thing `02 §9` calls *"the hardest and the most valuable"*. ⚠ **It is the only one of the three that is not verbatim** — it edits `upkeep` in place — and its header carries why: `minimal.toml` gives **two** reasons for having no chain, of which the header's *a chain here would be a Rule rescuing itself* is **correct and untouched**, while the comment at `upkeep` — *writing a terminal here would claim a source of supply* — is ***an argument about a link applied to a terminal***, which `RulesetLoader`'s refusal 3 exempts **by name** (*"a reporting terminal is exempt, and that is not a loophole. It rescues nothing by design"*). **The thing said to be impossible was one key and the loader had been accepting it all along.** Measured against `minimal.toml` at 2,048 Ticks it is **identical on every count** — 285 buildings, 741 pool, 256 retained, 339 condemnations, 83 aggregated, the same Lots on the same Ticks — with entries naming a condition going **0 → 256** and only the State Hash moving, which is `adr/0094`'s rescale precedent and is what a diagnosis *should* cost. **No golden baseline moves**: the committed session runs on `minimal.toml` |

`plans/0001-foundational-design.md` predates ADRs 0005–0011 and is stale. `docs/06-roadmap.md`
supersedes its build order. Do not trust it without checking.

## Working with the corpus

**`CONTEXT.md` governs vocabulary.** Domain terms are capitalised in prose — a Household, a
Bin, a Trip, a Segment, the Event Wheel. If a concept needs a name that isn't in `CONTEXT.md`,
add it there first. The file ends with a *Terms we deliberately do not use* section; those are
banned outright, and several of them (Agent, Cohort, Demand, Region) name failure modes the
design has already rejected.

**Decisions live in ADRs, not in prose.** If a design question gets settled, it gets an ADR in
`docs/adr/NNNN-lowercase-hyphenated-claim.md`. The filename is the claim, stated as a sentence.
The structure is: title as a claim, the decision in bold up front, `## Why`, `## Consequences`,
`## What would trigger revisiting`. That last section is not optional — a decision with no
revisit trigger is a decision nobody can reopen honestly.

**A claim a measurement could settle must not be settled by argument** (`adr/0043`). Type every claim
before settling it: *can you name the number that would refute this, and the machine that would produce
it?* If you can, it is **measurable** — route it to a named spike with that number written down, and do
not let any document cite it as decided until the number exists. If you cannot, it is **arguable** and
a session may close it. Six claims in the corpus have been measured false so far and **two of them sat
in documents `0002` marks fully argued**, so a green mark is not evidence a sentence was examined.

**A hash-bearing or world-creation number is chosen with a named ratifier or not at all** (`adr/0052`).
The sibling rule to `adr/0043`, and it governs numbers rather than claims: on the day such a number is
written down, record beside it in `0002` §D *the named thing that would ratify it* — a spike id, a
session letter, a quantity — and the trigger that would reopen it. **A category is not a name**;
*"a profile"* or *"a future spike"* satisfies the letter and defeats the point. It does **not** require
ratifying before choosing, which is often impossible and worse than the disease. Why it exists:
`adr/0044` had to *measure* the Map Layer cadence back out of three documents that cited it as settled,
and `0002` §D had reached eighteen rows without ever losing one. The triage that followed found
**five were not numbers at all** and **seven were unset** — and an unset number is a *gap*, not a debt,
because nothing accretes on a value that does not exist. Keep those three apart or the list stops
being readable.

**An unbuilt mechanism is not a design constraint** (`adr/0070`). The third sibling: `adr/0043` governs
**claims**, `adr/0052` governs **numbers**, and this governs **absences** — the third thing a sitting
reasons from, *this does not happen*. Before deciding anything on the ground that the simulation does not
do something, **name the mechanism and classify the absence**: *unbuilt* (specified, no builder),
*undesigned* (no specification), or *refused* (a decision says no). **Only *refused* is evidence.** This
project is young and most of it does not exist — no jobs, money, traffic, prices, bid, choice
model, immigration or renderer — so *the simulation does not do X* almost always means **nobody has built
X**, and a rule of inference whose premise is usually false is inverted rather than weak. The symptom to
watch for is a question of the form *given X does not exist, should Y compensate?*: if X is unbuilt, the
question is void and the answer is **build X**. Why it exists: session N spent a sitting arguing whether
construction should fill a Building to capacity, a question that exists only because `02 §5.2`'s
placement step is unbuilt — and two ledger entries had already concluded that a **number** settled an
equilibrium that a **mechanism** settles. **A design document is not a description of the build**, and
where it could be read as one it says which parts exist.

**A description of the build is where to look, and never what you found** (`adr/0093`). The **fourth
inference sibling**: `adr/0043` governs **claims**, `adr/0052` **numbers**, `adr/0070` **absences**, and
this governs **what the build does** — the input every sitting reasons from constantly (*the loader
already refuses this*, *that pressure source would cover it*) and the one nothing governed. Before
deciding on the strength of what the build does, **open the mechanism**: a sentence about it, in an ADR,
a plan, a doc-comment, or implied by what a test suite covers, tells you which symbol to read and never
what is in it. Where such a sentence is wrong it is wrong about the **trigger** — what fires it, what it
is keyed on, when it runs — because a description explains a mechanism's *purpose* and a purpose is not
a trigger. **Four sightings in three days, each costing a decision**: `adr/0064` read the test suite and
found a live defect that was not one (the loader's one untested guard); `adr/0079` read an ADR's summary
and recommended a pressure source keyed on something else; `adr/0091` read a plan's own recommendation
that *the Zone Rule condemns in its own time*, which `Condemn` never does; `adr/0090` read a doc-comment
saying `RoadGenerator` lays *"at world creation"*, when its one production call site is `SyntheticCity`.
**The writing half is what compounds: name a symbol, never a time.** *"At world creation"* cannot be
checked without already knowing the answer; *"when `SyntheticCity` runs"* is one grep. This corpus cannot
catch the failure any other way — its ~~three~~ ~~five~~ **six** mechanical checks are all document-to-document.
`plans/0012` **Cause 4**. ⚠ **And `adr/0093` committed its own defect in the sentence that listed them**,
found 2026-08-13: it said `CitationTests` *"checks that links resolve"*, which it does not — it regex-matches
`adr/\d{4}` and **never opens a target**, so a link to a file that did not exist passed every check there
was. Amended there, and the repair is `plans/0012` **check 8** (`LinkResolutionTests`, green at 2,292 links).
***A false description of a guard is worse than no guard, because it is the reason nobody builds the real
one*** — and *a count of the instruments is itself a fact stored in prose*, so name each check by what it
**opens**, never by what it is called.

**A caveat attached to a number does not travel with it** (`plans/0012` **Cause 5**). Not an inference
sibling and not a rule about reasoning — a rule about **quotation**, and the one failure in this corpus
whose two copies **agree to the last digit**. A figure is written down correctly with a clause saying what
it measures and what it must not be used for; somebody later needs a number of that shape, finds it, and
copies the **digits**. The clause stays where it was, still correct, doing nothing. **The tell is worse
than nothing**: a bare figure accumulates apparent authority every time it is repeated, so the corpus's
habit of quoting across documents is what makes it worse. Two halves, and the writing one compounds:
**reading** — quote the *sentence*, never the digits, and a number arriving with no clause saying what it
measures is a coincidence of magnitude rather than evidence; **writing** — **name a number after what it
measures, not after where it sits**, because *"186,624"* travels freely and *"R2's fixture fleet"* cannot
be silently made a denominator. **A percentage of a budget is the special case: *carry the bill, not the percentage*** — a share is a
measurement over a **product decision** (which speed rung, which clock), so `plans/0013`'s share is one
bill — **≥44–50 ms**, re-summed 2026-08-16 — read at several rungs, and `861.87%` survived two
independent changes to its own denominator to within 0.4% because a percentage hides which side moved.
State the milliseconds first and let the share follow. And **a number that is one half of a ratio says which half**: every
sighting so far is a figure used as the other side of a comparison it was never a side of.
⚠ **A third form, found 2026-08-16 by session T: *a correction attached to a number does not travel with
it either*.** `plans/0013`'s routing row never took `adr/0094`'s ×4 while the same file's volume row was
re-derived for it and a sidebar stated it outright — so **both copies were inside one file**, no
document-to-document check could see it, and **the copy that was wrong was the one that owns the sum**.
The only instrument that finds this is somebody re-summing the table by hand, and nothing schedules that.
⚠ **And the first sighting against an *external* source, same session**: `adr/0019`'s *"64 Ticks/s is
Factorio's rate"* — it is 60, 64 is `2⁶` off our own reference rate, and a figure landing within 7% of
another product's read as corroboration. ***Two rates sharing a unit are not thereby comparable.*** Why it exists:
`adr/0094` built a 27–58× Microscopic Cap gap on `plans/0013`'s 186,624, which that table labels *not a
stressed count* **in the cell** and warns about **in the paragraph beneath** — quoted a day after the
paragraph was read, and withdrawn by `adr/0096`. The lesson had already been coined twice as commentary
(`plans/0002`'s *an unratified number is more dangerous than an open question*), which is `adr/0093`'s
***an aside is not a rule*** arriving on a second cause.

**A local workaround is not a discharge** (`adr/0073`). Not an inference sibling — `0043`, `0052`, `0070`
and `0093` govern what a sitting may *conclude*, and this governs what a spike must *do with what it
already found*, which puts it beside `adr/0042`. **When a spike measures something and the cause lies in
code the spike does not own** — `Borough.Core`, the arithmetic substrate, a Ruleset, an analyser —
**route the finding to that code or to a named document with an owner, on the day, and before working
around it.** The order is the rule: the filing survives, the workaround makes the spike runnable, and
doing the workaround first is how the filing stops feeling necessary. Route by class exactly as
`adr/0070` routes absences: a **defect** → fix it there or `0003`; a **cost** → `0013`; a **question** →
`0002`; a **document now wrong** → `0012`. *"Worth recording beyond this spike"* is not a fifth class.
Why it exists: S2 R2 found `IntegerMath.FloorDiv` doing an unnecessary modulo, measured it at *most of*
231 ns an expansion, hoisted it out of its own loop, and left the substrate defective for S5 — which met
it in a kernel that cannot hoist, measured **1.50×**, and published a tripwire against `adr/0016`. **A
local workaround removes the finder's own exposure, and with it the only pressure that would have fixed
the source**, so the defect survived *because* a competent author fixed his own problem. Both spikes
blamed a design commitment they happened to be exercising; the actual fix was **reordering two operands
of an `&&`**. Corollary worth holding on its own: **a cost measured while exercising a decision is not
thereby a cost of that decision** — read the primitive in the inner loop before writing the sentence.

**Every significant decision cites a guiding concept** from `CONTEXT.md`'s tag table —
`EMERGENCE`, `LEGIBLE CAUSE`, `UNIQUE INDIVIDUALS`, `BOUNDED KNOWLEDGE`,
`SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`, `PLAYER GOVERNS`, `NO VERDICT`,
`FAST ITERATION`. A decision that cites none is a decision without a justification.

⚠ **`Borough` is the *codename* and it is frozen; the *public* name is a separate, deferred decision.**
Settled 2026-08-18 — [`plans/0003`](plans/0003-build-plan.md) → *The name* owns the record and the
boundary. The line is *does a human who is not a developer read the string?*: the namespace prefix,
`World.HashSeed` and the save's **`borosave`** magic number are codename and never change; the only
player-visible strings are two file extensions (`.borough`, `.borough-crash`) that **nothing dispatches
on**. So milestone 8's rename trigger is ***discharged rather than met***. ⚠ **Two names for one project
is `plans/0012` *Cause 1* by construction**, so they live in disjoint domains and that section is the one
record of which is which.

**Prose style is British** — modelled, behaviour, optimise, serialisation, sterilise. Documents
cross-reference by section (`02 §4.1`) and link relatively. The register is dense and
argumentative: state the claim, then the reasoning that survives objection. Match it.
**This governs documents only.** Chat replies to the user are plain English — lead with the answer,
explain each term and citation inline as you use it, and debrief at the end of a chunk of work.
Do not carry the corpus's register into the terminal.

**Superseded documents get a banner, never a deletion.** See the top of
`docs/adr/0005-two-fidelity-tiers.md` for the form.

## Architecture invariants

These are enforced mechanically because they fail silently. Full list in `docs/05 §4`.

1. **No Godot reference from `Borough.Core`, transitively** (`adr/0002`)
2. **No `float`/`double`** in simulation state or arithmetic — integers and Q16.16 only (`adr/0003`)
3. **No `Dictionary`/`HashSet` enumeration** in simulation code; no `System.Random` anywhere in it —
   build one and look up in it freely, never walk it
4. **Thread-count equivalence** — `run(log, threads=1).hash() == run(log, threads=8).hash()`
5. **Replay equivalence** — two runs of one Input Log produce identical State Hash sequences
6. **Save/reload equivalence** — the Factorio test: run N, save, reload, run M; vs run N+M
7. **No reference types in simulation state** — every struct in `Borough.Core` satisfies `unmanaged`
   (`adr/0036`), unless it carries `[ColdPath("why")]`, which is the hot/cold axis and the only
   exception: the hot path runs inside `step()` every Tick and holds no references; the cold path
   runs on a click and may

**Lints 1–3 and 7 are live.** `Borough.Analysers` reports them as build **errors**, ids `BOR0201`–`BOR0207`
(floating point, `Math.*`, raw `/`, masked shift counts, wall clock, unstable identity, a ratio pre-scaled
in 32 bits), `BOR0301`–`BOR0302`
(hash-map enumeration, `System.Random`), `BOR0701` (managed state) and `BOR0801`–`BOR0803` (the
`purpose_tag` enum). `BOR0901` is `adr/0003`'s per-field declaration — storage in a `[Table]` type
that is not a declared `Column` or the table's own `Rows`. Neither `BOR08xx` nor `BOR0901` is one of
the seven lints; the count stays seven. Lint 5 is live — `ReplayTests` and the golden baseline. **Lint 6 is
live as of milestone 8** — `FactorioTests` over seven cases and two Rulesets, and ⚠ **it is stronger than
a test suite as of task 10**: a save's header carries the State Hash of the world it holds, folded from
the **copy** rather than from the live world, so **every load** restores, rebuilds, recomputes and
refuses a mismatch ([`adr/0112`](docs/adr/0112-the-saved-set-is-the-hashed-set-so-a-save-can-compute-its-own-state-hash.md)).
***A test asserts this for the cases somebody wrote down; the header asserts it for the ones nobody
did.*** **Lint 4 alone still needs machinery that does not exist yet** — it wants a parallel phase, and
S5's clean 2-thread result is explicitly not a discharge.
Every diagnostic has a test that writes the violation and watches it fire — do not add one without.

**Every field in a table is declared once** as `(saved AND hashed)` or `(derived AND rebuilt)`, and
declaring it through `Rows.Saved`/`Rows.Derived`/`Rows.SavedHandle` is what *allocates* it — so the
State Hash cannot have a coverage hole. The hash folds values, never identity: a handle column folds
the target row's monotonic never-reused id, not the recycled slot index. Composition order is
**tables in declaration order, arrays in index order**.

Also banned in the core: `DateTime`, `Stopwatch`, `Environment.TickCount`, `Guid.NewGuid()`,
default `object.GetHashCode()`, and parallel loops accumulating into shared state.

Randomness is `hash(world_seed, entity_id, tick, purpose_tag)` — counter-based, never a stream.
Every distinct use gets a distinct `purpose_tag`; reusing one correlates two decisions invisibly.

Every variable-length collection in `Borough.Core` is an **intrusive index list** — a head index
on the owner, a `next` index on the element, both in flat arrays. Never a per-entity collection
object.

`Borough.Core.Arithmetic` is the one namespace exempt from the raw-`/` and shift lints, because it is
where their replacements are implemented. There is no `Math.*` anywhere, including there.

**No tuning number is a `const` in simulation source.** Everything the designer would want to
change lives in the TOML Ruleset and is hot-reloadable (`adr/0015`). A `const` where a Ruleset
value belongs is a defect, not a shortcut.

**A change is an optimisation if the State Hash is unchanged, and a design change otherwise** —
however it was motivated. This is the test that decides whether something may be tuned freely.

⚠ **It classifies a change; it does not price one** ([`adr/0100`](docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)).
**Moving the State Hash costs nothing while nobody is carrying a save** — the re-record is one command,
and the failing test prints it for you. **Never cite hash movement as a reason to defer, narrow or split
work**; a sentence of the form *"this moves the hash, so…"* is a defect in the document that contains it.
What survives is **attribution** (a hash move gets a commit whose subject explains it, which is what
`plans/0003`'s queue is *for*), **collision** (two sessions re-recording the same baselines is
scheduling, not cost) and **incidental movement** (unexplained movement is still a defect). The class
this was suppressing — renames, column splits, enum reshapes — is the class whose price grows fastest
with corpus size, so it is **cheaper today than it will ever be again**.

## Project layout

Five projects, one repository, two toolchains. The split is the architectural decision. A sixth,
`Borough.Analysers`, is a build-time input rather than part of the runtime architecture and is
deliberately not counted among the five (`05 §1`) — the test being that it does not ship.

| Project | Contents |
|---|---|
| `Borough.Core` | Pure C# library, zero Godot references. Typed tables, integer maths, Event Wheel, Ruleset interpreter, `step(inputs)`. **This is the game** |
| `Borough.Tests` | xUnit and BenchmarkDotNet. Determinism, invariants, save/reload, allocation benchmarks |
| `Borough.Headless` | Console runner. Loads a Ruleset and an Input Log, fast-forwards, dumps State Hashes |
| `Borough.Formats` | The artefacts that spell things in words: the Input Log codec (`.borough`), and the crash artifact that wraps it. References `Core`; referenced by both shells, which may never parse or emit a log themselves (`adr/0039`). Not the save — that is an array dump generated from the field declaration and stays in `Core` |
| `Borough.Godot` | Thin shell. Per-Chunk `MultiMeshInstance3D`, `Control` UI, per-frame snapshot |
| `Borough.Analysers` | `netstandard2.0` Roslyn analysers for `05 §4`'s lints 2, 3 and 7 and the `purpose_tag` check. Referenced by `Borough.Core` as an **analyser**, never as a dependency, so nothing in it reaches the running sim |

**The headless runner must never require Godot to be installed.** That constraint is the
cheapest continuous check that the boundary still holds.

**`Core` returns ids and numbers, never human-readable strings.** The shell owns every string a
human reads, resolved through the Ruleset. The real leak vector is not `using Godot;` — it is a
method that returns a formatted string because a panel wanted one.

```
dotnet build                  # must succeed with no GPU and no Godot installed
dotnet test                   # must be green
dotnet run --project src/Borough.Headless
dotnet run --project src/Borough.Headless -- --zones --ruleset rulesets/minimal.toml --ticks 5000
dotnet run --project src/Borough.Headless -- --commute --ruleset rulesets/minimal.toml --ticks 4096
dotnet run --project src/Borough.Headless -- --traffic --ruleset rulesets/congested.toml --citizens 16000 --ticks 512
dotnet run --project src/Borough.Headless -- --evidence --ruleset rulesets/diagnosed.toml --citizens 4000 --ticks 2048
dotnet run --project src/Borough.Headless -- \
  --ruleset rulesets/minimal.toml --reload-at 200 --ruleset rulesets/minimal-tuned.toml --ticks 400
```

## Constants

| Constant | Value | Kind |
|---|---|---|
| `TICKS_PER_DAY` | ~~8192~~ **2048 — ✅ BUILT 2026-08-13** | world-creation, baked into the save, not hot-reloadable. `Ticks.PerDay` — a `const`, where `adr/0015` says it should be Ruleset data; see `plans/0012`. [`adr/0094`](docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md): a Tick is **42.1875 s** of in-world time, a Day is **2m08s at 1×**, and a twenty-hour campaign holds **562 Days** against 140 — which is what `01 §4`'s Replacement Rate marker needs and what the old constant could not supply at any Life Stage length. **`adr/0019`'s prohibition falls and its premise is why**: it forbade this on the ground that a commute is a fixed number of Ticks, which `adr/0082` made false by deriving a Tick's duration *from* this constant — a commute is **1.39% of a Day at both values**, so no balance moves. **What rescales is decided by one question — *what is the quantity denominated in?*** Ticks (kept at its number), in-world time (unchanged, ×4 more of it per real second), Days (unchanged, ×4 faster in real time). ~~**Only Goods quantities rescale, ×4.**~~ ⚠ **Measured false on the day it was built — three things rescale, and see the end of this cell.** ⚠ **One cost**: routing ×4, taking `plans/0013` to ~305% at 4× and ~76% at 1×. The sub-step band goes 21–45 → **84–180** and a second claimed cost — a Microscopic Cap collapse — was **withdrawn the same day by [`adr/0096`](docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)**: priced at the design speed the Cap's supply side is *unchanged* from the old clock's top rung, and the gap it was quoted against had no denominator. **UNRATIFIED**; ratifier is **playtest**, refuting numbers named in both directions, and **lower is the expected direction**. ⚠ **Building it found the ADR's rescaling inventory wrong in two places, and both would have shipped in silence** — it says the Goods quantities are *the only rescaling*, and there were **three**, two moving **down** ×4 while the Goods move **up** ×4. `Speed.PerKilometrePerHour` was a **literal** 48,000, so an authored `walk_speed_kph = 5` would have walked the city at **1.25 km/h** with nothing failing to compile; and `revisit_ticks` / `pollution_decay_ticks` are durations the Rulesets themselves call *one Day*, which kept at 8192 would have meant **four**. ⚠ **`revisit_ticks` was actively misclassified by `adr/0094`'s own table and is struck from it** (2026-08-13, with the user in the room): `adr/0059` makes it a duration, and the ×4 Zone Rule cost per Tick is accepted because it is the same cost per Day. ***The name of a quantity is not its denomination***, and a conversion factor is denominated in two units at once so it belonged to no class in that table. **The fix in every case was a derivation rather than a new value** — `TravelTime` had always written its half as one and said so in its own remark, which makes this `plans/0012` **Cause 1** in code: one fact, two files, an expression in one and a value in the other, and the value is the copy that drifted. ⚠ **`adr/0071`'s two illustrations moved in opposite directions**: a 32-Tile Street at 50 km/h goes 0.87 → **0.22** Ticks, so the sub-Tick argument is four times more load-bearing, while a 5 km/h walk goes 3.66 → **14.65** Tiles/Tick, so the flooring error falls 20% → **4.4%**. ✅ **The Goods ×4 rescale shipped the same day as the separate commit, and it was two rescales rather than one** — the **Bin capacities** moved with the Goods (`sundries` 12 → **48**, `repairs` 1 → **4**), which nothing predicted. ***`amount` and `capacity` are both in Goods and belong to different rows of this ADR's own table***: `amount` is **Days**-denominated, so its number goes ×4 to hold Goods per Day; `capacity` is **Ticks**-denominated, because what must hold is **firings held** and firings × `rate` is a duration — but it is written in Goods, so keeping that duration still *requires* moving the number. ***The unit of a quantity is not its denomination***, which is `revisit_ticks`' lesson one level down: **twice in one build a class was read off a surface form**, a key name and then a unit. Left at 12 the larder held **one** firing where it held four, and a Bin that must be *exactly full* for one `consume` to succeed is a knife edge. ⚠ **The knife edge exposed a live defect that is filed unfixed as queue item 8** — a waiter whose *own* requirement falls is never re-checked, because `adr/0063` made the wake predicate read live state and the only thing that calls `Drain` is a write to the **Bin**. ⚠ **And the rescale commit shipped a false claim, corrected the same day by measuring it**: it said *a dwelling now holds 90 in-world minutes where it held 22.5, which is a change to the world rather than a neutral rescale*, and it is **behaviour-neutral** — the pre-rescale and shipped Rulesets over 4,096 Ticks give a **byte-identical 86-line census**, with only the State Hash moving because Bin levels are four times larger. The larder is 4 firings × `rate` 32 = **128 Ticks** and was 128 Ticks before the clock moved, so **90-vs-22.5 is HEAD against the *pre-clock* world** — `plans/0012` **Cause 5**, *a number that is one half of a ratio says which half*, committed inside the session that coined it. A sweep then found the larder is a **cost dial rather than a balance dial**: at capacities 24–96 no census row moves and only `rules evaluations` does, **upward** (20,134 → 25,023 going 48 → 96), because a deeper larder gives `restock` more headroom to keep working. It decides nothing because **it is deeper than the building's lifespan** — nothing produces `repairs`, so `upkeep` condemns every dwelling at 64 Ticks and a 128-Tick larder absorbs a shock that never arrives |
| `WHEEL_SIZE` | ~~8192~~ **2048 Ticks — ✅ BUILT 2026-08-13** | world-creation. Set by the longest routine sleep, which is bounded by one Day — so it moves with `TICKS_PER_DAY` and `adr/0019`'s note that their equality is *"a coincidence worth stating"* still holds |
| Reference tick rate | 16 Ticks/s → a Day is ~~8m32s~~ **2m08s** | host-side, runtime only. **Unchanged** — the Day moved because `TICKS_PER_DAY` did, not because the host runs faster. The ladder is **pause / 0.5× / 1× / 2× / 3× / 4×** (`01 §1`), and **3× is new**: 4× is now a 32-second Day and is *getting somewhere* rather than watching |
| Cell | 32×32 Tiles (≈128 m) | **design constant, never tuned** — it changes the State Hash |
| Chunk | a multiple of the Cell, ≥32×32 | tuning, hash-preserving, unvalidated. **Provisionally 1:1 with the Cell** |
| Map Layer cadence | pollution every 64 Ticks at offset 0; land value every 256 at offset 16 | tuning, hot-reloadable, **hash-bearing** — the designer's number and not the profiler's, measured in `adr/0044`. **Stated by `rulesets/minimal.toml`'s `[layers]` since slice 8 task 8**, and `adr/0044`'s claim now runs end to end from TOML text to State Hash. Still **unratified**: stating a number is not choosing it |
| Provenance trail cap (`N`) | **16** transitions retained in full, older ones aggregated to counts | world-creation, saved and **hash-bearing**. `RulesetTrailTable.Retained`. **UNRATIFIED** — the ratifier is the first real cross-patch diagnosis, and nothing can produce the refuting number until patches exist. A `const` rather than Ruleset data because a designer must not be able to reload a smaller window: the file whose adoption the history is about would be truncating that history |
| Industrial pollution kernel | separable tent, 1,024 m (8 Cells) | world-creation, **Ruleset data** — `[layers] kernel_metres`, frozen at world creation and refused on reload (slice 8 task 3). **UNRATIFIED** — the 1–10 km band is 10× wide and wants a source, and moving a number into a file does not ratify it |
| A `[[kind]]`'s occupancy | **3** in both shipped Rulesets | tuning, hot-reloadable, **hash-bearing** — `[[building]] occupants` (`adr/0068`). Derived from the Ruleset in force rather than frozen at construction, so lowering it **evicts** the overflow into the Unplaced Pool: a Bin has a consumer and occupancy has none. **UNRATIFIED**, but its named ratifier has run and did not refute |
| A `[[kind]]`'s employment | **8**, on `dwelling`, in all three shipped Rulesets | tuning, hot-reloadable, **hash-bearing** — `[[building]] jobs` (5b-bis tasks 2 and 4). `adr/0068`'s rule on a second axis, and it transplants because the *reason* does: a Bin over its ceiling drains because a Bin has a consumer, and a job has a holder and none, so a lowered ceiling **dismisses** the overflow. It counts **Citizens**, never Households. **Derived rather than chosen**: `1000/360 × 3` — S4 task 2's Household ratio through `occupants` — floors to 8, giving **0.96 jobs per resident**, so full employment is out of reach by construction and the shortage flow is never trivially zero. ⚠ **It is on the *dwelling* kind rather than on a workplace kind, which `0002` §D2 did not predict**: a second kind needs a second `[[zone_rule]]` and a second decline Rule or the city fills with offices, which is three decisions about content in a file whose first line says it makes none. **UNRATIFIED**; ratifier is task 8's long run |
| Placement pacing | `interval = 32`, `revisit_ticks = 1024`, `candidates = 3` | tuning, hot-reloadable, **hash-bearing** — the `[placement]` table (`adr/0069`). The **sample is derived** from the duration (`adr/0059` again), the duration is not. `revisit_ticks` shipped at 8192 and left 45% of the housing stock empty; `candidates` is `02 §5.3`'s N and **nothing can ratify it** until there is a choice model to score with. All three **UNRATIFIED** |
| Job assignment pacing | `interval = 32`, `revisit_ticks = 1024`, `candidates = 3` | tuning, hot-reloadable, **hash-bearing** — the `[jobs]` table (`adr/0081`, 5b-bis task 4). `[placement]`'s three keys on the employment axis, with the **sample derived** from the duration (`adr/0059`) and the duration authored. **Three numbers predicted rather than discovered**, because `adr/0069` is the standing counter-example: placement's ADR predicted none, its pass needed exactly these, and one shipped at a default that meant something else. **There is no search radius key and that absence is the decision** — the box is derived from the Commute Budget, and a `[jobs]` table in a Ruleset with no `commute_budget_minutes` is **refused at load**. `interval` and `revisit_ticks` are `[placement]`'s own values, copied as an argument rather than a convenience; only `candidates` is free, **and it is ratifiable for the first time here**, because placement's copy scores every candidate identically while this one filters on a real walk. All three **UNRATIFIED** |
| Road Graph geometry | `block_tiles = 32`, ~~`arterial_count = 8`~~ **`arterial_count = 0`**, `arterial_junction_tiles = 512`, `foot_crossing_every = 4`, `foot_paths_per_thousand_blocks = 40` | world-creation, **Ruleset data**, **hash-bearing** — the `[roads]` table (5a). `block_tiles = 32` is one Street per Cell boundary and therefore **≈16.2 km/km²**, which reproduces S2 R0's density *by construction rather than by a second measurement*. ~~**`foot_crossing_every` is Severance's dial and it does nothing below a threshold of Arterial length per unit of grid** — at 512-Tile blocks, or at two Arterials, moving it from every Street to none leaves the pedestrian network in one piece.~~ **⚠ MEASURED FALSE 2026-08-11 and the direction is inverted**: severance is monotone *increasing* in block size, so the shipped 32-Tile lattice is the **dead** end of the range — 0.0% at every dial value in `1..16` and every Arterial count up to 32. The key states a **ratio** and what reconnects a city is an **absolute count of crossings kept** (306 at block 32, 16 at block 512, dial fixed at 4). **`foot_paths_per_thousand_blocks` is the stronger Severance lever at the shipped values** and was never named as one. **`rulesets/severance.toml` is the rung where the dial works.** All five **UNRATIFIED**, and `foot_crossing_every` is *inert* rather than merely unratified. ⚠ **`arterial_count` went to 0 in both shipped Rulesets on 2026-08-13** (`plans/0003` queue item 6): it is a **per-map count**, so it could not survive a lattice sized to the population — eight across a 160-Tile lattice is a motorway every 20 Tiles — and **the fix is not to derive it**, because an Arterial should not be in a generator at all. `adr/0077` refuses Arterials in `CommandKind.Connect`, `adr/0090` says the generator makes land and the player makes every road, `adr/0014` grants an Arterial no frontage, and `RoadKind.Arterial` is constructed in exactly **one** place in the build — inside `RoadGenerator`. **A player tool nothing in the simulation can produce, sitting in the one structure the player does not author**, and 5a's 240-configuration sweep had already measured those eight severing **0.0%**. `rulesets/severance.toml` keeps its sixteen: it is a demonstration and says so in its own header |
| `lots_per_segment` | **5** | world-creation, **Ruleset data**, **hash-bearing** — the `[lots]` table (`adr/0078`). How many Buildings share one Street Segment, and therefore how many Lots a zoned block yields. **Derived rather than chosen**: it is `CONTEXT.md` → Address's own *"five Buildings share a Segment at the working figures"*, which is the **premise of the decision that keeps an Address off a Node** and therefore of the ~30,000-Segment figure every routing cost in `0013` is priced against. 5a's graph gives **33,024** Street Segments by construction, so the product is **165,120** Lots — against `World`'s independently-chosen 225 per 1,000 Citizens, or 225,000 at 1M: *two figures that never met, agreeing within a quarter.* **Lot depth does not exist and is not a second row** — a Lot has no extent in `LotTable`, so a depth would be a number chosen for a consumer nobody has designed. **UNRATIFIED**; ratifier is the first **5b-bis** run reporting a real Buildings-per-Segment distribution *(read 5b before 2026-08-12)* |
| Free-flow speeds and capacities | 50 / 90 / 5 km/h; 3,600 / 12,000 / 1,000 Vehicles per hour | tuning, hot-reloadable, **hash-bearing** — free-flow is `(derived AND rebuilt)` per `adr/0064`, so retuning a speed moves the **standing** city. The speeds are the one group here with a source outside the corpus; **the capacities are weaker and nothing reads them at all**, stored as whole Vehicles per Day so an unbuilt consumer does not dictate a representation (`adr/0070`). **UNRATIFIED** |
| Lots per Segment | **5** | world-creation, **Ruleset data**, **hash-bearing** — `[lots] lots_per_segment` (`adr/0078`). **Derived rather than chosen**: `CONTEXT.md` → Address already states *"five Buildings share a Segment"*, and it is the premise the *an Address is never a Node* refusal rests on. At the shipped `[roads]` it gives **33,024 Street Segments → 165,120 Lots**, against `World`'s independently-chosen 225 Lots per 1,000 Citizens = 225,000 at 1M — **two figures that never met, agreeing within a quarter**, which is the closest thing to corroboration this corpus has produced. **A Lot has no depth and there is no depth key**: a Lot has no extent in `LotTable`, so a depth would be a number chosen for a consumer nobody has designed (`adr/0070`). **UNRATIFIED**; the ratifier is the first Ruleset that models a city |
| Map | ~~4096² Tiles, 2048² documented fallback~~ **16384² Tiles — `CellGrid.WorldCells = 512`, 65.5 km a side, 4,295 km²** | world-creation, and **✅ BUILT 2026-08-13** ([`adr/0089`](docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)). ⚠ **It is NOT hash-bearing, which three documents predicted wrongly**: the flip moved none of the three golden baselines, because `05 §4`'s test asks whether a change moves *this* city and a map size is a bound on the cities that are *reachable*. So a design change came back reading as an **optimisation** — the comfortable failure mode, and worth holding. It is neutral **because `plans/0003` queue item 6 landed first**; before that the generator paved the map and this would have moved every hash in the project. A map is sized by **how many Commute Budgets fit across it** — 0.9 today, 3.7–5.2 at 512 — because that ratio, not area, decides whether the player can build separate towns or only one blob; ledger #1 sized it twice by area and never once by distance, and the one column that looked like a distance was the 73× units error `adr/0082` found. **1M is unchanged and so is the 3,700/km² developed density** — ⚠ *which is one fact, not two: 3,700 is an **output of the 1M target** and cannot corroborate a map (`plans/0012` **Cause 5**); the build-derived bracket is 2,738–5,136/km²* — the buildable fraction falls to **6.3%**, so a 1M city occupies 270 km² — *the whole of today's map* — and now sits in a region. Unbuilt ground is free per `adr/0021`, and the only structures scaling with map area are four **derived** `int[]` residency arrays, 262 KB → 4.2 MB. ⚠ **The blocker is a real defect**: `RoadGenerator` paves the entire map at world creation — 33,024 Segments today, **525,312** at 512, and **2.6M Lots against the 225,000 `World` allocates for 1M** — which is `adr/0021`'s *"scale with developed area, not map area"* being false in exactly one place, invisible until now because a 16 km map is one a city genuinely does pave. ⚠ **Its repair is *not* a design question and this row said so for a day** — `plans/0002` **ledger #2**, *open map or progressive unlock*, **closed as refused on 2026-08-12** ([`adr/0090`](docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)): the map is open, there is no unlock, and the player lays every Segment. The closure reached `plans/0002` and `plans/0003` and **not this row**, which is `plans/0012` **Cause 2** landing in the one file every cold start reads. ✅ **`plans/0003` queue item 6 SHIPPED 2026-08-13, so nothing stands in front of the flip and it is one line.** `RoadGenerator.LayInto` takes an **extent in Tiles** and `SyntheticCity` derives it from `World`'s own **225 Lots per 1,000 Citizens** — not from its Building count, which was tried first and moved the Unplaced Pool's equilibrium. A 1M city paves a **4,800-Tile lattice, 8.6% of a 512-Cell map**; the golden world goes **16,641 Nodes / 33,671 Segments → 36 / ~60**. ⚠ **And the blocker's ground was never true**: the generator has one production call site, `SyntheticCity`, reached only by `CommandKind.Populate` — *a verb no player has* — so a **player's** world has had no roads at all since 5a and `adr/0021` was not violated anywhere a player can reach. Three documents reasoned from a doc-comment saying *"at world creation"*; `adr/0093`, and its writing half is the fix — **name a symbol, never a time**. ⚠ **It cost 11.6 MB at 1M, not the ~4 MB `adr/0089` counted** — 192,780 KB resident against 204,412 KB, both measured — because a **fifth** map-scaled derived structure went uncounted: `StreetGrid`'s node and edge index, ~3.2 MB, **correctly sized from the map** since a player may lay a Street anywhere under `CommandKind.Connect`. ⚠ **And it broke three fixtures that were laying at map extent**, all the same way: `rulesets/severance.toml` stranded **0%** of pedestrians on the worst of eight seeds, so **the file that exists to demonstrate Severance had stopped demonstrating it with no number in it changed**; the walk-search benchmark's graph went 16,641 → 263,169 nodes; and the `[roads]` loader's two spatial maxima were `[InlineData]` literals, of which the refused one failed loudly and **the accepted one stayed green while ceasing to test a boundary at all**. Each now states the extent it was characterised at. ***A paved extent is not a map size.*** **UNRATIFIED**; ratifier is the first long run on the new size reporting a real developed-area distribution |
| Target population | 10,000 first hour / 1,000,000 late game | sizing |
| Tick budget | **15.6 ms at 4×, at 1,000,000 Citizens, on ONE CORE of the reference class** | ✅ **ARGUED AND SETTLED 2026-08-16 by session T** ([`plans/0027`](plans/0027-session-t-the-target-speed.md)) — [`adr/0105`](docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md) and [`adr/0106`](docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md). **The value did not change and its standing did**: 15.6 ms had sat in this table for the life of the project, denominating every percentage in the corpus, **stated and never chosen**. ⚠ **Quote the machine and the thread count with the number** — a 2020 six-core x86-64 desktop, i5-10400 class, DDR4-2133, `powersave`, **single-threaded**; a duration with no host is not a budget, and the *thread count* is the clause most likely to be dropped because it does not look like part of a duration. **Every rung of `01 §1`'s ladder is offered at every city size for ever**; a host that cannot sustain the chosen rung **dilates wall-clock time and reports *simulation running behind***, and **withdrawal is refused outright** — *taking options away while a city progresses is weird*, and the `Wait` step is hardest to close in exactly the city that would have lost its fast-forward. So `01 §1`'s *"the first thing a large city stops offering"* is **struck**. ⚠ **1× keeps a different job and is not the target**: it is the speed the game must be *enjoyable* at and the rung a **capability** is priced against — the Microscopic Cap stays on **62.5 ms**, because `adr/0096`'s revisit trigger fired, one of its two legs fell and the decision survived. ***A Cap and a bill are different objects***: dilation absorbs a bill, and nothing absorbs a permanently coarser city. ⚠ **The bill is ≥44–50 ms, so the target is ~3× away** — see the three-numbers paragraph above |
| The crossing cost | **30 s** in all three shipped Rulesets | tuning, hot-reloadable, **hash-bearing** — `[trips]` Ruleset data (`adr/0074`). What it costs on foot to reach the other side of a Segment. It exists because **side of street was kept rather than surrendered**: it lives as one bit on the **Address**, not as a second footway edge set, which would triple the graph and lose one Epoch and one search. The term applies only when two Addresses **share a Segment and differ in side**, so its blast radius is the across-the-street case and nothing else. ~~**Look for the derivation before picking a value** — a crossing is a real duration, and *half a signal period* is a property of a junction.~~ **The derivation was looked for on 2026-08-12 and there is none to be had**: this simulation has no signals, and the other candidate — the carriageway walked at walking pace — needs a Segment width `RoadSegmentTable` does not have. Both need an unbuilt mechanism, which `adr/0070` makes evidence of nothing, so 30 s is **chosen against the band `0002` §D states** — a third of the 92 s it takes to walk a 128 m block, and half a 60-second signal cycle from the other end. **UNRATIFIED, and the ratifier is half-run**: `--trips` moves the 1-block band's p90 detour **140% → 153%** and no other band at all, which is the geometric half; the behavioural half is 5b-bis task 8's run. The `[trips]` table was created with it (task 3) |
| Commute Budget | ~~**20 clock minutes**~~ **three rungs — fast 20, moderate 40, unsavoury 50 — ✅ BUILT 2026-08-13** | tuning, hot-reloadable, **hash-bearing** — `[trips]` Ruleset data. [`adr/0095`](docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md): `commute_budget_minutes` becomes the **ceiling** and two keys join it; **only the ceiling refuses**, and the two rungs below grade a commute that happens anyway. **A single threshold makes a cliff out of `adr/0017`**, the one rule the design says is graded — a city whose commutes creep from twelve minutes to nineteen reports **zero** and then reports a cliff, arriving exactly when `01 §4`'s spatial fix has stopped being cheap. **It opens no fifth Trip Fate**: three gradations of a cost in *time* all sit on the Budget's side of `adr/0076`'s second clause, so an unsavoury Trip **completes**, and the **unsavoury** rung is where `01 §4`'s *housed* Departures come from. The job-search box derives from the **ceiling**. ⚠ ~~**It restates `adr/0089`'s map ratio** — 5.6–7.8 crossings at the fast rung, **2.2–3.1 at the ceiling**, and the ceiling is what governs separability, so `WorldCells = 512` survives with less headroom than it was granted.~~ ⚠ **THAT RESTATEMENT IS WITHDRAWN 2026-08-14 — it plugged a foot-derived rung into a vehicle-denominated column** ([`adr/0089`](docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)'s **third correction**, `plans/0012` **Cause 5**, fourth sighting). `adr/0089`'s *Commutes across* column has always been a **vehicle** commute — the table carries *Foot crossing* as a **separate** column reading 18.5–26.2 h — and backing the speed out gives **25.2–35.4 km/h** before the amendment and **25.4–35.8** after, so **only the minutes moved**. These rungs are percentiles of a **foot-only** distribution, and this row says so four sentences earlier. **On foot the same map is 15.7 ceilings across, not 2.2–3.1.** The map decision stands on the vehicle column, which is the right one for a game whose commutes are driven; what falls is the claim that these three numbers corroborate it. ⚠ **And the same confusion is live in the code**: the box is a *time* × *walking speed*, so it covers **44.9×** the golden fixture's city and holds **100.0%** of the Buildings in the world at 4,000, 10,000 and 40,000 Citizens alike, first narrowing near **160,000** (`JobSearchBoxTests`, 2026-08-14). ***A box derived from a commute time can only ever filter in a foot-only world*** — at 50 km/h the ceiling reaches 41.7 km against a 19.2 km city at 1M — so it is inert **structurally** rather than temporarily, and 5b-bis task 4's *"looking beyond what could be accepted is looking where nothing can be found"* was **already false on the 1,000-Citizen fixture it shipped with**. Not repaired: an authored radius is a hash-bearing number with no ratifier either way, the walk search is the real filter, and the box measurably costs **1.06×** for a 5.34× widening. Filed to [`plans/0002`](plans/0002-open-questions.md) §C, together with the design question it exposes — ***walking is free of congestion by permanent decision (`03 §3.7`), so a compact city bypasses the traffic model, and the only built counterweight to compactness is pollution concentration***. **UNRATIFIED**; ratifier is playtest, and the refuting readings are an *unsavoury* rung never occupied or a *fast* rung never left. ✅ **BUILT 2026-08-13** — `[trips]` states three keys, the loader refuses a partial or non-increasing set, `EmploymentEngine` takes the **best rung it draws**, `JobCounter` has seven counters, and `--census` and `--commute` print all three. ⚠ **Building it found the ADR designed against one of `01 §4`'s two drivers without noticing**: that section names **Congestion** and **Separation**, and `01 §7`'s **Gridlock** overlay — *the commute-time distribution's upper tail sliding toward the Commute Budget wedge* — is the **congestion** reading, while `adr/0095` argues entirely from geography. Congestion cannot reach a commute by construction (`03 §3.7`: pedestrian networks do not saturate; `adr/0075` gives a Leg a cost and no path, which is `adr/0041`'s volume debt waiting on **5c**), so ***the three values are percentiles of a free-flow, foot-only distribution*** and a fourth revisit trigger says to reopen them **on the distribution** when 5c lands. ⚠ **No world this project can build occupies the top rung**, and it is the map rather than the numbers: the paved extent scales with **√population**, so 10,000 Citizens is **1.92 km** across against a ceiling reaching **4.17 km**, and over 1,024 Ticks *unsavoury* runs **0, 0, 10, 131, 738** at 10,000 / 20,000 / 40,000 / 80,000 / 160,000. **The fixture was not inflated to fill the rung and the values were not lowered to fit it** — 8/16/20 was proposed and refused with the user in the room, because ***a rung is a vocabulary and calling a twenty-minute commute unsavoury bends the words to fit a village***. ⚠ The committed baseline **lost** the refusal branch acquired three days ago: the ceiling moved 20 → 50 so `beyond` goes **3 → 0**. ⚠ **And the fast rung is mechanically load-bearing where the ADR's table says it does nothing** — *best rung drawn* stops on the first `Fast` candidate, so where that edge sits decides how many candidates are looked at and therefore who takes which vacancy (**2,307 against 2,301** employed on an identical city). ***An edge that refuses nothing is not thereby an edge that does nothing***, and a test bounds it at one percent. ⚠ **The tightest authorable ceiling is now 3 minutes**, where it was 1, because three strictly increasing rungs of ≥1 put a floor under it. *Original row follows.* The line between a Trip that completes and one whose Fate is *exceeded commute budget*, and since 5b-bis task 4 **also the size of the box a job seeker draws candidates from** — because looking beyond what could be accepted is looking where nothing can be found, and an authored radius beside it would be a second number that could contradict the first in silence. **Clock minutes, one currency across modes, and there is no per-mode weight** (`adr/0008`, session F). It is a **percentile of a Trip-cost distribution**, so it was unauthorable until one existed; `--trips` produced one, and 20 min is the rung that admits an eight-block neighbourhood at p90 and refuses half a sixteen-block one (`plans/0023` §B rules out the *median* specifically). **UNRATIFIED**; the ratifier is task 8's long run and its refuting numbers are named — a `jobs beyond budget` of **zero** means it is not binding, and `seeking − employed` at the whole population means it is too tight. ⚠ ~~**It is inert on the golden fixture and binding at 10,000 Citizens**, so the committed baseline does not reach the refusal branch~~ **The golden fixture went to 4,000 Citizens on 2026-08-13 (`plans/0003` queue item 6) and the Budget now refuses 3 walks out of 2,000 considered**, so the committed baseline *does* reach the refusal branch — acquired as a side effect of a change made for another reason. Under the shipped geometry `beyond` over 512 Ticks runs **0, 0, 3, 213** at 1,000, 2,000, 4,000 and 8,000 Citizens, so the fixture sits on the first rung that refuses anything at all. The negative assertion written *so the fact could not rot* is what said so, and it now asserts the branch is reached rather than how far past it the city is |
| Volume-delay function | **α 15%, β 4, clamp 400% — stated by `rulesets/congested.toml` only** | tuning, hot-reloadable, **hash-bearing** — `[traffic] alpha_percent`, `beta`, `clamp_percent` ([`adr/0099`](docs/adr/0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md), 5c task 6). BPR: a Segment's dwell is `free-flow × (1 + α × (v/c)^β)`, clamped, priced **on entry** from that Segment's volume at that instant. **α and the clamp are authored as percentages because the file has no decimals** — ***the name of a quantity is not its denomination***, and `alpha = 15` would be off by two orders of magnitude with nothing able to notice, which is `adr/0094`'s `Speed.PerKilometrePerHour` literal exactly. **Absent means roads never slow down**, which is also every city this project described before task 6, so an older Ruleset still means what it meant — and `minimal.toml`, `minimal-tuned.toml` and `severance.toml` all still omit it, so **`--traffic` refuses all three and points at `congested.toml`**. ⚠ **Sourced, not ratified**: 15 and 4 are textbook and are what S2 ran; **400% is recovered arithmetic** — S2 R8.0 published *an arc at the clamp costs 39.4× free-flow* and `1 + 0.15 × 4⁴ = 39.4` exactly, so the clamp is backed out of its own delay. S2 is **synthetic** and ran R4's uniform origin–destination draw, so nothing may be carried out of it bare (`plans/0012` **Cause 5**). ⚠ **β is bounded by arithmetic rather than taste** — 4⁸ is 65,536, exactly Q16.16's whole range — so the loader refuses above 6. ⚠ **A generated city never reaches a load where any of it matters**: `v/c` peaks at **0.44** at 4,000, 16,000 and 64,000 Citizens alike, because the paved extent scales with **√population**, so ***the same number sizes both the demand and the supply***. Congestion is something a **player** makes by laying too little road (`adr/0090`) and `CommandKind.Populate` cannot reach it — **third sighting of a dial inert at the shipped configuration**, after `foot_crossing_every` and the job-search box, and the second where it is *structural*. Both sides are asserted: the shipped city is byte-identical with and without the function, and a city whose Streets carry 200 Vehicles an hour is not. ⚠ **Two defects found by measuring rather than reasoning, both of which would have shipped silently.** The ratio divides a Vehicle **stock** by a Vehicle **flow**, and the conversion is **Little's Law** — a Segment at capacity holds `capacity/Tick × crossing time` = **9.2** Vehicles on a 128 m block (a 14 m spacing), where dividing by the flow alone gives 42 (a 3 m spacing, i.e. a car park). The first cut skipped it on the strength of `adr/0041`'s *a vehicle crosses about one Segment per Tick* — **stated as following from `TICKS_PER_DAY = 8192`**, and `adr/0094` made it **~4.6** without touching that ADR, so the function came back **×1.0000 at every population**. ***A premise licensing one quantity to stand in for another is itself a measurement, and a constant moved in another document can retire it silently.*** And flooring capacity to whole Vehicles/Tick loses everything under 2,048 a Day, so a narrow road hit the guard written for an **absent** capacity: ×2.48 at 200 Vehicles/hour and ×1.0000 at 60, which is not a curve any function has — ***a guard written for an absent quantity will fire on a small one***. ⚠ **STATED as of 5c task 7, in one file written for the purpose**, so it is a **debt rather than a gap** now — `rulesets/congested.toml`, which also cuts `street_capacity_per_hour` 3600 → 400 because at the shipped 3,600 nothing this project can generate reaches a load the function can price. ⚠ **The instrument that reads it is silent over most of its own range**: the two panels of `--traffic` come out **identical** at every capacity from 3,600 down to 600 and again at 200, and differ only at 400 — above, the extra dwell is *sub-Tick*, so the bill moves and a per-Tick volume snapshot cannot see it; below, every block clips at *over capacity* in both. ***The bill and the picture are driven by different thresholds.*** **UNRATIFIED**. ⚠ ~~**named ratifier is 5c task 8's long run**~~ **THAT RATIFIER RAN AND COULD NOT FIRE, 2026-08-15** — load came out at **0.0018 / 0.0048 / 0.0110** Vehicles per Segment per Tick at 4,000 / 16,000 / 64,000 Citizens, growing as roughly `P^0.66`, so ~**0.07** at 1,000,000 against a Segment holding **1.02** on this file's capacity and **9.2** on the shipped one. **No length of run varies the quantity**, because the generated lattice grows with the population it serves. **Named ratifier: a long run over a city whose Streets were laid by `CommandKind.Connect` and deliberately under-provisioned** — a machine ***and a world***, which is [`adr/0052`](docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)'s 2026-08-15 amendment and the finding this row produced. ✅ **That world was built the same day** — `ConnectedCityCongestionTests`, a dumbbell of two zoned districts joined by **one** Street corridor at the **shipped** 3,600 Vehicles an hour: `v/c` peaks at **65.1%** against a generated lattice's **43.4%** on the same Ruleset, and four times the district on the same one corridor puts the priced world at **1,074%** where its free-flow control sits at **130%**. **Refutable is the precondition, not the ratification** — ~~what remains is a reading somebody takes and calls right or wrong~~ ✅ **THAT READING WAS TAKEN 2026-08-15 AND NEITHER REFUTATION LANDS.** Peak `v/c` runs **65.1 / 97.7 / 1,074.3 / 2,767.0%** over four rungs; loaded against free-flow occupancy runs **6,784/6,784 → 122,725/80,978**, so *decorative* fails; and the clamp takes **0.00 / 0.00 / 0.04 / 0.52%** of loaded Segment-Ticks, so *binds routinely* fails at a peak **6.9× the clamp**. ⚠ **Read the clamp share as a steep trend and not a level** — ~13× a rung against a 2.6× peak — so it is *not routine here* and never *never routine*. ⚠ **The largest finding is not the row**: the priced and free-flow runs agree **to the Citizen** on who is employed and on every rung count at every rung while their occupancies differ by **51.6%** — [`adr/0046`](docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md) working as decided, ***congestion is a cost paid and never a cost avoided***, and the corpus sweep's *the traffic model has no feedback term* arriving with a number. An equality assertion now, so a driver model closing `03 §3.4`'s loop breaks the fixture and names the loop. **Still formally unratified**: both stated refutations were attempted on the world this row named and neither landed. ⚠ **And it found something bigger than the fixture: *the volume-delay function is a loop and not a formula*** — congestion slows a Vehicle, a slower Vehicle dwells longer, longer dwell **is** higher volume — so past `v/c` = 1 it feeds itself, and the corpus has only ever quoted BPR as a static curve. That is `03 §3.2`'s *use it only where it is strong* arriving with a number. Both rows moved **D2 → D1** in `plans/0002` |
| Household car ownership | **100% — stated by `rulesets/congested.toml` only** | tuning, hot-reloadable, **hash-bearing** — `[households] car_ownership_percent` ([`adr/0098`](docs/adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md), 5c task 5). **`01 §8` ledger #2's exogenous half, built.** A Household keeps a car or does not; a Citizen of one that does **drives everywhere** and one of a Household that does not **walks everywhere**. ⚠ **Mode choice — weighing a walk against a drive on the day — is a *different* question, settled by no ADR, and it is in neither of `06`'s two inventories**: it is `adr/0070`'s **undesigned** class, and ***an inventory of unplaced mechanisms structurally cannot list a mechanism nobody designed***. `06` now carries a sibling table that says in its own header it cannot be complete. **Ownership is derived from the Ruleset in force and is never a column** (`adr/0068`, `adr/0064`, a fourth axis), and what that buys beyond avoiding a frozen bit is that the owner set is **nested** — a fixed per-Household draw against a moving threshold — so **both saved alternatives fail in opposite directions**: re-rolled on reload churns the whole city for a one-point change, not re-rolled does not respond at all. **A car commute is three Legs** — `adr/0008`'s `walk → drive → walk`, the two flanking walks being session F's named placeholder and **zero-length** until milestone 8. ⚠ **The first cut built one Leg and violated that ADR**, reasoning from `World.VehicleAccessPoint`'s doc-comment, which forbids a **fallback** from an exhausted Parking Shed and says nothing about the count: ***a doc-comment forbidding one shape is not a decision permitting the others*** (`adr/0093`), and a **prohibition read as a specification is silently permissive everywhere it does not reach**. `EmploymentEngine` judges a candidate in the **seeker's own mode**, which is `adr/0008`'s single clock read in the mode the journey is made in. ⚠ **Absent means nobody drives, and it is reached by omitting the table rather than by a defaulted key**, because zero is a legitimate answer and a placeholder inside the range cannot announce itself. ⚠ **Every drive is quoted too cheap** — free-flow, no congestion — until 5c task 6. ⚠ **STATED as of 5c task 7**, so a **debt rather than a gap** — `rulesets/congested.toml` sets it to **100**, and 100 rather than a partial rate because that file exists to put Vehicles on Segments and a partial rate would make the demonstration a measurement of the rate instead of of the function. The other three shipped files still omit it, and **`--traffic` refuses a Ruleset that states `[traffic]` and not this**, because volume is vehicular by decision and the picture would be empty for a second, different reason. **There is no derivation to find and that search is recorded as having failed**. ⚠ ~~named ratifier is 5c task 8's long run~~ **THAT RATIFIER RAN AND COULD NOT FIRE, 2026-08-15**, and it failed for a *weaker* reason than the volume-delay row — its refuting readings are about **reach** and a generated city can produce those, but ***on a city that never congests, driving has no downside at all***, so any rate is as good as any other. ***A ratifier can be defeated by the world without being defeated by the instrument*** (`adr/0052`, amended 2026-08-15). **Named ratifier: the same under-provisioned `CommandKind.Connect` world** — the first city in which owning a car costs something — with the reach readings unchanged. ⚠ **THAT WORLD WAS READ 2026-08-15 AND BOTH REACH READINGS FIRE — and they refute the wrong thing.** `jobs beyond budget` is **0** and `moderate` and `unsavoury` are **0** at all four rungs, with `fast` at 3,630 / 8,294 / 14,436 / 22,698. **The cause is not the rate**: at 100% ownership every commute is a **drive at 50 km/h across a city at most 4 km wide**, while `adr/0095`'s three rungs are percentiles of a **foot-only** distribution — which this table's own Commute Budget row says four sentences from where the ratifier was written. ***A refuting reading named against one consequence cannot refute a number whose live consequence is a different one***: this rate is **inert on reach and load-bearing on congestion**, and it is congestion the world was built to expose. **So `adr/0052`'s amendment wants a third clause — a ratifier names a machine, a world *and a quantity*.** What is owed is a different pair of refuting readings, denominated in what a drive costs rather than in how far it reaches |
| ~~Commute peak factor~~ **The Shift model — three numbers** | `shift_start_earliest_hour`/`latest_hour` **6–10** on the employing kind; `[jobs] shift_hours_min`/`max` **6–10**; `[jobs] arrive_early_max_minutes` **15**. All four shipped Rulesets | tuning, hot-reloadable, **hash-bearing** — [`adr/0101`](docs/adr/0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md), built 2026-08-15. ⚠ **`commute_peak_factor` is RETIRED**, the **third** `plans/0002` §D row to lose a quantity rather than gain a value. A commute is now **two journeys**, both anchored on a Shift start hour belonging to the **Workplace** — so a Citizen who changes jobs changes their hours, reads them off `Workplace` and **stores no start hour at all** (`adr/0068`'s disposition on a fifth axis). A Building draws its hour inside its kind's band; a Citizen draws a Shift length and a punctuality margin. Departure is `start − planned commute − margin`, and **`planned_commute` is the one saved column** — what the journey cost *when the job was taken*, because no function of an id and a Ruleset recovers a fact about a past world: ***a value drawn once is derivable and a value measured once is not***. **The Day's shape is emergent**: a morning peak from the band's mass, a broad one because living further out means leaving earlier, a flatter evening because one start hour meets many Shift lengths, and a quiet night because a band with no mass there cannot put anybody on the road. **Tick 0 is midnight**, spent here because a Day with a gap between two peaks distinguishes its own ends. ⚠ **The ADR was wrong about its own mechanism in three places and the build corrected it**: the start draw is **triangular** (the mean of two draws) and not uniform — warranted because *a return is `start + shift`, the sum of two uniforms, which is already triangular*, so the shape was borrowed from inside the mechanism rather than invented; a Shift length is drawn in **Ticks** and not in the hours it is authored in (***the unit a quantity is authored in is not the unit it is drawn in*** — `adr/0094`'s lesson a third time); and there are **five** hash-bearing numbers where it counted three. ⚠ **The middle of the Day is EXACTLY ZERO and that is land use, not the draw** — `minimal.toml` declares one employing kind, so every workplace keeps the same hours. A second kind closes it with no engine change (test-local variant, `CommuteLongRunTests`), and ***a Day's quiet end is bounded by `latest start + longest Shift`***, so a **later** second kind fills the afternoon and **wakes the night** — only an **earlier** one adds midday traffic, by putting its *returns* there. ⚠ **Its sharpest finding is not about commutes**: ***an intrusive index that unlinks by recomputing its key cannot outlive a change to that key's inputs***. The roster recomputed a bucket from a `Reference.Severable` Workplace handle, so demolition made `Remove` a silent no-op and every re-employment double-linked — an `adr/0006` leak with a quadratic tail, repaired by a derived `CitizenTable.CommuteBucket`. **The old single-partition roster was safe only by luck**, its key being an id nothing can change. **UNRATIFIED**; ratifier is **a picture** — the departure profile over a whole Day — which is a real ratifier and not a category, because a profile with one spike, an empty afternoon or a busy night refutes it on sight |
| Microscopic Cap | **unset** | fixed world constant, still open. **It counts *Vehicles*, not Segments** (`adr/0062`) — the unit was wrong in three places and is the family of `adr/0053` and `adr/0059`. Its value is a **ratio nobody has both halves of**: Vehicles affordable in a Tick (**S5**) against Vehicles a real city stresses at once (`06` **5b-bis**, read 5b before 2026-08-12). ⚠ **The budget it is priced against is now the *design speed's* 62.5 ms, not 15.6 ms** ([`adr/0096`](docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)) — **~25,400 Vehicles, one core**. Pricing at the top rung chooses `03 §3.9`'s *simulation* degradation for every player permanently in order to avoid its *hardware* one on one machine, which is that table read backwards, and `01 §1` makes 4× the rung a large city withdraws anyway. ⚠ **Never quote 186,624 bare.** It is 2,592 Segments over an 80% stress threshold × 72 Vehicles a Microscopic Segment (`adr/0082`) — a real stressed-Vehicle estimate, and an **upper bound**, because S2 R2's uniform origin-destination draw is the longest-trip distribution available. Against `adr/0096`'s basis the gap is **7.3×**, not the 27–58× published and withdrawn on 2026-08-13. ***A caveat attached to a number does not travel with it*** (`plans/0012` **Cause 5**) — and the correction to that error dropped a caveat of its own, reading `plans/0013`'s compressed *not a stressed count* as *a whole fleet*, so **the registry entry names `upper bound` and points at the ADR that derives the number**. ~~**Two things owed**: a **2- and 4-thread** Lane kernel measurement (S5), the largest unclaimed multiple on the supply side; and~~ ⚠ **The threading measurement RAN 2026-08-14 and half-closes** (`spike-results` → *S5 L6*): **2 threads is 1.84–1.93×, settled**, so the pass scales and every one-core figure here is a floor rather than the answer; **4 threads is bimodal between ~2.5× and ~3.9× and is still owed**, because the pass needs four pinned cores clear at once and *the more parallel the thing being measured, the less a busy machine can measure it*. ⚠ **Quote the supply-side multiple as *at least 1.84× and plausibly near 4×*, never as 4× bare** — `adr/0096` exists because a number travelled without its clause, and a bare *near 4×* would be that failure with a friendlier number. ⚠ **The interference was another session's `dotnet test` at ~1018% CPU, not the desktop**, so the re-capture's first control is *nothing else running in this repository*; ***a spike measuring parallel scaling cannot share a machine with a code session running a test suite***, which is a second axis on which the board's *the three tracks do not contend* is false. **A free second result**: `05 §4` **lint 4** has its **first evidence in this project's life** — 0 disagreeing rows of 294,912 at every rung across eight captures — holding *by construction* because the Lane pass is wholly Lane-local, and ⚠ **not a discharge**, since lint 4 is about `step()` over the whole world and this is its easy case. Still owed: a **fallback tier below Microscopic**, recorded as foreseen and deliberately **undesigned** under `adr/0070` until the demand side exists |
| Sight Horizon | **1 Segment — DERIVED, and there is nothing to choose** | **Not tuning.** The floor is graph geometry (R8.1) and the **ceiling is comparison symmetry** (`adr/0046`, session D task 4): a driver has its own route and can read `N` live arcs along it, but has no route down an alternative beyond the first arc, so above 1 the comparison is asymmetric and biases diversion without bound. **The other parameter this name was wearing is the Rejoin crossing budget** (`adr/0061`), which is unset and is R6.4.2's cliff at 3. *Original row:* ~~tuning, hot-reloadable.~~ Its **floor** is a Road Graph property — the distance to the next node with a real choice — and S2 R8.1 derives it at **1 Segment** (`adr/0046`). **⚠ R6.4.2 found this is two parameters wearing one name**: rejoin success cliffs 19.14% → 85.74% at Horizon **3**, because rejoining means going round a block and a block is three Segments. **1** is *noticing a choice*; **3** is *recovering a route you have left*. `adr/0046` sets neither |
| Temperament base and spread | **unset** | tuning. Stable base plus per-decision jitter, two `purpose_tag`s. **The base/jitter blend weight has no argument behind it at all** and is the routing model's weakest number |
| Habit refresh cadence | **infinite — static per world. RATIFICATION WITHDRAWN 2026-08-10; the value stands and nothing now rests on the ratifier** | **The first ratification this corpus has taken back**, by session D task 5: R8.5's own limit clause forbids carrying its fire rate *"to any scheme that gives a Traveller more than one candidate route"*, and `adr/0060` has made a Habit exactly that. **Withdrawal costs nothing** — `adr/0061` supplies adaptation as a *switch between static candidates*, so no cadence and no hash-bearing number returns whatever a re-measurement says. Both qualifications are now closed rather than open: session M's is **two claims with two owners** (`adr/0046` owns *static under congestion*, `adr/0012` owns topology and says plainly it is **not** static — that is `T`, unset in `0002` §D2), and **S2 R7 discharged the `adr/0047` one on 2026-08-11** by writing the direction down: dispersal gives a jam more places to redistribute to, so self-correction closes **at least as easily** on a variant-supplied route set. Full text in `0002` §D |

## Definition of done for any milestone

This list is owned here; `docs/06-roadmap.md` rule 2 requires it and cites it. Cumulative obligations,
not milestones of their own. Refined per slice by `plans/0003 §Definition of done`.

- `dotnet build` succeeds and `dotnet test` is green, on a machine with no GPU and no Godot
- The invariants pass. **Sorted by frequency, never gated on build configuration** (`02 §10`) —
  `O(1)` at the write site per Tick, `O(n)` staggered, whole-world at end of run. The runs that
  surface these bugs are the million-Tick headless balance runs, and those are release builds
- The long-run test passes — 100k+ Ticks with **no collection and no magnitude** trending upward at
  steady state (`adr/0006`, and `adr/0003`'s extension of it to quantities)
- There is something to *look at* showing the milestone doing its job

Every milestone names the specific risk it retires. A milestone that cannot name one is either
not necessary yet or not understood well enough to start.

## Things to be careful about

- **Don't reach for an ECS.** `adr/0004` rejected it explicitly: the population is homogeneous
  and ECS earns its complexity through heterogeneous composition.
- **Don't add a demand scalar.** There is no RCI meter. The Unplaced Pool *is* the demand signal.
- **Don't collapse Citizens into groups.** No Cohorts, no shared decisions, ever (`adr/0005`).
- **Don't let fidelity depend on the camera.** Fidelity is a property of place, driven by Stress
  (`adr/0007`). The renderer cannot influence the simulation.
- **Don't add a collection without a sink.** `adr/0006` — nothing grows with elapsed time.
- **Don't move a mechanism between Rule families for performance.** Bin Rules and Sweep Rules
  differ in observable behaviour, so moving one is a change to the city (`adr/0033`).
- **Prefer off-the-shelf infrastructure.** `adr/0018` — Citybound shipped ten bespoke libraries,
  three engine rewrites, and no game. A bespoke component requires a written exception naming the
  property no library provides.
