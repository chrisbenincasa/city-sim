# Volume is attributed by the Traveller, not by the District pair

**A Segment's volume is incremented when a Traveller enters it and decremented when it leaves. There is
no `in_flight[origin_District][dest_District]` counter, and no periodic distribution of counts along
cached District-pair routes.** A Traveller therefore contributes congestion to exactly the Segments it
experiences congestion on. `LEGIBLE CAUSE`

This supersedes the mechanism in [`03 §3.3`](../03-agent-architecture.md), which those sections are owed
a rewrite for. It does **not** decide where a Trip's Segment sequence comes from — searched per Trip or
shared per origin-destination pair is a performance axis with no correctness content, and spike **S2**
measures it ([`plans/0010`](../../plans/0010-s2-routing.md) R2).

> **AMENDED by S2 R2, on evidence. This corrects a sentence, not the decision.** The paragraph's
> *"a performance axis with **no correctness content**"* is wrong on two counts. **Everything above it
> survives untouched** — a Traveller increments whatever it actually drives, so experience and
> contribution remain the same list of Segments under every rung, which is the whole of what this ADR
> decides. Numbers in [`spike-results`](../spike-results.md) → *S2 R2*.
>
> **The axis has three rungs, not two, and the third is the one this ADR created.** Direct attribution
> needs a **next Segment** every Tick, and a path is only one way to supply one: a **next-hop table**
> supplies one and stores no path at all. That rung is absent from `plans/0010` because the plan
> predates this ADR. **Searched is out on arithmetic rather than on a benchmark** — 716,800 ns per Leg
> against ~550 arrivals per Tick is ~400 ms of searching per 15.6 ms of budget — so the axis that
> survives is between the two coarse rungs.
>
> **Correctness content, first count: the detour, which is statistical.** A route shared per District
> pair costs **36.01%** mean detour (p90 **71.39%**) and a next-hop table **18.52%** (p90 **40.70%**),
> against a per-Trip search's zero. ⚠ **Both figures are taken on S2 R2's *uniform* origin-destination
> draw, and R4 later showed that draw is the longest-trip distribution available**: on a local draw the
> next-hop table's 18.52% becomes **128.82%**, which R4 calls *"a different city"*. The comparison below
> survives — both schemes were measured on the same draw — but neither number may be carried out of it
> as *the* cost of a path source. `plans/0012` **Cause 5**.
>
> The gap between the two is structural rather than incidental: a
> shared route is coarse at *both* ends, since the Traveller must reach the origin representative
> before the stored route means anything, while a next-hop table is followed from wherever the
> Traveller actually is and is coarse only on the destination side — worth almost exactly half the
> error, measured. **A Traveller driving 36% further is a different Trip, which under
> [`05 §4`](../05-technical-architecture.md) is a different city.** The figures are taken at node
> granularity and are therefore an upper bound.
>
> **Correctness content, second count: the representative funnel, which is structural and is the larger
> one.** Under either coarse rung *every* Trip bound for a District arrives through that District's
> **one representative node** — a shared route ends there, a next-hop column is a tree rooted there —
> so the arcs into it carry the whole of the District's inbound traffic. The same surge drives the
> watched arc to **412%** `v/c` under shared routes against **130%** under a per-Trip search. **The
> representative is not a summary of the District under these rungs; it is a hole every Trip is
> threaded through**, and a fidelity model that promotes on `volume / capacity` promotes there and
> nowhere else. That is the defect class `03 §3.9` rejects for the Microscopic Cap and that this ADR
> rejects for volume, arriving a third time by a different door. **Nothing in the corpus addresses it**;
> it is [`plans/0002`](../../plans/0002-open-questions.md)'s routing cluster, `plans/0010` decision 11,
> and this ADR does not settle it.
>
> **Argument 2 in *Why* below was also measured, and it is worse than the lag it describes.** R2b found the
> aggregate scheme does not report a jam late — it does not report it at all. Direct lag is zero at
> every rung and aggregate's lag reads *never* even at a **one-Tick** cycle, where there is no cadence
> left to blame: under a next-hop path source the smear deposits **0.00%** on a Segment direct reports
> at **108.51%**. **It is a *place* defect and no cadence fixes a place.** The two schemes agree closely
> on *how many* Segments are stressed (2,592 against 2,714 over an 80% threshold) and disagree
> completely on *which* — the shape most likely to pass an aggregate sanity check while being wrong
> about every individual road. This strengthens the Consequences' force-promotion bullet without
> resolving it: the lag justification is not merely superseded, it described the wrong defect.

## Why

**The corpus answered this question twice, differently, in one document, and nobody had noticed.**
`03 §3.3` attributes volume by District pair; `03 §3.6` animates a Traveller *"along its route"*. Those
are two different routes per Trip. Something had to give, and three arguments point the same way.

**1. Aggregate attribution breaks the assumption `03 §3.4` calls load-bearing.** That section's defence
of the whole fidelity model is that the circularity is *self-correcting in the dangerous direction*:
*"VDF underestimates congestion → routing over-uses the segment → volume rises → threshold crossed →
microscopic simulation finds the truth. The failure feeds the detector."* **That chain only closes if
the Segments a Traveller uses are the Segments it raises the volume of.** Under District-pair
attribution they are not — a Traveller experiences congestion on its own route and deposits congestion
on the District pair's route — so the failure feeds a *different* detector, watching different Segments.
`03 §3.4` names this as *"the load-bearing assumption of the whole scheme"* and adds that *"a change
that breaks it breaks everything downstream."* Direct attribution repairs it **by construction** rather
than by argument: experience and contribution are the same list of Segments, necessarily.

**2. Aggregate attribution lags the event it exists to catch, and `03 §3.3` says so itself.**

> *"a jam propagates backward at roughly 15 km/h — faster than any cycle worth running — so a
> cycle-driven region always lags the jam during exactly the event it exists to capture."*

That admission is why `03 §3.3` had to invent a **second trigger**, force-promotion on downstream
blocking, purely as compensation. Under direct attribution volume is exact every Tick and the lag has
nowhere to live. **A mechanism that exists to patch a compression is not a mechanism the design chose**,
and the compression had never been priced.

**3. Under `02 §2.1` a District is player-adjustable, which would make an organisational act change the
city.** `02 §2.1` settles District authorship as *both* — automatic by default, **player-adjustable as
an advanced action**. If the District pair keys both the volume counter and the cached route, then
redrawing a boundary changes volume attribution → Stress → Fidelity → travel times → the city, and
therefore the **State Hash**. That is the defect class `03 §3.9` rejects for the Microscopic Cap in
words that transfer unchanged — *"anything the host could vary must not be able to change an
outcome"* — except here it is the player varying it, through an affordance the design presents as
naming and grouping. `PLAYER GOVERNS` means the player governs the city, not the physics.

**The cost is bounded, small, and was never measured before being designed around.** A Tick is ~10.5
in-world seconds and a Segment is roughly a block, so a vehicle crosses **about one Segment per Tick**:
order 80,000 increment/decrement pairs per Tick at 1M, into a ~30,000-entry array of about 120 KB that
sits in L2. S4's **K2** — random gather by generational handle — is that inner loop and is already
measured on two machines. The aggregate scheme is *cheaper* at the District count `CONTEXT.md` now
anchors (~128 Districts on a full map puts it near 819,000 writes per congestion cycle against direct's
80,000 per Tick, so it wins for any cycle longer than ~10 Ticks). **We are knowingly paying for
correctness, and the price is one L2-resident array.**

> **The ~10 Ticks is measured at 105, and the last sentence overstates what is being paid.** S2 R2a
> priced the crossover on a measured crossing rate of 0.79–0.83 and a conserving smear: direct
> attribution is the **cheaper** scheme for any congestion cycle shorter than about 105 Ticks at the
> anchor. Detail and the peaking sweep are under *What would trigger revisiting*, where this arithmetic
> was filed as the trigger it discharged.

**Note which way this cuts against convenience.** Aggregate attribution is the cheaper option and the
one already written down. It is rejected on three correctness grounds, not chosen on a benchmark — which
is the [`05 §4`](../05-technical-architecture.md) rule applied by name: the two schemes produce
**different cities**, so this was never a measurement's decision to make.

## Consequences

- **`03 §3.3`, `§3.4` and `§3.6` are rewritten together.** `§3.3`'s counter and its distribution step go;
  `§3.4`'s circularity argument becomes structural rather than probabilistic; `§3.6` stops being in
  tension with anything.

  > **Written, carrying S2 R2's evidence — except the force-promotion clause below, which is a decision
  > and stays open.** `§3.3`'s counter and distribution step are gone with the superseded text kept
  > beside them; `§3.4`'s closure is now structural, with the **representative funnel** recorded there as
  > the exposure it does *not* cover. **The third section is `03 §3.8`, not `§3.6`** — the *"animated…
  > along its route"* sentence this ADR quotes lives there, and `§3.6` is the low-volume junction blind
  > spot, which has nothing to do with attribution. This ADR, `plans/0010` and the board all carry the
  > same mis-citation; it needed no rewrite either way, since the bullet is the half of the contradiction
  > that survived.
- **Force-promotion on downstream blocking is no longer justified by lag, and may not be justified at
  all.** Its stated reason was that cycle-driven attribution trails a backward-propagating jam. That
  reason is gone. It may still earn its place on the *second* argument `03 §3.3` gives — that a
  Statistical Segment is structurally blind to a full downstream neighbour — but **that argument must
  now stand on its own**, and it is a smaller claim than the one it was bundled with.
- **The District loses its last physics role and becomes what `CONTEXT.md` says it is**: Goods pooling,
  reporting, and the granularity of the travel-time matrix. Redrawing one can no longer change the
  State Hash through traffic. The matrix remains District-granular; only *attribution* leaves.
> **⚠ AMENDED 2026-08-12 by milestone 5b task 5. This decision is untouched; its *schedule* was wrong,
> and the two Consequences below are built to different depths as a result.**
>
> **The columns exist and the increment cannot.** 5a built `VolumeForward` and `VolumeBackward` as
> `(saved AND hashed)` per-Tick state, discharging the first bullet below in full. 5b built the
> invariant in the second — `Invariant.SegmentVolumeIsConserved`, whole-world tier. **Nothing
> increments either column, and nothing can**: the amendment at the top of this ADR says direct
> attribution *"needs a **next Segment** every Tick, and a path is only one way to supply one"*, and
> [`adr/0075`](0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) gives a Leg **a cost and no path**
> while no next-hop table exists anywhere in `Borough.Core`. **Volume waits on a path source**
> (`plans/0010` decision 11), not on vehicles.
>
> **The distinction matters because a second, true reason was in the way.** Only vehicular Legs
> increment and 5b resolves walk Legs only, so *"nearly vacuous in this slice"* is what 5b's brief
> carried — correct, and predicting that a vehicular Leg would make it work. It would not. Filed in
> [`plans/0012`](../../plans/0012-corpus-audit.md).
>
> **The invariant is written and vacuously satisfied, deliberately.** Both sides are zero and the check
> has a test that writes the violation by hand, because the alternative is a conservation check authored
> by whoever adds the first vehicular Leg, at the moment they are least able to notice the pairing is
> wrong.

- **Segment volume becomes hot per-Tick state on the Segment table**, `(saved AND hashed)` under
  `adr/0003`'s single declaration, incremented and decremented on Segment boundary crossings. It is a
  count of Travellers present, so it is conserved by construction and testable as such.
- **A new invariant belongs with the definition of done**: summed Segment volume equals the number of
  in-flight vehicular Travellers, every Tick. This is the same shape as milestone 8's parking-occupancy
  invariant, and for the same reason — a Traveller that vanishes without decrementing destroys the
  reading permanently, which is an `adr/0006`-class defect that presents as a road that looks busy
  forever.
- **Walk Legs still contribute nothing.** `CONTEXT.md` → Fidelity keeps pedestrians out of Stress
  entirely, so only vehicular Legs increment. That is what keeps the per-Tick figure at ~80,000 rather
  than at the full Leg count.
- **S2 still measures the cost**, and the decision now has a price attached rather than an assumption.
  `plans/0010` R2a's crossover survives as a measurement of *what this costs*, not of *whether to do
  it*.

## What would trigger revisiting

- ~~**The measured cost lands far above the estimate above.**~~ **DISCHARGED by S2 R2, and it moved the
  other way.** The arithmetic rests on a vehicle crossing about one Segment per Tick, which follows from
  `TICKS_PER_DAY = 8192` and a block-length Segment. If the Segment turns out much shorter than a block —
  S2 owns the road-density figure that decides it — the crossing rate rises and this should be re-priced
  before it is re-argued.

  > **The measured rate is 0.79–0.83 per vehicle per Tick, not 1.0**, reported at free flow *and* at the
  > morning peak because this ADR's estimate is a free-flow one and the simulation is not — congestion
  > lowers the rate and lowers the scheme's cost with it, so quoting only the congested figure would
  > credit direct attribution for a saving the jam paid for. The ~80,000 increment/decrement pairs per
  > Tick at 1M is therefore an **overestimate by about a fifth**, which is the opposite direction to the
  > one this trigger anticipated.
  >
  > **And the crossover is 105 Ticks at the anchor, an order of magnitude past the ~10 estimated above.**
  > This ADR reasoned from an assumed crossing rate and an unweighted smear; the rate is now measured and
  > the smear implemented in its **conserving** form — a Traveller on a route of total time `T`
  > contributes `t_s / T` to each Segment, so the shares sum to one and this ADR's invariant holds, where
  > adding the whole pair count to every Segment would have put one vehicle on fifty Segments at once and
  > **made the price of rejecting the alternative look smaller than it is**. So direct attribution is the
  > *cheaper* scheme for any congestion cycle shorter than about 105 Ticks, and **this ADR's *"we are
  > knowingly paying for correctness"* understates its own case**: at plausible cycle lengths it is not
  > paying at all. Where the crossover inverts is a peaking question — a 50-Tick cycle needs a 2.12× peak
  > and a 25-Tick cycle 4.25×, against a generator mix that caps the peak near 3× — and the peaking factor
  > is itself unsized, so that is a curve rather than a verdict.
- **The path source landing on a District-granular rung**, which is the exposure the amendment above
  opens and this ADR cannot close. Direct attribution guarantees that the Segments a Traveller raises are
  the Segments it drives; it guarantees nothing about *which* Segments those are. If a Statistical Trip's
  route is District-granular, they are the ones a partition chose, and Stress on a representative node is
  an artefact rather than a property of the city. That does not reopen attribution — it means this ADR's
  repair is exact at the Segment and silent about the route, and the guarantee should be quoted that
  narrowly until `plans/0010` decision 11 is answered.
- **Segment volume proves too noisy to drive hysteresis.** Exact per-Tick volume is spikier than a
  cycle-averaged one, and `adr/0007` already requires hysteresis to stop Segments flickering. If the
  two thresholds cannot be separated far enough to damp per-Tick noise, the answer is a smoothed
  *reading* of an exact count — never a return to attributing volume somewhere a Traveller has not
  been.
- **A future mechanism genuinely needs District-pair flow.** Freight assignment or a transit planner
  might. That is an argument for computing such a matrix *from* Segment volumes as a derived readout,
  not for moving attribution back.
