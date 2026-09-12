# 0075 — Base-game Ruleset viability

## Verdict

**Do not make hand-maintained, expanded TOML the whole-game authoring model.** The runtime
representation can execute a connected slice, but it makes authors repeat shared behaviour,
maintain derived quantities manually and understand implicit mechanism interactions. Changing
syntax alone does not address these costs.

Incremental game creation is technically possible: not every mechanism needs advance definition,
content dimensions do not all multiply, and existing saves can retain their original content.
The recommended direction is reusable domain definitions with explicit relationships and
exceptions, expanded deterministically into the current runtime representation initially.
Retain TOML as an inspectable output while testing that model; this is not a decision to invent
a DSL. Separately establish stable content identity and supported saved-city transitions.

This is an engineering verdict with a bounded prototype, **not a demonstrated human usability
result or a complete founding game**. No independent author handoff or learning-time measurement
was performed. The prerequisite for committing to the authoring surface is a representative
handoff and maintenance exercise, with expanded size and migration effects visible.

Investigation: `ruleset-viability`, base `c38f081`, 2026-09-12. Row 31 remains separately owned;
its work was neither imported nor changed. The numbered plan replaces the historical descriptive
filename; captured fixture comments retain their original bytes because they affect identity.

## 1. Upfront burden

The current corpus contains 49 demonstration files, with 171 Bin Rule declarations in total
and at most seven in any one file. These are overlapping demonstrations, not 171 independent
Rules that must all be assembled into the base game. They also omit planned gameplay.
[The census](evidence/ruleset-viability/results/content-census.json) is a count of today's files,
not an estimate of the finished game's size.

The connected slice has three Resources, three Building kinds, two trades, five Bin Rules,
two Zone Rules, one Policy and five Life Stages. Removing the unrelated repairs Good/upkeep,
layers, parking and market settings gives a smaller slice: **two Resources, four Bin Rules,
three kinds, two trades, two Zone Rules, one Policy and five Life Stages**. It still has 198
scalar fields and 21 explicit `resource`/`kind`/`business` references. Scalar fields are not
independent decisions: repeated names, fixed machinery and derived capacities inflate that count.

The smaller file loads and its 28-Day school run records 85 attendance occasions, 47 delivered
and 38 without a school in reach; the school ends with 32 places. This establishes an exercised
service path after those omissions, not equal balance or a complete economy. The connected
slice's purchases, wages, treasury and shortage runs are retained in the
[runtime study](evidence/ruleset-viability/runtime-study.md).

| Authoring cluster | Choices needed for this slice | What can remain outside the initial scope |
|---|---|---|
| Goods and use | One consumed Good, Money, who holds them, source and sink, consumption and storage cover | Other Goods and production chains; repairs removed in the smaller slice |
| Settlement/access | Housing and shop kinds, floor allocation, Streets/Lots, reachable jobs and shopping | Additional kinds; parking and terrain layers removed in the smaller slice |
| Employment/payment | Wages, pay period, shift hours, initial balances, prices | Additional trades and fiscal mechanisms |
| Public service | Teaching trade, school, staffing/place relation, grant and treasury | The service cluster can be introduced later; it is not required by every Ruleset |
| Population/Needs | Turnover, Needs, shopping thresholds, a sink for internally generated Unplaced Households | Additional Life Stages and Needs; omit whole mechanisms coherently |

Eighteen one-table omission probes clarify dependencies. Removing layers, lots, parking, jobs,
households, lattice, districts, market, shopping, income tax, treasury or Life Stages is accepted.
Removing roads, capacity, Trips, placement, Hinterland or Needs from this otherwise unchanged
slice is rejected. Acceptance does not establish equivalent behaviour: removing jobs disables
assignment; removing shopping changes how replenishment runs. Defaults must be packaged as a
coherent starting scenario, not selected by deleting arbitrary accepted tables.

A complete founding minimum cannot yet be demonstrated: paid Outside supply, labour-dependent
production and founding/arrival flow remain gameplay work. Controlled empty-input supply and
Populate are explicitly fixtures. Their absence does not justify requiring every future system
upfront, nor does this small exercise prove that the current 198-field interface is manageable.

## 2. Content growth and reusable intent

The controlled growth family has G Goods with one producer each, C consumer kinds, V economic
variants per consumer and S Life Stage declarations. Every consumer uses every Good. Each
consumer/Good relationship gets a replenishment and consumption Rule; each producer gets one
Rule. The real loader accepts every generated size below.

| C | G | V | S | Expanded kinds | Expanded Bin Rules | Explicit references¹ |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 1 | 1 | 1 | 2 | 3 | 12 |
| 2 | 1 | 1 | 1 | 3 | 5 | 18 |
| 4 | 1 | 1 | 1 | 5 | 9 | 30 |
| 4 | 2 | 1 | 1 | 6 | 18 | 60 |
| 4 | 4 | 1 | 1 | 8 | 36 | 120 |
| 4 | 2 | 2 | 1 | 10 | 34 | 108 |
| 4 | 2 | 4 | 1 | 18 | 66 | 204 |
| 4 | 2 | 1 | 4 | 6 | 18 | 60 |
| 4 | 2 | 1 | 8 | 6 | 18 | 60 |

¹ Occurrences of `resource`, `kind` and `business` reference fields; not unique edges or all
possible semantic relationships. [Raw counts and edit diff](evidence/ruleset-viability/results/content-growth.json).

Here the Rule count is **G + 2CGV**. This is a demonstrated product, not evidence of exponential
growth across every system. Life Stages add declarations, not Bin Rules. Recipes need only their
actual input/output edges, not every possible pair of Goods. Cosmetic variants need not become
new economic kinds. Conversely, the storage-only economic variants in this probe copy the
entire shared consumption behaviour because a Rule attaches to one kind. That repetition is
avoidable authoring work even when separate runtime instances are necessary.

A bounded [intent model](evidence/ruleset-viability/content-model.json) describes two Goods,
two consumer kinds and one Produce → Food conversion. One edit, Food daily use 32 → 48,
regenerates eight scalar changes: two storage capacities, two replenishment inputs, two
replenishment outputs and two consumption inputs. Both outputs load. The prototype retains
four-Day storage as a relationship and rejects consumption not exactly representable at its
cadence. It demonstrates consistent mechanical expansion, not balanced supply or user usability.
Both Goods are consumed in this synthetic basket; that is an experiment, not the game's Food design.

The prototype keeps ten expanded Rules. It therefore proves less duplicated maintenance, **not
less runtime work**. Its generator also contains shared defaults and conventions that must become
inspectable domain definitions before it could be an authoring product. Actual exceptions must
remain explicit. No generator should silently emit every combination just because it can.

The raw connected-file maintenance task needs four coordinated edits to raise use by 50%, keep
four Days of home storage and scale shop throughput/storage. The incomplete one-edit version
loads despite losing that intent. With shopping enabled, its replenishment Rule is interpreted
by the shopping mechanism rather than directly by its visible transfer amounts. The
[exercise](evidence/ruleset-viability/authoring-exercise.py) demonstrates why an intent preview
and explanation of the actor/mechanism matter more than syntax completion.

Wage and grant remain separate choices: doubling the teacher wage without funding bankrupts
the school in the retained 56-Day experiment; doubling its grant preserves its capacity there.
Do not enforce equality—the author may deliberately fund vacancies or underfund wages. Show
coverage and pay-period cash requirements together.

## 3. Live saves and content evolution

Content hashes identify exact bytes, including comments. They do not require freezing the game's
design. `CitySave` embeds TOML and verifies it against the saved header; loading a city uses that
content, not today's file. Exact replay additionally requires compatible simulation code and all
content referenced by its history. Continuing under new content is a separate, explicit transition;
its future State Hash is allowed to change.

The probe uses the actual `CitySave` implementation, an inhabited 1,000-capacity city with its
school placed, and a save at Tick 2,304. Each case loads the original package, establishes the
opening Ruleset, then requests the changed content through `TickInput`. It checks actual adoption,
end-of-run invariants, upgraded save/load equality and a 256-Tick continuation against the
unsaved upgraded world. This is a short persistence check, not a balance horizon.

| Change | Observed result |
|---|---|
| Comment; teacher wage; shared consumption | Adopted; no structural degradation; upgraded save/continuation passes |
| Add Good and conversion Rule | Adopted; 777 Rule Instances rearmed; upgraded save/continuation passes |
| Add housing kind; add Policy | Adopted; 774 Rule Instances rearmed; upgraded save/continuation passes |
| Rename occupied school; remove school² | Adopted, but one Building becomes derelict; upgraded save/continuation passes |
| Rename sundries everywhere | Adopted; 774 instances rearmed, reported zero dropped Bins; continuation passes, but this does **not** establish semantic identity preservation |
| Change founding treasury balance | Refused; State Hash and active content remain unchanged |

² Also removes the now-inapplicable education/place settings so the candidate itself is valid.
[Full loader and evolution results](evidence/ruleset-viability/results/content-probe.txt).
The original package remains unchanged throughout and its own 256-Tick continuation matches the
original world. Earlier command replay and longer save checks are in the runtime study.

Migration keys are name-derived. The school rename demonstrates that a spelling change is not
an identity-preserving alias. Resource migration additionally needs scrutiny across owners:
`World.Migrate` remaps Building Bins, then refits Household and Business Bins. A zero dropped-Bin
count and a matching save continuation do not prove that a renamed Good preserved the intended
stock identity. Do not advertise arbitrary rename/reorder/delete support from this probe.

Binary/table layout changes and future parser changes remain a separate compatibility boundary.
Pinned content needs continued support; upgrades need declared identity mapping, preservation or
explicit conversion of stock/occupants, and a preview of losses. Hash changes are legitimate;
requiring all content to be final before the first save is not a substitute for those contracts.

## 4. Recommended implementation and acceptance

1. **Author shared behaviours and relations.** Prototype named consumption baskets, recipes,
   service definitions and explicit kind attachments/overrides. Express units and storage/funding
   relations once. Keep stable deterministic expansion and source-to-output diagnostics. Start
   with the existing runtime representation; revisit runtime factoring if representative expanded
   counts or Rule Instances are unacceptable, with measurements rather than file length.
2. **Offer a coherent small starting scenario.** Expose initial choices by gameplay cluster and
   show dependencies when adding a mechanism. Keep economic source/sink and initial-funds choices
   visible. Do not require the whole simulation catalogue or disguise missing mechanisms as defaults.
3. **Make evolution explicit.** Preserve pinned content; distinguish loading from upgrading.
   Establish stable identities/aliases and supported transitions, including all Bin owners,
   occupied content and governed Policies. Preview destructive changes and verify old and new
   continuations independently. Coordinate with save compatibility rather than changing hashing.
4. **Validate with an independent author.** Have them add Food/recipe, a consumer kind, a storage
   variant and a service, then change shared consumption and funding and upgrade an inhabited save.
   Record independent choices, edits, mistakes, dependencies discovered and expanded counts.
   Require diagnosis through the authoring surface rather than C# inspection. Choose the final
   TOML/DSL/editor representation after this exercise; the JSON research model is not a product API.

The investigation is complete at this bounded engineering scope. Authoring implementation and
handoff belong to the authoring board item; release transitions to save compatibility; the actual
founding loop to first playable. None is claimed implemented by this study.

## Reproduction

```sh
python3 plans/evidence/ruleset-viability/content-study.py
dotnet build plans/evidence/ruleset-viability/Viability.csproj -m:1 -nr:false -p:NuGetAudit=false
dotnet plans/evidence/ruleset-viability/bin/Debug/net10.0/Viability.dll plans/evidence/ruleset-viability --content
dotnet src/Borough.Headless/bin/Debug/net10.0/Borough.Headless.dll --school \
  --ruleset plans/evidence/ruleset-viability/study-generated/lean.toml --citizens 1000 --ticks 57344 --schools 1
```

Generated TOML is disposable and recreated from the retained script/model; save packages are
written under `/tmp/borough-content-viability`. No production schema, simulation or golden fixture
changed. The research build passes with no warnings. Full runtime captures and driven observation
remain in the linked runtime study; no new visual capability was introduced here.

The working lane passed **3,399 tests**, zero failures/skips, with
`scripts/test.sh -- --no-restore -m:1 -nr:false`; log:
`/tmp/borough-test-20260912-163839.log`. All 12 non-omission generated inputs load;
nine content transitions pass save/continuation checks and the founding-balance refusal
preserves the old state. The full instrument suite was not run; this is not a playable milestone.
