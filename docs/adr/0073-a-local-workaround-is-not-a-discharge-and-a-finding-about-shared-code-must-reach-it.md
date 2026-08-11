# A local workaround is not a discharge, and a finding about shared code must reach the shared code

**When a spike measures something and the cause turns out to lie in code the spike does not own —
`Borough.Core`, the arithmetic substrate, a Ruleset, an analyser — the finding is routed to that code or
to a **named document with an owner**, on the day it is found, whether or not the spike has already
worked around it. **Working around it locally does not discharge it.** A remark in the spike's own source
is not a route, because the spike's source is deletable and its author is the only person who will ever
read it.**
`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `FAST ITERATION`

This is not one of the three inference rules.
[`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md),
[`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) and
[`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) govern what a sitting may
**conclude** — from a claim, a number, an absence. This governs what a spike must **do with what it
already found**, and it is closer in kind to
[`adr/0042`](0042-a-planning-document-cites-and-a-design-document-owns.md): a rule about which artefact
holds a fact, extended from documents to code.

## Why

**It has happened, it cost a slice, and the author who lost it did everything else right.** S2 R2 was
profiling A\* and found the expansion loop dominated by an unexpected term.
`spikes/S2.Routing/Routing/Heuristic.cs` records it verbatim:

> *"`Fixed.Div` routes through `IntegerMath.FloorDiv`, which costs a `/` and a `%`. That is **four
> 64-bit hardware divisions per node**, and in the first capture it was most of the denominator: 231 ns
> per expansion, against a CSR walk that should cost tens."*

Three paragraphs later, under its own bold heading: *"**Worth recording beyond this spike.**"* It was
recorded nowhere beyond that spike. S2 hoisted the division out of its inner loop — `TicksPerTile`
inverts once per query — published every routing number on the corrected path, and left
`IntegerMath.FloorDiv` exactly as it found it. On 2026-08-10 S5 hit the same defect in a kernel that
**cannot** hoist, because its divisors are a per-driver desired speed and a per-Tick gap; measured
**1.50×**; and published tripwire **T1** against
[`adr/0016`](0016-the-lane-is-the-entity-not-the-car.md) on that basis. The actual fix, found on
2026-08-11, is **reordering two operands of an `&&`**: the sign test before the modulo, so the modulo is
not evaluated when the signs agree. Bit-identical by construction, no State Hash moved, 1,060 tests
green. T1 is withdrawn.

**The mechanism is the nastiest part, and it is why exhortation would not have worked.** *A local
workaround removes the finder's own exposure, and with it the only pressure that would have fixed the
source.* S2 immunised itself in an afternoon and had, from that moment, no reason ever to think about
`FloorDiv` again — so the defect survived precisely **because** it was found by a competent author who
fixed his own problem. Nothing in the corpus was watching, because nothing was broken any more from
where the watcher stood.

**Both spikes misattributed the cost to a design commitment they happened to be exercising, and that is
the tell.** S2's remark is headed *"where routing on time is actually expensive"* and reads the four
divisions as the price of the corpus's time-valued cost function — the SC4 argument, a real decision with
an ADR. S5 attributed its 1.5× to `adr/0003`'s integer arithmetic — also a real decision with an ADR.
**Neither wrote *`FloorDiv` is doing a modulo it does not need*.** A spike exists to price a design
commitment, so when it finds an unexpected cost while exercising one, the commitment is the nearest
available explanation and it is *plausible* — which is the failure mode. **A cost measured while
exercising a decision is not thereby a cost of that decision**, and the substrate has to be read before
the attribution is made.

**The local fix was not free, which is the part that reads as pure loss in hindsight.** S2's hoist costs
about **two parts in ten thousand** of heuristic tightness — the reciprocal is floored and the product is
floored, so it stays admissible but under-shoots slightly, and the ladder's expansion counts can see it.
That was a sound trade against a hardware division. It was **not** a sound trade against reordering two
operands, and S2 had no way to know which trade it was making, because the alternative was in a file it
had already stopped suspecting.

**The remark lived in a file whose stated destiny is deletion.** The S2 harness is **29,719 tracked
lines awaiting deletion**, held today only because another session is still working inside it. Had that
deletion landed on schedule, the single copy of a finding about `Borough.Core` would have gone with it —
a finding that had, by then, already achieved nothing. **A spike is a scaffold; the corpus is the
building.** Anything written in a scaffold is written in pencil.

**This is [`plans/0012`](../../plans/0012-corpus-audit.md) *Cause 1* on a third axis.** That cause has so
far been *a fact with two copies, one of which drifted* and *a fact with no copy, re-derived wrongly from
the shape of its absence*. This is *a fact with exactly one copy, in the wrong artefact* — which behaves
like no copy at all, because the artefact is invisible to every reader who is not already inside it.

**Why *"worth recording beyond this spike"* is not a route.** It names no document, no owner and no
trigger, and so it is the same failure `adr/0052` catches in numbers: **a category is not a name.** The
author had the finding, the measurement, the diagnosis and the intent, and the sentence still produced
nothing, because there is no process that consumes an intention. `0002` consumes a question, `0003`
consumes a slice, `0012` consumes a correction, `0013` consumes a cost. A sentence addressed to nobody is
consumed by nobody.

## Consequences

- **A spike that measures code outside its own directory files the finding before it works around it.**
  The order matters: the filing is what survives, the workaround is what makes the spike runnable again,
  and doing the workaround first is how the filing stops feeling necessary. This is the whole rule; the
  rest is routing detail.
- **Findings route by what they are, exactly as absences do under `adr/0070`.** A **defect** in shared
  code → fix it there, or `plans/0003` if it is not a five-minute change. A **cost** that a consumer will
  meet → [`plans/0013`](../../plans/0013-tick-budget.md), which exists for precisely this and whose
  organising column is whether a row was measured or guessed. A **question** →
  [`plans/0002`](../../plans/0002-open-questions.md), typed per `adr/0043`. A **document that is now
  wrong** → `plans/0012`. There is no fifth class, and *"a comment in the spike"* is not one of the four.
- **A local workaround carries a marker at the workaround site naming what it works around and where the
  finding went.** `spikes/S2.Routing/Routing/Heuristic.cs` is owed one — deferred only because another
  session is working in that harness today. The marker is what makes the workaround **reversible**: S2's
  two-parts-in-ten-thousand of tightness is now buyable back, and nobody would have known to look.
- **A spike's attribution of an unexpected cost to the design commitment it is exercising is suspect
  until the substrate has been read.** Both instances here were plausible, cited a real ADR, and were
  wrong. The cheap discharge is to read the implementation of the primitive in the inner loop before
  writing the sentence — S5's whole finding is four lines of `IntegerMath`.
- **`adr/0043` gains a corollary in the other direction.** That rule stops an argument settling what a
  measurement could settle. This one stops a **measurement** settling nothing at all: a number that
  reaches only its own harness has not been published, however carefully it was taken.
- **It does not require fixing the source before continuing.** Blocking a spike on a `Core` change would
  be worse than the disease and would guarantee the rule is ignored. Work around it, ship the spike,
  route the finding — three acts, and only the third is new.

## What would trigger revisiting

- **The ledgers filling with findings nobody acts on.** The bar is a **measured** effect on shared code,
  not any observation about it. If `0002` and `0013` start accreting spike micro-observations, the bar is
  set wrong and the fix is to raise it to *the finder changed their own code because of it* — which is the
  exact signal S2 emitted and nobody caught.
- **Spikes ceasing to compile shared code in by source.** `spikes/S2.Routing/` and `spikes/S5.Lanes/`
  both compile the arithmetic substrate in and can name nothing else of `Core`, which is what makes their
  measurements bear on the real thing and this failure possible at all. A spike that vendored its own
  copy could not produce a finding of this class — and would be worth less for the same reason.
- **A third instance after this ADR.** The rule would then be understood and disobeyed, which is a
  different problem from being unstated, and the fix is mechanical rather than editorial: a spike's
  close-out checklist item, or a lint on `Borough.Core` symbols appearing in a spike's profiler output.
- **The spike track ending.** Phase 2 is the last phase with spikes in the plan. If measurement moves
  wholly inside the build, the finder and the owner become the same person and the routing problem
  dissolves.
