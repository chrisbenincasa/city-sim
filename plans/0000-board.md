# 0000 — The board

**Read this first.** A flat, scannable status of everything planned and everything done.
It is a *view*, not a source: [`0003`](0003-build-plan.md) owns the slice order and its gates,
[`0002`](0002-open-questions.md) owns the reasoning, `docs/adr/` owns the decisions. When they
disagree, they win. Update this file whenever a task lands.

**Where the project is:** Phase 1, **slice 5 closed** — all eight tasks, less task 7's trend assertion,
which was deliberately not written. The State Hash has a committed baseline under it,
`Borough.Headless` replays a `.borough` log and prints a diffable hash trace, the three invariant
tiers run in release on every Tick and at the end of every run, `--census` prints what every
collection did over a run, and a panic writes a crash artifact that replays back into the same panic.
**Slice 6, Map Layers, is done, and with it the last slice before the Phase 1 gate closes.** The Cell
grid, the sparse double-buffered Layer table, the separable integer convolution with superposition
and transpose invariance asserted bit-for-bit, the staggered schedule, incremental re-diffusion
proved identical to a full recompute, the three real Layers, the named holes that throw rather than
answering, `layer_cells(aabb, layer)` allocation-free, and the 100,000-Tick acceptance run.
`--layer pollution` prints a field, which is **the first thing this project has shown that is not a
number**. **Its owed decision is discharged by measurement rather than argument** (`adr/0044`), the
first time `adr/0043` has been applied outside the routing spike — and the claim was false. **The ADR
then got its own second half wrong by argument and had to withdraw it**, which is recorded rather
than amended away; see `0009` → *What building it found*, finding 6.

**The spike track has opened and moved six times.** **S2 R0 through R4 are done, and R5 is three
sections in** — the synthetic Road Graph, the density curve, the uncached denominator on the real
`(Segment, offset)` query shape and the heuristic verdict; then the travel-time matrix; then the path
source, the crossover and the attribution lag; then HPA\*, the cluster it owns, and the Tick budget
none of them fit into; then distance-vector, which lost to a scheme nobody had named; and now the
**edit storm**, which changed the unit. Numbers and decisions in
[`spike-results`](../docs/spike-results.md).

**R5 measured the gesture rather than the edit, and that is why it moved things two tasks could not.**
R3 and R4 each priced *one deleted Segment* and each said the case they could not reach was hundreds
in a single drag. **A player does not delete a Segment; a player drags.** Measuring the drag **fired
the global-flush tripwire** — `plans/0010` says a candidate needing one is *"out on a design
commitment, not on a number"*, and a single-counter Epoch **is** a global flush: against a no-edit
ceiling of **71.63%**, per-Segment keeps **96%** of it under a continuous storm and global keeps
**9%**. It **closed cluster size against R3's own bias** — 8 Chunks, not 16 — and it found that **the
per-Segment repair loop both earlier tasks wrote costs 23.26× the coalesced one**, with a worst case
of **253.22 ms from one gesture, sixteen Tick budgets**, invisible to both because the two spellings
are identical at a gesture of one. **The ladder's framing was also wrong**: this plan priced it as
*hit rate against revalidation cost* and there is no such trade — per-Segment is cheaper *and* more
precise at every edit rate, because a revalidation word is arithmetic and what it avoids is a search.

**R4 retired distance-vector and found something larger on the way past.** DSDV is out — not on
memory (**23.12 MiB** at the anchor, the tripwire does not fire) and not on correctness (with
sequence numbers it converges to exactly the rebuilt table) but **because it costs 2.13× the rebuild
it exists to avoid**, for a structural reason: an odd-sequence poison outranks every finite route in
circulation, so only a newer *even* number from the destination restores one, and **one broken link
re-floods the destination's whole tree.** *The property that makes deletion safe is the property that
makes deletion expensive.* The winner is **dynamic subtree repair at 4.71 ms against a 234.74 ms
rebuild**, which this plan never named — it was measured only because pricing solely the candidate a
plan names is how a spike produces a verdict it has not earned.

**And R4.8 is the largest finding in the spike so far, because it moves a number the corpus was
about to build on.** R2's **18.52%** mean detour for a next-hop table turns out to be a property of
the *draw*: S2 has sampled origin-destination pairs uniformly since R0, which R4.1 shows is the
**longest-trip distribution available** — 8.53 km mean on a 16.4 km map. On a plausible local-trip
distribution the same table's detour is **128.82%**. **A Traveller driving more than twice as far as
it should is a different city under `05 §4`**, so this is not a tuning figure — and it is R2's
representative funnel arriving from the other side. **The O-D draw is now a swept family**, which is
what makes R6 runnable at all.

**R1 answered the question the whole spike order was built around, and the answer is yes.** The matrix
carries the choice loop — **1.14 ns** scattered at the working District count against a tripwire at
13.66 ns — so `02 §5.8`'s *never resolve a route inside the choice loop* is enforceable. It also
settled three things nobody had a number for: **`adr/0020` is owed an amendment** (union-find returns
6 Settlements where Tarjan returns 8), **`02 §6`'s dirty-region rebuild is unsound** (it misses 72% of
the entries an edit changes), and the **volume-scope question R0 was told not to settle is the same
question as the `adr/0020` exposure**.

**R2 was supposed to retire DSDV and made it live instead.** The plan retires R4 *"if Statistical Trips
need no concrete path"* — but that clause predates `adr/0041`, which requires a Traveller to increment
the Segment it *enters* every Tick. What that needs is a **next Segment**, not a path, and a next-hop
table supplies one while storing none — which is distance-vector's data structure, reached from the
attribution side. R2 built it as a third ladder rung and it is the **better** of the two survivors on
error (**18.52%** mean detour against a shared route's 36.01%). **R4 is next**, and **R5 is where the
router is actually decided**, because the axis the two survivors differ most on is invalidation.

**R3 was supposed to confirm HPA\* and it weakened it — then found that nothing fits.** The cluster
narrows to **8 or 16 Chunks a side, the bias on 16** — and **R3 cannot close it**, because the axis
that separates them is an edit rate **R5** owns. And `adr/0014`'s *"the Chunk grid is already the
pathfinding cluster"* is measured false by **256× in area**: at one Chunk the abstract graph *is* the
Road Graph. What the hierarchy buys is **3.08×** on a cost-only query and **2.63×** once it must
return arcs — and R1 already answers the cost-only question at 1.14 ns, so the larger figure is
against a customer with a better answer. **The plan's *current standing favours HPA\** no longer has a
measurement behind it, and R4 runs against an open comparison.** Three things had to be measured
before that verdict was safe: the **transitive reduction** of the intra-cluster edges is mandatory and
lossless — degree 40 to 3, double the speedup, 100% optimal — **storing each intra-edge's arcs** is
mandatory alongside it, worth 1.50× → 2.63× on the refined query for 223.92 KiB — and **Botea's
transition sampling is out** at 80.49% mean detour. R0's amendment landed twice more, once on the
hierarchy and once on **the denominator itself**.

**No cluster size fits routing into the Tick budget, and that promotes R6.** The best rung refines a
route in **181,554 ns**, so **85 Trips may start per Tick** before routing owns the whole 15.6 ms. The
load is U-shaped in cluster size and pinned at both ends, so this is a floor rather than a rung that
was missed. The two exits are a **route cache** and **eight cores**, which makes **R6 load-bearing
rather than a late tidy-up** — whichever router R4 picks will need it. The figure is published as a
break-even rather than as *6.4× over budget* on a rule R3 states and the plan now carries: **gather a
tripwire as direct data where the data exists, and where it does not, invert the derivation until what
is published is measured.** The 550-arrivals denominator is a guess, and a wire built on it fires on
the guess.

**R2 also found the aggregate scheme fails worse than its tripwire anticipated.** `03 §3.3` confessed a
*lag*; the measurement says the smear reports the jam **in the wrong place** — 0.00% deposited on a
Segment carrying 108%, at every cycle including one Tick, where no cadence is left to blame. The two
schemes agree on *how many* Segments are stressed and disagree on *which*. **Force-promotion loses its
last bundled justification**, and `adr/0041`'s *"no correctness content"* about the path source is
owed a correction on two counts.

### Picking up from here — the state R5 left, and what it owes a decision

**R5.1–R5.5 are done and written up; R5.6 is not started.** The harness is
`spikes/S2.Routing/Storm/` plus `Harness/StormReport.cs`, run with `--storm` or `--path-source`.
Numbers in [`spike-results`](../docs/spike-results.md) → *S2 R5*; raw capture in
`spikes/S2.Routing/results/s2-r5-…-performance-turbo-cpu2+8-20260807T151916Z.md`, with **two more
captures of the identical configuration retained beside it** — so R5's counts are checked four ways
and its millisecond columns carry a measured spread rather than a disclaimer.

**The canonical capture is DONE, and taking it found a harness defect that had been distorting the
whole spike.** `spike-results` → *S2 R5* now quotes a pinned `performance` run throughout. Every
count is bit-identical across all three captures, so no conclusion moved — but three published
millisecond figures did, and **six others turned out to exist in no retained file at all**.

**The protocol's pinning was wrong, and it is fixed.** `routing-run.sh` pinned with `taskset -c 2`,
one logical processor, leaving the SMT sibling idle — so the .NET tiered JIT's background
compilation had nowhere to run but the measured core and landed on whatever was timed first.
Measured inside one capture by the denominator R5 already takes twice: **214.94 ms first against
43.99 ms last, 4.88× apart**, where the same pair under `taskset -c 2,8` reads **0.92×**. It
inflated the first-timed half of R5.2's table by ~3× and **reversed the 8-versus-16 cluster verdict
on its face**. **This answers the board's own open question about R0** — *why plain Dijkstra's
absolute moved 1.64× under pinning* — without needing either check that entry proposed.

**And every earlier `performance` capture in S2 was taken the same wrong way.** R0, R1, R3 and R4,
including session eleven's canonical re-capture. Counts and in-process ratios are untouched;
first-timed absolutes are not. Filed under *Owed*; R7 owns the re-capture.

**The harness now names every capture by section, CPU set and capture time, and never overwrites.**
The old spelling wrote every run to one filename keyed on machine configuration alone, so `--storm`
displaced a whole-run capture and a later run displaced that. That is how R5's first write-up came
to publish `3.79 ms`, `7.61 ms`, `161.79 ms`, `219.50 ms`, `21.25×` and a 13.26 ms worst Tick with
**no artefact behind any of them**.

**Four findings are recorded as debts and three of them still want a decision, not just a record.**
They are listed individually under *Owed* below; what follows is the shape of the argument each one
needs, because a debt that is filed but never typed is how this corpus has twice let a measured-false
claim sit in a 🟢 row.

| | The finding | What it wants | Where |
|---|---|---|---|
| **1** | **SETTLED by R5.5.4.** No Epoch rung was both affordable and correct across the whole core verb — per-Segment declares **100.00%** of the cache valid under addition and cannot ever notice, and only global is sound. **A TTL rotation removes the choice**: at **0.40 forced refreshes per Tick** the wrongly-valid count goes **38 → 0 within one rotation while 97.08% of the cache is retained**, against a control that plateaus at 23 and never moves again | **Session M** still owns the *contract* — whether modelled ignorance is what the city wants — but it now chooses rather than improvises | *Do these next*, row 1 |
| **2** | **A mean per-route cost times an arrival rate does not bound a Tick.** R3's *fits below 85 Trip starts* is a mean; at **16** starts R5 measures a worst Tick of **10.37 ms** of 15.6 ms | **Reopens the routing Tick budget share**, which `0002` already records as an unratified guess. The 10% row and R3's 85 need restating as a *worst*-case wire, not a mean one | *Owed — decisions* |
| **3** | **NOT GENERAL — R5.5 tested it and it does not generalise (0.91–1.51×).** A per-edit repair API invites the loop that destroys it — 23.26×, a **253.22 ms** worst gesture — and above ~63 clusters touched the coalesced repair loses to a full rebuild outright | **A shape decision, not a number**: anything the player can do to hundreds of objects at once needs its API shaped for the *gesture*. It generalises past routing and should be settled where the Chunk/Cell edit surface is | *Owed — findings* |
| **4** | **The eviction policy is a bigger lever than the Epoch below the highest edit rates** — 28–31% of lookups miss on direct-mapped collisions before a road is touched | **R6 owns it**, with `adr/0017`'s fixed-capacity least-used pattern as the candidate. R5 supplies the evidence that it is worth arguing | *Owed — findings* |

**And two caveats that must travel with any figure quoted out of R5**: the hit-rate *levels* rest on
an **invented pool** standing in for Trip repetition, and the Street half of R5.4 reads 0.00% because
**the synthetic grid is degenerate** — one Street per Cell boundary at a uniform speed gives very many
equal-cost shortest paths. Both are filed under *Owed*.

**A third caveat outranks both, and it belongs to all of S2 rather than to R5 — it is why R8 existed.**
**Every figure this spike published before R8 ran on a frozen cost basis.** R1's matrix, R2's ladder,
R3's hierarchy, R4's protocols and R5's storm all route over an arc-cost array computed once and never
moved. The storm invalidates a route because a *road was bulldozed*; nothing in S2 had ever
invalidated one because a road got **busy** — and under `adr/0041` the volume column moves every Tick.
**R8 has now closed the loop and quoted the numbers**, so this caveat is discharged for R8 and stands
for everything above it: quote nothing from R1–R5 as a statement about a *congested* city.

**And R8's own headline is not about routing cost at all.** *The network runs out of routes, not road* —
87.25% of traffic on 1% of the carriageway, 90.87% of it empty, capacity confirmed realistic. **It is
row 1 of *Do these next*** and it is a design question rather than a spike's.

---

**What is in front of the project is mostly argument, not code.** Slices 7–10 and every Phase 2
milestone are gated on designs written from research and never grilled — **eleven** sessions, tabulated
below, none of which touches slice 6 and almost all of which can run beside it. The board used to
list those gates as 🔴 marks against slices, which read as *wait*; they are work, and they are
available now. **One of the twelve, `06` itself, turned out to be two** — see session nine in *Done*,
and the audit note below it, which now has a diagnostic rather than just a suspicion.

---

## Do these next

**Three tracks, and they do not contend for anything.** The code track is somebody at a keyboard, the
argument track is a grilling session, the spike track is a machine running unattended. This board has
only ever ordered the first — which is why Phase 2 has looked further away than it is. **Almost
nothing standing between here and Phase 2 is code.**

| | Track | Task | Where | Why this one |
|---|---|---|---|---|
| **1** | argument | **The tree, and what a routing destination may be** — decision 11 on a **different axis** | [`0010`](0010-s2-routing.md) §Decisions owed 15 | **NEW, produced by R8, and it outranks everything else the spike found.** At 13% of this network's holding capacity, **87.25% of all traffic sits on the busiest 1% of the road and 90.87% of it carries nothing** — with capacity confirmed *realistic* (3,600 veh/h a Street reduces to a two-second saturation headway). **The network runs out of routes, not road.** One free-flow shortest-path tree per District means there is exactly **one route per (node, District) pair in the entire model**, and no amount of empty parallel carriageway can be reached from it. **R2's representative funnel does not bind** — R8 widened the definition once, printed both, and the columns are identical to the printed digit — so decision 11 has been argued as *how many access nodes a District exposes* and that is the wrong axis: a District with a hundred access nodes still has one tree per destination. It is also why **no rung of R8.0's load sweep is both congested and resolvable**, and why session M has a **fourth** defect in the same column as structural error, temporal error and diversion cost — *and it is not a cost, it is a spatial distribution*: the table's error is **correlated across the whole fleet**, not distributed over it |
| **1b** | argument | **M — the route cache's invalidation contract** | [below](#the-argument-track--what-stands-between-here-and-phase-2) | **R5.5 is done and it hands M everything it was waiting for**, and R8 is about to hand it one thing more. **Under Sight, a mid-journey diversion stops being exceptional and becomes routine** — and it is *free* under a next-hop table (read the table from wherever you now are) and costs a **fresh search** under a stored route. That is a per-Tick bill that scales with how congested the city is, and no version of M argued before this week knew it existed. **The standing brief is unchanged**: no Epoch rung is both affordable and correct across the whole core verb — per-Segment is exact under deletion and declares **100.00%** of the cache valid under *addition*, where it structurally cannot notice. **Five candidate mechanisms are tabulated** in [`spike-results`](../docs/spike-results.md) → R5.4, **two of them questions of intent rather than of cost**, so `adr/0043` types those *arguable* and a session may close them. **It gates R6**, which R3 promoted to load-bearing, and it is owed to `adr/0012` as the amendment that ADR has carried since R2. *Take M after R8.6 prices the third axis.* The path source turns out not to be one choice: a maintained next-hop table and a route cache are wrong in **different currencies** — the table's error is structural, fixed and visible (**16.58%** uniform, **149.73%** local, unmoved by a storm that deletes 1,021 Segments); the cache's is temporal, near-zero while it lasts, and under addition **permanent**. Neither is a rung on the other's ladder, so **which the city should have is `05 §4`'s question and not a benchmark's**. And R5.5.4 measured the way out: **a TTL rotation at 0.40 forced refreshes per Tick takes the wrongly-valid count 38 → 0 within one rotation while retaining 97.08% of the cache**, against a control that plateaus at 23 and never moves again. That is option **C** measured and it is what makes option **B** a design position rather than a defect — `BOUNDED KNOWLEDGE` permits ignorance of a new road **if it is modelled with a stated learning rate**, and a rotation period is exactly that |
| **1c** | spike | **S2 R5.6 — the Parking Shed** | [`0010`](0010-s2-routing.md) | The second Epoch consumer, and the last open section of R5. It scales with **Buildings** and is a *neighbourhood* rather than a *path*, so per-Segment has no obvious meaning for it and per-cluster fits it far better. **`CONTEXT.md` → Epoch must not be updated until it runs**, because a rung chosen on routes alone is chosen on the cheaper of the two consumers |
| **3** | code | **S0 — the synthetic 1M-Citizen city** | [`0003`](0003-build-plan.md) | **Slice 6 is done, so the Phase 1 gate closes here and this is what is behind it.** The corpus forbids opening Phase 2 content until S0 has run. Slices 7–10 are each behind a session in the argument track below, not behind code — so this is the only *code* task left that nothing else gates |
| **4** | argument | **`adr/0015` — hot reload** | [below](#the-argument-track--what-stands-between-here-and-phase-2) | **Slice 7's whole gate now runs through it.** `02 §4` residue is **closed** ([`adr/0045`](../docs/adr/0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md)) and closing it handed A **two named refusals** — the `on_fail` cycle check and the `fills` check — which are load-time Ruleset validation on this ADR's own error surface. Fold the **TOML dependency exception** in: a parser is what runs those refusals |
| **5** | argument | **`02 §7` + `adr/0006`** | [below](#the-argument-track--what-stands-between-here-and-phase-2) | Slice 9. And `02 §4` now leans on the Wheel harder than it did: a chain is walked **once on entry into shortage**, which is a wake rather than a poll |

*Why S2 first now:* the argument for delaying it was that the golden baseline should exist before
throwaway spike code starts changing `Core`. It does, and the runner is what a person uses to look at
what moved. Slice 5 is closed and no longer in front of it. **R0, R1 and R2 confirmed the delay cost
nothing** — the spike compiles the arithmetic substrate in by source and can name nothing else of
`Core`, so it has changed no simulation code at all.

*Why an argument session sits this high for the first time:* every remaining Phase 1 slice and every
Phase 2 milestone is gated on one, and none of them is gated on code. Running them behind the code
rather than beside it is what would make the Phase 1 gate a wall instead of a line.

---

## The argument track — what stands between here and Phase 2

**Phase 2 is not blocked by code.** Every milestone in it (`06` 5a–10) waits on a design written from
research and never argued, on one spike, or on a number nobody has chosen. `0002`'s readiness review
states the shape plainly: *Phase 2's wall is one large item, not many small ones.*

**None of these touches Map Layers, so all of them run in parallel with slice 6** unless the last
column says otherwise. Ordered by what they unblock, soonest first. Each is a session, not a task.

*Slices and milestones share numbers and are not the same thing* — slice 8 is hot reload and
milestone 8 is parking — so the *Unblocks* column always says which.

| | Session | What is actually missing | Unblocks | With slice 6 |
|---|---|---|---|---|
| **A** | **`adr/0015`** — hot reload | **No longer "never grilled at all."** `adr/0045` hands it **two named refusals** — the `on_fail` cycle check and the `fills` check — both load-time Ruleset validation on the error surface this ADR already specifies. Plus `06`'s *must not slip behind 3c*, which is unargued and circular, and the **TOML dependency exception**, since a parser is what runs the refusals | slices **7** and 8 | **yes** |
| ~~**B**~~ | ~~**`02 §4` residue**~~ | **CLOSED** — see *Done*. Produced `adr/0045`, struck `mean_workforce_experience`, inverted the Readout bound, and settled apply count. Its cycle-checking half moved to **A**, which is what moved A onto slice 7's gate | ~~slices 7, then 10~~ | — |
| **C** | **`02 §7` + `adr/0006`** — Event Wheel | Both never grilled. `02 §7` is partly spoken for by `adr/0033` and must be **read against it rather than fresh** | slice 9 | **yes** |
| **D** | **`03 §5`** — the traffic model | **The wall.** The most detailed unargued design in the project, now carrying transit vehicles. It is one large item and should be booked as more than one sitting | milestones 5b, 5c, 6, 7a | **partly** — the half that wants S2's numbers waits for R1–R3; the rest does not |
| **E** | **`adr/0005` + `adr/0007`** — fidelity | One session, not two: `0007` moved Fidelity from person to **place**, and `0005`'s tiers are what it moved. Written from research, not argued | milestones 7a, 7b | **yes** |
| **F** | **`adr/0008`** — walking is a simulated Leg | Written from research. It is what makes 5b *the irreversible milestone*, so the argument is owed before the Leg model is built rather than after | milestone 5b | **yes** |
| **G** | **`adr/0016`** — the lane is the entity | Written from research. Carries the order-of-magnitude claim the whole microscopic tier rests on | milestone 6 | **yes** |
| **H** | **`adr/0009`** — parking is modelled supply | Written from research. Its `adr/0006`-class occupancy leak is already named and needs the invariant specified with it | milestone 8 | **yes** |
| **I** | **`adr/0012`** — routing intent lives in the agent | Written from research, and already owes an amendment: the route cache's **eviction policy** and its **key** | milestone 5c | **after S2 R6** — the two caches are R6's subject, and R3 promoted R6 to load-bearing |
| **J** | **`05 §7` format half**, plus **map size** and **Outside Connection layout** | The three things `06`'s open-decisions table still has blocking save/load, narrowed from the map question that `adr/0020`–`0022` otherwise closed | milestone 10 | **yes** |
| **K2** | **`06`'s Phase 2 ordering** | The ordering only. **K1 is done** — see *Done* — so what remains is re-deriving the sequence against conserved Money, Hinterlands, Office, the labour system, transit and every Service, and placing the **seventeen mechanisms `06` now lists as having no milestone** | Planning Phase 2 at all | **last** — A–J move what it sequences |
| **M** | **The route cache's invalidation contract** — what a cached route is allowed to be wrong about | **Forced by measurement, and R5.5 has since supplied the numbers it was to argue without.** S2 R5.4 found no Epoch rung both affordable and correct across the core verb. **R5.5.4 measured option C and it works**: a TTL rotation at **0.40 forced refreshes per Tick** takes the wrongly-valid count **38 → 0 within one rotation while retaining 97.08%** of the cache, against a control that plateaus at **23** and never moves again — which is R5.4's *does not heal* measured rather than argued. **That makes option B a design position rather than a defect**: `BOUNDED KNOWLEDGE` permits not knowing about a new road **if the ignorance is modelled with a stated learning rate**, and a rotation period is exactly that. **So M is no longer choosing between five mechanisms — it is answering one question**: is modelled driver ignorance what this city wants, and at what rate? **And R5.5 adds a second half it did not have**: the path source is not one choice, because a maintained table and a cache are wrong in **different currencies** — structural and visible (16.58% uniform, **149.73%** local) against temporal and, unrotated, permanent. No benchmark ranks those; `05 §4` does. **Also still open: whether the contract is one contract**, since `05 §3`'s Parking Shed is a *neighbourhood* rather than a *path* | **R6**, and `adr/0012`'s owed amendment | **yes** |
| **L** | **A presentation design** | **It does not exist.** Every other phase is backed by a design document; rendering has none, and `05 §2`'s sim/render boundary is on the never-argued list while `adr/0002` was re-argued to serve *inspection*. **Write it first, then grill it** — unlike A–K this is not a session against an existing document | Phase 3, and planning it at all | **yes**, but blocked on S1 and S3 |

**Not arguable, and it is worth being explicit about why.** The **Microscopic Cap**'s value needs a
built traffic model; S2 R2 only informs it. **S2** itself is measurement — argument cannot close it,
which is exactly why it sits at the top of the code-adjacent order rather than in this table.

**Cheap, and due before slice 7 rather than during it:** a **TOML parser library is unnamed**, and
`adr/0003` requires any core dependency be argued against it explicitly. A determinism liability
entering the core needs a written exception. `0003` calls this argument cheap and says it should not
happen mid-slice.

### What must *not* be grilled yet

`0002` names these as playtest questions wearing design-question clothing, and the argument track
should not drift into them: health (#26), recreation (#27), Service variants (#28), car ownership
(#3), private capital (#7), and `01-player §1/§3/§4`. The governability problem especially —
*268 km² of individually-placed service Buildings* — **is not answerable by argument.** Somebody has
to try placing them.

### Audit these for the shape `adr/0043` names

**Every ADR and every design section is in scope, not only the ungrilled ones.** `adr/0043` requires a
claim to be typed *arguable* or *measurable*, and nothing in the corpus has ever been typed — including
the documents `0002` marks 🟢. Two of the five claims S2 measured false sat in green rows. The board
already names the likeliest remaining suspects: **`adr/0016`** carries the order-of-magnitude claim the
whole Microscopic tier rests on, and **`adr/0009`** and **`adr/0008`** are the same shape. Each reads
as decided and none of them has a number.

**`0002`'s blanket rows are part of the defect.** `adr/0010`–`0022` is thirteen ADRs under one green
mark from two sittings, two of which are now known false. A status whose granularity is coarser than
the claims it covers cannot be checked; split those rows as each ADR is revisited.

### Audit these for the shape `adr/0003`'s debt had

`0002` recorded a finding worth acting on before booking any of A–K: `adr/0003`'s owed validation sat
undischarged because **two separate debts had been filed as one**, and the runnable half was parked
behind a grilling session it did not actually need. Its own instruction — *worth auditing the other
🔴-blocked debts for the same shape* — has not been carried out. Doing it first is cheap and may
move work out of this table and into the code track.

**There are now two data points, not one.** Session nine found `06` to be the same shape by accident:
K was scheduled last because *"A–J move what it sequences"*, and that argument binds only the
**ordering**. Correcting claims that settled decisions falsify — K1 — depended on nothing, and ran in
one sitting. **The tell in both cases is a gate whose stated reason covers only part of what it
blocks.** The audit is still owed and now has a diagnostic to apply: for each 🔴 row, ask what the
gate's reason *does not* cover, and check whether that remainder is runnable today.

---

## Done

### Phase 0 / Phase 1 slices

- [x] **Slice 0 — solution scaffolding.** Four projects, build config, the three reflection guards, CI
- [x] **Slice 1 — S4, the kernel benchmark.** Tasks 1–10, all seven kernels on two machines, **no
      tripwire row fired**. Results in [`spike-results`](../docs/spike-results.md)
  - [ ] *task 11 — delete `spikes/S4.Kernels/`.* Held pending the XMP re-sweep, which is now **optional**
- [x] **Slice 2 — the arithmetic substrate.** All 7 tasks. Typed quantities, fixed point, tabulated
      `exp`/`log`, `draw()`, purpose tags → produced `adr/0038` and an amendment to `adr/0003`
- [x] **Slice 3 — the analysers.** All 6 tasks. Twelve diagnostics covering CI lints 2, 3 and 7 and the
      `purpose_tag` row → produced the rule-7 exception axis in `adr/0036`
- [x] **Slice 4 — typed tables and the field declaration.** All 11 tasks. Handles, columns, the single
      declaration, the State Hash, intrusive lists, `ResourceMap`, the first four tables → produced
      `BOR0901` and the project's first State Hash
- [x] **Slice 5 task 1** — `step(inputs)` and the eight-phase skeleton
- [x] **Slice 5 task 2** — the command model and the Input Log *(less the text codec)*
- [x] **Slice 5 task 3** — replay
- [x] **Slice 5 task 4** — the golden-hash baseline. A committed session trace *and* a committed world
      hash, because the session reaches one table in four until the player has verbs; the re-baselining
      procedure sits beside them
- [x] **Slice 5 task 5** — the headless runner, and **`Borough.Formats`, the fifth project**
      (`adr/0039`): the `.borough` codec, the hash-trace format the runner and the baseline share, and
      the Ruleset content hash. `--strict` inverted to a default refusal with `--force-ruleset` as the
      escape, per `05 §7`. `series(metric, window)` deferred to task 7, where the census gives it a
      second caller
- [x] **Slice 5 task 6** — the invariant tiers. Per-Tick at the write site, staggered by slice, and
      the whole-world walks at the end of every headless run. Throws by default so task 8 can catch at
      the Tick boundary; `Collect` is the switch for a balance run
- [x] **The tiers, costed.** A BenchmarkDotNet job in `Borough.Tests` against a constructed city:
      the staggered tier is **0.06% of a Tick at 100k Citizens**, and a full sweep of every row costs
      **a fifth of one State Hash**. `adr/0033`'s *unaffordable per Tick, trivial at the end of a run*
      is worth three orders of magnitude here. Numbers in [`0008`](0008-tick-and-replay.md)
- [x] **Slice 5 task 7 — the instrument, not the assertion.** The **Census** (`CONTEXT.md`), the
      `series(metric, window)` cold API deferred from task 5, and the runner's `--census` report.
      Three counters per table, because *slots climbing while live is flat* is the leak a row count
      cannot show. The ring is finite by construction — a census that grew with elapsed game time
      would be `adr/0006` in the instrument written to catch it — and an outrun window is **marked**
      incomplete rather than silently shortened
  - [ ] *the trend assertion.* Deliberately not written; see *Owed* below
- [x] **Slice 5 task 8 — the crash artifact.** `05 §8`'s reproduction rather than a dump: the log
      wrapped verbatim, the Tick that panicked, and the Ruleset actually in force. The runner takes
      an artifact wherever it takes a log, so **the loop closes** — one fed back panics at the same
      Tick and emits an identical file. `from` is the checkpoint-shaped field, zero until milestone
      10, and a reader that meets a non-zero one **refuses** rather than replaying a different city
- [x] **Slice 5 closed.** All eight tasks, less task 7's assertion
- [x] **Slice 6 closed — all ten tasks and the acceptance criteria.**
      `Borough.Core.Space`. The Cell and the Chunk are **two types**, not a comment, because the
      welding `adr/0034` split is the specific failure a comment cannot prevent. `LayerCellTable` is
      the project's **first `Buffering.TwoCopies`**, and slice 4's declared-but-unimplemented property
      is now real — `PrepareBack()` seeds the write half, `SwapBuffers()` makes it live. Superposition
      is **exact over twenty sources**, transpose invariance holds, **the in-place variant is kept in
      the test suite watching itself fail** rather than deleted, and incremental re-diffusion is
      **bit-identical** to a full recompute over twelve randomised rounds. Land value has **momentum**
      and Sealing is a **count**; `Fertility` and `Desirability` are **named holes that throw**, and so
      are the three line-source queries, because a placeholder returning zero is a value somebody will
      read, believe and tune around. `layer_cells(aabb, layer)` is the project's first hot query —
      allocation-free, measured, and checked by reflection to return no strings. **Six findings the plan
      did not anticipate**, in [`0009`](0009-map-layers.md) → *What building it found*: the rounding
      moved out of the passes, the double buffer turned out to be for **land value** rather than
      pollution, `PrepareBack` had to seed rather than clear, the integer lag had a **dead band** that
      was path dependence in stored state, the long-run test first measured a **transient** and read it
      as a leak, and `adr/0044` published a classification it then had to withdraw. Golden baselines
      re-recorded once, for a fifth table entering the hash composition and nothing else
- [x] **`adr/0044` — the Map Layer diffusion cadence is the designer's number, not the profiler's.**
      **The sixth claim in the corpus measured false, and the first outside S2** — the five before it
      came from the routing spike, and this one sat in `02 §1.2`'s **normative table**, the document
      other documents are told to cite rather than restate. Two worlds differing only in the diffusion
      period produce **different hash traces**, so under `05 §4` it is a design change. `adr/0043`
      worked exactly as written: the claim was typed *measurable*, the refuting number was named, and
      slice 6 was the machine. The kernel is settled with it — a separable tent reaching **1,024 m,
      unratified** — and the measurement found two things nobody asked for: **the divergence is
      transient** (a Layer is a convolution of its sources, not a function of its history), which
      strengthens the case rather than weakening it because a city is never settled; and **`02 §2.4`'s
      1–10 km plume band fails `02 §2.5`'s own guard rule 1** at 10× wide
- [x] **…and `adr/0044`'s first draft was wrong about where the number then goes, by argument.** It
      filed the cadence as a **world-creation constant**, reasoning that `adr/0015`'s Ruleset is *by
      definition* the numbers a designer may change without changing the city. `adr/0015` says the
      opposite in its own words — its content hash feeds the State Hash and reload is a **logged
      simulation event** — and its world-creation category carries a **membership test** (*was existing
      state recorded in units of the constant?*) that the cadence **fails** and the kernel radius
      **passes**. So the two numbers separate: the cadence is hot-reloadable Ruleset data the profiler
      may not touch, the radius is frozen per world. **`05 §9`, not `02 §1.2`, is where it was actually
      mis-filed** — the performance budget offered it as one of three multipliers. The finding worth
      keeping is the general one: **citing an ADR is not applying it**, and the difference is whether
      the test it states was run against the case. Cost: one document, because no code depended on the
      wrong half

### Planning and design

- [x] **S2 planned** — [`0010`](0010-s2-routing.md), and its gate cleared by defining **Segment** in
      `CONTEXT.md`
- [x] **`adr/0043` — a claim a measurement could settle must not be settled by argument.** Written
      after S2 R3, from the observation that **five claims in the corpus have now been measured
      false** and none of them had ever been *typed*. Every claim a grilling session touches is
      classified **arguable** or **measurable**; a session may settle the arguable ones and must
      route the measurable ones to a named spike, naming the number that would refute the claim and
      the machine that would produce it. The test is *can you name the refuting number and the
      machine?* **Two of the five sat in 🟢 rows of `0002`**, so the audit this implies is over every
      document rather than the ungrilled ones — the ADR's own first draft claimed otherwise and was
      corrected against the ledger before it was registered. It does **not** reduce how many sessions
      are needed: most of A–L are questions of intent, which have no refuting number
- [x] **S2 plan grilled before any code.** Thirteen findings; see *Owed* below for what it left behind
- [x] **S2 R0 — the Road Graph, the denominator, and the heuristic verdict.** `spikes/S2.Routing/`,
      which compile-links the arithmetic substrate by source and can name nothing else of `Core`, with
      the analysers loaded so `BOR0201` carries the plan's no-floating-point prerequisite as a build
      error. Findings: **the ~30,000-Segment placeholder is one Street per Cell boundary**, and at that
      density the mean Segment is 128 m — two statements in `CONTEXT.md` → Segment turn out to be one;
      **the Road Graph is not a memory constraint** at 2.0 MiB against K0's 172.3 MiB; the
      `(Segment, offset)` query shape costs ~250 ns against a 418 µs search, so **the shape the corpus
      committed to is free**; and **admissibility breaks at the first Arterial** — Manhattan returns a
      different route on 4% of drives with two Arterials on the map, which under `05 §4` is a different
      city. **`Chebyshev` is the heuristic**, beating the tighter `EuclideanFloor` by 1.8× because an
      exact integer square root costs more than the expansions it saves — a case where *nodes expanded*
      picks the wrong rung, which R3 must not repeat. Three harness defects recorded, one of which hid
      a graph with no Arterials in it behind four healthy-looking tables
- [x] **`adr/0040`** — the pathfinding cluster is a multiple of the Chunk, not the Chunk
- [x] **`adr/0041`** — volume is attributed by the Traveller, not the District pair
- [x] **Session nine — `06-roadmap.md`, and what a planning document may assert.** Taken **out of the
      board's order**, and legitimately: K was one blocked half and one runnable half filed as one.
      **K1 is done** — every claim a settled decision falsified is struck, not corrected. `06` lost its
      contents column entirely and its milestone rows are now **name plus risk retired**; Phase 0/1
      order points at `0003`, status at this board, mechanism at the design documents. It gained a
      table of **seventeen mechanisms with no milestone** — Money, Hinterlands, Office and the labour
      system, Density, Services, Crime, the nine Resources, Upkeep, Policy and the Sweep Rule family,
      transit, Taste, and more — and a short list of **instructions ADRs addressed to it and nobody
      executed**. Phase 2's *"the city is alive"* is replaced by what those ten milestones would
      actually produce: a transport and housing simulation with **no money in it, nobody employed, and
      no way for anyone to arrive**. Produced **`adr/0042`** — a planning document cites, a design
      document owns. **K2, the ordering, remains and stays last**
- [x] **Session B — `02 §4` residue, and the gate it moved rather than cleared.** Run beside slice 6.
      All four named items closed: **cycle checking handed to `adr/0015`** (the `on_fail` graph is
      static, so it is Ruleset validation and not a runtime guard); **no depth cap**, because the
      ladder bounds it structurally and the number is *measurable* and routed to slice 7;
      **`mean_workforce_experience` struck**, with experience folded into the labour Bin as a
      per-worker deposit multiplier; and **a predicate may read any declared Readout**. Produced
      **`adr/0045`** — a fallback chain is a source ladder over one Bin, so a failed chain
      **subscribes once at its head**, an asynchronous link declares what it `fills`, and a
      malformed chain is **refused** at load. Four findings the session was not looking for: the
      corpus's **worked example polled forever**, because `mark_input_starved` succeeds and a success
      re-arms the head — `adr/0033`'s polling defect reproduced by the subscription model's own
      example; **`02 §4.1`'s Readout bound pointed at a non-set**, since `02 §9` is an obligation to
      *expand* aggregates and contains no enumeration, so the bound is **inverted** and the Readout
      set is declared simulation-side, which also removes slice 7's accidental dependency on a
      presentation design that does not exist; **apply count is authored per Rule**, greedy or fixed
      with `min = max` as the fixed spelling, because `adr/0035`'s Upkeep must never draw more just
      because the treasury is full; and **the cost driver under subscription is shortage *churn***,
      not depth and not brokenness — which sharpens `adr/0033` from *most expensive when most broken*
      to *most expensive when most unstable*. **A draft of `adr/0045` published a depth cap of 5 and
      it was withdrawn**: R3's tripwire rule was written down and had not been run

---

## Unblocked, in order

### Main track — code

- [x] ~~**Slice 6 — Map Layers**~~ — [`0009`](0009-map-layers.md). **Closed.** See *Done*
- [ ] *the Phase 1 gate closes here* — **nothing in the code column stands in front of it**
- [ ] **S0** — the synthetic 1M-Citizen city. Unblocked the moment slice 6 lands, and **the corpus
      forbids opening Phase 2 content until it has run**
- [ ] Slices 7–10 — each behind a session in the argument track above, not behind code

### Parallel track — argument ([the table above](#the-argument-track--what-stands-between-here-and-phase-2))

- [ ] **A** — `adr/0015`, hot reload, **now carrying `adr/0045`'s two refusals and the TOML exception**
      · ~~**B** — `02 §4` residue~~ *(closed)* · **C** — `02 §7` + `adr/0006`
- [ ] **D** — `03 §5`, the traffic model *(more than one sitting)*
- [ ] **E**–**I** — the six research-written ADRs *(`0005`, `0007`, `0008`, `0009`, `0012`, `0016`)*
- [ ] **J** — save/load's three: `05 §7`'s format half, map size, Outside Connection layout
- [ ] **L** — write a presentation design, then grill it. Blocked on S1 and S3
- [ ] **M** — the route cache's invalidation contract. **NEW, forced by S2 R5.4**, gates **R6**, and
      carries `adr/0012`'s owed amendment. Two of its five candidates are *arguable* and three are
      *measurable* and belong to R6
- [ ] **K2** — re-derive `06`'s Phase 2 ordering, last. *(K1 done — session nine)*

### Parallel track — S2, routing ([`0010`](0010-s2-routing.md))

- [x] **R0 — the synthetic Road Graph, and the denominator.** Done. The density curve, the footprint,
      the `(Segment, offset)` denominator and the admissibility verdict. Numbers in
      [`spike-results`](../docs/spike-results.md)
- [x] **R1 — the travel-time matrix.** Done, and it is the task the prescribed order existed to
      reach. **The matrix carries the choice loop**: 1.14 ns scattered at the 121-District anchor,
      5.00 ns at 4,096, against a tripwire at S4's K2 gather of 13.66 ns — so the wire does not fire at
      any District count and `02 §5.8`'s rule is enforceable. **District count's ceiling is not L3**;
      the cache cliff arrives below the threshold that was supposed to follow from it, and what binds
      instead is the route store (4.06 GiB at 4,096 against a 172.3 MiB world) against the entry error
      (24.70% → 3.80% across the same sweep). **`adr/0020` is owed an amendment on evidence** — 6
      Settlements against Tarjan's 8 at a tight Commute Budget. **The volume-scope axis R0 was
      forbidden to settle turns out to be the `adr/0020` exposure itself**: per-Segment volume makes
      the matrix symmetric to the bit, which makes union-find right by construction and Stress blind to
      a directional peak, for a 5% saving on a structure that is 1.2% of the world. **`02 §6`'s
      dirty-region rebuild is unsound**, missing 309 of 429 changed entries on a central edit, and the
      sound alternative collapses into a full rebuild because a one-to-all fills a row. Two findings
      the plan never asked for: the **entry error** against a true query (11.32% at the anchor) and
      **time resolution** as a hash-bearing decision — a Day-average matrix reports 1 one-way District
      pair where the morning peak has 76
- [x] **R2 — searched against looked-up path, and the crossover.** Done, and it **revived the task it
      was supposed to retire**. The path source has **three** rungs, not two: `adr/0041` needs a *next
      Segment* every Tick rather than a *path*, so a **next-hop table** — distance-vector's own data
      structure — is a legitimate rung, and it is the better of the two survivors on error at
      **18.52%** mean detour against a shared route's **36.01%**. **The searched rung is out on
      arithmetic**, 716,800 ns per Leg against ~550 arrivals per Tick. **`adr/0041`'s *"no correctness
      content"* is wrong on two counts** — the detour, and the structural finding that *every* Trip
      into a District arrives through its one representative node, driving that node to **412%** `v/c`
      where searched routes give **130%**. **Direct attribution is the cheaper scheme below a 105-Tick
      cycle**, an order of magnitude past the ADR's estimate of ~10. **The aggregate scheme does not lag
      the jam, it misses it** — *never* at every cycle including one Tick, 0.00% deposited on a Segment
      carrying 108%; the two schemes agree on how many Segments are stressed and disagree on which.
      **The crossing rate is 0.79–0.83, not 1.0.** And a volume-conservation check caught a harness
      defect that had published a `v/c` of 883× with every other column looking healthy
- [x] **R3 — HPA\*, and the cluster size it owns.** Done, and it **weakened the option it was
      expected to confirm**. **Cluster size narrows to 8 or 16 Chunks, the bias on 16, and R3
      cannot close it** — 16 is 1.31× faster on the refined query and costs 0.92 ms more per deleted
      Segment, a per-Tick cost against a per-click one, and the edit rate that weighs them is R5's.
      `plans/0010`'s *decides cluster size, outright* is owed the
      correction — and `adr/0014`'s *"the Chunk grid is already the pathfinding cluster"* is **measured
      false by 256× in area**, because at one Chunk the abstract graph *is* the Road Graph (16,694
      portals against 16,697 nodes, expanding exactly the flat search's 4,138). **HPA\* buys 3.08×
      cost-only and 2.63× with arcs**, against a cost-only customer R1 already serves at 1.14 ns.
      **The transitive reduction is mandatory and lossless** — 133,816 abstract edges to 11,768,
      degree 40 to 3, 100% optimal throughout — and skipping it is what made the hierarchy read
      1.43×. **Storing each intra-edge's arcs is mandatory alongside it**, worth 1.50× → 2.63× on the
      refined query for 223.92 KiB. **Transition sampling is out** at 80.49% mean detour. **HPA\*
      wins the correctness column outright**: 100% optimal against R2's 18.52% and 36.01%.
      Preprocessing is 201 flat searches and a deleted Segment costs **1.30 ms**, because a reduced
      cluster's edge set must be **decided again rather than re-costed** — measured, not derived.
      **R0's amendment landed twice more** — once on the hierarchy, once on the denominator,
      which read 1,401,307 ns measured first and 477,609 ns measured last
- [x] **R4 — DSDV distance-vector.** Done, and it **retired the candidate it revived** while
      producing a larger finding it was not looking for. **Distance-vector is out on none of the
      three grounds anticipated**: not memory (**23.12 MiB** at the anchor against a 172.27 MiB
      world — the tripwire is measured and does not fire), not correctness (with sequence numbers it
      converges to *exactly* the rebuilt table, on a deleted Segment and on a severance alike), but
      **cost** — **500.69 ms against a 234.74 ms rebuild, 2.13× slower**. The reason is structural:
      an odd-sequence poison outranks every finite route in circulation by construction, so only a
      newer **even** number from the destination restores one, and **one broken link re-floods the
      whole tree**. **The winner was not on the ballot** — dynamic subtree repair at **4.71 ms**,
      49.76× the rebuild, 0 entries wrong. **`references.md`'s sequence-number claim is confirmed by
      measurement** at 1,620× the work and 16,684 of 16,697 entries still wrong without them.
      **R4.8 is the largest finding**: R2's 18.52% detour is a property of the uniform draw, and on a
      local-trip distribution it is **128.82%**. **The O-D draw is now a swept family**, discharging
      a debt four tasks old. **Congestion drift is priced** — the incremental/rebuild break-even is
      between **1% and 10% of arcs moved**, so the matrix refresh cadence *chooses the maintenance
      scheme*. Four harness defects, one of which read **232 s** per edit and would have published
      *distance-vector loses by three orders of magnitude* — caught by R2's *two measurements that
      agree that closely are not two measurements*
- [~] **R5 — the edit storm, and the Epoch ladder. R5.1–R5.5 DONE; R5.6 open.** It measured
      the **gesture** rather than the edit — the unit R3 and R4 both said they could not reach — and
      **fired a tripwire**: a single-counter Epoch *is* a global flush, which is *"out on a design
      commitment, not on a number"*, and it has the number too — against a no-edit ceiling of 71.63%,
      **per-Segment retains 96% under a continuous storm and global retains 9%**. **The ladder's own
      framing was wrong**: this plan priced it as *hit rate against revalidation cost* and per-Segment
      is cheaper *and* more precise at every edit rate, because a revalidation word is arithmetic and
      what it avoids is a search — **no rung on it trades accuracy for speed**. **Cluster size closes
      against R3's bias**: 8 Chunks is ~2× cheaper than 16 on a coalesced 256-Segment drag, so R3's
      *current standing favours 16* is withdrawn. **The repair loop R3 and R4 both wrote is a
      catastrophe on a gesture** — per-Segment rather than per touched cluster costs **23.26×**, a
      worst case of **253.22 ms, sixteen Tick budgets, from one drag** — and the two spellings are
      identical at a gesture of one, which is the only size either measured. **Repair loses to a full
      rebuild** above ~63 clusters touched. **R5.4 measured the *addition* — a section the plan did
      not have — and found no rung both affordable and correct across the whole core verb.**
      **R5.5 then decided the path source, and the answer is that it is not one choice.** A
      maintained next-hop table and a route cache are wrong in **different currencies** — structural,
      fixed and visible (**16.58%** uniform detour, **149.73%** local, unmoved across a storm
      deleting 1,021 Segments) against temporal, near-zero and **permanent**. **The shared District
      route is retired on a number**: ~180 ms per gesture *flat in gesture size*, because a rebuild
      does not care what was deleted. **R5.5.4 measured the way out of R5.4's hole** — a TTL rotation
      at **0.40 forced refreshes per Tick** clears the wrongly-valid count **38 → 0 within one
      rotation while retaining 97.08%** of the cache, against a control that plateaus at **23** and
      never moves again, which is R5.4's *does not heal* measured rather than argued. **Tripwire 4
      fired negative**: R5.2's 23.26× does **not** generalise (0.91–1.51× here), so it is a property
      of a cluster's edge set and not a corpus-wide API rule. **Still open: R5.6, the Parking Shed.**
      **The canonical capture is done — three of them**
- [ ] **R6 — the two caches, and `adr/0006`. PROMOTED by R3 to load-bearing.** No cluster size fits
      routing into the Tick budget (85 Trip starts at the best rung), and a cache is one of only two
      exits — the other being to spend eight cores' whole Tick budget on routing. R6 stops being an
      optimisation measured after the router choice and becomes a condition that choice depends on.
      It inherits a partial answer: R3's stored path arena already caches the intra-cluster half
- [x] **R8 — the congestion loop, and the three layers. DONE, all seven sections.** It closed the
      loop S2 had never closed — every earlier task routed over a cost array computed once, so nothing
      in this spike had ever invalidated a route because a road got **busy**. **The load-bearing
      question is answered: `03 §3.4`'s self-correction closes with only the local layers reading the
      VDF** — under a *sustained* demand asymmetry, Sight settles **42.62% below** a control with
      identical physics and no ability to respond, and the control settles *above* its own pre-surge
      level while Sight settles below it. **So static Habit survives as the null hypothesis** and the
      whole maintenance question stays shut: no refresh cadence, no hash-bearing number, and R4.6's
      break-even does not select an algorithm after all. **The Sight Horizon's floor is 1 Segment**,
      derived from the graph with no traffic — 98.02% of arrivals are already at a node with a real
      choice. **Temperament damps by 92.28%** where a herd exists, on an instrument shown able to
      separate a maximal-herd control; the wire stated on *monotonicity* is **REFUTED as written** and
      both readings stand side by side. **A stored route cannot afford Sight** — 3,951% of the Tick
      budget against 3.18%, which is M's third axis. **But the section's largest finding is none of
      those** — see the row below
- [ ] R7 — the report, the verdict, and deleting the harness

### Parallel track — Godot (Track B, no gate)

**These two have a job again.** `06` framed them as gating a commitment to Godot; `adr/0036` took the
core's language out of `adr/0001` and session eight confirmed the host argument, so there is no
decision left for them to gate. They are the **empirical inputs to session L** — a rendering ceiling
and a UI-cost figure — and L is what unblocks Phase 3. Their specifications in `06` were stale by
roughly an order of magnitude and have been struck; size them from `spike-results` and the 1M target.

- [ ] **S1** — chunked `MultiMeshInstance3D` at city scale. *Feeds L*
- [ ] **S3** — one data panel with a live multi-series graph. *Feeds L, and it is **the spike most
      likely to be skipped and most likely to change the decision***

---

## Owed — documentation debt, none of it blocking

Small, and each one is a place the corpus currently says something known to be wrong.

- [ ] **`03 §3.3`, `§3.4`, `§3.6` — joint rewrite**, owed by `adr/0041` and now carrying R2's
      evidence. The District-pair counter goes; the circularity argument becomes structural;
      **force-promotion must stand on its own second argument or go** — and R2 removed the last
      support for the first: `§3.3` confessed a *lag* and compensated for it, but the defect is that
      the smear reports the jam **in the wrong place**, which no cadence and no second trigger fixes
- [ ] **`adr/0012` amendment** — the route cache's **eviction policy** *and* its **key** (`adr/0012`'s
      *"keyed by origin-destination pair"* is ambiguous between nodes² and Buildings²)
- [ ] **`adr/0041` amendment** — owed by S2 R2, on evidence. *"Searched per Trip or shared per
      origin-destination pair is a performance axis with **no correctness content**"* is wrong on two
      counts: a shared route costs **36.01%** mean detour and a next-hop table **18.52%**, and *every*
      Trip into a District arrives through its **one representative node**, whose Stress is then an
      artefact of the partition. **The ADR's substantive claim survives untouched** — experience and
      contribution stay the same list of Segments under every rung — so this amends a sentence, not a
      decision. Its **revisit trigger is also discharged**: the crossing rate is 0.79–0.83, not the
      assumed 1.0, and the crossover sits at 105 Ticks rather than ~10
- [ ] **`adr/0020` amendment** — owed by S2 R1, on evidence. *"A connected component of the District
      graph… a union-find"* is not what `CONTEXT.md` → Settlement defines, and the two disagree about
      the city where the city is fragmenting. Tarjan is still cheap; it is simply not the ADR's claim
- [ ] **`02 §6` correction** — owed by S2 R1. *Slow cadence, dirty regions only* is **unsound**: a
      spatial test misses the long routes that cross an edit without ending near it — 309 of 429
      changed entries on a central edit. It is `CONTEXT.md` → Epoch's *when you pay* / *what survives*
      distinction arriving at the matrix instead of at the cache
- [ ] **"Zone" is used for the travel-time matrix's granularity, which is the District.** `CONTEXT.md`
      → Zone is *a permission set over land*; `CONTEXT.md` → District is *"the granularity of the
      travel-time matrix"*. `05 §422` and `references.md §2` both say *"zone-to-zone travel-time
      matrix"*, and `plans/0010` quoted the second verbatim — so this is a corpus-wide sweep and not a
      one-line fix, and a corrected quote is a broken one. Found by S2 R1, which spells it District
- [ ] **`spike-results`** — the 37k–111k in-flight band conflates duration sensitivity with peaking and
      must be re-derived on both axes
- [x] ~~**S2's timing tables are owed a canonical re-capture**~~ **DISCHARGED session eleven.**
      `sudo spikes/S2.Routing/tools/routing-run.sh` took R0 and R1 together under `performance`, turbo
      enabled, pinned to one physical core; `docs/spike-results.md` now quotes that capture throughout
      and the `powersave` run is retained beside it. **Captured twice, fourteen minutes apart, under
      the identical configuration** — so the nanosecond columns now carry a measured error bar rather
      than a disclaimer: drive-search absolutes reproduce within 2%, and the one DRAM-resident read
      within 12% — the exposure S4 already named for this machine — while a bootstrap recovered by
      difference between two loops reaches 29%. **Every count is bit-identical
      across all three captures**, which is the determinism check nobody had run. The tripwire column
      reads **0.36×** against a wire at 1.00×
- [x] ~~**Why plain Dijkstra's absolute moved 1.64× under pinning**~~ **ANSWERED by S2 R5's canonical
      capture, and the hypothesis was right.** Driving `None` went 779,150 ns unpinned → 1,278,071
      pinned, reproducing to 0.2%, while `Chebyshev` moved 0.04% across the same change. The
      hypothesis on file was *`taskset` leaves one visible logical processor and tiered-JIT
      background compilation now shares the measured core, which lands on whatever is timed first*,
      with the check named as *re-run the ladder in reverse, or with tiering disabled*. **Neither was
      needed**: R5 takes its denominator twice, which makes the artefact visible inside a single
      capture — 4.88× apart at one processor, 0.92× at two — and pinning to both threads of the core
      removes it. **The first-timed row of any S2 table taken before this fix is still the least
      trustworthy number in it**, which is now a re-capture task rather than a caveat. It already
      cost one claim — R0's *"`EuclideanFloor` is not faster than Dijkstra at all"* was true of the
      unpinned capture and stays struck
- [ ] **`05`** — strike the ~400k Trips/Day figure, known wrong and still standing in the authoritative
      document
- [ ] **`05 §3`** — Parking Shed invalidation needs the *when you pay / what survives* correction
      `CONTEXT.md` → Epoch has taken
- [x] ~~**`06`** — the S2 specification (*"30k Travellers"*) and S1's (*"20k Buildings"*) are stale~~
      **DISCHARGED session nine** by deletion rather than correction, per `adr/0042`: `06` no longer
      carries spike specifications at all. `0003` and `spike-results` own them
- [ ] **`adr/0012`, and two other filenames, use "Agent"** — banned outright by `CONTEXT.md`. 33
      occurrences across 22 files

## Owed — findings that change a later task

- [x] **R3 must not quote HPA\* in expansions saved.** *Discharged, and the warning was load-bearing:*
      the hierarchy expands **4.7× fewer** nodes and is **1.44×** faster unreduced, because a road
      network is degree-3 and the complete abstraction is degree-40. Quoted in expansions it would
      have read as a large win. **R6 still inherits the instruction.** Original wording: R0 measured a case where the currency does not
      convert: `EuclideanFloor` expands **11% fewer** nodes than `Chebyshev` and takes **1.8× as
      long**, and against plain Dijkstra it cuts expansions by 55% while being no faster at all. The
      cost is its exact integer square root, run twice per node pushed. `plans/0010`'s ladder specified
      nodes expanded, path cost and optimality; **adding a clock is R0's amendment to the plan**, and
      R3 and R6 inherit it — a hierarchy or a cache that saves expansions has not yet saved anything
- [ ] **An artefact that varies with the swept axis is not distinguishable from a result.** R1 needed
      **four** warm-up schemes before its cold-build column stopped falling smoothly with District
      count — which is precisely the shape a reader hopes a sweep will discover, and was the process
      leaving tier 0. Ruling out the per-rung explanations is what identified it as per-process:
      `OneToAll.Run` is called once per District, so the small rungs never call it enough. **R3 and R5
      sweep cluster size and edit rate and are exposed to the same failure**; only a warm pass over the
      whole sweep removes it. This is R0's *"the bootstrap column was mostly the sampler"* in its
      general form, and it is the second time in S2
- [ ] **A sample that shrinks with the swept axis manufactures a trend out of survivorship.** R1's
      entry-error section first drew Access Points uniformly and rejected those outside the named
      District; at 1,024 Districts a hit is one draw in a thousand, so the sample silently collapsed to
      **nine searches** and was printed beside rows built from 2,244. Third instance of the corpus's
      recurring shape, after R0.5's *mean cost when found* and R0's dead Arterials. **Any later section
      that samples inside a swept partition must report its sample size per rung**
- [ ] **An invariant is worth printing on the run where it reads *yes*, because it is worthless on
      the run where nobody printed it.** R2's next-hop rung tested arrival *after* entering the last
      arc, so an arriving Traveller was respawned without decrementing — `adr/0041`'s named
      `adr/0006`-class defect, *"a road that looks busy forever."* It published a peak `v/c` of
      **883×** while the footprint, the crossing rate, the detour and the crossover columns all looked
      healthy. The ADR had already specified the invariant that catches it — *summed Segment volume
      equals in-flight Travellers, every Tick* — and it found the bug on the first run it was printed.
      **Fourth instance in this spike of R0's *"an argument for reporting a quantity you expect to be
      boring"*, and the first where the corpus had written the check down in advance and the harness
      had simply not run it.** R3, R5 and R6 all mutate state a conservation law covers
- [ ] **Two measurements that agree to the last digit are not two measurements.** R2's shared and
      next-hop rungs reported **byte-identical** peaks, because the next-hop fleet was being spawned
      *at* the origin District's representative — which made it walk the shared route. The rung's whole
      claim is that it is followed from wherever the Traveller actually is, and the experiment had
      quietly removed the difference it existed to measure. **Nothing but the identical digits gave it
      away.** R3 compares a hierarchy against a flat search over the same graph and is exposed to the
      same class
- [ ] **An error rate that moves with an unrelated optimisation is not evidence.** R0's heuristic
      multiplies by a floored reciprocal rather than dividing, to remove four hardware divisions per
      node. The reciprocal's ~2-in-10,000 slack **partially cancels an overestimating metric's error**:
      the same change moved walking `Manhattan` from 35 of 300 non-optimal to 4 of 300, worst exactly
      where `adr/0008`'s walk Legs live. **Any later measurement of an error rate — R2b's attribution
      lag, R5's hit rate — should ask what else in the pipeline rounds in the same direction**
- [ ] **A denominator measured once has no error bar, and a denominator measured first has a
      systematic one.** R3's first pinned capture read **1,401,307 ns** for the flat search and
      **477,609 ns** for the same code measured after the sweep — a 193% spread — because the flat
      loop was the first timed thing in the process and the clock had not ramped. **Every ratio R3
      publishes divides by that number**, so the artefact would have decorated the whole task rather
      than one column. The harness now measures it twice and publishes both. **R4, R5 and R6 all
      divide by the same denominator** and must do the same. Fifth instance in S2 of R0's *"an
      argument for reporting a quantity you expect to be boring"*, and the first where the boring
      quantity was the denominator
- [ ] **A correctness column that cannot move is not evidence that the error is absent.** R3's detour
      read 0.00% at every cluster rung, which is the shape R2's byte-identical peaks wore. It is real
      — the abstraction is complete, so it cannot lose a route — but that was established by making
      the instrument move: **sampling transitions drove the same column to 80.49%**. A zero should be
      paired with a rung that is expected to be non-zero, or it is indistinguishable from an
      instrument that is not wired up
- [ ] **Nothing in the corpus invalidates a route when congestion changes, and R3 is the first task
      with a large number attached to that.** The Epoch bumps on an *edit*; the VDF makes travel time
      a function of `volume / capacity`; `adr/0041` moves volume **every Tick**. A flat search reads
      arc costs at query time and is always current — a structural advantage of the denominator R3
      never priced — while **every precomputed structure in S2 is stale the Tick after it is built**:
      HPA\*'s intra-cluster edges, R2's next-hop table, and R1's matrix alike. **Third invalidation
      mechanism in the corpus and the first with none at all**, after R1.7's dirty region and the
      scalar Epoch. **R5 must be given the refresh cadence of the routing cost basis before its edit
      storm means anything**; if that cadence is the time-of-day phase R1.8 found, the exposure
      evaporates, and if it is per-Tick both surviving routers are dead against a 15.6 ms budget
- [ ] **A tripwire should be gathered as direct data, and where it cannot be, the derivation should
      be inverted until what is published is measured.** R3's Tick-budget row was first drafted as
      *routing is 6.4× over budget*, which multiplies a measured per-route cost by **550 Trip starts
      per Tick** — a figure resting on a mean Trip duration the corpus calls provisional, in a spike
      with no Travellers and no Trip generation to improve it. **A wire whose denominator is a guess
      fires on the guess.** Published the other way round — *routing fits while fewer than 85 Trips
      start per Tick* — the quantity is a measured cost over a world constant and survives the arrival
      rate being measured elsewhere. **R4, R5 and R6 each have a row that can be inverted the same
      way**, and `plans/0010`'s tripwire section now carries the rule
- [x] ~~**S2's O-D draw is uniform over the map, and R0 flagged that as a placeholder that was never
      replaced.**~~ **DISCHARGED as a debt and reopened as an axis, by R4.1.** The draw is now a
      **swept family** — uniform, distance-decay at L = 1024/512/256 Tiles, monocentric — with
      uniform as a rung of the same sampler, so a difference between rows is the shape and cannot be
      the machinery. It cost a conclusion to find: R2's **18.52%** detour is 20.14% on uniform and
      **128.82%** on the tightest decay rung, because uniform is the **longest-trip** distribution
      available (8.53 km mean on a 16.4 km map) and a District-representative detour is a fixed error
      against a shrinking journey. **R3's speedups remain an upper bound and are now quantifiably
      so.** What is *not* discharged is the underlying absence: the family is invented, and only Trip
      generation can replace it — filed as `plans/0010` decision 14, and **no document may cite an
      S2 figure derived from it without naming the rung**. Original wording:
      **S2's O-D draw is uniform over the map, and R0 flagged that as a placeholder that was never
      replaced.** R0 said it could not have the distribution it was supposed to use and would take
      R1's; R1 produced none, and R3 inherited the uniform draw unchanged. **A uniform draw over 4,096
      Tiles produces long routes, and long routes are where a hierarchy wins widest** — so R3's
      speedups are an upper bound. It does not move the optimality counts, which are counts, and it
      does not move the ranking of the cluster rungs against each other. **R4 inherits the same draw**
      and a next-hop table's error profile is distance-dependent too, so the comparison is on the same
      footing but both sides are measured on a distribution nobody has confirmed. **R3.8's bypass
      table is the only evidence in the spike about the short end**, and it says the cliff is at one
      block
- [ ] **Two S2 tasks publish different absolutes for the same operation.** R2 reports 474.47 ms to
      build the 121-column next-hop table; R4 reports **234.74 ms** for the identical rebuild. R4
      substantially explains it rather than resolving it: **the same 121 backward Dijkstras read
      423.47 ms measured first in R4's own process and 234.74 ms measured later** — 1.80× apart,
      which is **R3's *a denominator measured first has a systematic error* reproducing a third
      time**, and R2's figure was also first-timed. Every R4 ratio is taken in-process against R4's
      own figure, so no conclusion moves. **R7 owes the reconciliation**
- [x] ~~**The canonical `performance` capture of R4 is owed**~~ **DISCHARGED.** Captured as root
      under `performance`, pinned, turbo enabled; 23.69 s, CPU stall 1.41%, memory stall 0.00%.
      `docs/spike-results.md` §S2 R4 now quotes it throughout, and the `powersave` run is retained
      beside it. **Against that run every non-timing row is bit-identical** — relaxations, rounds,
      wrong-entry counts, stranded counts, footprints, detour percentages and the whole O-D table —
      and only nanosecond columns move, by 2–7%. **The governor moved nothing R4 concludes from.**
      The scheme ranking, the sign of every comparison and the break-even band are all unchanged;
      dynamic repair's margin over a rebuild on one deletion widened from 37.99× to **49.76×**
- [ ] **`plans/0010` R3's *current standing favours HPA\* at 16 Chunks* is withdrawn — NEW, produced
      by S2 R5.2**, and it is a correction R3 asked for in advance. R3 narrowed cluster size to 8 or
      16, put the bias on 16 on a 1.31× faster refined query, and said the axis that separates them
      is an edit rate R5 owns. **8 is 1.9× cheaper on a coalesced 256-Segment drag, 5.8× on the naive
      worst case, and its full rebuild is 43.14 ms against 75.31 ms.** A 1.31× query advantage
      against a ~2× edit penalty picks 8, and **the canonical capture confirms it** — the one run
      that said otherwise is the mis-pinned one. **Conditional on R5.6**, which may rank a Parking Shed
      differently, so the sweep is not deleted
- [ ] **`HpaSearch` cannot see a Segment deleted under a Trip's own feet — NEW, found by S2 R5.5, and
      it is pre-existing.** The forward seed and goal remainders call `CostToEndpoint(graph, null, …)`,
      and a **null** cost array reads `graph.ArcCarTicks` — the pristine array — while the storm
      deletes into a shadow clone. **So the hierarchy returns a route down a road the player has just
      bulldozed.** `flat` found **416** unroutable over R5.5's sweep where four cache rungs found
      **16**. It is **common-mode across all three Epoch rungs**, so no R5.3 or R5.4 conclusion moves
      — but ***Unroutable* on any hierarchical row is evidence of nothing**, and **R6 must fix it
      before it caches anything.** Not repaired, because repairing it re-baselines R5.3
- [ ] **A per-edit repair API invites the loop that destroys it — NEW, produced by S2 R5.2**, and it
      is a shape rather than a number. **R5.5 tested it for generality and it does not have any**:
      looping `RepairSubtree` per Segment over a drag costs **0.91–1.51×**, not 23.26×, because a
      shortest-path subtree is *repaired* from the boundary inward where a cluster's edge set must be
      *decided* whole. **The finding is real and local to `AbstractGraph`**, not a corpus-wide rule. `RebuildFor(segment)` is the natural signature and looping it
      over a drag re-decides the same few clusters dozens of times, for **23.26×** the coalesced cost
      and a worst case of **253.22 ms**. The two spellings are **identical at a gesture of one**,
      which is the only size R3 and R4 ever measured, so no earlier task could have caught it.
      **Anything the player can do to hundreds of objects at once needs its API shaped for the
      gesture**, not for the object — and above ~63 clusters touched the repair loses to a **full
      rebuild** outright, so the path has two thresholds rather than none
- [ ] **No Epoch rung is both affordable and correct across the whole core verb — NEW, MEASURED by
      S2 R5.4**, which was not a task the plan had. Deletion is monotone-**worsening**, so a rung
      watching a route's own Segments misses nothing and per-Segment is exact. Addition is
      monotone-**improving**, and **a route computed before a road existed cannot contain it**, so
      per-Segment declares **100.00%** of the cache valid and structurally cannot notice — and
      **per-cluster fails the same way**, since a new fast link in a cluster the route never enters
      still beats it. **Only global is sound under addition, and R5.3 measured global as unusable.**
      Sized rather than argued: restoring **4 Arterial Segments — ~512 m, the smallest addition worth
      drawing** — leaves per-Segment serving stale routes on **9.22%** of resident entries at a mean
      **16.71%** detour and a worst of **62.65%**, against R2's 18.52% which the corpus treats as a
      serious correctness finding. **It is a floor, and unlike every other error in this spike it
      does not heal**: nothing the rung watches will ever move again, so only **eviction** removes
      it — and `adr/0012` keys by O-D **rather than by agent**, so it is every driver's route and a
      hot pair is the *least* likely to be evicted. **Five ways out are tabulated in
      [`spike-results`](../docs/spike-results.md) → R5.4**, and two of them are corpus decisions
      rather than engineering: **B** (weaken the contract to feasibility — which fits `BOUNDED
      KNOWLEDGE` if the ignorance is *modelled* and is a defect if it is *accidental*), and **E**
      (R1's matrix as an O(1) detector — **the relationship R1 explicitly declined to argue**,
      arriving from the other side). **Addition is measurable after all**, which R3 had thought it
      was not: build the abstract graph on the full graph so every portal slot is reserved, then
      delete a set and restore it
- [ ] **The synthetic grid cannot answer the Street half of that question — NEW, produced by S2 R5.4.**
      Restoring ordinary Street improved **0.00%** of cached routes at every size up to 126 Segments,
      because one Street per Cell boundary at a uniform speed gives very many *equal-cost* shortest
      paths: deleting a line leaves an equal-cost alternative one block over, so the cached cost never
      moved. **The zero is real and does not generalise** — a real network has heterogeneous speeds
      and far fewer ties. It is the same debt `CONTEXT.md` → Segment already carries from R0: **road
      density has a curve and no source**, and now so does road *homogeneity*. **The Arterial side is
      thin for the same reason**: the graph holds **8 Arterials and 104 Arterial Segments**, and an
      Arterial-only drag saturates at **4** — which is why R5.4 publishes one Arterial rung rather
      than a sweep, and why its 16.71% is stated as a **floor** rather than a curve
- [ ] **R5's cache hit rates rest on an invented pool, and no document may quote the level — NEW,
      produced by S2 R5.3.** A route cache works because real Trips **repeat**, and nothing in S2 can
      produce that recurrence because it needs Trip generation. R5 substitutes a fixed pool of 512
      O-D pairs sampled with repetition; drawing fresh pairs every Tick would report ~0% for every
      rung and compare nothing. **So every absolute hit rate in R5.3 is a property of the pool
      size.** What the pool cannot distort is the *ratio between rungs under the same pool*, which is
      what the ladder is for. **Exactly the handling R4.1's O-D family already has** — and that debt
      is discharged as an *axis* rather than closed, so this one should be too: **R6 must sweep pool
      reuse rate the way R4.1 swept trip length**, or its hit-rate curve is one guess wearing a
      measurement's clothes
- [ ] **`adr/0017`'s eviction pattern has a number for the first time — NEW, produced by S2 R5.3 and
      not looked for.** R5's miss column sits at **28–31% and does not move with edit rate at all**,
      which is the tell that it is collisions rather than staleness: **a direct-mapped route cache at
      2× over-provisioning loses about three lookups in ten before a single road is touched.** The
      decision belongs to **R6**, which owns eviction and the key; what R5 supplies is evidence that
      the policy is worth more than the Epoch rung below the highest edit rates
- [ ] **A mean per-route cost times an arrival rate does not bound a Tick — NEW, produced by S2 R5.3.**
      R3 published *routing fits while fewer than 85 Trips start per Tick*, derived from a mean. At
      **16** Trip starts R5 already measures a worst Tick of **10.37 ms** against a 15.6 ms budget.
      **S4's K6 said it first** — a run whose worst iteration was 100.2 ms read 2.462 ms at p99.9 —
      and R6 inherits the instruction along with R3's
- [x] ~~**The canonical `performance` capture of R5 is owed**~~ **DISCHARGED**, and taking it found a
      defect in the capture protocol itself. `spike-results` → *S2 R5* quotes the pinned run
      throughout and the unpinned `powersave` capture is retained beside it. **Every count, share and
      percentage is bit-identical across the two**, including R5.4's whole addition table, so no
      conclusion moved. Three absolutes did: the repair loop reads **23.26× and a 253.22 ms worst
      gesture** rather than 21.25× / 219.50 ms, and the worst Tick reads **10.37 ms** rather than
      13.26 ms
- [ ] **Every earlier `performance` capture in S2 carries a one-processor artefact — NEW, produced by
      R5's canonical capture.** `routing-run.sh` pinned with `taskset -c 2`, one logical processor
      with the SMT sibling idle, which starves the .NET tiered JIT's background compilation of
      anywhere to run but the measured core. Measured **within one capture** by the twice-taken
      denominator: **214.94 ms first against 43.99 ms last, 4.88× apart**, against **0.92×** under
      `-c 2,8`. It inflated R5.2's first-timed half by ~3× and **flipped the 8-versus-16 verdict**.
      **R0, R1, R3 and R4 were all captured this way**, session eleven's canonical re-capture
      included. Counts and in-process ratios are unaffected; **first-timed absolutes are not**, and
      R3's 1,401,307 ns / 477,609 ns denominator spread is very likely this rather than a cold clock.
      The harness is fixed — it reads `thread_siblings_list` and pins to both threads. **R7 owes the
      re-capture**, and it is cheap
- [ ] **A published absolute with no artefact behind it is not a measurement — NEW.** Six figures in
      R5's first write-up existed in no file under `results/`, because the harness wrote every run to
      one filename keyed on machine configuration alone: a `--storm` run displaced a whole-run
      capture, and a later `--storm` displaced that. **Fixed** — every capture is now named by
      section, CPU set and capture time and nothing is ever overwritten, and the retained captures
      have been renamed to the same scheme, which is what makes the artefact above visible in an
      `ls`. **The generalisation is owed a home**: this is *an argument for reporting a quantity you
      expect to be boring* arriving at the **retention** layer rather than the measurement one, and
      S4 and every later spike write to the same kind of directory
- [ ] **The canonical `performance` capture of R3 is owed, and it is now owed twice over.** The
      published figures come from a `powersave` capture, and it was pinned the **wrong** way —
      `taskset -c 2`, one logical processor — so it carries the artefact the entry above describes on
      top of the governor. Every *count* is configuration-independent and every ratio is taken within
      one process, so no R3 decision rests on it; **no absolute nanosecond figure should be quoted
      outside the section until it exists.** The harness pins correctly now:
      `sudo spikes/S2.Routing/tools/routing-run.sh --cluster`
- [ ] **Slice 7 ships two counters, and its tripwire is stated over cost rather than over depth.**
      Owed by `adr/0045`. **Rule evaluations per Tick**, and **walked chain depth** — the second
      already required by `02 §9`'s *"which fallback chain it walked and where it terminated"*.
      Published the R3 way: *chain walking fits while fewer than N evaluations occur per Tick*, never
      as a multiple over a guessed denominator. **The instrument is specified before the number is
      needed**, which is the shape task 7's Census owed and did not have. **Churn is the third
      thing to count**: a chain is walked once on entry into shortage, so supplied/short boundary
      crossings are the cost driver — and **greedy apply maximises them**, which makes burstiness a
      bill a designer chose rather than one the engine imposed
- [ ] **A helper is only as safe as the largest quantity anybody has yet asked it to measure.** R4's
      elapsed-time helper computed `elapsed × 1,000,000,000`, which passes `long.MaxValue` at about
      **9.2 seconds** on a nanosecond clock — and R4 measured a rung that took four minutes, which it
      published as **−8,267.51 ms**. The identical expression is correct in every earlier S2 section
      because every earlier section timed loops far below the threshold. **R5, R6 and R7 use the same
      harness**, and any of them may time a storm rather than a loop
- [ ] **A phase that does nothing does it very quickly, and reports success.** R4's DSDV poison phase
      was seeded with the nodes that *detect* a break rather than the nodes they *advertise to* — and
      since a correctly-implemented node rejects stale claims, nothing changed, nothing propagated,
      and the phase returned **converged: yes** after 2 rounds and 24 relaxations while leaving
      16,680 of 16,697 entries wrong. **A convergence flag is not a correctness check**, and the only
      reason this was caught is that a separate audit column existed to disagree with it
- [ ] **A defect that produces a plausible number is worse than one that produces an absurd one.**
      R4's audit counted the destination itself as unreachable, one phantom per column, which
      presented as a suspiciously round **121** — a number a reader could have rationalised. R2's
      883× `v/c` announced itself; this did not
- [ ] **The long-run trend assertion is owed by slice 7, and the instrument for it now exists.**
      *Decided:* task 7 shipped the Census and `series(metric, window)` and deliberately did not ship
      the assertion. Nothing in the world grows or shrinks yet — no Event Wheel, no Rules, no Trips —
      so *no collection trends upward at steady state* would pass against an empty world and a static
      one equally, and an assertion that cannot fail reads as covered. **Switch it on when slice 7
      gives the world churn**: sample on the trace cadence, take a series per metric over the tail of
      a 100k-Tick run, and fail on a positive trend in `slots` with `live` flat. The `--census` report
      prints the numbers today, and printing them is what makes the vacuity checkable rather than
      argued
- [ ] **The end-of-run tier allocates on the Large Object Heap at scale** — ~544 KB at 100k Citizens,
      ~5.4 MB extrapolated at 1M, Gen2 collections at the top of the measured range. Once per run,
      after the trace is written, so it perturbs nothing today. Fix is a scratch buffer on the
      registry; **do it when S0 shows a real 1M city**, not on an extrapolation

## Owed — decisions, and who owns them

- [x] ~~**Volume attribution's price** — S2 R2a. Decided by `adr/0041`; the cost is still
      unmeasured~~ **MEASURED by S2 R2a, and it is not a price.** Direct attribution costs
      **139,437 ns/Tick** at the derived 56,000 in flight, against an aggregate smear whose
      **crossover is 105 Ticks** at the anchor — so direct is the *cheaper* scheme at any plausible
      congestion cycle, an order of magnitude past `adr/0041`'s estimate of ~10. The ADR's *"we are
      knowingly paying for correctness"* understates its own case
- [~] **The Epoch's granularity — PARTLY SETTLED by S2 R5.3, for the route consumer only.**
      `CONTEXT.md` → Epoch already carries the *when you pay / what survives* distinction and already
      says **S2 settles the granularity by measurement**. It is now measured for routes and the
      answer is **per-Segment**: against a no-edit ceiling of 71.63% it retains **96%** under a
      continuous storm where the single counter retains **9%**, and it does so while being *cheaper*
      — 42 revalidation words a lookup against 0.71, and a lower mean Tick at every edit rate,
      because a revalidation word is arithmetic and what it avoids is a search. **Two things stop
      this closing.** First, per-Segment is exact under *deletion* and **R5.4 has since measured what
      it is under addition, which is worse than "unsound" — it is the worst rung available there**:
      100.00% of the cache declared valid, **9.22%** of entries stale at a mean **16.71%** detour
      from ~512 m of new Arterial, and permanently, because only eviction removes it and `adr/0012`
      keys by O-D rather than by agent. **Per-cluster fails identically; only global is sound, and
      global is the rung R5.3 measured as unusable.** So the answer for routes is *per-Segment plus a
      second mechanism*, and **five candidates are tabulated in
      [`spike-results`](../docs/spike-results.md) → R5.4** — two of which (**B**, weaken the contract
      to feasibility; **E**, R1's matrix as an O(1) detector) are the corpus's decision rather than a
      benchmark's. Second, **the Parking Shed is the other Epoch consumer** — it scales with Buildings
      rather than routes and is a *neighbourhood* rather than a *path*, so per-Segment has no obvious
      meaning for it and per-cluster fits it far better. **`CONTEXT.md` must not be updated until R5.6
      runs**, and a rung chosen on routes alone would be chosen on the cheaper of the two consumers
- [~] **The path source — HALF SETTLED by S2 R5.5, and the other half is not a benchmark's.**
      **Shared District route is retired on a number**: ~180 ms per gesture, flat in gesture size,
      against a next-hop table exact-again in 20.38 ms and answering a Trip in ~1 µs; and it is worse
      on error at every O-D rung. **What R5.5 establishes is that the remaining two are not rungs on
      one ladder.** A maintained table is wrong **structurally** — 16.58% uniform, **149.73%** local,
      and the figure does not move across a storm deleting 1,021 Segments — while a cache is wrong
      **temporally**, near-zero while it lasts and permanent under addition without a rotation. There
      is no measurement that ranks a fixed 16.58% against an occasional 62% that never heals;
      **`05 §4` says a different route is a different city and that is a decision about which city.**
      **Session M owns it**, now with numbers under it. **R5.5.4 removes the false choice** if the
      corpus wants it: a rotation at 0.40 forced refreshes per Tick makes the cache's error bounded,
      which is R5.4's option C measured and option B made legitimate
- [ ] **How coarse a routing destination may be** — **NEW, produced by S2 R4**, and it is decision 11
      and decision 8 arriving a third time. A District-granular route's detour is **20.14%** on the
      uniform draw and **128.82%** on a local-trip draw, because the error is roughly fixed in Ticks
      and the journey is not. The corpus has no position on what a District-granular answer may be
      wrong by. **Answer it once, with the representative funnel and the Commute Budget's
      granularity** — `plans/0010` decision 13
- [ ] **The matrix refresh cadence chooses the maintenance scheme** — **NEW, produced by S2 R4.**
      The incremental/rebuild break-even sits between **1% and 10% of arcs moved per refresh**, so
      below it incremental repair wins and above it a plain rebuild wins outright. A decision the
      corpus files as *tuning* selects an algorithm. `plans/0010` decision 12, and it should be
      settled with decisions 2 and 2a because all three are one argument about one object
- [ ] **The representative funnel** — **NEW, produced by S2 R2**, and nothing in the corpus addresses
      it. Under either surviving rung every Trip into a District passes through one node, so its Stress
      is an artefact of the partition rather than a property of the city. Same defect class `03 §3.9`
      rejects for the Microscopic Cap and `adr/0041` rejects for volume, arriving a third time by a
      different door. Filed as `plans/0010` decision 11
- [ ] **A player-drawn District still changes the city, through the *path source* rather than through
      volume** — **NEW, produced by S2 R2.** `adr/0041` closed this defect for attribution; both
      surviving path-source rungs key on the District, so moving a boundary moves a representative and
      changes the Segments a Traveller drives. `plans/0010` decision 10
- [x] ~~**The `adr/0020` exposure** — union-find computes weak connectivity, *"mutually reachable"* is
      strong~~ **SETTLED by S2 R1, against the ADR.** At a tight Commute Budget union-find returns
      **6 Settlements where Tarjan returns 8**, largest component 90 against 70 — a fifth of the map
      assigned to a Settlement it is not mutually reachable within. **`adr/0020` is owed an
      amendment**; see *Owed — documentation debt*. R1 also found the plan asked for the wrong
      instrument: an asymmetry distribution is a claim about travel times, and the test is whether the
      two algorithms disagree about the **city**. And the exposure is a **band, not a threshold** — the
      one-way pair count rises to 264 and falls back to 47 — so no generous Budget closes it
- [ ] **The travel-time matrix refresh cadence** — filed as tuning, almost certainly hash-bearing
- [ ] **The travel-time matrix's *time resolution*** — **NEW, produced by S2 R1**, and the corpus has
      never named it. A Day-average matrix reports **1** one-way District pair where the morning peak
      has **76**, so the two give the choice loop different answers to the same question and are
      therefore two cities under `05 §4`. Same class as the cadence above and should be settled with it
- [ ] **The Commute Budget's granularity** — **NEW, produced by S2 R1.** A matrix entry is wrong by
      **11.32%** (6.73 Ticks) against a true query at the working District count, and whether that is
      free or disqualifying depends on a granularity nothing states. **R6 is owed the same question
      about its cache key** and the two should be answered once
- [ ] **The sun arc's phase widths** — named in `02 §1.2` and `01 §7`, never sized, so no peaking factor
      exists anywhere. Probably hash-bearing
- [ ] **District count, cluster size, the Epoch's granularity** — all S2's, all swept. **District
      count is no longer an open sweep but an open trade**: R1 found the L3 ceiling everyone expected is
      *not* binding, and what binds is the route store against the entry error. **Road density is
      no longer among them**: R0 swept it and reports **16.20 km/km²** at the ~30,000-Segment rung.
      What is owed is not a sweep but a **source** — whether that density describes a real city — and
      `CONTEXT.md` → Segment keeps its disclaimer until somebody checks it
- [ ] **The cost unit for routing.** R0 routes in **Q16.16 Ticks**, and had to: a Tick is ~10.5
      in-world seconds and a vehicle crosses about one Segment per Tick, so whole-Tick costs make A\*
      minimise **hop count** while appearing to route on time. But `05 §121` says *"Q16.16 is for
      sub-Tile positions and nothing else"*. The alternative spelling — an integer count of a fixed
      fraction of a Tick — measures identically, so no number rests on this. **Whether the core
      acquires a second Q16.16 meaning is the corpus's decision, not a benchmark's.** Owed by R7
- [ ] **The routing Tick budget share** — 10% is a stated guess and **cannot** be ratified until the
      Tick's other consumers are priced. **S2 R5.3 sharpened what the wire has to be stated over,
      and it is not what R3 published.** R3's *routing fits while fewer than 85 Trips start per Tick*
      is derived from a **mean** per-route cost; at **16** Trip starts R5 measures a worst Tick of
      **10.37 ms** against a 15.6 ms budget. **A mean times an arrival rate does not bound a Tick** —
      S4's K6 said it first, where a run whose worst iteration was 100.2 ms read 2.462 ms at p99.9.
      So both the 10% row and R3's 85 are owed a restatement **over the worst Tick rather than the
      mean one**, and that is a change to what the tripwire measures rather than to its threshold
- [ ] **`LayerRuleset` is not yet read from the Ruleset** — owed by **slice 8**, not by slice 6, and
      this replaces an item that said `WorldConfiguration` and a log-format bump. `adr/0044`'s first
      draft owed both; on the corrected classification the cadence is **ordinary hot-reloadable
      Ruleset data**, so it arrives with the TOML loader alongside every other tuning number and needs
      no format version of its own. `LayerRuleset` is a constructor argument of `World` until then,
      which is the finished shape rather than a stopgap
- [ ] **A save migration path for the *kernel radius*, which is a shape `adr/0015` has no word for.**
      **NEW, produced by `adr/0044`.** The radius is world-creation-fixed because a Cell is stored in
      kernel units — but unlike `TICKS_PER_DAY`, nothing is *lost* by changing it: the sources survive,
      so one full re-diffusion repairs the map. `adr/0015` offers only *reload freely* and *refuse the
      reload*; this wants a third answer, **migrate**. The Chunk may be a second member. If a third
      appears, it is `adr/0015`'s two-category split that reopens, not `adr/0044`
- [ ] **The plume range wants a source, not an argument** — **NEW, produced by `adr/0044`.**
      `02 §2.4`'s *1–10 km* is 10× wide and `02 §2.5` guard rule 1 says two ranges more than ~5× apart
      are two fields wearing one name. Either industrial pollution is a near plume **and** a regional
      haze, or the band describes the spread across industries. Typed *measurable*; the kernel stays
      unratified until somebody produces the figure
- [ ] **A save migration path for a Chunk size change.** Chunk size is on the *cannot be retrofitted*
      list and nothing describes what happens if a profile later says it should move. **Its own session**
- [ ] **Labour as an input Bin, against `adr/0026`'s jobs** — **NEW, produced by session B**, and a
      reconciliation rather than a fresh question. `02 §4.1` dissolves *"only produces if staffed"*
      into a **labour input Bin** filled by arriving commute Trips, and session B loaded that Bin
      further by folding **experience** into it as a per-worker deposit multiplier. `adr/0026` has
      jobs as a **Household↔Business relationship**, so the labour Bin is a *second* representation
      of the same fact. `0002` flagged this before the extra load arrived; `04 §7` (Jobs) is **stale
      twice over** and is where it lands. **An economy session, not `02 §4`'s**
- [ ] **The Microscopic Cap** — still unset. Needs a built traffic model; S2 R2 only informs it

---

## Blocked

**Every row here but one names a session rather than a piece of work**, and that is the point of the
rework. S0 is the single row waiting on code; everything else is waiting on an argument nobody has
had, which means none of it has to wait for slice 6.

| | Blocked on | Which is |
|---|---|---|
| **Slice 7** — Rule engine, Bins and Rules | 🔴 `adr/0015`'s Ruleset validator | session **A**, and the TOML dependency exception with it. **`02 §4` residue is closed** — and closing it *moved* this gate rather than clearing it, because `adr/0045`'s two refusals are load-time validation |
| **Slice 8** — Rule engine, hot reload | 🔴 `adr/0015` | session **A** — *already* overdue against `06` |
| **Slice 9** — Event Wheel | 🔴 `02 §7`, `adr/0006` | session **C** |
| **Slice 10** — Zone Rules | depends on slice 7 | session **A**, transitively |
| **S0** — synthetic 1M-Citizen city | slice 6. *Until it exists, 1M is a hope* | the only row here blocked on code |
| **Phase 2 milestones 5a–10** | 🔴 `03 §5` and six research-written ADRs, plus S2 | sessions **D**–**J**, plus a spike |
| **Planning Phase 2 at all** | S0 must have run, and `06`'s ordering must be re-derived | session **K2** |
| **Phase 3** | 🔴 **a presentation design that does not exist** | session **L**, itself blocked on **S1** and **S3** |

**The Phase 3 row used to read *"unplanned by design, and stays that way"*, and that was wrong** — it
described a choice where the truth is an absence. Phase 3 is unplanned because rendering has never
been designed, never been argued, and has no document to argue: every other phase is backed by `02`,
`03`, `04` or `05`, and there is no equivalent for presentation. Worse, the interface it would build
on was **re-argued to serve something else** — `adr/0002` was rebuilt around hot and cold query
flavours on the finding that it had *"assumed a renderer because rendering is what an engine boundary
is usually for"*, when the actual consumer is an inspector. The chain is written down now, in `06` and
here: **S1 + S3 → L → Phase 3 is plannable.**
