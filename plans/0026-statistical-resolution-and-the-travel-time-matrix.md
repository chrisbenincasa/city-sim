# 0026 — Statistical resolution and the travel-time matrix

`06` milestone **5c**. The brief, then the record.

---

## Status

⚠ **NOT STARTED — scoped 2026-08-14.** All three named gates are discharged and none of the closures
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
| 1 | **What a drive Leg's endpoints are with no Parking Shed** | task 5 | Session F's placeholder trap is specific and expensive, and the two honest options — refuse the car commute, or name the absence — are a real fork rather than a detail |
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
