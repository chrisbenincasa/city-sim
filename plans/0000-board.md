# 0000 — The board

**Read this first.** A flat, scannable status of everything planned and everything done, and the one
place that orders the three tracks against each other.

---

## What is next

**Code — milestone 5c, task 8 of 8: the long acceptance run for traffic.** ⚠ **In flight** —
`tests/Borough.Tests/Movement/TrafficLongRunTests.cs` is in the working tree. It runs against
`rulesets/congested.toml`; `minimal.toml` states neither table. Brief and per-task record:
[`0026`](0026-statistical-resolution-and-the-travel-time-matrix.md).

⚠ **It is NO LONGER the named ratifier for the four hash-bearing numbers, and that changed on
2026-08-15 after it ran.** `[traffic]`'s α, β and clamp and `[households] car_ownership_percent` are
**not refutable by any run over a generated city**: the paved extent scales with the population it
serves, so `v/c` peaks at **0.44** at 4,000, 16,000 and 64,000 Citizens alike and BPR is only ever
evaluated where it is nearly flat. All four rows moved **§D2 → §D1** and now name a **world** — a city
whose Streets were laid by `CommandKind.Connect` and deliberately under-provisioned — which is
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)'s
amendment: ***a named ratifier names a machine and a world***. **Task 8 keeps its other job** — the
`adr/0006` acceptance run — and is not blocked by this. ✅ **Its flatness band was measured wrong and
is repaired, 2026-08-15**: 1/16 came off `CommuteLongRunTests`' quantities and vehicle-Ticks a Day has a
CV of 15.7%, so the assertion was **1.25σ wide** and failed one run in ten with nothing wrong under it —
a 200-Day run puts the tail slope at **+0.267/Day** and the half-difference at **−0.32σ**. The band is
derived from the tail's own spread now, at 3σ, and one leaked vehicle still reads **ten sigma**.

✅ **Two of the four readings were taken 2026-08-15 on that world, and they split two and two.**
`ConnectedCityCongestionTests` is a dumbbell of two zoned districts joined by one Street corridor,
populated through the door [`0003`](0003-build-plan.md)'s **item 9** opened. **`[traffic]`'s α, β and
clamp: neither refuting reading fires** — loaded against free-flow occupancy runs 6,784/6,784 at the
bottom rung to **122,725/80,978** at the top, so *decorative* fails; the clamp takes **0.52%** of loaded
Segment-Ticks at a peak **6.9× the clamp**, so *binds routinely* fails. **`car_ownership_percent`: both
readings fire and refute the wrong thing** — `beyond` is 0 and the rungs collapse into *fast* because at
100% ownership every commute is a drive across a ≤4 km city while `adr/0095`'s rungs are percentiles of
a **foot-only** distribution. ***A refuting reading named against one consequence cannot refute a number
whose live consequence is a different one***, so `adr/0052`'s amendment wants a third clause — **and a
quantity**. ⚠ **The largest finding is neither row**: the priced and free-flow runs agree **to the
Citizen** on who is employed at every rung while their occupancies differ by half, which is `adr/0046`
working as decided and the sweep's *no feedback term* arriving with a number. It is an equality
assertion now. ⚠ **The Microscopic Cap's demand side was NOT taken and is under-specified**: `03 §3.3`
makes the Stress thresholds *measured, not chosen* and nothing has measured them, so *Vehicles a real
city stresses at once* is **a number per threshold** rather than a number — and a count off a
four-rung demonstration does not travel to 1M. Readings in [`0002`](0002-open-questions.md) §D1.

✅ **Phase 2 was re-derived 2026-08-16 by session K, and `06` is the sequence.** The milestone table runs **5a–5c frozen, then 6–24**; a **retired-numbering table** in `06` makes every citation written before that date still resolve. **Numbering is now defined** — [`PROCESS.md`](../PROCESS.md) → *Numbering*: the integer is the position, a shipped number is frozen, inserting renumbers the unshipped tail, and `-bis`, `K1`/`K2` and sessions named *eight* and *nine* are recorded as retired forms. ⚠ **Two new argument rows exist** — **R** (threading) and **T** (the target speed) — which are two of `06`'s five obligations finally given an owner. **The next code row is 5c task 8, then milestone 6.**

**Argument — a sitting ran on 2026-08-15** and closed **both** remaining sections of `04`:
`§6` → [`adr/0102`](../docs/adr/0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md)
and [`adr/0103`](../docs/adr/0103-a-need-is-where-a-frequent-private-failure-accumulates.md), `§7` →
[`adr/0104`](../docs/adr/0104-a-skill-tier-is-earned-by-attendance-and-the-credential-stays-a-wall.md).
It then **assessed milestones 19 and 20**, whose gates nobody had ever asked about. Only `04 §3`
(Movement) is left ungrilled in that document. Nothing else in the argument track gates a slice; the
rest of it is a **menu** (*Open tracks*).

✅ **The commute became two journeys, 2026-08-15** —
[`adr/0101`](../docs/adr/0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md),
**built the same day it was decided**, and it is a demand term rather than a 5c task. ⚠ **This file
listed it as one of the `04 §6` sitting's ADRs and it is not one of them** — it came out of *why can a
generated city not congest*, which is the four-demand-terms finding, and filing it with the Needs sitting
would have hidden the only one of the four that has been built. `commute_peak_factor` is **retired**;
departure is now anchored on a Shift start hour belonging to the **Workplace**, and the Day's profile —
two peaks, a baseline, a quiet night — is a **reading** rather than a dial. All three golden baselines and
`session.borough` itself re-recorded. Its findings are in the ADR's *What building it changed*, and the
sharpest is not about commutes: ***an intrusive index that unlinks by recomputing its key cannot outlive a
change to that key's inputs.*** ⚠ **The middle of the Day is empty and that is LAND USE** — one employing
kind means one set of hours — which is the next demand term and is `adr/0070`'s *build X*.

✅ **The corpus was swept for unscheduled mechanisms, 2026-08-15, and `06`'s inventory went from
eighteen rows to thirty-nine.** Run because `02 §5.4`'s **residential choice model** had been invisible
to that table for its whole life and nobody had asked what else was. **Three findings outrank the rows.**
The table is a **flat list** and the mechanisms are a **dependency graph** — rows that have sat there for
months are downstream of other rows and say so nowhere, and the two deepest are the **Provider List** and
the **land value Layer**. The table can only hold **what a milestone could schedule**, which is a third
blind spot after the two already recorded, so four of the largest debts get their own table: a Ruleset
that models a city is *content*, an under-provisioned `Connect` world is a *fixture*, a play session is
neither, and `05 §6`'s threading policy is an *argument* — and that last one names *"milestones 8 and
11"* as its gate, where **there is no milestone 11**. And **two documents each name the other as
Evidence's owner**: `01 §6` says it is scheduled in `06`, `00-vision` says it is a foundational feature,
`06` attributes the claim back to `00-vision`, and no milestone builds it. ***`plans/0012` Cause 1 with
the copies pointing at each other rather than drifting apart***, which is why no reader of either ever
noticed. ⚠ **One consequence is live rather than archival** — the **driver model** row is what closes
`03 §3.4`'s loop, and 5c prices congestion on entry to a Segment while routing on free flow, which
[`adr/0046`](../docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)
refuses by name: **congestion is a cost paid and never a cost avoided**, so the traffic model has no
feedback term. ✅ **This discharges K's third gate**; two remain.

**Spike — nothing is scheduled.** S5 owes its 4-thread rung on a quiet machine; `spikes/S2.Routing/`
is **not** to be deleted (another session is working inside it).

**After 5c**, milestones **7** and **8** are the only Phase 2 rows with no gate on them. **21**, **22**
and **23** wait on sessions G and E. ✅ **19 and 20 were assessed 2026-08-15 and neither is gated on a
session**: 19 waits on three unmilestoned **mechanisms** — `02 §5.4`'s choice model, the Hinterland and
money — and 20 on `adr/0011`'s ⚫, promoted from a debt to a gate. See *Blocked*.

---

## How to read this file

**This is a view, not a source.** [`0003`](0003-build-plan.md) owns the slice order and its gates,
[`0002`](0002-open-questions.md) owns **every open question**, `docs/adr/` owns the decisions,
[`0013`](0013-tick-budget.md) owns what a Tick costs. When they disagree with this file, **they win**.
Update this file whenever a task lands.

**Three rules keep it a view rather than a second ledger.**

1. **Do not write an open question here.** That is how it once held 63 of them while the file named
   *open questions* held none.
2. **A cell here is at most three sentences.** The reasoning belongs to the slice plan, the spike plan
   or the ADR, and this file links to it.
3. **A closed row leaves.** Closed rows go to [`0000a`](0000a-board-archive.md) as one line each. The
   file has been cleared twice — the 999-line long-form board on 2026-08-12
   (`git show db6f19f:plans/0000-board.md`) and ~400 lines of closed-row narrative on 2026-08-15
   (`git show 26eeaf8:plans/0000-board.md`), both times because a view that carries its own history
   stops being scannable.

**[`0002`](0002-open-questions.md) §F owns the coverage map** — every design document and every ADR,
marked 🔴/🟡/🟢, with ⚫ where a supporting claim has been measured false. **§A–§E are questions somebody
has already framed; §F is where you look for what has never been examined at all**, and the second is
the one that has twice produced a surprise. *(§F has stopped tracking new ADRs three times; the fix is
[`0012`](0012-corpus-audit.md)'s mechanical check 5 — generate the ADR column from the directory.)*

> ⚠ **This file is the document most likely to be read instead of the build.** On 2026-08-13 a sitting
> read a paragraph here to answer *what is next* and reported work that had shipped an hour earlier in
> the same tree; `git log` would have said so in one command.
> [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
> applies to the board itself, which is what its own opening means by *a view, never a source*.

---

## State of play

**What the project is.** A city-builder whose simulation is an ordinary C# library with no game engine
inside it. Godot will be the display layer and has not been started.

**Where it is.** **Phase 2, milestone 5c** — statistical resolution and the travel-time matrix, seven of
eight tasks done. Phase 1 is closed and **its code column is empty**; slices 0–10 all shipped, and
[`0003`](0003-build-plan.md)'s hash-moving queue has one open item (**item 8**, a live wake-predicate
defect, filed unfixed because its two repairs are a design question); **item 9** was filed and built on
2026-08-15, so a player-shaped network can be given a population through the build's own door. Phase 2 has shipped **5a** (the
Road Graph), **5a-bis** (Lots and the road editor), **5b** (Trips and Legs), **5b-bis** (jobs and the
commute) and most of **5c**.

**No gate is red anywhere in the corpus, and no session gates a slice** — F was the last one that did.
S4, S0a, S0b, S2 R0–R8 and S5 have all run. Sessions A, B, C, D, F, H, J, M, P, Q, eight and nine are
closed; **N** is the only one still open, and what is open in it is task 5's residue — every ADR it
produced has shipped.

**What you can run today.**

```
dotnet run --project src/Borough.Headless                                    # build a city, print its tables and a State Hash
dotnet run --project src/Borough.Headless -- --seed 1 --ticks 10000          # step a session and print a hash trace
dotnet run --project src/Borough.Headless -- --census --series               # what every collection did, and the shape of it
dotnet run --project src/Borough.Headless -- --layer pollution               # a Map Layer as an ASCII field
dotnet run --project src/Borough.Headless -- --zones    --ruleset rulesets/minimal.toml   --ticks 5000
dotnet run --project src/Borough.Headless -- --roads    --ruleset rulesets/severance.toml --seed 3
dotnet run --project src/Borough.Headless -- --trips    --ruleset rulesets/minimal.toml
dotnet run --project src/Borough.Headless -- --commute  --ruleset rulesets/minimal.toml   --ticks 4096
dotnet run --project src/Borough.Headless -- --traffic  --ruleset rulesets/congested.toml --citizens 16000 --ticks 512
dotnet run --project src/Borough.Headless -- --help                          # every flag
```

**What works.** Typed tables where every field is declared once as saved-and-hashed or
derived-and-rebuilt. Integer-only arithmetic, a deterministic eight-phase Tick, an Input Log that
replays to identical hashes, a crash artifact that replays back into its own crash, Map Layers with
diffusion, and build-time analysers that turn the determinism rules into compiler errors. **Two Rule
families** (Bin Rules and Zone Rules), a **Road Graph** with Lots and Access Points, and a **movement
stack** that now runs: Trips, Legs, Travellers, a real Dijkstra, a travel-time matrix, a route cache, a
vehicular Leg and a volume-delay function.

**What does not exist.** Two of the eight Tick phases are empty. There is no money, no prices, no
renderer, and **no supply chain** — a chain between Buildings crosses an ownership boundary, which is
the District Pool, which is a named hole that throws. There is **no land use**: every Building has the
same occupants and the same posts, so `--commute` and `--traffic` both draw a uniform city.

**Two things the shipped Rulesets cannot demonstrate, and both are measured rather than suspected.**
`minimal.toml` severs **0.0%** of pedestrians at every dial value, which is why `severance.toml` exists;
and a generated city **cannot congest itself** — `v/c` peaks at 0.44 at 4,000, 16,000 and 64,000
Citizens alike, because the paved extent scales with **√population**, so the same number sizes both the
demand and the supply. Congestion is something a **player** makes by laying too little road
([`adr/0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)), and
`CommandKind.Populate` cannot reach it.

### The five numbers to hold in your head

| | Number | What it means |
|---|---|---|
| **The good one** | **8.72 ms a Tick at 1M — 55.9% of the budget at 4×** (S0b) | The **only** Tick figure ever taken from a real running city. Everything else priced is a spike's paper |
| **The sum** | [`0013`](0013-tick-budget.md) reads **≥17.8 ms a Tick** | ⚠ ***Carry the bill, not the percentage.*** ≥229% at 8×, ≥114% at 4×, ≥57% at 2× and ≥29% at 1× are **one bill over four product decisions**, and a share hides which side moved |
| **The row that decides it** | **Routing is 9.4–10.5 ms of those 17.8**, ×4 since the clock moved | Without it the ledger fits a 4× rung with room. Its unit came off a synthetic harness; its multiplicand counts **the wrong event** |
| **The correction with a known direction** | A diverting Traveller re-searching costs **134.135 ms a Tick** at target scale | It points **up**, and the cache cannot rescue it. **Answered rather than reduced** by [`adr/0061`](../docs/adr/0061-a-diversion-rejoins-by-local-descent-and-a-rejoin-is-never-a-search.md) — a Rejoin is local descent and there is no search |
| **Scale** | 1M Citizens in **86 MiB** of tables, **177 MB** resident; 100,000 Ticks in 11.75 s | The sizing risk is retired. **One State Hash is 32.47 ms — 2.08 Tick budgets**, so a per-Tick hash is unavailable at target |

**The meta-figure, and it is the one to be uneasy about.** *Every* time a fixture has been replaced by a
real world, the number came in **worse** — the Rule engine's unit cost by **2.8×**, Trips per Tick by
**32%**, the Zone Rule `sample` dimensionally wrong at scale, and every pre-S0a Tick figure taken over an
**empty world**. [`0013`](0013-tick-budget.md) states the general form: ***a unit cost is a hypothesis
until a real world has produced one***, and routing's has never met a world.

**Its one counterexample is instructive because it is not really one.** S5's L5 moved the Lane model
**1.50× in the good direction** — not because a fixture met a real world, but because the *instrument*
had a defect (`IntegerMath.FloorDiv` computing a modulo on every call). So the rule gains a second
clause: a number improves when the thing being measured was never what the measurer thought. Two
measured rows — the Rule engine's **6.4 ms** and S0b's **8.72 ms** — were taken on that substrate and are
now upper bounds by an unknown amount.

**Known problems, none urgent.**

- **Routing does not fit, and R6.3 found the reason is not the one we had.** Starting a trip is cheap;
  what is expensive is a driver changing its mind mid-journey, which the design made routine and then
  removed the cheap way of serving.
- **The network runs out of routes, not road** — 87.25% of traffic on 1% of the carriageway, 90.87% of
  it empty, at 13% of holding capacity. A design question, in [`0002`](0002-open-questions.md) §C.
- ⚠ **The job-search box does not filter, and it cannot in a foot-only world.** It covers **44.9×** the
  golden fixture's city and **100.0%** of its Buildings, first narrowing near 160,000 Citizens. The root
  is a **mode confusion** — `adr/0089`'s *Commutes across* column is a **vehicle** commute and
  `adr/0095`'s rungs are **foot** percentiles. [`0012`](0012-corpus-audit.md) *Cause 5*, fourth sighting.
- **The synthetic city fixture and `World`'s table sizing disagree** and nothing checks that they agree;
  Households land at exactly capacity, so the first one the simulation creates grows the table.
- **Every S2 and S0a absolute is `powersave`, mis-pinned, or both** — and **co-tenant load dominates the
  governor**. R8's controlled re-capture agrees on 724 of 744 cells within ±2% against a mismatched
  pair's 1.77×; *the variance S2 called run-to-run noise was almost entirely machine state.* **No capture
  filename records load.**
- Several documents still describe behaviour that later measurement contradicted — see *Owed* and
  [`0012`](0012-corpus-audit.md).

---

## Do these next

**Build. The argument track is not the constraint, and treating it as one is how this project starts
going in circles.** That is a correction the board made to itself after a session in which every
decision generated two or three more — *the design was generating design*. **The standing rule: an
argument session runs when something concrete is blocked on it, and never because it is available.**

> ⚠ **The three tracks contend, and the old sentence saying they do not was false twice over.**
> [`0012`](0012-corpus-audit.md) records the first: they do not contend for files, they plainly contend
> for **conclusions**. The second was measured on 2026-08-14 — **they contend for cores**. A threading
> capture taken while a parallel session ran `Borough.Tests` at **~1018% CPU** read its 4-thread rung
> **bimodally** while its 1- and 2-thread rungs were untouched.
>
> ***A spike measuring parallel scaling cannot share a machine with a code session running a test
> suite.*** The interference is **invisible in the artefact**, biases in one **direction**, and bites
> **hardest at the widest rung** — the rung a scaling measurement exists to read. The spike track runs
> **unattended, not concurrently**, and any capture whose subject is parallelism names *nothing else
> running in this repository* as its first control, ahead of the desktop and ahead of the governor.

| | Track | Task | Plan | Why this one |
|---|---|---|---|---|
| **1** | code | ⚠ **IN FLIGHT — `06` milestone 5c task 8, the long acceptance run for traffic.** Tasks 1–7 shipped 2026-08-14. Task 8 is the **named ratifier for four hash-bearing numbers** and must run against `rulesets/congested.toml`, since no other shipped file states `[traffic]` or `[households]`. ⚠ **It runs over whole Days rather than a round 100,000 Ticks** — every congestion figure this milestone published before task 8 was taken over 512 Ticks from Tick 0 with employment still ramping, and one of them was an artefact of exactly that | [`0026`](0026-statistical-resolution-and-the-travel-time-matrix.md) | It closes `03 §3.4`'s self-correcting loop: volume changes travel time, travel time changes routing, and the failure feeds the detector |
| **2** | code | ✅ **DONE 2026-08-15 — [`0003`](0003-build-plan.md) hash-moving queue item 9, filed and built the same day.** `CommandKind.Populate` and `CommandKind.Connect` were welded shut: `RoadGenerator.LayInto` throws on a world that already has Segments and `SyntheticCity.PopulateInto` called it unconditionally, so **no player-shaped network could get a population** — and under [`adr/0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md) that is every city anyone will ever have. **The populator's land half and people half are separable now** (`SyntheticCity.PeopleInto`), the verb count did not move, **1,438 green and no baseline re-recorded**. ⚠ **What it unblocks is still open**: the ratifier world for the `[traffic]` trio, `car_ownership_percent` and the **Microscopic Cap's demand side** can now be built through the build's own door — somebody has to run it and read it | [`0003`](0003-build-plan.md) → *The hash-moving queue*, [`0002`](0002-open-questions.md) §C | ⚠ **The weld was hiding an incomplete clamp.** At zero Lots the Building clamp divided by zero, and `PopulateInto` structurally could not reach zero — ***a mechanism with one caller has only the edge cases that caller can produce, so opening a door widens the input domain before it widens anything else*** |
| **3** | argument | ✅ **RAN 2026-08-15 — `04 §6` and `§7` both closed, then a corpus-wide sweep for unscheduled mechanisms.** Only `04 §3` (Movement) is left ungrilled in that document, and it gates nothing. The sweep took `06`'s inventory from eighteen rows to thirty-nine and **discharged K's third gate**; `§7` gave a Skill Tier an earner ([`adr/0104`](../docs/adr/0104-a-skill-tier-is-earned-by-attendance-and-the-credential-stays-a-wall.md)). *Original row:* ⚠ **`04 §6`, the Household's demand side.** Three ADRs so far: `0101` a commute is two journeys and the Day's shape belongs to the job; `0102` a housed Departure is a comparison the Household re-runs; `0103` a Need is where a frequent private failure accumulates. **`commute_peak_factor` is retired** — the third time a `0002` §D row has lost a quantity rather than gained a value | [`0002`](0002-open-questions.md) §D, §F | ⚠ **Only the last two of `04 §6`'s seven steps were settleable** — steps 1, 2 and 5 rest on the District Pool, prices and the Provider List, none of which exist. *A section can be closed on the half that has a mechanism* |
| **4** | spike | ⚠ **STILL BLOCKED — do not delete `spikes/S2.Routing/`.** The 5a gate is discharged (the port is done; nothing in `src/` or `tests/` compiles against it), but **another session is doing research inside it**, so it is live work and that is the gate now. **51 tracked C# files, 29,719 lines**; `Borough.slnx` still lists the project | [`0010`](0010-s2-routing.md) → *R7* | ⚠ **Do not read the first gate's clearance as the second's.** *A deletion held twice for unrelated reasons is the row that gets struck when the wrong one clears* |
| **5** | spike | **S5 owes two captures.** The **4-thread** Lane kernel rung is bimodal (~2.5× against ~3.9×) and needs four pinned cores clear at once; the canonical `performance` re-capture is owed beside it. **2 threads is settled at 1.84–1.93×** | [`0019`](0019-s5-lane-kernel.md), [`spike-results`](../docs/spike-results.md) → *S5* | ⚠ **Quote the supply-side multiple as *at least 1.84× and plausibly near 4×*, never as 4× bare.** `adr/0096` exists because a number travelled without its clause |
| **6** | code | **[`0003`](0003-build-plan.md) hash-moving queue item 8 — filed unfixed.** A waiter whose **own** requirement falls is never re-checked: `adr/0063` made the wake predicate read live state, and the only thing that calls `World.Drain` is a write to the **Bin**. Observed stable on four Bins from Tick 512 to 4096 | [`0003`](0003-build-plan.md) → *The hash-moving queue* | ***A live predicate with an event-driven trigger is only correct if every input to the predicate is an input to the trigger.*** Filed rather than fixed because both repairs are design questions |
| **7** | tidy | **Delete `spikes/S4.Kernels/`** — S4's task 11, open since the spike closed and gated on nothing | [`0004`](0004-s4-kernel-benchmark.md) | The same discipline as row 4: a deletion that size is taken deliberately, not as a consequence of a green suite |

**Closed rows are in [`0000a`](0000a-board-archive.md)**, one line each with the document that owns the
record. **The argument track has no promoted row** — the standing rule holds for everything in it: take
from the menu when something concrete is waiting, and leave it alone otherwise.

---

## Done

Slice status is [`0003`](0003-build-plan.md)'s ledger, which is the source; spike numbers are
[`spike-results`](../docs/spike-results.md)'s. This list says what each thing **was for** and links to
its record. **Findings live in the plan that produced them** — the third column is a pointer, not a
summary.

### Slices

| | Slice | Record | The finding that reached past it |
|---|---|---|---|
| 0 | Scaffolding — four projects, build config, CI | [`dev-environment`](../docs/dev-environment.md) | — |
| 1 | **S4** — the kernel benchmark | [`0004`](0004-s4-kernel-benchmark.md) | No tripwire fired. *Task 11 — delete `spikes/S4.Kernels/` — is still open* |
| 2 | The arithmetic substrate | [`0005`](0005-arithmetic-substrate.md) | `adr/0038`, and an amendment to `adr/0003`'s normative hash |
| 3 | The analysers | [`0006`](0006-analysers-and-lints.md) | Twelve diagnostics; `adr/0036`'s rule-7 exception axis |
| 4 | Typed tables and the field declaration | [`0007`](0007-typed-tables.md) | `BOR0901`, and the project's first State Hash |
| 5 | The Tick, the Input Log and replay | [`0008`](0008-tick-and-replay.md) | **`Borough.Formats`, the fifth project** (`adr/0039`). Its costing of the invariant tiers is worth three orders of magnitude |
| 6 | **Map Layers** | [`0009`](0009-map-layers.md) | **`adr/0044`** — the sixth claim measured false, and the first outside S2 — which then got its own second half wrong by argument and **withdrew rather than amended**. ***Citing an ADR is not applying it*** |
| 7 | **The Rule engine — Bins and Bin Rules** | [`0011`](0011-rule-engine-bins-and-rules.md) | `02 §4.3`'s worked example **destroyed money for six slices**, because the transcription into the loader's fixture dropped the money line. A green suite agreeing with the code instead of the document — the first of **four** such |
| 8 | **Hot reload** | [`0015`](0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md) | **`adr/0015`'s acceptance test, run: 0.70 s** against the 60–120 s warm rebuild it was written on — and it **could not have been run through a recorded log at all**, because a log names a Ruleset by content hash and *editing the file is the loop* |
| 9 | **The Event Wheel** — the fine wheel only | [`0016`](0016-the-event-wheel.md) | **776 tests, no baseline moved** — hash-neutral by construction, its own acceptance test. The end-of-run tier had been stamping every violation **Tick 0** in both acceptance runs |
| 10 | **Zone Rules — the second Rule family** | [`0014`](0014-zone-rules-and-the-sweep-family.md) | **Discharged `adr/0006`'s collection half for the first time.** The growth cycle **cannot be entered from a standing start**, and a sample stated as an absolute count makes a mechanism's time constant the size of the city |
| **5a** | **The Road Graph** — the first Phase 2 slice | [`0020`](0020-the-road-graph.md) | **The spike had the mode mask backwards from its own argument**, and no measurement it ran could have caught it — it never generates a one-way street. ⚠ Its severance claim was **measured false and inverted** on 2026-08-11 |
| **5a-bis** | **The Lot subdivider and the road editor** | [`0022`](0022-the-lot-subdivider-and-build-road.md) | **The sharpest finding is about the re-record, not about Lots**: a straight re-record would have retired a verb from the baseline while producing a full set of freshly correct hashes. Second sighting in three slices, so it is `GoldenSessionCoverageTests` now rather than a paragraph |
| **5b** | **Trips, Legs and the pedestrian layer** | [`0021`](0021-trips-legs-and-the-pedestrian-layer.md) | **A walk search has no unit cost** — 86.2 ns to 38.4 µs, a 446× span, because the search settles a *disc*. And ***Severance is a tail, not a median***, which makes the Commute Budget's being a **percentile** a measured requirement |
| **5b-bis** | **Jobs, the commute and the first Trip generator** | [`0023`](0023-jobs-and-the-commute.md) | **Four of its eight tasks found something about an *instrument* rather than about jobs** — a missing derivation, a censored distribution, a lossy conversion and a small-sample maximum. ***`adr/0043` types a claim by whether a machine could produce the number, and every one of these produced it*** |
| **5c** | **Statistical resolution and the travel-time matrix** — ⚠ tasks 1–7 of 8 | [`0026`](0026-statistical-resolution-and-the-travel-time-matrix.md) | ***A premise licensing one quantity to stand in for another is itself a measurement, and a constant moved in another document can retire it silently.*** And **a generated city cannot congest itself** — the same number sizes both the demand and the supply |

### Spikes

| | What it settled | Record |
|---|---|---|
| **S4** | The kernel benchmark; no tripwire row fired | [`spike-results`](../docs/spike-results.md) |
| **S0a** | **The sizing half of the Phase 1 gate** — 86 MiB for 1M rows. Found what it was not looking for: **run mode had never had a city in it**, so every Tick figure the corpus held had been taken over an empty world | [`spike-results`](../docs/spike-results.md) → *S0a* |
| **S0b** | **The Tick with work in it: 8.72 ms at 1M, 55.9% at 4×.** Its largest finding is not the price: **a Zone Rule's `sample` is absolute where the thing it paces is relative to the size of the city** | [`spike-results`](../docs/spike-results.md) → *S0b* |
| **S2** | **Retired the risk it existed for** — *pathfinding is slow at a load nobody measured, on a map already committed to*. R0–R8 done, R7's tail closed 2026-08-11. The one act left is deleting the harness, which is row 4 of *Do these next* and is **blocked** | [`0010`](0010-s2-routing.md), [`spike-results`](../docs/spike-results.md) → *S2* |
| **S5** | **The Microscopic tier's price**, which the corpus had transplanted from somebody else's float engine — and, in the end, a defect in our own. **~533,000–570,000 Vehicles a Tick a core** once `IntegerMath.FloorDiv` stops computing a modulo nobody asked for. **L6: 2 threads is 1.84–1.93×; 4 is owed** | [`0019`](0019-s5-lane-kernel.md), [`spike-results`](../docs/spike-results.md) → *S5* |
| **S1 / S3** | **Not run.** Track B, Godot only, no gate — the empirical inputs to session **L**. `06`'s specifications were stale by an order of magnitude and are struck | — |

⚠ **One caveat travels with every S2 figure and belongs to all of them.** Everything R1–R5 published ran
on a **frozen cost basis** — a route was invalidated because a road was *bulldozed*, never because one
got *busy*. R8 closed that loop for itself; **quote nothing from R1–R5 as a statement about a congested
city.**

### Sessions

Full records are in the linked ADRs and plans. **Closed argument-track rows are in
[`0000a`](0000a-board-archive.md).**

| | Produced | The thing it changed outside itself |
|---|---|---|
| **A** — `adr/0015`, hot reload | [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md) | **Cleared two slice gates rather than one**, and found `adr/0003`'s owed exception **was never owed** |
| **B** — `02 §4` residue | `adr/0045` | A fallback chain is a source ladder over one Bin. Found the corpus's **own worked example polled for ever**, and published a depth cap of 5 then **withdrew it** |
| **C** — `02 §7` + `adr/0006` | [`adr/0056`](../docs/adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md) | The Wheel's period is **exactly one Day** while `adr/0011` schedules Life Stages in **Days**, so **every Life Stage transition the design specified was unrepresentable** on the wheel it was specified to run on |
| **M** — the route cache's invalidation contract | An amendment to [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md) | Not the contract: **a Habit belongs to the Citizen, not the Traveller**, which sets the store to the population and means R5.5.4's rotation was never evidence about it |
| **eight** | `adr/0036`, `adr/0037`, `adr/0002` rebuilt | The core's language; one live world state; a boundary sized against an *inspector* rather than a renderer |
| **nine** — `06-roadmap.md` | [`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md) | **A planning document cites, a design document owns.** `06` is since [`0012`](0012-corpus-audit.md)'s **control case** — the only large document that came back clean, because it stores no status |
| **D** — `03 §5`, the traffic model | [`adr/0060`](../docs/adr/0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md)–[`0062`](../docs/adr/0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md), and **S5** | **The two largest things it produced were not on the brief**: a Habit is now `k` variants, the structural answer to *the network runs out of routes, not road*; and **the first ratification this corpus has withdrawn** |
| **N** — the Bin, the Pool and the economy | [`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)–[`0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) | **The corpus held two widths for one quantity** — `Money` is a `long`, a Bin's level was an `int` — so every payment narrowed 64 bits to 32. ⚠ **Still open** on task 5's `04 §6` residue; every ADR it produced has shipped |
| **F** — `adr/0008`, the Leg model | An amendment plus [`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md)–[`0076`](../docs/adr/0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md) | ***An `and` in a consequence is two consequences*** — the brief would have deleted the correct half. And **a placeholder inside the range of legitimate answers cannot announce itself**: a zero-length parking walk makes a full car park cost *less* than an empty one |
| **H** — `adr/0009`, parking | [`adr/0083`](../docs/adr/0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md), [`adr/0084`](../docs/adr/0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md) | The question blocking the Epoch rung for two sessions was a **false alternative** — *nobody could type it because there was nothing to choose between*. And ***a vacuously-satisfied invariant needs its state to exist — zero is a value, undefined is not*** |
| **J** — `05 §7`, map size, the Outside | [`adr/0085`](../docs/adr/0085-nothing-on-this-map-is-far-away-so-a-settlement-is-made-by-a-gap.md)–[`0088`](../docs/adr/0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md) → [`0024`](0024-session-j-the-save-the-map-and-the-outside.md) | **It reversed nothing and replaced the reason for everything**: ***a decision given several grounds is load-bearing on whichever ones survive, and nothing recomputes that when one falls***. S2 R1.5 had already measured its answer in a column nobody read |
| **P** — the player model, `01 §1/§3/§4` | [`adr/0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)–[`0096`](../docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md) → [`0025`](0025-the-player-model.md) | Twenty-four decisions. **The map starts empty, 1× is the design speed, a Day is 2048 Ticks, and the Commute Budget is three rungs.** Its sharpest output is ***a caveat attached to a number does not travel with it*** — [`0012`](0012-corpus-audit.md) **Cause 5**, which caught its own author within the hour |
| **Q** — the reach-failure memory | [`adr/0097`](../docs/adr/0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md), plus **mechanical check 8** | **The argument track stopped being the constraint**: a gate audit found milestone 5c unblocked and nobody knew. ***A gate is discharged by the work and struck by somebody, and only the first happens on its own*** |
| **K** — `06`'s Phase 2 ordering | `06` re-derived; [`PROCESS.md`](../PROCESS.md) → *Numbering*; sessions **R** and **T** | **The deliverable was an order and the findings were about the instrument.** ***A partially-shipped milestone reports as shipped, so a branch of it that throws is invisible to an inventory of unscheduled work*** — the **District Pool** (`RuleEngine.cs:803` throws on `Scope.Pool` **by name**) was in no row of a forty-row list because it lives inside milestone 3a, which is marked done. Two more: **milestone 10 was two milestones wearing one number** (Save/load here, *the Outside* on this board), so session J's clearance was recorded against the half it did not clear and **the Outside was never scheduled at all**; and the *Segment volume* row had **shipped** in 5c and nobody struck it. ⚠ **Two of the six roots are half-built** — `MapLayers.SetLandValueTarget` and `HouseholdTable.Money` are both **declared with only test callers**, so the two deepest debts are *producers*, not subsystems |
| **—** | [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md), [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) | The two process rules that have paid for themselves: six claims measured false, and a number **deleted** rather than added |

---

## Open tracks

### The argument track — a menu, not a queue

**Nothing in it gates a slice.** Take from it when something concrete is waiting, and leave it alone
otherwise. Ordered by what it unblocks. **Closed sessions are in [`0000a`](0000a-board-archive.md).**

| | Session | What is missing | Unblocks |
|---|---|---|---|
| **E** | `adr/0005` + `adr/0007` — fidelity | One session, not two: `0007` moved Fidelity from person to **place**, and `0005`'s tiers are what it moved. ⚠ **22 needs the `0007` half only** — `adr/0005` says in its own last line that its fidelity half was superseded | milestones **22**, **23** |
| **G** | `adr/0016` — the lane is the entity | Carries the order-of-magnitude claim the whole microscopic tier rests on. ⚠ **Partly discharged by measurement**, and `0002` §F marks it a *type-before-you-grill* suspect — a session **may not close** whatever part turns out measurable | milestone **21** |
| ~~**K**~~ | ~~`06`'s Phase 2 **ordering**~~ | ✅ **RAN 2026-08-16 and CLOSED.** Phase 2 is re-derived: **5a–5c frozen, then 6–24**, all forty inventory rows placed, the **dependency graph written down** (six roots, and the edges were in nobody's document), and the five obligations given **owners and triggers** rather than slots. ⚠ **It found one row stale, one root missing and one milestone wearing two names** — see *Done* → *Sessions*. **Its two open gates are not discharged and were not needed**: E and G move only 21–23, which is why the demand spine could be sequenced without them | — |
| **R** | ⚠ **NEW 2026-08-16** — `05 §6`'s threading policy | The obligation `06` could not give a milestone. ⚠ **Its stated gate was wrong twice**: *"gates milestones 10 and 11"* named a milestone 11 that never existed, and old-10 is now **8**, which K found unblocked — [`adr/0087`](../docs/adr/0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md) already decides the one threading fact a save needs, **in a session about save format, while the document that owns threading stayed ungrilled** | Phase 3 planning, via **L** |
| **T** | ⚠ **NEW 2026-08-16** — the target speed, 8× / 4× / 2× / 1× | The cheapest session in the corpus and the one most is denominated in. [`0013`](0013-tick-budget.md) reads as a share of nothing until it is settled, its own stop-and-fix condition *"cannot be evaluated until it has been argued"*, and [`adr/0096`](../docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md) has already re-based the Microscopic Cap on it once. **Arguable, so a session may close it** | [`0013`](0013-tick-budget.md) entire |
| **L** | **A presentation design** | **It does not exist.** Every other phase is backed by a design document; rendering has none. ⚠ **Two thirds of it is writable today** — only the `05 §2` boundary cost needs S1/S3 numbers | Phase 3 |

### Not arguable, and worth being explicit about why

The **Microscopic Cap**'s value needs a built traffic model, and **S2** is measurement — argument cannot
close either. [`0002`](0002-open-questions.md) names a set of **playtest questions wearing
design-question clothing** that the argument track must not drift into: health, recreation, Service
variants, car ownership, and `01 §1/§3/§4`. The governability problem — *268 km² of individually-placed
service Buildings* — **is not answerable by argument.** Somebody has to try placing them.

⚠ **Session P grilled all three `01` sections and left that set intact**, which is the thing to carry:
what a grilling closes is the **design** question, and the playtest question underneath it survives
unchanged. `§4` produced **two more entries for this list** — `TICKS_PER_DAY = 2048` and a Household's
life in Days, both hash-bearing, both with **playtest** as the only named ratifier. ***An examined
section is not thereby a settled one.***

### Two audits the corpus assigned itself

- ⚠ **OPEN — type every claim `arguable` or `measurable` ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)), across every ADR and design
  section, not only the ungrilled ones.** Two of the claims measured false sat in 🟢 rows of the coverage
  map. That map is [`0002`](0002-open-questions.md) §F, which marks three suspects ⚠ — **`adr/0016`**,
  **`adr/0009`**, **`adr/0008`** — each reading as decided, each carrying a quantitative claim, none with
  a number. **Type them before grilling them.**
- ✅ **RUN 2026-08-14 — re-check every 🔴-blocked debt for a gate whose stated reason covers only part of
  what it blocks.** Three clean positives (**milestone 21**, **Phase 3 / session L**, **K**), one partial
  (**22**). ⚠ **Its other half found a different failure**: **stale gates whose stated reason had gone
  false**, in `plans/0003`, `0002` §A and `06` alike. ***A gate is discharged by the work and struck by
  somebody, and only the first happens on its own*** — and **the summary line is the part nobody updates,
  because updating the row feels like finishing the job.**

---

## Owed — documentation debt, none of it blocking

**Open items only.** **The corpus-wide sweep's debts are [`0012`](0012-corpus-audit.md), not here** — a
debt in two ledgers is the defect `0012` exists to diagnose. **S2's instructions to its own later rounds
are [`0010`](0010-s2-routing.md)** → *Findings that change a later round*.

| | What is wrong | Owner |
|---|---|---|
| **`05 §9`** | Owed the State Hash's cost. It records the full-world double buffer deleted at *"8–15 ms at 1M"*; **one State Hash is 32.47 ms, 2–4× worse**, and the performance strategy does not mention it. What is missing is the statement that **a per-Tick hash is not available at the target** | slice work |
| **`05`** | Strike the ~400k Trips/Day figure — known wrong, still standing in the authoritative document | — |
| **`05 §3`** | Parking Shed invalidation needs the *when you pay / what survives* correction `CONTEXT.md` → Epoch has taken | — |
| **`03 §3.8`** | **Retyped by session D task 3: *measurable*, not a decision.** Run `03 §5.1`'s **spillback** scenario with force-promotion disabled and see whether the upstream Segment blocks. **No session may close it** | milestone 21's acceptance suite |
| **Two section numbers, corpus-wide** | **`02 §6` is *Goods movement*; the sentence is `02 §5.2` step 6.** **`03 §3.6` is the junction blind spot; the attribution sentence is `03 §3.8`.** Wrong in four documents each — a quotation copied forward instead of checked | a sweep |
| **"Zone" for the matrix's granularity** | It is the **District**. `05 §422` and `references.md §2` both say *"zone-to-zone travel-time matrix"*, and a corrected quote is a broken one — so this is a sweep, not a one-line fix | a sweep |
| **`adr/0012` and two other filenames use "Agent"** | Banned outright by `CONTEXT.md`. 33 occurrences across 22 files | a sweep |
| **`plans/0002`'s S0 specification** | Names four clauses as one spike. Split into S0a and S0b everywhere else; the ledger entry still reads as one item | — |
| **S0a's capture is `powersave`** | Every absolute is an **upper bound**; ratios unaffected. It does **not** ride with R7's re-capture — `routing-run.sh` captures S2's harness only. Must record **load** as well, or it reproduces the defect it exists to fix | S0a |
| **The synthetic fixture and `World`'s sizing derivation disagree**, and nothing checks it | `World` allocates 225 Lots and 150 Buildings per 1,000 Citizens; `SyntheticCity` builds **120** of each; **Households land at exactly capacity**, so the first one the simulation creates reallocates the table | slice 7's — the right ratio is a design question and a fixture is not where it gets settled |
| **The end-of-run tier allocates on the Large Object Heap at scale** | ~544 KB at 100k Citizens, ~5.4 MB extrapolated at 1M. Once per run, after the trace is written, so it perturbs nothing today | Fix with a scratch buffer **when a real 1M city shows it**, not on an extrapolation |

---

## Blocked

**There is no red gate left.** [`0003`](0003-build-plan.md)'s gate board is the source. What remains
below is Phase 2 and Phase 3 — everything `0003` does not own.

⚠ **This table is per-milestone and lists the cleared ones too.** It replaced a blanket row that was
narrowed five times, and the split then dropped three milestones silently. ***A blanket row is a status
whose granularity is coarser than the claims it covers***, and ***a per-milestone table that omits the
cleared ones is how the missing ones stay missing***. History in [`0000a`](0000a-board-archive.md).

| | Blocked on | Which is |
|---|---|---|
| **5c** — statistical resolution and the travel-time matrix | ✅ **NOT BLOCKED**, and has not been since before 2026-08-13. All three named gates discharged elsewhere, and **not one closure reached a gate board** — which is why it read as blocked for two days | ⚠ **In flight** — row 1 of *Do these next* |
| **6** — lane-as-entity traffic | 🔴 [`adr/0016`](../docs/adr/0016-the-lane-is-the-entity-not-the-car.md), written from research and never grilled. ⚠ **Partly discharged by measurement**: S5 measured the order-of-magnitude claim the milestone's risk rests on, so a session **may not close** whatever part is measurable | session **G** — ⚠ **and a runnable remainder was parked behind it**: the Lane kernel's 4-thread rung and S5's canonical capture, neither of which was ever G's to give |
| **22** — Stress-driven Fidelity with hysteresis | 🔴 fidelity. ⚠ **The gate is wider than the milestone**: `adr/0005`'s own last line says its **fidelity half was superseded by `0007`**, so 22 is gated on the `0007` half alone | session **E** (its `0007` half), then **5c** for Segment volume |
| **23** — the rotating Audit | 🔴 the same pair as 22 | session **E** |
| **7** — parking | ✅ **Cleared** by session **H**, 2026-08-12 | — |
| **19** — Households, the Unplaced Pool and Departure | ✅ **ASSESSED 2026-08-15, and the answer is that no session gates it — three unbuilt *mechanisms* do.** *(Original row: **no gate is named anywhere, and that is the finding rather than an omission** — unassessed is not the same as unblocked.)* Departure is now **fully specified**: [`adr/0102`](../docs/adr/0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md) makes it a comparison the Household re-runs and [`adr/0103`](../docs/adr/0103-a-need-is-where-a-frequent-private-failure-accumulates.md) closes the Need set at four, so **the design half is done and nothing here needs arguing**. What it cannot proceed without is **(1)** `02 §5.4`'s **residential choice model** — *a housed Departure **is** that mechanism firing* — **(2)** the **Hinterland**, which is the row a Household compares itself against, and **(3)** **money**, which the Destitute channel is denominated in. Needs are a fourth, and two of the four (Education, Health) additionally want **Attended services**. ⚠ **`adr/0102` created dependencies 1 and 2 on the day of this assessment**: a Departure that was a *threshold on a Need* needed neither, and a Departure that is a *comparison* needs both. ***Choosing a mechanism imports its dependencies, and nothing recomputes a gate board when it happens*** — which is 5b's *a decision that removes a representation defers every decision that reads it*, running in the opposite direction | **no session — three unmilestoned mechanisms.** Sequencing them is [`0003`](0003-build-plan.md)'s and session **K**'s, not this row's |
| **20** — Life Stages and self-generation | ✅ **ASSESSED 2026-08-15. One real gate, and it is the ⚫ nobody promoted.** [`0002`](0002-open-questions.md) §F2 marks [`adr/0011`](../docs/adr/0011-household-life-stages-and-self-generating-population.md) **⚫** — Life Stages scheduled in **Days** while the fine wheel's period is exactly one Day — and that is a **live defect in the ADR this milestone builds from**, so it is a gate rather than a debt and is promoted to one here. *A defect against an ADR is a gate on the milestone that reads it, and nothing in this corpus performs that promotion automatically.* Beyond it, 20 now also carries [`adr/0104`](../docs/adr/0104-a-skill-tier-is-earned-by-attendance-and-the-credential-stays-a-wall.md)'s tier step — *a Skill Tier is read off the accumulated Education Need when a Household's stage advances* — which needs **Attended services** to exist, and **Taste** ([`adr/0027`](../docs/adr/0027-preference-is-drawn-per-household-and-persists-for-life.md)), also unmilestoned. It sits **behind 19** by construction, since a Household must be able to form and to leave before its stages mean anything | **`adr/0011`'s ⚫**, then 19, then two unmilestoned mechanisms |
| **14** — the Outside | ⚠ **NOT cleared, and never scheduled.** Session **J** cleared `05 §7`'s **save-format** half and the Outside *layout*; the milestone it was recorded against was **Save/load** (old 10, now **8**), and the Outside itself had a row in `06`'s inventory the whole time. It is now milestone **14**. `plans/0012` *Cause 1* | — |
| **Planning Phase 2 at all** | ✅ **UNBLOCKED 2026-08-16 — session K ran and `06`'s Phase 2 is re-derived.** **5a–5c frozen, then 6 through 24**; all forty inventory rows placed; the dependency graph written down; the five obligations given owners and triggers. ⚠ **Two of K's three gates were never discharged and did not need to be** — E and G move only **21–23**, so the demand spine (6–19) was sequenceable without either, and those three rows are marked *position provisional* rather than held. ⚠ **What K could not settle it says out loud**: 21 and 22 are sequenced around a loop nothing closes — 5c prices congestion on entry to a Segment and routes on free flow, which [`adr/0046`](../docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md) refuses by name — so their **acceptance criteria** cannot be written until G lands | — |
| **Phase 3** | 🔴 **a presentation design that does not exist** | session **L**. ⚠ **S1 and S3 are themselves ungated** ([`0003`](0003-build-plan.md) gives both `Gate: none`), so the head of the chain is runnable and what actually stops it is that **Track B has never been stood up** — a fact about tooling, not a gate |

**Phase 3 is undesigned, not unplanned**, and the distinction describes an absence rather than a choice.
Worse, the interface it would build on was **re-argued to serve something else**: `adr/0002` was rebuilt
around hot and cold query flavours on the finding that it had *"assumed a renderer because rendering is
what an engine boundary is usually for"*, when the actual consumer is an inspector. **The chain is
S1 + S3 → L → Phase 3 is plannable.**
