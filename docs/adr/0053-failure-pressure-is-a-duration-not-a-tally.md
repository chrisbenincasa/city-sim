# Failure pressure is a duration, not a tally

**A Building's failure pressure is *how long it has been continuously failing*, measured from the
moment it entered the failing state — never a count of failure events.** It resets to nothing the
instant the Building stops failing, so recovery is total rather than a debt worked off, and it needs
no decay rate to stay bounded because nothing accumulates.
`LEGIBLE CAUSE` `HONEST DEGRADATION` `SOLVE THE ACTUAL PROBLEM`

**The obvious implementation is the wrong one, and it fails in the direction nobody would check.**
Counting the times a Building's Rules reach a reporting terminal *inverts severity*: it condemns the
Buildings that are partly working and spares the ones that have stopped entirely.

## Why

**The inversion is a consequence of
[`adr/0045`](0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md), which is why it is not
visible locally.** A Rule that fails does not retry. It subscribes to the Bin that stopped it and
sleeps until supply arrives — `02 §4.1`'s entire economics, and `RuleEngine.Stop` says so in its own
comment: *"a starved District costs nothing at all until supply arrives, where a retry timer would
cost the same as a firing Rule for as long as the shortage lasted."*

So a terminal report is a **transition into a state**, not a repeating signal, and the two Buildings
a decline model most needs to tell apart produce event counts in the wrong order:

| Building | Events emitted | Tally verdict | Correct verdict |
|---|---|---|---|
| Supply never arrives — comprehensively starved | **one**, then silence for ever | healthy, immortal | condemned |
| Supply arrives intermittently — partly working | wakes, fails, re-subscribes, repeatedly | condemned | surviving |

**A tally would therefore demolish the recovering city and preserve the dead one.** Nothing about the
code at the write site looks wrong; the defect lives in the interaction between the decline model and
a subscription model settled eight ADRs earlier, and it is only visible if you ask what a *permanently*
starved Building emits.

**`RuleInstanceTable.Reported` was already the right shape and was being read as the wrong one.** It is
a `ConditionId` **level** — the condition this Rule Instance is currently reporting, cleared when the
Rule fires. A level answers *is it failing now*. Integrating a level over time gives a duration;
counting the edges gives the tally above. The column did not need to change. The question asked of it
did.

**The duration also deletes a number rather than adding one.** `02 §5.9` requires that pressure decay
— pressure which only accumulates makes every Building eventually fall over at a rate set by elapsed
time rather than by conditions, which is [`adr/0006`](0006-no-collection-grows-with-elapsed-time.md)
failing inside the mechanism built to demonstrate the city is bounded. A tally needs a decay rate
authored, tuned and ratified. A duration needs none: it is `now − since`, it is bounded by the age of
the failing state, and it returns to zero on success by construction. Under
[`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) the cheapest
way to satisfy a ratifier is to find the derivation that removes the choice, and this is the second
time that has paid — after tau and the arming stagger.

**The threshold is expressed in missed firings, not in Ticks**, for the same reason. A Rule fires
every `rate` Ticks when healthy, so a Building silent for `N × rate` Ticks has missed `N` firings.
Authoring the threshold in Ticks would mean a Ruleset that halved every rate silently doubled every
Building's lifespan — one tuning change quietly retuning an unrelated mechanism. In missed firings the
threshold is dimensionless and survives arbitrary Ruleset rate changes. The number is still free; it
is merely a number somebody can argue about without knowing the rest of the file.

**Sampling reads the duration; it never produces it.** A Zone Rule's sample is when the city *notices*
a condemned Building, not when the Building *fails* — the pressure was already true before the sample
arrived. This is the distinction that makes sampled decline acceptable at all: `CONTEXT` → Zone Rule
justifies sampling because *developers do not evaluate every Lot*, an argument about an actor choosing
among alternatives, and **abandonment has no actor**. Sampling the accumulation would give every
condemned Building a random lifetime distributed by sample size and Lot count, which models nothing.
Sampling the observation costs a lag in noticing and distorts no physics.

## Amendment — slice 10 task 7: which failure counts, and where the clock lives

**Two things this ADR settled by argument did not survive contact with the code.** Both are
narrowings rather than reversals: pressure is still a duration, recovery is still total, and there is
still no decay rate.

**The signal is a Rule Instance asleep short of an *input*, never any reporting terminal.** The
section above reads `Reported` as the level to integrate, and `Reported` is set only where an author
has written an `on_fail` chain ending in a report. `rulesets/minimal.toml` has no chain at all — its
own header explains why, every source available today being the Building's own Bins — so under the
only Ruleset that exists `Reported` is permanently `ConditionId.None` and no Building could ever be
condemned. **The obvious repair is worse than the omission.** The one Rule that fails in that file is
the *producer*, and it fails on `Blocking.Space`: the Bin is **full**, which is the healthy
surplus steady state the file was built to demonstrate. A terminal there would mark every dwelling in
the city as distressed for being well supplied.

`02 §5.9` already names the source — *"Rules repeatedly hitting their terminal fallback (**input
starvation**)"* — and `RuleEngine.Check` already separates the two failures by name. Nothing had
connected the two facts. So **pressure integrates `Blocked == Blocking.Supply`**, which needs no
Ruleset content to work, and `Reported` keeps the job it already had: the **sentence** behind a
demolition where an author has written a chain, and honestly absent where they have not.

**The clock lives on the Rule Instance, not on the Building.** This ADR's own argument is that
integrating a level over time gives a duration — and the level is per Rule Instance. Two things
follow that a Building-level column cannot express. **The threshold is in missed firings and a rate
is a property of a Rule**, so a kind running one Rule at 8 Ticks and another at 32 has two different
meanings for *three missed firings*, and a Building carrying one clock would have to pick one
arbitrarily. And **two Rules that began failing at different moments are two durations**, of which
the Building's is the longest. That maximum is computed where the sample reads it and stored nowhere,
so the derived-on-read property this ADR insists on is strengthened rather than weakened.

**The sentinel is Tick 0, and it is sound rather than convenient.** A Rule Instance is armed uniform
over `[1, rate]` at construction, so none can come due — or therefore fail — at Tick 0. The one value
a real starvation can never carry is the one that means *not starving*.

## Consequences

- **A Rule Instance carries the Tick it began starving, not a counter, and a Building's pressure is
  the longest of its instances'.** Derived on read, per the amendment above. There is no per-Tick
  bookkeeping, no accumulator column to bound, and no whole-world pass to decay anything.
- **`adr/0006`'s magnitude half is satisfied structurally rather than by tuning.** There is no
  quantity here that can trend upward, so slice 10's long-run assertion tests row recycling rather
  than a decay rate somebody chose well.
- **Recovery is absolute.** A Building whose supply returns is indistinguishable from one that never
  failed. That is a design position and not merely an implementation: there is no hysteresis, no
  scarring, and no memory of a shortage that ended. `02 §5.9`'s *cycle, not a spiral* is the shape
  this supports.
- **The inspector sentence is a duration**, which is weaker than `02 §5.9`'s worked example
  (*"74% of work trips exceeded commute budget over 30 days"*) and the same shape. A proportion over a
  window needs a tally over a window, which is a different and much more expensive instrument — and it
  is the one §5.9 eventually wants. This ADR does not forbid it; it forbids the naive tally that looks
  like a cheap approximation of it and is not.
- **Two of the three remaining hash-bearing numbers survive**, and the decay rate is struck from
  `0002` §D before it was ever entered.

## What would trigger revisiting

- **A failure source that genuinely repeats.** The inversion argument rests on Rules being
  subscription-driven. `02 §5.9`'s other two sources are not: a failing Trip is an event per attempt,
  and a local condition below tolerance is a level sampled per Tick. When Trips arrive in milestone
  5b, pressure will be integrating *three* signals of different shapes, and *duration of the worst
  one* may stop being the right composition.
- **A decline model that wants scarring.** If play testing shows that instant, total recovery makes
  neglect free — that a player can let a District rot and fix it with one intervention — then some
  memory of past failure is wanted, and that is a tally by another name. It should arrive as a
  deliberate design change with this ADR cited, not as a quiet accumulator.
- **`adr/0045` being revisited.** If a failed Rule ever retries on a timer, terminal reports become a
  repeating signal, the inversion disappears, and a tally becomes merely a worse duration rather than
  a wrong one.
- **The threshold needing to differ per Building kind** in a way missed firings cannot express — for
  instance a kind whose Rules legitimately idle for long periods, where silence is not distress.
- **A city where being unable to *sell* is distress.** The amendment reads only `Blocking.Supply`,
  because a full Bin is today the healthy state of a Building with no customer. The moment a Rule's
  output has somewhere to go — `pool`, and the supply chains of Phase 2 — a producer stuck on
  headroom stops meaning *well supplied* and starts meaning *nobody is buying*, which is a real
  decline signal that this amendment currently ignores by name. **The tell is the arrival of the
  first Rule whose output crosses an ownership boundary**, not a play-test observation.
