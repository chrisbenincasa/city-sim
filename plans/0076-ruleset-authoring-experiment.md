# 0076 — Ruleset authoring under demanding maintenance

## Outcome and boundary

Try to reject one concrete authoring model cheaply: reusable consumption baskets, recipes and
storage relations, attached to Building kinds with explicit overrides. Follow
[0075](0075-base-game-ruleset-viability.md); do not infer usability from a small valid file.
Owner: `ruleset-viability`. This tree owns the experiment; row 31 remains untouched.

Use an isolated disposable compiler in `plans/evidence/ruleset-authoring/`. TOML is the initial
source representation, not a commitment to a language. No production schema/Core changes,
editor UI, mod packaging, general migration framework or whole-game balance work.

## 1. Agent implementation

1. Define a strict, shallow model: Goods, named baskets, recipes, consumer kinds, shared storage
   variants and explicit per-kind/variant overrides. Stable ids are separate from display labels.
   Avoid arbitrary inheritance and executable expressions; reject unknown keys/references.
2. Generate a synthetic catalogue of 20 consumer kinds, eight Goods and three storage variants,
   with different baskets and deliberate exceptions. Generate repetitive fixture declarations
   once; subsequent authoring edits happen in the TOML. Keep the baseline immutable for comparison.
3. Compile deterministically to existing runtime TOML. Produce a source-attributed explanation of
   effective consumption, storage, inheritance and exceptions, plus expanded Rules/references.
   A before/after report must identify affected and unchanged kinds and flag retirement/identity
   changes before anyone applies them to a real city.
4. Exercise shared consumption, one variant, a selective Good/recipe addition, removing an
   override, and tuning/renaming/retiring content against an inhabited save. Use real Formats and
   CitySave. Label static expectations separately from actual migration observations. A display
   label rename should preserve identity; an id change must not pretend to be a safe rename.
5. Check locality and determinism, exception preservation, strict diagnostics, real-loader
   acceptance and applicable save continuations. Record limits and provide concise commands and
   task descriptions for the handoff. Runtime expansion remains measured rather than hidden.

## 2. User handoff

The user independently attempts two or three written maintenance tasks using the source and
reports. The agent observes questions, errors and unexpected dependencies without silently
performing the edits. Do not mark the experiment complete before this observation and discussion.
Do not treat agent task timings as human usability evidence.

## 3. Failure conditions and decision

Reject or revise the model if ordinary changes require editing generated Rules, unrelated
exceptions proliferate, understanding an edit requires C# inspection, or save consequences
cannot be explained as preservation, conversion, refusal or explicit loss. A generator that
hides an excessive expanded product has not passed merely by reducing source edits.

For each task record authored choices/edits, affected definitions, preserved exceptions,
expanded counts, diagnosis and persistence results. After the handoff choose: extend this model,
revise a specific weakness, or reject it. Passing earns the next bounded step, not a full-game
architecture commitment.

## Progress

The user completed the three maintenance edits and reported that it was generally smooth,
except that requiring an empty `overrides` field was surprising. Review confirmed the intended
changes; the only compiler refusal was the omitted field. Accept omission as inheritance.
No timing or observation of report usage was captured. This supports these maintenance tasks,
not full-game scalability. The user's `catalogue.toml` is preserved as authored.

The follow-up tested these agreed questions:

1. Can shared behaviour and per-instance storage choices avoid a kind × variant product while
   preserving differences and explicit exceptions? Build a factored representation, compare its
   resolved values to the existing expansion at demanding sizes, and identify the exact Core/save
   changes it requires. Do not present a representation experiment as a completed engine migration.
2. Can a bakery express inputs, work, production, storage and sales with a small set of independent
   declarations, then vary staffing and storage without copying its behaviour? Exercise maintenance
   and interruption cases, and test actual Core capabilities. State unsupported semantics explicitly;
   never lower present workers to declared posts or relabel free production as paid supply.

Use finite typed relationships, explicit owners/units and sparse overrides. No arbitrary expression
language, whole-economy balance claim or production framework. The outcome is an evidence-backed
answer and an exact next implementation boundary, including any failed hypothesis.

| Task | Observed static effect |
|---|---|
| Basic Food use 32 → 48 | 30 variants change; 30 retain their different basket; storage Days/exceptions retained |
| Reserve storage 6 → 10 Days | 17 variants change; three kind-specific choices preserved |
| Add Spice/recipe to varied basket | 30 consumer variants change; basic basket remains untouched |
| Remove home_00 reserve override | One variant returns to inherited storage |
| Rename display label | Authoring metadata changes; identical runtime bytes |
| Retire/change home_00 id | Explicit removal/addition, not an alias; occupied Buildings become derelict |

[Checks](evidence/ruleset-authoring/checks.json) retain deterministic expansion, locality and six
source-error diagnostics. [Persistence observations](evidence/ruleset-authoring/persistence.txt)
use a 360-Household city with four housing kinds and eight producer kinds standing, then real
CitySave, reload, invariants and 256-Tick upgraded/old continuations. These are constructed fixture
and persistence checks, not balance or general semantic migration guarantees. Storage variants
and the alternate basket are deliberately inhabited so their edits affect live state.

**A concrete weakness already found:** 20/40/80 authored consumer kinds with three variants and
eight producers expand to 68/128/248 runtime kinds and 368/728/1,448 Rules. At 83 consumer kinds,
257 runtime kinds exceed the real loader's 254-kind limit. Reports expose the budget; this
expansion cannot be adopted as a scalable production model unchanged. The handoff supports these author-facing relationships; the follow-up below tests factoring
the runtime expansion separately.
No kind-width/schema change was made to make the experiment pass.

The research build passes without warnings. The working lane passed **3,399 tests**, no
failures/skips: `scripts/test.sh -- --no-restore -m:1 -nr:false`, log
`/tmp/borough-test-20260912-235542.log`. No production simulation, shell or golden fixture changed;
the full instrument suite is not required for this non-playable experiment.


## Factoring and bakery decision

The two follow-up questions now have bounded engineering answers in
[scaling-and-bakery.md](evidence/ruleset-authoring/scaling-and-bakery.md). Retain the shallow
source model; revise the runtime expansion. Separate saved instance selections and shared
behaviour remove the demonstrated kind/profile definition product. A bakery's richer authoring
remains local in the contract probe, but current Core produces with zero workers and lacks the
required work-dependent execution connection. Its existing paid local-market purchase should
be reused, not replaced by fixture supply.

Representation and isolated-contract checks are not production Core/save support. The next
implementation boundaries and acceptance are explicit in that report; no general language,
production schema or Core behaviour was changed here. The omission/default issue is fixed in
the compiler without editing the user's catalogue. The investigation is complete at this scope;
implementation belongs to the authoring/runtime and private-production board items.

Follow-up verification: research build and representation/contract/native probes pass. The
working lane passed **3,399 tests**, zero failures/skips;
`/tmp/borough-test-20260913-105449.log`. The native persistence fixture now uses each Building’s
automatically instantiated trade, without creating a second unfitted Business.
