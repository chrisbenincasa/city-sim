# The commit gate is the assertion tier, and a long test runs post-submit on a machine that is not yours

**Three lanes, not two. A commit is gated locally on `dotnet test -c Release --filter "tier!=instrument"` — the assertion tier, **42s** over 1,690 tests with nothing else running. The instruments and the long play states run **post-submit on a schedule, on a runner**, where their wall clock costs nobody's attention. [`CLAUDE.md`](../../CLAUDE.md)'s *Definition of done for any milestone* is unchanged and stays the whole unfiltered suite on the reference machine.** The band underneath is the operator's: **past five minutes a test stifles iteration and ten is the ceiling.** `FAST ITERATION` `HONEST DEGRADATION`

## Why

### 1. The gate being paid was never the gate that was written

`CLAUDE.md`'s list is titled ***Definition of done for any milestone*** and opens *"`dotnet test` — the whole suite, unfiltered — is green"*. That names a **milestone** boundary. It was being honoured at every commit, which is stricter than it says and bought nothing, because a milestone contains many commits and the suite runs at its end regardless.

⚠ ***A gate applied more often than it was written to be is not extra safety; it is an unpriced tax on every commit in between.***

### 2. 34m22s of the 36m22s full Release run is one test, and it cannot fail for the reason a gate exists

On the reference machine — a 2020 six-core i5-10400, twelve threads, `powersave`, **Release** — the full suite is **36m22s**, and `ParkingArrivalStreamTests.What_the_arena_is_worth_at_a_million_citizens` is **34m22s** of it ([`plans/0032`](../../plans/0032-test-tiers.md)). Both are Release full-suite wall clock and neither travels without that clause.

The discriminator `plans/0032` settled is *what would you do on the day it failed*. For that test the answer is **read the new number and paste it into a document** — it prices an allocator; it does not ask whether the city is correct. The commit gate was therefore spending nearly all of its wall clock on a check whose failure would not have stopped the commit.

⚠ **This is not an argument that the test is worthless, and `plans/0032` forbids that reading in terms**: *"it must not delete an instrument — the minutes buy a number the corpus quotes; the question is **when** it is paid."*

### 3. The answer to *when* is post-submit, and that is better than the milestone boundary this ADR first reached for

The first draft of this decision moved the instruments to the milestone gate. That is worse, for a reason the drafting missed and the operator did not: **a milestone is a long time to carry an unknown regression.** A scheduled post-submit run on a machine nobody is waiting at reports within a day, and the wall clock it spends is nobody's.

It also answers a question the local gate could never answer at all. The Definition of done requires *"100k+ Ticks with no collection and no magnitude trending upward at steady state"* — a **long play state**, which is exactly the class of regression a fast local gate is blind to by construction. That class has never had a lane. It does now, and it is the same lane.

⚠ **Post-submit is not a weaker gate, it is a differently-shaped one, and the shape is the point.** A pre-submit gate is *blocking* and therefore must be short; a post-submit gate is *notifying* and therefore may be long. ***A check that can take an hour is only affordable when nobody is waiting for it***, and the corollary is that making it affordable is what lets it be thorough rather than what lets it be skipped.

### 4. ⚠ A runner is a different machine class, so it can detect a regression and cannot produce a quotable figure

This is the sharp edge and it is easy to get wrong. [`0106`](0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md) requires a wall clock to name its machine class and thread count. Every timing figure in this corpus names the reference i5-10400. **A hosted runner is not that machine, and its class is not even stable between runs.**

So the two things the post-submit lane may and may not do:

| It may | It may not |
|---|---|
| Fail when an instrument **breaks** | Re-derive a number a document quotes |
| Flag a **relative** move against its own history | Supply that number's absolute value |
| Run the long play states for **collection and magnitude** growth, which are counts and are machine-independent | Update `plans/0013`, `docs/spike-results.md` or any figure carrying a machine clause |

⚠ ***A number produced on an unnamed machine is not the number it looks like***, which is [`plans/0012`](../../plans/0012-corpus-audit.md) **Cause 5** — the caveat that does not travel — arriving through a new door. **Producing a figure stays on the reference machine and stays a deliberate act.** CI tells you to go and re-measure; it is not the measurement.

### 5. The band is stated by the person doing the iterating, and that is the right source for it

**Past five minutes a test stifles iteration; ten is the ceiling.** No measurement settles this and [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) does not reach it — there is no number that refutes *"waiting this long makes me stop running it"*. It is a **preference about a working loop**, held by the only person who has one, and recording whose it is beats deriving it from something that merely looks objective.

`TierBudget.PerTest` is **4 minutes** and already sits inside the band; it was derived beforehand from the slowest honest assertion, the 100,000-Tick save run at 1m34s, doubled and rounded. Two routes landing in one band is worth a sentence and is evidence of nothing.

⚠ **No number here is hash-bearing**, so [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) does not apply and none belongs in [`plans/0002`](../../plans/0002-open-questions.md) §D. A test budget bounds how long a developer waits; it does not enter the city.

## Consequences

- **The pre-commit run drops from ~36m to 42s**, Release, reference machine, nothing else running. ⚠ **Two earlier readings of it, 1m52s and 50s, were taken while a second session ran the same suite on the same six cores** and are upper bounds rather than readings. Both figures are `plans/0032`'s and move with it.
- **A first `.github/workflows` lands in a repository that had none.** Two workflows: the assertion tier on every push, and the full suite plus a long headless balance run on a schedule.
- ✅ **BOTH LANES RAN GREEN 2026-08-19**, so [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s *unbuilt* classification is discharged for the **jobs**. ⚠ **It is not discharged for the *schedule***: the post-submit run was reached by `workflow_dispatch`, and a cron trigger has still never fired. ***A workflow proven by hand is a proven job and an unproven trigger***, which are two mechanisms sharing one file.
- **An instrument still compiles on every commit**, because `dotnet build` is untouched. The failure a deferral can hide is narrowed from *any* breakage to **runtime** breakage.
- **`CLAUDE.md`'s *Definition of done for any milestone* changes by not one word.** Its *Running the tests* table gains the post-submit lane and relabels the pre-commit row. [`plans/0003`](../../plans/0003-build-plan.md)'s *`dotnet test` must be green* likewise keeps meaning what it said.
- **The assertion tier's own wall clock is now the number that matters, and nothing guards it automatically.** `TierBudgetTests` bounds one test at 4 minutes; it cannot bound the tier's total, because a total is a wall clock and no test can observe the run it is running inside. What guards it is the figure in `CLAUDE.md` and somebody re-measuring — weaker than a test, and stated here rather than hidden.
- **`plans/0032`'s *must not weaken the Definition of done by stealth* is discharged rather than violated.** It required that changing what the gate names be an ADR. This is that ADR.

## What the runner actually cost, and the trap in reading it

First run, 2026-08-19, `workflow_dispatch` on `main`, GitHub-hosted `ubuntu-latest`, **class unstated and not stable between runs**:

| Job | Wall clock | What it did |
|---|---|---|
| `commit` / assertions | **1m59s** | checkout, SDK, `dotnet build -c Release`, the assertion tier |
| `post-submit` / `balance-run` | **5m04s** | the same setup, then 100,000 Ticks of `minimal.toml` |
| `post-submit` / `full-suite` | **40m03s** | the same setup, then `dotnet test -c Release` unfiltered |

⚠ **40m03s sits close enough to the reference machine's 36m22s to invite the exact error §4 forbids, and the closeness is what makes it dangerous.** Two numbers that disagree loudly are never confused; two that nearly agree read as corroboration. They are not comparable: the CI figure includes checkout, an SDK install and a full Release build, and it was produced on a machine whose core count, clock and neighbours are unknown and differ run to run. ***A figure that nearly matches a trusted one is quoted more readily than a figure that does not, and it is wrong in exactly the same way.***

**None of the three may be quoted as a cost of anything but the lane itself.** They say a job fits inside its ceiling. They do not price the suite, the simulation, or a Tick, and the moment one appears beside a reference-machine figure in a table it has become [`plans/0012`](../../plans/0012-corpus-audit.md) **Cause 5**.

## The lane runs on every push to `main`, and a quiet machine was never what made it pass

*Amendment, 2026-08-20. Nothing above is withdrawn; §3's window narrows and §4 is unchanged.*

**The trigger becomes `push` to `main`, and the nightly `schedule` stays as the backstop.** §3 chose a
daily window with a stated trade — *a day is the window a regression in a long play state may hide for;
the trade is against a runner-hour.* The operator's reading is that the window is too long when the
alternative is a developer running the 36-minute suite locally to find out sooner, and ***a lane nobody
will wait for is a lane whose window is set by how long somebody is willing to be blocked, not by how
long a regression may hide.*** A push-triggered run reports within the hour, on nobody's machine.

⚠ **§1's tax argument does not reach this and it is worth saying why.** *A gate applied more often than
it was written to be is not extra safety; it is an unpriced tax on every commit in between* — the tax
is **wall clock somebody is waiting on**. This lane is notifying rather than blocking, so its cost is a
runner-hour and not a developer-minute, and running it more often taxes nobody. **The cron stays**
because a repository with no pushes for a day still wants one reading, and because a scheduled run is
what survives a branch being the only place work is happening.

### ⚠ And the correction that matters more than the trigger: a quiet machine is a control on a CAPTURE, not on a RUN

`CLAUDE.md` states the quiet-machine control beside the test-cost table — *the 42s names nothing else
running in this repository as its first control* — and it is right, and it is about **taking a
reading**. It was then read, in this project, on 2026-08-20, as a reason not to run the full suite in
the background while doing anything else. ***That reading is wrong, and it is the expensive kind of
wrong, because it converts a rule about measurement into a rule about when you are allowed to work.***

Read from the test rather than from the prose about it
([`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)):
`ParkingArrivalStreamTests` is `[Trait(Tier.Key, Tier.Instrument)]`, and the **only two assertions in
the whole class** are `stream.Arrivals.Count > 0` and its twin — *the stream was not empty*. **Nothing
in it asserts a wall clock.** A noisy machine cannot make it fail. What a noisy machine costs is the
**accuracy of the figure it prints**, which is `0106`'s subject and nobody's until somebody copies that
figure into a document.

So, stated as the rule this ADR should have carried from the start:

| | Needs a quiet reference machine | Does not |
|---|---|---|
| **Producing a figure a document will quote** | ✅ and this is `0106`, unchanged | |
| **Running a suite to see whether it is green** | | ✅ noise cannot fail an assertion that does not name a clock |

⚠ **The corollary the operator asked for directly**: the 36-minute suite may be run detached while
other work continues, including other tests. The only thing lost is the printed figure, and losing it
costs nothing on a run nobody was going to quote. ***A gate is a question about correctness; a capture
is a question about speed; and only the second one needs the room to be silent.***

⚠ **And the premise underneath the complaint was already false.** The 34-minute instrument does **not**
run on every change and never did — §1 is exactly the decision that removed it. The working gate and
the commit gate are both `--filter "tier!=instrument"`, and both are **42s**. What costs 36 minutes is
the *milestone* gate and this lane, neither of which is a dev cycle.

## What would trigger revisiting

- **The assertion tier passing five minutes.** The band's own trigger, produced by one filtered run. The response is to find what landed untagged, never to raise the band.
- **The post-submit lane going red and staying red.** A notifying gate that is permanently red has stopped notifying, and at that point it is worse than no lane because it launders a real failure as background noise. The repair is to fix or delete it, not to mute it.
- **A CI figure appearing in a document with a machine clause it did not earn.** That is §4 failing in practice, and the response is a mechanical check in `tests/Borough.Tests/Corpus/` rather than a stronger warning in prose.
- **The push-triggered lane costing more than it returns.** A runner-hour per push is affordable at this repository's rate and would not be at ten times it. The response is to move the trigger back to a cron with a shorter window, not to delete the lane.
- **A cheap way to run the 34m instrument.** Its cost is a population sweep to a million Citizens; if that becomes minutes, the reason it left the commit gate is gone and it should come back.
