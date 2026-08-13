# The Microscopic Cap derives from the design speed's budget, and not from the top rung's

**The Cap is priced against a 62.5 ms Tick — 1×, the speed `01 §1` says the game is designed to be
played at — and not against 15.6 ms.** Setting it at the top rung buys a stutter-free fast-forward by
making every player's traffic permanently less accurate, which is
[`03 §3.9`](../03-agent-architecture.md)'s own table read backwards: a **simulation** limit and a
**hardware** limit are different boxes with different responses, and the top rungs belong in the second.

Two things are **recorded rather than decided** and are the other half of this ADR: **a fallback tier
below Microscopic will probably be needed**, and **a 2- and 4-thread measurement of the Lane kernel is
owed**, because every figure the Cap has ever been quoted from is one core.

Guiding concepts: `HONEST DEGRADATION`, `SOLVE THE ACTUAL PROBLEM`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
as to the basis — which of two degradations a cost belongs to is a design judgement. **The Cap's value
remains unset and measurable**, and nothing here sets it.

## Why

### `03 §3.9`'s table already assigns this, and pricing at 4× puts the cost in the wrong box

That section separates two things the design had been conflating:

| | Governed by | Response |
|---|---|---|
| The **simulation** hits its limit | `HONEST DEGRADATION` | more of the traffic overlay reads *modelled* rather than *exact* |
| The **hardware** hits its limit | Speed — a host concern the simulation cannot observe | fewer Ticks per second, plus a *simulation running behind* indicator |

**Deriving the Cap so that 4× always fits is choosing the first response to avoid the second.** It lowers
fidelity on every machine, permanently, in every city, so that a fast-forward rung never stutters on any
of them. The correct assignment is the other way round: set the Cap where the game is played, and let a
machine that cannot sustain 4× at that fidelity **dilate wall-clock time and say so** — which is
[`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md)'s surviving rider (*"dilate
wall-clock time, never skip Ticks"*, Factorio's documented behaviour) and costs nothing in
reproducibility, because the Cap itself is still identical on every machine.

**It is also the rung the binding case will not be running.** The cities with enough stressed Vehicles to
reach the Cap are the large ones, and `01 §1` now states that 4× is *"the first thing a large city stops
offering"*. So 15.6 ms is the budget of a speed that, exactly when the Cap matters, is not on the menu.

**What this is not** is a licence to spend four times as much. The Cap is a world constant and `03 §3.9`
requires it to be *"derived from the world rather than being a bare number"*; what changes here is the
**basis of that derivation** and nothing else. A world small enough to offer 4× is a world whose stressed
Vehicle count is nowhere near any of these figures.

### The number that made this look urgent was a ratio with no denominator

The trigger for this sitting was a claimed gap of **27–58×** between what the Lane kernel can afford and
what a city demands. **The demand side of that ratio does not exist.** The figure used — 186,624 — is
S2 R2's synthetic fixture population, and [`plans/0013`](../../plans/0013-tick-budget.md) labels it in
the table cell itself:

> | 186,624 — S2 R2's fixture, **not a stressed count** |

and closes that section with the warning the ratio then walked into:

> *"How many Vehicles a real city stresses at once is milestone 5b's and does not exist, which is why **no
> row in this table claims a share for it**. A number becoming a decision by being the only number in the
> room is a habit this corpus has already recorded, and **this table is where it would happen**."*

It happened, in [`0094`](0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md),
one day after that paragraph was last read. **The Cap counts Vehicles in *stressed* Segments and 186,624
is a whole fleet**, so the two quantities are not comparable at all and the ratio was never a ratio.

***A caveat attached to a number does not travel with it.*** This is now
[`plans/0012`](../../plans/0012-corpus-audit.md) **Cause 5**, filed on this sighting: a figure is quoted
correctly, its qualifying clause is left where it was, and **the two documents agree to the last digit**,
so no comparison between them can see anything wrong. It is Cause 4's sibling with the polarity reversed —
there the source sentence is wrong, here it is right and was abandoned — and its tell is *worse* than
nothing, because a bare figure accumulates apparent authority every time it is repeated. The corpus had
coined the lesson twice already, in `plans/0002` (*an unratified number is more dangerous than an open
question*) and in the paragraph above, and left it as commentary both times. ***An aside is not a rule***,
now evidenced on two separate causes.

### And the comparison held the budget fixed while the clock moved

The second half of the error is arithmetic. `adr/0094` quadrupled the sub-step ratio and the claimed
collapse was measured at a fixed 15.6 ms, which double-counts, because the budget a Tick gets is a
function of the same ladder. One core, at S5's slower reading of 29.3 ns a Vehicle a sub-step:

| Tick budget | 8192 — 21 sub-steps | 2048 — 84 sub-steps |
|---|---:|---:|
| **1× — 62.5 ms** | 101,600 | **25,400** |
| 2× — 31.25 ms | 50,800 | 12,700 |
| 4× — 15.6 ms | **25,400** | 6,350 |

**2048-at-1× and 8192-at-1×** differ by four, as `adr/0094` says they must. But **2048-at-1× and
8192-at-4× are the same number**, and it is the second pair the design actually cares about, because the
first clock's design speed was never where this was priced. Under the basis decided here, `adr/0094`
moves the Cap's supply side to **exactly what the corpus has been quoting all along**.

### Every figure in this ADR is one core, and that is the largest unexamined multiple

S5 ran single-threaded throughout, deliberately — *"the kernels that will be parallelised are decided by
[a later spike]"*. So the supply side of the Cap has never been measured at the thread count the game
will actually ship at, and the Lane kernel is a plausible candidate to thread well: `05 §4`'s lint 4
requires thread-count equivalence, a Lane's pass is over its own queue, and S5 measured that pass at
**17–29× a bare walk**, which is the signature of compute rather than of streaming. The bandwidth curves
in `spike-results` — 1.83× on six desktop cores, 3.75× on twelve M4 Pro threads — are about a different
kind of kernel and must not be borrowed.

**The measurement owed is 2 and 4 threads specifically**, not a full sweep. Those two rungs answer the
only question that matters here: whether a compute-bound queue pass behaves like the bandwidth curves at
all. If it scales near-linearly to 4, the supply side is roughly four times every figure above and this
question closes; if it is flat at 2, the one-core numbers are the real ones and the fallback below stops
being a probability.

### A fallback tier is foreseen, and deliberately not designed here

What `03 §3.9` has today is **binary**: a Segment is Microscopic, or its travel time comes from the VDF,
which §3.2 establishes is *structurally wrong* exactly where the Cap binds. That is a sharp edge in the
one place accuracy was supposed to matter most, and the shape of the repair is fairly obvious — a cheaper
middle tier, a queue without full car-following, between the two.

**It is not designed here and must not be.** The demand side is unmeasured, so *given the Cap is too
small, should something compensate?* is [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s
void question in its exact stated form, and the Lane kernel it would sit beside is unbuilt. What is
recorded is the **expectation**, so that a third tier arrives as a foreseen obligation rather than as a
discovery — and so that the two things which would make it unnecessary are named and can be watched.

## Consequences

**The Cap's derivation basis is a 62.5 ms Tick.** At the shipped clock, one core and 29.3 ns, that is
**~25,400 Vehicles**; at 27.4 ns, ~27,100. Neither is the Cap — the Cap is a ratio and this is one side
of it — and both figures are `powersave` lower bounds owed a re-take.

**`adr/0094`'s 27–58× claim is withdrawn, and its ÷4 stands with a rider.** The sub-step ratio really does
go 21–45 → 84–180 and the per-Tick cost really does quadruple; what is withdrawn is the comparison
against 186,624 and the pricing at 15.6 ms. `plans/0002` §D2, `plans/0013`, `CLAUDE.md` and
`plans/0000-board.md` all carry the corrected form.

**Over-running at 3× and 4× is the host's business.** A machine that cannot sustain them at this fidelity
dilates and shows *simulation running behind*, per `03 §3.9`'s second row and `adr/0019`'s rider. No
Input Log produces a different city on a different machine, which is the property `§3.9` protects and
which a host-tunable Cap would destroy.

**A 2- and 4-thread Lane kernel measurement is owed to S5** and is filed to `plans/0002` as measurable
with those two rungs named. It is the largest unclaimed multiple on the Cap's supply side.

**A fallback tier below Microscopic is recorded as likely** in `03 §3.9` and in `plans/0002`, as an
expectation with no design attached. Two things would retire it: the threading measurement coming back
near-linear, or a real stressed-Vehicle count landing under the supply figure.

**The Cap stays unset.** Its demand half has never been measured and nothing here measures it. What
changes is that the supply half is now quoted at a defensible rung and the ratio is no longer quoted at
all.

## What would trigger revisiting

**A real stressed-Vehicle count.** This is the measurement the Cap has been waiting for since `adr/0062`,
and it is a traffic model's to produce. If it lands under the supply figure at 1×, the Cap is *"not a
failure mode"* in the strong sense and the fallback tier is never built.

**The threading measurement coming back flat.** If the Lane kernel does not scale to 2 and 4 threads,
every figure here is final, the fallback tier becomes near-certain, and the sub-step ratio — which
`adr/0082` already names as the first lever — is what has to move.

**The speed ladder changing.** This ADR is keyed on 1× being the design speed and 4× being the rung a
large city withdraws. If either moves, the basis moves with it, and the derivation should be re-read
rather than the number re-tuned.
