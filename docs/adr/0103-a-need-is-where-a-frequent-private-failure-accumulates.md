# A Need is where a frequent private failure accumulates, and everything standing is a utility term

**A Need exists for each *exhaustible* thing a Household consumes and can be refused on arrival:
Sustenance, Satisfaction, Education, Health. Every other pressure — the commute, the rent, the
neighbourhood, whether a Service can reach you at all — is a term in the choice utility and never a
Need. The accumulator sits wherever the stream is dense enough to be a signal: frequent and private
puts it on the **Household**, rare and shared puts it on the **place**.**

`LEGIBLE CAUSE` `BOUNDED KNOWLEDGE`

Settled with the user in the room on 2026-08-15, grilling `04 §6`, and it is the sibling of
[`adr/0102`](0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md):
that one says a Household leaves by comparison, this one says what may enter the comparison as a Need.

## Why

### A utility function is already the thing that makes unlike quantities comparable

`04 §6` step 7 made a Departure follow from a degraded Need, so *one decision over several pressures*
looked as though it needed every pressure expressed as a Need — a Comfort dimension for the commute, an
Amenity dimension for the neighbourhood. That was drafted and refused, on the corpus's own warning in
[`CONTEXT.md`](../../CONTEXT.md) → *Terms we deliberately do not use*:

> **Any named tunable that is really an outcome of the choice model should be suspected of the same
> error.**

Making the commute a Need adds a smoothing scalar in front of a quantity `02 §5.4` already scores, and a
Hinterland is already authored with **a commute figure** for exactly that comparison. The Need would
have been a second copy of a term, with its own coefficient, capable of disagreeing with the first.
***A quantity that the choice model already weighs does not need a second currency to be weighed in.***

### Volatility looked like the test and it is not — the police case breaks it

The first rule proposed was *volatile pressures become Needs, standing ones become utility terms*: a
shelf empties and refills, so it needs smoothing; a rent does not. It survives one counter-example and
dies on the second, which the user supplied — **an unanswered police call is volatile and is nothing
like an empty shelf.**

The difference is not how often the world changes. It is that **one Household does not have enough
incidents to form an opinion.** A shopping occasion is frequent and private, so a Household's own
stream is dense enough to average into a scalar that means something. A call-out is rare, and its
consequence is *shared* — police failing to reach this neighbourhood is true for everybody in it.
Smoothing that per-Household produces noise wearing a Need's clothes, and the balance surface would be
dominated by the draw rather than by the city.

So the test is about **where the signal is**, not what kind of thing the pressure is:

| Stream | Accumulator | Becomes |
|---|---|---|
| frequent, private | the **Household** | a **Need** |
| rare, shared | the **place** | a standing **utility term** |

### `adr/0032` had already partitioned Services on the axis that decides it

[`adr/0032`](0032-services-are-delivered-by-trips-not-by-coverage.md) sorts Services by **who moves**,
and that sort turns out to answer this question without amendment:

| Mode | Who moves | Where the failure lands |
|---|---|---|
| **Attended** — education, health, recreation | the Household | a visit that fails, like a shelf |
| **Dispatched** — fire, police | the Service | an incident nobody answered — **shared**, so it lands on the place |
| **Networked** — power, water, sewage | nobody | readable as a state at any moment, so it is standing |

And that ADR **demoted the coverage Map Layer from mechanism to overlay, *"composed from the same
reachability the Trips use"*** — so the place-side accumulator the Dispatched services need already
exists, and is derived from real dispatch outcomes rather than from a decayed radius. **There is no
safety Need and there does not need to be one.**

### Attended splits again, and the divider is this week's own vocabulary

Recreation is Attended and is **not** a Need. `adr/0032` says so outright: *"recreation needs no new
machinery at all — a park is an Amenity entry."* The reason is that **a park cannot run out**. A shelf
can be empty; a school can be full; a clinic can be full; a park refuses nobody who can reach it.

That sorts on the bounds vocabulary settled two days earlier in
[`adr/0097`](0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md)'s
amendment and written into `CONTEXT.md` → *Supply and Space*:

- refuses on **Supply** or **Space** — it is there and cannot serve you now → a failed visit, per
  occasion, accumulating → a **Need**
- refuses on **reach** only → a standing property of your address → a **utility term** (Amenity, the
  commute)

The `Blocking` rename was done to stop one ADR using *stock* for the opposite bound. That it then sorts
Services is not a coincidence: ***a vocabulary that names the real axis does work in places nobody
renamed***.

### The granularity is per service, and the cost is named rather than hidden

Sustenance, Satisfaction, **Education, Health**. The alternative considered was one *Provision* Need
covering all Attended services, which is cheaper and was refused on `LEGIBLE CAUSE`: *the city is
failing to educate its children* and *the city is failing to treat its sick* are different diagnoses
with different verbs, and merging them re-commits at the Need level exactly what
`CONTEXT.md` → *Departure* refuses at the outcome level.

**The cost is real.** Education and Health are the first Needs with **no Good, no Bin and no price**
behind them, so `04 §1`'s clean Good → Need pairing stops being total, and Sustenance's degradation
rule — buy Food, move toward zero — does not transfer to a quantity nobody buys.

## Consequences

- **The Need set is four and grows only when a new exhaustible consumable arrives.** Adding a Need is
  now a claim that something can be *refused on arrival*, which is checkable, rather than a claim that
  something matters, which is not.
- **The commute, the rent, Amenity, and reach to any Service are utility terms.** They enter
  `02 §5.4`'s scoring directly. No Comfort Need, no Safety Need, no Mobility Need — and each of those
  was proposed and refused in this sitting.
- **Dispatched services have no per-Household state at all.** Their quality is a property of the place,
  carried by `adr/0032`'s overlay, and every Household at an address reads the same number.
- **A degradation rule for Education and Health is owed and is deliberately undesigned**
  ([`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)). It is *unbuilt and
  unspecified*, not refused, and it belongs to whichever milestone builds Attended services — nothing
  may reason from its absence in the meantime.
- **`04 §1`'s Good → Need diagram is no longer a total map** and must not be read as one. Two Needs have
  Goods; two do not.
- **`adr/0032`'s taxonomy gains a second job.** It was written to decide *who makes the journey*; it now
  also decides *where a failure accumulates*. Any future Service must be placed in it before it can be
  said whether it produces a Need.

## What would trigger revisiting

- **A Need whose stream turns out sparse in a running city.** This is *measurable* under
  [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md): the refuting
  number is failed occasions per Household per Day for each Need. A Need that a Household meets a
  handful of times a Day is the police case wearing Sustenance's clothes, and belongs on the place.
- **A Dispatched service becoming frequent and private.** Nothing in the design does this today, but a
  Service that visits every Household on a cadence would be Attended in everything but name and would
  reopen the sort.
- **Education or Health acquiring a Good.** If either is ever modelled as a consumable moving through
  Bins, its degradation rule follows Sustenance's and the *no Good behind it* cost in this ADR
  disappears — which would also make the four Needs uniform again.
- **The choice model shipping with a term the Need set duplicates.** Two quantities scoring the same
  pressure with independent coefficients is the failure this ADR exists to prevent, and it would be
  visible the first time a Departure's *reason* and its dominant Need disagree.
</content>
