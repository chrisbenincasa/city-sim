# Moving the State Hash costs nothing until somebody is carrying a save

**The State Hash test classifies a change; it does not price one. While no player holds a save, a
hash-moving change costs one documented command and a re-recorded baseline, and that cost must never be
cited as a reason to defer, narrow, split or reshape work.** The game is being implemented. Nobody is
carrying a city forward. A design change is therefore *cheaper today than it will ever be again*, and
treating hash movement as expensive is optimising for a constraint that does not exist yet.

`FAST ITERATION` `SOLVE THE ACTUAL PROBLEM`

Settled with the user in the room on 2026-08-14, after a sitting proposed splitting a vocabulary rename
into a prose half and a deferred code half on the grounds that the code half would move the hash — the
second time in one session that hash-fear had shaped a recommendation, and, per the user, something said
*"a million times"* and never written down.

## Why

### The rule that exists classifies, and it has been read as pricing

[`CLAUDE.md`](../../CLAUDE.md) → *Architecture invariants* states it exactly:

> **A change is an optimisation if the State Hash is unchanged, and a design change otherwise** —
> however it was motivated. This is the test that decides whether something may be tuned freely.

That is a **taxonomy**. Its purpose is to stop a behaviour change being smuggled in as a tidy-up, which
is a real hazard and is untouched by this ADR. It says nothing whatever about what a design change
*costs*, and the corpus has repeatedly supplied a cost by implication — because the re-record is
visible, mechanical and appears in commit after commit, so it reads as a toll.

**It is not a toll. It is a command**, and the failing test prints it for you:

```
dotnet run --project src/Borough.Headless -- --log tests/Borough.Tests/Golden/session.borough \
  --ruleset rulesets/minimal.toml --ruleset rulesets/minimal-tuned.toml \
  --ticks 2048 --hash-every 64 --out tests/Borough.Tests/Golden/session-trace.txt
```

***A guard that tells you how to satisfy it is not charging you anything.***

### The thing that would make it expensive does not exist

A hash move is expensive when somebody is holding state on the other side of it — a save that must
migrate, a replay somebody else recorded, a published number computed from a world nobody can rebuild.
**None of those exists.** There is no renderer, no build anyone has played, no save outside this
repository, and every golden artefact in the tree is regenerated from a command in the tree.

Under [`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) this is the *refused*-versus-
*unbuilt* distinction applied to a **cost** rather than to a mechanism: the migration burden is not being
waived, it is **not yet incurred**. Reasoning from it today is reasoning from an absence, and the answer
is not to pay it early.

### What genuinely survives, stated precisely, because this ADR is narrow

Three real constraints look like this one and are **not** repealed:

- **Attribution.** A hash move should be *explainable* — one commit whose subject says what moved the
  city and why. [`plans/0003`](../../plans/0003-build-plan.md)'s hash-moving queue exists for this and
  keeps its name. Its reason is that a change riding along inside an unrelated slice cannot be read back
  out of the baseline, which is true regardless of cost.
- **Collision.** Two sessions re-recording the same three baselines in one working tree conflict. That
  is **scheduling**, not economics, and the remedy is sequencing or a worktree — never abandoning the
  change.
- **Incidental movement.** A change that moves the hash for a reason unrelated to its purpose is still a
  defect worth chasing; `World.DestroyBuilding` carries a comment about exactly this. The signal is
  *unexplained* movement, not movement.

**The failure mode this ADR names is none of those.** It is a recommendation that gets *smaller* —
deferred, halved, downgraded to prose — because somebody priced the re-record as a cost to the design
rather than as a chore in the build.

### The evidence is that the rename this was written beside cost nothing

The `Blocking.Level`/`Blocking.Headroom` → `Supply`/`Space` rename touched fifty-two call sites across
four projects. It was proposed as *settle the vocabulary now, defer the code until 5c closes*. Done
whole instead, it took one build, one suite run and one re-record — and **the State Hash did not move at
all**, because the enum's explicit values were preserved. What moved was the four Rulesets' *content*
hashes, and only because comments inside them were edited.

So the deferral would have bought nothing, split one idea across two commits and two weeks, and left the
corpus carrying a word that [`adr/0097`](0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md)
had already used meaning its opposite. ***The estimate that justified the caution was not merely too
high; it was of the wrong quantity.***

## Consequences

- **No plan, board row, ADR or slice may cite hash movement as a cost.** If a change is right, it lands
  whole. A sentence of the form *"this moves the hash, so…"* is now a defect in the document that
  contains it, and the repair is to say what is *actually* at stake — attribution, collision, or nothing.
- **`plans/0003`'s hash-moving queue is a sequencing device, not a debt ledger.** Items sit in it so that
  each gets a commit whose subject explains the movement, not because they are expensive. An item must
  never be deferred *within* it for cost.
- **Re-recording is part of doing the work**, like running the suite. The three golden baselines and the
  two Ruleset content hashes are named in the failing test's own message; there is no separate expertise
  to acquire and no approval to seek.
- **The classification rule keeps its whole job.** *Optimisation versus design change* still decides
  whether something may be tuned freely and still catches a behaviour change wearing a tidy-up's
  clothes. This ADR removes an inference nobody licensed, not the rule.
- **Vocabulary and representation changes get radically cheaper, and should be taken eagerly.** Renames,
  column splits, enum reshapes and table reorganisations are exactly the class that hash-fear was
  suppressing, and they are exactly the class whose cost grows fastest with corpus size.

## What would trigger revisiting

- **The first save carried across a version by anybody outside this repository.** That is the event that
  creates the cost this ADR says does not exist. A playtest build that persists a city is the trigger,
  and it arrives before the renderer does.
- **Re-recording stops being one command.** If a baseline ever needs hand-editing, a judgement call, or
  more than the published invocation, the chore has become a cost and this should be re-argued.
- **A golden artefact acquires a reader outside the tree** — a published figure, a shared trace, a
  benchmark somebody quotes — since then a re-record silently invalidates something no test covers.
- **`adr/0015`'s hot-reload contract acquiring a save-compatibility clause.** Today a Ruleset reload is
  bounded by a running process; the moment a reload has to interpret an older world, this ADR's premise
  is gone.
