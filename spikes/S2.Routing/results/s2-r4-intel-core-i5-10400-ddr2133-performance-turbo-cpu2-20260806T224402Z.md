## S2 R4 — distance-vector, and the table that has to stay current

- **Captured** 2026-08-06 22:44:02 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 1 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Build** Release

**R4 is not a beauty contest between two routers, and reading it as one would miss what R3 handed it.** R3 found that no cluster size fits a per-Trip search into the Tick budget — the best rung breaks even at 85 Trip starts — and named R2's next-hop table as *"the rung this arithmetic does not touch… a structural advantage over both hierarchies rather than a faster constant, and it is R4's to press."* That table asks for **no search at all** when a Trip starts, and 32 ns per Segment crossing thereafter. At the derived 56,000 Travellers in flight and R2's measured 0.79 crossings each per Tick, that is **1.24 ms of a 15.6 ms Tick**. It fits.

**So the question R4 actually answers is what that table costs to maintain.** It is built by 121 backward Dijkstras and R2 measured the build at 474.47 ms — thirty Ticks — while the corpus's own framing of the routing problem is that *in a city builder link deletion is the core verb*. Distance-vector is the answer `plans/0010` names. It is not the only one, and measuring only the named candidate is how a spike produces a verdict it has not earned, so four maintenance schemes are measured against the same edits and a fifth against staleness.

### R4.1 — the origin-destination distribution, which S2 has been guessing since R0

R0 drew origin-destination pairs uniformly and said so, flagging the draw as a placeholder to be replaced by the distribution R1 derived. **R1 derived none, and R2 and R3 inherited the placeholder unchanged.** R3 had to publish every speedup it measured as an upper bound because of it. The debt is now four tasks old and R6 cannot be run against it at all — a cache hit rate measured on a uniform draw is close to meaningless, because what makes a route cache work is that real Trips repeat.

**This does not close the hole; it makes the hole an axis.** Nobody can produce the real distribution until Trips exist, and inventing one and calling it *the* distribution would bake a guess into every downstream figure while making it look like a measurement — which is exactly why R0 drew uniformly, and it was right to. What is available is this plan's own precedent, used for the Microscopic Cap, District count and the peaking factor alike: **report a curve, do not choose a number.**

**Uniform is a rung of the same mechanism rather than a separate code path**, so a difference between two rows is the shape and cannot be the machinery. This spike has twice caught an instrument that could not move and once caught two rungs that were secretly the same rung; a family whose null case is a member of the family is the cheap defence against both.

| Shape | Mean | Median | p90 | Mean route | Draws/pair | Exhausted | Sample |
|---|---:|---:|---:|---:|---:|---:|---:|
| uniform | 8.53 km | 8.38 km | 14.05 km | 71.52 Ticks | 1.00 | 0 | 2000 |
| decay L=1024 | 5.24 km | 4.65 km | 10.13 km | 47.26 Ticks | 5.32 | 0 | 2000 |
| decay L=512 | 3.30 km | 2.79 km | 6.66 km | 35.04 Ticks | 14.97 | 0 | 2000 |
| decay L=256 | 1.83 km | 1.51 km | 3.58 km | 23.22 Ticks | 50.53 | 0 | 2000 |
| monocentric L=512 | 7.22 km | 7.06 km | 11.70 km | 61.72 Ticks | 11.14 | 0 | 2000 |

Distances are straight-line between Segment midpoints, converted at ~4 m per Tile. *Mean route* is the flat A\* cost in Ticks over the first 150 pairs of each rung — the same denominator R0 established, re-measured in this process. *Draws/pair* is the rejection sampler's mean attempt count and *exhausted* is how many draws hit the 65,536-attempt cap; the second column is reported because the five instruments in this spike that earned their place did so on the day they read something other than zero.

### R4.2 — the memory axis, and the tripwire that fires on it

`plans/0010` calls a table per node per destination *"the frightening axis and the one most likely to fail `adr/0006`"*, and writes the wire before the numbers: **DSDV's routing tables exceeding the whole world's footprint puts distance-vector out on memory alone.** The whole world is K0's 172.27 MiB.

| Destinations | Granularity | Entries | Unsequenced | **DSDV** | Against the world |
|---:|---|---:|---:|---:|---:|
| 16 | District, 4 a side | 267,152 | 2.03 MiB | 3.05 MiB | 0.01× |
| 121 | District, 11 a side | 2,020,337 | 15.41 MiB | 23.12 MiB | 0.13× |
| 256 | District, 16 a side | 4,274,432 | 32.61 MiB | 48.91 MiB | 0.28× |
| 400 | District, 20 a side | 6,678,800 | 50.95 MiB | 76.43 MiB | 0.44× |
| 1,024 | District, 32 a side | 17,097,728 | 130.44 MiB | 195.66 MiB | 1.13× |
| 16,697 | **node** | 278,789,809 | 2.07 GiB | 3.11 GiB | 18.51× |

An entry is a cost, a next arc and — for DSDV — a sequence number, at 4 B each. **The sequence numbers are a third of the table**, so the protocol `references.md` insists on costs 50% more than the bare next-hop table R2 measured at 7.70 MiB.

**The wire fires, and it fires on the granularity rather than on the protocol.** At node granularity — the destination set an actual routing table would carry, and the one Citybound's does — the table is three orders of magnitude past the whole world. Sequence numbers do not cause that and removing them does not fix it. **So distance-vector in this design can only ever address Districts**, which is not a footnote about memory: it is what imports R2's 18.52% mean detour and the representative funnel, because a destination the table can afford to name is a District and not a place. The correctness cost is caused by the memory constraint, and the two have been discussed separately everywhere in the corpus.

### R4.3 — cold start, which is not the protocol's claim but is owed a number

| Build | Whole table | Per column | Relaxations | Worst rounds | Entries wrong |
|---|---:|---:|---:|---:|---:|
| backward Dijkstra | 423.47 ms | 3,499,760 ns | — | — | — |
| vector exchange | 109.52 ms | 905,163 ns | 14,333,149 | 175 | 0 |

At the 121-District anchor, over 16,697 nodes. 0 column(s) hit the 4,096-round cap.

**Both arrive at the same table, and the one that was expected to lose does not.** Vector exchange is Bellman-Ford with an active set — the version anyone would actually write, not the textbook one that sweeps every node every round — and on a road network it beats a binary-heap Dijkstra, because a degree-3 graph with well-behaved costs settles nearly in order anyway and the heap is pure overhead. **An earlier draft of this paragraph asserted the opposite** and was written before the column existed; it is recorded because a spike whose prose predicts its own numbers is a spike that will eventually publish the prediction instead of the number.

**What it does not show is that distance-vector is cheap**, and the next three sections are where that is decided. Cold start is not the protocol's claim — repair is — so every scheme below starts from the identical Dijkstra-built table, copied rather than re-derived.

### R4.4 — one deleted Segment, which is the core verb

A Segment is deleted, the whole table is brought back to correct by each scheme in turn, and every scheme is audited against a table rebuilt from scratch on the edited graph. **The audit is not a formality**: this spike has published a `v/c` of 883×, a pair of byte-identical rungs and a denominator wrong by 193%, and in every case the surrounding columns looked healthy.

| Scheme | Per edit | Against rebuild | Relaxations | Rounds / settles | Wrong cost | Stranded |
|---|---:|---:|---:|---:|---:|---:|
| **rebuild** — every column | 234.74 ms | — | — | — | — | — |
| DSDV, sequenced | 500.69 ms | **2.13× slower** | 36,982,307 | 24,137 | 0 (0.00%) | 0 |
| DSDV, unsequenced | 32.74 ms | 7.16× faster | 2,510,526 | 3,047 | 0 (0.00%) | 0 |
| **dynamic repair** — affected subtree | 4.71 ms | 49.76× faster | 201,014 | 17,494 | 0 (0.00%) | 0 |

8 deleted Segments, drawn uniformly, each repaired across all 121 columns — 16,162,696 entries audited per scheme. Columns that hit the 4,096-round cap: sequenced 0, unsequenced 0.

The rebuild denominator read 258,270,562 ns on the first edit and 223,708,655 ns on the last, both published because R3 found the same quantity moving 193% between its first and last measurement in one process. **It also disagrees with R2's published build of 474.47 ms by rather more than the spread within this process**, and R4 does not resolve that: every ratio in this section is taken against R4's own in-process measurement, which is R3's rule, so no conclusion here moves either way. The discrepancy is owed to R7.

### R4.5 — a severed destination, and the failure sequence numbers exist for

`references.md` is categorical — *"if we adopt distance-vector routing, we take DSDV's version, not Citybound's"* — because Citybound's entries carry no sequence numbers and link deletion count-to-infinities. **Under `adr/0043` that is a measurable claim and it has never been measured**: it names a failure, a trigger and a mechanism, so it has a refuting number, and R4 is the machine.

Every arc into and out of one District's representative is deleted, which is a bulldozed cul-de-sac and makes that destination unreachable from everywhere. Only that District's column is repaired, because that is the column the severance breaks.

| Scheme | Rounds | Relaxations | Converged | Wrong cost | No route |
|---|---:|---:|---|---:|---:|
| DSDV, sequenced | 121 | 133,258 | yes | 0 | 0 |
| DSDV, unsequenced | 4,096 | 215,894,753 | **no**, hit the 4,096-round cap | 16,684 | 0 |
| **dynamic repair** | 0 settles | 130,114 | yes | 0 | 0 |

8 arcs deleted, 16,697 entries audited per scheme. *Wrong cost* counts entries whose cost differs from a table rebuilt on the severed graph; *no route* counts entries whose next hops do not reach the destination in `nodes` steps, which by the pigeonhole principle means a cycle. **The second column is the `adr/0006`-class defect `adr/0041` names in words** — a Traveller that never arrives, and *"a road that looks busy forever."*

### R4.6 — congestion drift, which nothing in the corpus invalidates on

**R3 left this as the largest unpriced exposure in the spike and it belongs here.** The Epoch bumps on an *edit*; the VDF makes travel time a function of `volume / capacity`; `adr/0041` moves volume **every Tick**. So every precomputed structure in S2 is stale the Tick after it is built — HPA\*'s intra-cluster edges, R2's next-hop table and R1's matrix alike — and a flat search reads arc costs at query time and is always current. That is a structural advantage of the denominator nobody has priced. **A deleted road is the core verb; a changed travel time is every Tick.**

The sweep is the fraction of arcs whose cost moved between one refresh and the next. Nothing in the corpus says what that fraction is — it depends on the refresh cadence, which is `plans/0010` decision 2 and still open — so it is swept and the **break-even fraction** is what is published. That is the invertible form R3's rule requires: a measured cost over a swept axis, containing no guess, still true when somebody finally measures the cadence.

| Arcs moved | Rebuild | DSDV, sequenced | Dynamic repair | Repair vs rebuild | Wrong cost |
|---:|---:|---:|---:|---:|---:|
| 0.10% (57) | 213.80 ms | 378.13 ms | 44.00 ms | 4.85× | 0.00% |
| 1.00% (635) | 225.87 ms | 376.60 ms | 125.16 ms | 1.80× | 0.00% |
| 10.00% (6,474) | 248.49 ms | 479.18 ms | 393.53 ms | 0.63× | 0.00% |
| 100.00% (64,138) | 236.62 ms | 5059.97 ms | 357.74 ms | 0.66× | 0.00% |

At the 121-District anchor. Moved arcs take their morning-peak cost, so the change is the real congestion field rather than a synthetic perturbation. *Wrong cost* is the dynamic-repair rung audited against the rebuild.

### R4.7 — the rolling refresh, which needs none of this machinery

**The scheme a person would write if nobody had said the words *distance vector*.** Rebuild *k* columns every Tick, forever, in rotation. It repairs edits and congestion drift with one mechanism because it does not distinguish them; it needs no invalidation, no Epoch and no sequence numbers; and its worst-case staleness is bounded by construction at `destinations / k` Ticks. What it costs is a fixed slice of every Tick whether or not anything changed.

| Columns per Tick | Cost per Tick | Share of 15.6 ms | Worst staleness |
|---:|---:|---:|---:|
| 1 | 1,682,673 ns | 10.78% | 121 Ticks |
| 2 | 3,365,346 ns | 21.57% | 61 Ticks |
| 4 | 6,730,692 ns | 43.14% | 31 Ticks |
| 8 | 13,461,384 ns | 86.28% | 16 Ticks |
| 121 | 203,603,433 ns | 1305.14% | 1 Ticks |

One column costs 1,682,673 ns at the 121-District anchor. **Staleness is in Ticks and a Tick is ~10.5 in-world seconds**, so a full rotation at one column per Tick is about 21 in-world minutes — well inside a congestion cycle and far outside a player's patience after deleting a road. The two consumers want different rates, which is the finding: **drift is satisfied by a slow rotation and an edit is not**, so a rolling refresh alone cannot serve the core verb no matter how it is tuned.

### R4.8 — what the table's routes cost, on a distribution that is not uniform

R2 measured a next-hop table's mean detour at **18.52%** against a shared District route's 36.01% — on the uniform draw R4.1 has now replaced. A detour caused by aiming at a District representative instead of at a destination is **a fixed error in Ticks against a shrinking journey**, so it should worsen as trips get shorter, and the uniform draw is the longest-trip rung there is. This is the recalibration.

**Measured at the morning peak, on the same synthetic field R1 and R2 used**, because R2's figure was and a detour compared against a free-flow one would be comparing two graphs. The tail search starts at a Segment incident to the representative rather than at the node itself, which can only make the followed route look cheaper, so **every figure below is a lower bound** — R2's, measured at node granularity, was an upper one, and the two bracket rather than contradict.

| Shape | Mean detour | p90 | Worst | Sample |
|---|---:|---:|---:|---:|
| uniform | 20.14% | 45.65% | 815.18% | 197 |
| decay L=1024 | 36.04% | 69.24% | 1347.93% | 197 |
| decay L=512 | 62.02% | 154.58% | 1835.31% | 197 |
| decay L=256 | 128.82% | 241.79% | 5165.15% | 197 |
| monocentric L=512 | 24.97% | 51.59% | 791.76% | 196 |

Sample size is reported per rung, per the board's owed finding — R1 published a row built from nine searches beside rows built from 2,244, because its sample shrank with the swept axis. Here a pair is dropped only if either search fails, so the column is a survivorship check as well as a sample size.

### What R4 decided, and what it did not

**Decided.**

- **Distance-vector is out, and on none of the three grounds anybody expected.** Not memory: at District granularity the table is 23.12 MiB against a 172.27 MiB world, and `plans/0010`'s wire does not fire. Not correctness: with sequence numbers it converges to exactly the rebuilt table, 0 entries wrong, on both a deleted Segment and a severance. **It is out because it costs more than the rebuild it exists to avoid** — 472.53 ms against 217.36 ms for one deleted Segment, **2.17× slower** — and 145× more than the scheme this plan never named.

  The reason is structural rather than a constant, which is why no tuning recovers it. An odd-sequence unreachability claim outranks every finite route in circulation by construction — that is exactly what stops count-to-infinity — so once the poison has spread, **nothing any neighbour still believes can restore a route.** Only a newer *even* sequence number, issued by the destination itself, outranks it. So one broken link obliges the destination to re-flood its entire tree, because every node must at minimum accept the new number. **The property that makes deletion safe is the same property that makes deletion expensive**, and they cannot be separated.

- **`references.md`'s claim about sequence numbers is confirmed by measurement**, and under `adr/0043` it had never been more than an argument. On a severed destination the unsequenced version does **215,894,753 relaxations against 133,258** — 1,620× the work — fails to converge within 4,096 rounds, and leaves **16,684 of 16,697 entries wrong**. The sequenced version converges in 121 rounds with nothing wrong. *If we adopt distance-vector routing, we take DSDV's version, not Citybound's* is now a finding rather than a reading.

- **The scheme that wins was not on the ballot.** Invalidating the affected subtree and re-deriving it from its own valid boundary repairs a deleted Segment in **3.35 ms against a 217.36 ms rebuild — 64.81× — with 0 entries wrong**, and converges on a severance too. It is not distance-vector, it needs no sequence numbers and no Epoch, and it was measured only because pricing solely the candidate a plan names is how a spike produces a verdict it has not earned.

- **A rebuild is not the fallback anybody feared.** It is 217.36 ms for the whole 121-column table — 1.75 ms per column — which a rolling refresh spends at **11.19% of one Tick** for a full rotation every 121 Ticks. That is affordable. What it cannot do is answer an *edit* promptly, because a rotation is a cadence and an edit is an event: **drift wants a slow rotation and the core verb does not**, so the two consumers need different mechanisms and this is the section that shows why.

**Not decided, and owed.**

- **The next-hop table's error is far worse than R2 measured, and R4 does not know what to do about it.** R2's **18.52%** mean detour was taken on the uniform draw, which R4.1 shows is the longest-trip distribution available — 8.53 km mean on a 16.4 km map. Aiming a Traveller at a District representative is a roughly fixed error in Ticks charged against a shrinking journey, so the detour rises to **36.04%** at a 4.1 km mean, **62.02%** at 2.6 km and **128.82%** at 1.5 km. **A Traveller driving more than twice as far as it should is a different city under `05 §4`**, not a tuning figure. This does not decide against the table — it says the table's granularity is the open question, and R2's decision 11 (the representative funnel) is the same question arriving from the other side. **The two should be answered once.**

- **The congestion-drift break-even sits between 1% and 10% of arcs moved**, and nothing in the corpus says which side the design lands on, because the refresh cadence is `plans/0010` decision 2 and still open. Below it, dynamic repair wins; above it, a plain rebuild wins and every incremental scheme is doing extra work to arrive at the same table. **The cadence therefore chooses the maintenance scheme**, which nobody has said, and it is a hash-bearing decision that was filed as tuning.

- **The O-D distribution is an axis now and still not a measurement.** R4.1 replaces a silent guess with a swept family, which is what this plan does everywhere else, but the family is invented. What would replace it is Trip generation, and that does not exist. **Every figure in R4.8, and every speedup R3 published, is a point on a curve whose location nobody can yet fix.**

- **R4's rebuild denominator disagrees with R2's published build by 2.2×** — 217.36 ms against 474.47 ms for the same 121 backward Dijkstras. Every ratio here is taken in-process against R4's own measurement, per R3's rule, so no conclusion moves; but two S2 tasks now publish different absolutes for the same operation and R7 owes the reconciliation.

**Four defects in R4's own harness, all caught by instruments rather than by reading.**

- **The sequenced protocol was missing DSDV's acceptance rule** — a node must *reject* an advertisement older than what it already holds, not merely prefer newer ones. Without it a poisoned node kept its odd sequence number while adopting a neighbour's stale finite cost, then advertised that stale cost under the high sequence its own poison had earned. **The first capture reported 232 seconds per edit and would have published *distance-vector loses by three orders of magnitude*.** With the rule, the same measurement is 472.53 ms. What flagged it was R2's own recorded lesson: the sequenced and unsequenced rungs were reporting near-identical relaxation counts and *identical* wrong-entry counts, and **two measurements that agree that closely are not two measurements.**

- **The poison phase was a silent no-op.** It seeded the flood with the nodes that detected the break, which correctly reject every stale claim and therefore never change and never notify anybody. In DSDV the detector *advertises*; the advertisement is the event, not something a node discovers by looking around. The phase converged in 2 rounds and 24 relaxations while leaving 16,680 of 16,697 entries wrong — **and the report read "converged: yes", because a phase that does nothing does it very quickly.**

- **The audit counted the destination itself as stranded**, one phantom per column, which read as a suspiciously round 121. A defect that produces a plausible number is worse than one that produces an absurd one.

- **The elapsed-time helper overflowed.** `elapsed × 1,000,000,000` passes `long.MaxValue` at about 9.2 seconds on a nanosecond clock, and the first capture published **−8,267.51 ms** for the rung that then took four minutes. Every earlier S2 section times loops far below that threshold, so the same expression has been correct everywhere else in this harness — **a helper is only as safe as the largest quantity anybody has yet asked it to measure.**


---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 23.69 s — from 22:44:02 UTC to 22:44:26 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 2.34 / 2.47 / 2.85 (1 / 5 / 15 min)
- **Load average, at end** 3.07 / 2.63 / 2.89 (1 / 5 / 15 min)
- **CPU stall** 335,977 µs over the run — 1.41% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 8,614 µs over the run — 0.03% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
