# 0032 — Test tiers

Its own axis. **A proposal, not a milestone** — it changes what running the tests means and nothing
about the city.

---

## Status

✅ **BUILT 2026-08-19, the same day it was proposed, on `milestone-10-conserved-money`.** The
assertion tier is **42s over 1,690 tests in Release**, against a full suite of **36m22s** — Q1
answered, and the proposal's own guess of *"plausibly ~1 minute"* was right rather than merely the
right order. `CLAUDE.md`
→ *Running the tests* is the operator-facing half and is where the commands live.

⚠ **The sweep found seventeen instruments across eight classes, and the table below reaches three of
those classes.** Seven of the seventeen are the Parking rows named here; the other **ten** are in
`Movement` and `Tables`, and not one of them was suspected. ***A list of the slow things you noticed
is not a list of the slow things.*** The tell is a measurement rather than an argument: excluding
exactly the three classes this table names left a run **still going at 42 minutes**. What closed the
gap was classifying by *what a test is for* — the axis this document argued for — rather than by
which tests had been observed being slow, and the second reading is that the axis paid off in a
direction its own author did not expect.

⚠ **And every figure in the table below is Release while the diagnosis that produced it was Debug**,
which is a second reason the four looked sufficient. ***A duration quoted without its build
configuration is not a duration.***

*Original status follows.* 🟡 **PROPOSED 2026-08-19, unscoped, and it owns no decision yet.** It exists because a full
`dotnet test` was measured at **36m22s** on the reference machine while
[`plans/0030`](0030-save-load.md) records the same suite at **9m38s** the day before — and because
**34m22s of the 36 is one test**, which is not catching anything.

⚠ **It was found by accident.** The session that produced it was diagnosing a hang — an
`IndexList` self-link in the Parking Shed's supply index — and the suite's cost is what the
diagnosis kept running into rather than what it set out to measure. ***A cost nobody was looking for
is still a cost, and the rule that it must reach the document that owns it does not care how it was
found*** ([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)).

**Nothing here is settled.** Two of the three questions below are *measurable* and none of the three
has been measured.

---

## What was measured

On the reference machine — a 2020 six-core i5-10400, twelve threads, `powersave`, **Release**,
nothing else running — on 2026-08-19, against the milestone-7 merge:

| Test | Wall clock |
|---|---|
| `ParkingArrivalStreamTests.What_the_arena_is_worth_at_a_million_citizens` | **34m 22s** |
| `ParkingShedSizeTests.A_sheds_size_is_a_property_of_the_radius_and_not_of_the_city` | 9m 45s |
| `ParkingShedCostTests.Where_a_shed_querys_microseconds_go` | 9m 9s |
| `SaveLongRunTests.The_hundred_thousand_Tick_save_run` | 1m 34s |
| `ParkingArrivalStreamTests.What_the_shed_query_costs_a_tick_with_the_cache_and_without` | 36s |
| **the whole suite — 1,634 tests, all green** | **36m 22s** |

⚠ **Read these against the suite, not against each other.** `plans/0030` already recorded the shape
they sit in: *"a test's cost in isolation is not its cost in a suite"*, because xUnit runs collections
in **parallel**. ***The suite's wall clock is its longest test and not the sum of its tests.*** The
rows above therefore do not add up to 36m22s, and the recorded **9m38s** was never evidence that the
suite was cheap either — a suite that measures 8m58s has a nine-minute critical path, and back then
`ParkingShedSizeTests` **was** that path. Both figures record the length of one test with everything
else hiding inside it.

⚠ **The suite did not drift from 9m38s to 36m22s. It moved in one commit, on one day.**
`ParkingArrivalStreamTests` was added by **`639a3a0`** — milestone 7 task 3 — on **2026-08-19**, and
it did not exist when `plans/0030` took its measurement on 2026-08-18. So the twenty-seven minutes
have a single author and a single date, and the critical path changed hands from a nine-minute
instrument to a **thirty-four-minute** one. ***A suite's cost is not a slope, and looking for one is
what hides the commit that moved it.***

⚠ **Note what that commit's subject says**: *a query that reaches everything before it keeps anything
spends nine parts of its cost on what it discards*. The finding is a real one and the test earns its
existence. What it does not earn is thirty-four minutes of every future full run, for a number that
moves only when the shed query does.

⚠ **The 9m38s figure has already been misquoted once, by the session that wrote this document.** It
was read as *the suite costs 40 seconds*, which is the number beside it — the **delta** milestone 8's
two new tests added, `8m58s → 9m38s`. `plans/0012` **Cause 5** by construction, inside a day of the
sentence being written, and recorded here rather than quietly corrected.

---

## The proposal, and why the obvious axis is the wrong one

**The obvious axis is duration — small, medium, large — and it is wrong because it describes the
symptom.** The clearest case is the thirty-four-minute row, whose name is the argument:
*what the arena is worth at a million Citizens*. It is not asking whether the city is correct. It is
pricing an allocator, and the price is a figure for a document. The second clearest sits one table
over. Here is what `ParkingShedSizeTests` actually does:

```
3 radii (200 / 400 / 800 m) × up to 4 populations (golden, 16_000, 64_000, 1_000_000) × 1_024 Ticks
```

That is a parameter sweep with a million-Citizen case inside it, and it is not looking for a
regression. It is **producing the number** behind `[parking] radius_metres` and `shed_keeps` — the
two rows `CLAUDE.md`'s Constants table carries and [`plans/0002`](0002-open-questions.md) §D tracks
the ratification of. The same is true of its two neighbours, and their names say so out loud:
*where a shed query's microseconds go*, *what the arena is worth at a million Citizens*.

***The discriminator is assertion against instrument, and duration is downstream of it.*** An
**assertion** fails when the city changes and must therefore run every time. An **instrument**
produces a figure for a document to quote, and re-running it on every invocation re-derives a
constant that did not move. The project already has homes for what an instrument produces —
[`plans/0013`](0013-tick-budget.md) for what a Tick costs and
[`docs/spike-results.md`](../docs/spike-results.md) for recorded spike numbers — so the output has
somewhere to live that is not a nine-minute wait.

**Duration tiers then fall out for free**, because the instruments are the slow ones, and they fall
out as something *enforceable*: a test declares its tier and fails when it outgrows it. ⚠ **An
undeclared tier is the one thing this must not ship**, because a tier nothing checks is a per-test
status stored in a second place, which is `plans/0012` **Cause 1** — *every document that stores
per-slice status drifted*.

⚠ **AMENDED 2026-08-19 BY THE BUILD, and the prohibition is right about the risk and wrong about the
remedy.** *An undeclared tier must not ship* was read as *every test must declare one*, which is 142
files today and every new file for ever. What shipped instead is a **default the guard applies**:
absence means assertion, and an assertion is held to the budget automatically — so an undeclared test
is the **most** checked one rather than the least. ***A default the guard applies is not a second copy
of a status; a default the guard skips is.*** The exhaustive form would have paid friction on every
future file to protect a case the budget already catches by timing.

---

## What this must not do

- **It must not delete an instrument.** The nine minutes buys a number the corpus quotes; the
  proposal is about *when* it is paid, never about whether the measurement is worth taking.
- **It must not introduce a hand-maintained map from source paths to test traits.** That map is a
  second copy of the dependency graph and it will drift from the first. Real test-impact analysis
  needs per-test coverage data, which is a different and much larger piece of work — see Q3.
- **It must not weaken the Definition of done by stealth.** `CLAUDE.md` and
  [`plans/0003`](0003-build-plan.md) both say *`dotnet test` must be green*. If some tests stop
  running by default then that sentence names an invocation it no longer describes, and **changing
  what it names is an ADR** rather than a config edit.
  ✅ **DISCHARGED 2026-08-19 by [`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)**,
  which is that ADR. The sentence turned out to be a **milestone**'s gate that was being honoured at
  every commit, so it changed by not one word; what changed is what a *commit* is gated on, and the
  instruments gained a **third lane** — post-submit, on a runner, nightly. ✅ **Both lanes ran green
  2026-08-19** — the assertion job in **1m59s**, the whole suite in **40m03s**, a 100,000-Tick
  headless balance run in **5m04s**, all on a GitHub-hosted runner of unstated class, so
  [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s *unbuilt* is
  discharged for the **jobs**. ⚠ **Not for the *schedule***: that run was reached by
  `workflow_dispatch` and no cron has ever fired, so ***a workflow proven by hand is a proven job and
  an unproven trigger.*** ⚠ **And none of those three figures may be quoted as a cost of anything but
  the lane** — 40m03s sits close enough to the reference machine's 36m22s to read as corroboration,
  and it includes checkout, an SDK install and a full build on a machine nobody named.
- **It must not touch the State Hash.** Nothing here is a change to the city, so
  [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
  does not arise and no baseline moves.

---

## What was built

Five files in `tests/Borough.Tests`, thirteen tag sites, and one section of `CLAUDE.md`.

| File | What it is |
|---|---|
| `Tier.cs` | The two values and the trait key. ⚠ **Filter on `tier!=instrument` and never on `tier=assertion`** — the default is reached by *absence*, so the positive form selects the handful that bothered to say what they already were and drops the ~1,600 that did not |
| `TierTimingFramework.cs` | An assembly-level `[assembly: TestFramework]` hook that records every test's duration. **It observes and never decides** |
| `TierBudget.cs` | The 4-minute assertion budget and the recorded durations |
| `Corpus/TierBudgetTests.cs` | Fails when an assertion-tier test exceeds the budget, and prints the slowest ten either way |
| `Corpus/TierDeclarationTests.cs` | A declared tier is one that exists, and instruments stay under a quarter of the suite |

⚠ **`[Trait]` rather than a custom attribute, and that is a decision.** xUnit 2's custom trait
attributes need an `ITraitDiscoverer` named by **string** in a `[TraitDiscoverer]` — a citation no
compiler checks, in a project whose corpus rules exist because uncheckable citations rot. What the
plumbing buys is a prettier call site. ***A mechanism whose only advantage is syntax is not worth an
unchecked string.***

⚠ **Reading a trait back needs `CustomAttributeData`, and the obvious reflection fails silently.**
`TraitAttribute` takes its name and value as constructor arguments and **stores neither** — there is
no `Name` or `Value` property — so `GetCustomAttributes<TraitAttribute>()` yields objects with
nothing readable on them and both guards would have passed by finding *nothing* rather than by
finding *everything*. ***A check that reads the wrong surface reports the same green as a check that
passes*** — [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
on a library rather than on this build.

⚠ **The budget's own limit is that it cannot see a test it did not run.** `TierBudgetTests` reads
durations recorded when a test *finishes*, so it observes only what completed before it, and a
filtered run times only what passed the filter. This is why `CLAUDE.md` names the **full Release
run** as the pre-commit gate and not this test. ***A guard that runs inside the thing it measures
cannot bound the part of it that has not happened yet.***

---

## Open questions

Typed per [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
**None may be cited as decided until the number beside it exists.**

✅ **Q1 — what does the suite cost with the instruments excluded? MEASURED 2026-08-19: 42s over
1,690 tests, Release, all green, nothing else running.** Against 36m22s for the full suite, which is
**~52×**. The guess of *"plausibly ~1 minute"* is retired and turned out to be right.

⚠ **It took three readings to get one, and the first two are why this paragraph names its controls.**
1m52s and 50s were both taken while a **second session was running `Borough.Tests` on the same six
cores** — 804% combined on a twelve-core box. They were written down as **upper bounds**, which is
the one thing a spoiled measurement is still good for, and 42s is the first reading with the box to
itself. ***A test-cost capture is a parallelism measurement, so it takes a parallelism measurement's
controls*** — the rule [`plans/0000`](0000-board.md) already carried from a threading capture that
read bimodally on 2026-08-14, arriving at a second instrument that had not thought it applied.

⚠ **The number depends on the classification and not on the exclusion count, which is the finding.**
Excluding the three classes *this document names* left a run unfinished at **42 minutes**; it took
all **seventeen** instruments to reach under a minute, and **ten** of those had never been observed being
slow. The measurement that mattered was a **reading of every test's purpose**, not a stopwatch.

✅ **Q2 — is the discriminator assertion-against-instrument, or duration? SETTLED 2026-08-19, and it
carries both: the tier is the purpose, the budget is the check.** `Tier` declares two values and
`TierDeclarationTests` refuses a third, so the taxonomy is a purpose. `TierBudget` holds an
assertion-tier test to **4 minutes** — derived from the slowest honest assertion in the suite, the
100,000-Tick save run at 1m34s, doubled and rounded — and `TierTimingFramework` times every test
through an assembly-level hook so the budget applies to a test *nobody thought about*.

⚠ **The budget is the half that does the work, because the failure is an untagged test rather than a
mislabelled one.** A declaration guard asks whether somebody wrote a word down; a budget asks whether
the fast tier is still fast. ***A tier nothing times degrades silently, because every test in it
stays green while it gets slower.***

✅ **Q3 — should selection ever be driven by the change rather than by the tier? CLOSED AS MOOT
2026-08-19, on its own stated condition.** It was gated on Q1 and Q1 came back at 42s, which is
*"a minute"* for the purposes of the sentence that gated it. Test-impact analysis needs per-test
coverage data and a map this document already refused; nothing now justifies that work.
***A question that writes down the number that would close it can be closed by a measurement nobody
took for its sake.***

⚠ **No number here is hash-bearing**, so
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
does not apply and **none of these belongs in `plans/0002` §D**. A test budget bounds how long a
developer waits; it does not enter the city. Recorded explicitly because §D is where a chosen number
reflexively goes, and this one must not.
