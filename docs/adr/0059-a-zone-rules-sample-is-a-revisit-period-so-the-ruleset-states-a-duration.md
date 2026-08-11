# A Zone Rule's sample is a revisit period, so the Ruleset states a duration and the engine derives the count

**A `[[zone_rule]]` authors `revisit_ticks` — how long it takes the development industry to look at
every Lot once — and the engine derives `sample = ceil(Lots × interval ÷ revisit_ticks)` per trigger. An
absolute count of Lots per cycle is the wrong unit: it makes the quantity the city actually feels, which
is a *fraction of the city per cycle*, depend on the size of the city. The revisit period is set to one
Day from `TICKS_PER_DAY`, so it is derived rather than picked and there is no free parameter.**
`EMERGENCE` `SOLVE THE ACTUAL PROBLEM` `HONEST DEGRADATION`

## Why

### The absolute sample cannot build a city at target scale, and this is measured

S0b ran the shipped `[[zone_rule]]` — `interval = 32`, `sample = 4` — at 1,000,000 Citizens and
**built nothing**: `zones created` was 0 across 2,000 Ticks while `demolished` ran steadily, and
`zones vacant` was 0, because of ~512 Lots sampled not one was empty.

**The create predicate is not at fault, and the measurement is unusually clean about it.** `created`
equals `vacant` **exactly**, in every reading interval of every capture at every city size — so neither
the Unplaced Pool nor the permission bit has ever once declined a candidate. What limits creation is
vacancy, and vacancy is produced only by demolition.

`sample ÷ interval` is Lots-per-Tick and it is **absolute**, while the population it draws from is the
Lot table. So the period in which a given Lot is looked at once is `Lots ÷ (sample ÷ interval)`:

| Citizens | Lots | One visit per Lot every |
|---|---|---|
| 1,000 | 121 | 968 Ticks — **0.12 Day** |
| 1,000,000 | 120,001 | 960,008 Ticks — **117 Days** |

A player who bulldozes a district at target scale waits four game-months for the sampler to notice.
That is not pacing; it is a frozen city.

### It is structural rather than a badly chosen number, and the collapse is what proves it

The claim above is falsifiable, so S0b tested it rather than arguing it. If the only variable is
**τ = (sample ÷ interval) × Ticks ÷ Lots**, then equal τ must give equal occupancy at any size — and it
does, to **1.6 points across a 1,000× span in Lots**. The strongest row changed the **sample** 117×
instead of the Lot count and landed on the same curve, so τ is confirmed from both directions at once.

**No value of an absolute `sample` fixes this**, which is what makes it a decision about the unit rather
than about the number. Any constant makes the revisit period proportional to the Lot count.

### Cost was never the constraint, so the thing protecting the wrong unit was protecting nothing

`02 §5.7` justifies sampling with *"cost is constant regardless of Zone size — GlassBox's trick, and
the right one"*, and slice 10 task 9's tripwire confirmed it. That is the reason to fear a scaled
sample, so S0b measured it: holding a one-Day revisit at 120,001 Lots needs `sample = 469`, which is
**117× the Lot evaluations for no measurable Tick cost** — 18.36 s against 19.12 s over 2,000 Ticks,
the scaled run nominally the faster of the two and both inside a ±0.2 s spread.

**So the cost claim and the pacing claim were doing different jobs under one bullet.** The cost claim
survives and is stronger than stated. The pacing claim does not survive at all, and of `02 §5.7`'s four
mechanisms only **capital** scales with the city — the **build rate throttle** is absolute too, so two
of the four pace in a unit the section never names.

### The diegetic argument already says the right thing, and the model was not listening

`CONTEXT` → Zone Rule justifies sampling because *a developer does not evaluate every parcel*. That is a
sample per **developer**, and the model has exactly one developer per Zone Rule at every city size.
**A city of a million people has more developers than a city of a thousand.** Reading the sample as a
revisit period is what makes the implementation match the argument that admitted the mechanism.

Note what does *not* need to change: the Unplaced Pool is already a term in the create predicate, so
growth remains bounded by demand rather than by the sampler. `CLAUDE.md`'s *the Unplaced Pool is the
demand signal* is what makes it safe for the sampler to stop being a throttle.

### A duration divided out is a shape this project has already chosen twice

`adr/0051`'s decay tau and slice 8 task 3 settled exactly this: the Ruleset states
`pollution_decay_ticks`, a **duration**, and the per-cadence rate is divided out of it against whatever
cadence is in force — so a designer who doubles the period gets the same Day rather than two of them.
`revisit_ticks` is the same move against the Lot count instead of the cadence. Nothing new is invented,
and a durations-in-the-file convention gets its second consumer.

### Why one Day, and why that is not a number being chosen

`revisit_ticks` defaults to `TICKS_PER_DAY`. It is **derived rather than picked**, which is the arming
stagger's and tau's story: a Day is the period the rest of the simulation is denominated in, the
Ruleset's own rates are 8–32 Ticks so a Day is comfortably the coarser scale, and any other figure would
be a free parameter needing a ratifier under `adr/0052` for a quantity nobody has wanted to reason about.
**A designer may still author it**, because *how often the industry surveys the city* is legitimately a
feel decision — but the default is forced, so shipping requires no ratification.

## Consequences

- **Every hash moves, because the city's trajectory changes at every size.** ~~The three golden
  baselines are re-recorded.~~ **Two, as built** — `world-hash.txt` is a hand-built world with no
  Ruleset over it and no Zone Rule ever swept across it, so nothing here can move it, and re-recording
  it would have been a re-record of nothing. `World.HashSeed`'s version byte is **not** bumped: the fold
  is unchanged and this is a behaviour change, which is exactly the case the byte exists to distinguish
  *from*.
- **AMENDED BY THE CODE: the golden *session* had to be lengthened, and this ADR did not see it coming.**
  At the fixture's 132 Lots the derived sample is **1** where the retired `sample = 4` was four, so over
  the session's eight triggers it condemned and never landed on a Lot demolition had cleared: the
  committed trace **stopped covering the create branch entirely**, silently, because every hash moved
  anyway. 256 → **2,048** Ticks at a cadence of **64**, reload at **1,024**, plus a test asserting both
  branches ran. **The general form is worth more than the fix**: a baseline records what a run *did*, so
  a change that narrows what the run *reaches* is invisible in it by construction — which is the same
  shape as this ADR's own origin, where a tripwire confirmed the stated claim while the unstated one
  failed.
- **`ZoneSample.Draw`'s duplicate scan must go.** It is `O(sample²)` and justified in its own remarks by
  *"a sample is a handful of Lots, and a set would allocate, hash, and be walked in an order `05 §4`
  lint 3 bans"*. The premise dies here. At a one-Day revisit the scan is ~110,000 comparisons a trigger
  at 1M, amortised to ~3,400 a Tick and therefore affordable *today* — but it is quadratic in a quantity
  that is now proportional to the map, so it is a measured-affordable defect rather than a safe one, and
  it is replaced rather than carried. **As built it is *deleted* rather than replaced**: deduplicating
  within a trigger never bought coverage — the same slots come up either way and the scan only skipped
  the second look — and `02 §5.3`'s objection is to a doubled **weight**, which a boolean predicate has
  none of. The measured duplicate rate is ~0.2%, and because `sample ÷ lots` is now exactly
  `interval ÷ revisit_ticks` it is a property of the *file* rather than of the city, so one measurement
  settles every city size. `02 §5.4`'s choice model is the trigger for a stamp array.
- **`ZoneRuleEngine`'s scratch buffer changes what bounds it.** Its remark says *"bounded by the Ruleset
  rather than by elapsed time"*; it becomes bounded by the **Lot count**, hence by the map. Still
  `adr/0006`-safe — the map does not grow with elapsed time — but the stated reason is different and the
  remark is rewritten rather than left to be true by accident.
- **The sample becomes a *derived* quantity read from state, so it varies within a run** as Lots are
  painted. It is deterministic and the Lot count is already saved and hashed, so replay, thread
  equivalence and save/reload are untouched.
- **`sample` stops being a tuning number and its `0002` §D row is retired.** What replaces it is a
  derived default with no free parameter, so §D **loses a row rather than gaining one** — the first time
  that has happened, and `adr/0052`'s triage is where the distinction between a debt and a gap lives.
- **`02 §5.7`'s first bullet is split in two.** Sampling paces **cost**, and it is excellent at it.
  Growth is paced by **capital**, which does not exist yet — so between this ADR and capital's arrival
  the city's growth rate is bounded only by the Unplaced Pool, which is honest rather than tuned.
- **The 50% vacancy equilibrium and the five-sixths homeless figure both move**, and neither was a
  balance number to begin with: S0b showed they follow from one sampler driving both creation and
  demolition at the same rate, which is a property of `rulesets/minimal.toml` and not of the design.

## What would trigger revisiting

- **Capital arriving.** Once `02 §5.7`'s third mechanism exists it is the pacing mechanism, and the
  revisit period becomes a pure survey rate. If that makes the derived default feel wrong, the number
  becomes a real authored choice and needs a ratifier at that point rather than this one.
- **A Zone Rule scoped to a District rather than the city.** `CONTEXT` → Sweep Rule permits both. The
  denominator would become the District's Lot count, and whether a revisit period is per-District or
  per-city is a question this ADR does not answer because nothing can express it yet.
- **A create predicate that can score rather than only admit.** `02 §5.4`'s choice model would make
  *sample N and take the best* meaningful, at which point `N` buys choice as well as throughput and the
  two purposes may want different numbers. `0002` §D already records that `N` buys no choice today.
- **The map ceasing to bound the Lot count.** The scratch buffer's `adr/0006` safety rests on it. A
  procedurally extending map would make the sample unbounded and this would need a ceiling.
- **A measured cost.** The affordability here is one capture at one revisit period on `powersave`. A
  revisit period much shorter than a Day, or a map much larger than 4096², would want the number retaken
  before being trusted.
