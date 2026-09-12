# Authoring a gameplay Ruleset

Use TOML with the existing loader, generated schema and key reference. Keep the first
playable Ruleset in one file, organised by Resources, trades, Building kinds, Rules,
Zone Rules, Policies and shared parameters. Give each authored value its unit and each
intentional source, sink or fixture shortcut a short explanation. The
[viability investigation](../plans/base-game-ruleset-viability.md) contains the evidence
and the founding prerequisites; its experimental Ruleset is not base-game content.

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

## Scale only when needed

Do not compose gameplay by copying entire demonstration Rulesets. Their different failure
conditions are intentional, and their commentary can describe historical behaviour. The
loader and tests establish what runs now.

One compact gameplay file needs no new language. If several maintained gameplay variants
make repeated sections costly, first consider a small deterministic TOML composition step:
named shared fragments and explicit overrides, duplicate-name refusal, stable declaration
order, flattened output and source locations in diagnostics. Keep that step outside Core and
hash the runtime TOML through Formats. This is a proposal, not existing include syntax or a
new implementation prerequisite.

Revisit a DSL when measured content work shows that TOML's syntax or missing expressions
obstruct iteration after these simpler measures. A DSL cannot supply missing trade, labour or
founding mechanisms. Keep fixture sizing in the instrument under
[ADR 0164](adr/0164-a-ruleset-key-is-designer-facing-or-it-belongs-in-the-instrument.md).
