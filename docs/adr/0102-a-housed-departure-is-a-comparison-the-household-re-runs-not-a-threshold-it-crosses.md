# A housed Departure is a comparison the Household re-runs, not a threshold it crosses

**A Household leaves because a Hinterland scored better than where it lives, and for no other reason.
Three mechanisms do three jobs and none of them is a timer: the **Need** accumulates, the
**comparison** decides, and a **cadence** decides only when the Household looks. `04 §6` step 7's
*"sustained degradation"* names no duration, authorises no number, and describes what the Need already
does.**

`LEGIBLE CAUSE` `EMERGENCE`

Settled with the user in the room on 2026-08-15, grilling `04 §6` — the seven-step chain from a farm's
output falling to a housed Departure, never previously examined.

## Why

### The corpus had already decided this, in the one place nobody reads for decisions

[`CONTEXT.md`](../../CONTEXT.md) → *Terms we deliberately do not use* settles it in a sentence, while
refusing something else:

> **"Tax tolerance"** — no such scalar. Tolerance is emergent: a Household compares the city against a
> Hinterland using **the same utility function everyone uses**, and leaves or does not. **Any named
> tunable that is really an outcome of the choice model should be suspected of the same error.**

`02 §5.4` builds the machinery: a logit choice over scored candidates in which the Hinterland is **an
ordinary row** — *"a prospective Household compares staying outside against moving here with the
identical utility function"* — and a Hinterland is authored with median rent, service levels **and a
commute figure**. So leave-or-stay was a comparison before this sitting opened, and `04 §6` step 7 has
been describing a second, incompatible mechanism in the same corpus.

***A banned term carries the decision that replaced it***, which is why it was found in the list of
words rather than in a section about Departure.

### A threshold is a cliff, and this corpus refused one three days ago

A duration crossing a line is [`adr/0053`](0053-failure-pressure-is-a-duration-not-a-tally.md)'s
**Failure Pressure** — the *Building* mechanism, whose argument is explicitly that abandonment **has no
actor**: *"a Building does not fall over because somebody looked at it."* A Household has an actor. It
is the one object in the design that decides things about itself.

Under the threshold reading a Household on a bad commute leaves at the same moment whatever the outside
world looks like. Under the comparison it leaves sooner where a Hinterland is better and not at all
where one is worse, which is the gradient
[`adr/0095`](0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md) argued for on
2026-08-13 in almost these words: *a single threshold makes a cliff out of `adr/0017`*. Taking the
threshold here would reintroduce, three documents away, the shape that ADR had just removed.

### The duration does not move into the cadence, and the corpus has already refused that too

The tempting repair is to say the *sustained* duration survives as the re-evaluation period — the
Household compares every `C` Ticks and whatever is true then decides. That collapses two things
[`CONTEXT.md`](../../CONTEXT.md) → *Zone Rule* keeps apart:

> **The sample is when the city *notices*, not when the Building *fails*.** Sampling the accumulation
> rather than the observation would give every condemned Building a random lifetime whose distribution
> is set by sample size and Lot count, which models nothing.

`01 §6` states it generally: *"what the cadences bound is **perception, not response**."* A cadence that
carries the accumulator's job sets a Household's departure timing by the phase of its own timer rather
than by how bad things got.

**The accumulator does not need re-adding, because it is already there and it is the Need.** `04 §2`:
a Household buys Food and *"its Sustenance moves toward zero"* — a relative scalar that degrades while
unmet and recovers when met. That is a smoothed history of shortage by construction. *Sustained* in
step 7 was describing the Need, not naming a second timer beside it.

| Job | Mechanism | Number it authorises |
|---|---|---|
| accumulate | the **Need** | none — it is a scalar, not a threshold |
| decide | the **comparison** against the Hinterland | none — `02 §5.4`'s `μ` already exists and is owned there |
| perceive | the **cadence** | one, and it is a perception bound rather than a behaviour rule |

### Keeping the failure trigger is safe *because* the Need accumulates

Re-evaluation fires on an Event Wheel countdown **or immediately on a failed shopping occasion**, which
is the pattern [`CONTEXT.md`](../../CONTEXT.md) → *Provider List* already uses: *"a countdown, or
immediately on a failed Trip."*

The objection considered and rejected was that a failure trigger is one step from a failure *count*
being the trigger, which is the duration again. It is not, and the reason is the previous section: the
trigger changes **when the Household looks**, and what it reads is the smoothed Need, which one failed
occasion barely moves. Without the trigger a city can starve for a hundred Ticks with nobody noticing
until their countdown happens to fire. **The two decisions hold each other up**, and neither is safe
alone.

### Reachability is the Destitute channel's, so the Housed channel has two producers and not three

Three mechanisms appeared to feed the housed channel, having arrived separately over four days:
`§6`'s Need degradation, `adr/0095`'s **unsavoury** rung, and
[`adr/0097`](0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md)'s
reach-failure count on the Citizen. The third is not one. [`CONTEXT.md`](../../CONTEXT.md) →
*Unemployment* already routes it elsewhere, in its own words:

> **Destitution is a reachability failure wearing a money costume.** The Household has no money
> *because* it cannot reach work.

So `adr/0097`'s counter is evidence for the **Destitute** channel's diagnosis, and the Commute Budget
turns out to be one ladder feeding two channels by which side of the ceiling a Citizen lands on:
fifty minutes is a bad job taken — *fix what you have* — and past the ceiling is no job at all — *this
is what Policy is for*. Different remedies, so the channel split is doing exactly the work
`CONTEXT.md` → *Departure* claims for it.

## Consequences

- **`04 §6` step 7 is rewritten rather than annotated.** It stops being a mechanism of its own and
  becomes the choice model firing. The words *sustained degradation* are removed, because they read as
  a duration and the only duration in the chain is the perception cadence.
- **No number is created, and [`plans/0002`](../../plans/0002-open-questions.md) §D gains no row.**
  A threshold would have been a hash-bearing quantity requiring a named ratifier under
  [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md); a
  comparison requires none, because the coefficients are the choice model's and are owned by `02 §5.4`.
  ***A mechanism chosen correctly can retire a number rather than choose one***, which is
  [`adr/0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)'s shape a fifth
  time.
- **The Departure *reason* is a decomposition of the utility, never a fourth channel.** `02 §5.4`
  already supplies the readout — *"hard constraints are filters, soft trade-offs are utility"*, with
  the worked example *"37 Households left because no job was reachable"*. Channels name **remedies**;
  reasons name **terms**. A new pressure adds a term and never a channel.
- **`adr/0097`'s counter has a named consumer for the first time.** It said *"read by nothing yet"*;
  it is read by the Destitute channel's diagnosis, still in milestone 9a.
- **Milestone 9a inherits a specification rather than a design question.** It builds: a per-Household
  re-evaluation countdown on the Event Wheel, a failed-occasion trigger, and one call into `02 §5.4`'s
  scoring with the Hinterland as a row. Nothing here is buildable before the choice model, and that is
  correct under [`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) — the answer to
  *given the choice model does not exist, should Departure compensate?* is **build the choice model**.
- **The cadence is the one number this creates and it is a perception bound.** It belongs beside
  `[placement]`'s and `[jobs]`' `revisit_ticks` and is authored the same way, so it inherits their
  precedent rather than opening an argument.

## What would trigger revisiting

- **A measurement showing Departure timing dominated by cadence phase rather than by conditions.** That
  is the failure this ADR is written against, it is *measurable* under
  [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md), and the
  refuting number is the spread of departure times across Households whose conditions turned bad on the
  same Tick. A spread approaching the cadence means the cadence is deciding.
- **The choice model shipping without a stay-put alternative.** `02 §5.4` says the logit form *requires*
  one and names the Hinterland as it. A build that omits it leaves this ADR with nothing to compare
  against, and the threshold becomes the only available mechanism.
- **A Need ceasing to be an accumulator.** If Needs are ever made instantaneous readings, the smoothing
  this rests on is gone and both the failure trigger and the no-duration claim have to be re-argued
  together.
- **A pressure that is neither a Need nor a standing property of the address.** The taxonomy in
  [`adr/0103`](0103-a-need-is-where-a-frequent-private-failure-accumulates.md) is what makes *one
  decision over several pressures* expressible; a pressure fitting neither box reopens this.
</content>
</invoke>
