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

## Consequences

- **A Building carries the Tick it began failing, not a counter.** Pressure is derived on read. There
  is no per-Tick bookkeeping, no accumulator column to bound, and no whole-world pass to decay
  anything.
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
