# 0013 — The Tick budget ledger

**What does a Tick cost?** One table, one row per consumer, priced in **milliseconds** against the
settled target — **15.6 ms at 1,000,000 Citizens, on one core of the reference class** ([`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md),
[`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md))
— and, in the column that is the point of the document, **whether each row's multiplicand was measured
or guessed.**

⚠ **The bill is ≥44–50 ms and the target is 15.6.** That is a gap of ~3×, it is a **target with a
number** rather than a defect, and the two levers between them — threading, and routing's multiplicand
— are both named and neither is measured. See *When to stop building and start fixing*, which is
evaluable for the first time as of 2026-08-16 and says **keep building**.

---

## What this is, and what it must never become

**A view, exactly as [`0000`](0000-board.md) is a view.** Every number here is owned somewhere else:
by [`spike-results`](../docs/spike-results.md), by a slice plan, or by
[`0002`](0002-open-questions.md). This file restates them and **cites the owner in every row**. When
it disagrees with an owner, the owner is right and this file is stale.

**It is not a ledger of open questions.** `0002` owns those, and a debt in two ledgers is the defect
[`0012`](0012-corpus-audit.md) exists to diagnose. What this file adds is the one thing no owner can
hold, because it is a property of the *set*: **the sum, how it moves with the target speed, and how
much of it is resting on numbers nobody has measured.**

**It exists because the corpus kept producing alarming figures and nothing added them up.** Each sat
in its own record, in its own currency, with its own caveat, and `0002`'s routing-share row had been
waiting on exactly this — *"cannot be ratified until the Tick's other consumers are priced."*

---

## The budget, and the ladder it sits on

✅ **The target speed is settled: 4× at 1,000,000 Citizens — 15.6 ms** ([`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md),
session **T**, 2026-08-16, [`plans/0027`](0027-session-t-the-target-speed.md)). **Every rung of `01 §1`'s
ladder is offered at every city size for ever**; a host that cannot sustain the rung the player chose
**dilates wall-clock time and says so**, and no rung is ever withdrawn. So the shares below have a
denominator at last, and **15.6 ms is the one to read**.

⚠ **The ladder priced here was the wrong ladder until this session, and that is worth a sentence.** This
table carried **8×**, which session P removed from `01 §1`, and had no column for **0.5×** or **3×**,
which it added. Both `06` and `plans/0000` were still quoting the retired set — *"8× / 4× / 2× / 1×"* —
a month later. ***A table of options is a fact stored in prose and drifts like any other.***

| Speed | Ticks/s | Budget | What it is for (`01 §1`) |
|---|---|---|---|
| ~~8×~~ | ~~128~~ | ~~7.8 ms~~ | **Not a rung.** Removed from the ladder by session P and priced here for a month afterwards |
| **4×** | 64 | **15.6 ms** | **The target.** *Getting somewhere, not watching* |
| 3× | 48 | 20.8 ms | Fast-forward that still shows a commute peak |
| 2× | 32 | 31.25 ms | Comfortable once a city is settled |
| **1×** | 16 | 62.5 ms | **The design speed** — the game must be enjoyable here. A Day is 2m08s |
| 0.5× | 8 | 125 ms | Watching one thing happen. Traffic is visually truthful here (`01 §7`) |

**The two speeds do different jobs and both are load-bearing.** **1×** is what a *capability* is priced
against — [`adr/0096`](../docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)
puts the Microscopic Cap there, and `adr/0105` did not move it, because a Cap is a world constant that
decides which Segments are exact while a budget is a wall-clock bill dilation can absorb. **4×** is what
the *bill* in this document is targeted at, because it is the rung a 1M city must still offer.

⚠ **This document may no longer say the speed is unargued**, and the sentence that did is struck:
~~*"Nothing argues that 4× at 1M is the requirement… typed **arguable** under `adr/0043` — a session can
close it and no measurement can."*~~ A session closed it. What the sentence got right is worth keeping:
no measurement could have, and **the ledger could not have chosen a rung either** — see *What the sum
says*.

### The machine, and it is half the budget

[`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md):
**a wall-clock budget names a machine class and a thread count, or it is not a budget.** This file's
figures always carried the class in one line and never carried it into a quotation.

> **15.6 ms on ONE CORE of the reference class** — a 2020 six-core x86-64 desktop, Intel i5-10400 class,
> DDR4-2133, `powersave` governor, single-threaded. **Quote the class with the number.**

Absolutes are upper bounds; ratios are unaffected. A `performance`, turbo re-capture is owed and no
verdict turns on it. ⚠ **The thread count is the clause most likely to be dropped**, because it does not
look like part of a duration — and `plans/0013` **lever 2** is precisely that everything here is
single-threaded while Tick phase 2 is parallel by construction. *A budget of 15.6 ms is meaningless
until somebody says 15.6 ms of what*, and `05 §6` has never said. That is session **R**'s.

---

## Unit costs — what is actually measured

Separated from the ledger because a unit cost survives its multiplicand being wrong, and these are
the durable half of the document.

| Unit | Cost | Flat in? | Owner |
|---|---|---|---|
| One Rule **evaluation** (`02 §4.3`'s bakery terms) | **82.84 ns** | yes — 1.8% over two decades | [`0011`](0011-rule-engine-bins-and-rules.md) task 9 |
| One **chain rung**, marginal | **53.6 ns** | — | as above |
| One **whole engine Tick** per due Rule, no term work | **121.6 → 198.3 ns** | **no — the sort** | `RuleTickBenchmarks` |
| ⚠️ **One whole engine Tick per due Rule, *in situ*** | **~552 ns** | — | [`0011`](0011-rule-engine-bins-and-rules.md) **finding 42** |
| ⚠️ **One Rule evaluation, *in situ*** | **~329 ns** | — | as above |
| One **Zone Rule trigger**, 16-Lot sample | **488 → 740 ns** | **no — but the variable is the working set, not the Zone**: 1.56× over a 1,000× Zone against a control that moved 989× | [`0014`](0014-zone-rules-and-the-sweep-family.md) task 9 |
| **Pollution diffusion**, one Cell dirty | **31.6 µs** | — | `MapLayerBenchmarks` |
| **Pollution diffusion**, whole map | **1.01 ms** | — | as above |
| One **State Hash** at 1M | **32.47 ms** | — | [S0a](../docs/spike-results.md) |
| **The terrain table's fold**, whole map | **1.89 ms** | — | `TerrainFoldCostTests`, milestone 24 task 2 |
| **A whole-world fold** on `minimal.toml` at 1,000 Citizens, **with terrain** | **2.08 ms** | — | as above |
| ⚠️ **One Tick with `VerifyDecideWritesNothing` ON** — the default — on `minimal.toml` at 1,000 Citizens | **0.37 ms against 0.06 ms with it off** — ⚠️ **was 4.14 ms against 0.03 ms, 138×, for one afternoon** | — | as above. ⚠️ **This is a GUARD's cost and not the city's**, and it must never be quoted as a Tick cost. 🔴 **The 138× was terrain: the guard folded `TerrainCellTable`'s 262,144 rows TWICE A TICK**, and it now folds `World.TablesAPhaseCanWrite` instead — a table no phase writes is not evidence about what Decide wrote. What replaced it is `WorldInvariants.TerrainIsUnchangedSinceItWasLaid`, at the end-of-run tier, which is `02 §10`'s frequency sorting applied to a check on a thing that cannot change |
| **One routing worst Tick** at 16 Trip starts | **~9.4–10.5 ms** (published as 10.37) | — | [S2 R5](../docs/spike-results.md), five pinned captures |
| ⚠️ **One Parking Shed query** at the shipped 400 m radius | ⚠️ **6.40 µs — PROVISIONAL, do not quote** | — | `ParkingArrivalStreamTests`, milestone 7 task 4. **Taken on a machine running a second `Borough.Tests` host**, so it is an upper bound of unknown tightness rather than a reading. Re-take when the repository is quiet |
| ⚠️ **Parking Shed, the worst Tick** at **64,000** Citizens | ⚠️ **1.58–1.63 ms — PROVISIONAL, do not quote** | ⚠️ **unknown, and the population is the point** — this is 64k against a budget denominated at 1M, so it may not be quoted as a share of a Tick until the same capture runs at the budget's own population | `ParkingArrivalStreamTests`, milestone 7 task 4. **Same contamination as the row above**, and it is derived from that row's µs figure, so the two are one reading and not two |
| ⚠️ **One walk search**, 128 m | **86.2 ns** | **no, and this is the finding — see below** | `WalkSearchBenchmarks`, 5b |
| ⚠️ **One walk search**, 512 m | **347.5 ns** | — | as above |
| ⚠️ **One walk search**, 1.02 km | **1.43 µs** | — | as above |
| ⚠️ **One walk search**, 2.05 km | **5.99 µs** | — | as above |
| ⚠️ **One walk search**, 4.10 km | **38.4 µs** | — | as above |
| One walk search, **one settled node** | **35–40 ns**, rising to 65 at 4.10 km | **yes, and it is the durable one** | as above |
| One walk **across the street** (same Segment) | **16.8 ns** | yes — flat at every rung | as above |
| One walk search, **severed** (no route exists) | **14.9 ns** | yes — flat at every rung | as above |
| ⚠️ **One job assignment pass**, steady state | **7.0 ms per pass at 100,000 Citizens** | **no — it is a burst, see below** | [`0023`](0023-jobs-and-the-commute.md) task 4 |
| ⚠️ **One job assignment pass**, cold start | **48 ms per pass at 100,000 Citizens** | — | as above |
| **One walk search, in a real world** | **~32.5 µs** | **no — it is an attribution, see below** | [`0023`](0023-jobs-and-the-commute.md) task 7 |
| ⚠️ **Commute generation**, in a departure Tick | **0.52 ms at 100,000 Citizens** | **no — it runs on a third of Ticks** | as above |
| ⚠️ **Segment volume attribution**, at 1M | **~320,000 increment/decrement pairs a Tick** | **no — the multiplicand is arithmetic and its premise moved** | [`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md), re-derived 2026-08-14 |

### ⚠️ Volume attribution is four times what its own ADR priced, and nobody changed its number

**No new measurement — a re-derivation, routed here by [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)
on the day milestone 5c task 6 found it.** `adr/0041` prices direct volume attribution at *order 80,000
increment/decrement pairs per Tick at 1M*, from *a vehicle crosses about one Segment per Tick* — and
states in the open what that follows from: **`TICKS_PER_DAY = 8192`** and a block-length Segment.

[`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)
moved that constant to **2048** and did not touch `adr/0041`. A Tick is now **42.19** in-world seconds
and a 128 m Street at 50 km/h takes **9.2**, so the crossing rate is **~4.6 Segments per Tick** — which
is `adr/0071`'s own *0.87 → 0.22 Ticks* illustration seen from the other side. The pair count is
therefore **~320,000 a Tick**, and S2 R2a's measured **0.79–0.83** was taken under the old clock, so it
scales the same way rather than correcting it.

⚠ **Do not read this row as a millisecond figure.** It is a **count of array writes**, not a price: the
inner loop is S4's **K2** (random gather by generational handle) into an L2-resident array, and nothing
has run it at this rate. ***A unit cost is a hypothesis until a real world has produced one***, and this
row has a measured multiplicand and **no unit at all** — the opposite failure from the routing row above,
which has a unit and a multiplicand that counts the wrong event.

**What this costs the corpus beyond the arithmetic** is that `adr/0041`'s crossover against the aggregate
scheme was discharged at **105 Ticks** on the old rate. The direction is favourable — a higher crossing
rate makes direct attribution *dearer*, so the crossover moves **down** — but the number is not 105 any
more and nothing has recomputed it. `adr/0041`'s revisit trigger is re-opened there rather than here,
because it is a decision and this is a ledger.

### ⚠️ The job assignment pass is a burst, and the interval is what concentrates it

**Measured 2026-08-12 by wall-clock delta on the headless runner — `rulesets/minimal.toml` with and
without its `[jobs]` table, at 100,000 Citizens, release build, `--no-decide-guard`.** Not
BenchmarkDotNet, so it is a **first measurement rather than a unit cost**, and it is filed here on
`adr/0073`'s rule that a cost goes to this document on the day it is found.

| Run | Without `[jobs]` | With | Delta | Per Tick | **Per pass** |
|---|---|---|---|---|---|
| 2,000 Ticks (cold start) | 5.19 s | 8.22 s | 3.03 s | 1.51 ms | **48 ms** |
| 20,000 Ticks | 9.10 s | 16.08 s | 6.98 s | 0.349 ms | 11.2 ms |
| the 18,000 in between (steady state) | — | — | 3.95 s | 0.219 ms | **7.0 ms** |

**The per-pass column is the one that matters and the per-Tick column is the one that will get
quoted.** The pass runs on `interval = 32`, so its whole cost lands in **one Tick in thirty-two**:
the amortised figure is comfortable and the Tick it actually runs in is not. Scaled linearly to 1M
Citizens that is **~70 ms in the pass Tick against a 15.6 ms budget — 4.5×** at steady state and
**~480 ms, 31×,** at cold start. `Aggregate.Peak` exists for exactly this shape (`02 §4`: *burstiness
is authored under this design*), and this is the first consumer in the ledger whose peak and mean
differ by the interval itself.

**Where it goes is not the search.** A seeker pays `CountIn` over the box once and `NthIn` plus a
walk search per candidate: at the shipped 20-minute Budget the box is a 14-Cell radius, so 841 Cells,
and `CountIn` + three `NthIn` is **~3,400 `int` reads before any routing happens**. Three walk
searches at the 1.67 km the Budget reaches are ~5 µs each by the curve above. So the box walk and the
routing are the same order, which is worth knowing before anybody optimises the obvious one.

**The cold-start figure is 6.9× the steady-state one and that is the mechanism working.** Everybody
is unemployed on Tick 0, so every drawn Citizen is a seeker and every seeker routes; at equilibrium
most looks land on somebody who already works and cost one handle resolve. **A pass whose cost falls
as the city settles is the opposite of the collections `adr/0006` watches for**, and it means the
number to design against is the transient rather than the trend.

> ⚠ **AMENDED 2026-08-13 — the paragraph above is right about the peak and wrong about the floor, and
> the run that refutes it had already been taken.** *"Falls as the city settles"* implies it falls
> **toward zero**. It does not: 5b-bis task 8's 100,000-Tick run reports `jobs beyond budget` **never
> reaching zero**, with **4,561 of 10,000 employed against 9,608 posts declared** — so ~5,400 Citizens
> are excluded by **distance rather than supply** and remain seekers permanently. Every one of them
> re-runs up to `candidates` walk searches every time the sample draws them, for ever.
>
> **So the pass has a standing cost proportional to the *permanently excluded* population, not merely a
> transient one**, and that floor is invisible in the wall-clock deltas above because they were taken at
> 2,000 and 20,000 Ticks — *before the exclusion had separated itself from the cold start.* The two
> measurements do not disagree; the second one simply runs long enough to show a term the first could
> not. ***A cost that decays and a cost that decays to a floor are different rows***, and only the
> second needs designing against.
>
> **What it is a floor of is a design question, not a tuning one**, which is why this is an amendment
> rather than a re-measurement: the excluded population is set by the Commute Budget against the map,
> and [`adr/0095`](../docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md)
> has since made the Budget **three rungs** with only the ceiling refusing — which moves this floor and
> has not been re-measured at any rung. [`adr/0097`](../docs/adr/0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md)
> is what makes the floor **countable**: it puts the reach failure on the Citizen, so the excluded
> population stops being an aggregate nobody can name. **It does not reduce the bill** — a count is a
> memory and nothing reads it — and the repair that *would* cut the work is that ADR's rejected
> Epoch-stamped option, which becomes correct when 5c makes the choice deterministic.
>
> ⚠ **Owed: re-take the wall-clock delta at ≥100,000 Ticks**, at each of `adr/0095`'s three rungs. The
> refuting number is a steady-state per-pass cost that does **not** flatten — which would mean the floor
> is still growing and the exclusion is not an equilibrium at all.

**Three things would move it, in descending order of how much.** The **Commute Budget** — the box is
its square, so halving the Budget quarters the box *and* quarters the search by the walk curve's own
squared law, which is the same *distance beats count* lesson the walk-search section states one level
up. **`interval` against `revisit_ticks`** — the product is fixed by the revisit period, so shortening
the interval spreads the identical work over more Ticks and cuts the peak without changing the mean.
And **`candidates`**, which multiplies the routing and not the box walk.

**Two caveats, stated because the row will be quoted.** These are wall-clock deltas on a whole
process, so they include the Census and the process's own noise; the run-to-run spread was ~8%, and
the deltas are 1.9–2.3× that. And the 1M figures are **linear extrapolations**, which the pass's own
shape supports — the sample is derived from the population and the box is not — but which no
measurement in this project has ever left intact.

### ⚠️ Commute generation is the first continuous per-Tick consumer this milestone added

**Measured 2026-08-12 by wall-clock delta on the headless runner, three Rulesets at 100,000 Citizens
over 20,000 Ticks, release build, `--no-decide-guard`.** Owed by `plans/0023` task 5 and not filed
until task 7 — `adr/0073` says route a cost on the day, and this one was two days late.

| Ruleset | What runs | Wall clock | Delta |
|---|---|---|---|
| **A** — no `[jobs]` table | nothing | **8.24 s** | — |
| **B** — `[jobs]`, with `[[building]] jobs = 0` | the pass samples and never finds a vacancy | **9.31 s** | **+1.07 s** over A |
| **C** — the shipped file | the pass hires, and the hired commute | **21.32 s** | **+12.01 s** over B |

**B is the isolation that makes this readable, and it exists because `jobs = 0` loads.** A Ruleset that
grants no posts still runs the whole pass — the sample, the box, the candidate draw — and never routes,
because `World.HasJob` fails before `WalkRouting` is reached. So **B − A is the cost of looking with
nothing to find: 0.054 ms a Tick, 1.7 ms in the one Tick in thirty-two the pass runs in.** That is the
`interval` machinery on its own and it is cheap.

**C − B is routing, and it is two consumers that this method cannot separate.** Over the run the Census
counts **262,125 assignment routes** (166,704 that hired plus 95,421 refused for length — every
candidate that had a vacancy) and **107,602 commute Trips**, each one search. Attributing the 12.01 s
across all 369,727 gives **~32.5 µs a search in a real world**, which is the first walk-search figure
taken from anything but a microbenchmark — against `0013`'s own **14.9 ns** severed and sub-µs
connected rows, which were BenchmarkDotNet over a warm graph. ***A unit cost is a hypothesis until a
real world has produced one***, for the fourth time in this ledger.

**On that attribution, commute generation is 29% of the routing — 3.50 s, 0.175 ms a Tick amortised.**
Amortised is the wrong number to design against, because departures are spread over
`ceil(8192 / commute_peak_factor)` = **2,731 Ticks of a 8,192-Tick Day**, so the work lands on **one
Tick in three** and the departure Tick carries **~0.52 ms**. Scaled linearly to 1M Citizens that is
**~5.2 ms in a departure Tick against a 15.6 ms budget — 33%, on a third of all Ticks**, and unlike the
assignment pass it does not fall away as the city settles: everybody with a job commutes every Day for
ever.

**Two things this does not say.** It is a **delta between whole runs**, not a profile, so the split
between the two routing consumers is an attribution by count rather than a measurement — if a commute
search is systematically longer than an assignment search (it may well be: the assignment pass rejects
by cost and therefore stops early more often), the commute share is **understated**. And the levers are
the ones `adr/0081` already names: `candidates` multiplies the assignment side and nothing else,
`commute_peak_factor` moves the peak against the mean without changing the total, and **the Commute
Budget moves both**, because it is the box's side as well as the acceptance test.

### 🔴 ⚠️ The Decide guard costs 37.9 ms a Tick on a paved world, and it was measured and filed as if it were the city

**Measured 2026-08-21, `rulesets/bordered.toml`, release, 256 Ticks against a 1-Tick baseline, on the
reference machine but ⚠️ NOT ON A QUIET ONE — upper bounds.** ⚠️ **This entry replaces one filed the
same day that said *a gated world costs 38.7 ms a Tick and the cost does not move with population*.
Both halves of that sentence were wrong**, and what was actually measured is below.

| What ran | Per Tick |
|---|---|
| `bordered.toml`, guard **on** (the default) | **38.40 ms** |
| `bordered.toml`, `--no-decide-guard` | **0.51 ms** |
| `minimal.toml`, `--no-decide-guard` | **0.16 ms** |

**`Simulation.VerifyDecideWritesNothing` is the whole of the gap.** It calls `FoldEverything()`
**twice per Tick** — a complete State Hash fold of the world, before and after phase 2 — to check
`adr/0037`'s claim that Decide writes nothing, which is what lets every entity table be
single-buffered. On a world with **535,817** Segments a fold is **~19 ms**, so the guard is ~37.9 ms
and the simulation under it is **0.51 ms**.

⚠️ **The guard's own doc comment says this and has since it was written**: *"`O(world)` against a
phase that is meant to be `O(woken)` — affordable for a correctness run and not for a long one. Turn
it off for the 100,000-Tick test and leave it on everywhere else."* ***The defect was not in the
build; it was in reading a stopwatch and not asking what it was over.***

🔴 **And the population claim inverted once the guard was off.** Under the guard, 100, 1,000 and 4,000
Citizens all read the same, which is what made *independent of population* look like a finding. Without
it:

| Citizens | Per Tick |
|---|---|
| 100 | **0.59 ms** |
| 1,000 | **0.66 ms** |
| 4,000 | **1.09 ms** |

A fixed floor of roughly half a millisecond — the map — plus a term that grows with the city, which is
exactly the shape a Tick is supposed to have. ***A constant that swamps a signal makes every input look
like it does not matter***, and *does not move with population* is what that reads as from outside.

**Two things this leaves for somebody else.** ⚠️ Every other timing figure in this ledger was taken
with `--no-decide-guard` — the commute-generation entry above names it in its own method line — so the
withdrawn entry was **the only one in the file measuring a different thing**, filed beside the rest
without saying so. And ⚠️ **nothing tells an operator the guard is on**: a long run on a large world is
~75× slower than it needs to be and the runner says nothing, which is a `HONEST DEGRADATION` gap rather
than a cost. **Milestone 11 task 9's acceptance run must pass `--no-decide-guard`**, on the guard's own
instruction.

⚠️ **Still not a row.** The 0.51 ms is a whole-Tick wall clock on one world, not a consumer with a
multiplicand, and the fold cost belongs to the *hash* rather than to any phase. What a row needs is
still owed.

**And what a Pool under pressure costs, measured 2026-08-21 at milestone 11 task 9.** Same conditions,
same caveat — release, reference machine, **not quiet**, upper bounds — 16,384 Ticks, 1,000 founding
Citizens, `--no-decide-guard` throughout, one knock on each of four gates a Day:

| What ran | Per Tick |
|---|---|
| `bordered.toml`, no arrivals asked for | **0.51 ms** |
| `bordered.toml`, 48 Households a Day admitted | **0.85 ms** |
| `crowded.toml`, 384 Households a Day admitted | **2.16 ms** |

⚠️ **The map floor is the same 0.51 in all three** — both files pave the lattice to the boundary
because both declare a gate — so what the two lower rows price is **arrival, placement and departure
churn**, and it is roughly linear in the admitted rate. ⚠️ **`--citizens` is nearly inert here**: at
100, 400 and 1,000 founding Citizens `crowded.toml` reads **1.53 / 1.94 / 2.15 ms**, because within
four Days the Pool is ~1,470 whatever the world started with. ***The multiplicand is the arrival rate
and not the population***, which is the one thing this file's numbers are for.

⚠️ **Staggered invariants are 4% of it** — 33.83 s against 35.33 s over 16,384 Ticks with
`InvariantRegistry.RunStaggered` commented out — so they are not where a repair would go.

⚠️ **Still not a row either, and for a sharper reason than the one above.** The dominant term is
`[placement]`'s cadence meeting a Pool of ~1,470: 64 passes a Day, each drawing a sample that scales
with the Pool, three candidates each. **The Pool's steady-state size is set by `gives_up_after_days`,
which `plans/0002` §D1 records as UNRATIFIED** — so the multiplicand here is downstream of a number
nobody has settled. ***A cost derived from an unratified quantity is a cost that moves when the
designer answers a different question.***

### 🔴 ⚠️ The land value target pass costs 88 seconds a Tick on a fully paved world, and it is the noise query underneath it

**Measured 2026-08-22, `rulesets/bordered.toml`, 4,000 Citizens, release, per-Tick timings over 64
Ticks. ⚠️ NOT ON A QUIET MACHINE — a second session was running — so every figure here is an UPPER
BOUND**, which is the one thing a spoiled measurement is still good for.
**`tests/Borough.Tests/Space/SealingCostTests.cs` is the instrument and it prints all of it.**

🔴 **Land value's target pass is not a per-Tick cost at all — it is one Tick in 256 costing more than a
minute.** `MapLayers.Schedule` puts it at every 256 Ticks, offset 16, so in a 64-Tick window exactly one
Tick pays it:

| Tick | Per Tick, `bordered.toml` |
|---|---|
| any other Tick | **50–90 ms** |
| **Tick 16** — `SetLandValueTargets` | 🔴 **88,085 ms** |

⚠️ **It is NOT the Decide guard.** The row above this one is the reflex answer and it is wrong here:
`--no-decide-guard` on this world changes the total by **1.0×**. The guard folds a big world; this
folds nothing.

**The chain, and every link was measured rather than reasoned:**

| Term | Figure |
|---|---|
| `LayerCellTable` live rows after world creation | **262,144** — the whole 512×512 map |
| desirability samples per Cell (`DesirabilitySamplesPerAxis` 2×2) | **4** |
| `LineSourceQueries.Noise` calls in one pass | **1,048,576** |
| off-lattice Segments, scanned **twice** per call | **12,581** |
| off-lattice visits in one pass | 🔴 **26,384,269,312** |

🔴 **The cause is `LineSourceQueries.Level` scanning every off-lattice Segment in the world, and its own
doc comment says the scan is deliberate.** `StreetGrid.OffLatticeCount`'s remark rests it on
[`adr/0014`](../docs/adr/0014-grid-streets-with-freeform-arterials.md)'s *grid plus sparse
Arterials*: ***"It is a linear scan on purpose … adr/0014's layout is what makes the set small."***

⚠️ **The premise was true when it was written and a later feature falsified it silently.** The
off-lattice set was the Arterials, and `arterial_count` is **16**. A **foot path is off-lattice too**,
and `[roads] foot_paths_per_thousand_blocks = 40` is a rate **per block** — so the set grew with the map.
Of the 12,581, about **10,500 are foot paths**. ***A premise cited as an implementation strategy is a
premise that has to be re-checked whenever anything joins the set it describes***, and nothing re-checked
it — the enumeration defect this corpus keeps finding, arriving in a *performance* argument rather than
in a list.

✅ **FIXED 2026-08-22 — the off-lattice set is now bucketed by block and the window walks it.**
`StreetGrid` files each off-lattice Segment under the block of its **midpoint** and records
`OffLatticeReachBlocks`; `Level` widens its existing window by that reach and walks the buckets.

| | Tick 16 |
|---|---|
| before | **88,085 ms** |
| bucketed by endpoint | **10,233 ms** |
| bucketed by **midpoint** (shipped) | **6,578 ms** |

***13.4× and the answer does not move.*** ⚠️ **It is hash-preserving and the argument is not "it looked
the same"**: `Contribution` returns zero beyond `source.Range`, so a Segment outside the window
contributes zero as the background AND zero through `Above`, which compares it against that same zero.
The identity of `local` therefore only matters while it is *in* range, and in range it is in the window.
`LineSourceQueryTests` and `DesirabilityTests` pass unchanged — 20 assertions.

🔴 **What is NOT fixed, and it is the bigger half.** **6,578 ms is still 421× the 15.6 ms budget**, and
no per-query optimisation closes that: the pass is **1,048,576 queries** and the budget would allow about
**15 ns each**. ***The defect that remains is the pass, not the query.*** `SetLandValueTargets` walks
**every live Cell row every 256 Ticks**, and `02 §10`'s own rule — *`O(1)` at the write site per Tick,
`O(n)` **staggered**, whole-world at end of run* — says a whole-world sweep is the one shape it should
not have. Staggering it to `1/256` of the Cells per Tick gives **4,096 queries a Tick ≈ 25 ms**, which is
the right order and still needs the query faster.

⚠️ **Staggering moves the State Hash and is therefore a design change under `05 §4`, not an
optimisation** — which is why it is filed here and in [`0002`](0002-open-questions.md) rather than
done. [`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md)
makes the cadence **the designer's number**, and *when* a Cell retargets is that number.

#### ✅ AMENDED 2026-08-23 — **the pass is 80 ms and no stagger was needed, because the remaining defect was the query after all**

⚠️ ***The paragraph above is superseded and is kept because its reasoning was wrong in a way worth
keeping.*** It concluded *the defect that remains is the pass, not the query* from one division —
6,578 ms over 1,048,576 queries — and that division assumes every query has to do the work it did.
It did not.

**What the query was doing.** `LineSourceQueries.Level` runs two passes. Pass two prices each Segment
through `Contribution`, which returns zero in two array reads when the Segment carries no Vehicles.
**Pass one does not**: it finds the nearest Street by resolving two node handles and projecting a
point for *every* Segment in the window, and only afterwards asks about volume. ***So the expensive
half of a traffic query is the half that never looks at traffic.***

**And where nothing within range carries Vehicles the whole query is provably zero** — `background` is
zero, every `Above` compares its own zero against that zero and adds nothing, and `Log1P(0)` is zero.
The answer cannot depend on *which* silent Segment was nearest, so pass one need not run at all.
`TrafficPresence` stamps the blocks within range of a Vehicle-carrying Segment in **one linear scan of
the Segment table**, and `Level` consults it before pass one.

| `SetLandValueTargets`, `rulesets/bordered.toml`, 4,000 Citizens, 262,144 Cell rows | |
|---|---|
| before | **7,353 ms** |
| `TrafficPresence.Rebuild` | 4 ms |
| after | ✅ **80 ms** |

***85×, and hash-preserving on the same argument as the bucketing above*** — a skipped query returns
the zero it would have computed. The presence map is **keyed on the range it was built for and refuses
to answer for any other**, so noise and near-road pollution cannot share one by accident; a refusal
falls back to the full scan, which is slow and right.

🔴 ⚠️ **THE FIGURE ABOVE IS THE COST OF COMPUTING A FIELD THAT IS ZERO, AND THAT IS NOT A CAVEAT ON
THE SPEED-UP — IT IS ONE ON THE WORLD.** `rulesets/bordered.toml` at 4,000 Citizens peaks at **five
Vehicles in motion in a whole Day**, so almost every Cell takes the early-out. **Why that world is
empty is an open question** and is filed at [`0002`](0002-open-questions.md) §B — `congested.toml` at
16,000 Citizens peaks at **937**. ***Until that is answered, no row in this file measured on
`bordered.toml` is a measurement of a city with traffic in it***, and this one least of all: it is the
row whose cost the traffic decides.

**What this does to the stagger.** It does not answer it; it unblocks the work that was waiting on it.
The whole-world sweep is still the shape `02 §10` names as wrong, and a Cell still retargets on the
designer's cadence rather than a profiler's. What has gone is the *urgency* — 80 ms on one Tick in 256
does not make the assertion tier unusable, so **the stagger can be decided on its merits rather than
under a performance gun**, which is the condition
[`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md)
wanted for it in the first place.

⚠️ **`adr/0044` contradicts itself about who owns a stagger and this was noticed here rather than
settled.** Its Consequences bullet 1 says *"Two multipliers remain available to a profiler —
coarseness … and the stagger's phase"*; bullet 3 says *"The staggered offsets are hash-bearing too …
a design change a designer may make."* A per-Cell stagger moves only phase and never a period, so the
two bullets sort it opposite ways. **Filed, not resolved** — it is an ADR amendment and not a budget row.


⚠️ **This was latent, and Sealing did not cause it.** The pass is `O(live Cell rows)` and a row existed
only where something emitted pollution — one shipped Ruleset in ten. ***The Cell table's sparsity was
load-bearing and stated nowhere***, so eight worlds ran this loop over zero rows and it was free. Giving
Sealing a write path made the table dense and the latent cost arrived all at once. **Map-wide pollution
would have tripped exactly the same wire.**

### ⚠️ The walk search is not a unit cost, and the row that treated it as one hid the stronger lever

**Measured 2026-08-11 on the shipped 32-Tile lattice — 16,700 nodes, 32,890 Segments, which *is* the
million-Citizen city's graph rather than a scaled-down stand-in.** Zero allocation on every path at
every rung.

**The five rows above are one function, not five measurements**, and the spread is **446×** across
distances a person actually walks. The mechanism is measured rather than inferred: the search settles
**2, 3, 10, 40, 149 and 591** nodes at 1, 2, 4, 8, 16 and 32 blocks, which is **~4× per doubling** —
Dijkstra over a 2-D lattice settles a *disc*, so settled nodes go as the **square** of the distance,
and cost per settled node is flat at 35–40 ns. **So the durable unit here is the settled node, and
the walk search's cost is `≈ 37 ns × (distance)²`.** At 4.10 km it touches 591 of 16,700 nodes — 3.5%
of the city — so the early exit works and this is not an exhausted search.

**The ledger's 4–20 µs was not wrong so much as it was a point on a curve, and it is the point at
1.5–2.5 km.** Below 1 km the search is an order of magnitude cheaper than the low end; at 4 km it is
twice the high end. **A single number for a quantity spanning 446× is a row that cannot be checked**,
and every share it produced was really a statement about a walk-length distribution nobody wrote down.

***A route count is the multiplicand everybody reached for, and the distance distribution is the
stronger lever nobody named.*** Halving the mean walk length **quarters** the bill; halving the route
count halves it. That is the same shape as `foot_paths_per_thousand_blocks` turning out to be a
stronger Severance dial than `foot_crossing_every` — **the named parameter was not the one that
moves the outcome**, twice in two slices, in the same subsystem.

**⚠️ And the severed case is the cheapest query in the model, which is backwards from the intuition
the design was built on.** *No route exists* reads like the worst case — an exhausted search over a
whole component — and it is instead **14.9 ns, flat**, because union-find components over the foot
subgraph answer it by comparison. It is **~2,600× cheaper than a 4 km walk**. The severed Trip is the
one milestone 5b exists to report, so this is a cost the Trip generator does **not** have to design
around, and `--trips` can report Severance for nothing.

**Two caveats, stated because the row will be quoted.** The run was **in-process**
(`--inProcess`), because BenchmarkDotNet refuses to build its harness while a git worktree inside the
repo supplies a second `Borough.Tests.csproj`; the controls are flat across all six rungs and the
error bars are ~1%, so the numbers are self-consistent, but **an out-of-process re-run is owed** once
worktrees move outside the repo root. And these are **one draw** of the Arterial polyline, at the
suite's own seed — which is the draw that strands seven walkable nodes, the one seed in eight that
strands any.

**The two marked rows are the same quantities measured in a running city rather than in a fixture,
and they are 2.8× and 4.0× the synthetic ones.** They arrived after slice 7 task 10a put a Ruleset in
a 1M world for the first time, and they are the reason the *measured / guessed* column below is no
longer the only thing in this document a reader should be suspicious of. **This table's left-hand
column was never audited the way the multiplicand column was**: a fixture is best case on every axis
nobody was thinking about, and the Rule engine's was best case on three at once — no terms, every
Rule due in one bucket walked in slot order, and no Citizen or Household table competing for cache.
A unit cost is a hypothesis until a real world has produced one. **Routing's 10.37 ms came off a
synthetic harness too and has never met a world.**

> **S2 R7 briefly recorded 10.37 ms as having no artefact at all; that was retracted the same day**,
> when the re-run it prompted found the figure sitting in a table as `10370.13 µs`. The sweep's
> matcher had compared rendered strings, and this document renders in milliseconds where the harness
> prints microseconds. **The unit is measured.**
>
> **What the re-run did establish is that the row's precision was never real.** Seven captures now
> stand behind the cell and the five correctly pinned ones read **9.37, 9.45, 9.51, 10.37 and
> 10.51 ms**. The figure is a **maximum over 256 Ticks**, and a maximum quoted to two decimals in a
> budget row claims a precision a maximum does not have — so the row above now reads **~9.4–10.5 ms**.
> At 4× that is **60–67%** of the budget rather than a point estimate of 66%, which does not move the
> ledger's verdict and does change what may be said about it.
>
> **The row's weakness was always the multiplicand, and that has not improved.** R6.3 found it counts
> **the wrong event entirely** — under static Habit a Trip start is a *lookup*, and the expensive
> event is a **diversion**. A row whose multiplicand is wrong in kind cannot be rescued by a sound
> unit, so this document's dependence on the row stays qualitative. **What the episode adds is about
> this table's own auditing**: the *measured / guessed* column audits the multiplicand, `0011`
> finding 42 showed the unit column was never audited, and an attempt to audit it produced a false
> accusation before it produced a correction. **Three passes over one row, two of which found
> something and one of which invented something.**

**The gap was then attributed rather than left as a warning** (`0011` finding 43). Added to the
fixture one at a time at a fixed due count: **terms ×1.84**, **scatter ×1.49**, **population ×1.14** —
a product of **3.13×** against an observed **3.70×**, with the residual most likely the rest of the
term axis, since the instrument's balanced Rule cannot apply anything. **Terms are the largest axis
and were expected to be the smallest.** Two of the three are properties of a real city and cannot be
optimised away; the term axis is code, and it is `World.FindBin` searching a Building's intrusive Bin
list once per term per evaluation.

**The lesson has a mirror image, and session M walked straight into it three days later.** This
document's warning is *a measured multiplicand against a guessed one*, and the paragraph above adds
*a unit cost is a hypothesis*. Session M then produced **872% of the routing budget** for a proposed
Habit refresh — a **measured unit** (112 route computations per Tick, R7's re-capture) times a
**guessed multiplicand** (one Habit per Citizen at 1M, from nothing), and treated it as a reason to
discard a mechanism. It is the same defect as the Bin Rule row wearing the opposite face, and the
repair is not *stop estimating*: it is **type the two halves of an estimate separately before acting
on it.**

> A **shape** claim — *a rotation costs `store size ÷ period`, so it couples the learning rate to the
> population* — is arithmetic about the mechanism. It holds at every store size, needs no measurement,
> and is `arguable` under `adr/0043`. **That is what a design decision may rest on.**
>
> A **magnitude** claim — *872%* — is the shape multiplied by numbers, and it inherits the softest one.
> It is `measurable`, and until the multiplicand exists it may be used to *decide what to measure* and
> never to decide what to build.

Session M's outcome survived because the shape claim was sufficient on its own: the mechanism chosen
is bounded by a quantity already in the budget *whatever the multiplicand turns out to be*, which is a
property, not a number. **Prefer the option that is robust to your own estimate being wrong** — that
is the one line of this worth carrying to the next row in the ledger.

**The one non-flat unit is the intent sort, and it was found by looking for it.** Phase 3 sorts its
intents into the settle order, which is `O(n log n)` where everything else in the engine is linear.
Measured across two decades of due count the per-Rule cost rises **121.6 → 146.3 → 198.3 ns**, a
**1.63×** rise where `log₂` predicts **1.7×**. So the term is real, it is identified, and it is not a
blow-up: it costs about 63% more per Rule at 100,000 due than at 1,000.

---

## The ledger

At **1,000,000 Citizens**, on **one core of the reference class** ([`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)).
**Milliseconds first and the share follows**, per this file's own rule; the ladder is summed once
beneath the table rather than as four columns. **A row's share is only as good as its multiplicand**,
which is why that column sits next to it rather than in a footnote.

> ⚠ **RE-SUMMED 2026-08-16 by session T, and the correction was already written down in two other
> files.** This table priced routing at 9.4–10.5 ms and summed to ≥17.8 ms.
> [`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)
> quartered the Day; route searches fire per Trip and Trips are daily, so the routing row multiplies by
> **four**. `01 §1` states this and `CLAUDE.md` carries it; **the ledger that owns the sum never applied
> it.**
>
> ⚠ **And it is not merely stale against another document — it is inconsistent with itself.** The
> *Segment volume attribution* row two tables up was re-derived on **2026-08-14** by exactly this
> reasoning, ~80,000 → ~320,000 pairs a Tick, on the same clock change, in the same file.
> ***A premise that expires retires every site resting on it, and finding one of them is not finding
> them*** — 5c task 6's own finding, committed by the document that recorded it, nine days later.
>
> ⚠ **Carry the derivation, not the digit.** `01 §1` quotes ~47.6 ms. That is *≥17.8 ms with the routing
> row multiplied by four*, and the honest form is the band below, because ≥17.8 was itself a floor. A
> maximum multiplied by four is a coarse instrument — the routing unit is a **maximum over 256 Ticks**,
> so the burst it already contains is not obviously four times burstier. **The direction is certain and
> the factor is approximate.**
>
> ⚠ **The walk-search row plausibly moves the same way and nobody has done it.** Its multiplicand is 464
> routes; if those are commute-derived they are daily too. **Not applied here**, because applying a
> factor to a row whose provenance has not been checked is how the volume row's premise expired
> unnoticed in the first place. Filed as owed.

| Consumer | Phase | Cost/Tick | Multiplicand | Share at 4× |
|---|---|---|---|---|
| **Skeleton, staggered invariants, Layer schedule** | all | 0.112 ms | 1M rows — **measured** | 0.7% |
| ~~**Bin Rule engine**, whole Tick, before term work~~ | ~~1–3~~ | ~~10.42 ms~~ | ~~56,250 due — **guessed**~~ | — |
| **Bin Rule engine**, whole Tick, **in situ** | 1–3 | **6.4 ms** | 11,586 due — **measured, on a toy Ruleset** | **41%** |
| ⚠️ **Routing** | 4 Move | ~~9.4–10.5 ms~~ **37.6–42.0 ms** — unit **measured**, a *maximum*, **×4 for the shipped clock** | ~~16~~ **64** Trip starts — **guessed, and the wrong event** | **241–269%** |
| **Microscopic Lane model** | 4 Move | **27.4–29.3 ns a Vehicle** — unit **measured** (S5 L5), a `powersave` **lower bound** | **the Microscopic Cap — unset** | — |
| ⚠️ **Walk search** (pedestrian Legs) | 4 Move | **0.04 → 17.8 ms** — unit **measured**, a **curve in trip distance**, not a number. ⚠ **The ×4 above is not applied here and may be owed** | 464 routes — **guessed**, *and the distance distribution is a second guess nobody had named* | **0.3–114%** |
| ⚠️ **Map Layer diffusion**, on the Tick it lands | 5 Layers | 0.03–1.01 ms ⚠ **STALE — measured at a 128-Cell map; the map is 512** | dirty region — **measured range** | 0.2–6.5% ⚠ |
| ⚠️ **Land value producer** (`MapLayers.SetLandValueTargets`) | 5 Layers | **UNMEASURED.** Four `Desirability` calls a resident Cell, on one Tick in 256 — each one Cell-Layer read plus one line-source query over a 300 m window ([`adr/0126`](../docs/adr/0126-a-cell-samples-desirability-at-its-quadrant-centres-and-a-line-sources-area-mean-does-not-converge.md)) | ⚠️ **resident Cells — OBSERVED in one world and ZERO in the other eight.** *(Was: guessed at zero, because nothing in the build creates a Cell row but a pollution emission and no shipped Ruleset emitted any. **[`rulesets/fouled.toml`](../rulesets/fouled.toml) does**, from milestone 9 task 7.)* **163 Cell rows at 1,000 Citizens** over 100,000 Ticks, and **about 262 at 4,000**. ⚠ **That is a count and not a price** — the per-call cost is still **UNMEASURED**, and a count taken on the one world that emits says nothing about the world a real Ruleset would build. ***A row whose multiplicand is zero because the world is empty is not a cheap row, it is an unpriced one***, and it is now unpriced with a number beside it | — |
| **Zone Rules**, worst aligned Tick | 6 Growth | **0.012 ms** | 16 Rules triggering together — **guessed**; unit **measured** | **0.08%** |
| ⚠️ **District re-evaluation and the Pool reprice** | 6 Growth | **UNMEASURED, and this row exists to say so.** Two cadenced whole-table passes landed at milestone 12 and neither was priced: `DistrictWatershed.Evaluate` on `[districts] revisit_ticks`, a watershed over **every built Cell** of the world, and `World.RepriceDistrictPools` on a `Ticks.PerDay` boundary, one pass over one row per Good per District. ⚠ **They are not the same size** — the reprice is a handful of rows and the watershed scales with the built city — so ***one row for both is a placeholder and not an estimate*** | `CellGrid.WorldCellCount` for the first, Districts × Goods for the second — **neither measured, and only the second is small by construction** | — |
| **Event Wheel, general** | 1 Wake | **unbuilt** — slice 9 | — | — |
| **Commit** | 7 | **unbuilt** | — | — |
| | | **≥44–50 ms** | | **≥283–318%** |

**The bill across the ladder, which is one number read five times.**

| Rung | Budget | Share of ≥44–50 ms |
|---|---|---|
| 0.5× | 125 ms | 35–40% |
| **1×** — the design speed | 62.5 ms | **71–79%** |
| 2× | 31.25 ms | 141–159% |
| 3× | 20.8 ms | 212–238% |
| **4×** — **the target** | **15.6 ms** | **283–318%** |

> **⚠ The ladder table is one number read five times — carry the bill, not the percentage.** 35–40%
> through 283–318% is **≥44–50 ms** divided by five budgets. Nothing about the simulation changes down
> that column; what changes is a speed rung. **Every percentage in this file is a measurement over a
> decision, and only the measurement is a fact about the code** — which is why the rungs are now a
> separate table beneath the bill rather than four columns inside it, where they read as five different
> facts.
>
> This is not a stylistic preference. On 2026-08-13 the corpus's largest figure —
> [`adr/0061`](../docs/adr/0061-a-diversion-rejoins-by-local-descent-and-a-rejoin-is-never-a-search.md)'s
> **861.87%** — survived two independent changes to its own denominator to within 0.4%, because they
> happened to move in opposite directions, and **nothing could see it** while the number was carried as a
> percentage. Held as **134.135 ms** the same two changes are visible immediately: one of them multiplies
> the bill and the other does not touch it. *A percentage hides which side moved.*
>
> So: **state the milliseconds first and let the share follow.** Where a row above gives only a share,
> that is a defect in this file, not a shorthand. ⚠ **And state the machine and the thread count with
> it** ([`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)),
> which is the same rule one axis over: a bill without a host is a percentage without a denominator.

**Read the ladder across, not down.** The headline is no longer *fits at 2×, does not fit at 4×* — that
sentence was written against ≥17.8 ms and a speed nobody had chosen. **Stated as a bill it is one
sentence: the simulation as priced costs ≥44–50 ms a Tick on one core of the reference class, and the
target rung gives it 15.6.** The gap is **~3×**, it is a target rather than a defect, and
[`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md)
sets it knowing the number.

**⚠ The row carrying most of that sum is still the weakest one in the table, and now by more.** Routing
is **37.6–42.0 ms of the ≥44–50** — **85%** of the whole bill, where before the re-sum it was 55%. Take
it out and the ledger reads **6.6–8.0 ms**, which fits the target rung with room to spare. So the
headline is **a statement about routing and essentially nothing else**, more completely than when this
paragraph was first written. Its unit is measured, is a **maximum** rather than a mean, and spans
9.37–10.51 ms across five pinned captures before the ×4; its **multiplicand counts the wrong event** —
R6.3 found that under static Habit a Trip start is a lookup and the expensive event is a **diversion**,
priced at **134.135 ms** on its own. **So the one correction with a known direction points sharply up**,
and it points up on the row that is now five-sixths of the answer.

> ⚠ **The ×4 was in this file all along, attached to a different number.** The note directly below has
> said since 2026-08-13 that `adr/0094` *"multiplies every routing count by 4"* — correctly, in a
> sidebar about `adr/0061`'s 861.87% — and the routing **row** three paragraphs above it was never
> touched. ***A caveat attached to a number does not travel with it*** ([`plans/0012`](0012-corpus-audit.md)
> **Cause 5**) has a sibling: **a correction attached to a number does not travel either**, and this is
> the first sighting where both copies live in **one file**. The tell is that no cross-document check
> could have found it, because there was no second document to disagree with.

> **⚠ 861.87% is right by cancellation, 2026-08-13 — and this table has already recorded once what that
> costs.** The figure is a bill of **134.135 ms** at R8's rung over the **15.6 ms budget at 4×**. Both
> terms have since moved in opposite directions:
> [`adr/0096`](../docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)
> holds that a capability is priced at the design speed's **62.5 ms** — 214.6% — and
> [`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)
> multiplies every routing count by **4** — back to **858.5%**. Within 0.4% of where it started, for two
> reasons neither of which was the original one.
>
> **That is the second sighting of this table's own lesson.** The Bin Rule row was *right by
> cancellation* — a unit 2.8× too low against a multiplicand ~5× too high — and this note says of it:
> ***which is worse than being wrong, because any change moving one factor without the other would have
> gone unnoticed***. Exactly that happened here, twice, in two days, and nothing flagged it. **The repair
> is to carry the bill and not the percentage**: 134.135 ms is a measurement, 861.87% is a measurement
> divided by a product decision. `plans/0012` **Cause 5**, and the disqualifier registered for this
> figure is its **denominator**.

~~**The sum fell from ≥21.8 ms to ≥17.8 ms — ≥140% to ≥114% at 4× — and that is not good news.**~~
**The sum then rose to ≥44–50 ms, and the history of the figure is more instructive than any of its
values.** It went **≥21.8 → ≥17.8** when the Bin Rule row stopped being a guess and the correction
happened to point down — while the *unit* underneath it moved **up** by 2.8×, so the fall measured how
much slack there had been rather than any improvement. It then went **≥17.8 → ≥44–50** when session T
applied a clock change this document had been carrying in a sidebar for three days.

⚠ **Neither move was a measurement of the code.** One was a fixture being replaced by a city; the other
was arithmetic nobody had run. ***The sum has never once moved because the simulation got faster or
slower***, and a reader watching this row for engineering news has been watching the wrong thing for its
whole life.

**The sum is now short by a *named* hole rather than by an absent one, and that is the change S5
made.** Until 2026-08-11 this table had **no row for the Microscopic tier at all** — not even
*unbuilt*, which the Event Wheel and Commit both get — so the movement subsystem was priced in halves:
routing carried 60–67 points at 4× and the Lane model carried nothing, and nothing in the document
said so. The row above contributes **no share**, because its multiplicand is the Microscopic Cap and
the Cap is unset. **That is a gap and not a debt** (`adr/0052`'s distinction): nothing accretes on a
value that does not exist. What it now does is make the absence visible to anyone reading the sum.

> **⚠ The walk-search row's span is the whole point of it, and it must not be collapsed to quote.**
> Its low end assumes a 128 m mean walk and its high end 4.10 km, and the arithmetic between them is
> `464 × cost(distance)`: **0.04 ms** at 128 m (0.3% at 4×), **0.16 ms** at 512 m (1.0%), **0.66 ms**
> at 1.02 km (4.2%), **2.78 ms** at 2.05 km (17.8%), **17.8 ms** at 4.10 km (**114% on its own**). The
> unmeasured product this row replaces — 1.9–9.3 ms, 12–60% — is the band at 1.5–2.5 km, so **the
> question was never *what does a walk search cost* but *how far do people walk*, and no document
> asks it.** A realistic pedestrian trip is well under a kilometre, which puts the row nearer **1%**
> than 60%; a model that walks people across town puts it over budget by itself. **Nothing in the
> corpus constrains this**, and it cannot be constrained before a Trip generator exists — which is
> `0002` §A, and is why 5b ran §B first.
>
> **⚠ The unit above moved 1.64× on 2026-08-11, and not because the kernel changed.** S5's L5 found
> `IntegerMath.FloorDiv` evaluating its modulo unconditionally, so **every `Fixed.Div` in this project
> was two 64-bit divisions**. Correcting it is bit-identical — 1,060 tests green, no golden baseline
> moved — and took the Lane model from 47.3–48.0 ns to **27.4–29.3 ns**.
>
> **The other rows were then checked rather than assumed, and they are essentially unmoved.** An
> earlier revision of this note said the Bin Rule engine's **6.4 ms** and S0b's **8.72 ms** were
> *"upper bounds by an unknown amount"*. **The amount is now known and it is ~1%.** Measured by the
> two-point slope S0b itself uses — 200,000 Citizens under `rulesets/minimal.toml`, 2,000 against
> 8,000 Ticks, best of three, both trees built Release and pinned to the same core pair — the per-Tick
> cost reads **0.9483 ms before and 0.9383 ms after: 1.011×**.
>
> **The blast radius is narrow for a legible reason: the defect's cost is proportional to how
> division-dense the consumer is.** The Lane kernel does three divisions per Vehicle against ~41 ns of
> other work and gained **1.50×**; the Rule engine does about two per due Rule against ~552 ns and
> gained **1.1%**. **Map Layers cannot have been affected at all** — diffusion and decay go through
> `RoundDiv`, which has no modulo in it. And **routing's hot site was fixed before it was ever
> published**: see the note in `spike-results` → *S5, L5*. **No published figure in this document
> needs withdrawing.**

**What the unit buys, stated as a sensitivity rather than as a forecast.** At 29.3 ns a Vehicle — the
slower of L5's two readings — one core:

| Vehicles held Microscopic | Cost/Tick | 4× — the target | **1× — where the Cap is priced** |
|---:|---:|---:|---:|
| 25,000 | 0.73 ms | 4.7% | **1.2%** |
| 50,000 | 1.47 ms | 9.4% | **2.3%** |
| 100,000 | 2.93 ms | 19% | **4.7%** |
| 186,624 — S2 R2's fixture, **not a stressed count** | 5.47 ms | 35% | **8.7%** |
| **532,750** | **15.6 ms** | **100%** | 25% |

⚠ **This row's rung is 1× and the rest of this document's is 4×, and that is deliberate rather than an
oversight.** [`adr/0096`](../docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)
prices the **Cap** — a world constant that decides which Segments are exact — at the design speed, and
[`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md)
targets the **bill** at the top rung. Session T checked whether the target moving to 4× dragged the Cap
with it and decided it does not: **a Cap and a bill are different objects**, and dilation absorbs a bill
where nothing absorbs a permanently coarser city. ***Two numbers in one document may sit on two rungs
provided each says which and why.***

> ⚠ **AMENDED 2026-08-13, and this table's own closing warning came true one day after it was last read.**
> Two things. **The whole table is pre-sub-stepping**: [`adr/0082`](../docs/adr/0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md)
> gives car-following its own clock inside phase 4, so a Vehicle costs the ratio times a row's figure —
> **84–180** at [`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)'s
> clock, and the last row's ceiling of 532,750 becomes **~6,300 at 4× and ~25,400 at 1×**.
> [`adr/0096`](../docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)
> makes **1×** the rung the Cap derives against, so the **4× column stops being the one to read** for this
> row.
>
> **And the 186,624 row was borrowed as a denominator, exactly as the paragraph below forbids.** `adr/0094`
> published a 27–58× gap against it; it is a synthetic *fleet* and this table says so in the cell. The
> ratio is withdrawn. ***A caveat attached to a number does not travel with it*** — the annotation was in
> the right place, was read, and did not survive being quoted somewhere else.
>
> **Every figure here is one core.** A 2- and 4-thread Lane kernel measurement is owed to S5 and is the
> largest unclaimed multiple in the row.

**Read the last row as the ceiling and none of the others as a prediction.** It rose from 324,945 to
532,750 on the `FloorDiv` correction, which is worth noticing for what it says about ceilings quoted
from unaudited substrates rather than for the number. The Cap is a ratio
`adr/0062` settled the units of — it counts **Vehicles** — and S5 supplies only the affordable half.
How many Vehicles a real city stresses at once is milestone **5b**'s and does not exist, which is why
no row in this table claims a share for it. **A number becoming a decision by being the only number in
the room is a habit this corpus has already recorded**, and this table is where it would happen.

### Notes each row needs

- **The Microscopic row is the first in this table whose unit was measured *before* anything was built
  with it, and it inverts the document's own standing lesson.** `0013`'s organising column is *measured
  multiplicand against guessed*, and its general form is **a unit cost is a hypothesis until a real
  world has produced one**. Here the unit is measured on a kernel and the *multiplicand* is the thing
  that does not exist — the mirror image of the routing row, which has a measured unit and a
  multiplicand that counts the wrong event. **Both are half-priced rows and they are half-priced in
  opposite halves**, so neither can be repaired by the other's method: routing needs 5b's Trip
  generation to fix a **multiplicand**, and the Lane model needs 5b's stress counts to acquire one.
- ~~**The Lane model's unit is a `powersave` lower bound and its tripwire fired on that basis.**~~
  **The prediction in this note was right and its reasoning was wrong, which is worth keeping.** It
  said the unit *"may improve and cannot get worse"* and that the 1.23× T1 needed was within reach —
  attributing the slack to the **governor**. The slack was real and it was in `IntegerMath.FloorDiv`:
  L5 found 1.50× in a redundant modulo, T1 is withdrawn, and the figures above are the corrected ones.
  The `performance` capture is still owed and **no verdict turns on it**.
- **The Bin Rule row is now measured end to end in a running city, and the row it replaced was right
  by cancellation.** The struck row multiplied a synthetic unit cost that was **2.8× too low** by a
  multiplicand that was **~5× too high**, and the product landed within 40% of the truth. That is a
  worse failure than being wrong: any change that moves one factor without the other would have gone
  unnoticed, and both factors were about to move — the unit when a real Ruleset arrived, the
  multiplicand when a real one is chosen.
- **The new row's multiplicand is measured and is still not representative.** 11,586 due per Tick
  comes from `rulesets/minimal.toml`, which carries 2 Rule Instances per Building and says in its own
  header that it models no city; `0002`'s guess is 450 per 1,000 Citizens, which is 3.75 per Building.
  So the **41% share is not a forecast** — it is what this Ruleset costs. **The durable half is the
  unit cost**, and even that is a floor, because these Rules carry one term and `02 §4.3`'s bakery
  carries four.
- 🔴 **THE BIN RULE ROW'S MULTIPLICAND MOVED ON 2026-08-23 AND THE MEASUREMENT IS NOW STALE.**
  `06` milestone 25 task 2 moved `sundries` to the Household under
  [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md),
  so `rulesets/minimal.toml`'s dwelling went from **3 Rule Instances per Building** to **1 premises
  Rule plus 2 per Household** — **7** at `occupants = 3`. ⚠ **The 11,586 due per Tick was measured
  against the old file and is not a figure this Ruleset produces any more.** **The unit cost is
  untouched**, because nothing about an evaluation changed — which is this row's own claim that the
  durable half is the unit, tested by the first thing to move the other half. ***A re-measurement is
  owed and no verdict turns on it***: the row was already labelled *measured and still not
  representative*, and it is now measured, **stale** and still not representative. ⚠ **The share is
  not simply 2.33× either** — `consume` fires once per Household where it used to apply `occupancy`
  times in one evaluation, so the *applications* are unchanged and the *evaluations* are not, and
  those are the two halves the row multiplies. Nothing in this document may be updated by arithmetic;
  the number comes back from the instrument.
- **The Bin Rule row supersedes an earlier Phase-2-only estimate of ~60% and a whole-Tick estimate of
  67%.** The first multiplied a measured Phase 2 by an *inferred* Phase 3 re-check; the second
  measured all eight phases but in a fixture. Neither should be quoted.
- **The first row already contains the staggered invariant tier.** S0a's empty Tick is *"the phase
  skeleton, the staggered invariant tier and the Layer schedule, and nothing else"*, and slice 5's
  own extrapolation of the tier to 1M (~91 µs) is 81% of that 0.112 ms — two measurements
  corroborating. Adding them separately would double-count, and an earlier draft of this table did.
- **The Zone Rule row is the first whose multiplicand is guessed and whose share is negligible
  anyway, and that is the finding rather than the number.** Slice 10's tripwire measured that the cost
  of a trigger does not scale with the number of Lots it could look at — 1.56× over three orders of
  magnitude, against a control rung that moved 989× on the same data — so **the multiplicand cannot be
  *Lots***, which was the fear. What is left is *how many Zone Rules a Ruleset declares*, and sixteen
  of them triggering on the same Tick, which is the worst alignment a file can author, costs 0.08% of
  a 15.6 ms budget. The row is here so that a later Ruleset with a hundred Zone Rules has something to
  be checked against, not because 0.012 ms competes with anything.
- **The Layer row is the only one whose multiplicand is not a guess, and that is a property of the
  map rather than good luck.** The pollution kernel has a radius of 8 Cells, so an interior emitter
  makes 289 Cells resident and a 512×512 map **saturates at about 8,192 scattered sources**. That is
  below the 120,001 Buildings a 1M city holds by enough that no plausible industrial share puts a city
  on the sloped part of the curve, so residency is not a lever the city has. The range quoted is the
  dirty region instead, from one Cell to the whole map, and it is paid **once every 64 Ticks** —
  amortised, 0.5–15.8 µs per Tick, which is nothing on any budget.
  - ⚠ **The margin was 469× and is now 14.6×, and the sentence above survived the change without
    moving.** It read *"a 128×128 map saturates at about 256 scattered sources — **three orders of
    magnitude** below the 120,001"* until the map went to 512 Cells on 2026-08-13
    ([`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)). The knee
    moved **256 → 8,192** — 32× for a 16× area, because a halo of fixed radius covers a smaller share
    of a larger map and overlaps less. **The conclusion is unchanged and its ground is an order of
    magnitude weaker**, which is the state this table exists to make visible: *this row's multiplicand
    is not a guess* is now true by a factor of fifteen rather than obviously. Routed here from
    `MapLayerFixtureTests` on the day, under
    [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md),
    and the test asserts the **margin** as well as the knee — because an assertion on saturation alone
    goes green at 8,192 exactly as it did at 256, and the thing that changed would have gone
    unrecorded. ***A conclusion that survives a change to its own premise is not thereby confirmed by
    it.***
  - ⚠ **AND THE SAME CHANGE MOVED THE COST, WHICH NOBODY DID — found 2026-08-16 by session T.** The
    note above re-derived the **knee** for a 512-Cell map and left the **price** at its 128-Cell value.
    A whole-map recompute is `O(cells)`, so **1.01 ms at 16,384 Cells is ~16 ms at 262,144** — and at
    the target rung a whole-map pollution pass would **exceed a 15.6 ms Tick on its own, at any
    population**. The row's ceiling is stale by ~16×; its floor and its amortised figure are not, and
    the dirty region a real city makes is a fraction of the map, so **no verdict in this document
    turns on it**. ***One reader re-derived one consequence of a premise and the other consequence sat
    two lines away*** — the same shape as the routing ×4 above, in the same file, on the same day it
    was found there. **Owed: re-measure the whole-map row at 512 Cells** rather than scaling it, since
    a 16× extrapolation is exactly what this document refuses everywhere else.

### Out of the Tick, and they belong here anyway

**Shares at the target rung, 15.6 ms on one core of the reference class.**

| | Cost at 1M | Share at 4× | Why it is not a row above |
|---|---|---|---|
| **One State Hash** | 32.47 ms | 208% | Sampled on a cadence, never per-Tick. What it bounds is *how often a hash may be taken*, which every golden-baseline and bisection workflow is downstream of. `05 §9` does not mention it — [`0000`](0000-board.md) → *Owed* |
| **The Decide guard** | 76.4 ms | 490% | A correctness check, not a shipping consumer. On by default, `--no-decide-guard` for long runs. It is here because it was `O(world)` **with no switch at all** until S0a, and being a guard is not what made it affordable |
| **End-of-run invariants** | 4.84 ms at 100k, once | — | `adr/0033`'s *unaffordable per Tick and trivial at the end of a run*, which is the tiering working |

---

## What the sum says

**Three of the five priced rows rest on guessed multiplicands, and the largest one is among them.** It
would be wrong to read ≥283% at 4× as *the simulation is three times over budget*: it is two unit costs
multiplied by two guesses, plus two rows that are genuinely small, on one core of a 2020 desktop at
`powersave`. It would be equally wrong to dismiss it, because **the unit costs are real and the guesses
are the corpus's own** — 450 Rule Instances per 1,000 Citizens is `0002`'s number and rate 8 is
`02 §4.3`'s bakery.

> ⚠ **The ledger could not have chosen the target rung, and session T's largest finding is that
> everybody expected it to.** `06` files the obligation on the ground that *"`plans/0013`'s whole ledger
> reads as a share of nothing until it is settled"*, which is true, and the condition below said the
> chosen speed *"cannot be evaluated until it has been argued"*, which is also true. Between them they
> imply an arbitration this document cannot perform, for three reasons found while trying:
>
> **The sum was stale by a factor this file was already carrying in a sidebar.** **No rung fits** — at
> ≥44–50 ms the ladder runs 35–40% through 283–318%, and picking 1× because it is the last column under
> 100% would have been picking the column where *the row known to be wrong is small*. And **the option
> set was a retired ladder**: 8× priced, 0.5× and 3× absent.
>
> ***A ledger says what a choice costs and never which choice to make***, and the moment its largest row
> is wrong **in kind** it cannot even do the first reliably. The rung was chosen on product grounds
> ([`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md))
> and this document's job is to price it, which is the relationship it always had and never stated.

**The honest statements are the inverted ones**, per the rule S2 R3 established after drafting its own
budget row the wrong way round:

> ~~Bin Rule evaluation fits while fewer than **~188,000 evaluations** occur per Tick at 4×.~~
> **The Bin Rule engine fits while fewer than ~28,000 Rule Instances come due per Tick at 4×**
> (~56,000 at 2×) — measured in situ, whole Tick, one term per Rule.
> Routing fits while fewer than **85 Trips** start per Tick — *mean-derived*, with a measured worst
> Tick already at 66% from an arrival rate 5× lower.
> Pollution diffusion fits on any budget, at any city size, with the whole map dirty.
> Zone Rules fit while fewer than **~21,000 triggers** land on one Tick at 4× — measured at the
> largest Zone, and **independent of how many Lots those Zones contain**.

Each survives its multiplicand being settled elsewhere. None is a verdict.

**The Rule wire moved by 3.3× and it moved onto the corpus's own worked example.** Against `0002`'s
450 Rule Instances per 1,000 Citizens — 450,000 at 1M — the ~28,000 due per Tick at 4× requires a
**mean Rule rate above ~15.9 Ticks**, where the retired evaluation-based wire required only **4.8**.
`02 §4.3`'s bakery runs at **rate 8**: comfortable under the old wire, over budget at 4× under this
one, and exactly on the line at 2×. **That is a statement about the wire, not about the bakery** —
the multiplicand is still a guess and one worked example is not a Ruleset — but it is the first time
the two have been close enough to touch, and it is what the multiplicand being settled will decide.

---

## Levers that are designed in and have never been pulled

Named so the ≥283% is not read as a standing indictment of the architecture, and so nobody pulls one
*before* the multiplicands are real — which would be optimising against a guess.

1. ~~**The target speed.** The four columns above are the lever.~~ ⚠ **SPENT 2026-08-16, and it was
   spent in the direction that costs rather than the one that pays.**
   [`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md)
   sets the target at **4×**, the top of the ladder, because withdrawing a rung as a city grows is a
   worse player experience than a rung that runs slower than it says. **The largest single lever in the
   document is gone and it did not pay out** — a document that had four columns to choose from now has
   one, and the one is the tightest. That is the correct order of events (a product decision, then a
   bill) and it is worth saying plainly that this file is 3× harder to satisfy than it was yesterday.
2. **Everything measured is single-threaded, and Phase 2 is parallel by construction.** `adr/0037`
   makes Decide read-only; `02 §8` rule 3 makes randomness counter-based, so results are independent
   of evaluation order and Phase 2 needs no coordination at all. **Lint 4 — thread-count equivalence —
   is declared and not yet live**, and `05 §6` never decided which thread runs `step()`.
   - ⚠ **This is now lever 1, and it is the only named lever the size of the gap.** S5 L6 measured
     **1.84–1.93× at two threads** on the Lane kernel, bimodal 2.5–3.9× at four on a contended machine.
     ⚠ **That figure may not be carried to the Rule engine or to routing** — it is one kernel, and
     `adr/0096` exists because a number travelled without its clause. **Session R owns it**, and `06`
     records R as gating nothing, which was true of milestones and is false of this document.
3. **The sort is `O(n log n)` and the shuffle's remit is small.** `adr/0049` narrowed it to *who goes
   short when there is not enough, never how much anyone takes when there is* — so a cheaper
   construction that preserves that property is available in principle. **Not a defect and not
   scheduled**: it is 63% per Rule across two decades, not a wall.
4. **Precedent that these collapse when examined.** The Decide guard was 95% of a run and turned out
   to be a switch. `adr/0037`'s full-world double buffer was *"8–15 ms at 1M — 50–100% of the
   budget"* and deleting it cost nothing and added no bookkeeping.

---

## How this file gets less embarrassing

**Stop guessing multiplicands.** Every *guessed* cell names something that does not exist yet rather
than something nobody bothered to measure:

| The guess | What it is | What would replace it |
|---|---|---|
| 450 Rule Instances per 1,000 Citizens | A sizing ratio invented for the tables | A **real Ruleset** — slice 7 task 10 — then **S0b** counting what a city actually arms |
| Mean Rule rate of 8 Ticks | One worked example's bakery, generalised to every Rule | The same authored Ruleset, the first artefact with more than one rate in it |
| Trip arrival rate | Nothing generates Trips | Trip generation, `06` milestone 5b. Until then S2's O-D family is **invented**, and no figure derived from it may be quoted without naming the rung |
| ~~15.6 ms at 1M~~ **CLOSED 2026-08-16** | ~~A product decision nobody has argued~~ | ✅ **Session T** — [`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md). It was arguable, a session closed it, and **the answer was 15.6 ms after all** — the value is unchanged and the standing of it is not. ⚠ **This row was the whole of the question's home in the corpus**, because a number that changes no state has no place in `0002` §D |
| **The machine and the thread count** ⚠ **NEW** | A duration with no host is not a budget | ✅ **Named** by [`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md) — one core of the reference class. The **thread count** stays open and is session **R**'s |

**Price the unpriced.** ~~Phase 3, and Map Layer diffusion~~ — **both done**, and doing them changed
two things: the engine row grew from an inferred 60% to a measured 67%, and the Layer row turned out
to be the only one in the table with no guess in it at all.

What remains unpriced is **unbuilt**: the general Event Wheel drain (slice 9), Growth and Commit.

---

## When to stop building and start fixing

Recorded as a condition rather than a feeling, so that *are we ignoring this* is checkable.

**Keep building while every over-budget row has a guess in it.** Both do. `0000` already corrected
itself once away from letting an unbuilt concern set the order — *"the design was generating
design"* — and the same failure is available here in performance clothing. S0b exists precisely
because a Tick with nothing in it cannot be priced, which is what S0a proved by finding that every
Tick figure in the corpus had been taken over an empty world.

✅ **THE CONDITION IS EVALUABLE FOR THE FIRST TIME, 2026-08-16, and it says keep building.** It read
~~*"stop and do an architecture pass when this table sums past 100% at the chosen speed with **measured**
multiplicands… note that the chosen speed is itself one of the levers, so this condition cannot be
evaluated until it has been argued."*~~ The speed is argued and is no longer a lever, so the escape
clause is gone and the condition is restated with **three** terms rather than two:

> **Stop and do an architecture pass when this table sums past 100% of 15.6 ms at 1M with *measured*
> multiplicands, on a *measured* thread count, on the reference class.**

**Read today: 283–318%, largest multiplicand guessed, thread count unmeasured. Two of the three terms
are unsatisfied, so the answer is keep building** — which is what the paragraph above already says and
now says checkably.

⚠ **The third term is new and it is load-bearing.** Without it the condition fires the moment a
multiplicand lands, on a single-threaded figure, against a target that `adr/0105` set knowing the whole
architecture is parallel by construction and unexercised. **An architecture pass triggered by a number
nobody has threaded would be an architecture pass against the wrong architecture.** Session **R** is
what makes that term satisfiable.

**The two things that will fire it are named and owned.** Routing's multiplicand becoming real — it is
85% of the bill and its correction's direction is known to point **up** — and a threaded `step()` being
measured. ⚠ **If routing lands near R6.3's 134.135 ms, the gap is not 3× and no lever in this document
covers it**; that is the case the condition exists to catch, and it is the likelier of the two.

---

## What building this ledger found

**1. A benchmark that rebuilds its world per iteration measures the rebuild.** The first attempt at
Phase 3 used `IterationSetup` to rebuild a 100,000-Building world, because Phase 3 writes and cannot
be run twice over one arrangement. The measured `Apply` was then the first code to touch freshly
allocated arrays, so it paid that world's page faults and its collection: **error bars wider than the
means**, a 1,000-Building row reading 2.223 ms mean against a 1.517 ms median, and medians scaling
1.5× then 15.5× across rungs a decade apart. Forcing collection into the setup narrowed the bars and
**did not fix the shape**. *A cost that tracks world size rather than work is not a measurement of the
work*, and the figures were discarded rather than published with a caveat.

The way out was to make the work repeatable instead of the setup cheap: **a Rule with no terms leaves
the world bit-identical**, so a whole Tick can be stepped thousands of times over one warm
arrangement. It measures less — no term walking — but it measures it *honestly*, and it is what
identified the sort.

**2. The first Layer sweep swept the wrong axis and produced a falling curve.** Whole-map recompute
read 982 µs at 100 emitters, 1,024 µs at 1,000 and **662 µs at 10,000** — cost falling as sources
rise, which is S2's *an artefact that varies with the swept axis is not distinguishable from a
result*. Two causes, both in the fixture: the stride formula degenerated to 1 at the top rung and laid
every source in a contiguous run, and **residency saturates at ~256 emitters** *(on the 128-Cell map
this was measured on; it is ~8,192 at 512 Cells — see the Layer row above)*, so the axis had
stopped moving anything two rungs earlier. Re-swept over the dirty region, the column is monotone and
the whole-map row is flat — **and the flat row is the control that confirms the instrument**, since a
full recompute must not vary with a dirty rectangle it ignores. The 128-side incremental (975 µs)
converging on the full recompute (1,011 µs) is the second corroboration.

**3. A saturation claim was asserted before it was measured, and it was wrong by two rungs.** The
first version of `MapLayerFixtureTests` asserted that 16 scattered emitters would leave most of the
map resident. It leaves **26%**. The ladder now prints every rung and the assertion is made where the
measurement put it. Small, and exactly the shape `adr/0043` is about — the claim was cheap to check
and was nearly shipped as an argument.
