# 0019 — S5: the Lane kernel

> Spike emitted by [`0017`](0017-session-d-the-traffic-model.md) task 0's typing pass, and its brief
> there is authoritative on scope. Decisions under test:
> [`adr/0016`](../docs/adr/0016-the-lane-is-the-entity-not-the-car.md),
> [`adr/0007`](../docs/adr/0007-stress-driven-simulation-detail.md),
> [`adr/0003`](../docs/adr/0003-deterministic-integer-simulation.md). Design under measurement:
> [`03 §5`](../docs/03-agent-architecture.md).
>
> **This is a planning document and therefore cites rather than owns**
> ([`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)). Every
> figure below names its owner. If this document and its owner disagree, the owner is right.

## Status

**RUN 2026-08-10, all four rounds, twice, plus a BenchmarkDotNet cross-check.** Artefacts in
`spikes/S5.Lanes/results/`:

| Artefact | What it is |
|---|---|
| `s5-all-…-powersave-turbo-cpu2+8-20260811T013611Z.md` | L0–L4, first capture |
| `s5-all-…-powersave-turbo-cpu2+8-20260811T013715Z.md` | L0–L4, second capture. **Two runs are an error bar and one is an assertion** |
| `s5-bdn-…-powersave-turbo-cpu2+8-20260811T013905Z.md` | The BenchmarkDotNet cross-check, and its agreement table |

**The governor was `powersave` and turbo was enabled**, on `taskset -c 2,8`. Root was not taken, so
this is **not** the canonical capture: every absolute below is a **lower bound on this machine's
ability**, and it joins the standing caveat the corpus already carries over every S2 and S0a
absolute. `tools/lane-run.sh` run as root would produce the `performance` capture and the labels
would say so.

**Two of the five tripwires fired, and neither is the one that would have reached a design.**

- **T1 FIRED.** ~327,000 Vehicles per Tick per core with Overlaps, ~380,000 without, against
  `adr/0016`'s 400,000. **The order of magnitude survives the transplant and the figure does not.**
- **T3 FIRED, and hard.** The queue pass is **29× the bare walk over the same arrays**, not the
  *"constant of a `memcpy`"* the ADR claims. L1's attribution says where it goes: the IDM divides
  three times per Vehicle per Tick, and removing the two whose denominators never vary takes 41.14 ns
  to 25.20 ns — **1.63×**, from one substitution.
- **T2 did not fire**, T4 did not fire (break-even residency **1 Tick** against a threshold of 30),
  and **T5 did not fire — 1.00×**, which is the first time in this corpus that a whole-network rung
  has agreed with its fixed-working-set fixture. The scatter ≈1.5 pattern has a **negative** sighting
  at last, and the reason is legible: this kernel is arithmetic-bound, not memory-bound, so there is
  nothing for scatter to move.

**Nothing here has been published.** `docs/spike-results.md`, `plans/0000-board.md`,
[`0002`](0002-open-questions.md) and [`0013`](0013-tick-budget.md) are all untouched by this spike,
and the `adr/0016` amendment T1 calls for has not been written. **Publishing a conclusion is a
separate act that happens after review**, which is also why no ADR cites these numbers yet.

---

**S5 measures what `adr/0016`'s structure costs in `adr/0003`'s arithmetic.** A sorted
one-dimensional queue per Lane, Overlaps exchanged once per Tick, IDM car-following — in Q16.16
through `Borough.Core.Arithmetic.Fixed`, including its `checked` narrowing. Its product is
**Vehicles updated per Tick per core**, and from that the quantity a design can act on: **how many
Microscopic Segments fit in 15.6 ms**. It also prices **promotion and demotion**, which is the one
revisit trigger `adr/0016` names for itself and the one it names no instrument for.

**Risk retired.** [`0017`](0017-session-d-the-traffic-model.md) task 0 found the Microscopic tier
priced entirely on somebody else's engine. `adr/0016` rests its affordability on *"roughly 400,000
individually simulated cars on a single core"* and calls that *"the number that makes microscopic
traffic affordable at all"* — a figure from Citybound, measured in floating point, in a Rust engine,
against a frame rather than against our Tick. Meanwhile [`0013`](0013-tick-budget.md) **has no row
for the Microscopic tier at all**, not even *unbuilt*, which the Event Wheel and Commit both get. So
the movement subsystem is priced in halves: routing carries 60–67 of the ledger's ≥114 points at 4×
and the Lane model carries nothing. **That asymmetry is what made S5 a spike rather than a note.**

**What it must not become: a traffic simulation.** No Road Graph, no Trip generation, no Ruleset, no
content, no junction handover between Lanes. Those are milestone 5b's and milestone 6's, they cost an
order of magnitude more, and they answer a different question.

---

## Gate

**None.** S5 blocks on nothing and contends with nothing, which — as `0017` notes — makes it the
rarest thing in the current corpus. It needs no design decision closed, no entity to exist, and no
other spike's tail.

## Prerequisites

The arithmetic substrate, and nothing else of ours. `spikes/S5.Lanes/S5.Lanes.csproj` compile-links
`src/Borough.Core/Arithmetic/*.cs` rather than taking a project reference, on S2's precedent and for
S2's stated reason: under a project reference *"beyond the substrate"* is a rule nothing checks;
under this, `Fixed`, `IntegerMath` and `Transcendental` are the only things of ours present in the
compilation at all, and reaching for a table is a build error rather than a habit. **`Borough.Analysers`
is loaded as an analyser**, so `BOR0201` makes *no floating point* a build error in the harness as
well as in the kernel — which matters more here than anywhere, because the sentence under test was
measured with floats.

The project is deliberately **not** in `Borough.slnx`. S4 and S2 are both in it "for convenience" and
the convenience has a price: a spike in the solution is a spike the whole-repository build has an
opinion about. `tools/lane-run.sh` builds it by path.

---

## Where the code lives, and that it dies

```
spikes/S5.Lanes/            console project, BenchmarkDotNet, arithmetic substrate by source
spikes/S5.Lanes/results/    stamped captures, timestamped, never overwritten
docs/spike-results.md       the numbers and the decision — written by review, not by the spike
```

`06` is explicit that spike code is throwaway and that the value is entirely in the recorded numbers.
The last task deletes `spikes/S5.Lanes/` and records the deleting commit's parent in
`docs/spike-results.md`, exactly as `plans/0004` does for S4 and `plans/0010` for S2.

---

## Rounds

### L0 — the fixture, the row, and the denominator

Three things, none of which is a benchmark, and the spike is worthless without all three.

- **The units, each derived from a document rather than chosen.** Space is Tiles and a Tile is 4 m,
  from the Cell being 32×32 Tiles at ≈128 m. A Segment is **32 Tiles**, which S2 R0 measured and
  `CONTEXT.md` → Segment calls *"roughly a block-length link"*, with **four Lanes**. Free-flow speed
  is **1.05 Tiles per Tick**, which is `adr/0019`'s own row — 4.2 m per Tick at 8192 Ticks per Day.
- **The Tick is the integration step and there is no substepping to price.** This is worth stating
  because the arithmetic invites the opposite conclusion: 8192 Ticks against an 86,400-second day
  reads as 10.5 simulated seconds per Tick, which no car-following model survives. That is the wrong
  ratio. `adr/0019` derives `TICKS_PER_DAY` **from** car-following resolution — 4.2 m per Tick is 12%
  of the ~36 m safe following distance, against Treiber's Δt ≤ 0.5 s — and a Day is a simulation
  object of 8192 Ticks rather than a converted quantity. **The traffic model is upstream of the
  clock**, and the clock it chose already satisfies it.
- **The row.** Position, velocity, desired speed, Traveller id: four `int` columns, **16 bytes**,
  struct of arrays, one arena. Desired speed is per Vehicle because `UNIQUE INDIVIDUALS` means
  drivers differ, and because dropping it would understate the row by a quarter and flatter every
  bandwidth ratio downstream.
- **The denominator.** The same walk over the same arrays with the arithmetic removed. Measured here
  rather than divided against S4's recorded bandwidth: S4's figure was taken under a different
  governor on a different day, and dividing across that is a ratio between two machines.

*Decides:* whether `adr/0016`'s *"with the constant of a `memcpy`"* has a denominator to be checked
against at all.

### L1 — the queue pass

One Tick of IDM car-following down a sorted queue. **Every Lane is a ring**, so every Vehicle has a
leader and the kernel runs in the congested regime the Microscopic tier exists for rather than in
free flow. A ring is a fixture choice with a stated cost: it has no source and no sink, so it does
not measure Lane-to-Lane handover at a node, which is out of scope and would be a second spike.

Two sweeps and an attribution:

- **Queue length at a fixed Vehicle count**, redistributed across more or fewer Lanes. This is S0b's
  findings 42–43 taken as an instruction rather than as a warning: a sweep that varied the queue
  length *and* the working set together would report their product and call it the queue length.
- **Regime** — occupancy from a quarter of jam to solid. The kernel has three data-dependent
  branches, so flatness across traffic states is a claim rather than an assumption.
- **Where the time goes.** The IDM divides three times per Vehicle per Tick and two of those
  denominators never vary. A variant with those two replaced by precomputed reciprocals is measured
  beside the form as written. **This is an attribution and not a recommendation**: a reciprocal
  changes the arithmetic, so it changes the State Hash, so under `CLAUDE.md`'s own test it is a
  design change however it was motivated.

*Decides:* the unit cost, and how much of the gap from Citybound's figure is the **arithmetic**
rather than the **structure** — which are different findings with different consequences.

### L2 — the network, and the Overlap exchange

The rung is the number of Lanes, each holding what one Lane of a Segment holds at a standstill. This
is the sweep that answers the question: the Microscopic tier's cost is a whole-network cost, and
**a per-queue figure is a laboratory number until a network has produced one** — which is the
sentence `0013` now carries as its general lesson.

Then the Overlap exchange, which is where one dimension buys two and which **has no stated cost
anywhere in the corpus**. `adr/0016` says the exchange happens; what it costs depends on how a Lane
finds the Vehicle near the conflict point, and that is a data-structure decision nobody has taken. So
both plausible answers are measured — a **scan** of the partner's queue, which is what a first
implementation writes, and a **cursor** carried between Ticks, which is O(1) amortised and is state
promotion must materialise.

*Decides:* the headline. Vehicles per Tick per core, and from it Segments in 15.6 ms.

### L3 — promotion and demotion

`adr/0016` names *"promotion cost dominating the traffic budget"* as a condition that would reopen
it, and names no machine that could evaluate it. The condition is a ratio, so the answer is one:
promotion plus demotion per Vehicle, over the cost of running that Vehicle for one Tick, is a
**break-even residency** in Ticks.

Travellers are found through an **intrusive index list** threaded in arrival order rather than in
memory order, because that is the structure `CLAUDE.md` mandates and it is what a promotion will
actually walk. Modelling it as a contiguous scan would have made promotion look free and would have
been a fixture choice rather than a measurement. Queues are **settled** before demotion — a queue
straight out of promotion is at free flow everywhere, and demoting a free-flowing queue is the easy
half of the job.

*Decides:* `adr/0016`'s own revisit trigger, and whether `adr/0007`'s hysteresis window has a floor
it did not know about.

### L4 — the derived product

A view over L0–L3 and never a source, on `0013`'s discipline. Vehicles per Tick per core; Microscopic
Segments in 15.6 ms; break-even residency; and the tripwire table below, **evaluated in code** so
that the verdict is computed rather than asserted and moving a threshold is a diff.

---

## The tripwire, stated before the numbers arrive

Written here so it cannot be adjusted after seeing the result, which is the only way a tripwire
works. **Every threshold is transcribed from a document that already held it** — 400,000 from
`adr/0016`, 2,592 from S2 R2, 3–4× from `plans/0004`'s own tolerance, the traversal time from
`adr/0019`'s row, 1.5× from the corpus's three sightings of scatter. None is fitted to a reading.

> **Disclosure, because the rule is only worth anything if its violations are declared.** L0 and L1
> were run during harness development, before this table was written, and their readings were seen.
> The thresholds above are transcribed rather than chosen, which a reader can check against the five
> documents named; T2, T4 and T5 depend on sections that had not run at all.

| # | Condition | What fails, and what does not |
|---|---|---|
| **T1** | **Vehicles per Tick per core below 400,000** | `adr/0016`'s transplanted headline does not survive our arithmetic and our Tick. **This reaches a sentence, not a design**: the ADR's structural argument — no spatial index, predecessor is the previous array element, scheduling granularity is the Lane — is untouched by the constant. The response is an amendment naming our number, and `0013` gains the row it does not have |
| **T2** | **Vehicles per Tick per core below 186,624** — 2,592 Segments × 72 Vehicles | `adr/0007`'s *"bounded by network stress, not by population"* fails against the only adjacent count the corpus holds. **The count is a fixture size and not a stressed-Segment count**: S2 R2's `v/c` is unbounded because a Traveller there passes through a Segment regardless of load, and its rung is the uniform O-D draw, which `0017` names as the longest-trip distribution available. Quoting it without that sentence is the defect `0017` caveat 2 exists to prevent |
| **T3** | **Queue pass worse than 4× the bare walk** | `adr/0016`'s *"O(n) … with the constant of a `memcpy`"* is false in the letter. `plans/0004`'s own tolerance is 3–4× off a hand-computed ideal, and this is the same test on a kernel rather than on a machine |
| **T4** | **Break-even residency above 30 Ticks** — one Segment traversal at free flow | `adr/0016`'s own named revisit trigger fires, and the ADR names the response: *"wider hysteresis in `adr/0007`, not a third representation."* Above one traversal, the average promoted Segment is paying for a conversion it does not use |
| **T5** | **Network rung more than 1.50× the fixed-working-set queue rung** | The single-queue figure is a fixture number and may not be published as the unit cost. This is the corpus's own recurring shape — S0b findings 42–43, the Zone Rule tripwire, `0011` findings 42–43 — and a **fourth** sighting of scatter ≈1.5 would stop being a coincidence |
| — | Everything within tolerance | The Microscopic tier is affordable at the scale the corpus assumes and `0013` gains a row with a **measured** unit. Record and move on |

**The expected outcome is not the last row.** Every previous spike in this corpus that replaced a
fixture with a real workload came in worse — the Rule unit by 2.8×, Trips per Tick by 32%, the Zone
Rule `sample` dimensionally wrong at scale, and every pre-S0a Tick figure taken over an empty world.
A tripwire whose expected answer is *fine* is S4's shape; this one is not S4's shape.

---

## Acceptance

- A stamped capture in `spikes/S5.Lanes/results/` carrying, for each of L0 through L3, a figure and
  the denominator it divides against, plus the machine, the governor and the run's own contention
  window.
- **Vehicles per Tick per core** and **Microscopic Segments in 15.6 ms** stated as numbers, with the
  Vehicles-per-Segment assumption printed beside them rather than folded in.
- **Promotion and demotion priced separately**, and the break-even residency derived from them.
- The tripwire table evaluated row by row, with the verdict written as a sentence.
- The capture names its governor honestly. If it is `powersave`, the artefact says so **in the
  filename and in the report**, because the corpus already carries a standing caveat that every S2
  and S0a absolute is a `powersave` bound and a fourth one must not be quietly added to it.
- `dotnet build` and `dotnet test` remain green for the repository, which S5 cannot affect: it
  references nothing of ours but three files and is not in the solution.

## Decisions owed by this spike

- **A `0013` row for the Microscopic tier**, which today has none. Its unit is S5's; **its
  multiplicand is 5b's and S5 must not supply one.**
- **Whether the IDM's arithmetic form is a decision.** If the reciprocal variant moves the number
  materially, then *how the IDM is spelled* is hash-bearing and needs a ratifier under `adr/0052` —
  which would be a new §D2 row and is a consequence nobody anticipated when the parameters were
  filed there.
- **What promotion materialises.** If the Overlap cursor is load-bearing for the exchange, it is
  state a promotion has to build and a demotion has to discard, and `03` invariant 3's enumeration
  — queue position, headway, an in-progress Switch Lane traversal — is short by one.

## What this spike deliberately does not do

- **It does not set the Microscopic Cap.** `adr/0062` settled that the Cap counts **Vehicles**, and
  its value is a ratio nobody has both halves of: Vehicles affordable in 15.6 ms is S5's, and
  Vehicles a real city stresses at once is `06` 5b's. **Supplying one side and stopping is the whole
  discipline here** — the corpus has a recorded habit of a number becoming a decision because it was
  the only number in the room.
- **It does not choose the IDM's tuning parameters.** They are `plans/0002` §D2 rows, unset, and
  `adr/0052` binds. S5 uses a **fixture** set with Treiber's published values converted through
  `adr/0019`'s row, states every conversion, and measures regime sensitivity so a reader can see
  whether the answer is a property of the structure or of the tuning.
- **It does not simulate a road network.** No junction handover, no Switch Lane traversal, no Trip
  generation, no source or sink. A Lane is a ring, which is stated in the code and in L1 rather than
  discovered later.
- **It does not decide the fidelity boundary.** That is session E's, and `0017` task 3 already left
  it two constraints rather than a free hand.
