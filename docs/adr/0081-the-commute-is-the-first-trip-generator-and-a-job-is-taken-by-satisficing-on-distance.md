# The commute is the first Trip generator, and a job is taken by satisficing on distance

**A new milestone — `06` **5b-bis**, between 5b and 5c — builds the first Trip generator, and it is the
**commute**. A `[[building]]` kind declares **job slots**; a Citizen with none takes a job by
[`0017`](0017-agents-satisfice-they-never-optimise.md)'s rule — **the first acceptable one among sampled
candidates**, acceptable meaning *within the Commute Budget on foot* — and that assignment writes
`CitizenTable.Workplace`, which today is a fixture stride. A Citizen with a Workplace then generates a
commute Trip on a daily occasion. **No wage is read, and that is a stated debt rather than an oversight**:
[`0026`](0026-wages-are-posted-locally-and-never-cleared.md) makes a job choice a function of wage *and*
commute, so this builds the second term only, and the milestone therefore discharges the **assignment**
half of `06`'s *"Office, wages, the labour market, Skill Tiers, schooling"* row and **not** the wage half.
**The precondition all three candidates shared is a spatial index from a place to nearby Buildings, which
does not exist**, and it is the milestone's first task rather than the generator's implementation
detail.**

Settled by the sitting on [`plans/0002`](../../plans/0002-open-questions.md) §A, together with
[`0080`](0080-phase-4-does-not-wait-on-a-trip-generator-and-a-trip-is-entered-by-command.md), which
establishes that a generator is a milestone rather than a task and that milestone 5b does not wait on it.

`EMERGENCE` `BOUNDED KNOWLEDGE` `UNIQUE INDIVIDUALS` `SOLVE THE ACTUAL PROBLEM`

---

## Why

### The commute over the other two, and the reasons are not that it is easiest

Three candidates survived the sitting. Each needs the spatial index; they differ in what else they drag
in.

**Shopping** is what `plans/0021` recommended, on the true ground that
[`0067`](0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md) specifies
it down to its fields. Its preconditions are three mechanisms and two of them are ADR-sized: the
**Provider List with its acquisition cost** ([`0066`](0066-the-provider-list-is-an-intrusive-index-list-and-its-ruleset-length-is-a-cap-rather-than-an-allocation.md),
unbuilt), the **`Scope.Pool` market** ([`0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md),
which throws today), and an **occasion scheduler**. The refusal site is worth quoting because it is the
strongest argument against going first through the economy: *"implementing this as a Bin lookup ships an
unconserved economy, and **no refusal can catch that**."* A generator whose first precondition is a
mechanism whose failure mode is invisible is the wrong place to start.

**School** is cheapest — [`0032`](0032-services-are-delivered-by-trips-not-by-coverage.md) owns it, it
needs no money, no market, no Provider List and no Bin, and `HouseholdTable.LifeStage` already exists.
It is refused on a sequencing point that is easy to miss: `0032` sizes school Trips as ***"roughly +50%
on the commute peak"***. **It is specified as a fraction of the commute**, so building it first means
producing the derived quantity before the base one, and the derived quantity's own sizing fact becomes
uncheckable. A generator that can only be validated against a thing that does not exist is not the first
generator.

**The commute** is chosen because it is the Trip the rest of the corpus is already written against.
`CONTEXT.md` → Commute Budget is about it by name; `0032`'s sizing is relative to it; `03`'s peak
structure is built on it; and — the practical reason — **it is the only candidate that exercises the
Commute Budget**, whose named ratifier is *the first run long enough to produce a Trip cost
distribution*. Choosing shopping or school would have left `0002` §D2's largest unset number waiting for
a fourth thing to be built.

### Satisficing on distance is not a gravity model, and the difference is where the constraint lives

The obvious objection to assignment without a wage is that every Citizen then chooses by distance alone,
and the commute distribution becomes a distance-decay curve — which is a **gravity model**, the aggregate
device `00-vision` pillar 1 and [`0005`](0005-two-fidelity-tiers.md) exist to refuse.

It is not one, and the distinction is structural rather than presentational. A gravity model asserts
flows *between zones* proportional to mass over distance, with no individual in it. What this builds is
[`0017`](0017-agents-satisfice-they-never-optimise.md) applied per Citizen: each looks at a **sampled**
set of candidates — the same bounded-knowledge shape
[`0069`](0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md) already ships for
placement — and takes **the first acceptable one**, not the nearest and not the best.

**The distribution that emerges is produced by supply, not by a decay function.** Job slots are finite
and spatially clustered wherever the Ruleset's zoning put that kind, so nearby slots fill and later
Citizens travel further. The tail is scarcity. That is a mechanism producing a distribution, which is
exactly what `0080` requires of the thing tasks 6 and 8 will be measured against, and it is why a
sampler was refused and this is not one.

### What no wage actually costs, stated so it is not discovered later

`0026` posts a wage locally by fill rate, and a worker weighs wage against commute. Building the commute
term alone has one specific consequence: **nothing makes a Citizen travel past a nearer acceptable job.**
Real commute distributions have a wage-driven tail on top of the scarcity-driven one, and this model
produces only the second.

The direction of the error is therefore known and should be written down rather than discovered: **this
model's commutes are too short**, and every quantity derived from them — the Commute Budget percentile,
peak pedestrian density, the walk-search multiplicand in [`0013`](../../plans/0013-tick-budget.md) — is
biased **low**. A number biased in a known direction is usable; the same number with an unknown bias is
not, which is the whole content of `adr/0043`'s insistence that a measurement name what would refute it.

This is the reverse of the trap [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)
describes and it is worth being explicit that it is not that trap. `0070` forbids reasoning *from* an
absence to a compensating design position — it does not forbid **building half a mechanism when the half
is separable and the seam is named**. Assignment and pricing are separable here because the Trip needs a
*destination* and not a *salary*.

### The precondition nobody had named

All three candidates need the same thing and no document lists it: **a query from a place to the
Buildings near it.** `CellResidency` indexes Map Layer cells, not entities; `Frontage` maps a Lot to its
Segment in one direction only; there is no Cell → Buildings and no Segment → Lots reverse index anywhere
in `Borough.Core`.

It is the milestone's **first task**, ahead of the generator, and it is stated as a task rather than left
inside one because it is the piece that is reused: job assignment needs it, `0067`'s Provider List
acquisition will need it, `0032`'s Attended services will need it, and `02 §5.3`'s candidate sampling
already assumes it. Building it inside the generator is how it would acquire the generator's shape and
then be rewritten twice.

## Consequences

- **`06` gains a milestone, 5b-bis, between 5b and 5c.** Its named risk, per `06` rule 2: *that every
  number milestone 5b was to produce is taken against a fabricated origin-destination draw and lands in
  `0013` and `0002` as measured fact* — S2 R4's finding turned on the corpus's own instruments.
- **It carries 5b's tasks 4, 6 and 8**, transferred by `0080`: the generator, the Trip Census family
  with the Commute Budget as Ruleset data, and the 100,000-Tick run.
- **The `[trips]` Ruleset table is created here**, and it is where the **Commute Budget** and the
  **crossing cost** finally land. Both are hash-bearing and unset (`adr/0052`), and **5b-bis may choose
  neither by argument**: the Budget's ratifier is this milestone's own first long run, so it is set from
  a measured percentile or it is not set.
- **`CitizenTable.Workplace` stops being a fixture artifact.** It is written today by `SyntheticCity` as
  `(i * 7) % buildings`, read by nothing. Replacing a stride with an assignment pass **moves the State
  Hash**, so the golden baselines re-record, and the populator's stride is deleted rather than left as a
  fallback — two ways to acquire a workplace is the drifted-copy failure with both copies live.
- **A Building kind gains `jobs`**, a fourth `[[building]]` key beside `name`, `bins`, `condemn_after`
  and `occupants`. It is **tuning, hot-reloadable and hash-bearing**, and it is subject to `0068`'s rule
  for occupancy: derived from the Ruleset in force rather than frozen at construction, so lowering it
  **evicts** — a job has a holder and no consumer, exactly as occupancy does.
- **A second `[[building]]` kind exists in a shipped Ruleset for the first time.** `dwelling` has been
  the only kind in all three files since the Ruleset existed, so every loader and Zone Rule path that has
  only ever seen one kind gets its first real exercise, and `adr/0055`'s permission-set scoping gets its
  first case with something to distinguish.
- **The daily occasion is the milestone's one open design question**, and it is small: a commute is
  scheduled and periodic, so the sampled-sweep shape the Zone Rule and placement already use is
  available, and generalising the Event Wheel to a second table is **not** required. It should be argued
  in the milestone rather than assumed, because `0059` is the standing warning about deriving a sample
  from a duration versus choosing one.
- **Nothing here builds a wage, a Business, a vacancy posting or a labour market**, and `06`'s
  no-milestone row keeps its entry with the assignment half struck. A row half-discharged and marked is
  the form; a row deleted because part of it shipped is `plans/0012` *Cause 3*.

## What would trigger revisiting

- **The measured commute distribution being implausible in a way scarcity does not explain.** The stated
  bias is *too short*. If the first long run produces commutes that are too **long**, or bimodal, the
  cause is not the missing wage and the assignment mechanism itself is wrong.
- **`0026` arriving.** When wages are posted, job choice acquires its first term and this ADR's central
  simplification is spent. The question then is whether the assignment pass extends or is replaced —
  and the answer should be checked against whether the *distribution* moved, because a wage term that
  does not move it is a term nobody needed.
- **The spatial index proving to be the expensive part.** It is priced nowhere. If the query that finds
  candidate Buildings costs more than the walk searches it feeds, the generator's shape is wrong before
  its rate is, and `0013` gains a row nobody predicted — which is `0069`'s pattern, where a mechanism
  needed three hash-bearing numbers its ADR predicted none of.
- **A Citizen needing more than one destination.** This gives each Citizen exactly one Workplace and one
  Trip a Day. `0067`'s shopping, `0032`'s school and any second occasion make a Citizen's day a
  *schedule* rather than a repeated Trip, and that is the point at which the daily occasion stops being
  a sweep and becomes the thing the Event Wheel was generalised for.
