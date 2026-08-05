# Volume is attributed by the Traveller, not by the District pair

**A Segment's volume is incremented when a Traveller enters it and decremented when it leaves. There is
no `in_flight[origin_District][dest_District]` counter, and no periodic distribution of counts along
cached District-pair routes.** A Traveller therefore contributes congestion to exactly the Segments it
experiences congestion on. `LEGIBLE CAUSE`

This supersedes the mechanism in [`03 §3.3`](../03-agent-architecture.md), which those sections are owed
a rewrite for. It does **not** decide where a Trip's Segment sequence comes from — searched per Trip or
shared per origin-destination pair is a performance axis with no correctness content, and spike **S2**
measures it ([`plans/0010`](../../plans/0010-s2-routing.md) R2).

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

**Note which way this cuts against convenience.** Aggregate attribution is the cheaper option and the
one already written down. It is rejected on three correctness grounds, not chosen on a benchmark — which
is the [`05 §4`](../05-technical-architecture.md) rule applied by name: the two schemes produce
**different cities**, so this was never a measurement's decision to make.

## Consequences

- **`03 §3.3`, `§3.4` and `§3.6` are rewritten together.** `§3.3`'s counter and its distribution step go;
  `§3.4`'s circularity argument becomes structural rather than probabilistic; `§3.6` stops being in
  tension with anything.
- **Force-promotion on downstream blocking is no longer justified by lag, and may not be justified at
  all.** Its stated reason was that cycle-driven attribution trails a backward-propagating jam. That
  reason is gone. It may still earn its place on the *second* argument `03 §3.3` gives — that a
  Statistical Segment is structurally blind to a full downstream neighbour — but **that argument must
  now stand on its own**, and it is a smaller claim than the one it was bundled with.
- **The District loses its last physics role and becomes what `CONTEXT.md` says it is**: Goods pooling,
  reporting, and the granularity of the travel-time matrix. Redrawing one can no longer change the
  State Hash through traffic. The matrix remains District-granular; only *attribution* leaves.
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

- **The measured cost lands far above the estimate above.** The arithmetic rests on a vehicle crossing
  about one Segment per Tick, which follows from `TICKS_PER_DAY = 8192` and a block-length Segment. If
  the Segment turns out much shorter than a block — S2 owns the road-density figure that decides it —
  the crossing rate rises and this should be re-priced before it is re-argued.
- **Segment volume proves too noisy to drive hysteresis.** Exact per-Tick volume is spikier than a
  cycle-averaged one, and `adr/0007` already requires hysteresis to stop Segments flickering. If the
  two thresholds cannot be separated far enough to damp per-Tick noise, the answer is a smoothed
  *reading* of an exact count — never a return to attributing volume somewhere a Traveller has not
  been.
- **A future mechanism genuinely needs District-pair flow.** Freight assignment or a transit planner
  might. That is an argument for computing such a matrix *from* Segment volumes as a derived readout,
  not for moving attribution back.
