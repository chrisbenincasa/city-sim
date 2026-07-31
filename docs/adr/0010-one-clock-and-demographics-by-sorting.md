# One clock, and demographic change by sorting rather than aging

> **Extended and partly corrected by [`0011-household-life-stages-and-self-generating-population.md`](0011-household-life-stages-and-self-generating-population.md).** One clock stands. No per-Citizen aging stands. What was wrong is the claim below that Household composition must therefore be frozen, and the implication in "what would trigger revisiting" that life stages would compromise the one-clock decision — they do not, because a per-Household countdown in Days is an event on the existing wheel rather than a second time base. Sorting is now one of two demographic channels rather than the only one.

> **Quantified by [`0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md).** One clock stands, and "a few real minutes" is now pinned: a Day is 8192 Ticks, 8m32s at the default speed. Two clarifications that ADR adds, both of which are the arguments someone will make against one clock later:
> - **Tiered dispatch is not a second clock.** RimWorld's `TickRare`/`TickLong` and Dwarf Fortress's staggered 1/10/50/100/1000 buckets run cheap slow processes off *one* counter. Our Event Wheel already is that. Multiple clocks buy nothing for performance; they would only ever be for pacing.
> - **The speed ladder is the second clock, and a better one.** It delivers fast-growth-slow-traffic sequentially rather than simultaneously, costs nothing architecturally, is player-operated, and never makes two statements in incommensurable units. Only simultaneity is lost, and Evidence reconstructs the chain afterwards.

> **Generalised by [`0023-immigration-arrives-through-the-gate.md`](0023-immigration-arrives-through-the-gate.md).** Sorting stands entirely, but the framing below is too narrow in two places. **Sorting is not a schools mechanism** — it is what the choice model does whenever the Outside is an alternative, and schools are one term among many in a per-Life-Stage coefficient vector. Read "good schools attract already-educated families" as the *worked example*, not the mechanism. And the consequence below that *"immigration needs a composition model"* is answered: there is no composition model and no arrival scalar. Prospective Households evaluate four **Hinterlands** using the same utility function residents use, arrive as Trips through an Outside Connection, and the mix that results is emergent. The city's demographic profile is a readout of what it happens to be good at.
>
> One further correction, from [`0026`](0026-wages-are-posted-locally-and-never-cleared.md): the loop *"build schools → educated citizens → skilled workforce"* no longer closes by sorting **alone**. Experience on the job carries a Citizen from tier 1 to tier 2; only schooling reaches tier 3. So sorting is the sole channel for the *top* tier and shares the middle one with employment.

**The simulation has a single time scale.** A day takes a few real minutes at 1×; growth and decline are measured in days and weeks, not years; there is no calendar. **Citizens do not age.** Age is a static attribute drawn when a Citizen arrives, used to select role and routine. Demographic change happens through **who arrives and who leaves**, not through individuals transforming.

## Why

### One clock

Movement and growth want opposite time scales. Watching a rush hour build, a jam clear, or a walk get longer needs a day to last minutes. Cities growing and neighbourhoods declining wants years to pass in seconds.

The standard resolution is **two clocks** — a fast daily cycle for movement and a slow calendar for growth — and SimCity 4 effectively took it. **We reject it on Pillar 1 grounds.** With two clocks, "this shop closed because its customers' commutes got too long" stops being literally true: the commute happened on one clock and the closure on another, and the connection between them becomes a conversion factor. A conversion factor between two time bases is exactly the sort of unreasonable-about magic number this design exists to eliminate. `LEGIBLE CAUSE`

So: one clock, and growth is expressed in the same units the player watches traffic in. A Building declines after a run of bad *days*, and the player can sit and watch the bad days happen.

### Why aging then becomes impossible, and why that is fine

One clock forecloses aging arithmetically. At a few minutes per day, a Citizen reaching eighty takes on the order of a thousand real hours. Aging would need its own clock, which reintroduces the split through a side door.

The apparent cost is the whole generational layer: school demand, neighbourhood turnover, family flight, and the education-to-workforce loop. **Nearly all of it is recoverable through sorting instead of maturation** — the city's composition changes because different kinds of Household choose to arrive and leave, which runs on the discrete-choice machinery already specified rather than on a new subsystem.

| Generational effect | How sorting produces it |
|---|---|
| Rising school demand | Households with children choose to arrive |
| Neighbourhood turnover | The mix of Households choosing a District shifts |
| Family flight | Households with children depart — a distinct, diagnosable signal |
| Aging suburbs | Arrivals skew older while younger Households leave |

The loop that seemed to genuinely require maturation is *build schools → educated citizens → skilled workforce → high-value industry*. It closes under sorting as well, by a different and arguably better mechanism: **good schools attract already-educated families**, who bring their education with them, raising the skill supply and enabling higher-value Businesses. Residential sorting on school quality is among the strongest observed effects in real urban economics, and most people do not work where they went to school. The sorted version is not a substitute for the matured version — it is closer to the truth.

## What is actually lost

**Narrative, not mechanics.** No player will ever watch a Pinned family's children grow up. This is a real loss and should not be waved away, but it sits comfortably beside the standing anti-goal against Dwarf Fortress-style life histories — that is the direction where decades of development disappear.

Also lost: death as a demographic force, and population pyramids as a thing the player manages. Departure is the sole population sink, which [`0006`](0006-no-collection-grows-with-elapsed-time.md) already requires to exist and be effective regardless.

## Consequences

- **A Household needs composition** — a count of adults and a count of children. This is what generates school demand, workforce supply, and the difference between a family and a couple. Two small integers on an existing record.
- **Immigration needs a composition model.** What kinds of Household arrive is now a first-class question rather than a detail, because it is the *only* channel through which the city's demographics can change. It should respond to what the city offers: schools attract families, jobs attract working-age Households.
- **Departure must be reported by Household type.** "Families with children are leaving" and "childless Households are leaving" are completely different diagnoses, and the existing unhoused/housed split is orthogonal to it rather than a replacement.
- **No calendar means no seasons, no annual budget cycle, no anniversaries.** Anything the player might expect to be annual has to be expressed in days or not exist.
- **Education is an attribute of arriving Citizens**, conditioned on the city's school provision at the time they choose it — not a state that develops in place.

## What would trigger revisiting

If playtesting shows the city feels static in a way sorting cannot fix — the specific symptom being that players describe the population as scenery rather than as changing. The cheapest recovery is **life stages without a calendar**: a Citizen holds a stage rather than a number, and stages advance on a slow per-Citizen countdown. That buys the maturation loop without a second global clock. It is a real option and it is deliberately not being taken now.

---

## Superseding note — session five: Sorting finally has a mechanism, and education develops in place after all

Two consequences above are now wrong, and both were wrong for the same reason: this ADR was written before schools were reachable places.

**"Education is an attribute of arriving Citizens... not a state that develops in place" — struck.** [`0032`](0032-services-are-delivered-by-trips-not-by-coverage.md) makes a school an **Attended** Service, so a child's schooling is a quantity **accumulated per Day the school Trip completes**, and it sets the Skill Tier of the **new Young Household** formed when children leave home under [`0011`](0011-household-life-stages-and-self-generating-population.md). A school does not educate an adult; it sets the tier of a Household that does not exist yet.

This resolves the contradiction between this ADR and [`0026`](0026-wages-are-posted-locally-and-never-cleared.md), which stated *"only schooling reaches 3"* while the third superseding note above stated Sorting is not a schools mechanism. **Both are true and they compose**: schools *produce* the city's own tier-3 workers on a Life Stage lag, and schools *attract* already-educated Households immediately. The city has two education channels, at two completely different speeds.

**Sorting stops being an assertion.** It was always *"good schools attract already-educated Households"* with nothing underneath it. Under `0032` a school is a Provider List entry scored by the same logit that scores a job, so school quality enters residential utility with no mechanism written for it — and so does the corollary nobody wanted to author: **good schools raise land value, which prices out the Families the school was built for.**

**The school tiers map onto the Life Stages already in `0011`.** Primary serves Family, secondary serves Mature Family, and university attaches to the new Young Household as a **state, not a stage** — an In Education Household is 1–2 adults with no children, which is a Young Household exactly, so it fails this design's own definition of a stage. **Life Stage is composition; what a Household is *doing* is a separate field.**

**Primary is a gate, not a producer.** Tier 1 is the floor, so a primary school that merely conferred tier 1 would do nothing. It earns its place because secondary cannot accumulate without it — and a city that skips primary schools caps at tier 1, reaches tier 2 only through experience, and **never produces a single tier-3 worker**.

**What is lost above is partially recovered.** *"No player will ever watch a Pinned family's children grow up"* is now half false: a player can watch a Pinned Family's children accumulate schooling and can see which tier they will enter the workforce at. That is a maturation loop, and it arrived without a calendar — exactly the cheap recovery this ADR named under *what would trigger revisiting*.
