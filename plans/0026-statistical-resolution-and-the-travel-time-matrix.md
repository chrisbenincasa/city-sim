# 0026 — Statistical resolution and the travel-time matrix

`06` milestone **5c**. The brief, then the record.

---

## Status

⚠ **IN FLIGHT — scoped 2026-08-14; tasks 1, 2, 3 and 4 of 8 done 2026-08-14.** All three named gates are discharged and none of the closures
reached a gate board, which is why this milestone read as blocked for two days
([`0000`](0000-board.md) → *Blocked*, split per-milestone on 2026-08-13):

| Gate | State |
|---|---|
| Session **I** — `adr/0012`'s owed amendment | ✅ **In the file.** [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md) lines 47–63 carry R5 and R6: the induced error as an absolute, the coarse key's benefit unconfirmed and forbidden to cite, eviction at fixed capacity with four-way LRU on the high bits (conflict misses 20.0% → 3.8%), then session M's invalidation contract |
| **`03 §5`** — the traffic model | ✅ Session **D**, 2026-08-10, all five tasks → [`0017`](0017-session-d-the-traffic-model.md) |
| **S2** | ✅ R0–R8 complete. The only act left in that spike is deleting its harness, which gates nothing |
| *(a fourth `adr/0047` names)* — player-drawn versus derived Districts | ✅ Discharged **twice over**: `02 §2.1` settled it as **both**, and [`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md) severed routing's dependency on it regardless |

**The scope below was settled with the user in the room on 2026-08-14**, on one question: whether the
**vehicular Leg** belongs here or to milestone 6. It belongs here. Reasoning under *The named risk*.

---

## Why this milestone exists, in one paragraph

Everything that moves in this city walks, and every road it walks on is free. `WalkRouting` is a real
Dijkstra returning **a cost and no path**; a Leg holds a cost and two endpoints; the Segment's
`volume_forward` / `volume_backward` columns are saved, hashed and **written by nothing**; the
per-Segment `Epoch` has been maintained since 5a and **read by no consumer**; and
`RoadSegmentTable.CapacityPerDay` is derived and read by nobody at all. This milestone is where the
statistical half of [`adr/0005`](../docs/adr/0005-two-fidelity-tiers.md)'s ladder actually runs: a
**routing partition** and a **travel-time matrix** so a Household can estimate a journey without
searching for it, a **path source** so a Traveller occupies the Segments it drives, a **vehicular Leg**
so there is something whose occupancy counts, and the **volume-delay function** that turns those counts
back into travel time. That last arrow is `03 §3.4`'s self-correcting loop, and it is the reason the
whole fidelity scheme is allowed to work.

---

## The named risk

`06`'s stated risk is **that routing intent leaks into the world** — the GlassBox failure,
[`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md). A route belongs to the agent that
holds it; the world must not become a place where routes are stored, shared or centrally recomputed,
because then *why did this Household change jobs* is answered by *the pathfinder re-ran*.

> ### ⚠ The milestone also inherits a debt whose precondition was stated at half its size
>
> `06`'s 5b row says Segment volume *"waits on a path source — which is 5c, **not on vehicles**."*
> **The second half of that is wrong**, and it is wrong in the direction that has now cost this chain
> three milestones running.
>
> `03 §3.3` states it in its own sentence: *"A Segment's volume is incremented when a **vehicular**
> Traveller enters it and decremented when it leaves."* The shipped invariant agrees —
> `WorldInvariants.TrafficIsConserved` (`WorldInvariants.cs:103`) counts only Travellers whose current
> Leg mode is not `Foot`. And **nothing in the build creates a non-`Foot` Leg**: `TripEngine.Start`
> makes exactly one `Foot` Leg per Trip (`TripEngine.cs:181-184`) and `CommuteEngine` only walks.
>
> **So a path source is necessary and not sufficient.** A 5c that ships the partition, the matrix and a
> per-Segment next hop and no vehicular Leg leaves volume at zero, the conservation invariant vacuous,
> and **7a with nothing to threshold** — which is the exact outcome `06`'s own 5c row warns about, two
> sentences after the clause that would have caused it.
>
> **This is the third sighting of one shape in three milestones**: 5b task 4's generator had no
> destination set, 5b's volume had no path, and now the path has no vehicle. Each time the precondition
> was named accurately and enumerated short. ***A precondition stated in the singular is a precondition
> nobody has finished counting*** — and the check is cheap: name the mechanism, then name what *it*
> needs, until you reach something that exists.
>
> **Why the vehicular Leg is 5c's and not milestone 6's**, settled with the user: `03 §3.1` defines the
> tiers, and **Statistical** is *"time-advanced; travel time from `distance / speed`"* while
> **Microscopic** is *"real vehicles: 1-D lane queues, car-following, junction conflicts"*. This
> milestone is named *statistical resolution*; milestone 6 is *Lane-as-entity traffic*. A car Leg
> time-advanced across a Segment sequence, incrementing volume as it enters and decrementing as it
> leaves, **is the Statistical tier performing its own definition**. It involves no Lane and no queue.
> The sequence is then coherent: 5c produces volume, 7a thresholds it to promote a Segment, and 6 is
> what a promoted Segment is promoted *into*.

---

## Tasks

**1. The routing partition.** [`adr/0040`](../docs/adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md)'s
cluster, **sized in Cells rather than in Chunks**, `(derived AND rebuilt)`, and it does not exist in any
form — there is no District, partition or cluster type anywhere in `Borough.Core`. It is task 1 because
the matrix keys on it and because [`0002`](0002-open-questions.md) §D2 already says so.

> **Its size is the milestone's first hash-bearing number and the ledger already holds it, with its
> ratifier named.** §D2: *measurable and unset*, machine = **S2 R1's entry-error curve re-read at
> routing granularity with the route store out of the denominator**. ⚠ **Do not quote R1's 24.70%–3.80%
> sweep against it** — that was measured with the store in the denominator and on the **District** axis,
> which `adr/0047` deleted. `plans/0012` **Cause 5**, and the caveat is on the §D2 row because the
> digits travel and the clause does not. What makes the curve readable at all is that the thing which
> capped matrix granularity — a **4.06 GiB** route store at 4,096 zones against a 172.3 MiB world — is
> the thing `adr/0047` removes by serving routes from the cache, leaving the matrix holding only *times*
> (~4.2 MiB at 1,024², ~67 MiB at 4,096²).
>
> **Two constraints bracket it before any measurement runs.** It is a multiple of the **Cell**, which is
> frozen; and **Chunk size must divide it**. That second one is a correction
> [`adr/0040`](../docs/adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md) is
> **owed and has not been paid** — it sized the cluster in Chunks without running the hash test on the
> dependency that creates, and the Chunk is declared *tuning, hash-preserving*, so anything hash-bearing
> expressed in Chunks makes turning that knob change the city. **Pay it in this task**, not later.
>
> **`(derived AND rebuilt)` is settled and the test is `05 §3`'s, not convenience** — the partition is a
> function of the Cell grid and the Road Graph, both saved, so a rebuild reproduces it **exactly rather
> than plausibly**, provided the walk that rebuilds it visits in slot order. Get that wrong and two
> identical cities disagree, which is the failure [`0020`](0020-the-road-graph.md) found for a wholly
> derived table joining `World._tables`.
>
> ⚠ **`CONTEXT.md` has no entry for it.** *Routing partition* is `adr/0047`'s term and appears in no
> vocabulary file, while **District** has an entry that still claims to be *"the granularity of the
> travel-time matrix"* — the welding `adr/0047` cut. **Add the term and correct the District entry in
> this task**, because the next reader will otherwise take the two as synonyms, which is the whole error
> the ADR exists to prevent.

**2. The travel-time matrix, and it has a consumer already paying for its absence.** Partition ×
partition free-flow times, built by one-to-all searches from each partition's access node, `(derived AND
rebuilt)`, rebuilt on the Epoch.

> **The first consumer is in the tree and is measurable today.** `EmploymentEngine.TryEmploy` runs a
> full Dijkstra **per candidate** to decide whether a job is reachable — **~32.5 µs a walk search in a
> real world** (5b-bis task 7), up to `candidates` times per seeker per occasion. That is precisely the
> question a matrix answers: *roughly how long from here to there*, without a search. **So this task
> ships with a before-and-after that is not a microbenchmark**, and the acceptance number is the job
> pass's own cost.
>
> ⚠ **But the matrix must not silently become the acceptance filter.** 5b-bis task 4's whole design is
> two stages — a geometric candidate draw, then *the walk decides* — because *"an index that pre-filtered
> by reachability would answer the same question with the Severance invisible, which is exactly what
> `03 §3.7`'s mechanism has to be able to show."* A matrix estimate is an **estimate**; whether the
> Commute Budget refuses a job is a **fact about a route**. Replacing the second with the first deletes
> the Severance reading, which is the milestone-5a payoff `06` says to protect. **The honest use is a
> cheap reject followed by the real walk**, and the plan must say which readings that changes.
>
> **The asymmetry question lands here and nowhere else.** `RoadConnectivity`'s own remarks
> (`RoadConnectivity.cs:38-44`) say union-find gives **weak** connectivity while `CONTEXT.md`'s
> Settlement wants **mutual reachability**, and that the gap *"belongs to the slice that has a
> travel-time matrix to be asymmetric in, which is 5c and not this one."* It is this one.

**3. The path source, and it amends [`adr/0075`](../docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md).**
⚠ **It does amend it, and not on the sentence named below — see the task 3 record.** *A Leg stores a
cost and not a path* is untouched by producing a route on demand; the **Traveller** row is what needed
the edit.

`adr/0041` needs *a **next Segment** every Tick*; a Leg has *a cost and no path*; `WalkScratch` has no
predecessor array. Something must produce a Segment sequence and something must hold it.

> **`adr/0047` has already closed the shape and it closed the alternative too.** A route is
> `(Segment, offset) → (Segment, offset)` **served from the route cache**, and the maintained
> District-granular **next-hop table is out on four grounds, none of them cost** — 149.73% structural
> detour on a local draw, 87.25% of traffic on 1% of the road, it restores the physics `adr/0041`
> refused, and a tree holds **one** route to a place where a cache holds many. **R6 is the only exit**,
> which raises its stakes rather than lowering them.
>
> ⚠ ***A decision that removes a representation defers every decision that reads it***, and this is that
> rule being paid rather than discovered. `adr/0075` gave a Leg no path for good reasons and issued a
> write to `adr/0041` it did not know it was issuing. **Amend `0075` in place** — it is not superseded;
> a Leg is still a plan and a Traveller is still a cursor, and what changes is what the cursor is a
> cursor *into*.
>
> **Where the route lives is `adr/0012`'s question, not a storage detail.** *Routing intent lives in the
> agent*, and `CONTEXT.md` → Habit Route makes the stored path **per-Citizen rather than per-Traveller**.
> Session M's contract is the one to implement: **never wrong about a removal, boundedly wrong about an
> addition, the bound checked at use**, with the per-Segment Epoch as the witness — which is the first
> time in this project's life that Epoch has a reader.

**4. The route cache.** `adr/0012` lines 47–63, built rather than quoted: **eviction at fixed capacity,
four-way LRU on the high bits** (conflict misses 20.0% → 3.8%), the addition bound **checked at use**
with a proximity wake over it, and **no TTL rotation** — R5.5.4's rotation was the shed's answer and
`adr/0083` explicitly declines to carry the parameter across.

> ⚠ **Two errors in the paragraph above, both found before a line was written — see the task 4 record.**
> **The rotation attribution is backwards**: R5.5.4 rotated *this* store, resident population 412, and
> `adr/0012` says of that number *"0.40 forced refreshes per Tick is affordable there and **stays**"*.
> `adr/0083` is the **Parking Shed**'s and it declined to *take* the rotation, which is the opposite
> direction to carrying it away from here. `plans/0012` **Cause 5**. And **the addition bound and the
> proximity wake cannot be built in this milestone**: both hang on a per-Citizen **Habit**, and this
> document's own *What this milestone must not do* forbids one. What is buildable is the shared store,
> its exact removal test, and the rungs.

> ⚠ **The cache is the milestone's largest cost risk and the corpus has the number.** A diverting
> Traveller re-searching is **134.135 ms a Tick** at target scale, and the cache would need an **88.5%
> hit rate at 40,000 in flight and 95.9% at 111,000** to rescue it — on a key whose origin is *wherever
> congestion happened to be*, which R6.1b measured as its worst input. **`adr/0061` is what makes this
> survivable and it must be built as stated**: a diversion **rejoins by local descent and is never a
> search**. If this task finds itself adding a re-search path, that is the design being reopened, not an
> implementation detail — stop and file it.

**5. The vehicular Leg at Statistical resolution.** `TravelMode.Car` on a Leg, time-advanced,
`adr/0008`'s **three Legs minimum** for a car commute — walk, drive, walk.

> ⚠ **This task's precondition is parking, and parking is milestone 8.** A three-Leg car commute has to
> say where the car *is*, and [`adr/0009`](../docs/adr/0009-parking-is-modelled-supply-never-search.md)
> makes parking modelled supply whose scarcity *arrives as the walk Leg growing*. **Session F already
> found the trap and it is the sharpest thing in that sitting**: a placeholder here *pays the player for
> the shortage*, because a zero-length parking walk is what a garage genuinely produces, so a vehicle
> Access Point reachable as a fallback from an exhausted Parking Shed makes a **full car park cost less
> than an empty one** — and ***a placeholder whose value sits inside the range of legitimate answers
> cannot announce itself***.
>
> **So this milestone owes a decision before this task starts**, and it is the first thing to settle:
> what the drive Leg's endpoints are when no Parking Shed exists. The honest options are a **refused**
> car commute (no shed, no car — `adr/0070`'s *build X*, deferring to milestone 8) or an explicitly
> **named absence** in `adr/0079`'s sense, which reports rather than substitutes. **What it must not be
> is a zero-length walk.**

**6. The volume-delay function, which is what closes `03 §3.4`'s loop.** Volume attributed on enter and
released on leave per `adr/0041`; `CapacityPerDay` acquires its first reader; a Leg's cost stops being
pure free-flow.

> **This is the task the milestone is *for*, and the two before it are its preconditions.** Until a
> Segment's occupancy changes what a journey costs, volume is a number nobody reads until 7a and the
> self-correcting chain in `03 §3.4` does not close. §3.2's argument is that the VDF is used **only
> where it is strong** — unstressed Segments, where it sits near free-flow — and that *"we are not using
> the VDF to decide where the VDF can be trusted."*
>
> ⚠ **Its parameters are new hash-bearing numbers with no ratifier named anywhere**, which makes them
> `adr/0052`'s business on the day they are written. Do not take a textbook BPR α and β as *derived*;
> they are chosen, and the row goes into `0002` §D with the thing that would refute it.
>
> ⚠ **And there is an open question this task must not answer by accident**: *when* a Leg's cost is
> computed. `adr/0075` makes a Leg a plan with a cost, so a cost computed at plan time is **stale by the
> time it is driven** — which is `adr/0012`'s invalidation problem arriving on the cost rather than on
> the route. Name the choice and file it; do not let it be decided by whichever line is easier to write.
>
> **The conservation invariant stops being vacuous here**, and that is the acceptance test:
> `Invariant.SegmentVolumeIsConserved` (37) has been *"correct and temporarily trivial"* since 5b, by
> the deliberate precedent 5b set. It becomes load-bearing with **no edit**, which is what that
> precedent was for.

**7. Something to look at.** An eighth runner mode. The milestone's job is congestion, so the picture is
**where the traffic is** — per-Segment volume against capacity, before and after, on a world that steps.

> `--commute` and `--zones` are the precedent: a mode that steps the world because the quantity develops
> over time. ⚠ **The lesson from `--commute` applies directly**: printing a value beside one the reader
> already knows is what exposed a formatter that had been lossy in every duration it ever printed.
> **Print the free-flow time beside the loaded one**, so a VDF that does nothing and a VDF that is wired
> backwards are distinguishable at a glance.

**8. The 100,000-Tick run.** `adr/0006`'s collection half and `adr/0003`'s magnitude half, over the first
mechanism in this project that has a **per-Tick write to a saved column** on every moving entity.

> ⚠ **Volume is a level that must return to zero, and it is the first quantity here that can leak
> permanently.** An increment on enter with a missed decrement on leave is an `adr/0006`-class capacity
> loss that no per-Tick check would see — the same shape `adr/0084` names for parking occupancy, and the
> reason that milestone has **two** checks rather than one. Expect to need both: an `O(1)` release check
> at the write site and the end-of-run conservation sum that already exists.

---

## What this milestone must not do

- **No Lane kernel, no car-following, no junction conflicts, no queues.** That is milestone 6 and
  [`adr/0016`](../docs/adr/0016-the-lane-is-the-entity-not-the-car.md). A Statistical car is
  time-advanced.
- **No Stress thresholds, no Fidelity promotion, no hysteresis.** Milestone 7a. This milestone produces
  the quantity 7a reads; `RoadSegmentTable.Fidelity` stays the named hole it is.
- **No Habit, Sight or Temperament.** [`adr/0046`](../docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md),
  `adr/0060` and `adr/0061` are the *driver* model, and every one of them is downstream of congestion
  existing. A Traveller here drives the route it was given.
- **No District Pool.** `Scope.Pool` throws (`RuleEngine.cs:805`) and stays throwing — `adr/0047`
  decoupled routing from the District precisely so that this milestone need not wait on it.
- **No parking model.** See task 5: name the absence, do not fill it.
- **No re-tuning of the Commute Budget rungs.** [`adr/0095`](../docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md)'s
  **fourth revisit trigger** says to reopen them *on the distribution* when 5c lands, because today they
  are percentiles of a **free-flow, foot-only** walk. ⚠ **That trigger fires at the end of this
  milestone, not during it** — and note that it fires on task 6, not on task 3: congestion reaches a
  commute when the VDF exists, not when a path does.

---

## Definition of done

`CLAUDE.md`'s list, plus:

- The partition and the matrix are `(derived AND rebuilt)` and a rebuild reproduces them **exactly**,
  checked across a recycled slot.
- `Invariant.SegmentVolumeIsConserved` is **non-vacuously** satisfied — a run in which both sides are
  provably non-zero, asserted as such, because a vacuous pass is what it has done since 5b.
- The job pass's search cost is reported before and after the matrix, in a real world.
- Every hash-bearing number introduced has a row in [`0002`](0002-open-questions.md) §D with a **named**
  ratifier on the day it is written (`adr/0052`; a category is not a name).
- Three golden baselines re-recorded. **Every task here moves the State Hash**, so expect to re-record
  more than once and say so per task rather than at the end — 5b-bis's precedent.

---

## Open decisions this milestone owes, before the task that needs them

| # | Decision | Needed before | Why it is not settled here |
|---|---|---|---|
| ~~1~~ | ~~**What a drive Leg's endpoints are with no Parking Shed**~~ ✅ **STRUCK 2026-08-14, and the ground was never there.** Session F's trap is a **fallback from an exhausted Shed**, which cannot occur because no Shed exists — and `World.VehicleAccessPoint` had already forbidden it in its own doc-comment. The two Access Points are the **same Address** by construction (`World.cs:1044`, *built behaviour rather than an interim simplification*), so a drive is door to door and nothing is stood in for. ⚠ **What actually gated task 5 was not on this list at all**: nobody decides who drives, and mode choice is **undesigned** under `adr/0070` — see the record | task 5 | *(struck)* |
| 2 | **When a Leg's cost is computed**, and what makes it stale | task 6 | `adr/0075` makes a Leg a plan; a plan's cost ages. This is `adr/0012`'s problem on a second object and neither ADR addresses it |
| 3 | **Whether the matrix may reject a job candidate**, or only order them | task 2 | Deleting the Severance reading is the failure mode, and 5b-bis task 4 chose the two-stage shape deliberately |

**None of these is a number**, so `adr/0052` does not apply to them and `adr/0043` does — type each one
before settling it, and route anything measurable to a machine rather than to a sitting.

---

## The record

*(Filled per task as it lands.)*

### Task 1 — the routing partition. ✅ **DONE 2026-08-14.**

`Borough.Core.Space.RoutingPartition`, owned by `RoadGraph` and rebuilt beside the components.
`(derived AND rebuilt)`, not a `[Table]`, and **it moved no State Hash and re-recorded no baseline**,
because nothing reads it until task 2.

**What it is.** A square tiling of the map in Cells. Partitions holding no Node are not numbered;
occupied ones are numbered **row-major over the grid**, and each carries an **access node** — the live
node nearest its centre by Chebyshev distance, ties to the lower slot. The edge is
`RoutingPartition.DesignEdge`, **4 Cells — 128 Tiles, 512 m**, provisional and UNRATIFIED with the
ratifier named in [`0002`](0002-open-questions.md) §D2.

| Citizens | nodes | partitions | of the map | spread |
|---|---|---|---|---|
| 4,000 *(golden fixture)* | 121 | **9** | 0.05% | 3×3 |
| 10,000 | 256 | 16 | 0.10% | 4×4 |
| 40,000 | 961 | 64 | 0.39% | 8×8 |
| 160,000 | 3,721 | 256 | 1.56% | 16×16 |

**The size is provisional on purpose, and the reason is the storage class rather than impatience.**
`adr/0052` requires a *named ratifier* and never a ratification first; the partition is
`(derived AND rebuilt)`, so moving it costs a recomputation and never a save migration — which is the
asymmetry `adr/0040` was written on, used for the first time. §D2 named R1's entry-error curve as the
machine; **the ratifier moves to task 2's in-engine measurement**, which beats the spike on four axes
at once (real world, real draw, current clock, correct mode).

**Owed things paid.** `adr/0040`'s Cells-not-Chunks correction is **paid here** and the constructor is
where — a non-power-of-two edge, an edge below one Chunk and an edge past the map are all refused, and
a test asserts the divisibility that ADR's own fourth consequence says *"must be enforced ... it would
do so silently"*. `CONTEXT.md` gained a **Routing partition** entry.

#### Findings

**1. `plans/0002` §D2's disqualifier on R1's entry-error curve is factually wrong, and it is a
Cause 5 error sitting on a Cause 5 entry.** The row says the 24.70%–3.80% sweep *"was measured with
the store in the denominator and on the District axis, which is the axis `adr/0047` deleted"*. Both
clauses fail on inspection. `MatrixReport.MeasureError` (`spikes/S2.Routing/Harness/MatrixReport.cs:540-604`)
compares a matrix entry against a real A\* search's cost and divides by that same per-query cost; the
route store is a **separate size table** (`RouteStoreBytes`, `:214-248`) that never enters it — two
tables, one disqualifier, belonging to neither. And the harness's partition is a Cell-aligned square
grid over nodes (`Districts.cs:215-220`), which is geometrically *this* object; what `adr/0047` deleted
is who **owns** the number, not the geometry.

> **The three disqualifiers that are real were never written down, and each moves the number.** The
> relative figures are on a **uniform** origin-destination draw (`:551-558`), which S2 R4 measured is
> *a different city* — 18.52% → 128.82%. The absolute Ticks are **pre-`adr/0094`**
> (`spikes/S2.Routing/Graph/Units.cs:69` is `TicksPerDay = 8192`), so 6.73 Ticks at the 121 rung is
> **71 s** rather than the 4.7 minutes today's clock reads it as. And the costs are **car** times
> (`Modes.Car`), while every Commute Budget rung in the build is a **foot** percentile.
>
> ***A caveat that is wrong is worse than no caveat, because it is the reason nobody writes the right
> one.*** `adr/0093`'s *a false description of a guard* on a second object — there, a guard nobody
> built because a document said it existed; here, three disqualifiers nobody wrote because one was
> already sitting in the cell. **The writing half is the same fix: name what the number measures, not
> what it is disqualified by.** *"Uniform-draw, car-time, 8192-Tick"* is checkable against the harness
> in three greps; *"the store was in the denominator"* required reading 600 lines to refute.

**2. A matrix entry's error is a fixed distance and therefore a mode-dependent time, and no S2 reading
can settle the size because S2 was car-only.** Half a partition diagonal at 4 Cells is ~362 m: about
**0.4 minutes** by car at 50 km/h and about **4.3 minutes** on foot at 5 km/h, which is **21% of
`adr/0095`'s fast rung**. So a partition sized to serve a car matrix is an order of magnitude too
coarse for a foot one — and this project builds only foot Trips today. Halving the edge quadruples the
matrix, so the two pressures are genuinely opposed, and the honest expectation recorded in §D2 is that
**this number wants to go down**. ***This is the third mode confusion in three days*** — `adr/0089`'s
map ratio (vehicle) against `adr/0095`'s rungs (foot), then that finding's own write-up, and now a
spike curve. **The pattern is that a duration is quoted without the mode that performed it**, which is
`plans/0012` Cause 5's *name a duration after the mode that performs it*, coined two days ago and
earning its third sighting.

**3. The task's own brief was wrong about a document, in the shape `adr/0093` governs.** It said
`CONTEXT.md`'s District entry *"still claims to be the granularity of the travel-time matrix"* and
told this task to correct it. **It had already been corrected — twice, in two separate paragraphs of
that entry** — and it already used the term *routing partition* without defining it. The half that was
real is that there was **no entry** for the term. The brief was written by reading `adr/0047` and
inferring what `CONTEXT.md` must therefore say, which is exactly *a description of the build is where
to look, and never what you found* applied to prose instead of code. **Worth holding because the brief
was written yesterday, by the author who then executed it, in a session whose headline finding was the
same substitution.**

**4. `CONTEXT.md`'s Tick entry was still on the old clock**, found in passing and fixed here rather
than filed: it read **10.546875 s** a Tick and *"one clock minute is 5.6889 Ticks, so a 30-minute
budget is 171 Ticks"*, all of which are `Ticks.PerDay = 8192` arithmetic. `adr/0094` moved that
constant on 2026-08-13 and the closure reached the ADRs, `CLAUDE.md`, `plans/0013` and the code — and
not the vocabulary file every cold start reads. `plans/0012` **Cause 2**. **The reason it could rot is
that every one of those figures is a pure restatement of a constant**, so nothing computed from them
and nothing failed; the sub-step band (**~45× → ~180×**) went the same way. `adr/0082`'s own 108×
figure is left alone because that ADR's amendment did not touch it.

### Task 2 — the travel-time matrix. ✅ **DONE 2026-08-14.**

`Borough.Core.Movement.TravelTimeMatrix`: partition × partition free-flow times, built by one
one-to-all search per partition, `(derived AND rebuilt)`, refreshed against a new graph-wide
`RoadGraph.Version`. `WalkScratch` gained `SettleAll` and `CostTo` — the same Dijkstra with the
stopping rule removed, shared rather than forked so two tie-breaks cannot drift apart — and is now
mode-parameterised. **1,319 tests green and all three golden baselines reproduced unchanged.**

#### The ratifier, discharged

[`0002`](0002-open-questions.md) §D2 named the machine and it has run. Entry error against a **real
walk**, in clock minutes:

| Citizens | order | mean | p50 | p90 | max over | max under |
|---|---|---|---|---|---|---|
| 4,000 *(golden fixture)* | 9 | −0.85 | −1.15 | +3.74 | **+9.22** | −8.83 |
| 40,000 | 64 | −0.23 | −0.05 | +3.98 | **+8.30** | −9.17 |

**The error does not grow with the city.** It is a partition-*local* quantity, so 4 Cells buys ±9
minutes on foot at every scale — which is the property that makes the size ratifiable at all from a
fixture. The size **stands at 4 Cells**, still formally unratified against a *product* judgement
(is ±9 minutes acceptable?) but no longer unmeasured.

#### Findings

**1. The geometric margin is not a bound, and that was measured rather than argued.**
`TravelTimeMatrix.EntryError` computes one partition crossed at the mode's speed — **6.1 minutes** on
foot at the shipped size — and the measured worst overstatement is **9.2**. A reject built on the
geometric figure would have discarded reachable work. The reason is structural: an entry runs access
node to access node and a journey runs Address to Address, so the difference is two *within-partition
walks*, and a walk is **road** distance, which nothing bounds by the partition's size. ***A structure
laid over a graph cannot bound a quantity measured on the graph*** — `BuildingResidency`'s *a
catchment is a time rather than a distance* one level up. The method survives, redocumented as a
scale and explicitly not a bound.

**2. ⚠ The distance-reject is inert, and it is the job-search box a third time.** With a safe margin
it fires **0 times in 400** at both 4,000 and 40,000 Citizens; with a **zero** margin it fires 3
times at 40,000. The binding term is not the margin — a 40,000-Citizen city is 3.84 km across, about
46 minutes on foot, against a **50-minute** ceiling. **The city is smaller than the Commute Budget**,
which is `b852d4d`'s finding arriving on a third mechanism in one day, after the job-search box and
`adr/0089`'s map ratio. **The yield was measured before anything was wired**, which is the whole
lesson of that commit turned into a habit.

**3. ⚠ The sound reachability reject needs no matrix, and 5a built it.** A matrix's Impassable is
*not* a certainty: an entry is anchored on an **access node**, so a partition holding two
disconnected pieces can report *severed* for a journey that succeeds. `RoadNodeTable.FootComponent`
has no such hole — union-find unions both endpoints of every Segment admitting the mode — and it
agreed with a real search **399 times out of 399** on `rulesets/severance.toml`. **It has been in the
tree since milestone 5a and nothing has ever read it.** The reject is now in `WalkRouting.Cost`
rather than in `EmploymentEngine`, so every walk consumer gets it and there is one place to be right.
*(The matrix did not actually misfire on that fixture — 0 in both directions — which makes its
unsoundness an **untriggered hazard** rather than a refuted one, and the test says so instead of
asserting a 0 that is a property of the fixture's geometry.)*

**4. ⚠ The reject is worth ~4%, and the recommendation that won it was wrong.** It was proposed on
the ground that a failing search settles the origin's whole component and is therefore the most
expensive kind. Measured over 399 walks × 200 on `severance.toml`, with **321 of 399 rejected**:
**1.03 → 0.99 µs a walk.** ***A search that fails because of Severance is cheap precisely because
Severance is what made it fail*** — the barrier that removes the route also shrinks the component the
search would have explored, so severance and expensive-failure are anti-correlated. **The argument
that survives is insurance rather than performance**: that anti-correlation is a property of how
`severance.toml` severs — it *shatters* — and a city **bisected** by a river with its bridges gone
has two large components, where a failing search settles half the city. The corpus has no fixture for
that case. Kept on that basis with the 4% written down, so nobody later quotes it as a win.

> ⚠ **Do not read this task's ~1 µs a walk against `plans/0013`'s ~32.5 µs.** Different city,
> different origin-destination distribution, and these walks are mostly fast failures inside
> shattered components. `plans/0012` **Cause 5**, declined rather than committed.

**5. The first timing instrument was invalid on exactly the path it was measuring.**
`WalkScratch.Relaxed` is reset by `Begin`, which the reject never reaches, so a rejected walk leaves
the *previous* walk's count standing and a sum over it double-counts. The totals came back
**bit-identical** with and without the reject and read as *the reject never fires* — when it fires
321 times in 399, which counting the firings directly established in one line. ***An instrument that
is only valid on the path it is measuring away is not an instrument.*** The settle counts are gone
from that test and the reason is written where they were.

**6. Task 1's mode-agnostic access node did not survive contact, as task 1 predicted it might not.**
The node nearest a partition's centre may be an Arterial junction, whose Arcs carry `Car` and not
`Foot` — so a foot row anchored on it settles nothing and reports the partition **severed from the
entire city**, which is the one reading this structure exists to keep honest. `RoutingPartition` now
carries one access node per mode, filtered to nodes some Arc *leaves* in that mode. ⚠ **That gave the
partition an ordering constraint its own comment denied**: task 1 put the rebuild last saying it *"reads
only the nodes ... so it has no ordering constraint"*, and it now reads the Arcs. Corrected in place
rather than overwritten, because **a stated absence of a constraint is what a later reader reorders
against**.

**7. The reject is an optimisation by `05 §4`'s own test**, and the golden baselines are what say so:
it returns Impassable in exactly the cases the search would have, so every counter, Fate and rung
downstream reads the same and no State Hash moved. **The matrix moved no hash either, because nothing
reads it** — task 1's *prospectively hash-bearing* warning is still standing and still unspent.

---

### Task 3 — the path source. ✅ **DONE 2026-08-14.**

**Scoped with the user in the room to the route-finder alone** — produce a route, prove it is right,
store nothing. The route cache is task 4 and the vehicle is task 5, and neither is anticipated here.

`WalkScratch.Begin(nodeCount, recordPath: true)` arms two predecessor arrays; `Relax` writes the Arc
a node was reached by and the node it came from; `PathTo(arcs, node, span)` walks them back and emits
the **Segment** slots origin-first. `Search` gains a `TravelMode` and an `Arrived` property. No new
saved data, no Traveller column, no table, and **all three golden baselines are unchanged**.

`RoutePathTests`, six tests. `adr/0075` amended in two places.

#### Findings

**1. The brief was wrong about which sentence of `adr/0075` it amends, and the right one is a
different field.** It named *a Leg stores a cost and not a path* — which producing routes on demand
does not touch, because that ADR already homes a drive path in
[`adr/0060`](../docs/adr/0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md)'s
route cache. ***Where a route is stored is a different question from whether one can be produced***,
and `0075` answered the first while **nothing in the corpus had answered the second**. What does need
amending is the **Traveller** row: *which Leg it is on* and *the arrival Tick* is a complete cursor for
a **Statistical** journey — that tier is time-advanced, so a Traveller between endpoints is nowhere in
particular — and an incomplete one for a **Microscopic** journey, which `adr/0041`'s amendment requires
to name *a next Segment every Tick*. Amended there, and the field discharges `03 §4` invariant 3's owed
enumeration at the same time, since position along a route is exactly what demotion discards. **Written
now rather than at the write site in milestone 6**, where an unenumerated field is the bug that
invariant exists to catch.

**2. `Search` had been foot-hardcoded since 5b and the fix was a parameter that could have been
inert.** It tested the foot bit and read `FootTime` directly, where task 2's `SettleAll` already took
a mode. `RoadArcs.TimeFor(i, Foot)` **is** `FootTime[i]`, so the walk path is bit-identical and no
baseline moved. ⚠ **A mode threaded through a signature and not through the loop compiles, passes
every existing test, and prices every car journey at walking pace** — a defect with no symptom until
task 5, and by then the parameter would read as covered. The test is therefore *a car is quicker than
a walk over the same Streets* rather than *the parameter exists*: the shipped Ruleset gives both modes
the same lattice, so topology is held constant and only the speed can move.

**3. `Search` did not say where it ended, and a walk is why nobody noticed.** It seeds two origin
endpoints, reads two destination endpoints and returns the cheapest of four combinations — and the
cost does not carry which one won. A walk never asks; a vehicle must, because the endpoint decides the
last Segment of the route and therefore which side of the street it arrives at. ***A field nothing
reads is indistinguishable from a field nothing needs***, which is 5b-bis task 6's *a Census family
with no reader* on the producing side rather than the reporting side. `Arrived` is one property.

**4. A route is recovered, never accumulated, and that is what makes recording nearly free.** Dijkstra
already holds the entire tree of cheapest paths when it stops; the only thing missing was which Arc
reached each node, which is two array stores per **improvement**. A per-node path list would be
`O(nodes × path length)` of copying for an answer exactly one node needs. Recording is opt-in so every
existing walk pays nothing, and `adr/0075` is the reason it must be — a walk Leg discards its route
**by decision rather than by omission**, so the default has to be the decision.

**5. Validation is reconstruction, because there is nothing to compare against.** This is the first
route this project has produced, so no second implementation and no baseline holds one. What survives
without one is stronger than a spot check: take the Segment list *back to the Road Graph*, follow it
hop by hop from the origin using only the node's own Arcs, and require that it **connects**, **ends at
the destination**, and **sums to the settled cost exactly**. A route that is plausible but wrong fails
all three, and the middle claim is the load-bearing one — it is what separates a real route from a set
of Segments lying near it.

**6. Two smaller ones, both about answers that look like other answers.** A route of length **zero** is
correct for a journey beginning where it ends, and `NoPath` means there is no route at all; collapsing
them would make a severed destination read as *you are already there*, and both produce a traveller
that does not move. And **a truncated route is a different route rather than a partial one** — it ends
at a node the traveller was passing through — so a buffer too small is left untouched and the required
length returned, which is the only contract under which ignoring the answer fails loudly.

---

### Task 4 — the route cache. ✅ **DONE 2026-08-14**, and it did not choose its own rung.

**Scoped with the user in the room on one question — measure the staleness rungs or pick one — and
the answer was measure.** `adr/0043`: the choice is settleable by a number, S2 settled it on a
synthetic lattice under a uniform origin-destination draw, and every figure in this corpus that moved
from a fixture to a real world moved. So `RouteStaleness` is a **switch with three rungs** and the
numbers below are taken here.

`RouteCache`: fixed capacity, **four-way set-associative, LRU within a set, indexed on the high bits**
of a splitmix64 finaliser over a **node-id pair**. An entry stores each Segment's **handle and Epoch**,
so a lookup rejects it exactly when a Segment it names was removed or edited — **the first reader the
per-Segment Epoch has ever had**, five milestones after 5a built it. `(derived AND rebuilt)`, outside
`World._tables`, and **nothing in the Tick calls it**.

`RouteCacheTests`, nine tests, of which three are measurements that assert only what would make them
meaningless.

#### What it costs, on real home-to-work pairs

The draw is `EmploymentEngine`'s own, taken at Tick 1024 on the golden fixture: **1,254 employed
Citizens over 1,031 distinct node pairs, 222 Segments live**. Store 4,096 entries, stride 64.

| | Exact | Keep | KeepAndRotate |
|---|---:|---:|---:|
| Extra searches from one 4-Segment gesture | **472** | 43 | 43 |
| Routes left stale after the addition | 0 | **14** of 1,254 | 14 → **0** over 1,024 Ticks |
| Mean detour over the draw | 0% | **0.44%** | 0.44% → 0% |
| Worst single detour | 0% | **200%** | 200% → 0% |

**Served against searched: 0.525 µs against 12.349 µs — 23.5×**, at a store that covers the working
set. ⚠ *That figure is a property of the coverage and not of the cache; see below.*

**And what the store has to cover grows with the city.** Every column here is measured; ⚠ **none of
them is extrapolated, and finding 3 is what an earlier draft did when it tried.**

| Citizens | Employed | Distinct pairs | Median route | p90 | Max | Shared | Hit @1024 | Hit @4096 |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1,000 | 308 | 201 | — | — | 8 | 34.7% | 100.00% | 100.00% |
| 2,000 | 625 | 445 | — | — | 10 | 28.8% | 94.08% | 100.00% |
| 4,000 | 1,254 | 1,031 | **5** | 9 | 13 | **17.78%** | 53.19% | 98.88% |
| 8,000 | 2,584 | 2,248 | — | — | 19 | 13.0% | 12.93% | 86.84% |
| 16,000 | 5,199 | 4,808 | **8** | 12 | 26 | **7.52%** | 2.83% | 36.68% |

⚠ **These are *foot* routes and the Commute Budget caps them absolutely** — 50 minutes at 5 km/h is
4.17 km, about **32 blocks** — so the length column has a ceiling and is not a curve to project. By car
the same ceiling reaches 41.7 km against a 19.2 km city at 1M, so it does not bind at all and route
length becomes a property of the map. ***The two distributions are different questions and only the
foot one exists.*** The employment column is `rulesets/minimal.toml`'s and **not a design target**:
`[[building]] jobs = 8` on the dwelling kind gives 0.96 jobs per resident, and the file's own header
says it models no city.

#### Findings

**1. ⚠ The largest one is that a key on *pairs* buys almost nothing, and it is the number
[`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md) said could not be taken.** That
ADR: *"the price of the key is settled exactly and the benefit cannot be settled at all until Trip
generation exists (`06` 5b)."* It exists. The share of commutes that **share a node pair with another
commute** — the whole of what a shared store buys over a per-traveller one — is **17.78% at 4,000
Citizens and 7.52% at 16,000**, and it **falls as the city grows**, because the paved extent grows with
the population. R6.1b measured the same thing from the coarsening side (*collapse reads 1.00× on every
row*) and could not see it. ***A key that merges almost nothing is a key chosen for a property the
traffic does not have.*** [`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md) and
`adr/0012` are both amended; where routes live at 1M is [`0002`](0002-open-questions.md) §C and is
**not 5c's**.

**2. ⚠ The access pattern a commute produces is the one LRU is provably worst on, and `adr/0012`
specifies LRU.** A commute is a **once-per-Day cyclic scan** — every employed Citizen departs once a
Day, in an order `CommuteRoster` fixes — so over a working set larger than the store, LRU evicts
exactly the entry needed next. At 16,000 Citizens, 4,808 distinct pairs, a 1,024-entry store, against a
ceiling of **21.30%**:

| store | ceiling | LRU | MRU | Random | refuse-to-displace |
|---:|---:|---:|---:|---:|---:|
| 256 | 5.32% | 0.79% | 5.08% | 0.85% | **5.64%** |
| 1,024 | 21.30% | **2.83%** | 19.54% | 3.79% | **22.41%** |
| 4,096 | 85.19% | **36.68%** | 72.42% | 58.30% | **74.51%** |

**R6's four-way result is untouched and was answering a different question** — conflict misses, a
property of the hash, across 1/2/4/8 ways. Which entry a *full set* gives up had never been measured
against this draw. ⚠ **Random fails here and succeeds in the textbook because the textbook cache is
fully associative**: inside a four-way set a random victim still churns the set out over one cycle.
`RouteEviction` defaults to **MRU** — within three points of the best and, unlike refuse-to-displace,
still able to admit a pair whose Citizen changed job. ***A policy that wins on a frozen input has not
been shown to win***, and the measurement re-ran one commute set four times.

**3. ⚠ This task's first close-out carried a memory figure and every input to it was misused.** It said
route storage at 1M was *~4 GB*. Withdrawn entirely — the full account is `plans/0012` **Cause 5**,
seventh sighting. In short: a `√population` fit through five points, a route-length **maximum** where
memory scales on the **median** (26 against 8), an employment ratio taken from a Ruleset whose header
says it models no city, and a **cache working set** used as a count of routes that must exist at once.
***An extrapolation is a claim about a mechanism, not about a curve*** — the Commute Budget caps a foot
route absolutely at ~32 blocks and the fit ran straight through it. **No memory figure for route
storage exists or may be quoted**; the distribution that would produce one is a **car** route
distribution and task 5 is where it is taken.

**4. The brief was wrong twice and both were caught by reading the source rather than the summary.**
It said *no TTL rotation — R5.5.4's rotation was the shed's answer and `adr/0083` explicitly declines
to carry the parameter across*. R5.5.4 rotated **this** store, resident population 412, and `adr/0012`
says of it *"0.40 forced refreshes per Tick is affordable there and **stays**"*; `adr/0083` is the
**Parking Shed**'s and declined to *take* the rotation, the opposite direction. `plans/0012`
**Cause 5**, and the tell was the same as ever — a number quoted with somebody else's clause. And the
brief's *addition bound checked at use with a proximity wake* **cannot be built in this milestone at
all**: both hang on a per-Citizen Habit, which this document's own *must not do* list forbids. Third
consecutive task whose brief was wrong about a document (`adr/0093`).

**5. The rung is deliberately not chosen, and the blocker is a fixture rather than an argument.** The
detour numbers are an order of magnitude smaller than R5.5.4's — 14 of 1,254 at 0.44% against 38 of
412 at 16.35% — because **both shipped Rulesets set `arterial_count = 0`**, so a four-Segment gesture
deletes four *Streets* on a dense lattice and everybody walks round one block. That is milestone 5a's
***severance is a property of the grid's fineness relative to the barrier***, arriving on the detour
axis rather than the connectivity one. On this fixture the exact rung spends **34 extra searches to
correct one stale route**, which chooses nothing. All three rungs ship; `0002` §C names the machine.

**6. The handle is load-bearing and the Epoch alone would have been a live defect.** A validated entry
compares each Segment's stored Epoch against the current one — and a freed slot is recycled, with the
new Segment opening at **Epoch 1**, which is exactly what a never-edited Segment carries. So a stored
Epoch of 1 on a recycled slot is a **false hit**: a route through a road that was demolished and
replaced by a different road in the same slot. Reachable in the committed golden session, which
bulldozes at Tick 129 and lays at Tick 200. The entry stores `Handle<RoadSegment>` beside the Epoch and
`Rows.IsValid` catches the generation. ***An Epoch is a per-object clock and says nothing about which
object it belongs to.***

**7. The measurement instrument had to compare costs and not routes, and comparing routes would have
reported every rung as equally bad.** Two different Segment lists of identical cost are the same
answer, and a lattice of identical Streets produces them constantly — which is precisely why
`WalkScratch.Precedes` breaks ties on the node slot. The detour column is `served cost` against
`fresh search cost` on the graph as it now stands.

**8. Two smaller ones.** `RouteCache` is the first thing here to need a **named integer mix**, because
`object.GetHashCode()` is banned in `Core` and an inline shift-xor would have been a fourth
undocumented mixing function in the tree; splitmix64's finaliser is used and named. And the analyser
caught the constructor's `entries / Ways` as `BOR0203` on the first build — **the raw-`/` lint firing
on a line where truncation genuinely is the intent**, which is the case it is most often argued away
in.

---

### Task 5 — the vehicular Leg at Statistical resolution. ✅ **DONE 2026-08-14**, and the decision it
was gated on was not the one that blocked it.

[`adr/0098`](../docs/adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md).
`WalkRouting.Cost` takes a `TravelMode` and selects the subgraph, the connectivity labels and the speed
rule from it; `TripEngine.Start` takes one and stamps it on the Leg; `CommuteEngine` and
`EmploymentEngine` both read `World.ModeOf`; a `[households] car_ownership_percent` decides who drives.
**No State Hash moved**, because neither shipped Ruleset states a `[households]` table.

⚠ **1. The owed decision had already been taken, and reading a doc-comment instead of the ADR nearly
shipped a violation of it.** The first cut of this task built a car commute as **one** door-to-door Leg,
on the ground that `World.VehicleAccessPoint`'s doc-comment forbids session F's trap — which it does.
But the trap it forbids is a **fallback from an exhausted Shed**, and `adr/0008`'s decision line is
unamended: *"a car commute is therefore never one Leg; it is at minimum `walk → drive → walk`"*. Session
F went further and **named the placeholder** in that ADR's own amendment — the flanking Legs run from the
pedestrian Access Point to the vehicle one, *"which are equal by construction today, making those Legs
zero-length"*, with the milestone-8 retrofit priced at *"one endpoint swap"*. So the three-Leg shape is
**specified**, with its placeholder chosen and its cost named, and a car commute here is **three Legs**.
***A doc-comment forbidding one shape is not a decision permitting the others*** — `adr/0093` exactly:
the comment was correct, it named the right symbol, and what was in it was a **prohibition** rather than
the **specification**, which is silently permissive everywhere it does not reach.

⚠ **1b. The multi-Leg machinery 5b built had never been exercised.** Every Trip in this project has had
exactly one Leg since Trips existed, so `AdvanceTravellers`' cursor, `TripTable`'s Leg list and *mean
Legs per Trip* were all running on their trivial case, and a one-Leg car commute would have left them
there while looking finished. `TripEngine.Start` now takes **Building slots** rather than Addresses, so
the per-Leg Access Point choice happens inside the one door and no caller can get it wrong.

⚠ **2. What actually blocked the task is that nobody decides who drives, and the corpus has no
instrument that could have told us.** `CommuteEngine` is the only Trip generator and it walked
everybody. Mode choice appears in **no milestone row in `06`** *and* in **none of its *Mechanisms with
no milestone* rows** — because that inventory's own opening line is *"every row below is settled by an
ADR and appears in no milestone anywhere in this document"*. Under `adr/0070` it is therefore
**undesigned**, not unbuilt. ***An inventory of unplaced mechanisms structurally cannot list a mechanism
nobody designed***, so the one place anybody would look is blind to the entire class. **Fourth
consecutive milestone** to find a precondition it had not finished counting — 5b task 4's missing
destination set, 5b's missing path, 5c scoping's *volume needs vehicles and not merely a path*, and now
the vehicles with no driver.

**3. The design answers it one level up, and the answer was already half-built.** `01 §8` ledger #3 is
*is car ownership a choice?*, **live and half-answered**: session five settled that ownership is a
**persistent Household state**, and `01 §8` says of the other half in its own words — *every Household
owning a car is the simple assumption… only becomes interesting once transit exists*. Transit has no
milestone. So an exogenous rate on the **Household** is the design being followed rather than
`adr/0070`'s *given X does not exist, should Y compensate*, and it lands on the entity the design names.
It is also the only shape this milestone could have used: a Household that owns a car drives **every**
day, so its route is stable, which is what `adr/0060`'s Habit and task 4's cache both rest on.

**4. Ownership is derived from the Ruleset, and the reason is a property nobody predicted.** It is
`hash(seed, household id, tick 0, CarOwnership) % 100 < rate` with **no column** — `adr/0068`'s rule and
`TripRuleset.TryRung`'s, on a fourth axis. What the derivation buys beyond avoiding `adr/0064`'s
frozen-at-construction defect is that **the owner set is *nested***: a fixed per-Household draw against
a moving threshold, so lowering the rate takes cars only from the Households at the top of their own
ordering. **Both saved alternatives fail, in opposite directions** — re-rolled on reload churns the
entire city for a one-point change, not re-rolled does not respond at all. A test walks the rate down
five rungs asserting nobody ever *acquires* a car.

**5. A Citizen is judged for a job on the clock they travel on, and that is `adr/0008` rather than a
refinement.** Session F refused a per-mode weight on the Commute Budget *precisely so* a walk and a
drive are compared on one clock, and one clock only works if it is read in the mode the journey is made
in. The rule lives on `World.ModeOf` and not in either engine, because two copies would let a Citizen
take a job they could **walk** to and then **drive** there — `plans/0012` **Cause 1** written in code.

⚠ **6. The observable had to be a flow, and a table scan reported zero for a city that made thousands of
journeys.** A completed Trip is released and its Legs with it, and on this fixture a commute is
**sub-Tick** — created and completed inside one call to phase 4 — so counting live rows in `LegTable`
finds only the handful still in flight. ***A table scan counts what survives, and a flow is the thing
that did not.*** The Trip Census family gains `walk legs` and `drive legs`, which tasks 6 to 8 need
anyway since only a vehicular Leg increments volume. ⚠ **The family now carries three denominators that
do not cross** — Fates count Trips that *ended*, cost bands count Trips *created*, these two count
**Legs** — and in a city of drivers `walk legs` is **twice** `drive legs` while neither equals the Trip
count.

**7. The car route-length distribution, which is the number task 4 owes `plans/0002` §C.** The **drive**
Leg only — the two flanking walks cross no Segment, which is what *zero-length* means. Measured at 100%
ownership on the shipped geometry, **not fitted to anything**:

| Citizens | Commutes | Median | p90 | Max | Mean |
|---:|---:|---:|---:|---:|---:|
| 1,000 | 245 | 1 | 3 | 5 | 1.7 |
| 4,000 | 1,074 | 4 | 8 | 12 | 4.1 |
| 8,000 | 2,137 | 6 | 11 | 17 | 6.1 |
| 16,000 | 4,403 | 8 | 16 | 26 | 8.8 |

⚠ **Do not extend it by fitting it** — that is the error `plans/0012` **Cause 5**'s seventh sighting
records, and the mechanism here is a **different and much weaker** cap than the foot one: the 50-minute
ceiling at a Street's 50 km/h reaches **41.7 km**, wider than the paved extent of any city this project
can build, so on the car side **the map bounds the route and the Budget does not**. A bound that comes
from the fixture moves when the fixture does.

⚠ **8. A drive and a walk take the same Streets here, and that is a property of the fixture rather than
a finding about modes.** Both shipped Rulesets set `arterial_count = 0`, so no Segment admits one mode
and not the other, and the two distributions agree **exactly** — 1,074 routes, mean 4.08 Segments, in
both. ***A comparison run on a fixture that lacks the mechanism under comparison measures the fixture.***
Recorded because the number moves the moment a player lays an Arterial, which `adr/0090` makes the only
way one can now exist, and somebody would otherwise read today's agreement as evidence the modes are the
same.

⚠ **9. Every drive in the project is quoted too cheap, and it is the one mode where that grows with the
city.** `03 §3.7` makes free-flow the *exact* answer for a walk because pedestrian networks do not
saturate; for a car it is an underestimate until task 6's volume-delay function lands. Related and
unrepaired: the **job-search box is still derived from the Budget at *walking* speed**, so a driver's
catchment is a pedestrian's box — inert today (`plans/0002` §C: the box holds 100.0% of Buildings up to
~160,000 Citizens) and silently clipping the moment a city outgrows it.

**10. Two smaller ones.** `WalkRouting`'s two **closed-form** cases — a journey along one Segment, and
one refused by the reachability test — never reach the search, so `PathTo` answers *no path* for a
journey that is perfectly possible: ***absent is not zero and it is not impassable***, and a volume pass
that read a missing path as *no Segments to credit* would drop every same-Segment drive in the city. And
the mode parameter is **required with no default**, because task 3's own finding was that a mode
threaded through a signature and not through a loop compiles and passes every test — a `Foot` default
here is that hazard pointing the other way, and **Foot is a legitimate answer, so it cannot announce
itself**.
