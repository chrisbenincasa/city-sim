# 0071 — Education changes a Citizen's working life

**`plans/0045` row 29.** Carries `06` milestone 15's remaining education and employment work.

## Status

🟢 **BUILT 2026-09-09.** All ten tasks landed; `scripts/test.sh` is green at 3,055 tests, fourteen of
them new in `tests/Borough.Tests/Rules/SchoolingTests.cs`. Every
number below is PROVISIONAL under `plans/0045` standing order 4, which suspends `adr/0052`, so none of
them opens a `plans/0002` §D row and none needs a ratifier.

⚠ **Five findings are recorded below under *What the headless lane found*, and the first of them says
the demonstration Ruleset does not exercise the private university.** The mechanism is covered by
`tests/Borough.Tests/Rules/SchoolingTests.cs` instead; the world is too poor to buy a degree, and the
reason is an economy question rather than a tuition number.

**Four saved columns joined the hash and three previously-dead ones gained writers**, so every golden
artefact was re-recorded: `world-hash.txt`, `session-trace.txt` and `driving-session-trace.txt`
(`adr/0100` — a hash move gets a commit subject that explains it).

---

## What the row asks for

> **Education changes a Citizen's working life.** Connect actual attendance, Skill Tier, employment
> eligibility and access to different earnings, through the school-to-work transition and Household
> consequences. Include experience progressing Tier 1 → 2 and the schooling requirement for Tier 3.

Its boundary names `CitizenTable.SkillTier`, `CivicEngine`, `EmploymentEngine`, `WorkSchedule` and
`WageEngine` as the starting points, and puts the expanded schooling levels, experience and
job-transition details in this scoping pass.

---

## What the build already holds — surveyed 2026-09-09

**Three saved, hashed columns on the Citizen have neither a writer nor a reader in `src/`.** Their
only write in the whole repository is `tests/Borough.Tests/Golden/GoldenFixtures.cs:661-663`.

| Column | Declared | State |
|---|---|---|
| `skill_tier` | `CitizenTable.cs:83`, `Saved<byte>` | Folds into every world's State Hash and means nothing |
| `experience` | `CitizenTable.cs:82`, `Saved<long>` | Same |
| `employment` | `CitizenTable.cs:84`, `Saved<byte>` | Same. Employment is expressed today by whether `Workplace` resolves |

**Attendance is already a real journey, and it already has two spellings.**

| Path | Gate | Where the Need moves |
|---|---|---|
| Scheduled | Ruleset states `[school]` | `CivicEngine.cs:222`, **at dismissal, after a `TripFate.Completed` arrival** |
| Unscheduled | no `[school]` | `ServiceEngine.cs:849`, **at dispatch — the Trip's fate is never consulted** |

`CivicEngine.ScheduleSchool` (`:117`) draws a bell and a dismissal minute per school Building id,
refuses an impassable or over-Budget walk, and runs a full outbound / present / return day.
`ServiceEngine.Collect` (`:475`) already enqueues **every eligible child separately** when `[school]`
runs, so per-child attendance is a fact the build can already see. `ServiceEngine.Traveller` (`:1070`)
picks a child, and a child is `Citizens.Age == 0` in a world declaring `[[life_stage]]`.

**Places are floor area.** `World.DeclaredPlaces` (`World.cs:5972`) is `FloorTilesOf(b)` over
`[capacity] floor_tiles_per_place`; `ServiceEngine.Match` is a deferred-acceptance match that turns
away the surplus and `ServiceEngine.Fail` (`:859`) records the refusal as `Miss.Full`.

**The Education Need is a saturating deficit and not an accumulator.** `RuleEngine.Write`
(`RuleEngine.cs:991`) clamps every Need to `[floor, 0]`. At `scheduled-school.toml`'s
`education_degrade = 2` / `education_recover = 2` it recovers exactly as fast as it falls and tops out
at zero, so a Household schooled throughout and one schooled only for the last fortnight are
indistinguishable on the Day its children leave. ⚠ **`adr/0104`'s *"the Need already holds the
history"* is therefore half true**, and decision 1 below is what that costs.

**Job matching is geometry and nothing else.** `EmploymentEngine.TryEmploy` (`:328`) dilates a
`CellRect` by the Commute Budget, draws uniformly over Buildings in it, takes the **first** tenant
where `World.HasJob` (`World.cs:6821`) is true, and ranks only by `CommuteRung`. The whole eligibility
set is four filters: live row, `IsOfWorkingAge`, no resolving `Workplace`, and a dwelling with an
Access Point. **No tier, credential, education or experience filter exists anywhere in the pass.**

**Jobs are derived, not declared.** `[[building]] jobs` and `[[business]] jobs` are both **retired at
the loader** (`RulesetLoader.cs:236-240`, `:2603-2609`). `World.TryDeclaredJobs` (`World.cs:6484`)
divides the premises' floor by `[capacity] floor_tiles_per_job`. There is no `requires_*`, `min_tier`
or education key anywhere in `rulesets/ruleset.schema.json`.

**Pay is flat per trade.** `WorkSchedule.Accrue` (`Movement/WorkSchedule.cs:98-103`) accrues
`trade.WagePerDay` per on-duty Tick; `WageEngine.Pay` (`:291`) settles it. The only per-Citizen inputs
are `LastPaidDay`, `EarnedWage`, `WageRemainder` and a drawn shift length. `WageEngine`'s own class
doc says so: *"This pays a flat declared rate, because the posted wage is unbuilt."*

**Formation exists and writes one field.** `LifeStageEngine.Sweep` (`:101`) → `World.SpawnChildren`
(`World.cs:2173`) forms **one Household per child**, moves the Citizen across, and writes
`Citizens.Age[child] = DrawAdultAge(child)` and nothing else. `World.FormHousehold` (`:2234`) opens a
zero balance and joins the Unplaced Pool — **the new Household is unhoused**, which decision 6 turns
on. `World.Dismiss` (`World.cs:7061`) is the existing path out of a job.

---

## The corpus conflict this row resolves

🔴 **Two records disagree about the route to Tier 3, and neither cites the other.**

| Source | What it says |
|---|---|
| [`adr/0104`](../docs/adr/0104-a-skill-tier-is-earned-by-attendance-and-the-credential-stays-a-wall.md) | The tier is read off the **Education Need the Household accumulated while it had children**, at Young Household formation. University is not mentioned |
| `CONTEXT.md` → *Schooling* | **University, attended by a Young Household In Education, "qualifies for Tier 3 — the only route there"** |

**They are layered rather than chosen between**, and the layering costs nothing either one claims:
childhood schooling sets tier **1 or 2** at formation, exactly as `adr/0104` describes, and
**university sets 3**, exactly as `CONTEXT.md` describes. `adr/0104`'s own summary already permits it —
*"schooling **influences** both boundaries and **gates** only the top"*. What moves is the number that
ADR files to `plans/0002` §D2: *how well-schooled a childhood must have been to reach tier 3* becomes
*how well-schooled a childhood must have been to qualify for university*, one level down and playing
the same role. **File this for [`plans/0012`](0012-corpus-audit.md)** on the day the freeze lifts;
under standing order 1 it gets no ADR now.

---

## Decisions taken at scoping

**1. The childhood score is a 70/30 blend of per-child attendance and the Household's ending
Education depth, both normalised to a 0–100 share.** *(Player's decision.)*

```
attendance = min(100, 100 * attended_days / full_attendance_days)   -- per CHILD
depth      = 100 * (Education[hh] - floor) / (0 - floor)            -- per HOUSEHOLD, at formation
score      = (attendance_weight * attendance + (100 - attendance_weight) * depth) / 100
```

Attendance carries 70 because `CONTEXT.md` → *Schooling* says the constant is *"a **duration** — Days
of completed attendance per level"*, and because a per-child count makes two siblings with different
school access into two different adults, which is `UNIQUE INDIVIDUALS` rather than a Household
average. Depth carries 30 because it is what `adr/0104` actually named, and because it is the only
term that can see a collapse in the final stage. ⚠ **Both weights are Ruleset keys and are expected to
move during balancing.**

**2. Three levels, and primary is a gate rather than a producer.** *(Player's decision.)*

| Level | Attended by | Effect |
|---|---|---|
| **1 — primary** | a Family's children | **A gate.** Secondary attendance below the primary gate does not count toward the score |
| **2 — secondary** | a Mature Family's children | The score's attendance term |
| **3 — university** | a Young Household **In Education** | The only route to Tier 3 |

The level a child attends is **derived from the Household's Life Stage**, not chosen and not stored on
the child: Family → 1, Mature Family → 2. A `[[building]] level` key on a `serves = "education"` kind
says which level it teaches.

**3. In Education is a Household *state*, not a Life Stage.** `CONTEXT.md` says so in as many words —
*"Studying and Unemployment are states, not stages … an In Education Household is 1–2 adults with no
children, which is a Young Household exactly"*. A new saved `state` byte on `HouseholdTable`.

**4. A student supplies no labour.** On enrolment the adult is `World.Dismiss`ed and skipped by
`EmploymentEngine`. This is the design's first Household that is a net fiscal cost on purpose, and it
is the whole reason `CONTEXT.md` files university under an investment rather than a service.

**5. Graduation is a fixed duration and confers Tier 3 on exit.** *(Player's decision.)* No university
Trips, no per-Day attendance, no second accumulator. ⚠ **This is the one place in the row where a
Service does not reach people by a journey**, which is `adr/0032` not applying rather than being
contradicted — a university here is a Building the player places and a Household enrols in, and the
attendance machinery stays with levels 1 and 2. **Named as a revisit trigger below.**

**6. Two university kinds, separated by which scarcity rations them.** *(Player's decision.)*

| | Rationed by | Charges | Employs |
|---|---|---|---|
| **Public** | **places** — floor area over `floor_tiles_per_place`, the same ceiling schools already have | nothing | nobody |
| **Private** | **money** — what the Household can pay | `tuition_per_day` | nobody, in this row |

**The degree is identical out of both.** A private degree that was *better* was considered and refused
on `CONTEXT.md` → *Skill Tier*'s own ground — the 2 → 3 cut earns its place *"because a category is not
a quantity"*, and letting money buy a better credential turns it back into one.

⚠ **A public university costs nothing to run here, and that absence is stated rather than built.**
Funding a service out of the treasury is `plans/0045` **row 32**'s scope by name. Under `adr/0070` an
unbuilt mechanism is not a design constraint, so nothing in this row may reason from a public
university being free.

**7. Public first, then private.** A qualified school-leaver takes a public place if one is free, else
tries private, else goes to work. Satisficing rather than optimising (`adr/0017`). This is what makes
underbuilding public capacity legible: the private system is visibly the overflow.

**8. Tuition is charged per Day and a Household that cannot pay drops out.** Entry requires a balance
covering `tuition_per_day * university_days`, so a drop-out means the money went somewhere else —
a failure with a nameable cause rather than a knob. **No reserve fraction and no second number.**

**9. No enrolment draw.** An `enrol_percent` was considered and refused: it would be a provisional
number with no mechanism behind it, supplying variation that capacity and affordability already
supply. Every number in the enrolment chain is a place or a price.

**10. The credential requirement is a minimum on the trade, and it filters.** `[[business]]
requires_tier`, checked in `TryEmploy` beside `World.HasJob`. `02 §5.4`'s *hard constraints are
filters, soft trade-offs are utility*. **A minimum only** — a tier-3 Citizen may take a tier-1 post,
because refusing one would strand people for no mechanism's benefit.

**11. `Employment` gets its writer, with the refusal reason.** *(Player's decision.)* A byte enum:
`None, Employed, NoVacancy, BeyondReach, BelowCredential`. This is what makes the unsuccessful path
sayable — *"unemployed: three posts in reach, all require tier 3"* — and it gives a dead hashed column
a reader.

**12. Earnings are the trade's rate times a tier percentage times a within-band experience premium.**
*(Player's decision.)*

```
pay_per_day = wage_per_day * wage_tier_percent[tier] / 100
                           * (100 + premium) / 100
premium     = experience_premium_percent * min(experience, band) / band
```

`CONTEXT.md` → *Skill Tier* names the within-tier premium as *"the design's only source of
productivity growth"* and as an intensive margin beside the extensive one. The premium is capped at
the band ceiling and never past it.

**13. Experience accrues per Day worked, carries 1 → 2 and never reaches 3.** `adr/0104` exactly. The
unschooled rate is a **ratio to** the schooled rate, so the Ruleset states one quantity and the engine
derives the other (`adr/0059`'s shape). A Citizen is *schooled* for this purpose if their childhood
score cleared the same cut that qualifies for university.

---

**14. A departing child takes an equal share of its parent Household's balance.** *(Player's
decision, taken during implementation.)* Derived rather than authored — the balance splits evenly
between the parent and each child leaving, so a family with three children keeps a quarter.

🔴 **Without it the private half of the university is dead and the row demonstrates half of itself.**
`World.FormHousehold` opened a Household at zero and its own remark said so deliberately: *"a
generated Household inherits nothing"*, because the estate goes to the treasury at dissolution rather
than down. But a student supplies no labour and enrolment is decided before anybody has ever been
paid, so a school-leaver could never afford tuition and **every enrolment in every city would be
public**. ⚠ **Money is conserved across it** — a transfer between two purses through the same doors a
wage uses, never an endowment, so `Invariant.MoneyIsConserved` stays green. And it is what makes
family wealth a term in who reaches Tier 3, which is the dilemma decision 6's split exists to produce.

**15. Enrolment is decided ONCE, on the first Day a formed Household has a dwelling.** A Household
that was asked repeatedly would be one that quits its job the year a university opens, which is a
different mechanism from a school-leaver's decision. ***So a university takes a generation to pay
off***, which is the lag the design says education should have.

**16. The private university employs its own Tier 3 staff, and that is what keeps its till bounded.**
Trap 6 below wanted a `rates` levy; a wage is better, because it makes tuition circulate rather than
drain, and because it gives a graduate somewhere to work in a world with no Office. ⚠ **It is not
decision 6 reopened** — employing staff is not a better degree.

---

## The order the work has to happen in

Tasks 1–5 are testable without 6, and 6 is the largest new mechanism in the row.

| # | Task | Touches |
|---|---|---|
| **1** | **Levels.** `[[building]] level` on a `serves = "education"` kind; two saved `ushort` columns on the Citizen counting attended Days per level; the level derived from the Household's Life Stage at `ServiceEngine.Traveller`; the counter incremented at the same two sites the Need recovers | `RulesetLoader`, `Ruleset`, `CitizenTable`, `ServiceEngine`, `CivicEngine` |
| **2** | **The childhood score and the tier at formation.** The blend, written onto each child in `World.SpawnChildren` before `DrawAdultAge` | `World`, `Ruleset` |
| **3** | **Experience and promotion.** A daily pass over employed Citizens; two rates; 1 → 2 only | `EmploymentEngine`, `CitizenTable` |
| **4** | **The credential filter and `Employment`.** `[[business]] requires_tier`; the enum; the refusal reason written at every early return in `TryEmploy` | `RulesetLoader`, `EmploymentEngine`, `World` |
| **5** | **Earnings.** The tier percentage and the experience premium, at `WorkSchedule.Accrue`'s accrual site and `WageEngine.Pay`'s non-scheduled fallback | `WorkSchedule`, `WageEngine`, `Ruleset` |
| **6** | **In Education.** The Household `state` column; entry evaluated **once the Household is housed**, because a formed Household is unhoused and cannot ask what is in reach; public-then-private; tuition per Day; drop-out; graduation to Tier 3. ⚠ **The intrusive student list was refused**: `ServiceEngine.Collect` already walks every Household on the same Day boundary, so a second walk is paid for, and a derived index would owe `DerivedRebuildAuditTests` a fixture for a saving nothing has measured a need for | `HouseholdTable`, `World`, `SchoolingEngine`, `Simulation` |
| **7** | **The demonstration Ruleset.** Derived from `scheduled-school.toml`: primary, secondary, both university kinds, a tier-3 employer that actually earns, and paying trades at tiers 1 and 2 | `rulesets/` |
| **8** | **The instrument.** Extend `--school` with the pipeline — children in each level, the score distribution, enrolment public and private, drop-outs, graduates, tier mix, employment by tier, earnings by tier, and the refusal reasons | `SchoolDump` |
| **9** | **Inspection.** A Citizen panel carrying level attendance, score, tier, experience and the employment reason; a tier overlay over dwellings | `Main.Information`, `Main.Panels` |
| **10** | **The checks.** Replay, save/reload, determinism, `DerivedRebuildAuditTests` for the student list, and the golden re-record | `tests/` |

---

## The traps, in the order they will be met

1. **The unscheduled school path credits a Trip that never arrived.** `ServiceEngine.cs:849` writes
   recovery at dispatch. An attendance counter written there would count journeys, not attendance.
   **The counter is incremented at arrival on the scheduled path and at dispatch on the unscheduled
   one**, and the demonstration Ruleset states `[school]` so the row's own evidence is arrival-based.
   ⚠ **Do not read attendance figures off a Ruleset without `[school]`.**
2. **A formed Household is unhoused.** `World.FormHousehold` joins the Unplaced Pool, so "is a
   university in reach?" is unanswerable at formation. Entry is a **decision the daily sweep makes
   once the Household has a dwelling**, and a Household that is never housed never enrols.
3. **A student may already have a job.** Employment needs a dwelling, and so does enrolment, so a
   newly-housed adult can be employed before the sweep reaches them. Enrolment calls `World.Dismiss`.
4. **`Citizens.Age == 0` means two things.** It is *child* in a world with `[[life_stage]]` and *this
   world has no demographics* everywhere else — `World.IsOfWorkingAge` (`World.cs:2127`) turns on
   exactly that. Every new predicate must ask `Rules.DeclaresLifeStages` first.
5. **Tuition must conserve money.** It moves from the Household's balance Bin to the private
   university Business's till through `World.Withdraw`/`World.Deposit`, never `BinTable.Move`, for
   `WageEngine.Pay`'s stated reason. `Invariant.MoneyIsConserved` stays green.
6. **A till that only fills is a magnitude trending upward.** The private university's balance must
   have a sink or the long-run test fails on `adr/0006`. ✅ **Resolved by decision 16** — the trade
   pays Tier 3 wages out of it, so tuition circulates rather than draining, and a `rates` levy is not
   needed.
7. **Four new saved columns move every golden hash.** Two per-level counters on the Citizen, `state`
   on the Household, and the writers for three columns that were previously always zero. Re-record
   with the command in `tests/Borough.Tests/Golden/README.md`; the commit subject explains the move
   (`adr/0100`).
8. **The tier filter can empty the labour market.** A demonstration whose only paying trade requires
   tier 3 employs nobody for the first two generations. The Ruleset needs paying work at tier 1 from
   Tick 0, or the row's first hundred Days show nothing but unemployment.

---

## The numbers

🔴 **Every one is PROVISIONAL, chosen by taste under `plans/0045` standing order 4.** `adr/0052` is
suspended, so there is no ratifier, no `plans/0002` §D row and no ADR. **Nothing this row measures
ratifies anything.**

| Key | Table | What it is |
|---|---|---|
| `attendance_weight_percent` | `[schooling]` | The 70 in the blend |
| `full_attendance_days` | `[schooling]` | Attended Days that score 100 |
| `primary_gate_days` | `[schooling]` | Primary attendance below which secondary does not count |
| `tier2_score` | `[schooling]` | The childhood cut for Tier 2, and the university qualification |
| `university_days` | `[schooling]` | How long In Education lasts |
| `tuition_per_day` | `[[business]]` | What a private university charges |
| `school_level` | `[[life_stage]]` | Which level this stage's children attend; 3 is refused |
| `level` | `[[building]]` | Which level a `serves = "education"` kind teaches |
| `requires_tier` | `[[business]]` | The credential minimum on a trade |
| `wage_tier_percent` | `[jobs]` | Three percentages, tier 1 to 3 |
| `experience_premium_percent` | `[jobs]` | The within-band ceiling |
| `experience_per_day` | `[jobs]` | What a Day worked is worth |
| `tier2_experience` | `[jobs]` | The 1 → 2 promotion threshold |
| `unschooled_experience_percent` | `[jobs]` | The unschooled rate, as a ratio to the schooled one |

`level` on `[[building]]` and the `[schooling]` table are **required exactly where they are usable and
refused everywhere else**, the rule `TryAttendedRates` already applies to `education_degrade`.

---

## What watching it found — 2026-09-09

Read against `--school --ruleset rulesets/schooling.toml --citizens 2000 --ticks 300000 --schools 4`,
which is 146 Days on 16 schools, four of each kind — and against the shell, driven over its socket on
the same Ruleset with three of each kind placed by hand at Day 48.

🔴 **F1. Every Household in the demonstration world is destitute by Day 3, and it took the shell to
see it.** The inspector on one ordinary dwelling: *Household 286 · restock · waiting for money ·
**available 13 money units** · current shortfall since Tick 6,761 · 12,242 missed firings*, and its two
neighbours read 6 and 188. ***A Household holding thirteen units cannot buy a degree costing 8,192
however the tuition key is set*** — which is why the private college takes nobody, and it is not a
number this row chose. `restock` and `maintain` buy from the Pool every 8 and 16 Ticks against an
opening balance of 8,192–40,960 and a tier-1 wage of 4,096 a Day, so the purse is empty within three
Days and stays empty. **Lowering the two `[[hinterland]] prices` — sundries 100 → 16, repairs 250 → 32
— took employment from 82 to 146 and Tier 2 from 160 to 187, and moved private enrolment not at all.**
⚠ **The rates are `waged.toml`'s unchanged**, so this is a property of the base file rather than of the
schooling additions. **Balancing it is not this row's work**; what the row owes is that the mechanism is
proved somewhere, and `tests/Borough.Tests/Rules/SchoolingTests.cs` is where.

**F2. The private university is therefore inert, and tuition is not the reason.** 121 public
enrolments, **0 private**, 0 tuition paid, 0 drop-outs. Cutting `tuition_per_day` 512 → 64 changed
nothing; deleting the public `university` kind so the college was the only level-3 building gave **23
turned away and still 0 enrolled**; the same college with its `tuition_per_day` key removed takes 14
students. So it is reachable, it has room, and `Till` classifies it correctly — F1 is the whole cause.

**F3. The credential wall is the largest single reason anybody is out of work.** 183 `BelowCredential`
against 97 `NoVacancy` and **0 `BeyondReach`**. The `office` posts requiring Tier 3 stand empty while
tier-1 adults are refused at their doors, which is the row's own claim arriving as a number: a filter
that never bound would have read zero here.

**F4. The wage ladder is 5.8× end to end and only 2.6× of that was authored.** Tier 1 earns a mean
5,057 a Day, tier 2 earns 9,174, tier 3 earns 29,575. `wage_tier_percent` states 100/160/260, so the
rest is the experience premium compounding on top of a **different posted wage** — a tier-3 Citizen
works at an `office` posting 12,288 rather than a `grocer` posting 4,096. ⚠ **Do not read 5.8× as the
tier multiplier**; it is the multiplier times the premium times which employer will have them.

**F5. Schools are 16% short of places and nobody is out of reach.** 16,725 occasions, 14,112 delivered,
**2,613 refused at a full school and 0 unreachable**. In a world this dense the binding scarcity is
floor area rather than Severance, which is the opposite of what `adr/0032` was written against — and it
is why `--schools` is the lever that moves this instrument rather than the lattice origins.

**F6. `children in primary` reads 0 while primary schools record attendance.** The pipeline panel
counts children by their Household's Life Stage, and at Day 146 no Household stands in the stage that
maps to level 1 even though level-1 Days were attended earlier in the run. ***It is a level reading
rather than a flow***, and the trajectory table's `attended` column is where the primary schools are
visible.

**F7. A hand-built fixture cannot exercise job assignment at all, and it looks like a passing test.**
A Lot made through `Lots.Create` has no frontage and therefore no Address, so `World.AccessPoint`
returns `Address.None` and `EmploymentEngine.Assign` skips the seeker **before counting it as
seeking** — the pass runs, reports nothing and asserts green. `EmploymentTests` never noticed because
it drives `Employ`, `Dismiss` and `Adopt` directly and never runs the search. ***So the credential
test is built on `SyntheticCity`***, and any future test of the assignment pass must be.

**F8. Driving the shell cannot select a Household, so the education panel is reachable only by hand.**
`hold look` plus `click` selects a Building; the Household sections open from an *Inspect Household →*
link, and no verb presses one. So `Main.Schooling`'s section was read by hand and not asserted from a
driven run. ⚠ **A `hold` target for a Household would close this**, and it is the same gap any future
Household-level panel will meet.

---

## Definition of done

`plans/0045`'s amended definition: **done means you watched it and were surprised.**

- A Citizen's education changes which jobs accept them and what they earn, watched in the shell.
- The unsuccessful path is visible in the same world: a school-leaver refused by every post in reach,
  with the panel naming the credential as the reason.
- A public university full and a private one absorbing the overflow, with the poorer Households not
  reaching either.
- A drop-out — a Household that enrolled and ran out of money — traced to what it spent instead.
- `scripts/test.sh` green; replay, save/reload and determinism hold across the new columns.
- Findings recorded here, in this document, on the day.

## What this does not do

- **No treasury funding of anything.** Row 32 owns it, by name.
- **No posted wage.** `adr/0026`'s fill-rate-adjusted wage stays unbuilt; this row scales a flat rate.
- **No tier 0.** `adr/0104` refused a fourth segment and nothing here reads *never-schooled*
  differently from *unskilled*.
- **No parent drop-off**, no university Trips, and no change to the school day.
- **No Sorting change.** Educated Households do not yet arrive from the Hinterland preferring good
  schools; that is row 31's stock.

## What would trigger revisiting

- **A university that is never oversubscribed and never unaffordable**, which would mean the two
  scarcities are not binding and the public/private split is decoration.
- **Decision 5's fixed duration reading as a placement puzzle rather than a service.** If enrolment
  turns out to want a route and a Budget, the attendance machinery from levels 1 and 2 is the answer
  and `adr/0032` is why.
- **`adr/0032`'s gentrification corollary failing to appear.** It is the only damper on the
  schools → tier 3 → Office → exports loop; `adr/0104` withdrew a separate one on the strength of it.
- **A long run producing no tier-3 adults in a city with working universities**, or producing them at
  the same rate as one without. Both refute the cut directly.
