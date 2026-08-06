# A planning document cites, and a design document owns

**A planning document may assert only project facts — order, gates, risks, status, and what is owed to whom. Every claim it makes about what the simulation *does* is a citation to the document that owns it, never a restatement.** Planning documents are [`06-roadmap.md`](../06-roadmap.md), [`plans/0000-board.md`](../../plans/0000-board.md), [`plans/0003-build-plan.md`](../../plans/0003-build-plan.md) and the per-slice plans. Design documents are [`CONTEXT.md`](../../CONTEXT.md), [`00`](../00-vision.md)–[`05`](../05-technical-architecture.md), and the ADRs.

`SOLVE THE ACTUAL PROBLEM` `HONEST DEGRADATION`

## Why

**Because the alternative was measured, and it produced eleven false claims in one document.** `06-roadmap.md` was never grilled and drifted for eight sessions. A sweep against the corpus found that essentially none of its prose was original — the Simulator Effect argument restated [`00-vision`](../00-vision.md), the Evidence triple restated [`02 §9`](../02-simulation-model.md), the Citybound yak-shave restated [`adr/0018`](0018-prefer-off-the-shelf-infrastructure.md), the Cities: Skylines 2 renderer story restated [`05 §11`](../05-technical-architecture.md) *and* [`00-vision`](../00-vision.md) — and that every one of its false claims was a copy whose original had moved:

| The copy | The original that moved |
|---|---|
| milestone 3a's `rate` | [`adr/0033`](0033-two-rule-families-scheduled-and-swept.md) replaced polling with subscription |
| 5c's *"travel time `distance / speed`"* | `CONTEXT` → Volume-Delay Function made BPR the mechanism behind every Statistical Segment |
| *"invariant assertions run in debug builds"* | [`02 §10`](../02-simulation-model.md) called that backwards and sorted them by frequency instead |
| *"Public transit. Not in scope, and possibly never."* | [`adr/0029`](0029-transit-is-in-and-right-of-way-is-the-only-axis.md) — *"Transit ships."* |
| *"revisit `adr/0001`"* on the core's language | [`adr/0036`](0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) took the language out of `0001` |
| milestone 8's parking-as-Rules | [`adr/0009`](0009-parking-is-modelled-supply-never-search.md)'s own superseding note |

**Nobody was careless.** Each of those originals moved for a good reason, in a session that did its job properly, and in every case the author updated the document they were arguing. What no session does — what no session *can* reliably do — is walk every other document looking for uncited paraphrases of the claim just changed. A copy with no owner is a copy nobody updates.

**The rule makes the failure impossible rather than unlikely.** A row that reads *"5c — Statistical resolution; mechanism in [`03 §5`]"* cannot go stale, because it asserts nothing that can be false. This is the same move the project already made with [`adr/0003`](0003-deterministic-integer-simulation.md)'s per-field declaration: not *remember to hash every field*, but *declaring the field is what allocates it*, so the coverage hole is structurally unavailable. Discipline that depends on remembering is discipline that fails at the eighth session.

**It also gives the re-derivation an acceptance test.** `06`'s Phase 2 must be re-derived once the designs behind it are argued. Under this rule that job is finished when every row is a name, a risk and a citation — a checkable condition, rather than a judgement about whether the prose is current.

## What this does not bind

**Design documents may restate each other, and should.** [`00-vision`](../00-vision.md) and [`05 §11`](../05-technical-architecture.md) both tell the Cities: Skylines 2 story, and that is not a defect: a design document restates *in order to argue*, and the argument is the point — the same evidence supports a claim about art direction in one place and a claim about renderer architecture in the other. Stripping either to a cross-reference would remove the reasoning that makes it persuasive.

The distinction is **what the document is for**. A design document exists to make an argument, and an argument that cannot be followed without three other tabs open is a bad argument. A planning document exists to say what to do next and in what order, and it needs no argument about mechanism to do that — it needs a pointer.

Extending the rule to design documents was considered and rejected. It would put a great deal of good prose in violation on the day it was written, and a rule that starts life with fifty exceptions is a rule nobody follows.

## Consequences

- **`06-roadmap.md` loses its contents column.** A milestone row becomes a name and the risk it retires. This removed ten of the eleven false claims outright rather than correcting them.
- **`06`'s warrant narrows** to the phase model, its four pacing and review rules, and the risk field. Phase 0/1 order goes to [`0003`](../../plans/0003-build-plan.md), live status to [`the board`](../../plans/0000-board.md), mechanism to the design documents.
- **Deletion is the default remedy for a stale claim in a planning document**, not correction. Correcting it re-creates the copy.
- **An ADR that assigns work to a planning document must say so explicitly**, because the planning document can no longer infer it from a mechanism description. [`adr/0029`](0029-transit-is-in-and-right-of-way-is-the-only-axis.md)'s *"dwell time is roadmap work"* is the model, and `06` now carries a section listing such instructions as debts with named creditors — which is checkable, where a survey is only as good as the day it was taken.
- **Planning documents get shorter and less readable as narrative.** This is a real cost and it was accepted knowingly: `06` is no longer the one place to read about where the project is going in continuous prose. The judgement is that a readable document which is wrong in eleven places is worth less than a thin one which cannot be.
- **Cross-references become load-bearing**, so a broken one is now a defect rather than an inconvenience. `06` cited [`deferred.md`](../deferred.md) for a transit entry that `adr/0029` had removed, and nothing caught it.

## What would trigger revisiting

- **If citation density makes a planning document unusable in practice** — specifically, if a cold start requires opening more than two or three other documents to understand what the next slice is. The test is [`CLAUDE.md`](../../CLAUDE.md)'s cold-start path: board → build plan → slice plan. If that path stops working without a fourth and fifth tab, the rule is costing more than the drift it prevents.
- **If a mechanism turns out to have no design-document home**, so the citation has nowhere to point. The correct response is to write the missing design, not to inline it into the plan — but if that happens repeatedly, the document boundaries are wrong and this ADR is treating a symptom.
- **If the rule is observed to be routinely violated** in new planning documents. That would mean it is fighting how the work actually gets written, and a rule nobody follows is worse than no rule, because it makes the corpus look checked when it is not.
