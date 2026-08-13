# 0023 — Jobs and the commute (milestone 5b-bis)

> The slice brief for [`06`](../docs/06-roadmap.md) milestone **5b-bis**, *Jobs, the commute, and the
> first Trip generator*.
> Decisions to be built: [`adr/0081`](../docs/adr/0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md)
> (the milestone's own), [`adr/0017`](../docs/adr/0017-agents-satisfice-they-never-optimise.md),
> [`adr/0068`](../docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md),
> [`adr/0069`](../docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md),
> [`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md),
> [`adr/0076`](../docs/adr/0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md).
> Design realised: `02 §5.3`, `03 §3`, `CONTEXT.md` → Commute Budget, Trip, Workplace.
>
> **This is a planning document and therefore cites rather than owns**
> ([`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)). Every
> figure below names its owner. If this document and its owner disagree, the owner is right.

## Status

**🔨 IN FLIGHT (2026-08-12), and nothing gates it.** Milestone 5b closed the same day
([`0021`](0021-trips-legs-and-the-pedestrian-layer.md)); this milestone was created by the sitting that
closed it, and both of its ADRs were written then. There is no session in front of it and no measurable
claim it must wait on.

**It inherits three tasks from 5b** — the generator, the Trip Census family with the Commute Budget, and
the 100,000-Tick run — because each measures an origin-destination **distribution** and 5b had none to
measure.

## Why this milestone exists, in one paragraph

**Phase 2 as sequenced never produced a place a person would go.** All seven Trip generators the corpus
names sit in `06`'s *Mechanisms with no milestone*, so `0002` §A — *what generates a Trip* — had no
answer at any point in the plan, and 5b's tasks 4, 6 and 8 were each void as posed. The fix under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) is **build X**, and
`adr/0081` chooses which X: **the commute**, because it is the Trip the rest of the corpus is already
written against and the **only candidate that exercises the Commute Budget**, whose named ratifier is
*the first run long enough to produce a Trip cost distribution*.

## The named risk

`06` rule 2: **that every number milestone 5b was to produce is taken against a fabricated
origin-destination draw and lands in [`0013`](0013-tick-budget.md) and [`0002`](0002-open-questions.md)
as measured fact.** This is not hypothetical — **S2 R4 ran the experiment on the corpus's own
instruments**: a uniform origin-destination draw put a District-granular route's detour at 18.52% where a
local-trip draw puts it at **128.82%**, *"which under `05 §4` is a different city"*. A sampler would not
approximate the answer; it would fabricate one with the right shape.

> **⚠ And this milestone carries a known bias, stated by `adr/0081` before anything is measured.**
> Job choice is a function of **wage and commute** ([`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md))
> and this builds the second term only, so **nothing makes a Citizen travel past a nearer acceptable
> job**. The commutes come out **too short** and every quantity derived from them — the Commute Budget
> percentile, peak pedestrian density, `0013`'s walk-search multiplicand — is biased **low**. *A number
> biased in a known direction is usable; the same number with an unknown bias is not.* Every figure this
> milestone publishes must carry that qualifier at its site, not only here.

## Tasks

**1. The spatial index: a query from a place to the Buildings near it.** It exists nowhere —
`CellResidency` indexes Map Layer cells rather than entities, and `Frontage` maps a Lot to its Segment in
one direction only. **It is task 1 rather than a detail of task 4** because it is the piece that is
reused: job assignment needs it, [`adr/0067`](../docs/adr/0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md)'s
Provider List acquisition will need it, [`adr/0032`](../docs/adr/0032-services-are-delivered-by-trips-not-by-coverage.md)'s
Attended services will need it, and `02 §5.3`'s candidate sampling **already assumes it**. Built inside
one consumer it acquires that consumer's shape and is then rewritten twice.

> **The shape is already set by two things in the tree and should not be re-derived.** `CellResidency`
> is the precedent for *where it lives*: a flat array over the 128² Cell grid, plus-one encoded so a
> zeroed entry reads as **absent**, `(derived AND rebuilt)`, and **beside the table rather than in it**
> — `BOR0901` rejects storage in a `[Table]` that is not a declared column, and a per-Cell head is not
> per-row storage. `CLAUDE.md`'s standing rule is the precedent for *what it is*: **an intrusive index
> list** — a head index on the owner, a `next` index on the element, both flat. So: a head array over
> Cells beside the table, a `next` column on the Building rows, and no per-entity collection object.
> The query itself follows `layer_cells(aabb, layer)`, the project's first hot query — **allocation-free
> and string-free, and both are checked**.
>
> **It must be `(derived AND rebuilt)` and the test for that is `05 §3`'s, not convenience.** A
> structure earns that classification only if its **order** is recoverable from saved state rather than
> merely its membership. A Building's Cell is a function of its Lot's saved coordinates, so a rebuild
> reproduces this **exactly rather than plausibly** — but *only if the walk that rebuilds it visits rows
> in slot order*. Get that wrong and two identical cities disagree, which is the failure `0020` found
> for a wholly-derived table joining `World._tables`.

**2. `[[building]] jobs`, a fourth key.** Beside `name`, `bins`, `condemn_after` and `occupants`.
**Tuning, hot-reloadable and hash-bearing**, and subject to `adr/0068`'s rule for occupancy: **derived
from the Ruleset in force rather than frozen at construction**, so lowering it **evicts** the overflow. A
job has a holder and no consumer, exactly as occupancy does — *a Bin drains because a Bin has a consumer;
occupancy and employment do not, so they evict.*

> **This is where a second `[[building]]` kind enters a shipped Ruleset for the first time.** `dwelling`
> has been the only kind in all three files since the Ruleset existed, so **every loader path and Zone
> Rule path that has only ever seen one kind gets its first real exercise**, and `adr/0055`'s
> permission-set scoping gets its first case with something to distinguish. Expect defects here that are
> nothing to do with jobs, and file them where they belong rather than working round them
> ([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)).

**3. The `[trips]` Ruleset table.** It does not exist. It is created here and it is where the **Commute
Budget** and the **crossing cost** land — both hash-bearing, both currently unset, and **neither may be
chosen by argument** (`adr/0052`). The Budget's named ratifier is *this milestone's own first long run*,
so it is set from a **measured percentile** or it is not set at all; `CONTEXT.md` → Commute Budget fixes
its currency as **clock minutes, one currency across modes, with no per-mode weight** (session F). The
crossing cost applies only where two Addresses **share a Segment and differ in side** (`adr/0074`), and
**the derivation should be looked for before a value is picked** — a crossing is a real duration and
*half a signal period* is a property of a junction.

> **⚠ §B says which percentile the Budget must not be: the median.** It is 100% of the grid ideal in a
> severed city and an intact one alike, so a median Budget cannot distinguish the two cities the
> Severance demonstration exists to tell apart.

**4. The assignment pass.** A Citizen with no Workplace samples candidate Buildings with a free job slot
and takes **the first acceptable one** — `adr/0017`, satisficing, never the nearest and never the best —
where *acceptable* means **within the Commute Budget on foot**. It writes `CitizenTable.Workplace`.

> ~~**`SyntheticCity`'s `(i * 7) % buildings` stride is DELETED, not left as a fallback.** Two ways to
> acquire a workplace is `plans/0012` *Cause 1* with both copies live and executing. **This moves the
> State Hash**, so the golden baselines re-record.~~ **✅ DONE IN TASK 2 (2026-08-12), and the ceiling
> is what deleted it rather than this argument.** The reasoning above is right and it is not what
> happened: `[[building]] jobs` made employment a quantity the Ruleset grants, no shipped Ruleset
> grants any, and so the stride's 1,000 workplaces were all jobs that did not exist — dismissed *en
> masse* by `EvictOverflow` at the golden session's reload, as a moved hash rather than as a
> paragraph. The baselines re-recorded there. **Task 4 therefore only adds**, and a Citizen's
> Workplace is written by nothing in the meantime.
>
> **⚠ Predict three hash-bearing numbers here, because `adr/0081` predicts none and `adr/0069` is the
> standing counter-example.** Placement's ADR predicted no numbers and the pass needed **`interval`,
> `revisit_ticks` and `candidates`** — and the first of those shipped at a value copied from a default
> that meant something else, leaving 45% of the housing stock empty. This pass is the same shape: a
> sampled sweep over a population looking for something. **Derive what `adr/0059` says to derive** — the
> *sample* comes from a **duration** and the duration is authored — and put every one of them in `0002`
> §D with a named ratifier on the day it is written.
>
> **What happens to a Citizen whose Workplace is demolished is a decision, not a detail.** `adr/0054`'s
> precedent for a demolished dwelling is that the Households go to the Pool **with their money intact**;
> the employment analogue is that the Workplace clears and the Citizen re-enters assignment. Say it
> explicitly, because the alternative — a `HandleColumn` quietly resolving to nothing — is `Column.cs`'s
> own warning about a walk that cannot tell *unemployed* from *corrupt*.

**5. The commute generator, and the daily occasion.** A Citizen with a Workplace generates a commute Trip
on a daily occasion, through the same door 5b built: the Trip, its Legs and its Traveller, resolved in
Tick phase 4 by `TripEngine`, which needs **no change** for this. ~~`TripPurpose.Commanded` is deleted here
— it exists so that the absence of a generator was legible in data, and its own doc comment says it is
expected to go.~~ **⚠ IT IS KEPT (2026-08-12), and the reason is that its job changed rather than
ended.** The sentence this rests on is `adr/0080`'s, and that ADR says the verb *"becomes a test
affordance rather than the only door"* — a **demotion**, which is not a deletion. The value is worth
*more* after the generator than before it: while every Trip was commanded it distinguished nothing, and
now it is the only thing that tells a fixture's Trips from a city's, so its own rule — *a Trip with this
purpose in a real run is a Trip nobody meant to make* — became **checkable** on the day it stopped being
vacuous. Deleting it would have left `CommandKind.Trip` either untagged or lying.

> **The occasion is this milestone's one open design question, and `adr/0081` says it is small.** A
> commute is **scheduled and periodic**, so the sampled-sweep shape the Zone Rule and placement already
> use is available and **generalising the Event Wheel to a second table is not required**. Argue it here
> rather than assuming it — `adr/0059` is the standing warning about deriving a sample from a duration
> against choosing one.

**6. The Trip Census family, and the Commute Budget measured.** 5b's task 6. The four `TripCounter` flows
already exist and are already read; what this adds is the **cost distribution** the Budget is a
percentile of, and `TripFate.ExceededCommuteBudget` becoming reachable for the first time — it is
structurally unreachable until the Budget exists as Ruleset data.

> **⚠ Two corrections, both found on 2026-08-12 while building it.** *"Already read"* was **false**: the
> four flows reached the Census and reached `--census` through nothing at all, so for a whole milestone
> a Trip Fate was readable only by writing a test. And the Fate was made reachable in **task 4**, when
> the Budget was set, not here — it is reached in an ordinary run by the *placement* pass moving a
> Household after the job was accepted. **The distribution this task builds cannot ratify the Budget**,
> because a commute exists only where the Budget already admitted one; the uncensored distribution is
> `--trips`', which is why task 3 refused to set a Budget before taking it.

**7. Something to look at.** `--zones`, `--roads` and `--trips` set the precedent, including **refusing
rather than degrading** when the Ruleset declares nothing. The picture worth printing is not a Trip
count: it is **where people work against where they live**, and the same city after the jobs move.

**8. The 100,000-Tick run.** 5b's task 8, and `06`'s definition of done: no collection and no magnitude
trending upward at steady state (`adr/0006`, and `adr/0003`'s extension to quantities). Plus 5b's task 4,
**peak pedestrian density**, which is the measurement the whole re-scope was about.

> **⚠ Read slice 10 task 11 before recording anything from this run.** *A baseline records what a run
> did, so a change that narrows what the run reaches is invisible in it by construction* — and
> `GoldenSessionCoverageTests` exists because 5a-bis nearly retired a verb from the baseline while
> producing a full set of freshly correct hashes. A commute that never fails reaches none of the Fate
> branches this milestone exists to exercise.

## What this milestone must not do

- **No wage, no Business, no vacancy posting, no labour market.** `adr/0026` is still owed a milestone,
  and `06`'s no-milestone row keeps its entry with the **assignment half struck**. A row deleted because
  part of it shipped is `plans/0012` *Cause 3*.
- **No second destination per Citizen.** One Workplace, one Trip a Day. `adr/0067`'s shopping and
  `adr/0032`'s school make a Citizen's day a **schedule** rather than a repeated Trip, and that is the
  point at which the daily occasion stops being a sweep — it is `adr/0081`'s own revisit trigger, not
  this milestone's scope.
- **No Segment volume, and the reason is not the one that looks obvious.** 5b left
  [`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)'s
  attribution unbuilt because direct attribution *"needs a next Segment every Tick"* and
  [`adr/0075`](../docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) gives a Leg **a cost and
  no path**. Adding a commute does not supply one. **Volume is 5c's**, and the conservation invariant
  shipped in 5b is waiting for it.
- **No `Fidelity`.** Volume is 5b's and 5c's; the threshold that reads it is 7a's.

## Definition of done

The cumulative list in `CLAUDE.md` applies unchanged. Specific to this milestone:

- A Citizen's Workplace is **acquired by a mechanism**, and `SyntheticCity` no longer strides.
- A commute Trip is generated, routed, resolved and counted, with **every one of `adr/0076`'s four Fates
  reachable** — and the run that proves it says which ones it actually reached.
- The **Commute Budget is set from a measured percentile of this milestone's own distribution**, with the
  percentile argued and the bias qualifier attached, or it is **not set** and `0002` §D says why.
- The spatial index is **allocation-free and string-free on the hot path**, both checked, and it has a
  test that a rebuild reproduces it **exactly** rather than plausibly.
- All three golden baselines re-recorded, with the commit message saying what moved and why.

## The record

*(Filled in as tasks close.)*

### Task 1 — the spatial index, built

**`Space/BuildingResidency.cs`**: a Cell → Buildings reverse index, `(derived AND rebuilt)`, plus the
hot query `In(CellRect, BuildingTable, Span<int>)`. Two flat `int` arrays over the 128² Cell grid for
head and tail; `BuildingTable.CellNext` is the element side. Maintained at `World.CreateBuilding` and
`World.DestroyBuilding`, rebuilt in `World.RebuildDerived` after the Lot reverse index. Nine tests.

**Nothing here was invented, and that is the report.** Every choice was already made somewhere in the
tree and the task was to find which precedent governed: `CellResidency` for *where a coordinate-owned
head lives* and why `BOR0901` makes that correct rather than a dodge; `CLAUDE.md`'s intrusive-index-list
rule for *what it is*; `MapLayers.LayerCells` for the hot query's shape, down to truncating rather than
throwing; `Frontage` for *why it is not a registered table* — `plans/0020`'s finding that a wholly
derived table folds the allocator's four scalars and makes the hash depend on how many times it was
rebuilt.

**⚠ It is not a catchment, and the terms had to be kept apart deliberately.**
`LineSourceQueries.Amenity` already defines a catchment as *"a walkable catchment on the Road Graph — a
**time** rather than a distance"*, whose range *"no geometry on the Cell grid can express"*. This
answers the strictly geometric question and answers nothing about reachability. **The two stages must
not be merged**: the box supplies candidates and the walk decides acceptability, so *a Building across
an unbridged motorway is a candidate that fails* — which is a Severance the model can report. An index
that pre-filtered by reachability would delete that reading and would need the Road Graph's Epoch.

**The one real decision was `IndexList` gaining a `Span<int>` constructor.** Its owner here is a **Cell**,
which has no row, so head and tail cannot be columns; the element side stays a column because an element
*is* a row. The asymmetry is the point rather than a compromise — what makes it an index list is the
threading, not where the head is kept. The alternative was re-implementing the linking inside
`BuildingResidency`, which is a duplicate of the one structure `CLAUDE.md` says every collection in the
core must be.

**The test that licenses the declaration is the recycled-slot one**, and it is the only one of the nine
that discriminates. `InsertOrdered`'s own remarks give the failing sequence — *create A and B, free A,
create C, and appending gives `B, C` where a rebuild gives `C, B`* — so a maintained list built with
`Append` passes every membership assertion and fails only there. In production it would present as a
saved-and-reloaded city draining a list in a different order from a continuously-run one, **with nothing
to report it**, because the list is derived and therefore not folded.

**No baseline moved**, because `cell_next` is derived and derived columns are not hashed. That is
worth stating rather than assuming: it is the reason this task could ship separately from task 4, which
does move the hash.

### Task 2 — `[[building]] jobs`, built

`KindDefinition.Jobs` beside `Occupants`, the loader key with its negative refusal, and
`World.TryDeclaredJobs` / `HasJob` / `Employ` / `Dismiss`. `EvictOverflow` gained a second loop, drawn
against its own `PurposeTag.JobEviction`. **The worker list came with it** —
`BuildingTable.WorkerHead`/`WorkerTail` and `CitizenTable.WorkerNext`, `(derived AND rebuilt)`, walked
in `RebuildDerived` after the member list — because eviction cannot pick a loser without one, and
`CitizenTable`'s own comment had been saying since slice 10 that *"a Building-to-workers reverse index
does not exist and belongs to the labour system"*. This is the labour system. Two invariants, ids
**38** and **39**. Eighteen tests. **The second `[[building]]` kind did not enter a shipped Ruleset**:
the mechanism is separable from the content, and shipping a `workplace` kind needs a second
`[[zone_rule]]` to raise one, which is task 4's.

**The finding is that the stride was a defect nothing could have reported, and the key is what made it
sayable.** `SyntheticCity` gave every Citizen a Workplace on a stride coprime with the Building count,
so that *"the commute matrix is not the identity"* — 1,000 workplaces, in `dwelling`s, under Rulesets
that grant no jobs at all. It was correct-looking for four slices because **employment was not a
quantity anything could count**: with no ceiling there was nothing for the assignment to violate, no
guard that could fire, and no test that could be written. The moment `jobs` existed the golden
session's reload dismissed all 1,000 at once, and the baseline moved at sample 16 — the mechanism
reporting the fixture correctly at the first instant it was able to.

***A quantity nothing can count cannot be contradicted.*** That is
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) seen from
underneath. The ADR's rule is about what a sitting may *conclude* from an absence; this is about what
an absence *hides*. The unbuilt ceiling was not a constraint anybody reasoned from — it was the reason
a live defect had no symptom, and the fix arrived as a hash move rather than as an argument. The
generalisation worth keeping: **a mechanism's first appearance is also the first audit of every
fixture that was writing its state by hand**, and the audit is free only if you read the hash move
instead of re-recording past it.

**The stride is deleted here rather than in task 4**, which is where the plan put it. The plan's
reasoning survives intact — two ways to acquire a workplace is `plans/0012` *Cause 1* with both copies
executing — and it is not what forced the deletion. Keeping it would have shipped a city that employs
everybody until Tick 1,024 and nobody afterwards, with the sacking triggered by a Ruleset reload;
deleting it re-records the three baselines **once** instead of twice. Task 4 now only adds.

**Two smaller ones.** **Demolition unlists a worker and does not clear the handle**, and the asymmetry
with `Dismiss` is the decision rather than an oversight: a bulldozed employer leaves a severed handle,
which is `Reference.Severable`'s whole purpose and reads as *the job stopped existing*; a **lowered
ceiling** leaves the employer standing, so a severed handle there would be a lie about a Building the
player can still see. Clearing on demolition would also make a demolition write a saved column for a
reason that has nothing to do with the demolition, which is a hash move with no cause. And **the
worker list is invisible to every hash-based test in the repository** — replay, the golden baseline and
save/reload all compare hashes, and a derived column is in none of them — so `RebuildDerived` agreeing
with the maintained list is asserted directly, across a recycled slot, exactly as task 1's index was.
Without that assertion an `Append` would pass every other test in the file.

### Task 3 — the `[trips]` Ruleset table, built

`TripRuleset(CrossingCost, CommuteBudget)` beside `LotRuleset`, `Ruleset.Trips`, the loader's
`ReadTrips` with four refusals, and `TravelTime.FromSeconds` / `FromMinutes` — the authored-unit
conversions, which live in `Borough.Core.Quantities` on `Speed.FromKilometresPerHour`'s precedent
because `02 §2` puts the exchange rate outside the simulation. All three shipped Rulesets state
`crossing_seconds = 30` and **none states a `commute_budget_minutes`**. The two live call sites that
had been passing `TravelTime.Zero` — `Simulation.ApplyTrip` and `--trips` — now read the table and
**refuse a Ruleset that has none**. Twenty-six tests, 1,192 green.

**`TripFate.ExceededCommuteBudget` acquired a producer here rather than in task 6**, because the
Budget's only consumer today is the one place a Trip is resolved. It is therefore **reachable and
unreached**: no shipped Ruleset states a Budget, so the golden baseline and every long run until
task 8 report zero of them, which is the intended state and is asserted in both directions.

**The finding is that the derivation was looked for and there is none to be had, and that a failed
search is a result which has to be written down.** `0002` §D2 said *look for the derivation before
reaching for a value* and gave its shape — *half a signal period, a property of the junction* — and
this corpus has three precedents where that search **succeeded** and the number turned out to be
already in the tree: tau is `TICKS_PER_DAY ÷ cadence`, the arming stagger is the Rule's own rate, and
`lots_per_segment` is `CONTEXT.md` → Address's own five. This one fails, twice and for the same
reason: **there are no signals, and a Segment has no width**, so both candidate derivations rest on a
mechanism nobody has built — which [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)
makes evidence of nothing. *A search for a derivation is cheap and repeating it is not, so the
negative result belongs in the row beside the number.* The 30 s is then chosen against the band §D2
already stated, and both ends of that band agree: a third of the 92 s it takes to walk a 128 m block,
and half a 60-second signal cycle.

**⚠ And `--trips` has already run half the ratifier, which is the part worth quoting.** With the term
at 30 s against the same run at zero, the **1-block band's p90 detour moves 140% → 153%** and **no
other band moves at all**. That is `adr/0074`'s *its blast radius is the across-the-street case and
nothing else* **measured rather than argued** — the one claim in that ADR a machine could settle, and
it took one flag. The behavioural half — whether the term changes which jobs people take — is task 8's.

**The second finding is that the two numbers in one table are unset in two different senses, and that
is what forced the first optional key inside a present table this loader has.** `[placement]`'s rule
is that an author who writes the table has said the pass runs, so every key inside it is required.
`[trips]` cannot take that rule: a crossing cost may be **chosen with a named ratifier**
(`adr/0052` permits exactly that), and a Commute Budget is a **percentile of a distribution that does
not exist until commutes do**, so a value would be the thing that ADR forbids. So the Budget key is
optional and **the omission is the statement**: a city with no ceiling, in which nothing is refused
for its length.

***A value that means "unset" has to be outside the range of legitimate answers.*** That is session
F's *a placeholder whose value sits inside the range of legitimate answers cannot announce itself*
turned from a warning into a schema rule, and it is used twice here. Every minute count is a
legitimate Budget, so **no minute count can mean unset** — hence `TravelTime.Impassable`, which
nothing can author. And every second is a legitimate crossing cost — **zero most of all**, since that
is `adr/0074`'s rung 1 and what this corpus had by omission — so `TripRuleset.None` is deliberately
**not `default`**: an absent `[trips]` carries an impassable crossing, and a consumer that skips the
refusal produces an impassable Leg rather than a silently free crossing. Every other optional table
in this loader has a key that cannot legitimately be zero, and that is the only reason those get to
use `default`.

**The third is a constraint on when the Budget can be measured at all: a threshold read off a
distribution its own presence shapes must be measured in the city that lacks it.** A Commute Budget
in force censors the Trip-cost distribution it is a percentile of, because the Trips that would have
been in the tail are the ones it refuses. So *no ceiling* is a **precondition of the named ratifier**
rather than a convenience of the schema, and task 8's run must be made against a Ruleset that states
no Budget or it will measure a number back out of a run that number had already shaped.

**⚠ Fourth, and it is a warning for task 8: the golden session contains no `trip` command at all.**
Only the two **Ruleset content hashes** moved here — `session-trace.txt` did not, because a Ruleset's
content hash rides in the Input Log and not in the State Hash, and nothing in that session creates a
Trip. So the crossing cost is live, hash-bearing, and **asserted by no committed baseline**; the whole
Trip model sits outside the golden trace. That is slice 10 task 11's finding on its third outing —
*a baseline records what a run did* — and the remedy is task 8's rather than this task's, because a
`trip` command in the golden session would fix the coverage and still not exercise a generator.

**No ADR, and the reason is that nothing here is a new decision.** The term is `adr/0074`'s, the
ratifier rule is `adr/0052`'s, the placeholder rule is session F's, and the treatment of the two
missing derivations is `adr/0070`'s. What task 3 added is their application to a schema, which is
what a slice plan's record is for.


### Task 4 — the assignment pass, built

`EmploymentEngine` in Tick phase 6, **behind placement and ahead of the Zone Rules**, plus the
`[jobs]` Ruleset table, a `JobCounter` Census family and the `[trips] commute_budget_minutes` the
pass cannot run without. A Citizen with no Workplace and a home to search from draws `candidates`
Buildings from a box around that home and takes **the first with a free job slot it can walk to
inside the Commute Budget** — `adr/0017`, satisficing, never nearest and never best. It writes
`CitizenTable.Workplace` through `World.Employ`, which is the door task 2 built. **Twenty-five tests,
1,217 green against task 3's 1,192, and all three golden baselines re-recorded.**

**The shipped Rulesets gained four numbers and a fifth was already there**: `[[building]] jobs = 8`
on `dwelling`, `[trips] commute_budget_minutes = 20`, and `[jobs]` with `interval = 32`,
`revisit_ticks = 1024`, `candidates = 3`. All five are hash-bearing, all five are in `0002` §D1 with
a named ratifier, and **two of them are derived rather than chosen** — see below.

**Ordering.** Behind placement because a commute is anchored at a dwelling, so somebody housed a line
above can look for work on the same Tick rather than waiting a whole interval for an address they
already have; ahead of the Zone Rules because a Building condemned this Tick should not acquire a
worker first, and taking a job in a doomed Building is a churn nothing reports.

**The search box is derived from the Commute Budget and there is no radius key, which is the
decision this task turns on.** A radius chosen freely would be a hash-bearing number with no
ratifier, and worse: an unbounded or over-wide draw is **S2 R4's uniform origin-destination
distribution**, which R4 measured to be *a different city* rather than a noisier one — a
District-granular route's detour goes from 18.52% to 128.82% between the two draws. So the box is
what a walk within the Budget can cover, and **the loader refuses a `[jobs]` table in a Ruleset with
no `commute_budget_minutes`**. That is `ReadLots(roads)`'s cross-table precedent in a stronger form:
there the second table supplies a ceiling, here it supplies the first table's entire geometry. One
number does one thing, and the alternative — an authored radius *and* an authored Budget — is two
numbers that can contradict each other **silently**: a radius smaller than the Budget makes the
Budget inert, and one larger wastes the search.

**Two stages, and merging them would delete the reading the milestone is for.** Task 1's own remark
already said it — *the box supplies candidates and the walk decides acceptability* — and this is the
consumer it was written for. A vacancy the Road Graph cannot deliver inside the Budget is counted
as **`beyond`** and left, which is the only counter in the Census that reports the shape of the
network rather than the state of the economy. A pass that pre-filtered candidates by reachability
would employ exactly the same people and report zero.

**Four flows rather than two, and the third and fourth are the ones worth having.** `PlacementCounter`
records why a single *placed* counter is not enough: a queue nobody is looking at and a queue with
nowhere to go read identically. This pass has a **third** way to do nothing, so *considered − seeking*
is the price of sampling the population instead of maintaining a list of the unemployed,
*seeking − employed* is the shortage, and *beyond* is the geography. That is slice 7 task 9's
`evaluations − due` lesson applied **before** the fact rather than after it.

#### The findings

**The load-bearing one is that the Cell-uniform draw is right on paper and wrong in this repository,
and only a measurement could say so.** The first implementation drew a **Cell** from the box and then
a Building inside it, which is `PlacementEngine`'s own Lots-not-Buildings argument transplanted to a
grid — *a look that lands on empty ground found nothing*, which is what looking for work in a city
with none near you is, and it keeps the geography in the answer rather than in a filter. It is
unusable here: the shipped Rulesets hold on the order of **1,200 Buildings across 16,384 Cells**, so
a Cell-uniform look finds an employer roughly **one occasion in four hundred** and the mechanism
would be unobservable in every world anybody runs. ***An argument about what a draw means is
independent of whether the draw ever hits anything***, and the second question is a property of the
fixture rather than of the design. The draw is uniform over the Buildings in the box, which also
makes job density attract workers — a claim rather than a default, and the honest one.

**A Budget chosen against the map is not thereby exercised by every world on it, and the golden
fixture is the world where it is inert.** Twenty minutes is read off `--trips` over the shipped
`[roads]`, which is a property of the *map*; the golden session is 1,000 Citizens, which the
populator houses in ~120 Buildings on one contiguous strip of blocks, and **every pair in it is
within twenty minutes' walk**. So `beyond` is **0** there and a steady **48 per census interval** at
10,000 Citizens on the same Ruleset. That is slice 10 task 11's finding for the **fourth** time —
*a baseline records what a run did* — arriving from a new direction: not a change that narrowed what
the run reaches, but a number whose binding case the run never contained. It is a test in both
directions rather than a paragraph: `A_vacancy_the_walk_cannot_reach_is_counted_and_not_taken` runs
at a two-minute Budget, and `The_shipped_budget_is_inert_on_a_world_this_small` asserts the zero, so
the day the fixture changes somebody has to read the note.

**`[[building]] jobs` landed on the `dwelling` kind rather than on a workplace kind, and `0002` §D2
predicted otherwise.** A second kind needs a second `[[zone_rule]]` to raise it, a second decline
Rule so that it **churns rather than accumulating until the city is all offices**, and a land-use
split — three decisions about *content*, in a file whose first line says it makes none. Living above
the shop is the smallest arrangement in which the pass has somewhere to send anybody, and it claims
nothing: the geography still binds, because the box is still a box and the Budget still refuses what
the Road Graph cannot deliver. The row is amended rather than quietly satisfied.

**Two of the five numbers are derived, which is the corpus's fourth and fifth successful search for a
derivation.** `jobs = 8` is the floor of `1000/360 × 3` — S4 task 2's Household ratio through
`occupants` — giving **0.96 jobs per resident**, so full employment is out of reach *by construction*
and the shortage flow is never trivially zero. `revisit_ticks` and `interval` are `[placement]`'s own,
and the copy is an argument: a person without a job and a family without a home look at the same rate
for the same reason, and nothing in this project yet distinguishes them. Only `candidates` is free —
**and it is ratifiable here for the first time**, because placement's copy scores every candidate
identically while this one filters on a real walk.

**The cost is measured and it is a burst, which is filed to [`0013`](0013-tick-budget.md) rather than
tuned here.** Wall-clock delta against the same Ruleset with `[jobs]` deleted, at 100,000 Citizens:
**7.0 ms per pass at steady state and 48 ms at cold start**, all of it landing in one Tick in
thirty-two. Linearly that is **~70 ms against a 15.6 ms budget in the pass Tick** at 1M, and **31×**
during the transient. Two things about it are worth carrying: the **box walk and the routing are the
same order** — 841 Cells counted four times against three ~5 µs searches — so the obvious thing to
optimise is not obviously the right one; and **the cost falls as the city settles**, because a look
that lands on somebody already employed costs one handle resolve, which makes the transient rather
than the trend the number to design against. Shortening `interval` against a fixed `revisit_ticks`
spreads the identical work and cuts the peak without touching the mean, which is the lever `0013`
names first.

**No ADR.** `adr/0081` is the decision and this is its construction; the box derivation is a refusal
to open a number rather than a new one, and `adr/0052`, `adr/0059`, `adr/0069` and `adr/0070` supply
the rest of the reasoning unchanged.

### Task 5 — the commute generator and the daily occasion, built

`CommuteEngine` in Tick phase 4, ahead of `TripEngine.Advance`, plus `CommuteRoster` — a
`(derived AND rebuilt)` partition of the population by departure phase — `TripPurpose.Commute`, and a
`[jobs] commute_peak_factor` the departure window is derived from. **A Citizen with a Workplace and a
home walks to work once a Day, and it is the first Trip in this project nobody asked for.**
`TripEngine` needed no change, which was `adr/0080`'s stated bet and is now a measurement rather than
a prediction: the cursor, the Fate, the release and the Census all ran a generated Trip unedited.

**One number, one enum value, one derived structure, one shared entry point.**
`Simulation.ApplyTrip`'s Trip-creation body moved to `TripEngine.Start` on the day the second caller
appeared, so a Trip has exactly one door — which is what `TripPurpose.Commanded`'s own rule
(*nothing downstream may branch on the purpose*) would otherwise have been quietly violated by, in
the one place a reader would not look for a branch.

#### The occasion: a phase, not a schedule

**This is the task's one design question and it answers itself once the two constants are put side by
side.** A commute recurs **every Day**; `EventWheel.Size` is **exactly a Day** (`Ticks.PerDay`, 8192).
So a Citizen armed on the Wheel re-arms at `+8192` for ever and **never leaves the bucket it started
in** — which makes the bucket a partition of the population by a *constant*, and ***a bucketing on a
constant is derivable rather than scheduled***. The Wheel route would have cost a saved column, a
per-Tick re-arm and a generalisation of the Wheel to a second table, and bought a structure that never
changes. `adr/0081` says generalising the Wheel is *not required*; this is why it would not have
helped either.

So `CommuteRoster` is `(derived AND rebuilt)`, 128 KB of head/tail arrays over the Day plus
`CitizenTable.CommuteNext` — **a column declared in slice 4 for a Wheel that never carried a Citizen
and read by nothing for five slices**, renamed from `WheelNext` here and load-bearing for the first
time.

**The phase is drawn from the Citizen's monotonic id, not from its slot, and the difference is a
spatial wave.** A slot-derived phase would need no index at all — bucket `b` is the arithmetic
progression `b, b + window, …` — and it would be wrong here, because `SyntheticCity` assigns Citizen
`i` to Household `i mod H` and Household `j` to Building `j mod B`. **Slot order is a fixed stride
through the Building table**, so a phase read off it sends whole streets to work together for a reason
with no cause in the city. That is `02 §8` rule 5, and it is the failure the Unplaced Pool's draw
already exists to avoid.

#### The finding: the peak and the window are one number seen from two sides

**`commute_peak_factor` was going to be a fifth free hash-bearing number and it turned out to be a
restatement of one somebody had already measured.** Under a uniform departure window of `W` Ticks the
instantaneous departure rate is `TICKS_PER_DAY ÷ W` times the daily average — so the **peaking
multiplier** and the **window** are the same quantity. S2 R7 measured the morning peak as an
independent **2–3×** multiplier on in-flight Travellers; it did not measure a window, and nothing in
the corpus ever has. So the Ruleset states the side with evidence and `JobRuleset.CommuteWindow`
derives the side the engine needs: `window = ceil(TICKS_PER_DAY ÷ commute_peak_factor)`. **That is
`adr/0059` a fourth time** — state what a designer has a reason for, derive what the loop consumes —
and it is the corpus's **sixth successful search for a derivation**, against task 3's failed one.

**3 implies an eight-hour departure window, which is wide for a rush hour and is exactly what 3×
means.** The temptation is to narrow it because *rush hour* is a shorter thing than that; narrowing it
without moving the factor is impossible, and moving the factor is asserting a peak height nobody has
measured. **A taller, narrower peak is a claim about the *shape* of the distribution, and the corpus
has measured its *height* and nothing else** — so a tapered profile would be a curve invented at the
write site, which is what task 4's Cell-uniform draw was nearly the mirror of. A factor of **1** is a
Day with no peak at all and is the control the demonstration can be run against.

**No `peak_offset`, and the absence is a decision.** Tick zero of the Day is the first departure Tick.
An offset would be a second hash-bearing number whose only consumer is a clock nothing else reads —
no Rule, no Layer, no Zone Rule asks what time of day it is — so *when* within the Day the peak falls
is **unobservable**, and choosing it would be `adr/0052`'s prohibition exactly: a number with no
ratifier and no consequence.

#### Two smaller ones

**Nobody can be in flight when their next departure comes round, and it is the *loader* that
guarantees it.** The generator has no overlap guard and needs none: a Trip that is not `InFlight` on
creation never gets a Traveller, one that is has passed the Commute Budget, and `RulesetLoader`
refuses `[jobs]` in a Ruleset with no `commute_budget_minutes` (task 4). A Budget is stated in minutes
and a Day is 24 in-world hours, so the overlap is **arithmetically unreachable rather than merely
unlikely** — and it is unreachable because of a refusal written for an entirely different reason,
which is worth knowing before somebody relaxes that refusal.

**The golden session now contains Trips, which closes the hole task 3 shipped and named.** That task
recorded that the committed baseline held **no `trip` command at all**, so the whole Trip model sat
outside it. A generator fixes that without a command.
`GoldenSessionCoverageTests.The_session_sends_people_to_work_without_a_trip_command` is the assertion,
and it carries the limit it cannot check: the session is **2,048 Ticks against a 2,731-Tick window**,
so the baseline reaches three quarters of the departure phases and **no Citizen in it departs twice**.
Covering the second departure means lengthening the session past a Day, which is a change to the
baseline rather than a line in a test.

**Thirteen tests, 1,230 green against task 4's 1,217, and the two session baselines re-recorded** — both
Ruleset content hashes moved because `[jobs]` gained a key, and `session-trace.txt` moved for a second
and better reason: the city now makes Trips. **`world-hash.txt` did not move**, and that is the
declaration working — a hand-built world states no `[jobs]`, and the roster is derived either way.

**No ADR.** `adr/0081` is the decision and this is its construction; the phase-not-schedule argument is
a refusal to open a mechanism rather than a new decision, and `adr/0059`, `adr/0064` and `adr/0080`
supply the rest unchanged.

### Task 6 — the Trip Census family and the cost distribution, built

`TripCostBucket`, a **seventh Census family** and the corpus's first histogram: seven bands of clock
minutes, filled at Trip **creation**, drained on reading like every other flow. Plus the four Trip
Fates reaching `--census` at last. **Thirty-two tests, 1,262 green against task 5's 1,230, and no
baseline moved** — the Census folds into nothing.

**The task's stated content was already half-built, and finding that out is the first thing worth
recording.** The plan said this task adds *the cost distribution* and makes
`TripFate.ExceededCommuteBudget` *reachable for the first time*. The second was done in task 4, when
the Commute Budget was set — and it is not merely reachable, it is **reached in an ordinary run**, by
a mechanism nothing predicted: **the placement pass moves a Household into a different dwelling after
the job at the other end was accepted**, so a commute that was inside the Budget when it was chosen is
outside it the next morning. That is the only route to the Fate this milestone has, and it is a real
one rather than a contrivance.

#### The finding: a family with no reader is a family nobody can see

**Milestone 5b built `TripCounter`, wired all four Fate flows through `Census.Observe`, tested them,
and never added them to `--census`.** For a whole milestone the only way to read a Trip Fate was to
write a test — *a consumer no operator has*. Nothing was wrong: the counters were correct, the
addressing was correct, the tests were green. What was missing was the last ten lines, in a file
neither the ADR nor the plan mentions.

That is `adr/0064`'s finding on a different axis. There, a guard existed and had no test, so the suite
you would read to find out what the loader refuses did not name it. Here a *mechanism* existed and had
no reader, so the report you would read to find out what the city does did not name it. **Both are the
same failure — a fact with no copy at all, rather than two copies that drifted** — and both are
invisible to every test that passes. `CensusReportTests` is now the guard, and it asserts a **family**
appears rather than a number, because no test of a counter can notice that the counter is unprinted.

#### The finding: this distribution cannot ratify the number it is about

**A commute exists only because the assignment pass already accepted the job at the other end of it,
inside the Commute Budget.** So the ceiling is **upstream**, and the distribution of realised commutes
is censored by the number it would be used to ratify. The intuitive experiment — *lower the Budget and
watch the Trips pile up above it* — does not work, and the way it fails is the sharp part: at a
one-minute Budget the golden fixture makes twenty commutes and **every one of them is under a minute**.
The distribution **collapses into its shortest band rather than growing a tail**, because what a lower
Budget removes is the *acceptances*, not the journeys.

***A distribution censored by a number is not evidence about that number.*** The uncensored one is
`--trips`' geometric census over every Building pair, which task 3 insisted be taken *before* any
Budget was set — and this is the proof that the insistence was right, because there is **now no way to
take that reading again from inside a run**. Task 3 recorded the reason as *the source of a percentile
must stay uncensored by the number read off it*; that was an argument then and it is a measurement now.

#### Two smaller ones

**The ladder is geometric in clock minutes and is deliberately *not* derived from the Commute
Budget**, which was the tempting choice and would have needed no free numbers at all — `adr/0059`'s
shape, six buckets stated as fractions of `commute_budget_minutes`. It would have been **useless for
the one job the family has**: the ratifier compares runs at *different* Budgets, and a distribution
measured in units of the number under test has the same shape at every value of it. ***A ruler must not
move with the thing it measures.***

**The bucket edges are free numbers and need no ratifier, and that is worth stating because every
other number in this milestone needed one.** `adr/0052` governs *hash-bearing and world-creation*
numbers; a Census bucket edge is neither, because the Census is read-only and folds into nothing.
Changing an edge changes what a report looks like and no city anywhere. **It is an instrument's
resolution, and `0002` §D would be the wrong home for it** — a §D that accreted instrument settings
would stop being readable as the list of numbers the city depends on.

**One thing seen and deliberately not changed.** `[[building]] jobs` sits on the `dwelling` kind (task
4), so a Citizen can be employed in the Building they live in, and the generator makes that a Trip of
essentially no length. It is not wrong — living above the shop is the arrangement task 4 chose on
purpose — but it puts non-journeys in the first band. **Refusing it would be a behaviour change made
inside an instrumentation task**, which is how an unrelated edit gets committed under a feature's name
(`adr/0073`'s ordering rule, read the other way round). Filed for task 7's picture, which is where a
commute of zero will be visible as a dot on top of its own home.

**No ADR.** `adr/0076` closes the Fate set and `adr/0081` owns the milestone; a histogram of an
existing quantity decides nothing.

### Task 7 — something to look at, built

`--commute`: **where people work against where they live, by block, before and after the jobs are
taken**, with the run's cost distribution underneath it. The **seventh runner mode** and the **second
that steps the world** — `--zones` is the first, and for the same reason: employment is a thing that
happens over time, so unlike `--roads` and `--trips` this picture has a real *before*. That before is
the control, and it is the strongest thing in the output: **a city at Tick 0 has nobody employed
anywhere**, so every block exports its whole population and none takes anybody in. **Eleven tests,
1,273 green against task 6's 1,262.**

**The quantity is a *balance*, not a count, and that is what makes it a new picture.** A grid of
worker counts is a grid of population, which `--zones` already shows. What is new is the **direction
people move in the morning**: a block reads as exporting workers, importing them, or within a quarter
of parity. A city where nobody moves and a city where everybody moves are then two different pictures
rather than two similar ones.

**It refuses a Ruleset with no `[jobs]` rather than printing a city of the unemployed**, which is
`--zones`' polarity for a sharper reason than usual: *a grid of universal export is what this dump
prints on purpose in its first frame*, so printing it in the second would make **a broken assignment
pass and a file that grants no work indistinguishable**. Employment is content twice over — `[jobs]`
states the cadence, `[[building]] jobs` states the posts — and the option-layer complaint names both,
because a reader who supplies one gets the other refusal and the two have to lead to the same place.

#### The finding: an instrument printed every duration short, and only a round number showed it

**`--trips`' minute formatter dropped the sub-Tick fraction before converting**, so every figure that
instrument has ever printed was short by up to one Tick — **10.546875 s of in-world time**. It
surfaced because this dump prints the Commute Budget in its header and the shipped `20` came out as
**19.9**. On a round number that is a rounding artefact nobody looks twice at; on `--trips`' own
2.5-minute band it is **7%**, and on the 1-block p90 that *task 3's crossing-cost ratifier is read
off* it is the same 7%.

***A defect that only shows on a value you happen to know is a defect that hides in every value you
do not.*** The corpus's own instance of this is `adr/0074`'s placeholder rule — *a value inside the
range of legitimate answers cannot announce itself* — and 19.9 minutes is squarely inside it. What
made it visible was **printing a number the file had also stated**, which is a property worth
designing for rather than getting lucky with. Fixed at the source per `adr/0073`: the multiplication
now happens before the shift, in `TripDump.Minutes`, which this dump calls rather than copies.

**And fixing it broke a test in the way that test's own doc comment warns about.**
`No_band_reports_a_walk_of_no_time_at_all` asserted the report does not contain the substring
`0.0 min` — and the corrected header prints `20.0 min`, which does. Three paragraphs below it in the
same file, `Unreachable`'s remark already says *parsed rather than substring-matched, because the
substring lies*, recorded when `0 pair(s) had no pedestrian route` turned out to be a substring of
`220 pair(s)`. ***A rule written down beside the code it governs is not thereby applied to the code
next to it***, which is `adr/0044`'s *citing an ADR is not applying it* at the scale of one file. It is
a digit-boundary match now.


#### What the picture says about the city we have

**Near-total parity: at 10,000 Citizens, 222 blocks of 228 come out within a quarter of parity, 4
export and 2 import.** That is not a defect and it is worth reading carefully — it is exactly what
`jobs` on the `dwelling` kind produces. Every Building has the same 8 posts and the same 3 occupants,
so **there is no land use in this city at all**, and the commute is a shuffle rather than a flow. Task
4 recorded the choice and its reason (a workplace kind needs a second `[[zone_rule]]` and a second
decline Rule, which is content); this is the first time the consequence has been **visible** rather
than argued. The picture will be worth looking at again the day a second kind exists, and it will look
completely different.

**29 of 6,107 employed Citizens work in the Building they live in**, which task 6 saw and deliberately
did not change. It is counted here because nothing else can count it: the Census counts the Trip, the
State Hash folds it, and **neither can say the journey went nowhere**.

#### The cost task 5 owed and task 7 paid

**`adr/0073` says route a cost to [`0013`](0013-tick-budget.md) on the day it is found, and task 5's
was two days late.** The generator starts one walk search per departing Citizen and nothing measured
it. Three Rulesets at 100,000 Citizens over 20,000 Ticks: **no `[jobs]` 8.24 s; `[jobs]` with
`[[building]] jobs = 0` 9.31 s; the shipped file 21.32 s.**

**The middle run is the isolation, and it exists because `jobs = 0` loads** — the pass samples, draws
candidates and never routes, because `World.HasJob` fails first. So the sampling machinery on its own
is **1.7 ms in the one Tick in thirty-two it runs in**, and everything else is routing.

**Routing is 12.01 s across 369,727 searches — ~32.5 µs each, the first walk-search figure this corpus
has taken from a real world** rather than from a microbenchmark over a warm graph. On a count
attribution, commute generation is 29% of it: **0.175 ms a Tick amortised, ~0.52 ms in a departure
Tick**, and departures land on one Tick in three. At 1M that is **~5.2 ms against a 15.6 ms budget in a
departure Tick, 33%** — and unlike the assignment pass it **does not fall away as the city settles**,
because everybody with a job commutes every Day for ever. Filed with its method and its coupling
stated.

**No ADR, and no new number that needs one.** The balance band is a quarter and the window is 128
characters; both are properties of a terminal rather than of a city, on the Census histogram's
standing — the dump folds into nothing, so changing either changes a report and no city anywhere.
