# An unbuilt mechanism is not a design constraint

**Before any decision is taken on the ground that the simulation does not do something, name the
mechanism that would do it and classify the absence: *unbuilt* (specified, no builder), *undesigned* (no
specification), or *refused* (a decision says no). **Only *refused* is evidence.** The other two are the
expected state of a young project and must not generate a design position, a compensating behaviour, or
a number.**
`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `HONEST DEGRADATION`

This is the third of three sibling rules and the one that governs **absences**.
[`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) governs
**claims** — *can you name the number that would refute this?*
[`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) governs
**numbers** — *can you name the thing that would ratify it?* This one governs the third thing a sitting
reasons from: *this does not happen*. The test is **name the mechanism and say which of the three it
is.**

## Why

**The base rate makes it near-certain to be wrong here.** Phase 1's code is a Tick, typed tables, Map
Layers and two Rule families. There are no jobs, no money, no movement, no roads, no renderer, no
prices, no bid, no choice model, no immigration and no Departure. `06`'s own *Mechanisms with no
milestone* table lists **seventeen** settled decisions that appear nowhere in the plan. **So when the
simulation does not do something, the overwhelmingly likely reason is that nobody has built it** — and a
rule of inference whose premise is usually false is not a weak rule, it is an inverted one.

**Session N found it three times in one day, and the third instance produced a whole fork that did not
exist.** The sitting asked, at length, whether construction should fill a new Building to its capacity.
That question exists **only** because `02 §5.2`'s Household placement is unbuilt: with placement, a
Building fills over the following Days and there is nothing to decide. **An absence generated a design
position, and the position was about compensating for the absence.** The full table is in
[`plans/0012`](../../plans/0012-corpus-audit.md) → *Cause 1*.

**It runs in both directions, which is what makes it a rule rather than a caution.** The same session
found [`adr/0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)
asserting the loader refused nothing of a kind it had refused since slice 7 — *an ADR wrong about the
code* — and then found *the code allowed to be wrong about the design*. Both are one fact with no copy
being re-derived from the shape of its absence. The corrective is symmetric and cheap: **before
recording that something does not exist, name the file you looked in.**

**The design documents are not descriptions of the build and must not be read as such.** `02 §5.2`
specifies a six-step loop and one step is built. `02 §2.2` says Lots are *"generated, not painted"* and
every Lot this project has ever run was painted. Nothing is wrong with either sentence; what was missing
is the marker. A reader without it does what two ledger entries did — take the document for the build,
find the build lacking, and conclude something about the **design**.

**The pattern already exists in the code and this extends it to prose.** Slice 6 shipped **named holes
that throw**: `RuleEngine`'s `pool` scope does not return an empty Bin, it raises with a sentence saying
the District Pool does not exist. That is exactly this rule, implemented — an absence that **announces
itself** rather than degrading into a plausible answer. The equivalent in a document is a sentence
saying which parts exist, and the equivalent in a sitting is this classification.

**Why *refused* is the only evidential category.** A refusal is a decision with an ADR, a reason and a
revisit trigger, so reasoning from it is reasoning from the corpus. *Unbuilt* is a statement about the
schedule and *undesigned* is a statement about attention, and neither carries information about what the
city should do. **Conflating them is how a scheduling accident acquires the authority of a decision.**

## Consequences

- **A sitting that finds itself asking *given X does not exist, should Y compensate?* stops and
  classifies X.** If X is unbuilt or undesigned, the question is void and the output is *build X* or
  *design X* — not a compensating behaviour in Y. This is the check that would have saved session N task
  2 its first three exchanges.
- **Absences route by class.** *Undesigned* → [`0002`](../../plans/0002-open-questions.md) §E, owed work.
  *Unbuilt with a specification* → [`0003`](../../plans/0003-build-plan.md), which is where session N
  task 2's placement pass went. *Refused* → an ADR, which it already has.
- **A design document that could be mistaken for a description of the build says which parts exist.**
  Three now do: `rulesets/minimal.toml`'s header, `02 §5.2`, and `02 §5.7`'s measured note. `02 §2.2` is
  owed one.
- **This does not license ignoring the build.** A measurement taken on the build is evidence about the
  build and often about the design too — `0011`'s findings 40–43, `adr/0044`'s cadence, `adr/0063`'s
  defect in the committed baseline. **What is barred is inferring from what the build does *not* do**,
  which is the one thing an incomplete system cannot testify to.
- **It sharpens `adr/0042`.** A planning document cites and a design document owns; this adds that
  **neither describes the build**, and that the artefact which does is the code and its tests.

## What would trigger revisiting

- **The build ceasing to be materially incomplete.** When most specified mechanisms exist, the base rate
  inverts and an absence starts carrying information again. That is somewhere past Phase 2, and the
  honest signal is `06`'s no-milestone table emptying rather than a date.
- **The rule being used to wave away a real constraint.** If *"that is just unbuilt"* starts dismissing
  absences that are actually refusals — or unbuilt for a reason nobody re-examined — the classification
  has become a formality, and the fix is to require the ADR or the ledger row by name rather than the
  word.
- **A decision that genuinely must be taken before its mechanism exists.** These exist and the rule does
  not forbid them: `adr/0054` drained the Pool blind because acceptance was unbuildable, and said so.
  The rule is satisfied by **naming the absence and the reason**, not by waiting. If that becomes the
  common case rather than the exception, the classification is doing no work.
