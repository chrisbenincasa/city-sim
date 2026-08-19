# 0032 — Test tiers

Its own axis. **A proposal, not a milestone** — it changes what running the tests means and nothing
about the city.

---

## Status

🟡 **PROPOSED 2026-08-19, unscoped, and it owns no decision yet.** It exists because a full
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
- **It must not touch the State Hash.** Nothing here is a change to the city, so
  [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
  does not arise and no baseline moves.

---

## Open questions

Typed per [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
**None may be cited as decided until the number beside it exists.**

**Q1 — what does the suite cost with the instruments excluded? *Measurable*, and unmeasured.**
The refuting number is a wall clock; the machine that produces it is one `dotnet test --filter` run.
The session that wrote this guessed *"plausibly ~1 minute"* from the parallelism argument above and
**that guess is not evidence**. Measure it before any of the rest is worth arguing.

**Q2 — is the discriminator assertion-against-instrument, or duration? *Arguable*.**
The case above is that duration describes the symptom. The case against is that an operator wants to
ask *"what can I run in ten seconds?"* and a tier named after a purpose does not answer that. A
resolution may well carry both — the tier is the purpose, the budget is the check.

**Q3 — should selection ever be driven by the change rather than by the tier? *Measurable, and
gated on Q1.*** If Q1 comes back at a minute, the whole question is moot and closing it costs
nothing. It only becomes live if the assertion-only suite is itself too slow to run on every change,
and the number that would settle *that* is the same wall clock.

⚠ **No number here is hash-bearing**, so
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
does not apply and **none of these belongs in `plans/0002` §D**. A test budget bounds how long a
developer waits; it does not enter the city. Recorded explicitly because §D is where a chosen number
reflexively goes, and this one must not.
