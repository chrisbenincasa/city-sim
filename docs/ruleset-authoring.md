# Authoring a gameplay Ruleset

The current TOML exposes the simulation's execution definitions. The workflow below helps a
developer investigate them; **it has not been validated as a human's whole-game authoring
workflow**. The [viability investigation](../plans/0075-base-game-ruleset-viability.md) recommends
reusable domain definitions and explicit relationships, followed by an independent author handoff.
A compact file, successful run or schema completion does not establish
that its dependencies and implicit behaviour are manageable by hand.

The first requirements are bounded upfront specification, manageable growth in content and a
supported evolution path once cities are saved. Readability and mechanical editing cost matter,
but an editor or DSL cannot establish those properties by itself. Determine which behaviours
are independently authored, shared, derived or handled by a general mechanism, then assess how
Goods, Building kinds, recipes and variants combine. Measure expanded definitions as well as
what a person types. Preserve deliberate design choices and explicit exceptions.

Saved cities currently embed their TOML. Changing today's file therefore does not by itself
invalidate their content hash; they still load their pinned content on a compatible build.
Upgrading those cities is a separate compatibility task. The authoring model needs a policy for
stable identity, supported changes and migrations; hashes do not require designing all content
before the first save. See [0075's save boundary](../plans/0075-base-game-ruleset-viability.md#3-live-saves-and-content-evolution).

## Work from the circuit

For each Good, trace its source, seller, payment, transport, buyer's Bin and consumption.
For each employer, trace income, jobs, wage, pay period and failure. For a public service,
add treasury income, grant, staffing, places and attendance. List opening capital separately
from recurring income. A positive treasury or a loaded file does not establish viability.

The current Pool purchase selects a local Business seller. A Hinterland price anchors the
market; it does not create an import supplier. Empty-input production is an explicit source,
not a purchase. Rules can convert Goods and emit pollution, but employing Citizens does not
by itself make that production depend on their work. These distinctions need mechanisms or
an explicit founding scenario decision before they become claims in playable content.

## Keep coupled values visible

| Authored intent | Values to check together |
|---|---|
| Consumption per Day | Rule input amount, `apply`, `rate` (Ticks), Bin capacity; shopping derives daily use from the net local consumption Rules |
| Days of shopping stock | Consumption above, `low_days`, `target_days`, carry/storage capacity and shop opening times |
| Production throughput | Input/output amounts, `apply`, `rate`, available inputs and output storage; fixed-count production has no automatic staffing factor |
| Paid public jobs | Trade `wage_per_day`, `pay_period_days`, Policy `interval`, grant `amount`, `apply = { derived = "jobs" }` and wage tier/experience settings if enabled |
| Building capacity | Geometry and occupied floor share, `floor_tiles_per_occupant`, `floor_tiles_per_job`, `floor_tiles_per_place`; service places also depend on staffing |
| A Good in a District market | Resource, ownership of declared Bins, supply/consumption Rules, reachable seller and Hinterland price declarations |
| Population turnover | Life Stage successors and children, housing space, Unplaced give-up duration, and the arrival/Outside contract owned by row 31 |

`rate = 256`, consumption `4`, fixed apply `1` gives nominal use of 32 per Day at
2,048 Ticks/Day. A three-Day shopping target is therefore 96; a smaller Bin prevents
holding that target. This arithmetic is a starting estimate: blocking, Trips and availability
still determine the delivered result. Time acceleration belongs to the host.

A grant per declared job and a wage per worker are different policies. Keep both controls:
a designer may deliberately fund vacancies or underfund wages. Doubling pay is one edit;
maintaining the same grant coverage is a second edit when both cadences are one Day.
Do not add a loader rule requiring their equality. Show their relation in the authoring
report, including pay-period cash needs, rather than hiding it in duplicated comments.

## Iterate and retain the evidence

1. Read the relevant descriptions in [the generated reference](ruleset-reference.md), then
   run the actual loader. The editor schema provides completion and types but deliberately
   permits unknown keys. The loader checks references, retired keys and several cross-table
   constraints. It does not prove a supply chain, solvency or adequate service coverage.
2. Use a fixed seed, command sequence and population. Report purchases/deliveries, payroll
   and shortfall, treasury flows and residual, Need levels and service capacity/attendance.
   Preserve flow sums across every Tick; do not sum a repeated last-event reading.
3. Change one intended balance relation. Retain the control and a precise semantic diff.
   The viability instrument generates variants from one file; it does not maintain copied
   Rulesets. Rerun past the relevant paydays, Life Stages and buffer exhaustion.
4. For an in-session change, register both Rulesets in the catalogue and record the transition
   through Formats' Input Log codec. Verify that the new values actually took effect. The
   headless session runner supports this; specialised dumps must not be assumed to consume
   `--reload-at`. The shell's current tuner **regenerates** the city. A governed Policy override
   survives reload, so inspect its effective value, not just the new default.
5. Run end-of-run invariants and replay/save continuation checks. Watch the shell's trigger,
   response and consequence. Separate tested duration/seed from steady-state or general
   balance claims. Existing Goods-conservation and exclusive-location invariants are still
   unimplemented, so an invariant pass does not certify those properties.

Use a fresh run for changes to frozen world geometry/layer contracts. For supported reloads,
retain every exact Ruleset used: comments and whitespace affect the content hash. Read
[the golden procedure](../tests/Borough.Tests/Golden/README.md) before changing golden inputs.

## Recommended author-facing model

A candidate surface should let an author:

- Define a Good's consumption, recipes, owners and supply paths together, and see missing links.
- Express quantities with their units and intentional relations, such as four Days of storage,
  while inspecting the integer capacities and firing schedules they expand into.
- Share a consumption or service definition across Building kinds, with explicit local overrides.
- Distinguish design choices from derived consequences: grant coverage need not equal payroll,
  but its relationship to posts, workers and pay periods should be visible before a run.
- See which actor executes each Rule and which mechanism takes over its behaviour.
- Inspect the resulting dependency graph and connected economic report using content names,
  with errors pointing back to the authoring source.

These are requirements for the authoring prototype, not implemented product syntax. The bounded
[research expander](../plans/evidence/ruleset-viability/content-study.py) demonstrates shared
maintenance but retains the expanded Rule count; it is not a supported content format. Expansion must preserve deterministic
identity/order and produce a Ruleset accepted by the existing loader. Its output must remain
inspectable; hiding contradictory assumptions behind a generator would not solve the problem.

Extend the bounded prototype through 0075's independent author handoff, including growth,
maintenance, diagnosis and saved-city evolution. Track expanded Rules and explicit exceptions
as well as authored choices. TOML with reusable domain definitions, a DSL and a
structured editor are candidates. Includes alone reduce repeated text but do not preserve
relationships, explain implicit ownership or make balance legible.

Do not require all simulation mechanisms before testing authoring, and do not use their absence
to dismiss the authoring problem. Runtime capability and human assembly cost need separate
verdicts. Keep fixture sizing in the instrument under
[ADR 0164](adr/0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md).
