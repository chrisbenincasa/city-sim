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
| Buckets shift lazily on write, never on a sweep | There is no Resource-to-Bin index — Bins are reached through their owner's list — so a per-Resource sweep would scan the whole table at every cycle boundary |
| Labour's shelf life is an hour or two, far shorter than a shift | A Resource's cycle is global but shift start is drawn per-Business, so a daily labour cycle would evaporate half a night shift's labour at midnight. Making expiry much finer than a shift dissolves the mismatch with no per-owner offset. It is also what happens — a worker present at 9am supplies an hour of work at 9am, and an idle hour cannot be banked for the evening |
| A Rule whose `rate` exceeds its labour input's shelf life is refused at load | Short-lived labour makes a slow Rule starve itself: a bakery firing daily would see only the last hour's work and waste the rest. The engine is right and the content is wrong, so the loader says so with a file and a line |
| The labour Bin is uncapped, like a money Bin | There is no physical container — the unused worker-time at a premises is just who is standing there and for how long. The cap that matters already exists upstream, because `floor_tiles_per_job` derives posts from floor area and a Business cannot employ more workers than its posts allow. Capping the Bin applies the same limit twice, and the second application is the one that fails silently. Shelf life is what keeps the quantity bounded |
| Labour's tier and experience grading declares its own percentages rather than reusing the wage's | Pay and productivity are separate causes that coincide today only because the flat `wage_per_day` is a productivity proxy. A designer should be able to say a tier-2 worker is paid 40% more and produces 25% more, which is the ordinary relationship between the two. Two Ruleset keys cost less than a coupling nobody could later explain |
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
beside the wage accrual. Amount per Tick is graded by Skill Tier and Experience, which turns quality
into quantity — a skilled worker deposits more labour than a novice, and greedy apply then scales
throughput with experience without reading experience anywhere.

Because labour expires far faster than a shift, the Bin holds roughly the last hour or two of work
rather than an accumulating total. A Rule therefore fires against **who is present now**, not
against who has been present today. Production tracks the working day, drops when a shift ends, and
rises again when the next one starts, none of which needs a rate to express.

**3. Age buckets on `BinTable`.** A fixed inline array of N counters plus the Tick the Bin last
shifted. The live level is a pure function of the stored buckets and the elapsed cycles.

⚠ **The read and the write must be split, or this breaks the phase discipline.** `RuleEngine.Check`
runs in Phase 2, which must not write — `Simulation.VerifyDecideWritesNothing` enforces it. So Check
computes the live level and stores nothing; Phase 3 commits the shift. The same arithmetic runs
twice and one result is discarded.

Anything asserting bounded quantities must read the live level rather than the stored one, or it
will see stock that has notionally already spoiled.

**4. A waste count.** What the shift discards, per Business, reported as Evidence.

A labour Bin is uncapped, so a deposit never fails and no worker's presence is silently dropped. Labour Bins exist only where the premises' kind declares one, as with every other Bin, so a Business with no production Rule carries none and its workers deposit nowhere.

**5. Save format and goldens.** New saved columns, so the format advances and the golden artefacts
are re-recorded under the procedure in `tests/Borough.Tests/Golden/README.md`.

## The Ruleset surface

- `[[resource]] family = "labour"`, plus `shelf_life_cycles` and the cycle duration. Hours at the
  Ruleset boundary are already precedented by `shift_start_earliest_hour`, even though `CONTEXT.md`
  rejects hour as internal vocabulary.
- A `[[recipe]]` states labour among its inputs, at an amount per application.
- `[jobs]` gains the per-worker deposit rate, plus `labour_tier_percent` and
  `labour_experience_premium_percent` to grade it. Both mirror `wage_tier_percent` and
  `experience_premium_percent` in shape and are independent of them in value. Per-trade productivity
  belongs in the `[[recipe]]`'s labour amount rather than in a second base rate.
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
7. A Bin untouched across many cycles reads the same live level as one touched every cycle.
8. Save and reload mid-cycle preserves the buckets and the shift clock, and the resumed world hashes
   identically.
9. A Rule whose rate outruns its labour's shelf life is refused at load, with the file and line.
10. Paid local input, output purchases, payroll, staff loss and recovery, all in the same world.

## Open questions

1. **Does the per-Tick labour deposit fit the budget?** `WorkSchedule.Accrue` already walks every
   Citizen each Tick, but adding a Bin write per on-duty Citizen is new traffic on the hottest table.
   Needs measuring against `plans/0013` before the deposit cadence is fixed. The fallback is one
   deposit per Business per Day, which costs the intra-day production curve and the direct link from
   a failed commute to that day's output.
2. **What does N cost?** Bucket count against `BinTable`'s current row width and live Bin count in a
   grown city. If it is noise, put the array on every Bin unconditionally and skip the indirection.
3. **What is `min` on a production Rule's apply band?** At `min = 1` an understaffed bakery fails
   outright and gets the wait list, which is the legible behaviour. At `min = 0` it succeeds at zero
   applications and re-arms having done nothing, which is the silent non-event `02 §4.1` names. The
   first looks right; it needs stating rather than assuming.
4. **Should a labour-family Resource with a long shelf life be refused at load?** Leaving the Bin
   uncapped leans on labour expiring quickly. A Ruleset could declare otherwise and accumulate. The
   guard is the same shape as the rate refusal and was not taken when the capacity was decided.
5. **Where does a Household's labour go?** Only a Business has a labour Bin under this scope. Whether
   domestic work is modelled at all is unasked.

## Corpus defects found while scoping

Route under `adr/0073`; neither blocks this work.

- `docs/04-economy-and-goods.md:56` states that a `wage` key is refused at load until milestone 15.
  False against the build: `[[business]] wage_per_day` loads and `WageEngine` pays it.
- `docs/deferred.md` *"A school employs nobody"* is stale. Schools are staffed and paid; what remains
  unbuilt is demand-determined payroll sizing.
