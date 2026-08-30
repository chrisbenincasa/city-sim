# 0046 — Life Stages and a self-generating population

**`plans/0045` queue item 7, and its real name is `06` milestone 20.**

## Status

✅ **ALL FIVE STAGES BUILT — 2026-08-30.** ***The city generates its own population.*** `aged.toml`
at 2,000 Citizens: 720 Households seeded, all dead by Day 212, **234 standing at Day 400 and every
one born here**. Replacement Rate **1.45 against 2.00**, the authored band's own mean. The
working-age gate costs that world **15% of its labour force** — 1,411 of 1,411 employed before it,
1,200 of 1,411 after. **The estate goes to the treasury**; `World.Dissolve` carries the candidates it
rejected.

⚠ **The queue calls this *"Ageing, birth, death — write `Citizens.Age`"* and that title is wrong in one
word.** Nothing here makes a Citizen age. `CONTEXT.md`, [`adr/0010`](../docs/adr/0010-one-clock-and-demographics-by-sorting.md)
and [`adr/0011`](../docs/adr/0011-household-life-stages-and-self-generating-population.md) all say a
Citizen's age is **static, drawn on formation** — and the mechanism that carries a life is the
**Household's Life Stage**. The queue item is satisfied literally all the same: `Citizens.Age` is
declared, saved, hashed and **written by nothing**, and it gets its writer here.

---

## The finding that unblocks it

🔴 **Milestone 20 and milestone 18 were each waiting for the other, and neither knew.**

[`adr/0011`](../docs/adr/0011-household-life-stages-and-self-generating-population.md) specifies stage
countdowns *"as an ordinary event on the Event Wheel"*. `EventWheel.Arm` (`EventWheel.cs:164`) **throws**
on any delay of `WHEEL_SIZE` or more, and `WHEEL_SIZE` is 2048 Ticks, which is exactly one Day — so
***every Life Stage transition the ADR specifies is unrepresentable on the wheel it was specified to
run on***. That is session C's check, written into the ADR.

And the reason the coarse wheel was never built is in the throw's own message
(`EventWheel.cs:172`): *"A longer sleep is the coarse wheel's, and it has no consumer **until Life
Stages**."*

***So the wheel was deferred for want of Life Stages, and Life Stages were gated on the wheel.*** Each
mechanism was cited as the other's reason not to exist. Under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) both are *unbuilt*
rather than *refused*, and the answer to an unbuilt mechanism is **build X** — which is what makes this
a scoping problem and not a design one. `adr/0011` is **correct and unbuildable**; only the second half
is this document's business.

---

## Decisions taken at scoping

**1. The coarse wheel is built first, and this document does not scope it.**
[`plans/0036`](0036-the-coarse-day-wheel.md) already surveyed and scoped it in full, and
[`adr/0056`](../docs/adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md) already decided its
shape — one bucket per Day, cascading into the fine wheel at each Day boundary, *a radix over one
clock rather than a second clock*. Stage 0 below is **execute `plans/0036`**; nothing about the wheel
is restated here, because a second copy is `plans/0012` **Cause 1** by construction.

⚠ **The alternative was considered and rejected on the day.** A saved `NextStageDay` compared against
today on the Day boundary is what `WageEngine`, the market reprice and the water graph already do,
and ~195 rows a Tick amortised at 400,000 Households is not a milestone's cost. ***It was rejected
because Life Stages are the third Day countdown the design names, not the first*** — Need decay and
the housed re-evaluation wait behind the same throw — and because a sweep would leave
`EventWheel.Arm` throwing with a message naming a consumer that now exists. **A repair that leaves
the error message lying is not a repair.**

**2. A Household's whole life is on the order of 160 Days, and
[`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)
governs the number.**

🔴 ***The corpus states two incompatible lifespans and this is the first document to put them side by
side.*** `adr/0011`'s **Cost** section reasons from *"a life spanning on the order of a thousand
Days"*. `adr/0094`'s revisit trigger says the Day is **too slow** if *"a twenty-hour campaign still
does not contain enough generations for Replacement Rate to be readable, which at 562 Days means a
Life Stage life longer than about 190 Days."*

A thousand-Day life puts **less than one generation** inside a whole campaign, so **Replacement Rate —
the readout the entire mechanism exists to produce — could never be read by a player**. `adr/0094`
therefore wins, because it is reasoning from what a person can see and `adr/0011` is reasoning from
what a machine can afford. ⚠ **`adr/0011`'s cost section is consequently wrong by about 5×** — ~5,000
stage decisions a Day rather than ~1,000 — which is still around half of one percent of the decision
volume already committed to, so **the conclusion survives and only the arithmetic moves.** Filed for
`plans/0012`.

**3. `Citizens.Age` is written once, at formation, and never advances.**
`CONTEXT.md` → *Life Stage*, `adr/0010` and `adr/0011` all say so in the same words, and nothing in the
build reads age at all today. ⚠ **What would justify revisiting is named here so nobody has to guess**:
*a mechanism that must treat two adults in one Household differently by age*. Until one exists, a
per-Citizen counter would be a hashed column with no reader, which is the disease this queue item was
opened to cure rather than a second case of it.

---

## What the build already holds — surveyed 2026-08-27

⚠ **STRUCK 2026-08-30.** It listed nine symbols and the state each was in before any of this was
built; all nine have moved, and a table of where things *were* can only drift. ***Read the code.***

---

## The order the work has to happen in

**The ordering is the safety property and it is not negotiable.** A source without a sink is
[`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md) exactly; a sink without a
source merely empties, and an emptying city is bounded below by zero. ***So dissolution ships before
generation, and the city is allowed to die before it is allowed to breed.***

| | Stage | What lands | Population |
|---|---|---|---|
| **0** | The coarse Day wheel | [`plans/0036`](0036-the-coarse-day-wheel.md), unchanged | — |
| **1** | Stages advance | The five-stage table, scheduled transitions only. `LifeStage` gets its writer; `Citizens.Age` gets its draw | unchanged |
| **2** | Dissolution | Childless and Empty Nest dissolve. **The estate question** below | **falls** |
| **3** | Generation | ⚠ **TWO transitions and this row said one.** Young's exit is the *fertility decision* — a child count drawn, zero routing to Childless — and Mature Family's exit sends those children out as Young Households. Without the first, Replacement Rate is a restatement of the Ruleset and `childless` stays unreachable | **can rise** |
| **4** | The working-age gate | `World.IsOfWorkingAge`, read by both passes. ⚠ **Its guard on `DeclaresLifeStages` is the load-bearing half**: `Citizens.Age` is zero wherever no stages are declared, so a gate reading the column alone makes twenty Rulesets into cities of children | **falls 15%** |

⚠ **Stage 2 ends with an EMPTY city, and that is correct rather than unfinished** — a sink whose
source is stage 3.

✅ **Separating stage 4 from stage 3 paid**: the gate's cost was measurable *because* nothing else
moved employment that day.

🔴 **`[[building]] jobs = 8` IS NOT RE-DERIVABLE, and that is the answer rather than a deferral.**
`--stages`' *Is there work for the people who can work?* panel measures the ratio it sets:
**0.96 at its lowest — the derivation exactly — 12.51 at its highest, mean 2.86.** It is the **product of
two factors** and ⚠ **the larger is not about children.** Nothing on either demographic world
condemns, so the dwelling stock is **monotone — 0 Days of 400 saw it fall** — while the population
oscillates. ⚠ **That is not `adr/0006` violated**, which is why no long run ever caught it: the stock
is bounded by peak demand and converges, so every collection check passes. ***The unbounded thing is
the ratio, whose denominator is free to fall.*** So `jobs` is downstream of a stock with no sink, and
re-deriving it first would be
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)'s
local workaround. `StageDumpTests` pins all three readings.

---

## The traps, in the order they will be met

**1. ✅ ANSWERED 2026-08-28 — the estate goes to the treasury.** `DestroyHousehold` frees every Bin a
Household owns and the `MoneySupply.Issued` write lives in `Depart`, because **emigration exports the
money and a death cannot**. `World.Dissolve` carries the two rejected candidates and the reasoning;
`Invariant.MoneyIsConserved` is the exact equality that holds it.

**2. ✅ MET AND FIXED 2026-08-28 — one pass, not two.** `EmploymentEngine` sized its sample from
`LiveCount` while drawing over `SlotCount`; it now sizes from `SlotCount`. ⚠ **`PlacementEngine.Found`
never had the defect** — this document said "the same shape" and was wrong, and the fix was to copy
the pass that was already right. ⚠ **And the table was not going sparse for the first time**: a
give-up departure has destroyed Households since milestone 11, so `crowded.toml` was already sparse.

**3. ⏸ DEFERRED at stage 2 — the fix is a SAVE-FORMAT change, not a line edit.** `SaveHeader` carries
no founding Citizen count, so `Session.cs:313` rebuilds
`WorldConfiguration(world.Citizens.Rows.LiveCount)` into a field `WorldConfiguration.cs:16` calls
*"a capacity and not a population"*. Once births move the live count off the constructed capacity, a
resumed run sizes every derived table differently. `SyntheticCity.cs:283` and `:628` read
`Rows.Capacity` **as** the population.

**4. ✅ NOT A TRAP — checked 2026-08-28.** `CreateCitizen` leaves `LastPaidDay` zero, but `World.Employ`
starts the pay clock on the day of hire and is the one door onto the Workplace handle, so no Citizen
can reach a payday carrying it. `Age` and `SkillTier` still arrive zero and still have no writer.

**5. ✅ HELD — no calendar was invented.** Every duration is in Days, including the adult age band,
whose `1..160` is a lifetime in this world rather than a human age.

**6. ✅ HELD — no member-count column was added.** Composition is the member list's length, and
`World.SpawnChildren` walks it looking for age zero rather than reading a stored pair.

---

## The numbers

🔴 **All PROVISIONAL, chosen by taste, no ratifier and no `plans/0002` §D row** — which is what
[`plans/0045`](0045-amnesty.md) suspends
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
in order to allow. They are numbers that let the mechanism run, and nothing more.

A stage's countdown is a **floor**: `adr/0011` specifies the wake as a uniform draw over `[N, N+W)`
Days, with `W` authored per `[[life_stage]]`, *"so a founding generation does not transition on one
Tick"*. ⚠ **`W` is the load-bearing half.** Without it every Household created at Tick 0 transitions
together for ever, and the city breathes in lockstep — a demographic echo that would read as a
mechanism rather than as an artefact of world creation.

| Stage | `N` | `W` |
|---|---|---|
| Young | 24 | 8 |
| Family | 48 | 16 |
| Mature Family | 48 | 16 |
| Childless / Empty Nest | 40 | 16 |

≈**160 Days** to a full life at the floor, inside `adr/0094`'s ~190 and giving roughly **three
generations** to a 562-Day campaign.

**The Ruleset grows `[[life_stage]]`**, and a world declaring none has no demographics at all — the
nine shipped files stay exactly as they are, on `[[hinterland]]`'s precedent. The demonstration world
is a new file rather than an edit to a standing one.

---

## Definition of done

`plans/0045`'s, and it is the hard half: ***you watched it happen and something surprised you.***

🔴 **ANSWERED 2026-08-30, AND THE ANSWER IS NO.** The question was whether the founding
generation's echo ever damps. Over 400 Days and three generations on `aged.toml` it does not:
**busiest ÷ mean 7.0×, and 116 of 400 Days see no transition at all.** The population *oscillates*
between **156 and 452** Households with a period near the chain's own length, and ***individual
stages go completely empty and refill*** — `mature_family` holds nobody at Day 160 and 155 at Day
242. A test asserting every stage occupied at one instant failed on exactly this.

⚠ **So `W` is too narrow**: four windows of 8–16 Days do not smear a cohort across a 160-Day life.
Widening it is an edit to `aged.toml` alone, judged by `--stages`.

**Replacement Rate is delivered**: children per fertility *decision*, zero draws included, against
the 2.00 that falls out of conservation. ⚠ **The denominator is the trap** — dividing by the
Households that bore *at least one* child reports the fertility of the fertile, and the stagnation
spiral `adr/0011` describes would read as a healthy city.

## What this does not do

**No Needs, no schools, no Taste, no dwelling-size preference.** `adr/0011` lists six preference axes
that hang off the stage table and calls it *"the most load-bearing data in the design"*; every one of
them is a consumer of Life Stage rather than part of it, and
[`adr/0027`](../docs/adr/0027-preference-is-drawn-per-household-and-persists-for-life.md) owns the
drawing. **This milestone builds the clock and the table, and the readers arrive after.**

**No housed departure.** `World.Depart` refuses a housed Household and that channel is milestone 16.
`World.Dissolve` is a **third** route into `DestroyHousehold` and its remark says why it is not that.

⚠ **And it does not make immigration redundant.**
[`adr/0023`](../docs/adr/0023-immigration-arrives-through-the-gate.md) makes Hinterlands finite
stocks, so ***internal generation is the only growth channel that survives the late game*** — the two
are complementary.
