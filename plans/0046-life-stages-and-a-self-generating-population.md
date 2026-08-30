# 0046 — Life Stages and a self-generating population

**`plans/0045` queue item 7, and its real name is `06` milestone 20.**

## Status

🟡 **STAGES 0, 1 AND 2 BUILT — 2026-08-28.** ***The city now empties*** — `aged.toml` at 2,000
Citizens reaches zero by Day 210 — the correct state between a sink and its source. **The estate goes
to the treasury**; `World.Dissolve` carries the candidates it rejected.

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

⚠ **The alternative was considered and rejected on the day.** A Day countdown does not strictly need a
wheel: a saved `NextStageDay` column compared against today on the Day boundary is what `WageEngine`,
the market reprice and the water graph all already do, and the corpus argues for it in
`Simulation.cs` — *"18's wheel exists so that MANY Day countdowns can share a structure."* At ~400,000
Households that scan is ~195 rows a Tick amortised, which is not a milestone's worth of cost.
***It was rejected because Life Stages are the third Day countdown the design names, not the first***
— Need decay and the housed re-evaluation are both waiting behind the same throw — and because
building the sweep would leave `EventWheel.Arm` still throwing with a message naming a consumer that
now exists. **A repair that leaves the error message lying is not a repair.**

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

| Symbol | Where | What it is |
|---|---|---|
| `HouseholdTable.LifeStage` | written at `World.cs:1573` | 🔴 **A byte that is written on creation and advanced by NOTHING.** The third dead column found this week, after `Citizens.Age` and `Citizens.Health` |
| `CitizenTable.Age` | `CitizenTable.cs:94` | `ushort`, Days, `Touch.Cold`, **no writer** |
| `CitizenTable.Health` | `CitizenTable.cs:95` | `byte`, **no writer**, and out of scope here |
| `World.CreateCitizen` | `World.cs:1708` | Sets `HouseholdOf` and `NextEventTick` and **nothing else** — every other column arrives zeroed |
| `World.DestroyCitizen` | `World.cs:2138` | The **only** site that frees a Citizen row. Unrosters, releases parking, unlinks from Household and employer |
| `World.DestroyHousehold` | `World.cs:2177` | Drains the member list through `DestroyCitizen`, ends the tenancy, frees **every** owned Bin |
| `World.Depart(Household)` | `World.cs:1791` | The emigration channel. **Unhoused-only**, and it is where `MoneySupply.Issued` is decremented (`World.cs:1819`) |
| `PlacementEngine.GivesUp` | `PlacementEngine.cs:452` | `gives_up_after_days`. **The one production path that frees a Citizen row today** |
| `EmploymentEngine.Assign` | `EmploymentEngine.cs:203` | Three conditions: live, not already employed, has a reachable home. **No age term, no `SkillTier`, no stage** |

**Nothing kills a Citizen today except giving up on housing**, and only in the two Rulesets that
declare a gate. The Citizen table has therefore been **dense for the life of this project**, and two
passes say so in their own comments.

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
| **3** | Generation | Mature Family's exit spawns Young Households into the Unplaced Pool. Replacement Rate | **can rise** |
| **4** | The working-age gate | Children stop taking jobs and founding businesses | unchanged |

⚠ **Stage 2 ends with an EMPTY city, and that is correct rather than unfinished** — a sink whose
source is stage 3.

⚠ **Stage 4 is separated from stage 3 deliberately.** It is hash-bearing, it moves the labour supply,
and `[[business]] jobs = 8` was derived from *"1000/360 × 3 = 8.33 Citizens"* — a ratio that assumes
**every Citizen works**. Landing it with generation would make two changes to employment on one day
and leave neither attributable.

---

## The traps, in the order they will be met

**1. 🔴 `DestroyHousehold` destroys money silently, and a death has no recipient.**
`World.cs:2224` frees every Bin a Household owns, balance included, and the `MoneySupply.Issued` write
lives in `Depart` rather than here — because *destroying a Household is a table op with several
callers and only one of them means "emigrated"*. **Emigration exports the money
(`adr/0142`); a death cannot.** So stage 2 has to answer an estate question the corpus has never
asked: does a dissolving Household's balance pass to the Households it spawned, return to the
treasury, or leave the money supply? ***Whatever it does, it must decrement `Issued` or
`Invariant.MoneyIsConserved` will fire***, and that invariant is an exact equality.

**2. ✅ MET AND FIXED 2026-08-28 — one pass, not two.** `EmploymentEngine` sized its sample from
`LiveCount` while drawing over `SlotCount`; it now sizes from `SlotCount`. ⚠ **`PlacementEngine.Found`
never had the defect** — this document said "the same shape" and was wrong, and the fix was to copy
the pass that was already right. ⚠ **And the table was not going sparse for the first time**: a
give-up departure has destroyed Households since milestone 11, so `crowded.toml` was already sparse.

**3. ⏸ DEFERRED at stage 2 — the fix is a SAVE-FORMAT change, not a line edit.** The founding Citizen
count is nowhere in `SaveHeader`, so `Session.cs` has no better value to reach for. What it damages is
a written artefact rather than a running world: the trace header, and the `.borough` log a replay
would build a *fresh* city from. Save and resume conflate capacity with population.
`Session.cs:313` rebuilds `WorldConfiguration(world.Citizens.Rows.LiveCount)`, and
`WorldConfiguration.cs:16` documents that field as *"a capacity and not a population"*. Once births
and deaths move the live count away from the constructed capacity, a resumed run sizes every derived
table differently from the run it resumed. `SyntheticCity` reads `Rows.Capacity` **as** the population
in two places (`SyntheticCity.cs:283`, `:628`), correct only while the table has never grown.

**4. ✅ NOT A TRAP — checked 2026-08-28.** `CreateCitizen` leaves `LastPaidDay` zero, but `World.Employ`
starts the pay clock on the day of hire and is the one door onto the Workplace handle, so no Citizen
can reach a payday carrying it. `Age` and `SkillTier` still arrive zero and still have no writer.

**5. There is no calendar and there must not be one.**
`CONTEXT.md`'s banned vocabulary: *"'Year' / 'month' / 'season' — there is no calendar. Say Day."*
Every stage duration is in Days. The precedent for anything that wants a longer unit is
`[[business]] pay_period_days = 7`, which *"is a week because seven Days is a week, and `CONTEXT.md`
needs no new noun."*

**6. A Household has no member-count column.**
Composition is the member list's length. A stage table that states *adults + children* has to be
read against that list rather than against a stored pair.

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

A stage histogram over a long run is the thing to watch, and the specific question to ask it is
**whether the founding generation's echo ever damps**. `W` exists to smear it; whether four widths of
16 Days are enough to blur a cohort across 160 Days of life is not something this document can settle
by reasoning, and it is the first number that will move.

**Replacement Rate is the readout the milestone owes** — children per Household, where **two is exact
replacement** and the threshold is *"a consequence of conservation rather than a chosen constant"*.
It cannot be produced before stage 3 and is meaningless before stage 2.

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
are complementary, and a city that generates its own next generation is what makes housing Families
structurally necessary rather than merely encouraged.
