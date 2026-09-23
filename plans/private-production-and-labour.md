# Private production and labour

## Outcome

A production Rule's throughput depends on the workers actually present at its premises. Labour is a
Bin the Business spends, deposited by present workers and expiring within an hour or two. A bakery
with four bakers and a warehouse of flour bakes four bakers' worth of bread.

Shelf life generalises out of labour's expiry: a Resource may declare one, held as a small fixed set
of age buckets in the Bin whose shift cadence the Resource itself declares. Spoiled stock is counted
and discarded.

Scoped 2026-09-20 against `main` plus [0077](0077-ruleset-source-loading.md)'s package branch.
Supersedes the board's *Private production and labour* row. The board names this a prerequisite for
the founding loop and for the full Goods tree.

## Decisions taken

| Decision | Reason |
|---|---|
| Labour is a Bin a Rule **spends**, not a Readout it consults | A Readout is consulted, never spent, so two Rules on one Business would each see the full staff and both run at full rate. A Business could dodge its own labour constraint by splitting one Rule into two. `adr/0049` and `02 §4.1` both name the labour input Bin |
| A production Rule states labour among its inputs | Greedy apply already takes `min(band ceiling, every affordable)`, so staffing bounds output with no new engine arithmetic. This is the whole answer to *why not bake the entire warehouse at once* |
| The authored apply-band `max` stays as the equipment ceiling | Four bakers and one oven is a different limit from four bakers. Both are real, both are expressible, neither replaces the other |
| Labour expires rather than accumulating | Nothing else bounds it. An idle Business would bank a week of labour and spend it in one firing the moment its inputs arrived |
| Shelf life is a property of a Resource, not a special case for labour | Labour's shelf life is an hour or two and fish is days. One mechanism is cheaper than two, and labour is its shortest-lived case |
| Age buckets are a fixed small count in the row; the Resource declares the cycle | `shelf life = cycle × buckets` expresses any duration at constant storage. Precision error is a constant fraction of shelf life, so an hour matters on bread and a month does not on flour |
| Buckets shift in a Phase 3 sweep of the expiry table at each cycle boundary | The expiry table holds one row per expiring Bin, so the sweep visits only Bins that can spoil. The stored level stays true, so the roughly 60 existing `LevelAt`/`SpaceAt` readers need no Tick-aware read and Phase 2 recomputes nothing. Spoilage moves the level through the ordinary write, so a producer asleep on a full perishable Bin wakes. Shifting lazily on write was the earlier choice; it left every reader seeing spoiled stock and nothing to wake that producer (decided 2026-09-22) |
| Labour's shelf life is an hour or two, far shorter than a shift | A Resource's cycle is global but shift start is drawn per-Business, so a daily labour cycle would evaporate half a night shift's labour at midnight. Making expiry much finer than a shift dissolves the mismatch with no per-owner offset. It is also what happens — a worker present at 9am supplies an hour of work at 9am, and an idle hour cannot be banked for the evening |
| A Rule whose `rate` exceeds its labour input's shelf life is refused at load | Short-lived labour makes a slow Rule starve itself: a bakery firing daily would see only the last hour's work and waste the rest. The engine is right and the content is wrong, so the loader says so with a file and a line |
| The labour Bin is uncapped, like a money Bin | There is no physical container — the unused worker-time at a premises is just who is standing there and for how long. The cap that matters already exists upstream, because `floor_tiles_per_job` derives posts from floor area and a Business cannot employ more workers than its posts allow. Capping the Bin applies the same limit twice, and the second application is the one that fails silently. Shelf life is what keeps the quantity bounded |
| Labour's tier and experience grading declares its own percentages rather than reusing the wage's | Pay and productivity are separate causes that coincide today only because the flat `wage_per_day` is a productivity proxy. A designer should be able to say a tier-2 worker is paid 40% more and produces 25% more, which is the ordinary relationship between the two. Two Ruleset keys cost less than a coupling nobody could later explain |
| The deposit is denominated against elapsed work, unlike the wage | `WorkSchedule.Accrue` divides `wage_per_day` by the Citizen's own shift length, so pay is a fixed amount per day worked and a ten-hour shift earns what a six-hour one earns. Production must not work that way, because hours at the oven are what bake the bread. `labour_per_day` is instead what a full day of continuous work deposits, accrued per on-duty Tick over `Ticks.PerDay`. The level in a Bin then means the same thing at every Business, so a `[[recipe]]`'s labour amount is portable; under the wage's shape it would vary with the shift length each worker happened to draw |
| The deposit carries a per-Citizen remainder, as the wage does | `Ticks.PerDay` is 2048, so the per-Tick share is a shift rather than a divide, but tier and experience grading would still die in the truncation without somewhere to keep the fraction. `WageRemainder` is the precedent and the shape |
| A production Rule's apply band states `min = 1` | At `min = 0` the firing never falls below its minimum, so it succeeds at zero applications and re-arms every `rate` Ticks through every closed night — the retry timer `02 §4.1` refuses, paid by every production Business in the city. At `min = 1` the firing fails, sleeps on the labour Bin and does not re-arm, and the morning shift's deposit is the wake it was already waiting for |
| Short of labour does not start the pressure clock | `[[business]] work_days` is a weekly bit mask and six shipped Rulesets state a five-day week, so a weekday Business starves for about 62 hours between Friday's last shift and Monday's first. Every shipped `tenancy_ends_after_days` and `condemn_after_days` sits inside that, so arming the clock would evict or condemn every weekday Business on its first weekend. A load-time refusal cannot rescue it without forcing every decline threshold above three Days, which destroys their use as a decline signal. `RuleEngine.Stop` already distinguishes two failures where only one starts the clock — short of an input starves, out of space does not — and short of labour is the third case |
| A labour Resource whose shelf life outlives `shift_hours_min` is refused at load | An uncapped Bin is safe only while labour expires, and nothing otherwise stops a file declaring week-long labour. Labour that survives a shift change can be banked, so a Business shut for a weekend would spend 62 hours of standing around in one Monday firing. The bound is derived from the file rather than stated as an hour figure in the loader, which is a number nobody could ratify. It is deliberately loose — five-hour labour passes against a six-hour shortest shift — because what the design depends on is labour not crossing a shift boundary, not a particular duration. The refusal message says which of the two it is enforcing |
| A Household has no labour Bin, and a dwelling produces without staffing | Domestic work appears nowhere in `CONTEXT.md` or the simulation and economy documents, so it is undesigned rather than refused and `adr/0070` says an undesigned absence generates no design position. The consequence is written down rather than closed: a dwelling declares no labour Bin, so its Rules cannot state labour and produce for free, and a designer could put a factory in a house. Refusing production Rules on housing kinds would close it and would also refuse working fixtures. [Domestic labour](../docs/deferred.md#domestic-labour) |
| Spoiled stock is counted, not converted | [Waste as a Resource that moves](../docs/deferred.md#waste-as-a-resource-that-moves) |
| The posted wage stays out of scope | `adr/0026`'s fill-rate wage is unbuilt, and `adr/0070` says an unbuilt mechanism is not a design constraint. The flat `wage_per_day` that exists is enough to demonstrate payroll against production |

## What exists today

Established from code, not from documentation.

| Piece | Where | State |
|---|---|---|
| The present-worker predicate | `WorkSchedule.OnDuty` + `CitizenActivity.AtWork` | Built. `WorkSchedule.Accrue` walks every live Citizen each Tick and accrues wage only when both hold and the Citizen is not too ill. **Pay already depends on attendance; production does not** |
| Declared posts | `World.TryDeclaredJobs` | Built. The Business's tenancy share of its premises' floor over `[capacity] floor_tiles_per_job`. Divides twice, because a Business takes one of its premises' tenancies |
| `Readout.Jobs` | `Readouts.ReadBusiness` | Built, Business-scoped only. **Declared posts, never filled ones** — so `apply = { derived = "jobs" }` scales with floor area, not with staffing |
| Greedy apply | `RuleEngine.Check` | Built. Nets deltas per Bin, takes `min(band ceiling, every affordable)`. Already scales with input availability |
| Purchases | `RuleEngine.Buy` | Built. A `pool` input resolves to a local seller and settles three deltas atomically |
| Payroll | `WageEngine` | Built, flat rate. Daily entitlement accrues per Tick against attendance; payday moves money from the Business's balance Bin to the worker's Household |
| Staff loss | `plans/0065` | Built. Repeated short paydays dismiss staff and leave the premises standing |
| The gap | `plans/evidence/ruleset-authoring/scaling-and-bakery.md` | Measured: six declared posts, **zero workers**, 32 Produce consumed and 64 Food produced |
| A second labour mechanism | `World.Staffed` | Built, outside the Rule engine. Scales a service Building's places by `workers / jobs`. ⚠ Returns places **unscaled** when the trade is derelict, so deleting a `[[business]]` block silently disables staffing for every school in the city |

## Core: what changes

**1. A labour Resource family.** Resources already carry a `family`, and the loader knows three:
`good` moves on vehicles, `money` does not, `utility` flows without either. Labour is a fourth. It
has no haulage story, no market row, and cannot appear in a `pool` term. Money is the standing
precedent for a Resource that is not a Good.

**2. Deposit.** `WorkSchedule.Accrue` already holds the walk and the predicate, so the deposit goes
beside the wage accrual. Each on-duty Tick deposits `labour_per_day` over `Ticks.PerDay`,
graded by Skill Tier and Experience, which turns quality
into quantity — a skilled worker deposits more labour than a novice, and greedy apply then scales
throughput with experience without reading experience anywhere.

Because labour expires far faster than a shift, the Bin holds roughly the last hour or two of work
rather than an accumulating total. A Rule therefore fires against **who is present now**, not
against who has been present today. Production tracks the working day, drops when a shift ends, and
rises again when the next one starts, none of which needs a rate to express.

**3. Age buckets in an expiry table.** A separate table beside `BinTable`, with one row per
expiring Bin: a saved handle to the Bin and a fixed inline array of N counters. A derived
Bin-to-row index finds the row from the Bin. The cycle is global, so each boundary follows from the
Tick and no per-row clock is stored.

At each cycle boundary a Phase 3 sweep walks the expiry table. For each row it discards the oldest
bucket, moves the Bin's level down by that amount through the ordinary write, and opens an empty
newest bucket. A deposit adds to the newest bucket. A withdrawal draws the oldest buckets first.
The stored level therefore always equals the sum of the buckets, and every reader stays correct.

⚠ **The sweep lands on one Tick per cycle.** Its cost is unmeasured. Measure it at city scale
before this slice merges. If the spike matters, stagger Bins across the cycle.

**4. A waste count.** What the shift discards, per Business, reported as Evidence.

A labour Bin is uncapped, so a deposit never fails and no worker's presence is silently dropped. Labour Bins exist only where the premises' kind declares one, as with every other Bin, so a Business with no production Rule carries none and its workers deposit nowhere.

**5. Save format and goldens.** New saved columns, so the format advances and the golden artefacts
are re-recorded under the procedure in `tests/Borough.Tests/Golden/README.md`.

## The Ruleset surface

- `[[resource]] family = "labour"`, plus `shelf_life_cycles` and the cycle duration. Hours at the
  Ruleset boundary are already precedented by `shift_start_earliest_hour`, even though `CONTEXT.md`
  rejects hour as internal vocabulary.
- A `[[recipe]]` states labour among its inputs, at an amount per application.
- `[jobs]` gains `labour_per_day`, plus `labour_tier_percent` and
  `labour_experience_premium_percent` to grade it. Both mirror `wage_tier_percent` and
  `experience_premium_percent` in shape and are independent of them in value. Per-trade productivity
  belongs in the `[[recipe]]`'s labour amount rather than in a second base rate. `labour_per_day`'s
  note in `RulesetKeyNotes` states that it counts elapsed work while `wage_per_day` counts a day
  worked, because the shared suffix otherwise invites the wrong reading.
- A demonstration Ruleset with a real production chain. `stocked.toml` demonstrates recipe syntax
  with deliberately trivial recipes; `provisioned.toml` has the chain but no labour.

## Boundaries

Out of scope, each for a stated reason:

- **The posted wage.** Unbuilt, and `adr/0070` applies. Use the flat rate.
- **Trash as a Resource.** Deferred, with its own entry.
- **Equipment as a first-class concept.** The authored band `max` expresses it. A separate capital
  mechanism needs its own scope and does not block this one.
- **Migrating `World.Staffed` onto the labour Bin.** Services would then contend for labour with
  production, which is correct, but it changes every school in every shipped Ruleset. Named as a
  follow-up rather than carried here. Its derelict-trade fallback is a defect worth fixing whether
  or not the migration happens.
- **The Business nobody will work at.** Short of labour no longer starts the pressure clock, so a
  Business with no employees looks exactly like one that is closed, and the player cannot see the
  difference. That gap is real and it is not this plan's to close: zero employees is a hiring
  failure, and hiring already has its own machinery in declared posts, `adr/0026`'s fill rate and
  `plans/0065`'s staff loss. Reading it off a production Rule's starvation clock would detect it in
  the wrong place.
- **Per-instance parameter variation.** Already scoped out of the runtime factoring plan.

## Acceptance checks

Behaviour, in one Core world:

1. Zero workers present produces zero, and the Rule fails on **Supply** against its labour Bin with a
   wait list and an `on_fail` chain — not a silent success at zero applications.
2. Half staffing halves output.
3. Output accrues across the working day rather than in one firing.
4. A worker whose commute fails deposits nothing, and that premises' production falls the same day.
5. Labour does not survive its shelf life. An idle Business banks nothing, and a busy one late in
   the day holds no more than a busy one early in it.
6. A Resource with a declared shelf life spoils on schedule, and the discarded quantity is reported.
7. A Bin nobody writes to across many cycles still spoils on schedule, with no write needed to
   bring its level current.
8. Save and reload mid-cycle preserves the buckets, and the resumed world hashes
   identically.
9. A Rule whose rate outruns its labour's shelf life is refused at load, with the file and line, and
   so is a labour Resource whose shelf life outlives the shortest shift the file permits.
10. Paid local input, output purchases, payroll, staff loss and recovery, all in the same world.
11. A Business on a five-day `work_days` mask crosses its closed weekend accruing no failure
    pressure, keeps its tenancy and is not condemned, and resumes production on the Monday shift.

## Measurement results and implementation choices

The [first report](evidence/private-production-and-labour/README.md) prices individual deposits
and unconditional expiry storage. The [2026-09-22 follow-up](evidence/private-production-and-labour/combined/README.md)
compares guarded per-Business combining and storage allocated only for expiring Bins, with raw
Release samples, machine/load conditions and reproducible prototypes. Core, Rulesets, save schema
and golden fixtures are unchanged.

1. **Combine only when both wait lists are empty.** Actual Core probes show individual writes of
   6 then 1 can wake two waiters needing 6 each, while a combined 7 wakes only one. A guarded path
   retains individual writes when Supply or Space waiters are present and matches the individual
   path's State Hash. In two high-rate million-Citizen controls, combining saves 6.71 / 6.19 ms
   against individual+dense. Including compact storage leaves 13.35 / 13.82 ms of additional work
   above the existing wage pass. These are empty-queue desktop diagnostics, not whole-Tick capacity
   certification. The lower-rate control adds about 2.9 ms and has no same-Business writes to merge
   within a Tick, so it establishes no combining speed-up. Keep individual fractional accrual and
   per-Tick response; a per-Day fallback is not adopted.
2. **Compact expiry saves memory for the labour-focused case.** Its measured row is 64 bytes,
   including allocator fields and a saved Bin handle, plus a derived 4-byte reverse index on every
   allocated Bin slot. At one million Citizens with one labour Bin per Business, it uses 14.87 MiB
   versus 68.66 MiB of dense expiry storage; combining adds 1.72 MiB of scratch. The underlying Bin
   allocator's capacity doubling remains a separate cost. If every existing Good in that fixture
   also expires, compact storage crosses another capacity boundary and slightly exceeds dense
   storage. Bucket precision and authored work units remain content/design choices; the measurements
   do not select them merely by cost.

**Next:** use guarded combining and compact expiry as candidates for the integrated production
slice. The chosen boundary sweep drops the measured row's 8-byte clock, and neither the sweep nor
its Tick spike was part of these measurements. Verify actual Rule-generated queues and wake timing, bounded row removal/reuse, fractional
progress, expiry and save/replay equivalence, and price the real authored rate in the
acceptance world. The existing wage-only pass
already exceeds the 15.6 ms whole-Tick target in the constructed million-Citizen controls; these component savings
cannot settle the complete city's Tick budget. Both measurement steps are complete; the prototypes
do not complete the gameplay acceptance checks above.

## Corpus defects found while scoping

Route under `adr/0073`; neither blocks this work.

- `docs/04-economy-and-goods.md:56` states that a `wage` key is refused at load until milestone 15.
  False against the build: `[[business]] wage_per_day` loads and `WageEngine` pays it.
- `docs/deferred.md` *"A school employs nobody"* is stale. Schools are staffed and paid; what remains
  unbuilt is demand-determined payroll sizing.
