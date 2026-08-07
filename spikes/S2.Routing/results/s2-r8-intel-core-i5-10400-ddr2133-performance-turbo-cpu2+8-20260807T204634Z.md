## S2 R8 — the congestion loop, and whether three layers make it natural

- **Captured** 2026-08-07 20:46:34 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 2 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Build** Release

Working rung: 33,018 Segments, 16,697 nodes, 66,036 arcs, 121 Districts. **The fleet size is not inherited from R2** — R8.0 measures it, for the reason given there.

**The loop closes on both sides.** Fleet volume → live BPR cost → *both* a Sight decision at each crossing *and* the traversal time the Traveller is actually charged → volume. Every earlier task in this spike routed over an array computed once; R8's own first draft closed only the routing arrow, charging Travellers **free-flow** residuals, and that was wrong in a way worth recording. With free-flow residuals `03 §3.4`'s middle arrow — volume → travel time — does not exist: the VDF is computed, routing reads it, and nothing in the world slows down. *Volume* then means concurrent users of an arc rather than an accumulation, and the amplification that makes a jam a jam — slower, so longer residence, so higher volume, so slower still — is simply absent. It is present here.

**`v/c` here is the same quantity R2b published**: `volume / (capacity × free-flow time)`, reached through `Congestion.LiveRatioUnclamped`. An earlier draft divided by the bare flow capacity because the private `Ratio` does — which is right there, since that method reads a demand field deposited as a *share* of capacity rather than a count of Travellers, and wrong here by the free-flow factor. Two figures sharing the name `v/c` and differing by 13% is how a corpus acquires a contradiction nobody can find later. **Reconciling it changes what `LiveCarTicks` returns**, because BPR now reads the reconciled ratio, so the delay at a given volume is higher than the draft's was. `adr/0046` names the same hazard from the other side: Sight and Promotion must read the *same* quantity or the city diverts around a jam it never promotes.

**The oscillation metric, defined before any number is printed.** Over the top-64 volume indices by mean volume during warm-up, the **mean absolute Tick-over-Tick change in `v/c`**, in Q16.16, across the measurement window. It is taken on the **unclamped** ratio: `Congestion.MaximumVolumeCapacity` puts a ceiling at 4.00 and the busiest 64 indices are precisely the ones likely to be sitting on it, so a clamped metric would read zero whether the network was still or thrashing. A supplementary column differences the **mean over the 64** instead of averaging the 64 differences; it is 8× less exposed to arrival noise and it is a diagnostic, not the metric. **It is no longer what any tripwire fires on** — see the restatement below.

**Habit carries a known granularity error and is used anyway.** R4 and R5.5 measured a District-granular next-hop route as structurally wrong by 16.58% on the uniform draw and 149.73% on the local one. It is the null hypothesis `adr/0046` requires — *static per world* — and diversion under it is free, which is the property R8.6 prices against a stored route. **R8's stability conclusions carry to either path source; its cost column does not.**

**Run protocol.** 256 warm-up Ticks, during which the top-64 are selected, then **two consecutive 96-Tick measurement windows, both printed**. Two windows more than 25% apart are reported as **not steady** rather than averaged. Every Tick of every rung asserts `TotalVolume() == Size × 65,536`, `Unplaced() == 0` and `Bounded == 0`; any failure is printed in the rung's row and again in the tripwire block.

### The finding, before anything else

**At 12.98% of this network's holding capacity, 87.25% of all traffic is on the busiest one per cent of its road and 90.87% of it is carrying nothing.** That is R8.0's measurement at the operating load of 40,000 Travellers against a network that holds 308,016 at `v/c = 1` everywhere, whose Streets are derived below to be running at a textbook two-second saturation headway. **It is not a congestion measurement.** It is a statement about what a District-granular free-flow shortest-path tree does to a road network: it funnels a whole city onto a skeleton and leaves nine tenths of the carriageway unused. There is exactly one route per (node, District) pair in the entire model, and no amount of empty parallel road can be reached from it. This outranks every timing column in the section.

**It is decision 11 arriving from a third side.** R2 measured the representative *funnel* at the destination node and put it at 412% `v/c`. R8.0 widened that definition once, from the arcs arriving at a representative to a four-Segment convergence zone, printed both, and found the funnel does **not** bind here — the columns are identical to the printed digit at every rung. The binding term is not the node where routes converge; it is the **tree upstream of it**. Decision 11 has been argued as a question about how many access nodes a District exposes, and that is the wrong axis: a District with a hundred access nodes still has one shortest-path tree per destination, and one tree is what concentrates the traffic. **That is a different fix from the one the question has been asking for**, and it is upstream of anything the access-node count can reach.

**It is why there may be no good operating load, and the sweep answers that rather than leaving it to be inferred.** Two terms with numbers on them: a rung is *congested* if p99 over occupied indices has reached free-flow saturation (1.00), and *resolvable* if fewer than 10.00% of readings on the busiest 64 indices sit past the BPR clamp, which is the ceiling R8.0's retired criterion named. **No rung on the sweep is both, so the data supports the claim.** The largest resolvable rung is 5,000, where p99 over occupied indices is only 0.42 — the network is not congested there in any sense the statistic can see. The smallest congested rung is 20,000, where 78.12% of top-64 readings are already past the clamp. **Under a District-granular free-flow tree there is no load at which this network is both congested and resolvable.** The concentration is what closes the gap: because the traffic is on one per cent of the road, the busiest arcs go past the clamp long before the network as a whole has anything worth calling congestion on it. That is the tension R8.0's two criteria exposed and it is not an artefact of either criterion.

**It bears directly on session M.** M has been choosing between a maintained next-hop table and a cached route on two axes — structural error and temporal error — with R8.6 adding diversion cost as a third. This is a **fourth**, and it is in the same column as the first three: a maintained free-flow table does not merely go stale, it *concentrates*. Every Traveller bound for a District follows one tree, so the table's error is not distributed over the fleet, it is correlated across all of it, and the correlation shows up as a saturated skeleton beside an empty network. A route cache does not have this property for free either — it depends on what seeded the routes — but a scheme that gives a Traveller more than one candidate route to begin with is the only kind that can. **M should be told that the table's fourth defect is not a cost, it is a spatial distribution.**

### The tripwires, stated before the run

S4's practice and its stated reason: *the wire was written before the numbers arrived precisely so it could not be reasoned around afterwards.* Verdicts are in the block at the foot of the section; the conditions are here.

| # | Condition | The number |
|---:|---|---|
| 1 | **Sight lowers `v/c` against the control.** With the loop closed on both sides this *is* the question R8 exists to answer | **p99** `v/c` at R8.1's floor is at least 5.00% below the Horizon-0 control's |
| 2 | **The instrument is connected: Sight changes the volume trajectory relative to a control with identical physics and no ability to respond** | mean `v/c` over the top-64 differs from the control's by at least 5.00%, **and** the control records exactly zero diversions |
| 3 | Conservation, every Tick, every rung | `TotalVolume() == Size × 65,536`, `Unplaced() == 0`, `Bounded == 0`, zero spawn failures |
| 4 | Steady state established, never assumed | every rung's two measurement windows within 25% of each other |
| 5 | The Sight pass's cost is **measured**, never a per-decision cost times a guessed decision rate | `Move(N) − Move(0)`, with `Refresh` timed as its own column |
| 6 | Every table names its O-D rung and its load | — |

**Two further conditions are stated here because they govern what may be published, and both are new to this capture.** First, R8.0's operating load is selected by the largest rung whose **p99** `v/c` stays below the BPR clamp; the criterion it replaces — a clamp-share count over the busiest sixty-four indices — is kept, printed and compared, and if the two disagree the p99 one governs. Second, **no verdict about Temperament may be published unless a maximal-herd positive control has first been shown to separate from the swept family by at least 25.00%** on at least one of the two herd metrics. A refutation read off a flat column is worthless unless the column has been shown able to move.

**Every `v/c` figure in this section is a quantile ladder and not a maximum, and that is the largest single change since the previous capture.** Every table from R8.0 to R8.5 reported *peak `v/c`* — one maximum over tens of thousands of volume indices — and three separate arguments in the previous version were built on the shape of that column. S4 already established what a maximum over a large noisy population is worth: a run whose worst iteration was 100.2 ms read 2.462 ms at p99.9. The ladder is p50, p90, p99, p99 over occupied indices, and the maximum, in that order, with the maximum last because a runaway arc is still worth seeing and is not a headline. Where a rule or a selection used to read the maximum it now reads p99, and each such restatement is written down where it happens.

**The `p99 occupied` column was not asked for and is here because the spike found the instruction incomplete.** The correction that arrived was *read it on p99*; running that revealed that this network's volume indices are nine parts empty, so an unconditioned p99 reads the boundary of the empty region and — demonstrably, in R8.3's cross-load ladder — takes the same value on every rung of a Horizon sweep. A statistic that cannot move cannot carry a verdict, and that is provable from the ladder without reference to any outcome. **The conditioned column is the spike's correction and not the instruction's.** It is printed in every ladder from R8.0 onward and no wire in this capture was restated onto it: R8 scores its wires as written and recommends the conditioned rung to its successor.

**Tripwires 1 and 2 are restatements and the originals are kept visible.** They were first written as *"the instrument must move — rung a's oscillation materially above the control's"* and *"the control must not move — Horizon 0 must show near-zero oscillation"*, and both were written for a model in which Travellers consumed **free-flow** residuals. Under that model the VDF was computed, routing read it, and nothing in the world slowed down: `03 §3.4`'s middle arrow — volume → travel time — did not exist, so the Horizon-0 control genuinely had no dynamics and *quiet* was a sensible thing to demand of it.

**That model is wrong and this run does not use it.** A Traveller now consumes the arc's **live** traversal time, so a jam slows the Travellers in it, which lengthens their residence, which raises the volume, which raises the cost. The control therefore has genuine dynamics and asking it to be quiet would be asking it to be the old model. What it does not have is any ability to *respond*, and that is what makes it the right control: the only difference between it and a Sight rung is routing. The restated wires isolate exactly that, and the oscillation amplitude is still reported throughout — it is simply no longer what a wire fires on. Nothing here is amended away; the original wording is above and the reason for the change is this paragraph.

Habit is one free-flow next-hop table over the 121 anchor Districts, built once in 325.14 ms and **never refreshed**. It resides in 7.70 MiB, and R8 adds a second column of the same shape — the settled free-flow distance to each District, 7.70 MiB — because a branch score is meaningless without the remainder. That column is opt-in and absent from every earlier task's resident-size figure.

### R8.1 — the actionable-junction distance. No traffic at all

For every arrival — a node *and* the arc arrived by — the distance to the nearest node at which the driver has a **real choice**: at least two onward car-passable arcs once the way back is discounted. `adr/0046` makes this the one routing parameter whose lower bound is derivable rather than tuned, so it is derived before any behavioural argument runs.

**The state is the arrival and not the node, and that is forced rather than chosen.** Whether a node is a choice depends on the arc used to reach it, so a node has no answer independent of one. The node projection below takes each node's **worst** arrival, because a floor derived from the best arrival is not a floor.

| Distribution over | Count | At distance 0 | p50 | p90 | max |
|---|---:|---:|---:|---:|---:|
| arrivals, Segments | 64,103 | 98.02% | 0 | 0 | 5 |
| arrivals, free-flow Ticks | 64,103 | 98.02% | 0.00 | 0.00 | 10.60 |
| nodes, worst arrival, Segments | 16,660 | 96.24% | 0 | 0 | 3 |

35 arrivals of 64,138 reach no real choice at all, and 23 arrive somewhere whose only onward car arc is the one arrived by — a forced U-turn, which this model does not offer. Both are excluded from the distribution above and printed here so the denominator is visible.

**The floor for the Sight Horizon is taken as the p90 of the arrival distribution, 1 Segment(s)**, and the choice of p90 over the median is stated rather than assumed: a horizon set at the median is a horizon that is structurally useless to half the crossings in the city. `adr/0046`'s claim *a Sight Horizon of one is actionable* is **not refuted** by the median and **not refuted** at p90.

**This is the graph's answer and not the driver's.** It weights a cul-de-sac nobody uses as heavily as the arterial ramp the whole city crosses. R8.3's *no-alternative share* column is the same finding weighted by where drivers actually are, and neither may be published without the other.

### R8.0 — the load this network carries. It runs before everything else

**You cannot measure a congestion response without first establishing what load the network carries, and R8's first draft did not.** It inherited R2's 40,000 Travellers on the grounds that figures should be comparable — but R2 was pricing attribution and did not care whether the network was gridlocked. With live residuals and BPR at `β = 4` an arc at the clamp costs **39.4×** free-flow, so its Travellers dwell 39× longer, so its volume rises further. That is positive feedback, and it pins at the clamp from any load high enough to reach it.

Every rung here runs at **Horizon 0** — no routing response at all — so what is being measured is the network and the physics and nothing else. Rung is uniform.

**`v/c` is a quantile ladder and not a peak, and this is the second criterion R8.0 has had.** The first was *the largest rung at which fewer than 10.00% of the top-64 indices sit at or above `MaximumVolumeCapacity`*, and it is kept here rather than amended away — it is still printed, in the *Past the clamp* column, and it still selected the load the first capture ran at. What was wrong with the section it fed was not that criterion but everything around it: **every other figure in R8 was a maximum over 33,018 volume indices**, which is the worst available summary of a large noisy population and the mistake S4 already paid for once. So the ladder replaces the peak everywhere, and the selection criterion is restated on it, **before the sweep runs**:

> **The operating load is the largest rung at which p99 `v/c` over every car-carrying volume index stays below the BPR clamp, 4.00** — the largest load, that is, that leaves ninety-nine per cent of the network inside the range BPR can actually resolve. The rung at which p99 first passes 1.00 is reported alongside it as the point where the busiest percentile reaches free-flow saturation, and is not itself a selector.

**`v/c` is reported twice: over every volume index, and with decision 11's funnel arcs excluded.** Under District-granular routing every Trip into a District arrives through one node, and R2 already measured that funnel at 412% `v/c`. The gap between the two is how much of this network's congestion is the *partition* rather than the *city*. The two are split across two tables here only because twelve ladder columns do not fit one.

| Travellers | v/c p50 | v/c p90 | v/c p99 | v/c p99 occupied | v/c max | Zero-volume share | Mean v/c, top-64 | Past the clamp | Arrivals/Tick | Mean journey, Ticks | Steady |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|
| 1,000 | 0.00 | 0.00 | 0.09 | 0.20 | 0.62 | 97.89% | 0.04 | 0.00% | 13.86 | 71.74 | yes |
| 2,500 | 0.00 | 0.00 | 0.09 | 0.31 | 0.75 | 95.31% | 0.11 | 0.00% | 34.23 | 72.08 | yes |
| 3,500 | 0.00 | 0.00 | 0.12 | 0.42 | 1.25 | 93.83% | 0.15 | 0.00% | 48.07 | 71.72 | yes |
| 5,000 | 0.00 | 0.00 | 0.20 | 0.42 | 25.39 | 92.67% | 1.30 | 6.25% | 59.35 | 83.40 | yes |
| 7,500 | 0.00 | 0.00 | 0.20 | 0.62 | 34.28 | 91.43% | 3.85 | 20.31% | 74.50 | 97.16 | yes |
| 10,000 | 0.00 | 0.00 | 0.20 | 0.85 | 41.77 | 91.00% | 7.21 | 39.06% | 83.11 | 113.90 | yes |
| 20,000 | 0.00 | 0.00 | 0.42 | 18.00 | 52.51 | 91.12% | 14.01 | 78.12% | 95.96 | 144.28 | yes |
| 40,000 | 0.00 | 0.00 | 1.18 | 25.17 | 64.34 | 90.87% | 18.30 | 81.25% | 119.38 | 162.75 | yes |
| 80,000 | 0.00 | 0.00 | 10.07 | 31.89 | 82.14 | 90.40% | 24.01 | 84.43% | 145.19 | 175.06 | yes |

The same sweep, with decision 11's funnel taken out:

| Travellers | v/c p99, all | p99, 1-hop funnel out | p99, 4-hop zone out | v/c max, all | max, 1-hop out | max, 4-hop out | Zone share of top-64 |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1,000 | 0.09 | 0.09 | 0.09 | 0.62 | 0.62 | 0.62 | 0.00% |
| 2,500 | 0.09 | 0.09 | 0.09 | 0.75 | 0.75 | 0.75 | 0.00% |
| 3,500 | 0.12 | 0.09 | 0.09 | 1.25 | 1.25 | 1.25 | 0.00% |
| 5,000 | 0.20 | 0.20 | 0.20 | 25.39 | 25.39 | 25.39 | 0.00% |
| 7,500 | 0.20 | 0.20 | 0.20 | 34.28 | 34.28 | 34.28 | 0.00% |
| 10,000 | 0.20 | 0.20 | 0.20 | 41.77 | 41.77 | 41.77 | 0.00% |
| 20,000 | 0.42 | 0.42 | 0.42 | 52.51 | 52.51 | 52.51 | 6.25% |
| 40,000 | 1.18 | 1.18 | 1.29 | 64.34 | 64.34 | 64.34 | 3.12% |
| 80,000 | 10.07 | 10.07 | 11.06 | 82.14 | 82.14 | 82.14 | 4.68% |

**Operating load: 40,000 Travellers.** The p99 criterion above selected it, and every table from R8.2 onward names it.

**Does the load move when the knee is read on p99 rather than on the top-64 clamp share?** The retired criterion selects 5,000 and the stated one selects 40,000, so the answer is **yes**. Everything downstream runs at the p99-selected load, and any figure a reader carries over from the previous capture is at the wrong load. p99 first reaches free-flow saturation — 1.00 — at 40,000 Travellers.

**And the criterion has a defect that this sweep exposed and that the sentence stating it did not anticipate.** p99 is taken over *every car-carrying index*, and at the selected load 90.87% of those indices are **empty**. A ninety-ninth percentile of a population that is nine parts nothing is roughly an eighty-ninth percentile of the part that is carrying traffic, and this network's congestion lives in its busiest fraction of one per cent. So the stated criterion looks *past* the jam rather than at it: it selects a load at which 81.25% of readings on the busiest sixty-four indices are past the clamp — which is the exact condition R8.0 was written to prevent. The *occupied only* column is printed beside the ladder so the dilution is visible; it reads 25.17 at the selected load.

**The criterion is not retuned, and the run is not repeated until it gives a nicer answer.** It was stated before the sweep and it governs, exactly as written. What is done instead is what `adr/0044` did with its own second half: the defect is recorded where it happened, and the section is measured **at both loads** — the full Sight ladder is repeated at 5,000 after R8.3 as a stated cross-check. If R8's central answer is the same at both, the selection did not matter and the ladder was the whole of the correction. If it is not, then **the answer to R8's central question is load-dependent**, and that is a larger finding than either load's number.

#### What an operating load of this size means

`SegmentCapacity` is Q16.16 vehicles per Tick and the graph carries two values: **3,600 veh/h** on a Street and **12,000 veh/h** on an Arterial, both **whole-Segment rather than per-direction** — `GraphParameters.Working` runs `VolumeScope.PerSegment`, so the two directions share a volume index and share the capacity. A Tick is 10.55 s of in-world time and the conversion `(veh/h) × 192 = Q16.16 veh/Tick` is exact, so nothing here carries a rounding of its own.

**Is 3,600 veh/h a realistic Street?** It works out at 1,800 veh/h per direction, and the check that settles it is the headway it implies. A Street's free-flow speed is 50 km/h; the shortest car Segment traverses in 0.87 Ticks; so at `v/c = 1` there are **9.21 vehicles present on the Segment**, 4.60 per direction. Over 128 m that is one vehicle every ~28 m, which at 13.9 m/s is a **two-second headway** — the textbook saturation headway for an urban lane. The capacity is not low. It is a single lane per direction running at saturation flow, and `v/c = 1` here means what it means in the traffic-engineering literature it was borrowed from.

Summed over all 32,069 car-carrying indices, the network holds **308,016 vehicles at `v/c = 1` everywhere**. The operating load of 40,000 Travellers is **12.98%** of that.

And at that load the network is, almost everywhere, **empty**: 90.87% of car-carrying indices hold no vehicle at all, the median index reads 0.00, the ninetieth percentile reads 0.00 — and **87.25% of all volume sits on the busiest one per cent of indices.**

**Against the design's own numbers.** `CLAUDE.md` targets 10,000 population in the first hour and 1,000,000 late game over 268 km² and ~30,000 Segments; S4 task 2 derived **56,000 vehicles in flight** as the Day average, and `plans/0010` carries a 2–3× peaking correction on top of it — 111,000 to 170,000 at the morning peak. This network reaches the knee at 40,000. That is **71.42% of the derived Day average** and 23.52% of the top of the peaking band.

**So which of the two answers is it?** Neither, and the derivation says so cleanly: the capacity is realistic, and the network does *not* run out of road. It runs out of **routes**. 90.87% of the carriageway is carrying nothing while a fraction of a per cent of it is past a clamp where BPR can no longer tell one jammed arc from another, and the mechanism that puts it there is named and not mysterious: **Habit is a single shortest-path tree on free-flow costs, so every Traveller bound for a District is following the same tree into the same representative node.** There is one route per (node, District) pair in the entire model, and no amount of empty parallel road can be reached from it.

**That finding is promoted to the top of the section**, with its three consequences, because it outranks every timing column here. What follows below is the rest of R8 measured inside it.

Two limits on how far it travels. It is measured on a **synthetic grid** whose Arterials were placed to be severable rather than to carry a city, and it is measured with **one Traveller per Trip and no departure-time spread**, so the whole fleet is on the road at once. Both make concentration worse than a real city's would be. Neither is capable of making an empty network look full: the zero-volume share is a direct reading and it does not depend on either.

**What the sweep says about the representative funnel.** Under District-granular routing every Trip into a District arrives through one node, and R2 measured that funnel at 412% `v/c`. At the operating load p99 is 1.18 over every index, 1.18 with the arcs arriving at a representative removed, and 1.29 with the whole 4-Segment convergence zone removed; the maxima are 64.34, 64.34 and 64.34. The zone holds 3.12% of the busiest 64 indices.

**The funnel does not bind here, and that is worth stating because it was expected to.** Removing the convergence zone barely moves the reading. The reason is arithmetic: only *destinations* converge under this origin-destination model — origins are scattered real nodes — and arrivals are divided across every non-empty District, so each representative receives a small fraction of the fleet's arrival rate. R2's 412% was measured with **both** endpoints pinned to representatives, which is a different and harsher query shape. **The congestion R8 measures is in the network, not in the partition** — which makes the rest of the section about routing, which is what it is for.

**Where the network turns over.** Between 1,000 and 80,000 Travellers p99 goes 0.09 → 10.07, the maximum goes 0.62 → 82.14, the zero-volume share goes 97.89% → 90.40%, and the share of the busiest 64 indices past the clamp goes 0.00% → 84.43%. Mean journey time goes 71.74 → 175.06 Ticks while arrivals per Tick go 13.86 → 145.19 — **throughput rising far more slowly than load, which is the dwell-time feedback doing exactly what it is supposed to do.** The transition is not gradual, and the rungs are spaced to find where it happens rather than to bracket a chosen answer.

Read the *Steady* column too. A rung that does not settle inside two short windows is a rung where the dwell-time feedback has not finished amplifying, and it is the direct evidence for whether this loop converges at all rather than running away. The operating load is 40,000.

### R8.2 — the loop closes, and the instrument moves

Rung **a** — Habit plus Sight at the Horizon R8.1 set (1), Temperament spread 0 — against rung **control**, which is Habit alone at Horizon 0. Both carry identical physics: live residuals, 40,000 Travellers, uniform. The only difference is that the control cannot respond.

| Rung | v/c p50 | v/c p90 | v/c p99 | v/c p99 occupied | v/c max | Mean v/c, top-64 | Oscillation w1 | w2 | Steady | Diversions/Tick | Crossings/Tick |
|---|---:|---:|---:|---:|---:|---:|---:|---:|:-:|---:|---:|
| control, N=0 | 0.00 | 0.09 | 0.96 | 23.43 | 69.11 | 22.96 | 0.58 | 0.51 | yes | 0.00 | 7883.50 |
| a, N=1 | 0.00 | 0.09 | 1.73 | 16.48 | 113.14 | 15.64 | 0.72 | 0.53 | **no** | 1269.51 | 9013.37 |

**TRIPWIRE 1 — Sight lowers `v/c`: FIRED.** Read at **p99**: 1.73 against the control's 0.96, a change of -79.03% against a stated bar of 5.00%. The maxima are 113.14 against 69.11, printed because a runaway arc is worth seeing and not because the wire turns on it. **Advisory, and stated after the fact rather than before it**: read over occupied indices only, the same quantile is 16.48 against 23.43.

**TRIPWIRE 2 — the instrument is connected: PASS.** Mean `v/c` over the top-64 moves from 22.96 to 15.64, 31.87% against a stated bar of 5.00%; the control recorded 0 diversions and rung a recorded 243,747.

**Tripwire 1 fired, and the two columns above disagree about the sign of the answer, which is the finding rather than an embarrassment.** p99 `v/c` went **up** — 0.96 to 1.73 — while the mean over the busiest sixty-four indices went **down**, 22.96 to 15.64, and so did the same quantile taken over the indices that are actually carrying something: 23.43 to 16.48. All three are true and they describe one behaviour: **Sight redistributes.** It takes load off the extreme tail, where the arcs were far past the clamp, and puts it onto arcs that were previously carrying nothing — and a percentile of a population that is nine parts empty rises the moment previously-empty arcs start carrying traffic, however much relief the busy arcs got. A router that spreads a jam over more road *must* raise an unconditioned middling quantile. That is what spreading is.

**Beside that sits an argument about the instrument that does not depend on the outcome, and it is what distinguishes this from reasoning around a wire that fired.** The unconditioned p99 is a quantile of a population in which 88.57% of members are **empty road**. A ninety-ninth percentile of that population is roughly an eighty-ninth percentile of the part carrying traffic, and it sits at the boundary of the empty region where nothing distinguishes one rung from another. **The demonstration is in R8.3's cross-load ladder below**: at the lighter load the unconditioned p99 reads the *same value on every rung of the Horizon sweep* — a statistic that cannot move. That is an instrument defect, it is provable by inspecting the ladder without knowing what any rung did, and it is the same class of defect R8.2 exists to catch. It does not unfire the wire. It bounds what the wire is evidence of.

**So the wire is scored FIRED and is not rewritten.** It was stated before the run, in this form, and this capture is what it says about it. But every reading in this section that is conditioned on *road that is carrying traffic* moves the other way, and there are four of them: the mean over the busiest sixty-four indices, p99 over occupied indices, the share of readings past the BPR clamp, and mean journey time. All four are in R8.3's table and all four improve. The one reading that fires is the one taken over a population that is nine parts empty road. **A fourth version of this wire belongs in R8's successor and it should read the share past the clamp** — the only one of the five that is a statement about whether the *model* can still see what it is simulating — with the occupied quantile beside it.

**The control is not quiet and must not be expected to be.** It carries the same physics as rung a — Travellers slow in jams, dwell longer, and pile up — so its oscillation column is the network's own dynamics with routing held out of it. The wire that asked for a quiet control was written for the open-loop model and is restated above rather than amended away. **Whether R8.3 to R8.6 may be published turns on tripwire 2**: if the control and rung a have the same trajectory, costs are being computed and not read, and `plans/0010` refuses the rest of the section.

### R8.3 — the Sight sweep

Temperament spread 0 throughout, at the **uniform** origin-destination rung, 40,000 Travellers, base threshold 0.10 (the placeholder R8.4 replaces). The expectation being tested is that `v/c` falls with `N`, cost rises with `N`, and the no-alternative share explains whatever `N = 1` does. **If `v/c` is flat in `N`, Sight is not a mechanism and `adr/0046`'s middle layer is wrong.** The reading is taken on **p99** and the maximum is carried alongside it.

| N | v/c p50 | v/c p90 | v/c p99 | v/c p99 occupied | v/c max | Mean v/c, top-64 | Past the BPR clamp | Oscillation | Diversions/Tick | No alternative | Mean journey, Ticks | Refresh ns/Tick | Move ns/Tick | Sight ns/Tick | of 15.6 ms |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0.00 | 0.09 | 0.96 | 23.43 | 69.11 | 22.96 | 92.18% | 0.55 | 0.00 | — | 240.04 | 1,551,234 | 359,012 | — | — |
| 1 | 0.00 | 0.09 | 1.73 | 16.48 | 113.14 | 15.64 | 83.30% | 0.63 | 1269.51 | 2.41% | 220.75 | 1,487,143 | 1,240,340 | 881,328 | 5.64% |
| 2 | 0.00 | 0.09 | 3.14 | 13.01 | 81.25 | 10.34 | 51.95% | 0.61 | 4887.31 | 1.82% | 233.92 | 1,487,378 | 2,370,649 | 2,011,637 | 12.89% |
| 4 | 0.00 | 0.00 | 3.25 | 15.62 | 209.42 | 13.50 | 49.16% | 1.13 | 4751.54 | 2.09% | 278.68 | 1,524,561 | 2,524,195 | 2,165,183 | 13.87% |
| 8 | 0.00 | 0.09 | 3.35 | 6.93 | 93.14 | 5.82 | 40.56% | 0.37 | 6722.03 | 1.69% | 251.18 | 1,521,764 | 4,863,846 | 4,504,834 | 28.87% |
| 16 | 0.00 | 0.09 | 2.81 | 5.53 | 77.35 | 6.58 | 42.49% | 0.39 | 8140.03 | 1.17% | 236.37 | 1,545,129 | 9,115,577 | 8,756,565 | 56.13% |
| 32 | 0.00 | 0.09 | 2.48 | 5.75 | 36.45 | 5.46 | 50.99% | 0.24 | 7313.80 | 0.94% | 201.49 | 1,522,492 | 12,592,912 | 12,233,900 | 78.42% |

**Read the saturation column before anything else in this table, because at this load it is a warning and not a footnote.** `Congestion.MaximumVolumeCapacity` caps the ratio BPR reads at **4.00**, on R1's stated grounds that a Statistical travel time past that point is already the wrong instrument. Past it the delay multiplier is constant, so the router cannot tell a bad jam from a catastrophic one, and every `v/c` above 4.00 is a quantity it was structurally blind to while it formed. **R8.0's selection criterion exists to hold this column down and at this load it did not**: the control sits at 92.18%, which means the busiest sixty-four indices are almost entirely inside the region the model cannot resolve. R8.0 says why — a p99 taken over a population that is nine parts empty looks past the jam — and does not tune it away.

**What that column then does down the ladder is the single most persuasive number in this section, and it is not the one the tripwire reads.** It falls from 92.18% at Horizon 0 to 40.56% at N = 8. Sight is pulling the busiest arcs **out of the unresolvable region** — more than halving the share of readings the congestion model is blind inside. That is a mechanism doing exactly what `adr/0046` claims for it, measured on the one column that cannot be argued about, and it is the reading a future statement of tripwire 1 should probably be built on. It is **not** substituted for the wire as written: the wire says p99 and p99 is what it is scored on.

**The conditioned ladder is printed beside the unconditioned one, and it is what decides whether the asymmetry argument below is needed at all.** p99 over *occupied* indices goes 23.43 at Horizon 0 down to 5.53 at N = 16, and across the whole ladder it is **not** monotone.

**The conditioned rung is not monotone either, so the asymmetry argument is not an artefact of the unconditioned statistic and stands on its own.**

**On p99, `v/c` still does not fall monotonically in `N`, so the asymmetry argument survives the change of statistic and is worth naming.** BPR at β = 4 with `α = 0.15` and the ratio clamped at 4.00 makes one saturated arc **39.4× its free-flow time** — a Street that runs in 0.87 Ticks free-flow runs in 34, against a mean journey of order 80. So a lookahead of even two arcs can put more live cost in front of a driver than the free-flow remainder behind it, and the remainder is what makes the branches comparable. Where that happens the comparison is dominated by its live half, the detour is charged at free-flow and looks cheap, and `N` stops behaving like a monotone knob. It is not a defect in the implementation: it is what a live-versus-lagged comparison does when the live half is bounded only by the clamp and the lagged half is not bounded at all, and it is a constraint on the base threshold that nothing in `adr/0046` anticipates.

The maximum column is **not** monotone, which is stated for completeness and carries no argument either way: it is one arc.

**The `Refresh` column is a finding on its own, and at this load it is the dominant one.** Recomputing the live cost array is `O(arcs)`, touches nothing else, and costs **more than the entire traveller loop** at every Horizon below 8 — before a single Traveller has looked at anything. It does not scale with the fleet, so it does not get better at 1M; it gets relatively cheaper only against work that grows. **The conclusion is not that the sweep is expensive. It is that a sweep is the wrong shape: cost updates have to be incremental and local.** Under `adr/0041` volume is written by Travellers entering and leaving arcs, so the set of arcs whose cost actually moved in a Tick is exactly the set of arcs somebody crossed — a few hundred, not 66,036 — and it is already enumerated by the loop that caused it. A per-Tick VDF sweep over every arc in the world recomputes a number that did not change for something like ninety-nine arcs in a hundred, which is the same shape of mistake as diffusing a Map Layer that nothing has touched. Whatever ships must update the arcs the Tick wrote and leave the rest alone; a staggered cadence would bound the cost but would also make a driver's Sight depend on which stagger bucket the arc in front of him fell into, which is `adr/0044`'s hash-bearing problem arriving in the routing layer.

**The Sight column is a difference and never a product.** It is this rung's measured `Move` cost minus the control's, which charges Sight for exactly the work Horizon 0 does not do. `plans/0010`'s R3 rule — *invert the derivation until what is published is measured* — refuses the alternative of a per-decision cost times a guessed decision rate. The `Refresh` column is separate for the same reason: it is `O(arcs)` and independent of fleet size, so folding it into the traveller loop would charge the Sight sweep for 66,036 arcs it never looked at.

**Selected Horizon: 1.** The rule is stated rather than eyeballed — the lowest **p99** `v/c` among the non-control rungs, ties broken toward the smaller Horizon, and never below R8.1's floor of 1. R8.3's cross-check and all of R8.4 run there. **This selects nothing for the Ruleset**; R8 reports curves exactly as R1 did for the District count, and the corpus decides.

#### The selected Horizon across R4.1's swept family, N = 1, 40,000 Travellers

**R4 found that S2's uniform draw had been hiding a conclusion**, so a figure taken at one rung is a figure whose rung has to be named. Every row here is the same Horizon under a different draw.

| O-D rung | v/c p50 | v/c p90 | v/c p99 | v/c p99 occupied | v/c max | Mean v/c, top-64 | Oscillation | Diversions/Tick | No alternative | Mean journey, Ticks | Steady |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|
| uniform | 0.00 | 0.09 | 1.73 | 16.48 | 113.14 | 15.64 | 0.63 | 1269.51 | 2.41% | 220.75 | **no** |
| decay L=1024 | 0.00 | 0.09 | 2.37 | 16.37 | 73.35 | 20.64 | 0.55 | 1463.81 | 2.42% | 166.83 | yes |
| decay L=512 | 0.00 | 0.09 | 2.04 | 16.26 | 71.72 | 22.25 | 0.56 | 1269.21 | 2.51% | 123.04 | yes |
| decay L=256 | 0.00 | 0.09 | 1.40 | 15.93 | 130.75 | 24.08 | 3.63 | 2690.31 | 2.21% | 77.15 | yes |
| monocentric L=512 | 0.00 | 0.09 | 1.34 | 19.42 | 82.36 | 18.06 | 0.76 | 1285.58 | 2.09% | 215.51 | yes |

#### The same ladder at 5,000 Travellers, the load the retired criterion chose

**R8.0's two criteria disagreed, so the section is read at both loads rather than at the one that suits it.** Everything above runs at 40,000, which the stated p99 criterion selected. Everything here is the identical sweep at 5,000, which the retired clamp-share criterion selected, and which R8.0 shows is the largest load leaving the busiest sixty-four indices mostly inside the range BPR can resolve. Nothing is selected off this table.

| N | v/c p50 | v/c p90 | v/c p99 | v/c p99 occupied | v/c max | Mean v/c, top-64 | Past the BPR clamp | Oscillation | Diversions/Tick | No alternative | Mean journey, Ticks | Steady |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|
| 0 | 0.00 | 0.00 | 0.20 | 0.42 | 23.98 | 1.27 | 6.25% | 0.07 | 0.00 | — | 82.73 | yes |
| 1 | 0.00 | 0.00 | 0.20 | 0.62 | 20.29 | 1.56 | 13.55% | 0.16 | 44.81 | 2.48% | 84.11 | yes |
| 2 | 0.00 | 0.00 | 0.20 | 0.85 | 18.01 | 0.95 | 7.51% | 0.25 | 136.80 | 2.49% | 81.67 | yes |
| 4 | 0.00 | 0.00 | 0.20 | 2.70 | 22.63 | 1.24 | 9.20% | 0.14 | 328.32 | 2.18% | 90.77 | yes |
| 8 | 0.00 | 0.00 | 0.20 | 0.64 | 29.76 | 0.72 | 4.98% | 0.09 | 173.79 | 2.30% | 82.00 | yes |
| 16 | 0.00 | 0.00 | 0.20 | 0.64 | 25.60 | 0.72 | 4.36% | 0.09 | 151.95 | 2.34% | 81.96 | yes |
| 32 | 0.00 | 0.00 | 0.20 | 0.85 | 10.78 | 0.51 | 2.49% | 0.11 | 366.43 | 1.96% | 75.02 | yes |

**Tripwire 1's reading at this load: it would have fired here too.** p99 goes 0.20 → 0.20 at N = 1, 0.00% against the same 5.00% bar. The ladder is monotone in `N` on p99 here.

**And here is the reason that reading should not be trusted either, at either load.** At 5,000 Travellers roughly nine car-carrying indices in ten hold nothing, so the ninety-ninth percentile *over every index* is a reading taken at the edge of the empty region rather than inside the traffic — which is why it is 0.20 at Horizon 0 and barely moves anywhere on the ladder. **A statistic that cannot move is the failure R8.2 exists to catch, and R8's own headline statistic has it — across 7 rungs the unconditioned p99 takes 1 distinct value(s), against 5 for the conditioned one. That count is a property of the ladder and can be checked without knowing what any rung did, which is what makes it an argument about the instrument rather than about the answer.** Read over occupied indices the same ladder goes 0.42 → 0.62 at N = 1 (-48.14%) and reaches 0.62 at N = 1.

**So the correction the ladder was asked for was right and incomplete, and the incompleteness is recorded here rather than smoothed over.** Replacing a maximum over tens of thousands of indices with a quantile ladder was the correct move and it dissolved three separate arguments this section had been carrying. What it then revealed is that the population being summarised is nine parts *empty road*, so an unconditioned quantile below about p99.9 describes the emptiness and not the traffic. The conditioned column — p99 over indices that are carrying something — is printed in every ladder in this section from R8.0 onward, and it is the column a successor task should state its wires on. R8 does not restate its own wires mid-capture on a statistic it chose after seeing the numbers.

**The answer does not change with the load, which is the more comfortable of the two outcomes and the less interesting one.** Whatever the wire says at the operating load, it says here too, so R8.0's disagreement between its two criteria changed what the section reports and not what it concludes.

### R8.4 — the improvement on offer, then the Temperament sweep

**Before any threshold is swept, what is a threshold *on*?** At every crossing where at least one alternative survived the filters, `(habitScore − bestScore) / habitScore` is recorded — the relative improvement the best alternative offers over Habit, which is exactly the quantity the diversion test compares against. N = 1, uniform, 40,000 Travellers, 96 Ticks after warm-up.

| Quantile | Over every decision | Over decisions offered anything at all | the latter as a percentage |
|---:|---:|---:|---:|
| p10 | 0.000000 | 0.000976 | 0.09% |
| p25 | 0.000000 | 0.031250 | 3.12% |
| p50 | 0.000000 | 0.125000 | 12.50% |
| p75 | 0.000000 | 0.250000 | 25.00% |
| p90 | 0.250000 | 0.500000 | 50.00% |

**78.35% of decisions are offered an improvement of exactly zero** — an alternative existed and none of them beat Habit. No threshold in `[0, 1]` can act on those, because the diversion test is a strict inequality, so they are decisions a threshold is not *about*. **The base rungs are therefore read off the 198,795 decisions that were offered something.** Sited over all 918,381 the median is exactly zero, which makes the base zero, which makes every spread rung `share × 0` — thirteen identical rows reported as a Temperament sweep. That happened once and the column above is what caught it.

**Why so many decisions offer nothing is not a mystery, and the explanation is structural rather than statistical.** Habit is `NextHopTable` — a shortest-path tree over the free-flow cost array, one tree per District. At every node on that tree the habit arc is the *first arc of the cheapest free-flow route to the destination*, which is to say it is optimal by construction and was optimal before any Traveller moved. An alternative arc is by definition off the tree, so it pays a free-flow penalty the moment it is taken, and it pays it in full and immediately. Both branch scores then carry a free-flow remainder — `DistanceOf(node, District)` — which is the same quantity computed from the same tree, so the remainders differ by exactly that penalty. **An alternative can therefore only win when the live congestion on the first 1 arc(s) of the habit branch exceeds the free-flow penalty of leaving the tree.** Below that, the comparison is decided by arithmetic that congestion never enters, and the improvement is exactly zero — not small, zero, because `bestScore` and `habitScore` are then ordered by the free-flow tree alone.

**Cross-check against the no-alternative column, which measures a different thing.** At N = 1, 2.41% of crossings had **no surviving alternative at all** — a dead end, a U-turn, or an arc the next-hop table has no distance for. So the 78.35% above is emphatically not that: in those decisions an alternative existed, was scored, and lost. The two columns are independent and both are printed, because *nowhere to go* and *nowhere better to go* have different consequences — the first is a property of the graph and the second is a property of the routing.

**The consequence is the useful part: Sight fires rarely, and that is what makes it affordable.** Of 1,730,568 crossings over the two windows, 243,747 produced a diversion — 14.08%. The share of decisions offered anything at all, 21.64%, is the ceiling on that, and the diversion rate is the tighter figure because a threshold sits between them. **R8.6's per-Tick bill must be the cost of one re-search multiplied by the diversion rate and not by the crossing rate**, and that is what it does — R8.6 multiplies by the measured diversions per Tick from this very sweep. A reader who costs Sight by assuming every crossing re-plans will overstate it by more than an order of magnitude. A reader who forgets that the diversion rate is itself a function of the base threshold will understate how much that Ruleset value costs.

There is a sting in it. The same structure that makes Sight cheap is the structure R8.0 found concentrating the whole fleet onto a fraction of the network: **one free-flow tree per District is both why alternatives rarely win and why there is congestion for them to win against.** Sight is being asked to relieve a jam its own null hypothesis created, using a score that same null hypothesis anchors. That is not an argument against `adr/0046` — it is the reason the ADR's Habit layer is named a *layer* and not a *baseline* — but it does mean the ~21.64% fire rate is a property of District-granular routing and should not be carried over to any scheme that gives a Traveller more than one candidate route to begin with.

The whole histogram, so the shape is visible and not only its quantiles:

| Improvement at least | Decisions | Share |
|---:|---:|---:|
| 0.000000 | 719,586 | 78.35% |
| 0.000015 | 4,056 | 0.44% |
| 0.000030 | 2,938 | 0.31% |
| 0.000061 | 3,342 | 0.36% |
| 0.000122 | 2,421 | 0.26% |
| 0.000244 | 2,324 | 0.25% |
| 0.000488 | 2,475 | 0.26% |
| 0.000976 | 3,042 | 0.33% |
| 0.001953 | 4,104 | 0.44% |
| 0.003906 | 4,313 | 0.46% |
| 0.007812 | 6,327 | 0.68% |
| 0.015625 | 8,343 | 0.90% |
| 0.031250 | 12,284 | 1.33% |
| 0.062500 | 18,723 | 2.03% |
| 0.125000 | 31,563 | 3.43% |
| 0.250000 | 65,513 | 7.13% |
| 0.500000 | 27,027 | 2.94% |

918,381 decisions with at least one surviving alternative, in **octaves** rather than equal bins: bucket `k` holds `[2^(k−1), 2^k)` in Q16.16 units. An equal-width histogram of a thousand bins was tried first and put every quantile from p10 to p90 in the first bin — a true reading and a useless one. The improvements on offer span orders of magnitude, and where they sit relative to the placeholder 10% is the whole of why the placeholder did nothing.

**The base threshold rungs below are these quantiles, and nothing else.** A threshold at p90 means one decision in ten clears it; a threshold at p10 means nine in ten do. Sweeping a threshold across the distribution it is applied to is the only way the sweep can be said to have covered anything — and it is the correction R8.4's first attempt needed, which swept spread around a base nobody had sited.

#### The base threshold, swept across the distribution above

| Base threshold | Quantile | Oscillation | v/c p50 | v/c p90 | v/c p99 | v/c p99 occupied | v/c max | Mean v/c, top-64 | Diversions/Tick | Mean journey, Ticks | Steady |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|
| 0.000976 | p10 | 6.41 | 0.00 | 0.09 | 0.85 | 24.07 | 101.56 | 19.10 | 7879.46 | 191.72 | yes |
| 0.031250 | p25 | 9.87 | 0.00 | 0.09 | 1.40 | 20.31 | 123.58 | 22.40 | 4841.93 | 205.85 | yes |
| 0.125000 | p50 | 0.55 | 0.00 | 0.09 | 1.62 | 16.70 | 176.91 | 16.25 | 1193.21 | 214.95 | yes |
| 0.250000 | p75 | 0.51 | 0.00 | 0.09 | 2.48 | 16.15 | 158.78 | 16.58 | 1056.53 | 226.71 | yes |
| 0.500000 | p90 | 0.46 | 0.00 | 0.09 | 2.48 | 18.21 | 58.15 | 17.72 | 885.15 | 233.69 | yes |

**The spread sweep runs at the base the median fell in, 0.125000.** That is a position in the measured distribution and not a reading off this table.

**A base threshold of exactly zero is a legitimate rung and not a degenerate one.** It means *divert whenever the alternative is strictly better at all* — `adr/0017`'s satisficing switched off, which is the comparison the whole layer needs. If the distribution puts its median there, that is the finding: the improvements this model offers a driver are too small for a relative threshold to have anything to bite on, and **that is a statement about the score, not about Temperament.**

#### The spread and the blend

At N = 1, 40,000 Travellers, base threshold 0.12. Spread is a **share of the base**; blend weight 0 is pure per-decision jitter and 1.00 is pure stable character. `adr/0046` argues **both endpoints fail**, for different reasons, and that is a claim with a number attached.

**Two metrics, because amplitude and synchrony are different quantities and the ADR's claim is about the second.** `adr/0046` says an identical rule over an identical input *"produces a herd: the whole flow switches to the alternative together, the alternative jams, the whole flow switches back."* That is many drivers making the **same** move at the **same** time. *Oscillation* — the mean absolute Tick-to-Tick change in `v/c` over the busiest 64 indices — measures how much the network moves, which a herd does cause but so does anything else. *Synchrony* measures the thing itself: of the diversions taken in one Tick, the share that went to the **same arc**, with the effective number of distinct arcs those diversions spread over alongside it. A perfect herd reads 100% and 1.00.

| Spread, of base | Blend | Oscillation | Synchrony | Effective arcs | v/c p50 | v/c p90 | v/c p99 | v/c p99 occupied | v/c max | Mean journey, Ticks | Diversions/Tick | Steady |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|
| **positive control** | — | 7.51 | 8.02% | 47.78 | 0.00 | 0.09 | 0.75 | 24.07 | 142.47 | 194.77 | 9337.50 | yes |
| 0.00 | — | 0.55 | 4.72% | 74.34 | 0.00 | 0.09 | 1.62 | 16.70 | 176.91 | 214.95 | 1193.21 | yes |
| 0.06 | 0.00 | 0.56 | 4.60% | 78.20 | 0.00 | 0.09 | 1.73 | 16.70 | 111.27 | 221.11 | 1204.61 | yes |
| 0.06 | 0.50 | 0.57 | 4.58% | 78.75 | 0.00 | 0.09 | 1.73 | 17.35 | 105.64 | 223.36 | 1179.09 | yes |
| 0.06 | 1.00 | 0.59 | 4.22% | 80.05 | 0.00 | 0.09 | 1.73 | 16.48 | 112.52 | 222.95 | 1220.82 | yes |
| 0.12 | 0.00 | 0.60 | 4.70% | 74.18 | 0.00 | 0.09 | 1.51 | 16.81 | 121.27 | 221.89 | 1170.36 | yes |
| 0.12 | 0.50 | 0.56 | 5.85% | 69.59 | 0.00 | 0.09 | 1.84 | 17.14 | 115.64 | 217.79 | 1326.26 | yes |
| 0.12 | 1.00 | 0.56 | 4.38% | 81.57 | 0.00 | 0.09 | 1.84 | 17.14 | 114.39 | 212.45 | 1227.16 | yes |
| 0.25 | 0.00 | 0.50 | 4.17% | 88.95 | 0.00 | 0.09 | 2.04 | 16.92 | 115.64 | 224.19 | 1250.65 | yes |
| 0.25 | 0.50 | 0.54 | 4.47% | 80.19 | 0.00 | 0.09 | 1.62 | 16.59 | 104.39 | 219.64 | 1138.03 | yes |
| 0.25 | 1.00 | 0.50 | 4.13% | 88.52 | 0.00 | 0.09 | 1.95 | 16.81 | 86.26 | 222.67 | 1220.68 | yes |
| 0.50 | 0.00 | 0.45 | 3.89% | 102.73 | 0.00 | 0.09 | 2.26 | 15.93 | 107.52 | 226.52 | 1181.70 | yes |
| 0.50 | 0.50 | 0.48 | 3.97% | 96.48 | 0.00 | 0.09 | 2.26 | 16.59 | 116.90 | 227.22 | 1281.35 | yes |
| 0.50 | 1.00 | 0.48 | 3.92% | 97.59 | 0.00 | 0.09 | 2.37 | 16.70 | 121.90 | 226.40 | 1255.97 | yes |

**The positive control is a construction built to herd maximally, and the criterion for believing either metric was stated before it ran.** Base threshold **0**, spread **0**: every driver applies the same rule to the same live costs, diverts the moment an alternative is better at all, and draws nothing at random. `adr/0046`'s herd is not a risk on that row, it is the definition of it. The bar: **a metric counts as a herd detector only if the positive control exceeds every swept row by at least 25.00%.**

Oscillation: 7.51 against a swept worst of 0.60 — **separates**. Synchrony: 8.02% against a swept worst of 5.85% — **separates**. The positive control's diversions spread over 47.78 effective arcs against a swept low of 69.59.

**At least one metric separates, so a flat sweep is a reading and not an instrument failure.** Whatever the spread column does below, it does it on an instrument that has been shown capable of telling a herd from a non-herd on this network. The tripwire verdict on Temperament stands as measured.

**A note on why the herd may be hard to produce here at all.** A herd needs many drivers at the same junction facing the same choice in the same Tick. R8.4 measures that the improvement on offer is zero for the large majority of decisions, so the population that could herd is small before Temperament is applied to it; and the diversions that do happen are spread over the effective-arc count in the table above. If that number is large, the network is not herding for reasons that have nothing to do with the layer under test, and no setting of the spread could show otherwise.

**One pattern in this table was odd enough to name in the previous capture and is reported here as retired.** The blend-0.50 rows carried visibly higher *peak* `v/c` than either endpoint at the same spread, which is the opposite of what `adr/0046` predicts. That reading was a maximum over 33,018 indices — the highest-variance column in the report — and the ladder is what it should have been read on. Whether anything survives at p99 is visible above; a three-row pattern in a thirteen-row table is exactly the size of thing that turns out to be nothing, and it was.

**Amplitude must fall monotonically in spread across at least the first three rungs.** Flat or non-monotone refutes the layer, and `adr/0046` already names what would replace it: staggered decision Ticks, or hysteresis on the diversion itself — both cheaper than Temperament and neither serving `UNIQUE INDIVIDUALS`, so the trade would be explicit. **That wire is now conditional on the paragraph above**: it can only refute the layer if the metric it reads was shown able to detect a herd.

**One doubt, recorded rather than settled: what the threshold is a share *of*.** The diversion test is `habitScore − bestScore > threshold × habitScore`, and `habitScore` is the live lookahead **plus the whole remaining free-flow journey**. So the absolute margin a Traveller demands scales with how far it still has to go: a driver two Segments from its destination has an effective margin near zero and diverts on almost anything, whichever Temperament it drew. The counter-argument is that this is right — *"this saves me a tenth of my trip"* is a reasonable thing for a person to weigh, and `adr/0017`'s **substantially better** is a relative test against the incumbent, not an absolute one. The formula is therefore kept and the distribution is published so the objection can be checked against it. An absolute threshold in Ticks, or one taken as a share of the lookahead alone, is a different measurement and R8 has not made it. Settling which is right by argument is what `adr/0043` exists to refuse.

#### Is the herd killed by the threshold, or by the variation?

**The positive control herds at 7.51 and every swept spread rung sits two orders below it. That is a switch, not a gradient, and it throws until now went unasked: *where* did it happen?** If the herd dies at the first non-zero threshold, then the spread ladder above was swept in a regime with no herd left in it, and its flatness is a statement about the siting rather than about `adr/0046`'s third layer. The two hypotheses — *per-Citizen variation does not damp* and *a threshold already damped everything, so there was nothing left to damp* — have very different consequences, and the sweep as sited cannot tell them apart.

**Both bars stated before either reading.** (i) *A threshold damps* if the lowest oscillation on the base ladder is **at most a quarter of the highest** — a factor of four, chosen rather than derived, and large enough that noise on this column cannot produce it. (ii) *Variation damps* if, at the most herding non-zero base, oscillation falls by at least 25.00% from spread 0 to the largest spread — the same bar the positive control had to clear to be believed at all.

The base ladder at spread 0, with the positive control as its zero rung:

| Base threshold | Oscillation | Synchrony | Diversions/Tick |
|---:|---:|---:|---:|
| 0.000000 *(positive control)* | 7.51 | 8.02% | 9337.50 |
| 0.000976 | 6.41 | 7.46% | 7879.46 |
| 0.031250 | 9.87 | 10.08% | 4841.93 |
| 0.125000 | 0.55 | 4.72% | 1193.21 |
| 0.250000 | 0.51 | 4.65% | 1056.53 |
| 0.500000 | 0.46 | 3.05% | 885.15 |

**Reading (i).** The ladder runs from 9.87 to 0.46, a factor of 21.22×, so **a threshold does damp** against the stated bar of four. And the transition is **inside** the ladder rather than at its first step: the smallest non-zero base, 0.000976, still reads 6.41 — the same order as the positive control. The herd survives a small threshold and dies at a larger one.

**So the spread ladder is re-sited.** The base above ran at 0.125000, the median of the improvement distribution, which the reading above places well past the transition. It is re-run at **0.031250**, the non-zero base with the *highest* measured oscillation — the one place on the ladder where a herd demonstrably still exists for Temperament to damp. Blend is held at 0.50, the even mixture `adr/0046` actually argues for, because a claim swept over two axes at once is a claim about neither.

| Spread, of base | Oscillation | Synchrony | Effective arcs | v/c p50 | v/c p90 | v/c p99 | v/c p99 occupied | v/c max | Diversions/Tick | Steady |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|:-:|
| 0.00 | 9.87 | 10.08% | 25.60 | 0.00 | 0.09 | 1.40 | 20.31 | 123.58 | 4841.93 | yes |
| 0.06 | 0.63 | 5.29% | 49.31 | 0.00 | 0.09 | 1.18 | 18.87 | 108.83 | 3991.98 | yes |
| 0.12 | 0.65 | 7.96% | 37.89 | 0.00 | 0.09 | 1.29 | 19.95 | 118.15 | 3687.55 | yes |
| 0.25 | 5.71 | 15.38% | 17.00 | 0.00 | 0.09 | 1.07 | 21.48 | 183.59 | 6051.57 | yes |
| 0.50 | 0.76 | 6.61% | 53.46 | 0.00 | 0.09 | 1.18 | 18.00 | 153.15 | 2651.13 | yes |

**Reading (ii).** Inside the herding regime, oscillation goes 9.87 at spread 0 to 0.76 at the largest spread — 92.28% against a stated bar of 25.00%. **Per-Citizen variation does damp** where there is something to damp.

**That overturns the verdict above, and the overturned one is left standing rather than deleted.** The spread ladder in the previous subsection was sited at the median of the improvement distribution, which is past the transition — it swept Temperament through a regime in which the base threshold had already killed the herd, and read flat for that reason. Sited where a herd exists, spread damps. `adr/0046`'s third layer is **not refuted**, and the lesson is about siting: a sweep across a measured distribution is not automatically a sweep across the regime the mechanism operates in.

**But the fall is not monotone, and `adr/0046`'s wire is stated on monotonicity rather than on a net fall — so the two readings disagree and the disagreement is real.** At spread 0.25 of the base the amplitude goes back up to 5.71 from 0.65 on the rung below it. What makes that worth a paragraph rather than a shrug is that **three independent columns move together on it**: synchrony reaches 15.38%, the effective arc count falls to 17.00, and diversions per Tick rise to 6051.57. Amplitude alone could be noise. Amplitude, synchrony and concentration agreeing is a herd.

**It is one measurement, so it gets one cheap test rather than a paragraph of reasoning.** The ladder was swept at a single blend weight. If the re-herd is a property of *how much* the thresholds are spread, it should survive changing *what they are spread with*; if it is one noisy rung, it should not. The same spread is re-measured at the two blend weights the ladder did not use.

| Blend | Oscillation | Synchrony | Effective arcs | Diversions/Tick |
|---:|---:|---:|---:|---:|
| 0.50 *(the ladder's)* | 5.71 | 15.38% | 17.00 | 6051.57 |
| 0.00 | 0.86 | 7.50% | 37.19 | 4924.12 |
| 1.00 | 0.65 | 11.12% | 24.21 | 3930.32 |

The rule, stated before the two rows were read: the re-herd **survives a blend** if that blend's amplitude stays above the midpoint between the offending rung and the rung below it. It survived **0 of 2**.

**So it is one noisy rung and the net-fall reading is the honest one.** The re-herd does not survive a change of blend weight, which is what a property of the spread would have to do. It is recorded rather than removed — the columns did move together and that is why it was tested — but it carries no conclusion, and `adr/0046`'s wire fails on monotonicity for a reason the data does not support calling a mechanism.

##### The wire, scored as written

`plans/0010` states it as *"amplitude must fall monotonically in spread across at least the first three rungs. Flat or non-monotone refutes the layer."* The first three rungs of the ladder above read 9.87, 0.63, 0.65. **The wire is non-monotone and by the wire as written `adr/0046`'s third layer is REFUTED.** That is the score. It is not softened, and the net-fall reading below is not offered in its place — substituting a statistic for the one a wire was stated over is the exact move this section has caught itself making twice already.

**Beside it, an argument about the instrument that does not depend on how the ladder fell.** The two blend rows above were measured at **one spread with nothing else changed**, and they returned 0.86 and 0.65 — a spread of 0.20 between two measurements the monotonicity claim treats as the same point. **That is a floor on what this instrument can resolve between adjacent rungs**, established by a measurement taken for a different purpose. The step that breaks the wire is 0.02. 1 of the ladder's 4 steps is smaller than the resolution, which means the rungs they join are **mutually indistinguishable**. A monotonicity test over values the instrument cannot separate is not a test, and that holds whichever way those rungs had happened to fall — had they come out in ascending order the wire would have passed on the same non-information.

**What survives is a different statistic, and it is labelled as one.** Over the same ladder the amplitude falls 9.87 → 0.76, 92.28% against the 25.00% bar stated before the run. That is a **net fall**, not a monotone fall; it is a claim about the ladder's endpoints and the wire was a claim about its steps. Both are printed, neither is presented as the other, and the tripwire table carries them on separate rows.

##### And the general lesson, which is now the third instance in R8

**A wire stated on monotonicity cannot distinguish *nothing happened* from *it saturated at the first rung*.** The mechanism here is a **cliff, not a gradient**: the first spread rung alone takes the amplitude from 9.87 to 0.63, a factor of 15.59×, and everything after it is flat inside the noise. Monotonicity was specified for a shape this phenomenon does not have, so the wire fires on the *saturation* rather than on any failure of the layer.

**That is the same class of defect as the other two this section found, and three instances make it the pattern rather than the anecdote.** A maximum over 33,018 volume indices was chosen before anyone knew the distribution was nine parts empty. An unconditioned p99 was chosen before anyone knew the same thing. A monotonicity test was chosen before anyone knew the response was a cliff. **Each was a statistic chosen before the shape of what it would measure was known**, and each survived into a published wire because nothing in the process asks that question. `adr/0043` requires a claim a measurement could settle to name the number that would refute it; R8's experience adds a second requirement to that — **name the shape you expect, because a number read off the wrong shape is not evidence.** A wire should be re-derived once the first measurement shows what the response looks like, and the re-derivation stated and scored separately rather than swapped in.

**And the siting lesson beside it, which is this section's most transferable finding.** The spread ladder was originally sited at the **median of the measured improvement distribution** — a defensible choice, made precisely to avoid sweeping around a number nobody had grounded, and it put every rung past the transition where the base threshold had already killed the herd. *A sweep across a measured distribution is not automatically a sweep across the regime the mechanism operates in.* The two are different axes and coinciding is a coincidence. Siting a sweep requires locating the **regime**, which means finding the transition first — and finding it cost five rungs here.

### R8.5 — does `03 §3.4`'s loop actually close?

The load-bearing one, and **the version of it that ran in the previous capture could not answer the question**. That version replaced 40% of the fleet with Travellers bound for one District and watched the network recover. It recovered — five times out of five, at Horizon 0 as reliably as under Sight — and the reason was in the harness: this fleet respawns a Traveller against the pool the instant it arrives, so a one-off retarget is a **pulse with a half-life of one journey**. Any system at all recovers from a pulse it stops receiving. Control and Sight settling identically was not a null result; it was no result.

**The surge is now sustained.** `SightFleet.SustainSurge` weights the *respawn pool* toward the surged District — 40% of every respawn for the whole 640-Tick window — on top of the same initial retarget. That is R1's monocentric morning peak as it actually behaves: people keep leaving for the centre for hours. Demand stays asymmetric while the network is watched, so the control cannot come down by waiting. Rung is uniform, 40,000 Travellers.

**And that changes the shape of the question.** Under a pulse the question was *does it recover*. Under a sustained asymmetry it is **does it reach a bounded steady state, and at what level** — a network that settles at a ruinous plateau has not self-corrected, and one that never settles at all has diverged. Control and Sight should differ in the level they settle at rather than in whether they settle. The re-peak rule and its three versions are retired with the pulse they judged, and are named here rather than removed.

**The level rule, stated before the run and not touched after it.** The watched series is **p99 `v/c` over occupied car-carrying indices**, sampled every 4 Ticks — occupied, because R8.3's cross-load block demonstrates *without reference to any outcome* that the unconditioned quantile cannot move on this network. Then:

> A rung has reached a **bounded steady state** if the mean of the last quarter of the window is within 25% of the mean of the third quarter — two consecutive quarter-window means agreeing, which is R8.2's two-window steadiness test applied to a series instead of a scalar. It is **unbounded** otherwise. The **settling level** is the mean over the last quarter. **Sight beats the control** if both bound and Sight's settling level is at least 5.00% below the control's — the same bar every other comparison in this section uses. If either fails to bound, the comparison is not made, and that is the result.

**Repeated into 5 destination Districts and reported as a distribution.** One destination is one draw, and a single settle from a single District is an anecdote about that District's approach roads. The Districts are spread evenly across the index range for reproducibility rather than chosen for being busy.

| Rung | District | Pre-surge level | Peak | Peaks at Tick | Third quarter | Last quarter | Bounded | Settling level | Over pre-surge | End of window |
|---|---:|---:|---:|---:|---:|---:|:-:|---:|---:|---:|
| control, N=0 | 0 | 23.64 | 31.78 | 636 | 24.85 | 27.62 | yes | 27.62 | 16.83% | 31.78 |
| control, N=0 | 24 | 23.64 | 27.54 | 580 | 24.51 | 25.97 | yes | 25.97 | 9.85% | 25.28 |
| control, N=0 | 48 | 23.64 | 31.78 | 620 | 27.88 | 30.04 | yes | 30.04 | 27.07% | 29.18 |
| control, N=0 | 72 | 23.64 | 41.87 | 592 | 35.36 | 39.48 | yes | 39.48 | 67.03% | 40.68 |
| control, N=0 | 96 | 23.64 | 31.56 | 300 | 27.82 | 24.70 | yes | 24.70 | 4.48% | 23.21 |
| Sight, N=1 | 0 | 21.56 | 25.92 | 88 | 14.92 | 15.84 | yes | 15.84 | -26.50% | 16.26 |
| Sight, N=1 | 24 | 21.56 | 25.59 | 128 | 14.73 | 17.02 | yes | 17.02 | -21.04% | 18.62 |
| Sight, N=1 | 48 | 21.56 | 24.18 | 68 | 14.81 | 15.05 | yes | 15.05 | -30.17% | 16.37 |
| Sight, N=1 | 72 | 21.56 | 25.59 | 100 | 16.58 | 15.01 | yes | 15.01 | -30.37% | 15.10 |
| Sight, N=1 | 96 | 21.56 | 25.62 | 88 | 19.28 | 17.45 | yes | 17.45 | -19.04% | 17.46 |

The same runs as a distribution:

| Rung | Bounded | Unbounded | Settling level, min | median | max | Median over pre-surge |
|---|---:|---:|---:|---:|---:|---:|
| control, N=0 | 5 | 0 | 24.70 | 27.62 | 39.48 | 16.83% |
| Sight, N=1 | 5 | 0 | 15.01 | 15.84 | 17.45 | -26.50% |

**The reading.** The control bounded 5 of 5 and Sight bounded 5 of 5. Median settling level: control 27.62, Sight 15.84, a difference of 42.62% against a stated bar of 5.00%.

**Both rungs bound and Sight settles materially lower, so `03 §3.4`'s loop closes with only the local layers reading the VDF.** That is the claim `adr/0046` is most exposed on, tested under a demand asymmetry that does not go away, against a control carrying identical physics and no ability to respond. It does not make Sight sufficient — the level it settles at is printed and a reader can judge whether that plateau is a city anybody would want — but the mechanism is real and the self-correction is not being done by the harness.

**A caveat the *Peaks at Tick* column earns, and it does not cut the way a caveat usually does.** 4 of 5 control runs and 0 of 5 Sight runs reached their highest sample inside the **last quarter of the window** — the same quarter the settling level is read over. A series whose maximum is at the end may still be climbing, and the quarter-agreement test can pass on a slow climb. Where that happens the settling level is a **lower bound** on where that rung would eventually sit. Since it is the control that peaks late here, the effect is to *understate* the control's plateau and therefore to understate Sight's advantage; the conclusion is not at risk from it, and a longer window would widen the gap rather than close it. A successor should run the window until the peak sample is not in the last quarter, which is a stated condition and cheap to check.

**One limit on this row, stated because it bounds the conclusion either way.** The surge is sustained on the *destination* only: origins still come from the rung's own pool, so what is modelled is everybody heading for one District rather than everybody leaving one place for it. A real morning peak is asymmetric at both ends. R2's 412% funnel figure was measured with both endpoints pinned, and it is the harsher shape; this row is the milder one and its result should be read as a lower bound on how hard a real peak would press.

### R8.6 — what a diversion costs, by path source

Under a next-hop table a mid-journey diversion is **free**: the Traveller reads a different arc out of the table and resumes from wherever it now is. Under a stored route the same diversion costs a fresh search from the current node. Sight makes diversion a *routine* event rather than an exception, so this stops being a footnote and becomes a per-Tick bill that scales with how congested the city is.

**It does not go through `HpaSearch`.** R5.5 found a pristine-seeding defect there and R6 owns it; R8's diversion search reads the live cost array directly, so nothing here inherits it.

At N = 1, uniform, 40,000 Travellers, 1269.51 diversions per Tick measured in R8.3. 512 of 512 re-searches found a route.

| Path source | Per diversion | × diversions/Tick | of 15.6 ms |
|---|---:|---:|---:|
| next-hop table read | 391 ns | 496,380 ns | 3.18% |
| flat A\* over live costs | 485,507 ns | 616.35 ms | 3951.01% |

The table read is timed over 64 passes of the captured sites because one read is a single strided load and a single-pass timing would be measuring the clock. The search is timed once per site, cold, with its arcs returned — R0's denominator shape, over the **live** array rather than the free-flow one. `sink` is 906233984 and exists so the reads are not elided.

**This is the third axis session M is owed** — structural error, temporal error, and now diversion cost — and R7 states the verdict. It decides nothing on its own.

### The denominator, measured first and last

R5's rule after the one-processor pinning artefact: a denominator read only at the start of a long section describes the minute before the section rather than the section. R5's own capture read 1,401,307 ns first and 477,609 ns last, which is what the practice exists to catch.

- One uncached point-to-point search, arcs returned, free-flow costs: 805.16 µs measured first, 415.52 µs measured last, 1.93× apart.

### The tripwires, and each one's verdict

| # | Tripwire | Verdict | Reading |
|---:|---|:-:|---|
| 1 | Sight lowers p99 `v/c` against the control | **FIRED** | 1.73 against 0.96, -79.03% against a bar of 5.00%, at N = 1, 40,000 Travellers |
| 1a | *Advisory, stated after the numbers and scoring nothing*: the same wire read over **occupied** indices | would pass | 16.48 against 23.43. Nine car-carrying indices in ten are empty, so wire 1's population is mostly empty road |
| 2 | The instrument is connected — Sight changes the trajectory, control cannot respond | **PASS** | mean `v/c` 22.96 → 15.64, 31.87%; control diversions 0 |
| 3 | Conservation, every Tick, every rung | **PASS** | 0 volume, 0 unplaced, 0 bounded, 0 spawn failures, over 42 rungs |
| 4 | Steady state established by two-window agreement, not assumed | **FIRED** | 2 of 42 rungs outside 25%; each is marked **no** in its own table |
| 5 | The Sight pass's cost is measured, never derived | **PASS** | R8.3's `Sight ns/Tick` is this rung's measured `Move` minus the control's, and `Refresh` is timed separately |
| 6 | Every table names its O-D rung and its load | **PASS** | R8.0 and R8.2 to R8.6 name both in the section text; R8.3's cross-check names one rung per row |
| — | `adr/0046` / `plans/0010`: Temperament damps the herd — amplitude falls **monotonically** in spread across the first three rungs | **REFUTED** | the ladder is non-monotone, scored as written and not softened. Beside it: two measurements at **one** spread with only the blend changed differ by 0.20, which is a floor on what this instrument resolves between rungs; the step that breaks the wire is 0.02, and 1 of 4 steps is below the floor. **A monotonicity test over values the instrument cannot separate is not a test**, and that holds whichever way they had fallen. The mis-sited ladder at the improvement median read non-monotone and is left standing in R8.4's history; see R8.4 |
| — | *A **different** statistic from the wire above, published beside it and never in its place*: the **net fall** in amplitude across the same ladder | **damps** | 9.87 → 0.76, 92.27% against a 25.00% bar stated before the run. A claim about the ladder's **endpoints**; the wire above was a claim about its **steps**. The response is a cliff — the first rung alone carries it — and monotonicity was specified for a shape this phenomenon does not have; see R8.4 |
| — | `adr/0046`: Sight is a mechanism — p99 `v/c` falls with `N` | **non-monotone** | see R8.3 |
| — | `adr/0046`: under a **sustained** asymmetry of demand, `03 §3.4`'s self-correction still closes with only the local layers reading the VDF | **not refuted** | both rungs reach a bounded steady state and Sight settles materially lower than a control with identical physics and no ability to respond; see R8.5 |

**The Temperament verdict has now changed twice and the final one is this**: `plans/0010`'s wire, which is stated on monotonicity, is **REFUTED** — scored as written, on the statistic it named, and not substituted. Standing beside it and not in place of it: the layer **does** damp the amplitude, by a large factor, sited where a herd exists; the wire fails because the response is a cliff rather than a gradient and the rungs after the cliff are separated by less than the instrument can resolve. The earlier flat reading at the improvement distribution's median was a **siting artefact** and is left standing in this section's history rather than deleted, as is the `REFUTED` it produced. **What a successor should carry forward: keep the layer, stop claiming monotonicity for it, and re-state the wire on a shape the response actually has.**

For reference and no longer for firing: one Traveller present is worth 0.10 of this `v/c` at the median car-passable index, so an oscillation amplitude is readable in Travellers per Tick per arc.

### The caps imposed, all of them

A silent truncation reads as *we covered everything* when it did not, so each one is here whether or not it changes a conclusion.

- The origin-destination pool is 4,096 pairs per rung, drawn once and drawn from thereafter. At the anchor rung 0 were discarded as unreachable or degenerate, leaving 4,096.
- R8.0's rungs are short — 128 warm-up Ticks and two 64-Tick windows against 256 and 96 everywhere else — because the sweep only has to find where the network breaks. A rung marked **no** under *Steady* has not settled inside that budget and its numbers are a trajectory rather than a level.
- The funnel is defined as the volume indices of arcs **arriving at** a District representative — one hop. A two-hop definition would catch more of the convergence and would also start catching ordinary through-traffic, so the narrow definition is used and the column reads as a lower bound on how much of the congestion is the partition's.
- A ladder quantile is reported at the **lower edge** of a bucket a sixty-fourth of a `v/c` unit wide, so every quantile understates by up to 0.015625. The maximum is exact and unquantised.
- The cross-load ladder repeats only R8.3's Horizon sweep at 5,000. R8.4's Temperament sweep, R8.5's surge and R8.6's diversion cost are measured at the operating load only, and none of them has been checked for load-dependence.
- R8.4's base rungs are the 5 quantiles of the measured improvement distribution, deduplicated to 5 distinct thresholds. Where several quantiles share an octave the row names all of them.
- R8.4's spread-0 row is measured once rather than once per blend weight: with no spread the blend multiplies nothing and three identical rows would read as three agreeing measurements.
- R8.5 runs 5 destination Districts × 2 rungs. Each run is 256 warm-up Ticks plus a 640-Tick observation with a full 33,018-index ladder scan every 4 Ticks, which is the most expensive thing in this section.
- R8.6 prices 512 diversion sites, captured from a live rung during warm-up rather than drawn. The capture buffer is 4,096 sites per Tick and a Tick that overflows it is truncated silently by the fleet — the count here is the honest denominator.
- The `v/c` ladder is scanned every 4 Ticks rather than every Tick, and every reading from both windows is pooled into one distribution. A full sweep of 33,018 volume indices per Tick would be most of this section's runtime, and the ladder is a property of the observation rather than of a Tick.
- The Sight Horizon is swept at **one** origin-destination rung — uniform, the family's anchor — and only the selected Horizon is carried across the other four. The full cross product is 7 × 5 runs and does not fit ten minutes.
- Mean journey time counts only journeys that **completed inside a 96-Tick window**. A long journey that spans the window is not in it, so the figure is biased short and is comparable between rungs rather than absolutely. With live residuals that bias is larger than it was, because a congested journey is a long one.



---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 111.30 s — from 20:46:34 UTC to 20:48:25 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 0.41 / 0.79 / 1.10 (1 / 5 / 15 min)
- **Load average, at end** 1.28 / 1.00 / 1.14 (1 / 5 / 15 min)
- **CPU stall** 148,127 µs over the run — 0.13% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 40,836 µs over the run — 0.03% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
